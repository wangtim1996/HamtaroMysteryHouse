using K_PathFinder.Collector;
using K_PathFinder.GraphGeneration;
using K_PathFinder.Graphs;
using K_PathFinder.PFDebuger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace K_PathFinder {
    public class NavMeshTemplateCreation {
        public XZPosInt gridPosition;
        public AgentProperties properties { get; private set; }
        public bool stop { get; private set; }

        //callbacks
        public Action<NavMeshTemplateCreation> callbackAfterGraphGeneration { private get; set; }
        public WaitCallback graphGenerationCallback { get; set; }

        private Dictionary<XZPosInt, YRangeInt> chunkRange;//careful with that

        public ChunkData chunkData;

        //things that collect collider info
        //private ColliderCollectorPrimitivesAbstract _primitivesCollector;
        //private ColliderCollectorTerrainAbstract _terrainCollector;



        ////*******EXPERIMENTAL*******//
        //ShapeCollector shapeCollectorCPU;
        ////*******EXPERIMENTAL*******//

        public float voxelSize { get; private set; } //most important value
        public float maxSlope { get; private set; }
        public float agentRadiusReal { private set; get; }

        //agent values in fragment size
        public int agentRagius { get; private set; }
        public int agentHeight { get; private set; }
        public int agentCrouchHeight { get; private set; }
        public int maxStepHeight { get; private set; }

        public int battleGridDensity { private set; get; } //distance between battle grid voxels 
        public int coverExtraSamples { private set; get; } //take extra samples into cover

        //bools
        public bool doNavMesh { private set; get; }
        public bool doBattleGrid { private set; get; }
        public bool canCrouch { get; private set; }
        public bool canJump { get; private set; }
        public bool doCover { private set; get; }
        public bool doHalfCover { private set; get; }

        //resolution of grid that fits inside chunk
        //why it's central explanet in next coment
        public int startX_central { get; private set; }
        public int startZ_central { get; private set; }

        public int endX_central { get; private set; }
        public int endZ_central { get; private set; }

        public int lengthX_central { get; private set; }
        public int lengthZ_central { get; private set; }

        //extra-values is resolution of voxel grid with applyed extraOffset
        //and extraOffset is size of extra-samples on border (cause we care whats outside chunk)
        public int extraOffset { get; private set; }

        public int startX_extra { get; private set; }
        public int startZ_extra { get; private set; }

        public int endX_extra { get; private set; }
        public int endZ_extra { get; private set; }

        public int lengthX_extra { get; private set; }
        public int lengthZ_extra { get; private set; }

        //chunk position with applyed extra-offset
        private float _offsetedChunkX, _offsetedChunkZ;
        private Bounds _bounds; //of chunk
        public List<string> ignoredTags;
        public bool checkHierarchyTag;
        public LayerMask includedLayers;

        public AreaPassabilityHashData hashData;
        public NavmeshProfiler profiler = null;

        public ColliderCollector colliderCollector;

        //RESULT
        public Graph graph;

        public NavMeshTemplateCreation(Dictionary<XZPosInt, YRangeInt> chunkRange, AreaPassabilityHashData hashData, XZPosInt gridPosition, AgentProperties properties) {
            this.gridPosition = gridPosition;
            this.hashData = hashData;
            this.properties = properties;
            this.chunkRange = chunkRange;
        }

        public bool Match(XZPosInt gridPosition, AgentProperties properties) {
            return this.gridPosition == gridPosition && this.properties == properties;
        }

        public void Stop() {
            stop = true;
        }


        #region POPULATION STAGE
        //all things here are used in UNITY main thread so things here can use unity API

        /// <summary>
        /// Function to populate template with further information. Here goes information about colliders and also usage of compute shaders if it enabled
        /// </summary>
        public void PopulateTemplate() {
#if UNITY_EDITOR
            if (Debuger_K.useProfiler) {
                profiler = new NavmeshProfiler(gridPosition.x, gridPosition.z);
                profiler.StartProfile();
            }
#endif

            //getting target space
            YRangeInt chunkSize = GetChunkSizes();//get chunk height size
            chunkData = new ChunkData(gridPosition.x, gridPosition.z, chunkSize.min, chunkSize.max);//create chunk data so further code are a bit more readable
            GetInitialValues();//getting all public values listed on top to support further work
            PopulateCollectors();//getting colliders to make navmesh from them
        }

        /// <summary>
        ///check if chunk size already defined. if true then return it. if not then it uses unity API to get it
        /// </summary>
        /// <returns>chunk Y range divided by grid size</returns>
        private YRangeInt GetChunkSizes() {
            YRangeInt result;
            //if this chunk data already exist then return it
            lock (chunkRange) {
                if (chunkRange.TryGetValue(gridPosition, out result))
                    return result;
            }
            //if not then create it

            //guessing height if not provided
            float gridSize = PathFinder.gridSize;
            float e = gridSize * 0.5f;
            Vector3 v3e = new Vector3(e, e, e);

            float currentX = gridPosition.x * gridSize + e;
            float currentZ = gridPosition.z * gridSize + e;

            float cyrMinY = PathFinder.gridLowest * gridSize;
            float cyrMaxY = PathFinder.gridHighest * gridSize;
            float castDistance = Math.Abs(cyrMaxY - cyrMinY);

            Vector3 sMax = new Vector3(currentX, cyrMaxY, currentZ);

            LayerMask mask = properties.includedLayers;
            int minY, maxY;

            RaycastHit[] hits = Physics.BoxCastAll(sMax, v3e, Vector3.down, Quaternion.identity, castDistance, mask);

            if (hits.Length > 0) {
                float highest = hits[0].point.y;
                float lowest = hits[0].point.y;

                if (hits.Length > 1) {
                    for (int i = 1; i < hits.Length; i++) {
                        if (hits[i].point.y > highest)
                            highest = hits[i].point.y;

                        if (hits[i].point.y < lowest)
                            lowest = hits[i].point.y;
                    }
                }

                minY = (int)(lowest / gridSize) - 1;
                maxY = (int)(highest / gridSize);
            }
            else {
                minY = 0;
                maxY = 0;
            }

            result = new YRangeInt(minY, maxY);

            lock (chunkRange)
                chunkRange[gridPosition] = result;

            return result;
        }
  
        /// <summary>
        /// set bunch of public values listed on top
        /// </summary>
        private void GetInitialValues() {
            ignoredTags = properties.ignoredTags;
            checkHierarchyTag = properties.checkHierarchyTag;
            includedLayers = properties.includedLayers;

            maxSlope = properties.maxSlope;
            voxelSize = PathFinder.gridSize / properties.voxelsPerChunk;

            agentRadiusReal = properties.radius;
            agentRagius = Mathf.RoundToInt(agentRadiusReal / voxelSize);
            agentHeight = Mathf.RoundToInt(properties.height / voxelSize);
            maxStepHeight = Mathf.RoundToInt(properties.maxStepHeight / voxelSize);
            doNavMesh = properties.doNavMesh;

            canJump = properties.canJump;
            canCrouch = properties.canCrouch;

            if (canCrouch)
                agentCrouchHeight = Mathf.RoundToInt(properties.crouchHeight / voxelSize);

            extraOffset = Math.Max(1, Mathf.RoundToInt((properties.radius * properties.offsetMultiplier) / voxelSize));

            float chunkX = chunkData.realX;
            float chunkZ = chunkData.realZ;

            startX_central = Mathf.RoundToInt(chunkX / voxelSize);
            startZ_central = Mathf.RoundToInt(chunkZ / voxelSize);

            endX_central = Mathf.RoundToInt((chunkX + PathFinder.gridSize) / voxelSize);
            endZ_central = Mathf.RoundToInt((chunkZ + PathFinder.gridSize) / voxelSize);

            lengthX_central = Mathf.Abs(startX_central - endX_central);
            lengthZ_central = Mathf.Abs(startZ_central - endZ_central);

            _offsetedChunkX = chunkX - (extraOffset * voxelSize);
            _offsetedChunkZ = chunkZ - (extraOffset * voxelSize);

            startX_extra = startX_central - extraOffset;
            startZ_extra = startZ_central - extraOffset;

            endX_extra = endX_central + extraOffset;
            endZ_extra = endZ_central + extraOffset;

            lengthX_extra = extraOffset + lengthX_central + extraOffset;
            lengthZ_extra = extraOffset + lengthZ_central + extraOffset;

            _bounds = new Bounds(chunkData.centerV3, chunkData.boundSize + new Vector3(extraOffset * voxelSize * 2, 0, extraOffset * voxelSize * 2));

            doCover = properties.canCover;
            doHalfCover = properties.canHalfCover;
            coverExtraSamples = properties.coverExtraSamples;

            doBattleGrid = properties.battleGrid;
            if (doBattleGrid)
                battleGridDensity = properties.battleGridDensity;
        }

        /// <summary>
        /// collector are thing for collecting info about colliders
        /// if you want to modify information or add maps there is best place to start   
        /// </summary>
        private void PopulateCollectors() {
            if (profiler != null)
                profiler.AddLog("collecting colliders");

            HashSet<Collider> collidersHS = new HashSet<Collider>();

            //unity return ONE collider for terrain in editor but when playmode active it return lots! for evefy fucking tree! who invent this? why?!
            foreach (var collider in Physics.OverlapBox(chunkOffsetedBounds.center, chunkOffsetedBounds.extents, Quaternion.identity, properties.includedLayers, QueryTriggerInteraction.Ignore)) {
                collidersHS.Add(collider);//for this reason everything are puttet into hashset to exclude dublicates
            }

            Collider[] colliders = collidersHS.ToArray();

            colliderCollector = new ColliderCollector(this);
            colliderCollector.profiler = profiler;
            colliderCollector.AddCollider(colliders);

            List<AreaWorldMod> mods = new List<AreaWorldMod>();
            ChunkContentMap.GetContent(chunkData.x, chunkData.z, mods); //this uses some grid based thing. you give it list to fill and where
            colliderCollector.AddModifyers(mods);
            hashData.AssignData();

        }
        #endregion

        #region GRAPH GENERATION STAGE
        //this stage executed in whatever thread. nothing here are really managed

        //it is all awesome pipeline that generate navmesh
        public static void ThreadWorker(object obj) {
            NavMeshTemplateCreation template = (NavMeshTemplateCreation)obj;
            if (template == null)
                Debug.LogError("invalid argument while using thread worker");

            try {
                template.GenerateGraph();
            }
            catch (Exception e) {
                if (template.profiler != null)
                    template.profiler.DebugLog(ProfilderLogMode.warning);
                Debug.LogError(e);
                throw;
            }
        }

                
        public void GenerateGraph() {    
            if (profiler != null) {
                profiler.AddLog("start thread", Color.green);
                profiler.StartThreadStuff();
            }

            if (stop) {
                if (profiler != null) profiler.Abort();
                return;
            }

            //VolumeContainer volumes = new VolumeContainer(this);

            ////collecting terrain
            //if (profiler != null) profiler.AddLog("start collecting volumes", Color.green);
            //if (_terrainCollector.collectedCount > 0) {
            //    if (profiler != null) profiler.AddLogFormat("collecting terrains. Count: {0}", Color.green, _terrainCollector.collectedCount);
            //    _terrainCollector.Collect(volumes);
            //    if (profiler != null) profiler.AddLog("terrain collected");
            //}
            //else {
            //    if (profiler != null) profiler.AddLog("No terrain to collect", Color.green);
            //}

            //if (stop) {
            //    if (profiler != null) profiler.Abort();
            //    return;
            //}



            //switch (PathFinder.colliderCollectorType) {
            //    case ColliderCollectorType.CPU:
            //        shapeCollectorCPU.Collect(volumes);
            //        break;
            //    case ColliderCollectorType.ComputeShader:
            //        //collecting primitives
            //        if (_primitivesCollector.collectedCount > 0) {
            //            if (profiler != null) profiler.AddLogFormat("collecting primitives. Count: {0}", Color.green, _primitivesCollector.collectedCount);
            //            _primitivesCollector.profiler = profiler;
            //            _primitivesCollector.Collect(volumes);
            //            if (profiler != null) profiler.AddLog("primitives collected");
            //        }
            //        else {
            //            if (profiler != null) profiler.AddLog("No primitives to collect", Color.green);
            //        }
            //        break;
            //}


            colliderCollector.Collect(); //also apply modifyers

            VolumeContainerNew volume = new VolumeContainerNew(this);
            volume.AddGenericColliders(colliderCollector.shapeCollectorResult);
            volume.DoStuff();

            GraphGeneratorNew generator = new GraphGeneratorNew(volume, this);
            graph = generator.MakeGraph();
            //GraphGenerator generator = new GraphGenerator(volumes, this);
            //graph = generator.MakeGraph();

            //if (stop) {
            //    if (profiler != null) profiler.Abort();
            //    return;
            //}

            ////process collected volumes. remove intersections, reduce volume count, generate connections, flags, etc
            ////best place to modify information about world or add flags
            //if (profiler != null) profiler.AddLog("volumes container start doing stuff");
            //volumes.DoStuff();
            //if (profiler != null) profiler.AddLog("volumes container end doing stuff");

            //if (stop) {
            //    if (profiler != null) profiler.Abort();
            //    return;
            //}

            //if (profiler != null) profiler.AddLog("graph generator start doing stuff", Color.green);
            //GraphGenerator generator = new GraphGenerator(volumes, this);
            //graph = generator.MakeGraph();
            //if (profiler != null) profiler.AddLog("graph generator end making graph");

            //if (stop) {
            //    if (profiler != null) profiler.Abort();
            //    return;
            //}

            //if (profiler != null) profiler.AddLog("Return all remaining volumes to object pool");
            //volumes.ReturnAllVolumesToObjectPool();

            //graph = new Graph();

            if (profiler != null) {
                profiler.AddLog("end thread", Color.green);
                profiler.EndThreadStuff();
                profiler.EndProfile();
                profiler.DebugLog(ProfilderLogMode.log);
            }

            //Graph graph = new Graph(chunk, properties);
            if (callbackAfterGraphGeneration != null)
                callbackAfterGraphGeneration.Invoke(this);      
        }
        #endregion
               

        public bool IgnoredTagsContains(string tag) {
            return ignoredTags.Contains(tag);
        }

        #region acessors
        public Bounds chunkOffsetedBounds {
            get { return _bounds; }
        }
        public int gridPosX {
            get { return gridPosition.x; }
        }
        public int gridPosZ {
            get { return gridPosition.z; }
        }
        //actualy cant remember why it's here
        public Vector3 halfVoxelOffset {
            get { return new Vector3(voxelSize * 0.5f, 0, voxelSize * 0.5f); }
        }
        public Vector3 realOffsetedPosition {
            get { return new Vector3(_offsetedChunkX, 0, _offsetedChunkZ); }
        }
        public float realOffsetedPositionX {
            get { return _offsetedChunkX; }
        }
        public float realOffsetedPositionZ {
            get { return _offsetedChunkZ; }
        }
        #endregion

        public override bool Equals(object obj) {
            NavMeshTemplateCreation otherTemplate = (NavMeshTemplateCreation)obj;

            if (otherTemplate == null)
                return false;

            return Match(otherTemplate.gridPosition, otherTemplate.properties);
        }
        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }
}