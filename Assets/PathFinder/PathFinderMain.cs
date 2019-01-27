using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;

using K_PathFinder.VectorInt;
using K_PathFinder.Settings;
using K_PathFinder.Graphs;
using K_PathFinder.PathGeneration;
using System.Text;
using K_PathFinder.Serialization;
using K_PathFinder.EdgesNameSpace;
using System.Linq;
using K_PathFinder.Trees;
using K_PathFinder.PFTools;
using UnityEngine.Profiling;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using K_PathFinder.PFDebuger;
using UnityEditor;
#endif


namespace K_PathFinder {
    public static class PathFinder {
        public const string VERSION = "0.49";
        public const int CELL_GRID_SIZE = 10; //hardcoded value. this value tell density of cell library in graph. 10x10 is good enough
        public const double AGENT_POSITION_UPDATE_COOLDOWN = 0.0166667d;//at least wait one frame between this measure

        //main values
        public static List<PathFinderAgent> agents = new List<PathFinderAgent>();
        public static PathFinderScene sceneInstance { get; private set; } //for coroutines and debug 
        private static PathFinderSettings _settings;
        private static bool _acceptingWork = true;
        private static bool _areInit = false;
        private static int _mainThreadID;        
        private static Dictionary<GeneralXZData, Graph> _chunkData = new Dictionary<GeneralXZData, Graph>(); //actual navmesh
        private static Dictionary<XZPosInt, YRangeInt> _chunkRange = new Dictionary<XZPosInt, YRangeInt>();  //chunk height difference
        private static AreaPassabilityHashData _hashData = new AreaPassabilityHashData(); //little thing to avoid accessing area library all time. we just send copy of it in every thread so it a lot less locked
        
        //map
        //private static Graph[,] map = new Graph[0, 0];
        //private static int mapMinX, mapMinY, mapMaxX, mapMaxY;
 
        //main thread control
        private static object threadLock = new object();
        private static object threadLockForVeryImportantThings = new object();
        private static Thread pathfinderMainThread;
        private static ManualResetEvent threadEvent = new ManualResetEvent(true);
        private static bool _mainThreadActive = false;

        //public values
        public static float gridSize;

        //queues. that where all type of tasks are sitting
        private static Dictionary<GeneralXZData, NavMeshTemplateCreation> _currentWorkDictionary = new Dictionary<GeneralXZData, NavMeshTemplateCreation>();//dictionary with queued work
        private static Queue<NavMeshTemplateCreation> templateQueueStage1 = new Queue<NavMeshTemplateCreation>();//queue to populate template in unity thread and then move to separated thread
        private static Queue<NavMeshTemplateCreation> templateQueueStage2 = new Queue<NavMeshTemplateCreation>();//queue to finalize graph in pathfinder thread
        private static Queue<NavMeshTemplateCreation> templateQueueStage3 = new Queue<NavMeshTemplateCreation>();//queue to finalize graph in unity thread
        private static WorkBatcher<NavMeshTemplateDestruction> destructQueue = new WorkBatcher<NavMeshTemplateDestruction>();

        //private static List<NavMeshTemplateDestruction> destructionList = new List<NavMeshTemplateDestruction>();//list to destroy graphs in pathfinder thread
        private static int activeThreads = 0;
        private static bool flagRVO = false;

        //update agent navmesh position variables
        private static ManualResetEvent[] _updateAgentPositionEvents;
        private struct UpdateAgentPositionThreadContext {
            public readonly int threadStart, threadEnd;
            public readonly ManualResetEvent manualResetEvent;
            public UpdateAgentPositionThreadContext(int start, int end, ManualResetEvent resetEvent) {
                threadStart = start;
                threadEnd = end;
                manualResetEvent = resetEvent;
            }
        }
        private static DateTime lastDateTimePositionUpdate;
        
        private static WorkBatcherThreadPool<PathTemplateMove> threadsMove = new WorkBatcherThreadPool<PathTemplateMove>();
        private static WorkBatcherThreadPool<InfoTemplateCover> threadsCover = new WorkBatcherThreadPool<InfoTemplateCover>();
        private static WorkBatcherThreadPool<InfoTemplateBattleGrid> threadsGrid = new WorkBatcherThreadPool<InfoTemplateBattleGrid>();
        private static WorkBatcherThreadPool<InfoTemplateAreaSearch> threadsAreaSearch = new WorkBatcherThreadPool<InfoTemplateAreaSearch>();
        
        //indexes
        enum NavMeshTaskType {
            navMesh = 0,
            graphFinish = 1,
            createChunk = 2,
            disconnect = 3
        }

        public static void RegisterAgent(PathFinderAgent agent) {
            lock (agents)
                agents.Add(agent);
        }

        public static void UnregisterAgent(PathFinderAgent agent) {
            lock (agents)
                agents.Remove(agent);
        }


        #region pipelines
        //Pipeline:      
        //1) populate template in UNITY main thread
        //2) continue in whatever thread
        //3) return to connection in pathfinder thread
        //4) finish unnesesary things in unity thread
        //unity thread > whatever thread > pathfinder thread > unity thread

        private static void PathFinderMainThread() {
            _mainThreadActive = true;
            while (true) {
                lock (threadLock) {
                    if (settings.useMultithread == false) {
                        Thread.Sleep(100);
                        continue;
                    }
                }
                
                threadEvent.WaitOne();

                lock (threadLockForVeryImportantThings) {
                    if (_mainThreadActive == false)
                        break;
                }

                var curDestruction = destructQueue.currentBatch;
                destructQueue.Flip();
                if(curDestruction.Count > 0) {
                    while (curDestruction.Count > 0) {
                        var current = curDestruction.Dequeue();
                        GeneralXZData currentXZData = current.data;

                        Graph graph;
                        lock (_chunkData) {
                            if (_chunkData.TryGetValue(currentXZData, out graph) == false)
                                continue;
                            _chunkData.Remove(currentXZData);
                        }
           
                        graph.OnDestroyGraph();

#if UNITY_EDITOR
                        Debuger_K.ClearChunksDebug(currentXZData);
#endif

                        if (current.queueNewGraphAfter)
                            QueueNavMeshTemplateToPopulation(currentXZData);
                    }
                }

                //stage 3 is where finished templates with finished graphs are ready to be assembled into navmesh
                //they do this one by one to avoid misconnections
                lock (templateQueueStage2) {
                    if (templateQueueStage2.Count > 0) {
                        while (templateQueueStage2.Count > 0) {
                            if (_acceptingWork == false)
                                return;

                            NavMeshTemplateCreation template = templateQueueStage2.Dequeue();

                            Graph graph = template.graph;
                            SetGraph(graph); //add graph to navmesh
                            graph.FunctionsToFinishGraphInPathfinderMainThread();

                            lock (templateQueueStage3) {
                                templateQueueStage3.Enqueue(template);
                            }
                        }
                    }
                }


                DateTime curTime = DateTime.Now;         
                if (curTime.Subtract(lastDateTimePositionUpdate).TotalSeconds > AGENT_POSITION_UPDATE_COOLDOWN) {
                    UpdateAgentNavmeshPosition();
                    kDAgentTree.BuildTree();
                    kDAgentTree.FindNearestAgentsAsync(32);
                    lastDateTimePositionUpdate = curTime;
                }

                int maxThreads = settings.maxThreads;
                threadsMove.PerformCurrentBatch(maxThreads);
                threadsCover.PerformCurrentBatch(maxThreads);
                threadsGrid.PerformCurrentBatch(maxThreads);
                threadsAreaSearch.PerformCurrentBatch(maxThreads);

                //rvo
                if (flagRVO) {
                    flagRVO = false;
                    try {
                        PathFinderMainRVO.UpdateAllAgents();
                    }
                    catch (Exception e) {
                        Debug.LogError(e);
                        throw;
                    }
                }

                if (threadsMove.haveWork == false & 
                    threadsCover.haveWork == false & 
                    threadsGrid.haveWork == false) {
                    lock (threadLock) {
                        int workCount = 0;
                        workCount += destructQueue.currentBatch.Count;
                        lock (templateQueueStage2)
                            workCount += templateQueueStage2.Count;

                        if (workCount == 0)
                            threadEvent.Reset();
                    }
                }
            }
        }

        private static void PathFinderSingleThreadedMainThreadForDebug() {
            //*******************NOT MULTITHREAD*******************//

            //removing graphs
            var curDestruction = destructQueue.currentBatch;
            destructQueue.Flip();
            if (curDestruction.Count > 0) {
                while (curDestruction.Count > 0) {
                    var current = curDestruction.Dequeue();
                    GeneralXZData currentXZData = current.data;

                    Graph graph;
                    if (_chunkData.TryGetValue(currentXZData, out graph) == false)
                        continue;
                    _chunkData.Remove(currentXZData);

                    graph.OnDestroyGraph();

#if UNITY_EDITOR
                    Debuger_K.ClearChunksDebug(currentXZData);
#endif

                    if (current.queueNewGraphAfter)
                        QueueNavMeshTemplateToPopulation(currentXZData);
                }
            }

            //creating graphs
            if (templateQueueStage1.Count > 0) {
                while (templateQueueStage1.Count > 0) {
                    NavMeshTemplateCreation template = templateQueueStage1.Dequeue();
                    template.PopulateTemplate();
                    template.GenerateGraph();
                    Graph graph = template.graph;
                    SetGraph(graph); //add graph to navmesh
                    graph.FunctionsToFinishGraphInPathfinderMainThread();
                    graph.FunctionsToFinishGraphInUnityThread();
                    graph.OnFinishGraph();
                    _currentWorkDictionary.Remove(new GeneralXZData(graph.gridPosition, graph.properties));

#if UNITY_EDITOR
                    if (Debuger_K.doDebug)
                        graph.DebugGraph();
#endif
                }
            }

            UpdateAgentNavmeshPosition();
            kDAgentTree.BuildTree();
            kDAgentTree.FindNearestAgentsAsync(32);

            int maxThreads = settings.maxThreads;
            threadsMove.PerformCurrentBatch(maxThreads);
            threadsCover.PerformCurrentBatch(maxThreads);
            threadsGrid.PerformCurrentBatch(maxThreads);
            threadsAreaSearch.PerformCurrentBatch(maxThreads);

            if (flagRVO) {
                flagRVO = false;
                PathFinderMainRVO.UpdateAllAgents();
            }

            //*******************NOT MULTITHREAD*******************//
        }

        private static IEnumerator PathfinderUnityThread() {
            while (true) {
                //ChunkContentMap.UpdateMods();
                //AreaWorldModManager.BuildTree();

                if (_acceptingWork == false) {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                if (multithread) {
                    //*******************MULTITHREAD*******************//
                    //populate new batch of work with colliders 
                    if (templateQueueStage1.Count != 0 && activeThreads <= _settings.maxThreads) {//if current active work count lesser than maximum count
                        while (templateQueueStage1.Count != 0 && activeThreads <= _settings.maxThreads) {//add new work while have work or exeed maximum number of work    
                            NavMeshTemplateCreation template = templateQueueStage1.Dequeue();//take template
                            template.PopulateTemplate();//populate it with colliders
                            template.callbackAfterGraphGeneration = (NavMeshTemplateCreation value) => {
                                lock (threadLock)
                                    activeThreads--;

                                PushTaskStage2Template(value);//push to pathfinder main thread this template after it finished
                            };

                            //start do things in whatever thread       
                            template.graphGenerationCallback = new WaitCallback(NavMeshTemplateCreation.ThreadWorker);
                            lock (threadLock)
                                activeThreads++;

                            ThreadPool.QueueUserWorkItem(template.graphGenerationCallback, template);//sending template to thread pool
                        }//unity really strugle doing this so find better way 
                    }

                    //finalize finished work
                    lock (templateQueueStage3) {//lock since template added to it in another thread
                        if (templateQueueStage3.Count != 0) {//if it have work at all
                            while (templateQueueStage3.Count != 0) {
                                NavMeshTemplateCreation template = templateQueueStage3.Dequeue();//take template

                                Graph graph = template.graph;
                                graph.FunctionsToFinishGraphInUnityThread();//finish work which use unity API
                                graph.OnFinishGraph();//set flag so graph finaly ready to work

                                //Debug.Log("Lock templateQueueStage3");
                                lock (_currentWorkDictionary)
                                    _currentWorkDictionary.Remove(new GeneralXZData(graph.gridPosition, graph.properties));//finaly remove template from dictionary
#if UNITY_EDITOR
                                if (Debuger_K.doDebug)
                                    graph.DebugGraph();
#endif
                            }
                        }
                    }
                    //*******************MULTITHREAD*******************//
                }
                else {
                    PathFinderSingleThreadedMainThreadForDebug();
                }
                yield return new WaitForEndOfFrame();
                continue;
            }
        }
        #endregion

        public static void Update() {
            //Debug.Log("Update");
            threadEvent.Set();
        }
        
        private static void PushTaskStage2Template(NavMeshTemplateCreation input) {
            lock (templateQueueStage2)
                templateQueueStage2.Enqueue(input);
            Update();
        }
        

        #region update agent info
        private static void UpdateAgentNavmeshPosition() {
            lock (agents) {
                int threads = settings.maxThreads;

                if (_updateAgentPositionEvents == null || _updateAgentPositionEvents.Length != threads) {
                    _updateAgentPositionEvents = new ManualResetEvent[threads];
                    for (int i = 0; i < settings.maxThreads; i++) {
                        _updateAgentPositionEvents[i] = new ManualResetEvent(true);
                    }
                }

                int curIndex = 0;
                int agentsPerThread = (agents.Count / threads) + 1;

                for (int i = 0; i < threads; i++) {
                    int end = curIndex + agentsPerThread;

                    if (end >= agents.Count) {
                        end = agents.Count;
                        _updateAgentPositionEvents[i].Reset();
                        ThreadPool.QueueUserWorkItem(UpdateAgentNavmeshPositionThreadPoolCallback, new UpdateAgentPositionThreadContext(curIndex, end, _updateAgentPositionEvents[i]));
                        break;
                    }
                    else {
                        _updateAgentPositionEvents[i].Reset();
                        ThreadPool.QueueUserWorkItem(UpdateAgentNavmeshPositionThreadPoolCallback, new UpdateAgentPositionThreadContext(curIndex, end, _updateAgentPositionEvents[i]));
                    }

                    curIndex = end;
                }

                WaitHandle.WaitAll(_updateAgentPositionEvents);

                //foreach (var agent in agents) {
                //    Vector2 agentPos = agent.positionVector2;
                //    XZPosInt p = ToChunkPosition(agentPos);
                //    ChunkAgentHolder holder;
                //    if (agentPositions.TryGetValue(p, out holder) == false) {
                //        holder = new ChunkAgentHolder(new ChunkData(p), 3);
                //        agentPositions.Add(p, holder);
                //    }

                //}
            }
        }
        private static void UpdateAgentNavmeshPositionThreadPoolCallback(System.Object threadContext) {
            try {
                UpdateAgentPositionThreadContext contex = (UpdateAgentPositionThreadContext)threadContext;

                Cell cell;
                bool outsideNavmesh;
                Vector3 closestPoint;

                for (int i = contex.threadStart; i < contex.threadEnd; i++) {
                    PathFinderAgent agent = agents[i];
                    if (agent.properties == null | agent.updateNavmeshPosition == false)
                        continue;

                    if (TryGetClosestCell(agent, out cell, out outsideNavmesh, out closestPoint)) {
                        lock (agent) {                
                            agent.nearestCell = cell;               
                            agent.nearestNavmeshPoint = closestPoint;
                            if (outsideNavmesh) {
                                Vector3 agentPos = agent.positionVector3;
                                if (SomeMath.SqrDistance(agentPos.x, agentPos.z, closestPoint.x, closestPoint.z) < 0.001f)
                                    outsideNavmesh = false;
                            }
                            agent.outsideNavmesh = outsideNavmesh;                      
                        }
                    }
                    else {
                        agent.outsideNavmesh = true;
                        agent.nearestNavmeshPoint = agent.positionVector3;
                    }
                }
                contex.manualResetEvent.Set();
            }
            catch (Exception e) {
                Debug.LogErrorFormat("Error occured while trying to find nearest point on navmesh for agent: {0}", e);        
                throw;
            }
        }
        #endregion
        public static void UpdateRVO() {
            lock (threadLock) {
                flagRVO = true;
                Update();
            }
        }
                
        private static void SetGraph(Graph graph) {
            //Debug.Log("set " + graph.gridPosition);
            lock (_chunkData) {
                _chunkData.Add(new GeneralXZData(graph.gridPosition, graph.properties), graph);
            }
        }

        #region editor
#if UNITY_EDITOR
        public static int DrawAreaSellector(int current) {
            return settings.DrawAreaSellector(current);
        }
#endif
        #endregion

        #region PathFinder management
        public static void SetMaxThreads(int value) {
            settings.maxThreads = Math.Max(value, 1);
        }
        public static void SetCurrentTerrainMethod(TerrainCollectorType type) {
            settings.terrainCollectionType = type;
        }
        //return if scene object was loaded
        public static bool Init(string comment = null) {
            if (!_areInit) {
                _areInit = true;
                //asume init was in main thread
                _mainThreadID = Thread.CurrentThread.ManagedThreadId;
                lastDateTimePositionUpdate = DateTime.Now;
             
            }

            //Debug.Log("Init " + comment + " : _settings == null " + (_settings == null));

            if (Thread.CurrentThread.ManagedThreadId != _mainThreadID)
                return false;

            if (_settings == null) {
                _settings = PathFinderSettings.LoadSettings();
                gridSize = _settings.gridSize;
                foreach (var item in _settings.areaLibrary) {
                    _hashData.AddAreaHash(item, true);
                }

                PathFinderMainRaycasting.Init();

#if UNITY_EDITOR
                if (Debuger_K.doDebug)
                    Debug.LogFormat("settings init");
#endif
            }

            //I REALLY NEED TO MAKE INITIALIZATION BETTER
            ColliderCollector.InitCollector();
            ChunkContentMap.Init(); 

            if (pathfinderMainThread == null) {     
                threadEvent = new ManualResetEvent(false);
                pathfinderMainThread = new Thread(PathFinderMainThread);
                pathfinderMainThread.Name = "Pathfinder Main Thread";
                pathfinderMainThread.Start();   
            }

            if (sceneInstance == null || sceneInstance.gameObject == null) {
                GameObject go = GameObject.Find(_settings.helperName);

                if (go == null) {
                    go = new GameObject(_settings.helperName);
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(go, "Created Pathfinder helper");
#endif
                }

                sceneInstance = go.GetComponent<PathFinderScene>();

                if (sceneInstance == null)
                    sceneInstance = go.AddComponent<PathFinderScene>();

                sceneInstance.AddCoroutine((int)NavMeshTaskType.navMesh, PathfinderUnityThread());
                //_sceneInstance.AddCoroutine((int)NavMeshTaskType.graphFinish, ChunkConnection());
                //_sceneInstance.AddCoroutine((int)NavMeshTaskType.disconnect, ChunkDisconnection());

                sceneInstance.InitComputeShaderRasterization3D(Resources.Load<ComputeShader>("ComputeShaderRasterization3D"));
                sceneInstance.InitComputeShaderRasterization2D(Resources.Load<ComputeShader>("ComputeShaderRasterization2D"));

                sceneInstance.Init();

                LoadCurrentSceneData();
                return true;
            }
            else
                return false;
        }

        public static void CallThisWhenSceneObjectWasGone() {
            if (sceneInstance == null)//if it's already null then do nothing
                return;
            
            Debug.Log("PathFinder: scene object was destroyed. clearing data and debug");
            sceneInstance = null;

            ClearAllWork();
            ChunkContentMap.Clear();
#if UNITY_EDITOR
            Debuger_K.ClearGeneric();
            Debuger_K.ClearChunksDebug();
#endif
        }
        
        public static void StopAcceptingWork() {
            _acceptingWork = false;
        }
        public static void StartAcceptingNewWork() {
            _acceptingWork = true;
        }
        public static void ClearAllWork() {
            Profiler.BeginSample("Start clearing currentWorkDictionary");
            //Debug.Log("Lock ClearAllWork");
            lock (_currentWorkDictionary) {
                //Debug.Log("_currentWorkDictionary: " + _currentWorkDictionary.Count);
                foreach (var item in _currentWorkDictionary.Values) {
                    item.Stop();
                }
                _currentWorkDictionary.Clear();
            }
            Profiler.EndSample();

            Profiler.BeginSample("Start clearing chunk data");
            lock (_chunkData)
                _chunkData.Clear();
            Profiler.EndSample();

            Profiler.BeginSample("Start clearing current work");

            templateQueueStage1.Clear(); //it is thread safe

            lock (templateQueueStage2)
                templateQueueStage2.Clear();

            lock (templateQueueStage3)
                templateQueueStage3.Clear();

            destructQueue.Clear();

            Profiler.EndSample();

            if (sceneInstance != null)
                sceneInstance.StopAll();            
        }
        public static void Shutdown() {
            StopAcceptingWork();
            ClearAllWork();
            sceneInstance.Shutdown();
            _areInit = false;
            lock (threadLockForVeryImportantThings) {
                _mainThreadActive = false;
            }
            Update();

        }
        #endregion

        private static bool AreTemplateInProcess(XZPosInt position, AgentProperties properties) {
            //Debug.Log("Lock AreTemplateInProcess");
            lock (_currentWorkDictionary) {
                return _currentWorkDictionary.ContainsKey(new GeneralXZData(position, properties));
            }
        }


        /// <summary>
        /// Return area from global dictionary by it's ID.
        /// ID is writen on the left side in PathFinder menu.
        /// 0 = Default, 1 = Not Walkble.
        /// </summary>
        public static Area GetArea(int id) {
            Init();
            if (SomeMath.InRangeInclusive(id, 0, settings.areaLibrary.Count - 1))
                return settings.areaLibrary[id];
            else {
                Debug.LogWarning("Requested Area index are higher than maximum index. Returned Default");
                return settings.areaLibrary[0];
            }
        }

        /// <summary>
        /// return Default area which have id 0
        /// </summary>
        public static Area getDefaultArea {
            get { return settings.areaLibrary[0]; }
        }

        /// <summary>
        /// return Unwalkable area which have id 1
        /// </summary>
        public static Area getUnwalkableArea {
            get { return settings.areaLibrary[1]; }
        }

        #region path generation and info extraction
        public static void GetPath(PathFinderAgent agent, Vector3 target, Vector3 start, bool snapToNavMesh, Action callBack, bool applyRaycastBeforeFunnel = false, bool ignoreCrouchCost = false) {
            Init();

            if (_acceptingWork == false)
                return;

            threadsMove.AddWorkPooled(new PathTemplateMove.WorkContext(agent, target, start, snapToNavMesh, applyRaycastBeforeFunnel, ignoreCrouchCost, callBack));
            Update();
        }

        public static void GetCover(PathFinderAgent agent, float maxMoveCost, Action callBack, bool ignoreCrouchCost = false) {
            Init();

            if (_acceptingWork == false)
                return;

            threadsCover.AddWorkPooled(new InfoTemplateCover.WorkContext(agent, maxMoveCost, ignoreCrouchCost, callBack));
            Update();
        }

        public static void GetBattleGrid(PathFinderAgent agent, int depth, Action callBack, params Vector3[] vectors) {
            Init();

            if (_acceptingWork == false)
                return;

            threadsGrid.AddWorkPooled(new InfoTemplateBattleGrid.WorkContext(agent, depth, vectors, callBack));
            Update();
        }

        /// <summary>
        /// if searchToArea == false then agent will search outside sellected area
        /// </summary>
        public static void GetPathAreaSearch(PathFinderAgent agent, Area target, bool searchToArea, float maxSearchCost, Vector3 start, bool snapToNavMesh,
            Action callBack, bool applyRaycastBeforeFunnel = false, bool ignoreCrouchCost = false) {
            Init();

            if (_acceptingWork == false)
                return;

            threadsAreaSearch.AddWorkPooled(new InfoTemplateAreaSearch.WorkContext(agent, target, maxSearchCost, searchToArea, start, snapToNavMesh, callBack, applyRaycastBeforeFunnel, ignoreCrouchCost));
            Update();
        }


        #region raycasting
        //Personal guideline:
        //1) Make use of GetCellForRaycast. It is pain when raycast started at corner or edge of chunk and only bugs comes from this. 
        //   It will slightly offset XZ and it is much better then gazzilion other problems. Much better consistent offset then unexpected results.
        //2) Main parameters for raycastings is "position", "direction" and "properties". Everything else is optional. So all raycast functions take firstly this three and THEN everything else

        //Notes:
        //* Starting position can be altered not only in GetCellForRaycast but also in raycasting itself. Cause it also can be started on cell Edge. 
        //  There is actualy myltiple ways to fix that but when start position are at exact start or end point of edge then it is disaster.
        //  So if starting point are too close to cell edge then it will be offseted to direction of cell center. Which guarantee that it will be still enclosed by Cell.

        //TODO:
        // * Variations of raycasting without range parameter

        public static bool GetCellForRaycast(ref float x, ref float y, ref float z, AgentProperties properties, out Cell cell) {
            //slight offset in case when request exactly on edges and corners
            if (x % gridSize <= 0.0001f)
                x += 0.001f;
            if (z % gridSize <= 0.0001f)
                z += 0.001f;

            return TryGetCell(x, y, z, properties, out cell);
        }

        #region agent raycast
        #region single
        //generic private function
        //only viable if agent have nearest navmesh point info
        private static bool Raycast(PathFinderAgent agent, float directionX, float directionZ, float maxRange, Area expectedArea, bool checkArea, Passability expectedPassability, bool checkPassability, out RaycastHitNavMesh2 hit) {
            if(agent.outsideNavmesh) {
                hit = new RaycastHitNavMesh2(agent.positionVector3, NavmeshRaycastResultType2.OutsideGraph, null);
                return false;
            }

            Cell cell = agent.nearestCell;
            Vector3 pos = agent.nearestNavmeshPoint;
            float x = pos.x;
            float z = pos.z;

            if (x % gridSize == 0)
                x += 0.001f;
            if (z % gridSize == 0)
                z += 0.001f; 

            //check is cell have expected area
            if (checkArea && cell.area != expectedArea) {
                hit = new RaycastHitNavMesh2(x, pos.y, z, NavmeshRaycastResultType2.AreaChange, cell);
                return true;
            }
            //check is cell have expected passability
            if (checkPassability && cell.passability != expectedPassability) {
                hit = new RaycastHitNavMesh2(x, pos.y, z, NavmeshRaycastResultType2.PassabilityChange, cell);
                return true;
            }

            RaycastAllocatedData allocated = PathFinderMainRaycasting.Rent();//take allocated data to perform raycast
            PathFinderMainRaycasting.Raycast2Body2(x, pos.y, z, directionX, directionZ, cell, maxRange, checkArea, checkPassability, expectedArea, expectedPassability, allocated, out hit);//perform raycast
            PathFinderMainRaycasting.Return(allocated);//return allocated data to pool
            return (int)hit.resultType > 1;//return true if it too close to borders to perform raycast, or hit border, or hit cell with change in expected area or passability (if it checked)
        }

        /// <summary>
        /// Agent navmesh raycast. Only viable if agent update it's navmesh information
        /// Take position and direction. Note direction is X and Z axis. You specify direction in top down view.
        /// return true pretty much always exept agent start outside navmesh
        /// </summary>     
        public static bool Raycast(PathFinderAgent agent, float directionX, float directionZ, out RaycastHitNavMesh2 hit) {
            return Raycast(agent, directionX, directionZ, float.MaxValue, null, false, Passability.Unwalkable, false, out hit);
        }

        /// <summary>
        /// Agent navmesh raycast. Only viable if agent update it's navmesh information
        /// Take position, direction and max range. Note direction is X and Z axis. You specify direction in top down view.
        /// return true if outside navmesh or hit distance is closer than maxRange. return false only when hit outside maxRange
        /// </summary>       
        public static bool Raycast(PathFinderAgent agent, float directionX, float directionZ, float maxRange, out RaycastHitNavMesh2 hit) {
            return Raycast(agent, directionX, directionZ, maxRange, null, false, Passability.Unwalkable, false, out hit);
        }

        /// <summary>
        /// Agent navmesh raycast. Only viable if agent update it's navmesh information
        /// Take position, direction, max range and expected Area. Note direction is X and Z axis. You specify direction in top down view.
        /// return true if outside navmesh or hit distance is closer than maxRange. Hit triggered if cell dont have expected Area. return false only when hit outside maxRange
        /// </summary>       
        public static bool Raycast(PathFinderAgent agent, float directionX, float directionZ,float maxRange, Area expectedArea, out RaycastHitNavMesh2 hit) {
            return Raycast(agent, directionX, directionZ, maxRange, expectedArea, true, Passability.Unwalkable, false, out hit);
        }

        /// <summary>
        /// Agent navmesh raycast. Only viable if agent update it's navmesh information
        /// Take position, direction, max range and expected Passability. Note direction is X and Z axis. You specify direction in top down view.
        /// return true if outside navmesh or hit distance is closer than maxRange. Hit triggered if cell dont have expected Passability. return false only when hit outside maxRange
        /// </summary>       
        public static bool Raycast(PathFinderAgent agent, float directionX, float directionZ, float maxRange, Passability expectedPassability, out RaycastHitNavMesh2 hit) {
            return Raycast(agent, directionX, directionZ, maxRange, null, false, expectedPassability, true, out hit);
        }
        
        /// <summary>
        /// Agent navmesh raycast. Only viable if agent update it's navmesh information
        /// Take position, direction, max range, expected Area and Passability. Note direction is X and Z axis. You specify direction in top down view.
        /// return true if outside navmesh or hit distance is closer than maxRange. Hit triggered if cell dont have expected Passability. return false only when hit outside maxRange
        /// </summary>       
        public static bool Raycast(PathFinderAgent agent, float directionX, float directionZ, float maxRange, Area expectedArea, Passability expectedPassability, out RaycastHitNavMesh2 hit) {
            return Raycast(agent, directionX, directionZ, maxRange, expectedArea, true, expectedPassability, true, out hit);
        }
        #endregion

        #region multiple with ONE range parameter
        private static bool Raycast(PathFinderAgent agent, Vector2[] directions, float maxRange, Area expectedArea, bool checkArea, Passability expectedPassability, bool checkPassability, ref RaycastHitNavMesh2[] hit, bool checkResult) {
            int length = directions.Length;

            if (hit == null || hit.Length != length) {
                hit = new RaycastHitNavMesh2[length];
            }

            if (agent.outsideNavmesh) {
                hit[0] = new RaycastHitNavMesh2(agent.positionVector3, NavmeshRaycastResultType2.OutsideGraph, null);
                return false;
            }

            Cell cell = agent.nearestCell;
            Vector3 pos = agent.nearestNavmeshPoint;
            float x = pos.x;
            float z = pos.z;

            if (x % gridSize == 0)
                x += 0.001f;
            if (z % gridSize == 0)
                z += 0.001f;

            //check is cell have expected area
            if (checkArea && cell.area != expectedArea) {
                hit[0] = new RaycastHitNavMesh2(x, pos.y, z, NavmeshRaycastResultType2.AreaChange, cell);
                return true;
            }

            //check is cell have expected passability
            if (checkPassability && cell.passability != expectedPassability) {
                hit[0] = new RaycastHitNavMesh2(x, pos.y, z, NavmeshRaycastResultType2.PassabilityChange, cell);
                return true;
            }
                            
            Vector2 cur;
            RaycastAllocatedData allocated = PathFinderMainRaycasting.Rent();
            for (int i = 0; i < length; i++) {
                cur = directions[i];
                RaycastHitNavMesh2 curHit;
                PathFinderMainRaycasting.Raycast2Body2(x, pos.y, z, cur.x, cur.y, cell, maxRange, checkArea, checkPassability, expectedArea, expectedPassability, allocated, out curHit);
                hit[i] = curHit;
            }
            PathFinderMainRaycasting.Return(allocated);


            if (checkResult) {
                for (int i = 0; i < length; i++) {
                    if ((int)hit[i].resultType > 1)
                        return true;
                }
                return false;
            }
            else
                return true;
        }

        //only position and direction
        public static bool Raycast(PathFinderAgent agent, Vector2[] directions, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(agent, directions, float.MaxValue, null, false, Passability.Unwalkable, false, ref hit, checkResult);
        }
        //range
        public static bool Raycast(PathFinderAgent agent, Vector2[] directions, float maxRange, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(agent, directions, maxRange, null, false, Passability.Unwalkable, false, ref hit, checkResult);
        }
        //range, area
        public static bool Raycast(PathFinderAgent agent, Vector2[] directions, float maxRange, Area expectedArea, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(agent, directions, maxRange, expectedArea, true, Passability.Unwalkable, false, ref hit, checkResult);
        }
        //range, passability
        public static bool Raycast(PathFinderAgent agent, Vector2[] directions, float maxRange, Passability expectedPassability, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(agent, directions, maxRange, null, false, expectedPassability, true, ref hit, checkResult);
        }
        //range, area, passability
        public static bool Raycast(PathFinderAgent agent, Vector2[] directions, float maxRange, Area expectedArea, Passability expectedPassability, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(agent, directions, maxRange, expectedArea, true, expectedPassability, true, ref hit, checkResult);
        }
        #endregion

        #region multiple with MULTIPLE range parameter
        //generic private function to single raycast
        private static bool Raycast(PathFinderAgent agent, Vector2[] directions, float[] maxRanges, Area expectedArea, bool checkArea, Passability expectedPassability, bool checkPassability, ref RaycastHitNavMesh2[] hit, bool checkResult) {
            int length = directions.Length;

            if (hit == null || hit.Length != length) {
                hit = new RaycastHitNavMesh2[length];
            }

            if (agent.outsideNavmesh) {
                hit[0] = new RaycastHitNavMesh2(agent.positionVector3, NavmeshRaycastResultType2.OutsideGraph, null);
                return false;
            }

            Cell cell = agent.nearestCell;
            Vector3 pos = agent.nearestNavmeshPoint;
            float x = pos.x;
            float z = pos.z;

            if (x % gridSize == 0)
                x += 0.001f;
            if (z % gridSize == 0)
                z += 0.001f;

            //check is cell have expected area
            if (checkArea && cell.area != expectedArea) {
                hit[0] = new RaycastHitNavMesh2(x, pos.y, z, NavmeshRaycastResultType2.AreaChange, cell);
                return true;
            }

            //check is cell have expected passability
            if (checkPassability && cell.passability != expectedPassability) {
                hit[0] = new RaycastHitNavMesh2(x, pos.y, z, NavmeshRaycastResultType2.PassabilityChange, cell);
                return true;
            }

     
            Vector2 cur;
            RaycastAllocatedData allocated = PathFinderMainRaycasting.Rent();

            if (maxRanges != null) {
                for (int i = 0; i < length; i++) {
                    cur = directions[i];
                    RaycastHitNavMesh2 curHit;
                    PathFinderMainRaycasting.Raycast2Body2(x, pos.y, z, cur.x, cur.y, cell, maxRanges[i], checkArea, checkPassability, expectedArea, expectedPassability, allocated, out curHit);
                    hit[i] = curHit;
                }
            }
            else {
                for (int i = 0; i < length; i++) {
                    cur = directions[i];
                    RaycastHitNavMesh2 curHit;
                    PathFinderMainRaycasting.Raycast2Body2(x, pos.y, z, cur.x, cur.y, cell, float.MaxValue, checkArea, checkPassability, expectedArea, expectedPassability, allocated, out curHit);
                    hit[i] = curHit;
                }
            }
            PathFinderMainRaycasting.Return(allocated);


            if (checkResult) {
                for (int i = 0; i < length; i++) {
                    if ((int)hit[i].resultType > 1)
                        return true;
                }
                return false;
            }
            else
                return true;
        }
        //range
        public static bool Raycast(PathFinderAgent agent, Vector2[] directions, float[] maxRanges, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(agent, directions, maxRanges, null, false, Passability.Unwalkable, false, ref hit, checkResult);
        }
        //range, area
        public static bool Raycast(PathFinderAgent agent, Vector2[] directions, float[] maxRanges, Area expectedArea, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(agent, directions, maxRanges, expectedArea, true, Passability.Unwalkable, false, ref hit, checkResult);
        }
        //range, passability
        public static bool Raycast(PathFinderAgent agent, Vector2[] directions, float[] maxRanges, Passability expectedPassability, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(agent, directions, maxRanges, null, false, expectedPassability, true, ref hit, checkResult);
        }
        //range, area, passability
        public static bool Raycast(PathFinderAgent agent, Vector2[] directions, float[] maxRanges, Area expectedArea, Passability expectedPassability, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(agent, directions, maxRanges, expectedArea, true, expectedPassability, true, ref hit, checkResult);
        }
        #endregion
        #endregion

        #region normal raycast
        #region single raycast
        //generic private function
        private static bool Raycast(float x, float y, float z, float directionX, float directionZ, AgentProperties properties, float maxRange, Area expectedArea, bool checkArea, Passability expectedPassability, bool checkPassability, out RaycastHitNavMesh2 hit) {
            //get start cell
            Cell cell;
            if (GetCellForRaycast(ref x, ref y, ref z, properties, out cell) == false) {
                hit = new RaycastHitNavMesh2(x, y, z, NavmeshRaycastResultType2.OutsideGraph, null);
                return false;
            }

            //check is cell have expected area
            if(checkArea && cell.area != expectedArea) {
                hit = new RaycastHitNavMesh2(x, y, z, NavmeshRaycastResultType2.AreaChange, cell);
                return true;
            }
            //check is cell have expected passability
            if (checkPassability && cell.passability != expectedPassability) {
                hit = new RaycastHitNavMesh2(x, y, z, NavmeshRaycastResultType2.PassabilityChange, cell);
                return true;
            }

            RaycastAllocatedData allocated = PathFinderMainRaycasting.Rent();//take allocated data to perform raycast
            PathFinderMainRaycasting.Raycast2Body2(x, y, z, directionX, directionZ, cell, maxRange, checkArea, checkPassability, expectedArea, expectedPassability, allocated, out hit);//perform raycast
            PathFinderMainRaycasting.Return(allocated);//return allocated data to pool
            return (int)hit.resultType > 1;//return true if it too close to borders to perform raycast, or hit border, or hit cell with change in expected area or passability (if it checked)
        }

        //only position and direction
        /// <summary>
        /// Navmesh raycast.
        /// Take position and direction. Note direction is X and Z axis. You specify direction in top down view.
        /// Normaly raycast return true when it outside border or hit something - this one return pretty much always true since max range is float.MaxValue. So this function just void
        /// </summary>     
        public static void Raycast(float x, float y, float z, float directionX, float directionZ, AgentProperties properties, out RaycastHitNavMesh2 hit) {
            Raycast(x, y, z, directionX, directionZ, properties, float.MaxValue, null, false, Passability.Unwalkable, false, out hit);
        }
        /// <summary>
        /// Navmesh raycast.
        /// Take position and direction. Note direction is X and Z axis. You specify direction in top down view.
        /// return true pretty much always exept when raycase start outside navmesh
        /// </summary>     
        public static bool Raycast(Vector3 position, Vector2 directionXZ, AgentProperties properties, out RaycastHitNavMesh2 hit) {
            return Raycast(position.x, position.y, position.z, directionXZ.x, directionXZ.y, properties, float.MaxValue, null, false, Passability.Unwalkable, false, out hit);
        }
        //range
        /// <summary>
        /// Navmesh raycast.
        /// Take position, direction and max range. Note direction is X and Z axis. You specify direction in top down view.
        /// return true if outside navmesh or hit distance is closer than maxRange. return false only when hit outside maxRange
        /// </summary>       
        public static bool Raycast(float x, float y, float z, float directionX, float directionZ, AgentProperties properties, float maxRange,  out RaycastHitNavMesh2 hit) {
            return Raycast(x, y, z, directionX, directionZ, properties, maxRange, null, false, Passability.Unwalkable, false, out hit);
        }
        /// <summary>
        /// Navmesh raycast.
        /// Take position, direction and max range. Note direction is X and Z axis. You specify direction in top down view.
        /// return true if outside navmesh or hit distance is closer than maxRange. return false only when hit outside maxRange
        /// </summary>       
        public static bool Raycast(Vector3 position, Vector2 directionXZ, float maxRange, AgentProperties properties, out RaycastHitNavMesh2 hit) {
            return Raycast(position.x, position.y, position.z, directionXZ.x, directionXZ.y, properties, maxRange, null, false, Passability.Unwalkable, false, out hit);
        }
        //range, area
        /// <summary>
        /// Navmesh raycast.
        /// Take position, direction, max range and expected Area. Note direction is X and Z axis. You specify direction in top down view.
        /// return true if outside navmesh or hit distance is closer than maxRange. Hit triggered if cell dont have expected Area. return false only when hit outside maxRange
        /// </summary>       
        public static bool Raycast(float x, float y, float z, float directionX, float directionZ, AgentProperties properties, float maxRange, Area expectedArea, out RaycastHitNavMesh2 hit) {
            return Raycast(x, y, z, directionX, directionZ, properties, maxRange, expectedArea, true, Passability.Unwalkable, false, out hit);
        }
        /// <summary>
        /// Navmesh raycast.
        /// Take position, direction, max range and expected Area. Note direction is X and Z axis. You specify direction in top down view.
        /// return true if outside navmesh or hit distance is closer than maxRange. Hit triggered if cell dont have expected Area. return false only when hit outside maxRange
        /// </summary>       
        public static bool Raycast(Vector3 position, Vector2 directionXZ, AgentProperties properties, float maxRange, Area expectedArea, out RaycastHitNavMesh2 hit) {
            return Raycast(position.x, position.y, position.z, directionXZ.x, directionXZ.y, properties, maxRange, expectedArea, true, Passability.Unwalkable, false, out hit);
        }
        //range, passability
        /// <summary>
        /// Navmesh raycast.
        /// Take position, direction, max range and expected Passability. Note direction is X and Z axis. You specify direction in top down view.
        /// return true if outside navmesh or hit distance is closer than maxRange. Hit triggered if cell dont have expected Passability. return false only when hit outside maxRange
        /// </summary>       
        public static bool Raycast(float x, float y, float z, float directionX, float directionZ, AgentProperties properties, float maxRange, Passability expectedPassability, out RaycastHitNavMesh2 hit) {
            return Raycast(x, y, z, directionX, directionZ, properties, maxRange, null, false, expectedPassability, true, out hit);
        }
        /// <summary>
        /// Navmesh raycast.
        /// Take position, direction, max range and expected Passability. Note direction is X and Z axis. You specify direction in top down view.
        /// return true if outside navmesh or hit distance is closer than maxRange. Hit triggered if cell dont have expected Passability. return false only when hit outside maxRange
        /// </summary>            
        public static bool Raycast(Vector3 position, Vector2 directionXZ, AgentProperties properties, float maxRange, Passability expectedPassability, out RaycastHitNavMesh2 hit) {
            return Raycast(position.x, position.y, position.z, directionXZ.x, directionXZ.y, properties, maxRange, null, false, expectedPassability, true, out hit);
        }
        //range, area, passability
        /// <summary>
        /// Navmesh raycast.
        /// Take position, direction, max range, expected Area and Passability. Note direction is X and Z axis. You specify direction in top down view.
        /// return true if outside navmesh or hit distance is closer than maxRange. Hit triggered if cell dont have expected Passability. return false only when hit outside maxRange
        /// </summary>       
        public static bool Raycast(float x, float y, float z, float directionX, float directionZ, AgentProperties properties, float maxRange, Area expectedArea, Passability expectedPassability, out RaycastHitNavMesh2 hit) {
            return Raycast(x, y, z, directionX, directionZ, properties, maxRange, expectedArea, true, expectedPassability, true, out hit);
        }
        /// <summary>
        /// Navmesh raycast.
        /// Take position, direction, max range, expected Area and Passability. Note direction is X and Z axis. You specify direction in top down view.
        /// return true if outside navmesh or hit distance is closer than maxRange. Hit triggered if cell dont have expected Passability. return false only when hit outside maxRange
        /// </summary>             
        public static bool Raycast(Vector3 position, Vector2 directionXZ, AgentProperties properties, float maxRange, Area expectedArea, Passability expectedPassability, out RaycastHitNavMesh2 hit) {
            return Raycast(position.x, position.y, position.z, directionXZ.x, directionXZ.y, properties, maxRange, expectedArea, true, expectedPassability, true, out hit);
        }

        public static bool RaycastForMoveTemplate(float x, float y, float z, float dirX, float dirY, float range, Cell cell, RaycastAllocatedData allocated, out RaycastHitNavMesh2 hit) {
            PathFinderMainRaycasting.Raycast2Body2(x, y, z, dirX, dirY, cell, range, true, true, cell.area, cell.passability, allocated, out hit);//perform raycast
            return (int)hit.resultType > 1;//return true if it too close to borders to perform raycast, or hit border, or hit cell with change in expected area or passability (if it checked)
        }
        #endregion

        #region multiple with ONE range parameter
        //generic private function to single raycast
        private static bool Raycast(float x, float y, float z, Vector2[] directions, AgentProperties properties, float maxRange, Area expectedArea, bool checkArea, Passability expectedPassability, bool checkPassability, ref RaycastHitNavMesh2[] hit, bool checkResult) {
            int length = directions.Length;

            if (hit == null || hit.Length != length) {
                hit = new RaycastHitNavMesh2[length];
            }

            Cell cell;
            if (GetCellForRaycast(ref x, ref y, ref z, properties, out cell) == false) {
                hit[0] = new RaycastHitNavMesh2(x, y, z, NavmeshRaycastResultType2.OutsideGraph, null);
                return false;
            }

            //check is cell have expected area
            if (checkArea && cell.area != expectedArea) {
                hit[0] = new RaycastHitNavMesh2(x, y, z, NavmeshRaycastResultType2.AreaChange, cell);
                return true;
            }
            //check is cell have expected passability
            if (checkPassability && cell.passability != expectedPassability) {
                hit[0] = new RaycastHitNavMesh2(x, y, z, NavmeshRaycastResultType2.PassabilityChange, cell);
                return true;
            }

     

            //Vector3 start = new Vector3(x, y, z);
            Vector2 cur;
            RaycastAllocatedData allocated = PathFinderMainRaycasting.Rent();

            for (int i = 0; i < length; i++) {
                cur = directions[i];
                RaycastHitNavMesh2 curHit;
                PathFinderMainRaycasting.Raycast2Body2(x, y, z, cur.x, cur.y, cell, maxRange, checkArea, checkPassability, expectedArea, expectedPassability, allocated, out curHit);
                hit[i] = curHit;
            }
            PathFinderMainRaycasting.Return(allocated);


            if (checkResult) {
                for (int i = 0; i < length; i++) {
                    if ((int)hit[i].resultType > 1)
                        return true;
                }
                return false;
            }
            else
                return true;
        }

        //only position and direction
        public static bool Raycast(float x, float y, float z, Vector2[] directions, AgentProperties properties, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(x, y, z, directions, properties, float.MaxValue, null, false, Passability.Unwalkable, false, ref hit, checkResult);
        }
        //range
        public static bool Raycast(float x, float y, float z, Vector2[] directions, AgentProperties properties, float maxRange, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(x, y, z, directions, properties, maxRange, null, false, Passability.Unwalkable, false, ref hit, checkResult);
        }
        //range, area
        public static bool Raycast(float x, float y, float z, Vector2[] directions, AgentProperties properties, float maxRange, Area expectedArea, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(x, y, z, directions, properties, maxRange, expectedArea, true, Passability.Unwalkable, false, ref hit, checkResult);
        }
        //range, passability
        public static bool Raycast(float x, float y, float z, Vector2[] directions, AgentProperties properties, float maxRange, Passability expectedPassability, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(x, y, z, directions, properties, maxRange, null, false, expectedPassability, true, ref hit, checkResult);
        }
        //range, area, passability
        public static bool Raycast(float x, float y, float z, Vector2[] directions, AgentProperties properties, float maxRange, Area expectedArea, Passability expectedPassability, ref RaycastHitNavMesh2[] hit, bool checkResult = true) {
            return Raycast(x, y, z, directions, properties, maxRange, expectedArea, true, expectedPassability, true, ref hit, checkResult);
        }
        #endregion
        
        #region multiple with MULTIPLE range parameter
        //generic private function to single raycast
        private static bool Raycast(float x, float y, float z, Vector2[] directions, AgentProperties properties, float[] maxRanges, Area expectedArea, bool checkArea, Passability expectedPassability, bool checkPassability, ref RaycastHitNavMesh2[] hit, bool checkResult) {
            int length = directions.Length;

            if (hit == null || hit.Length != length) {
                hit = new RaycastHitNavMesh2[length];
            }

            Cell cell;
            if (GetCellForRaycast(ref x, ref y, ref z, properties, out cell) == false) {
                hit[0] = new RaycastHitNavMesh2(x, y, z, NavmeshRaycastResultType2.OutsideGraph, null);
                return false;
            }

            //check is cell have expected area
            if (checkArea && cell.area != expectedArea) {
                hit[0] = new RaycastHitNavMesh2(x, y, z, NavmeshRaycastResultType2.AreaChange, cell);
                return true;
            }
            //check is cell have expected passability
            if (checkPassability && cell.passability != expectedPassability) {
                hit[0] = new RaycastHitNavMesh2(x, y, z, NavmeshRaycastResultType2.PassabilityChange, cell);
                return true;
            }
            
            Vector2 cur;
            RaycastAllocatedData allocated = PathFinderMainRaycasting.Rent();

            if (maxRanges != null) {
                for (int i = 0; i < length; i++) {
                    cur = directions[i];
                    RaycastHitNavMesh2 curHit;
                    PathFinderMainRaycasting.Raycast2Body2(x, y, z, cur.x, cur.y, cell, maxRanges[i], checkArea, checkPassability, expectedArea, expectedPassability, allocated, out curHit);
                    hit[i] = curHit;
                }
            }
            else {
                for (int i = 0; i < length; i++) {
                    cur = directions[i];
                    RaycastHitNavMesh2 curHit;
                    PathFinderMainRaycasting.Raycast2Body2(x, y, z, cur.x, cur.y, cell, float.MaxValue, checkArea, checkPassability, expectedArea, expectedPassability, allocated, out curHit);
                    hit[i] = curHit;
                }
            }

            PathFinderMainRaycasting.Return(allocated);


            if (checkResult) {
                for (int i = 0; i < length; i++) {
                    if ((int)hit[i].resultType > 1)
                        return true;
                }
                return false;
            }
            else
                return true;
        }
        //range
        public static bool Raycast(float x, float y, float z, Vector2[] directions, AgentProperties properties, float[] maxRanges, ref RaycastHitNavMesh2[] hit, bool checkResult = false) {
            return Raycast(x, y, z, directions, properties, maxRanges, null, false, Passability.Unwalkable, false, ref hit, checkResult);
        }
        //range, area
        public static bool Raycast(float x, float y, float z, Vector2[] directions, AgentProperties properties, float[] maxRanges, Area expectedArea, ref RaycastHitNavMesh2[] hit, bool checkResult = false) {
            return Raycast(x, y, z, directions, properties, maxRanges, expectedArea, true, Passability.Unwalkable, false, ref hit, checkResult);
        }
        //range, passability
        public static bool Raycast(float x, float y, float z, Vector2[] directions, AgentProperties properties, float[] maxRanges, Passability expectedPassability, ref RaycastHitNavMesh2[] hit, bool checkResult = false) {
            return Raycast(x, y, z, directions, properties, maxRanges, null, false, expectedPassability, true, ref hit, checkResult);
        }
        //range, area, passability
        public static bool Raycast(float x, float y, float z, Vector2[] directions, AgentProperties properties, float[] maxRanges, Area expectedArea, Passability expectedPassability, ref RaycastHitNavMesh2[] hit, bool checkResult = false) {
            return Raycast(x, y, z, directions, properties, maxRanges, expectedArea, true, expectedPassability, true, ref hit, checkResult);
        }
        #endregion
        #endregion        
        #endregion

        #region management
        //give me Graph
        public static bool GetGraph(XZPosInt pos, AgentProperties properties, out Graph graph) {
            Init("GetGraph");
 
            lock (_chunkData) {
                GeneralXZData key = new GeneralXZData(pos, properties);

                if (_chunkData.TryGetValue(key, out graph))
                    return true;
                else {
                    QueueNavMeshTemplateToPopulation(pos, properties);
                    return false;
                }
            }
        }
        public static bool GetGraph(int x, int z, AgentProperties properties, out Graph graph) {   
            return GetGraph(new XZPosInt(x, z), properties, out graph);
        }
        public static bool GetGraphFrom(XZPosInt pos, Directions direction, AgentProperties properties, out Graph graph) {
            switch (direction) {
                case Directions.xPlus:
                return GetGraph(pos.x + 1, pos.z, properties, out graph);

                case Directions.xMinus:
                return GetGraph(pos.x - 1, pos.z, properties, out graph);

                case Directions.zPlus:
                return GetGraph(pos.x, pos.z + 1, properties, out graph);

                case Directions.zMinus:
                return GetGraph(pos.x, pos.z - 1, properties, out graph);

                default:
                    Debug.LogError("defaul direction are not exist");
                    graph = null;
                    return false;
            }
        }




        //try give me Graph 
        public static bool TryGetGraph(XZPosInt pos, AgentProperties properties, out Graph graph) {
            return _chunkData.TryGetValue(new GeneralXZData(pos, properties), out graph);
        }
        public static bool TryGetGraph(int x, int z, AgentProperties properties, out Graph graph) {
            return TryGetGraph(new XZPosInt(x, z), properties, out graph);
        }
        public static bool TryGetGraphFrom(XZPosInt pos, Directions direction, AgentProperties properties, out Graph graph) {
            switch (direction) {
                case Directions.xPlus:
                    return TryGetGraph(pos.x + 1, pos.z, properties, out graph);

                case Directions.xMinus:
                    return TryGetGraph(pos.x - 1, pos.z, properties, out graph);

                case Directions.zPlus:
                    return TryGetGraph(pos.x, pos.z + 1, properties, out graph);

                case Directions.zMinus:
                    return TryGetGraph(pos.x, pos.z - 1, properties, out graph);

                default:
                    Debug.LogError("defaul direction are not exist");
                    graph = null;
                    return false;
            }
        } 


        private static void FixPosition(ref float x, ref float z) {
            if (x % gridSize < 0.001f)
                x += 0.001f;
            if (z % gridSize < 0.001f)
                z += 0.001f;
        }

        /// <summary>
        /// try get closest Cell.
        /// return closest position to navmesh (inside chunk where position are)
        /// </summary> 
        public static bool TryGetClosestCell(float x, float y, float z, AgentProperties properties, out Cell cell, out bool outsideCell, out Vector3 closestPoint) {
            if (properties == null) 
                throw new NullReferenceException("Agent properties can't be null when searching closest cell");
            
            if (x % gridSize < 0.001f)
                x += 0.001f;
            if (z % gridSize < 0.001f)
                z += 0.001f;

            Graph graph;
            if (TryGetGraph(ToChunkPosition(x, z), properties, out graph)) {
                return graph.GetClosestCell(x, y, z, out cell, out outsideCell, out closestPoint);
            }
            else {
                cell = null;
                outsideCell = false;
                closestPoint = Vector3.zero;
                return false;
            }
        }
        /// <summary>
        /// try get closest Cell.
        /// return closest position to navmesh (inside chunk where position are)
        /// </summary> 
        public static bool TryGetClosestCell(Vector3 pos, AgentProperties properties, out Cell cell, out bool outsideCell, out Vector3 closestPoint) {
            return TryGetClosestCell(pos.x, pos.y, pos.z, properties, out cell, out outsideCell, out closestPoint);
        }
        public static bool TryGetClosestCell(Vector3 pos, AgentProperties properties, out Cell cell, out bool outsideCell) {
            Vector3 closestPoint;
            return TryGetClosestCell(pos, properties, out cell, out outsideCell, out closestPoint);
        }
        public static bool TryGetClosestCell(Vector3 pos, AgentProperties properties, out Cell cell, out Vector3 closestPoint) {
            bool outsideCell;
            return TryGetClosestCell(pos, properties, out cell, out outsideCell, out closestPoint);
        }
        public static bool TryGetClosestCell(Vector3 pos, AgentProperties properties, out Cell cell) {
            Vector3 closestPoint;
            bool outsideCell;
            return TryGetClosestCell(pos, properties, out cell, out outsideCell, out closestPoint);
        }
        public static bool TryGetClosestCell(PathFinderAgent agent, out Cell cell, out bool outsideCell, out Vector3 closestPoint) {
            return TryGetClosestCell(agent.positionVector3, agent.properties, out cell, out outsideCell, out closestPoint);
        }
        public static bool TryGetClosestCell(PathFinderAgent agent, out Cell cell, out bool outsideCell) {
            Vector3 closestPoint;
            return TryGetClosestCell(agent.positionVector3, agent.properties, out cell, out outsideCell, out closestPoint);
        }
        public static bool TryGetClosestCell(PathFinderAgent agent, out Cell cell, out Vector3 closestPoint) {  
            bool outsideCell;
            return TryGetClosestCell(agent.positionVector3, agent.properties, out cell, out outsideCell, out closestPoint);
        }
        public static bool TryGetClosestCell(PathFinderAgent agent, out Cell cell) {
            Vector3 closestPoint;
            bool outsideCell;
            return TryGetClosestCell(agent.positionVector3, agent.properties, out cell, out outsideCell, out closestPoint);
        }
        
        /// <summary>
        ///try get nearest hull.
        ///return closest no navmesh hull point (that inside chunk where position are)
        ///hull is where border of navmesh are
        /// </summary>
        public static bool TryGetNearestHull(Vector3 pos, AgentProperties properties, out Cell cell, out Vector3 closestPoint) {
            Graph graph;
            if (TryGetGraph(ToChunkPosition(pos), properties, out graph)) {
                return graph.GetClosestToHull(pos, out cell, out closestPoint);
            }
            else {
                cell = null;
                closestPoint = new Vector3();
                return false;
            }
        }
        public static bool TryGetNearestHull(Vector3 pos, AgentProperties properties, out Cell cell) {
            Vector3 closestPoint;
            return TryGetNearestHull(pos, properties, out cell, out closestPoint);
        }
        public static bool TryGetNearestHull(Vector3 pos, AgentProperties properties, out Vector3 closestPoint) {
            Cell cell;
            return TryGetNearestHull(pos, properties, out cell, out closestPoint);
        }
        public static bool TryGetNearestHull(PathFinderAgent agent, out Cell cell, out Vector3 closestPoint) {
            return TryGetNearestHull(agent.positionVector3, agent.properties, out cell, out closestPoint);
        }
        public static bool TryGetNearestHull(PathFinderAgent agent, out Cell cell) {
            Vector3 closestPoint;
            return TryGetNearestHull(agent, out cell, out closestPoint);
        }
        public static bool TryGetNearestHull(PathFinderAgent agent, out Vector3 closestPoint) {
            Cell cell;
            return TryGetNearestHull(agent, out cell, out closestPoint);
        }

        public static bool TryGetCell(float x, float y, float z, AgentProperties properties, out Cell cell, out Vector3 closestPoint) {
            //slight offset in case when request exactly on 
            //if (x % gridSize == 0) {
            //    x += 0.01f;
            //    Debug.Log("x % gridSize == 0");
            //}
            //if (z % gridSize == 0) {
            //    z += 0.01f;
            //    Debug.Log("z % gridSize == 0");
            //}

            Graph graph;
            if (TryGetGraph(ToChunkPosition(x, z), properties, out graph)) {
                //Debuger_K.AddLine(pos, graph.chunk.centerV3,Color.cyan);
                return graph.GetCell(x, y, z, out cell, out closestPoint);
            }
            else {
                cell = null;
                closestPoint = new Vector3();
                return false;
            }
        }

        public static bool TryGetCell(float x, float y, float z, AgentProperties properties, out Cell cell) {
            Vector3 closestPoint;
            return TryGetCell(x, y, z, properties, out cell, out closestPoint);
        }

        /// <summary>
        /// try get Cell. 
        /// return closest to point Cell if point inside cell from top projection
        /// </summary>
        public static bool TryGetCell(Vector3 pos, AgentProperties properties, out Cell cell, out Vector3 closestPoint) {
            return TryGetCell(pos.x, pos.y, pos.z, properties, out cell, out closestPoint);
        }
        public static bool TryGetCell(Vector3 pos, AgentProperties properties, out Cell cell) {
            Vector3 closestPoint;
            return TryGetCell(pos, properties, out cell, out closestPoint);
        }
        public static bool TryGetCell(PathFinderAgent agent, out Cell cell, out Vector3 closestPoint) {
            return TryGetCell(agent.positionVector3, agent.properties, out cell, out closestPoint);
        }
        public static bool TryGetCell(PathFinderAgent agent, out Cell cell) {
            Vector3 closestPoint;
            return TryGetCell(agent.positionVector3, agent.properties, out cell, out closestPoint);
        }



        //***************************QUEUE GRAPH***************************//
        //functions to order navmesh at some space
        #region QUEUE GRAPH
        private static void QueueNavMeshTemplateToPopulation(GeneralXZData data) {
            if (_acceptingWork == false)
                return;

            Init();

            NavMeshTemplateCreation template;

            //Debug.Log("Lock QueueNavMeshTemplateToPopulation");
            lock (_currentWorkDictionary) {
                if (_currentWorkDictionary.ContainsKey(data) || _chunkData.ContainsKey(data))
                    return;

                template = new NavMeshTemplateCreation(_chunkRange, CloneHashData(), data.gridPosition, data.properties);
                _currentWorkDictionary[data] = template;
            }

            templateQueueStage1.Enqueue(template);
        }
        private static void QueueNavMeshTemplateToPopulation(XZPosInt pos, AgentProperties properties) {
            QueueNavMeshTemplateToPopulation(new GeneralXZData(pos, properties));
        }
        public static void QueueGraph(Vector2 worldTopPosition, AgentProperties properties) {
            Init();
            QueueNavMeshTemplateToPopulation(new GeneralXZData(ToChunkPosition(worldTopPosition.x, worldTopPosition.y), properties));
        }
        public static void QueueGraph(Vector3 worldPosition, AgentProperties properties) {
            Init();
            QueueNavMeshTemplateToPopulation(new GeneralXZData(ToChunkPosition(worldPosition.x, worldPosition.z), properties));
        }

        public static void QueueGraph(int x, int z, AgentProperties properties, int sizeX = 1, int sizeZ = 1) {
            Init();
            if (sizeX <= 0 | sizeZ <= 0) {
                Debug.LogWarning("you trying to create navmesh with zero size. Which is not make any sence");
                return;
            }
            for (int _x = 0; _x < sizeX; _x++) {
                for (int _z = 0; _z < sizeZ; _z++) {
                    QueueNavMeshTemplateToPopulation(new GeneralXZData(x + _x, z + _z, properties));               
                }
            }
        }
        public static void QueueGraph(XZPosInt pos, AgentProperties properties) {
            QueueGraph(pos.x, pos.z, properties);
        }
        public static void QueueGraph(XZPosInt pos, VectorInt.Vector2Int size, AgentProperties properties) {
            QueueGraph(pos.x, pos.z, properties, size.x, size.y);
        }
        public static void QueueGraph(Bounds bounds, AgentProperties properties) {
            Init();
            XZPosInt min = ToChunkPosition(bounds.min);
            XZPosInt max = ToChunkPosition(bounds.max);
            QueueGraph(min.x, min.z, properties, max.x - min.x + 1, max.z - min.z + 1);
        }

        public static void QueueGraph(Vector2 startTop, Vector2 endTop, AgentProperties properties) {
            DDARasterization.DrawLineFixedMinusValues(startTop.x, startTop.y, endTop.x, endTop.y, gridSize, (int x, int y) => {
                QueueNavMeshTemplateToPopulation(new GeneralXZData(x, y, properties));
            });
        }

        public static void QueueGraph(Vector3 start, Vector3 end, AgentProperties properties) {
            DDARasterization.DrawLineFixedMinusValues(start.x, start.z, end.x, end.z, gridSize, (int x, int y) => {
                QueueNavMeshTemplateToPopulation(new GeneralXZData(x, y, properties));
            });
        }


        #endregion

        //***************************REMOVING GRAPHS***************************//
        //functions to order removing graphs at some space
        #region REMOVING GRAPH
        /// <summary>
        /// function to remove graph at some space
        /// IMPORTANT: if bool createNewGraphAfter == true then pathfinder are also add this graph to generation queue after it was removed
        /// </summary>
        /// <param name="data">position and properties</param>
        /// <param name="createNewGraphAfter">do add graph after removing?</param>
        public static void RemoveGraph(GeneralXZData data, bool createNewGraphAfter = true) {
            Init("RemoveGraph");

            if (_acceptingWork == false)
                return;

            destructQueue.Add(new NavMeshTemplateDestruction(data, createNewGraphAfter));
            Update();
        }

        /// <summary>
        /// function to remove graph at some space
        /// IMPORTANT: if bool createNewGraphAfter == true then pathfinder are also add this graph to generation queue after it was removed
        /// </summary>
        /// <param name="pos">position</param>
        /// <param name="properties">properties</param>
        /// <param name="createNewGraphAfter">do add graph after removing?</param>
        public static void RemoveGraph(XZPosInt pos, AgentProperties properties, bool createNewGraphAfter = true) {
            RemoveGraph(new GeneralXZData(pos, properties), createNewGraphAfter);
        }
        
        /// <summary>
        /// function to remove graph at some space with target size in chunks
        /// IMPORTANT: if bool createNewGraphAfter == true then pathfinder are also add this graph to generation queue after it was removed
        /// </summary>
        /// <param name="x">remove start X</param>
        /// <param name="z">remove start Z</param>
        /// <param name="properties">properties</param>
        /// <param name="sizeX">remove size X</param>
        /// <param name="sizeZ">remove size Z</param>
        /// <param name="createNewGraphAfter">do add graph after removing?</param>
        public static void RemoveGraph(int x, int z, AgentProperties properties, int sizeX = 1, int sizeZ = 1, bool createNewGraphAfter = true) {
            for (int _x = 0; _x < sizeX; _x++) {
                for (int _z = 0; _z < sizeZ; _z++) {
                    RemoveGraph(new XZPosInt(x + _x, z + _z), properties, createNewGraphAfter);
                }
            }
        }

        /// <summary>
        /// function to remove graph at space that include target bounds in world space
        /// IMPORTANT: if bool createNewGraphAfter == true then pathfinder are also add this graph to generation queue after it was removed
        /// </summary>
        /// <param name="bounds">target bounds in world space</param>
        /// <param name="properties">properties</param>
        /// <param name="createNewGraphAfter">do add graph after removing?</param>
        public static void RemoveGraph(Bounds bounds, AgentProperties properties, bool createNewGraphAfter = true) {
            float offset = properties.radius * properties.offsetMultiplier;
            Vector3 v3Offset = new Vector3(offset, 0, offset);
            XZPosInt min = ToChunkPosition(bounds.min - v3Offset);
            XZPosInt max = ToChunkPosition(bounds.max + v3Offset);
            VectorInt.Vector2Int size = new VectorInt.Vector2Int(Math.Max(1, max.x - min.x + 1), Math.Max(1, max.z - min.z + 1));
            RemoveGraph(min.x, min.z, properties, size.x, size.y, createNewGraphAfter);
        }

        /// <summary>
        /// function to remove graph at space that include multiple bounds in world space
        /// IMPORTANT: if bool createNewGraphAfter == true then pathfinder are also add this graph to generation queue after it was removed
        /// </summary>
        /// <param name="properties">properties</param>
        /// <param name="createNewGraphAfter">do add graph after removing?</param>
        /// <param name="bounds">target multiple bounds in world space</param>
        public static void RemoveGraph(AgentProperties properties, bool createNewGraphAfter = true, params Bounds[] bounds) {
            for (int i = 0; i < bounds.Length; i++) {
                RemoveGraph(bounds[i], properties, createNewGraphAfter);
            }
        }
        #endregion
        #endregion
        
        #region public values acessors
        public static bool areAcceptingWork {
            get { return _acceptingWork; }
        }
        public static bool areInit {
            get { return _areInit; }
        }
                
        public static PathFinderSettings settings {
            get {
                return _settings;

                //if (_settings == null)
                //    _settings = PathFinderSettings.LoadSettings();
                //return _settings;
            }
        }

        //public static float gridSize {
        //    get { return settings.gridSize; }
        //}

        public static int gridLowest {
            get { return settings.gridLowest; }
        }
        public static int gridHighest {
            get { return settings.gridHighest; }
        }

        public static bool multithread {
            get { return settings.useMultithread; }
        }

        public static TerrainCollectorType terrainCollectionType {
            get { return settings.terrainCollectionType; }
        }
        public static ColliderCollectorType colliderCollectorType {
            get { return settings.colliderCollectionType; }
        }
        #endregion 

        #region position convertation
        public static int ToGrid(float value) {
            return (int)Math.Floor(value / gridSize);
        }
        public static XZPosInt ToChunkPosition(float realX, float realZ) {
            return new XZPosInt(ToGrid(realX), ToGrid(realZ));
        }
        public static XZPosInt ToChunkPosition(Vector2 vector) {
            return ToChunkPosition(vector.x, vector.y);
        }
        public static XZPosInt ToChunkPosition(Vector3 vector) {
            return ToChunkPosition(vector.x, vector.z);
        }

        public static Bounds2DInt ToChunkPosition(float x1, float x2, float y1, float y2) {
            if (x2 < x1) {
                float temp = x1;
                x1 = x2;
                x2 = temp;
            }

            if (y2 < y1) {
                float temp = y1;
                y1 = y2;
                y2 = temp;
            }

            return ToChunkPositionPrivate(x1, x2, y1, y2);
        }


        public static Bounds2DInt ToChunkPosition(Bounds bounds) {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            return ToChunkPositionPrivate(min.x, max.x, min.z, max.z);
        }

        private static Bounds2DInt ToChunkPositionPrivate(float minX, float maxX, float minY, float maxY) {
            return new Bounds2DInt(ToGrid(minX), ToGrid(minY), ToGrid(maxX), ToGrid(maxY));
        }
        #endregion

        #region hash data
        public static void AddAreaHash(Area area, bool isGlobalArea) {
            //Debug.LogFormat("added {0}", area.name);
            lock (_hashData)
                _hashData.AddAreaHash(area, isGlobalArea);
        }

        public static void RemoveAreaHash(Area area) {
            lock (_hashData)
                _hashData.RemoveAreaHash(area);
        }

        //prefer cloning and use clone than this cause it lock
        public static int GetAreaHash(Area area, Passability passability) {
            lock (_hashData)
                return _hashData.GetAreaHash(area, passability);
        }
        public static void GetAreaByHash(short value, out Area area, out Passability passability) {
            lock (_hashData)
                _hashData.GetAreaByHash(value, out area, out passability);
        }

        public static AreaPassabilityHashData CloneHashData() {
            lock (_hashData)
                return _hashData.Clone();
        }
        #endregion

        #region serialization
#if UNITY_EDITOR
        //saving only for editor cause it saved in scriptable object


        public static void SaveCurrentSceneData() {
            Init("SaveCurrentSceneData");

            SceneNavmeshData data = sceneInstance.sceneNavmeshData;
            if (data == null) {
                string path = EditorUtility.SaveFilePanel("Save NavMesh", "Assets", SceneManager.GetActiveScene().name + ".asset", "asset");

                if (path == "")
                    return;

                path = FileUtil.GetProjectRelativePath(path);
                data = ScriptableObject.CreateInstance<SceneNavmeshData>();
                AssetDatabase.CreateAsset(data, path);
                AssetDatabase.SaveAssets();
                Undo.RecordObject(sceneInstance, "Set SceneNavmeshData to NavMesh scene instance");
                sceneInstance.sceneNavmeshData = data;
                EditorUtility.SetDirty(sceneInstance);
            }

            HashSet<AgentProperties> allProperties = new HashSet<AgentProperties>();
            foreach (var key in _chunkData.Keys) {
                allProperties.Add(key.properties);
            }      

            List<AgentProperties> properties = new List<AgentProperties>();
            List<SerializedNavmesh> navmesh = new List<SerializedNavmesh>();
            Dictionary<GameObject, int> gameObjectLibraryIDs = new Dictionary<GameObject, int>();

            foreach (var curProperties in allProperties) {
                properties.Add(curProperties);
                NavmeshLayserSerializer serializer = new NavmeshLayserSerializer(_chunkData, _chunkRange, gameObjectLibraryIDs, curProperties);
                SerializedNavmesh serializedNavmesh = serializer.Serialize();
                serializedNavmesh.pathFinderVersion = VERSION;
                navmesh.Add(serializedNavmesh);
            }

            GameObject[] goLibraryArray = new GameObject[gameObjectLibraryIDs.Count];

            foreach (var pair in gameObjectLibraryIDs) {
                goLibraryArray[pair.Value] = pair.Key;
            }

            data.properties = properties;
            data.navmesh = navmesh;
            EditorUtility.SetDirty(data);

            Undo.RecordObject(sceneInstance, "Save Serialized Data you probably should not undo this");
            sceneInstance.gameObjectLibrary = goLibraryArray;
        }

        public static void ClearCurrenSceneData() {
            Init("ClearCurrentData");
            SceneNavmeshData data = sceneInstance.sceneNavmeshData;
            sceneInstance.gameObjectLibrary = new GameObject[0];
            if (data == null) {
                Debug.LogWarning("data == null");
                return;
            }

            if (data.properties != null) {
                if (data.properties.Count > 0) {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Cleared:");
                    for (int i = 0; i < data.properties.Count; i++) {
                        sb.AppendFormat("properties: {0}, graphs: {1}, cells: {2}", data.properties[i].name, data.navmesh[i].serializedGraphs.Count, data.navmesh[i].cellCount);
                    }
                    Debug.Log(sb);
                }
                else
                    Debug.Log("nothing to clear");
            }

            if (data.properties != null)
                data.properties.Clear();

            if (data.navmesh != null)
                data.navmesh.Clear();

            EditorUtility.SetDirty(data);
        }
#endif

        public static void LoadCurrentSceneData() {
            Init("LoadCurrentSceneData");

            var sceneNavmeshData = sceneInstance.sceneNavmeshData;

            if (sceneNavmeshData == null) {
#if UNITY_EDITOR
                if (Debuger_K.doDebug)
                    Debug.LogWarning("No data to load");
#endif
                return;
            }

#if UNITY_EDITOR
            if (Debuger_K.doDebug)
                Debug.LogWarning("Load current data");
            Debuger_K.ClearChunksDebug();
#endif
                
            GameObject[] gameObjectLibrary = sceneInstance.gameObjectLibrary;

            foreach (var go in gameObjectLibrary) {
                AreaWorldMod awm = go.GetComponent<AreaWorldMod>();
                if (awm != null)
                    awm.Init();
            }

            lock (_chunkData) {
                List<AgentProperties> properties = sceneNavmeshData.properties;
                List<SerializedNavmesh> navmesh = sceneNavmeshData.navmesh;

                for (int i = 0; i < properties.Count; i++) {
                    AgentProperties curProperties = properties[i];

                    if (curProperties == null) {
                        Debug.LogWarning("deserialized properties no longer exist so Pathfinder skip it");
                        continue;
                    }

                    SerializedNavmesh curNavmesh = navmesh[i];

                    //removing old graph if it exist
                    List<GeneralXZData> removeList = new List<GeneralXZData>();
                    foreach (var graph in _chunkData.Values) {
                        if (graph.properties == curProperties)
                            removeList.Add(new GeneralXZData(graph.gridPosition, curProperties));
                    }
                    for (int removeIndex = 0; removeIndex < removeList.Count; removeIndex++) {
                        _chunkData.Remove(removeList[removeIndex]);
                    }

                    NavmeshLayerDeserializer deserializer = new NavmeshLayerDeserializer(curNavmesh, curProperties, gameObjectLibrary);
                    var deserializedStuff = deserializer.Deserialize();

                    //create chunk if needed and clamp size if it outside
                    foreach (var deserialized in deserializedStuff) {
                        XZPosInt pos = deserialized.chunkPosition;
                        YRangeInt curRange;
                        if (_chunkRange.TryGetValue(pos, out curRange)) {
                            _chunkRange[pos] = new YRangeInt(
                                Mathf.Min(curRange.min, deserialized.chunkMinY),
                                Mathf.Max(curRange.max, deserialized.chunkMaxY));
                        }
                        else
                            _chunkRange.Add(pos, new YRangeInt(deserialized.chunkMinY, deserialized.chunkMaxY));
                    }

                    List<Graph> graphs = new List<Graph>();
                    //put graphs inside chunks
                    foreach (var deserialized in deserializedStuff) {
                        //Chunk chunk = _chunkData[deserialized.chunkPosition];

                        XZPosInt pos = deserialized.chunkPosition;
                        YRangeInt ran = _chunkRange[pos];

                        Graph graph = deserialized.graph;
                        graph.SetChunkAndProperties(new ChunkData(pos, ran), curProperties);
                        _chunkData[new GeneralXZData(pos, curProperties)] = graph;
                        graph.OnFinishGraph();
                        graphs.Add(graph);
                    }

                    //connect chunks
                    foreach (var graph in graphs) {
                        for (int direction = 0; direction < 4; direction++) {
                            Graph neighbour;
                            if (TryGetGraphFrom(graph.gridPosition, (Directions)direction, curProperties, out neighbour)) {
                                graph.SetNeighbour((Directions)direction, neighbour);
                            }
                        }
#if UNITY_EDITOR
                        if (Debuger_K.doDebug)
                            graph.DebugGraph();
#endif
                    }
                }
            }            
        }


        #endregion

        #region things to help debug stuff
#if UNITY_EDITOR
        static Vector3 ToV3(Vector2 pos) {
            return new Vector3(pos.x, 0, pos.y);
        }
        static Vector2 ToV2(Vector3 pos) {
            return new Vector2(pos.x, pos.z);
        }

        public static void CellTester(Vector3 origin, AgentProperties properties) {
            //Graph graph;
            //if (TryGetGraph(ToChunkPosition(origin), properties, out graph) == false || graph.canBeUsed == false)
            //    return;

            //Cell cell;
            //bool outsideCell;
            //Vector3 closestPosToCell;

            //graph.GetClosestCell(origin, out cell, out outsideCell, out closestPosToCell);

            //foreach (var pair in cell.dataContentPairs) {
            //    Debuger_K.AddLine(pair.Key, Color.magenta);

            //    if (pair.Value == null)
            //        continue;

            //    Cell connection = pair.Value.connection;
            //    Debuger_K.AddLabel(pair.Key.centerV3, connection.Contains(pair.Key));
            //}
        }
#endif
        #endregion  
    }

    #endregion
}