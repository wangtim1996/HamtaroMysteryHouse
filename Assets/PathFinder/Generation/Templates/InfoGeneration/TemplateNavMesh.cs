using UnityEngine;
using System;
using System.Collections.Generic;

using K_PathFinder.GraphGeneration;
using K_PathFinder.Graphs;
using System.Linq;
using System.Threading;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder {
//    /// <summary>
//    /// this class provide information along the way cause it's suck to calculate all grid, sizes and other values bazillion times
//    /// other than scrip execution it's also provide numders so only the one place where it's calculated per iteration. much easier to change it or find where all got wrong
//    /// </summary>
//    public class NavMeshTemplate : PathFinderTemplate {
//        private Action<Graph> onEndCallBack;
//        private Dictionary<XZPosInt, YRangeInt> chunkRange;//careful with that

//        public ChunkData chunkData;

//        //things that collect collider info
//        private ColliderCollectorPrimitivesAbstract _primitivesCollector;
//        private ColliderCollectorTerrainAbstract _terrainCollector;

//        public float voxelSize { get; private set; } //most important value
//        public float maxSlope { get; private set; }
//        public float agentRadiusReal { private set; get; }

//        //agent values in fragment size
//        public int agentRagius { get; private set; }
//        public int agentHeight { get; private set; }
//        public int agentCrouchHeight { get; private set; }
//        public int maxStepHeight { get; private set; }

//        public int battleGridDensity { private set; get; } //distance between battle grid voxels 
//        public int coverExtraSamples { private set; get; } //take extra samples into cover

//        //bools
//        public bool doNavMesh { private set; get; }
//        public bool doBattleGrid { private set; get; }
//        public bool canCrouch { get; private set; }
//        public bool canJump { get; private set; }
//        public bool doCover { private set; get; }
//        public bool doHalfCover { private set; get; }

//        //resolution of grid that fits inside chunk
//        //why it's central explanet in next coment
//        public int startX_central{ get; private set; }
//        public int startZ_central { get; private set; }

//        public int endX_central { get; private set; }
//        public int endZ_central { get; private set; }

//        public int lengthX_central { get; private set; }
//        public int lengthZ_central { get; private set; }

//        //extra-values is resolution of voxel grid with applyed extraOffset
//        //and extraOffset is size of extra-samples on border (cause we care whats outside chunk)
//        public int extraOffset { get; private set; }

//        public int startX_extra { get; private set; }
//        public int startZ_extra { get; private set; }

//        public int endX_extra { get; private set; }
//        public int endZ_extra { get; private set; }

//        public int lengthX_extra { get; private set; }
//        public int lengthZ_extra { get; private set; }

//        //chunk position with applyed extra-offset
//        private float _offsetedChunkX, _offsetedChunkZ;
//        private Bounds _bounds; //of chunk
//        private List<string> _ignoredTags;

//        public AreaPassabilityHashData hashData;
//        public NavmeshProfiler profiler = null;
        

//        public NavMeshTemplate(Dictionary<XZPosInt, YRangeInt> chunkRange, XZPosInt gridPosition, AgentProperties properties) : base(gridPosition, properties) {
//            this.chunkRange = chunkRange;
//        }

//        public void SetCallBack(Action<Graph> onEndCallBack) {
//            this.onEndCallBack = onEndCallBack;
//        }

//        public void Populate() {

//#if UNITY_EDITOR
//            if (Debuger_K.useProfiler) {
//                profiler = new NavmeshProfiler(gridPosition.x, gridPosition.z);
//                profiler.StartProfile();
//            }
//#endif
//            //getting target space
//            YRangeInt chunkSize = GetChunkSizes();
//            chunkData = new ChunkData(gridPosition.x, gridPosition.z, chunkSize.min, chunkSize.max);
//            GetInitialValues();//getting all public values to support further work
//            PopulateCollectors();//getting colliders to make navmesh from them
//        }

//        private void GetInitialValues() {
//            _ignoredTags = properties.ignoredTags;

//            maxSlope = properties.maxSlope;
//            voxelSize = PathFinder.gridSize / properties.voxelsPerChunk;

//            agentRadiusReal = properties.radius;
//            agentRagius = Mathf.RoundToInt(agentRadiusReal / voxelSize);
//            agentHeight = Mathf.RoundToInt(properties.height / voxelSize);
//            maxStepHeight = Mathf.RoundToInt(properties.maxStepHeight / voxelSize);
//            doNavMesh = properties.doNavMesh;

//            canJump = properties.canJump;
//            canCrouch = properties.canCrouch;

//            if (canCrouch)
//                agentCrouchHeight = Mathf.RoundToInt(properties.crouchHeight / voxelSize);

//            extraOffset = Math.Max(1, Mathf.RoundToInt((properties.radius * properties.offsetMultiplier) / voxelSize));

//            float chunkX = chunkData.realX;
//            float chunkZ = chunkData.realZ;

//            startX_central = Mathf.RoundToInt(chunkX / voxelSize);
//            startZ_central = Mathf.RoundToInt(chunkZ / voxelSize);

//            endX_central = Mathf.RoundToInt((chunkX + PathFinder.gridSize) / voxelSize);
//            endZ_central = Mathf.RoundToInt((chunkZ + PathFinder.gridSize) / voxelSize);

//            lengthX_central = Mathf.Abs(startX_central - endX_central);
//            lengthZ_central = Mathf.Abs(startZ_central - endZ_central);

//            _offsetedChunkX = chunkX - (extraOffset * voxelSize);
//            _offsetedChunkZ = chunkZ - (extraOffset * voxelSize);

//            startX_extra = startX_central - extraOffset;
//            startZ_extra = startZ_central - extraOffset;

//            endX_extra = endX_central + extraOffset;
//            endZ_extra = endZ_central + extraOffset;

//            lengthX_extra = extraOffset + lengthX_central + extraOffset;
//            lengthZ_extra = extraOffset + lengthZ_central + extraOffset;

//            _bounds = new Bounds(chunkData.centerV3, chunkData.boundSize + new Vector3(extraOffset * voxelSize * 2, 0, extraOffset * voxelSize * 2));

//            doCover = properties.canCover;
//            doHalfCover = properties.canHalfCover;
//            coverExtraSamples = properties.coverExtraSamples;

//            doBattleGrid = properties.battleGrid;
//            if (doBattleGrid)
//                battleGridDensity = properties.battleGridDensity;
//        }

//        private YRangeInt GetChunkSizes() {
//            YRangeInt result;
//            //if this chunk data already exist then return it
//            lock (chunkRange) {
//                if (chunkRange.TryGetValue(gridPosition, out result))
//                    return result;
//            }
//            //if not then create it

//            //guessing height if not provided
//            float gridSize = PathFinder.gridSize;
//            float e = gridSize * 0.5f;
//            Vector3 v3e = new Vector3(e, e, e);

//            float currentX = gridPosition.x * gridSize + e;
//            float currentZ = gridPosition.z * gridSize + e;

//            float cyrMinY = PathFinder.gridLowest * gridSize;
//            float cyrMaxY = PathFinder.gridHighest * gridSize;
//            float castDistance = Math.Abs(cyrMaxY - cyrMinY);

//            Vector3 sMax = new Vector3(currentX, cyrMaxY, currentZ);

//            LayerMask mask = properties.includedLayers;
//            int minY, maxY;

//            RaycastHit[] hits = Physics.BoxCastAll(sMax, v3e, Vector3.down, Quaternion.identity, castDistance, mask);

//            if (hits.Length > 0) {
//                float highest = hits[0].point.y;
//                float lowest = hits[0].point.y;

//                if (hits.Length > 1) {
//                    for (int i = 1; i < hits.Length; i++) {
//                        if (hits[i].point.y > highest)
//                            highest = hits[i].point.y;

//                        if (hits[i].point.y < lowest)
//                            lowest = hits[i].point.y;
//                    }
//                }

//                minY = (int)(lowest / gridSize) - 1;
//                maxY = (int)(highest / gridSize);
//            }
//            else {
//                minY = 0;
//                maxY = 0;
//            }

//            result = new YRangeInt(minY, maxY);

//            lock (chunkRange)
//                chunkRange[gridPosition] = result;

//            return result;
//        }      

//        /// <summary>
//        /// collector are thing for collecting info about colliders. this function are executing in main threads and collect further information
//        /// if u wanna modify information or add maps there is best place to start   
//        /// </summary>
//        private void PopulateCollectors() {
//            Debug.LogError("no use here");
//            //if(profiler != null)
//            //    profiler.AddLog("collecting colliders");            

//            //HashSet<Collider> collidersHS = new HashSet<Collider>();

//            ////unity return ONE collider for terrain in editor but when playmode active it return lots! for evefy fucking tree! who invent this? why?!
//            //foreach (var collider in Physics.OverlapBox(chunkOffsetedBounds.center, chunkOffsetedBounds.extents, Quaternion.identity, properties.includedLayers, QueryTriggerInteraction.Ignore)) {
//            //       collidersHS.Add(collider);               
//            //}

//            //Collider[] colliders = collidersHS.ToArray();

//            //switch (PathFinder.terrainCollectionType) {
//            //    case TerrainCollectorType.UnityWay:
//            //        _terrainCollector = new ColliderCollectorTerrainUnityWay(this, colliders);
//            //        break;
//            //    case TerrainCollectorType.CPU:
//            //        _terrainCollector = new ColliderCollectorTerrainCPU(this, colliders);
//            //        break;
//            //    case TerrainCollectorType.ComputeShader:
//            //        _terrainCollector = new ColliderCollectorTerrainComputeShader(this, colliders);
//            //        (_terrainCollector as ColliderCollectorTerrainComputeShader).CollectUsingComputeShader();
//            //        break;
//            //}

//            //if (profiler != null) {
//            //    profiler.AddLog("collected terrain templates: " + _terrainCollector.collectedCount);
//            //    profiler.AddLog("start collecting primitive colliders");
//            //}

//            //switch (PathFinder.colliderCollectorType) {
//            //    case ColliderCollectorType.CPU:
//            //        _primitivesCollector = new ColliderCollectorPrimitivesCPU(this, colliders);
//            //        break;
//            //    case ColliderCollectorType.ComputeShader:
//            //        _primitivesCollector = new ColliderCollectorPrimitivesComputeShader(this, colliders);
//            //        (_primitivesCollector as ColliderCollectorPrimitivesComputeShader).CollectUsingComputeShader();
//            //        break; 
//            //}
         

//            //if (profiler != null) 
//            //    profiler.AddLog("collected primitives: " + _primitivesCollector.collectedCount);            
//        }

//        //all this awesome pipeline
//        //threaded
//        public override void Work() {
//            if (profiler != null) {
//                profiler.AddLog("start thread", Color.green);
//                profiler.StartThreadStuff();
//            }

//            if (stop) {
//                if (profiler != null) profiler.Abort();
//                return;
//            }

//            VolumeContainer volumes = new VolumeContainer(this);

//            if (profiler != null) profiler.AddLog("start collecting volumes", Color.green);
//            _terrainCollector.Collect(volumes);
//            if (profiler != null) profiler.AddLog("terrain collected");
//            _primitivesCollector.Collect(volumes);            
//            if (profiler != null) profiler.AddLog("collected");


//            hashData = PathFinder.CloneHashData();

//            if (stop) {
//                if (profiler != null) profiler.Abort();
//                return;
//            }

//            if (profiler != null) profiler.AddLog("volumes container start doing stuff");
//            volumes.DoStuff();
//            if (profiler != null) profiler.AddLog("volumes container end doing stuff");

//            if (stop) {
//                if (profiler != null) profiler.Abort();
//                return;
//            }

//            if (profiler != null) profiler.AddLog("graph generator start doing stuff", Color.green);
//            GraphGenerator generator = new GraphGenerator(volumes, this);
//            Graph graph = generator.MakeGraph();
//            if (profiler != null) profiler.AddLog("graph generator end making graph");

//            if (stop) {
//                if (profiler != null) profiler.Abort();
//                return;
//            }

//            if (profiler != null) {
//                profiler.AddLog("end thread", Color.green);
//                profiler.EndThreadStuff();
//                profiler.EndProfile();
//                profiler.DebugLog(ProfilderLogMode.log);
//            }

//            GC.Collect();


//            //Graph graph = new Graph(chunk, properties);
//            if (onEndCallBack != null)
//                onEndCallBack.Invoke(graph);
//        }

//        public static void ThreadWorker(object obj) {
//            NavMeshTemplate template = (NavMeshTemplate)obj;
//            if (template == null)
//                Debug.LogError("invalid argument while using thread worker");

//            try {
//                template.Work();
//            }
//            catch (Exception e) {
//                if (template.profiler != null)
//                    template.profiler.DebugLog(ProfilderLogMode.warning);
//                Debug.LogError(e);
//                throw;
//            }
//        }

//        public bool IgnoredTagsContains(string tag) {
//            return _ignoredTags.Contains(tag);
//        }

//        public Bounds chunkOffsetedBounds {
//            get { return _bounds; }
//        }

//        //actualy cant remember why it's here
//        public Vector3 halfFragmentOffset {
//            get { return new Vector3(voxelSize * 0.5f, 0, voxelSize * 0.5f); }
//        }
//        public Vector3 realOffsetedPosition {
//            get {return new Vector3(_offsetedChunkX, 0, _offsetedChunkZ); }
//        }
//    }
}
