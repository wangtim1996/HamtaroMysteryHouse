using System;
using System.Collections.Generic;
using UnityEngine;
using K_PathFinder.GraphGeneration;
using System.Collections;
using System.Linq;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

//namespace K_PathFinder {
//    //FIX: CaptureArea create not necesary objects
//    //TODO: if it's just one terrain potentialy you can cut GenerateConnections() and make it faster by aprox 0.1 time

//    public class VolumeContainer {
//        //extra length values cause there is small overhead to call template.length_extra this much times. besides look nicer
//        //public for debug purpose only
//        public readonly int sizeX, sizeZ, flattenedSize;
//        NavMeshTemplateCreation template;
//        NavmeshProfiler profiler;

//        //public List<Volume> volumes = new List<Volume>();
//        public Volume[] volumes = new Volume[0];
//        public List<VolumeArea> volumeAreas = new List<VolumeArea>();

//        float connectionDistance;
//        bool doCrouch, doCover, doHalfCover;
//        float halfCover, fullCover;   

//        bool SINGLE_TERRAIN_CASE = false; //if thi flag is true then some steps are skipped. its very important

//        //HashSet<VolumePos> nearOsbtacleSet = new HashSet<VolumePos>();
//        //HashSet<VolumePos> nearCrouchSet = new HashSet<VolumePos>();

//        List<VolumePos> nearObstacle = new List<VolumePos>();
//        List<VolumePos> nearCrouch = new List<VolumePos>();

//        public BattleGrid battleGrid = null; //it's creates there and transfered further to graph. there is just no graph at this moment so it's end up here

//        public List<Volume> volumeTerrain = new List<Volume>();
//        public List<VolumeSimple> volumeTerrainTrees = new List<VolumeSimple>();
//        public List<VolumeSimple> volumesSolid = new List<VolumeSimple>();
//        public List<VolumeSimple> volumesModifyArea = new List<VolumeSimple>();
//        public List<VolumeSimple> volumesMakeHole = new List<VolumeSimple>();


//        //here is just generated circular patterns
//        //dont have much use but potentialy can
//        static Dictionary<int, CirclePattern> patterns = new Dictionary<int, CirclePattern>();

//        public VolumeContainer(NavMeshTemplateCreation template) {
//            this.template = template;
//            this.profiler = template.profiler;
//            connectionDistance = template.properties.maxStepHeight;
//            doCrouch = template.canCrouch;
//            doCover = template.doCover;

//            if (doCover) {
//                fullCover = template.properties.fullCover;
//                doHalfCover = template.doHalfCover;
//                if (doHalfCover)
//                    halfCover = template.properties.halfCover;
//            }

//            sizeX = template.lengthX_extra;
//            sizeZ = template.lengthZ_extra;
//            flattenedSize = sizeX * sizeZ;
//        }

//        public int GetIndex(int x, int z) {
//            return (z * sizeX) + x;
//        }
        
//        public void ReturnAllVolumesToObjectPool() {
//            for (int i = 0; i < volumes.Length; i++) {
//                volumes[i].ReturnToPool();
//            }
//            volumes = null;
//        }

//        public void AddTerrainVolumes(Volume terrain, List<VolumeSimple> trees) {
//            volumeTerrain.Add(terrain);

//            //compacting trees
//            if (trees.Count > 2) {
//                if (profiler != null) profiler.AddLogFormat("Start compressing trees. Current: {0}", trees.Count);

//                List<VolumeSimple> compactTrees = new List<VolumeSimple>();

//                while (trees.Count > 0) {
//                    VolumeSimple current = trees[0];
//                    trees.RemoveAt(0);

//                    VolumeDataPoint[] curData = current.data;


//                    for (int treeIndex = trees.Count - 1; treeIndex >= 0; treeIndex--) {
//                        VolumeSimple target = trees[treeIndex];
//                        VolumeDataPoint[] targetData = target.data;              
//                        bool interupted = false;

//                        //here acessed same index so no need to acess by x and z
//                        for (int i = 0; i < flattenedSize; i++) {
//                            if (targetData[i].exist) {
//                                if (curData[i].exist)
//                                    interupted = true;
//                                else {
//                                    curData[i] = targetData[i];     
//                                    targetData[i].exist = false;                                  
//                                }
//                            }
//                        }

//                        if (interupted == false) {
//                            trees.RemoveAt(treeIndex);
//                            target.ReturnToPool();
//                        }
//                    }

//                    compactTrees.Add(current);
//                }
//                trees = compactTrees;
//            }
//            if (profiler != null) profiler.AddLogFormat("End compressing trees. Current: {0}", trees.Count);

//            volumeTerrainTrees.AddRange(trees);
//        }

//        public void AddSimpleVolume(VolumeSimple volume) {
//            if (volume.infoMode == ColliderInfoMode.Solid) {
//                volumesSolid.Add(volume);
//            }
//            else if (volume.infoMode == ColliderInfoMode.MakeHoleApplyArea |
//                     volume.infoMode == ColliderInfoMode.MakeHoleRetainArea) {
//                volumesMakeHole.Add(volume);
//            }
//            else if (volume.infoMode == ColliderInfoMode.ModifyArea) {
//                volumesModifyArea.Add(volume);
//            }
//        }

//        protected static int Comparer(VolumeSimple left, VolumeSimple right) {
//            //if (left.priority < right.priority)
//            //    return -1;
//            //if (left.priority > right.priority)
//            //    return 1;

//            if (left.area.overridePriority < right.area.overridePriority)
//                return -1;
//            if (left.area.overridePriority > right.area.overridePriority)
//                return 1;

//            return 0;
//        }

//        private void ApplyAreaModifyer(Volume targetVolume, VolumeSimple modifyer) {
//            if (targetVolume == null) {
//                Debug.LogError("targetVolume == null");
//                return;
//            }

//            if (modifyer == null) {
//                Debug.LogError("modifyer == null");
//                return;
//            }

//            int sizeX = targetVolume.sizeX;
//            int sizeZ = targetVolume.sizeZ;

//            if (sizeX != modifyer.sizeX || sizeZ != modifyer.sizeZ) {
//                Debug.LogError("volume sizes dont match");
//                return;
//            }

//            //bool[] existance1 = targetVolume.existance;
//            //float[] max1 = targetVolume.max;
//            VolumeDataPoint[] dataTarget = targetVolume.data;
//            VolumeDataPoint[] dataMod = modifyer.data;



//            //bool[] existance2 = modifyer.existance;
//            //float[] max2 = modifyer.max;
//            //float[] min2 = modifyer.min;
//            Area modArea = modifyer.area;

//            if (modArea.id == 1) {//non walkable area
//                for (int index = 0; index < flattenedSize; index++) {
//                    if (dataTarget[index].exist && dataMod[index].exist) {
//                        if (dataMod[index].BetweenMinMax(dataTarget[index].max)) {
//                            dataTarget[index].passability = (sbyte)Passability.Unwalkable;
//                        }
//                    }
//                }
//            }
//            else {
//                Area[] area1 = targetVolume.area;
//                bool applyed = false;

//                for (int index = 0; index < flattenedSize; index++) {
//                    if (dataTarget[index].exist && dataMod[index].exist) {
//                        if (dataMod[index].BetweenMinMax(dataTarget[index].max)) {
//                            area1[index] = modArea;
//                            applyed = true;
//                        }
//                    }
//                }
//                if (applyed)
//                    targetVolume.containsAreas.Add(modArea);
//            }
//        }

//        //return true if new volume was generated
//        //if applyModifyerArea is true then modifyer area will be sellected to overriden pixels
//        //else original area will be sellected
//        private bool ApplyHoleMaker(Volume targetVolume, VolumeSimple modifyer, out Volume newVolume) {
//            newVolume = null;

//            if (targetVolume == null) {
//                Debug.LogError("targetVolume == null");
//                return false;
//            }

//            if (modifyer == null) {
//                Debug.LogError("modifyer == null");
//                return false;
//            }

//            bool applyModifyerArea = modifyer.infoMode == ColliderInfoMode.MakeHoleApplyArea;

//            int sizeX = targetVolume.sizeX;
//            int sizeZ = targetVolume.sizeZ;

//            if (sizeX != modifyer.sizeX || sizeZ != modifyer.sizeZ) {
//                Debug.LogError("volume sizes dont match");
//                return false;
//            }
//            VolumeDataPoint[] dataNewVolume = null;
//            Area[] newVolumeArea = null;
//            VolumeDataPoint[] dataTarget = targetVolume.data;
//            Area[] targetArea = targetVolume.area;
         

//            //bool[] modExistance = modifyer.existance;
//            //float[] modMin = modifyer.min;
//            //float[] modMax = modifyer.max;
//            //int[] modPass = modifyer.passability;

//            VolumeDataPoint[] dataMod = modifyer.data;

//            Area modArea = modifyer.area;            
//            bool areaApplyed = false;

//            for (int index = 0; index < flattenedSize; index++) {
//                VolumeDataPoint modData = dataMod[index];
//                VolumeDataPoint targetData = dataTarget[index];

//                if (targetData.exist && modData.exist) {
//                    float tMax = targetData.max;
//                    float tMin = targetData.min;
//                    float mMax = modData.max;
//                    float mMin = modData.min;

//                    if (mMin <= tMin) {
//                        if (mMax > tMin) {
//                            if (mMax < tMax)
//                                dataTarget[index].min = mMax;
//                            else
//                                dataTarget[index].exist = false;
//                        }
//                    }
//                    else {
//                        if (mMin <= tMax) {
//                            if (mMax < tMax) {
//                                if (newVolume == null) {
//                                    newVolume = Volume.GetFromPool(targetVolume, modArea);
//                                    dataNewVolume = newVolume.data;
//                                    newVolumeArea = newVolume.area;
//                                }

//                                dataNewVolume[index] = new VolumeDataPoint(tMax, mMax, dataTarget[index].passability);
//                                newVolumeArea[index] = targetArea[index];
//                            }

//                            dataTarget[index].min = mMin;         

//                            if (applyModifyerArea) {
//                                if (modArea.id == 1) {
//                                    dataTarget[index].passability = (sbyte)Passability.Unwalkable;
//                                }
//                                else {
//                                    areaApplyed = true;
//                                    targetArea[index] = modArea;
//                                    dataTarget[index].passability = modData.passability;
//                                }
//                            }
//                            else {
//                                dataTarget[index].passability = modData.passability;
//                            }
//                        }
//                    }
//                }
//            }

       

//            if (areaApplyed) {
//                targetVolume.containsAreas.Add(modArea);
//            }

//            return newVolume != null;
//        }

//        /// <summary>
//        /// function to apply volume to existed volumes
//        /// </summary>
//        /// <param name="volume">volume</param>
//        /// <param name="addVolume">
//        /// special case. sometimes volume need to be readded to apply it cause if added volumes under existed one then it change shape of already existed volume. 
//        /// so there is way sround - just reapply changed volume </param>
//        public void ApplyVolume(Volume volume, bool addVolume) {
//            if (volume == null)
//                return;

//            if (sizeX != volume.sizeX || sizeZ != volume.sizeZ) {
//                Debug.LogError("volume sizes dont match");
//                return;
//            } 

//            //apply to volume all existed volumes
//            //bool[] existance1 = volume.existance;
//            //float[] max1 = volume.max;
//            //float[] min1 = volume.min;
//            //int[] passability1 = volume.passability;

//            VolumeDataPoint[] data1 = volume.data;
//            Area[] area1 = volume.area;    

//            HashSet<Volume> reAddMe = new HashSet<Volume>();

//            foreach (var applyedVolume in volumes) {
//                if (applyedVolume == volume)
//                    continue;

//                VolumeDataPoint[] data2 = applyedVolume.data;
//                Area[] area2 = applyedVolume.area;
     

//                bool doCrouch = template.canCrouch;
//                float walkableHeight = template.properties.height;
//                float crouchHeight = template.properties.crouchHeight;


//                Volume vHigh_v, vLow_v;
//                VolumeDataPoint[] vHigh, vLow;

//                //this iteration uses same index so no need to do this by x and z

//                for (int index = 0; index < flattenedSize; index++) {
//                    VolumeDataPoint vdp1 = data1[index];
//                    VolumeDataPoint vdp2 = data2[index];

//                    if (vdp1.exist && vdp2.exist) {
//                        float max1_v = vdp1.max;
//                        float max2_v = vdp2.max;

//                        if (max1_v == max2_v) {//here we fix z-fighting. bigger is better if height overlaping                                 
//                            if (vdp1.min > vdp2.min) {
//                                data2[index].UpdateMinPassability(vdp1.min, vdp1.passability);
//                                data1[index].exist = false;
//                                if (area1[index].overridePriority > area2[index].overridePriority)
//                                    area2[index] = area1[index];                          
//                            }
//                            else {
//                                data1[index].UpdateMinPassability(vdp2.min, vdp2.passability);
//                                data2[index].exist = false;
//                                if (area2[index].overridePriority > area1[index].overridePriority)
//                                    area1[index] = area2[index];                            
//                            }
//                        }
//                        else {
//                            if (max1_v > max2_v) {
//                                vHigh_v = volume;
//                                vLow_v = applyedVolume;
//                            }
//                            else {
//                                vHigh_v = applyedVolume;
//                                vLow_v = volume;
//                            }

//                            vHigh = vHigh_v.data;
//                            vLow = vLow_v.data;

//                            if (vLow[index].max < vHigh[index].min) {
//                                float heightDif = Math.Abs(vHigh[index].min - vLow[index].max);

//                                //!!!!!!!!!!!!!!!!!!!!
//                                if (heightDif < connectionDistance) {// then we remove it cause it cause lots of trouble
//                                    vLow[index].exist = false;
//                                    vHigh[index].min = vLow[index].min;
//                                    if (vHigh_v != volume)
//                                        reAddMe.Add(vHigh_v);
//                                }
//                                else {
//                                    if (doCrouch) {
//                                        if (heightDif <= crouchHeight)//if lower than crouch height
//                                            vLow[index].passability = (sbyte)Passability.Unwalkable;
//                                        else if (heightDif < walkableHeight && //if lower than full height
//                                            heightDif > crouchHeight && //but highter than crouch height
//                                            vLow[index].passability > (sbyte)Passability.Slope) //and if it passable at least
//                                            vLow[index].passability = (sbyte)Passability.Crouchable;

//                                    }
//                                    else if (heightDif <= walkableHeight) { //just do check passable height
//                                        vLow[index].passability = (sbyte)Passability.Unwalkable;
//                                        //Vector3 l = GetRealMax(x, z, vLow);
//                                        //Vector3 h = GetRealMin(x, z, vHigh);
//                                        //Debuger_K.AddLine(l,h,Color.blue);
//                                        //Debuger_K.AddLabel(SomeMath.MidPoint(l, h), heightDif);                 
//                                    }
//                                }
//                            }
//                            else if (vLow[index].max >= vHigh[index].min || vLow[index].min > vHigh[index].min) {
//                                vLow[index].exist = false;
//                                vHigh[index].min = Math.Min(vdp1.min, vdp2.min);

//                                if (vHigh_v != volume)
//                                    reAddMe.Add(vHigh_v);
//                                continue;
//                            }
//                        }
//                    }
//                }   
//            }

//            //resizing array to new size
//            if (addVolume) 
//                AddVolume(volume);            

//            foreach (var v in reAddMe) {
//                ApplyVolume(v, false);
//            }
//        }

//        private void AddVolume(Volume volume) {
//            volume.id = volumes.Length;
//            Volume[] newVolumes = new Volume[volumes.Length + 1];
//            for (int i = 0; i < volumes.Length; i++) {
//                newVolumes[i] = volumes[i];
//            }
//            newVolumes[volume.id] = volume;
//            volumes = newVolumes;

//            foreach (var item in volume.containsAreas) {
//                PathFinder.AddAreaHash(item, true);
//            }
//        }

//        private void RemoveAllAndAddTestVolume() {
//            //Area defaultArea = PathFinder.getDefaultArea;
//            //Volume testVolume = Volume.GetFromPool(template.lengthX_extra, template.lengthZ_extra, defaultArea);

//            //int patternSize = 4;

//            //for (int x = 0; x < template.lengthX_extra; x++) {
//            //    for (int z = 0; z < template.lengthZ_extra; z++) {
//            //        int patternZ = (int)(z / (float)patternSize);
//            //        int patternX = (int)(x / (float)patternSize);

//            //        bool oddX = (patternX & 2) != 0;
//            //        bool oddZ = (patternZ & 2) != 0;

//            //        testVolume.SetVoxel(x, z, 0, defaultArea, oddX | oddZ ? 3 : 0);
//            //        //Vector3 p = GetRealMax(x, z, testVolume);
//            //        //Debuger_K.AddLabel(p, (patternX & 2));
//            //    }
//            //}
//            //volumes = new Volume[] { testVolume };
//        }
               

//        //Debug.Log(volumesMakeHole.Count);
//        //foreach (var volume in volumesMakeHole) {
//        //    for (int x = 0; x < sizeX; x++) {
//        //        for (int z = 0; z < sizeZ; z++) {
//        //            int index = GetIndex(x, z);
//        //            if (volume.existance[index]) {
//        //                Vector3 vector = GetRealPos(x, volume.min[index], z);
//        //                Color color;
//        //                switch (volume.passability[index]) {
//        //                    case 0: color = Color.red; break;
//        //                    case 1: color = Color.magenta; break;
//        //                    case 3: color = volume.area.color; break;
//        //                    default: color = Color.white; break;
//        //                }
//        //                Debuger_K.AddDot(vector, color, 0.04f);
//        //                //if (x < 10)
//        //                //    Debuger_K.AddLabelFormat(vector, "{0}, {1}", x, z);
//        //                //Debuger_K.AddDot(GetRealPos(x, volume.max[index], z), Color.black);
//        //            }
//        //        }
//        //    }
//        //}


//        public void DoStuff() {
//            if (profiler != null) profiler.AddLog("Start combining volumes");

//            List<Volume> SINGLE_TERRAIN_CASE_list = new List<Volume>();
//            if (volumesSolid.Count == 0 && volumeTerrain.Count == 1) {
//                SINGLE_TERRAIN_CASE = true;
//            }

//            List<Volume> volumePool = new List<Volume>();
//            foreach (var item in volumeTerrain) {
//                volumePool.Add(item);
//            }
//            foreach (var item in volumeTerrainTrees) {
//                Volume tree = Volume.Convert(item);
//                if (SINGLE_TERRAIN_CASE) {
//                    SINGLE_TERRAIN_CASE_list.Add(tree);
//                }
//                tree.isTree = true;
//                tree.dead = true;
//                volumePool.Add(tree);
//            }
//            foreach (var item in volumesSolid) {
//                volumePool.Add(Volume.Convert(item));
//            }

//            #region Apply Modifyers
//            if (volumesMakeHole.Count > 0) {
//                if (profiler != null) profiler.AddLog("Apply make hole shapes");
//                //volumesMakeHole.Sort(Comparer);

//                //applying all shape modifications in advance
//                for (int i = 0; i < volumePool.Count; i++) {
//                    foreach (var mod in volumesMakeHole) {
//                        Volume newVol;
//                        if (ApplyHoleMaker(volumePool[i], mod, out newVol))
//                            volumePool.Add(newVol);
//                    }
//                }

//                foreach (var item in volumesMakeHole) {
//                    item.ReturnToPool();
//                }
//                volumesMakeHole.Clear();
//            }

//            if (volumesModifyArea.Count > 0) {
//                if (profiler != null) profiler.AddLog("Apply modify area shapes");
//                volumesModifyArea.Sort(Comparer);

//                //then apply area modifications
//                for (int i = 0; i < volumePool.Count; i++) {
//                    foreach (var mod in volumesModifyArea) {
//                        ApplyAreaModifyer(volumePool[i], mod);
//                    }
//                }
//                foreach (var item in volumesModifyArea) {
//                    item.ReturnToPool();
//                }
//                volumesModifyArea.Clear();
//            }
//            #endregion


//            if (SINGLE_TERRAIN_CASE) {
//                #region quick subtraction
//                VolumeDataPoint[] dataTerrain = volumeTerrain[0].data;

//                foreach (var tree in SINGLE_TERRAIN_CASE_list) {
//                    VolumeDataPoint[] dataTree = tree.data;
//                    for (int i = 0; i < flattenedSize; i++) {
//                        if (dataTerrain[i].exist && dataTree[i].exist && dataTree[i].BetweenMinMax(dataTerrain[i].max))
//                            dataTerrain[i].exist = false;
//                    }
//                }
//                #endregion     
//                AddVolume(volumeTerrain[0]);
//                foreach (var tree in SINGLE_TERRAIN_CASE_list) {
//                    AddVolume(tree);               
//                }
//            }
//            else {
//                foreach (var item in volumePool) {
//                    ApplyVolume(item, true);
//                }
//            }

//            //volumeTerrain.Clear();
//            //volumeTerrainTrees.Clear();
//            //volumesSolid.Clear();
//            if (profiler != null) profiler.AddLog("End combining volumes");

//            //RemoveAllAndAddTestVolume();
//            if (volumes.Length == 0) {
//                if(profiler != null)
//                    profiler.AddLogFormat("zero colliders to process on {0} chunk so we stop do stuff", template.gridPosition.ToString()); 
//                return;
//            }



//            if (SINGLE_TERRAIN_CASE == false) {
//                if (profiler != null) profiler.AddLogFormat("start merge volumes. now volumes: {0}", volumesAmount);
//                MergeVolumes(); //reduce amount of volumes 
//                if (profiler != null) profiler.AddLogFormat("end merging. now volumes: {0}", volumesAmount);
//            }

//            //foreach (var volume in volumes) {
//            //    if (volume.dead == false)
//            //        volume.CreateConnectionsArray();//to create array for storing connections data
//            //}

//            //remove dead volumes and reassign IDs
//            volumes = (from vol in volumes where vol.dead == false select vol).ToArray();
//            for (int i = 0; i < volumes.Length; i++) { volumes[i].id = i; }

//            //List < Volume > newVolumesList = new List<Volume>();
//            //for (int i = 0; i < volumesAmount; i++) {
//            //    if (!volumes[i].dead) {
//            //        newVolumesList.Add(volumes[i]); //move to temporary list
//            //    }
//            //}
//            ////vreate new array and reassign IDs
//            //volumes = new Volume[newVolumesList.Count];
//            //for (int i = 0; i < newVolumesList.Count; i++) {
//            //    volumes[i] = newVolumesList[i];
//            //    volumes[i].id = i;
//            //}
                     
//            //if (SINGLE_TERRAIN_CASE) {
//            //    volumeTerrain[0].ConnectToItself();
//            //}
//            //else {
//            //    if (profiler != null) profiler.AddLogFormat("start generating connections", volumesAmount);
//            //    GenerateConnections(); //generates connection between voxels so each voxel know their neighbours  
//            //    if (profiler != null) profiler.AddLog("end generating connections");
//            //}

//            if (profiler != null) profiler.AddLogFormat("start generating connections", volumesAmount);
//            GenerateConnections(); //generates connection between voxels so each voxel know their neighbours  
//            if (profiler != null) profiler.AddLog("end generating connections");

//            if (profiler != null) profiler.AddLog("start generating obstacles");
//            GenerateObstacles(); //populating obstacles collection so we know where is obstacles are
//            if (profiler != null) profiler.AddLog("end generating obstacles");

//            //foreach (var item in nearObstacle) {
//            //    Debuger_K.AddDot(GetRealMax(item) + (Vector3.up * 0.1f), Color.magenta);
//            //}

//            if (profiler != null)
//                profiler.AddLog("end generating obstacles");

//            //create jump spots
//            if (template.canJump) {
//                if (profiler != null)
//                    profiler.AddLog("agent can jump. start capturing areas for jump");

//                int sqrArea = template.agentRagius * template.agentRagius;
//                int doubleAreaSqr = sqrArea + sqrArea + 2; //plus some extra

//                foreach (var nearObstaclePos in nearObstacle) {
//                    if (volumes[nearObstaclePos.volume].data[GetIndex(nearObstaclePos.x, nearObstaclePos.z)].passability >= (sbyte)Passability.Crouchable &&
//                        volumes[nearObstaclePos.volume].GetState(nearObstaclePos.x, nearObstaclePos.z, VoxelState.InterconnectionAreaflag) == false)
//                        CaptureArea(nearObstaclePos, sqrArea, doubleAreaSqr, true, AreaType.Jump);
//                }

//                if (profiler != null)
//                    profiler.AddLog("end capturing areas for jump");
//            }

//            if (doCover) {
//                if (profiler != null)
//                    profiler.AddLog("agent can cover. start generating covers");

//                //important to check it before growth
//                GenerateCovers(template.agentRagius + template.coverExtraSamples);

//                for (int i = 0; i < volumesAmount; i++) {
//                    GenerateCoverMaps(volumes[i]);
//                }
      
//                //foreach (var v in volumes) {
//                //    var t = v.coverType;
//                //    for (int x = 0; x < sizeX; x++) {
//                //        for (int z = 0; z < sizeZ; z++) {
//                //            if(t[x][z] > 0) {
//                //                Debuger_K.AddDot(GetRealMax(x, z, v) + (Vector3.up * 0.1f), Color.green);
//                //            }
//                //        }
//                //    }
//                //}

//                if (profiler != null)
//                    profiler.AddLog("end generating covers");
//            }

//            if (profiler != null)
//                profiler.AddLog("start growing obstacles");

//            GrowthObstacles(
//                nearObstacle,
//                template.agentRagius * template.agentRagius,
//                (VolumePos vp) => { return volumes[vp.volume].data[GetIndex(vp.x, vp.z)].passability < (sbyte)Passability.Crouchable; },
//                (VolumePos vp) => { return volumes[vp.volume].data[GetIndex(vp.x, vp.z)].passability >= (sbyte)Passability.Crouchable; },
//                (VolumePos vp) => { volumes[vp.volume].data[GetIndex(vp.x, vp.z)].passability = (sbyte)Passability.Unwalkable; },
//                template);

//            if (profiler != null)
//                profiler.AddLog("end growing obstacles");

//            if (doCrouch) {
//                if (profiler != null)
//                    profiler.AddLog("agent can cover. start generating cover obstacles");

//                GrowthObstacles(
//                    nearCrouch,
//                    template.agentRagius * template.agentRagius,
//                    (VolumePos vp) => { return volumes[vp.volume].data[GetIndex(vp.x, vp.z)].passability == (sbyte)Passability.Crouchable; },
//                    (VolumePos vp) => { return volumes[vp.volume].data[GetIndex(vp.x, vp.z)].passability == (sbyte)Passability.Walkable; },
//                    (VolumePos vp) => { volumes[vp.volume].data[GetIndex(vp.x, vp.z)].passability = Math.Min((sbyte)Passability.Crouchable, volumes[vp.volume].data[GetIndex(vp.x, vp.z)].passability); },
//                    template);

//                if (profiler != null)
//                    profiler.AddLog("end generating cover obstacles");
//            }


//            if (template.doBattleGrid) {
//                if (profiler != null)
//                    profiler.AddLog("agent use battle grid. start creating battle grid");

//                BattleGrid();

//                if (profiler != null)
//                    profiler.AddLog("end creating battle grid");
//            }

//            if (profiler != null)
//                profiler.AddLog("start creating general maps");

//            //as an end part thereis some map generated for graph generator
//            for (int i = 0; i < volumesAmount; i++) {
//                GenerateGenealMaps(volumes[i]);
//            }

//            if (profiler != null)
//                profiler.AddLog("end creating general maps");

////#if UNITY_EDITOR
////            if (Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false) {
////                if (profiler != null)
////                    profiler.AddLog("start adding volumes to debuger");

////                Debuger_K.AddVolumes(template, this);

////                if (profiler != null)
////                    profiler.AddLog("end adding volumes to debuger");
////            }
////#endif
//        }

//        //reduce amount of volumes
//        private void MergeVolumes() {
//            Dictionary<int, HashSet<int>> banned = new Dictionary<int, HashSet<int>>(); //id, banned id's

//            foreach (var volume in volumes) {
//                banned[volume.id] = new HashSet<int>() { volume.id };
//            }

//            foreach (var volume in volumes) {
//                if(volume.isTerrain | volume.isTree)//cause it's just waiste of time
//                    foreach (var banHashSet in banned.Values) 
//                        banHashSet.Add(volume.id);
//            }

//            while (true) {
//                foreach (var curVolume in volumes) {
//                    if (curVolume.dead)
//                        continue;

//                    VolumeDataPoint[] curData = curVolume.data;
//                    Area[] curArea = curVolume.area;
//                    int[] curFlags = curVolume.flags;

//                    foreach (var otherVolume in volumes) {
//                        if (otherVolume.dead || banned[curVolume.id].Contains(otherVolume.id))
//                            continue;

//                        VolumeDataPoint[] otherData = otherVolume.data;
//                        Area[] otherArea = otherVolume.area;
//                        int[] otherFlags = otherVolume.flags;

//                        bool skip = false;

//                        for (int i = 0; i < flattenedSize; i++) {
//                            if (curData[i].exist && otherData[i].exist) {
//                                banned[curVolume.id].Add(otherVolume.id);
//                                banned[otherVolume.id].Add(curVolume.id);
//                                skip = true;
//                                break;
//                            }
//                        }

//                        if (skip)
//                            continue;

//                        //layer not skipped mean we connect two layers
//                        for (int i = 0; i < flattenedSize; i++) {
//                            if (otherData[i].exist) {
//                                curData[i] = otherData[i];
//                                curArea[i] = otherArea[i];
//                                curFlags[i] = otherFlags[i];
//                            }
//                        }        

//                        foreach (var item in otherVolume.containsAreas)
//                            curVolume.containsAreas.Add(item);

//                        otherVolume.dead = true;                
//                    }
//                }
//                break;
//            }
//        }

//        //generates connection between voxels so each voxel know their neighbours        
//        private void GenerateConnections() {
//            //reminder: volume.id equals to volume index in volumes list
//            int volumesCount = volumesAmount;//cause little overhead. we took this value too much times in code below

//            //x
//            for (int x = 0; x < sizeX - 1; x++) {
//                for (int z = 0; z < sizeZ; z++) {
//                    int curIndex = GetIndex(x, z);
//                    int curIndex_xPlusOne = GetIndex(x + 1, z);

//                    for (int curVolumeIndex = 0; curVolumeIndex < volumesCount; curVolumeIndex++) {
//                        if (volumes[curVolumeIndex].data[curIndex].exist) {
//                            float curMax = volumes[curVolumeIndex].data[curIndex].max;
//                            float closestStep = float.MaxValue;
//                            int closestVolume = 0;

//                            for (int otherVolumeIndex = 0; otherVolumeIndex < volumesCount; otherVolumeIndex++) {
//                                if (volumes[otherVolumeIndex].data[curIndex_xPlusOne].exist) {
//                                    float curStep = Math.Abs(curMax - volumes[otherVolumeIndex].data[curIndex_xPlusOne].max);

//                                    if (curStep < closestStep) {
//                                        closestStep = curStep;
//                                        closestVolume = otherVolumeIndex;
//                                    }
//                                }
//                            }

//                            if (closestStep <= connectionDistance) {
//                                volumes[curVolumeIndex].connections[(int)Directions.xPlus][x][z] = closestVolume;
//                                volumes[closestVolume].connections[(int)Directions.xMinus][x + 1][z] = curVolumeIndex;
//                            }
//                        }
//                    }
//                }
//            }

//            //z
//            for (int x = 0; x < sizeX; x++) {
//                for (int z = 0; z < sizeZ - 1; z++) {
//                    int curIndex = GetIndex(x, z);
//                    int curIndex_zPlusOne = GetIndex(x, z + 1);

//                    for (int curVolumeIndex = 0; curVolumeIndex < volumesCount; curVolumeIndex++) {
//                        if (volumes[curVolumeIndex].data[curIndex].exist) {
//                            float curMax = volumes[curVolumeIndex].data[curIndex].max;
//                            float closestStep = float.MaxValue;
//                            int closestVolume = 0;

//                            for (int otherVolumeIndex = 0; otherVolumeIndex < volumesCount; otherVolumeIndex++) {
//                                if (volumes[otherVolumeIndex].data[curIndex_zPlusOne].exist) {
//                                    float curStep = Math.Abs(curMax - volumes[otherVolumeIndex].data[curIndex_zPlusOne].max);

//                                    if (curStep < closestStep) {
//                                        closestStep = curStep;
//                                        closestVolume = otherVolumeIndex;
//                                    }
//                                }
//                            }

//                            if (closestStep <= connectionDistance) {
//                                volumes[curVolumeIndex].connections[(int)Directions.zPlus][x][z] = closestVolume;
//                                volumes[closestVolume].connections[(int)Directions.zMinus][x][z + 1] = curVolumeIndex;
//                            }
//                        }
//                    }
//                }
//            }
//        }

//        //populating obstacles collection so we know where is obstacles are
//        private void GenerateObstacles() {
//            //cause kinda long :D
//            sbyte crouchVal = (sbyte)Passability.Crouchable;
//            sbyte walkVal = (sbyte)Passability.Walkable;
            
//            foreach (var volume in volumes) {
//                VolumeDataPoint[] data = volume.data;      
//                int id = volume.id;

//                for (int x = 1; x < sizeX - 1; x++) {
//                    for (int z = 0; z < sizeZ; z++) {
//                        int index = GetIndex(x, z);
//                        VolumeDataPoint point = data[index];

//                        if (point.exist == false || point.passability < crouchVal)
//                            continue;

//                        int value = volume.connections[(int)Directions.xPlus][x][z];

//                        if (value == -1 || volumes[value].data[GetIndex(x + 1, z)].passability < crouchVal) {
//                            volume.SetState(index, VoxelState.NearObstacle, true);
//                        }

//                        value = volume.connections[(int)Directions.xMinus][x][z];
//                        if (value == -1 || volumes[value].data[GetIndex(x - 1, z)].passability < crouchVal) {
//                            volume.SetState(index, VoxelState.NearObstacle, true);
//                        }

//                        if (doCrouch && point.passability == walkVal) {
//                            value = volume.connections[(int)Directions.xPlus][x][z];
//                            if (value != -1 && volumes[value].data[GetIndex(x + 1, z)].passability == crouchVal) {
//                                volume.SetState(index, VoxelState.NearCrouch, true);
//                                this.nearCrouch.Add(new VolumePos(id, x, z));
//                            }

//                            value = volume.connections[(int)Directions.xMinus][x][z];
//                            if (value != -1 && volumes[value].data[GetIndex(x - 1, z)].passability == crouchVal) {
//                                volume.SetState(index, VoxelState.NearCrouch, true);
//                                this.nearCrouch.Add(new VolumePos(id, x, z));
//                            }
//                        }
//                    }
//                }

//                for (int x = 0; x < sizeX; x++) {
//                    for (int z = 1; z < sizeZ - 1; z++) {
//                        int index = GetIndex(x, z);
//                        VolumeDataPoint point = data[index];

//                        if (point.exist == false || point.passability < crouchVal)
//                            continue;

//                        int value = volume.connections[(int)Directions.zPlus][x][z];

//                        if (value == -1 || volumes[value].data[GetIndex(x, z + 1)].passability < crouchVal) {
//                            volume.SetState(index, VoxelState.NearObstacle, true);
//                            //nearOsbtacleSet.Add(new VolumePos(id, x, z));
//                        }

//                        value = volume.connections[(int)Directions.zMinus][x][z];
//                        if (value == -1 || volumes[value].data[GetIndex(x, z - 1)].passability < crouchVal) {
//                            volume.SetState(index, VoxelState.NearObstacle, true);
//                            //nearOsbtacleSet.Add(new VolumePos(id, x, z));
//                        }

//                        if (doCrouch && point.passability == walkVal) {
//                            value = volume.connections[(int)Directions.zPlus][x][z];
//                            if (value != -1 && volumes[value].data[GetIndex(x, z + 1)].passability == crouchVal) {
//                                volume.SetState(index, VoxelState.NearCrouch, true);
//                                //this.nearCrouch.Add(new VolumePos(id, index));
//                            }

//                            value = volume.connections[(int)Directions.zMinus][x][z];
//                            if (value != -1 && volumes[value].data[GetIndex(x, z - 1)].passability == crouchVal) {
//                                volume.SetState(index, VoxelState.NearCrouch, true);
//                                //this.nearCrouch.Add(new VolumePos(id, index));
//                            }
//                        }
//                    }
//                }

//                //totaly worth it
//                for (int x = 0; x < sizeX; x++) {
//                    for (int z = 0; z < sizeZ; z++) {
//                        int index = GetIndex(x, z);
//                        VolumeDataPoint point = data[index];                   
//                        if (point.exist == false || point.passability < crouchVal)
//                            continue;
//                        if (volume.GetState(index, VoxelState.NearObstacle))
//                            nearObstacle.Add(new VolumePos(id, x, z));
//                        if (doCrouch && volume.GetState(index, VoxelState.NearCrouch))
//                            nearCrouch.Add(new VolumePos(id, x, z));
//                    }
//                }
//            }


//            //fix obstacle set
//            //needed only if there covers generated
//            if (template.doCover) {
//                if (profiler != null)
//                    profiler.AddLog("agent do cover so we need fix nearOsbtacleSet. start fixing");

//                //int are number of neighbour.
//                //at least 2 needed to make changes. 
//                //to avoid extra changes start value is 5 cause 4 neighbours is max number

//                Dictionary<VolumePos, int> dictionary = new Dictionary<VolumePos, int>();
//                foreach (var nearObstacle in nearObstacle) {
//                    dictionary.Add(nearObstacle, 5);
//                }

//                foreach (var nearObstacle in nearObstacle) {
//                    for (int i = 0; i < 4; i++) {
//                        VolumePos result;
//                        if(TryGetLeveled(nearObstacle, (Directions)i, out result)){
//                            if (dictionary.ContainsKey(result))
//                                dictionary[result]++;
//                            else
//                                dictionary.Add(result, 1);
//                        }
//                    }
//                }

//                foreach (var extendedObstacle in dictionary) {
//                    if (extendedObstacle.Value < 5 && extendedObstacle.Value > 1) {
//                        VolumePos pos = extendedObstacle.Key;
//                        nearObstacle.Add(pos);
//                        volumes[pos.volume].SetState(pos.x, pos.z, VoxelState.NearObstacle, true);
//                    }
//                }

//                if (profiler != null)
//                    profiler.AddLog("end fixing");
//            }
//        }

//        private void GenerateGenealMaps(Volume volume) {
//            AreaPassabilityHashData hashData = template.hashData;
//            sbyte minPass = (sbyte)Passability.Crouchable;
//            int extra = template.extraOffset;

//            //used values
//            VolumeDataPoint[] data = volume.data;
//            Area[] area = volume.area;
//            int[] hashMap = volume.hashMap;
//            bool[] heightInterest = volume.heightInterest;

//            for (int x = extra; x < sizeX - extra; x++) {
//                for (int z = extra; z < sizeZ - extra; z++) {
//                    int index = GetIndex(x, z);
//                    VolumeDataPoint point = data[index];
//                    if (point.exist && point.passability >= minPass) {
//                        hashMap[index] = hashData.GetAreaHash(area[index], (Passability)point.passability);
//                        heightInterest[index] = true;                                  
//                    }
//                }
//            }
//        }
        
//        public void GenerateCoverMaps(Volume volume) {
//            sbyte minPass = (sbyte)Passability.Walkable;
//            int extra = template.extraOffset;

//            //used values
//            VolumeDataPoint[] data = volume.data;
//            int[] coverHashMap = volume.coverHashMap;
//            bool[] coverHeightInterest = volume.coverHeightInterest;

//            for (int x = extra; x < sizeX - extra; x++) {
//                for (int z = extra; z < sizeZ - extra; z++) {
//                    int index = GetIndex(x, z);
//                    VolumeDataPoint point = data[index];
//                    if (point.exist == false ||
//                       point.passability < minPass ||         
//                       volume.GetState(index, VoxelState.NearObstacle)) {
//                       coverHashMap[index] = -1;
//                    }
//                    else {
//                        coverHashMap[index] = MarchingSquaresIterator.COVER_HASH;
//                        coverHeightInterest[index] = true;
//                    }
//                }
//            }
//        }

//        //this ugly function are shift voxels in growth pattern from obstacles and crouch areas
//        private void GrowthObstacles(
//            List<VolumePos> origins,
//            float sqrDistance,
//            Func<VolumePos, bool> positive,
//            Func<VolumePos, bool> growTo,
//            Action<VolumePos> grow,
//            NavMeshTemplateCreation template) {

//            Dictionary<VolumePos, VolumePos> originsDistionary = new Dictionary<VolumePos, VolumePos>();//position of value, position of origin
//            HashSet<VolumePos> lastIteration = new HashSet<VolumePos>(origins);

//            foreach (var item in origins) {
//                originsDistionary[item] = item;
//                grow(item);
//            }

//            Dictionary<VolumePos, HashSet<VolumePos>> borderDictionary = new Dictionary<VolumePos, HashSet<VolumePos>>();

//            while (true) {
//                foreach (var lastIterationPos in lastIteration) {
//                    for (int i = 0; i < 4; i++) {
//                        VolumePos value;
//                        if (TryGetLeveled(lastIterationPos, (Directions)i, out value) == false
//                            || positive(value) | growTo(value) == false)
//                            continue;

//                        if (borderDictionary.ContainsKey(value) == false)
//                            borderDictionary.Add(value, new HashSet<VolumePos>());

//                        borderDictionary[value].Add(originsDistionary[lastIterationPos]);
//                    }
//                }


//                HashSet<VolumePos> newIteration = new HashSet<VolumePos>();

//                foreach (var curPoint in borderDictionary) {
//                    VolumePos? closest = null;
//                    int dist = int.MaxValue;

//                    foreach (var root in curPoint.Value) {
//                        int curDist = SomeMath.SqrDistance(curPoint.Key.x, curPoint.Key.z, root.x, root.z);
//                        if (curDist < sqrDistance & curDist < dist) {
//                            dist = curDist;
//                            closest = root;
//                        }
//                    }
//                    if (closest.HasValue) {
//                        newIteration.Add(curPoint.Key);
//                        originsDistionary.Add(curPoint.Key, closest.Value);
//                        grow(curPoint.Key);
//                        //Vector3 curP = GetRealMax(curPoint.Key);
//                        //Vector3 closestP = GetRealMax(closest.Value);
//                        //Debuger_K.AddLine(curP, closestP, Color.cyan);
//                    }
//                }

//                if (newIteration.Count == 0)
//                    break;

//                lastIteration = newIteration;
//                borderDictionary.Clear();
//            }
//        }

//        public VolumeArea CaptureArea(VolumePos basePos, int sqrArea, int sqrOffset, bool addAreaFlag, AreaType areaType, bool debug = false) {
//            VolumeArea areaObject = new VolumeArea(GetRealMax(basePos), areaType);

//            HashSet<VolumePos> area = new HashSet<VolumePos>();
//            area.Add(basePos);

//            HashSet<VolumePos> lastAreaIteration = new HashSet<VolumePos>();
//            lastAreaIteration.Add(basePos);

//            HashSet<VolumePos> doubleArea = new HashSet<VolumePos>();

//            while (true) {
//                if (lastAreaIteration.Count == 0)
//                    break;

//                HashSet<VolumePos> newAxisIteration = new HashSet<VolumePos>();

//                foreach (var lastIterationPos in lastAreaIteration) {
//                    //captured only further positions
//                    int curDistance = SomeMath.SqrDistance(basePos.x, basePos.z, lastIterationPos.x, lastIterationPos.z);

//                    for (int i = 0; i < 4; i++) {
//                        VolumePos neighbour;
//                        if (TryGetLeveled(lastIterationPos, (Directions)i, out neighbour) == false)
//                            continue;

//                        int neighbourDistance = SomeMath.SqrDistance(basePos.x, basePos.z, neighbour.x, neighbour.z);

//                        if (neighbourDistance >= curDistance && neighbourDistance <= sqrOffset && doubleArea.Add(neighbour)) {
//                            newAxisIteration.Add(neighbour);
//                            if (neighbourDistance < sqrArea)
//                                area.Add(neighbour);
//                        }
//                    }
//                }
//                lastAreaIteration = newAxisIteration;
//            }

//            foreach (var pos in area) {
//                int s = GetIndex(pos.x, pos.z);
//                HashSet<VolumeArea> a;
//                Volume v = volumes[pos.volume];
//                if (v.volumeArea.TryGetValue(s, out a) == false) {
//                    a = new HashSet<VolumeArea>();
//                    v.volumeArea.Add(s, a);
//                }
//                a.Add(areaObject);
//                v.SetState(pos.x, pos.z, VoxelState.InterconnectionArea, true);
//            }

//            if (addAreaFlag) {
//                foreach (var pos in doubleArea) {
//                    volumes[pos.volume].SetState(pos.x, pos.z, VoxelState.InterconnectionAreaflag, true);
//                }
//            }
//#if UNITY_EDITOR
//            if (debug) {
//                Vector3 basePosReal = GetRealMax(basePos);
//                foreach (var item in area) {
//                    Debuger_K.AddLine(basePosReal, GetRealMax(item), Color.cyan);
//                }

//                Debuger_K.AddLabel(basePosReal, string.Format("type: {0}\ncount: {1}\nsqr area: {2}\nsqr offset: {3}",
//                    areaType,
//                    area.Count,
//                    sqrArea, sqrOffset));
//            }
//#endif


//            volumeAreas.Add(areaObject);
//            return areaObject;
//        }

//        /// some more spaghetti code. used to rate fragments from 0 to 2 in cover capability
//        /// 0 is no cover, 1 is half cover, 2 is full cover. goto used to exit nested loops
//        private void GenerateCovers(int sampleDistance) {       
//            sampleDistance = Math.Max(2, sampleDistance);

//            var pattern = GetPattern(sampleDistance);
//            int patternSize = pattern.size;
//            int patternRadius = pattern.radius - 1;
//            bool[][] patternGrid = pattern.pattern;

//            Volume volume;

//            int minIndex = template.extraOffset;
//            int maxIndexX = template.lengthX_extra - template.extraOffset;
//            int maxIndexZ = template.lengthZ_extra - template.extraOffset;                    

//            foreach (var item in nearObstacle) {
//                volume = volumes[item.volume];
//                int baseIndex = GetIndex(item.x, item.z);
//                if (volume.dead || item.x < minIndex || item.z < minIndex || item.x > maxIndexX || item.z > maxIndexZ || volume.data[baseIndex].passability < (sbyte)Passability.Crouchable)
//                    continue;
  
//                float baseMax = volume.data[baseIndex].max;

//                for (int x_pattern = 0; x_pattern < patternSize; x_pattern++) {
//                    for (int z_pattern = 0; z_pattern < patternSize; z_pattern++) {
//                        if (patternGrid[x_pattern][z_pattern] == false)//we have base and we have to check this space to cover
//                            continue;

//                        int checkX = item.x - patternRadius + x_pattern;

//                        if (checkX < 0 || checkX >= sizeX)
//                            continue;

//                        int checkZ = item.z - patternRadius + z_pattern;

//                        if (checkZ < 0 || checkZ >= sizeZ)
//                            continue;

//                        int checkIndex = GetIndex(checkX, checkZ);

//                        #region debug
//                        //Vector3 p1 = GetRealMax(baseX, baseZ, volume);
//                        //Vector3 p2 = template.realPosition
//                        //        + (new Vector3(checkX, 0, checkZ) * template.fragmentSize)
//                        //        + (template.halfFragmentOffset)
//                        //        + new Vector3(0, p1.y, 0);

//                        //Debuger3.AddLine(p1, p2, Color.red);
//                        #endregion

//                        int cover = 0;

//                        for (int i = 0; i < volumes.Length; i++) {
//                            if (volumes[i].GetState(checkX, checkZ, VoxelState.Tree)) {
//                                volume.SetState(baseIndex, VoxelState.NearTree, true);
//                                volume.coverType[baseIndex] = 0;
//                                goto CONTINUE;
//                            }

//                            if (volumes[i].data[checkIndex].min > baseMax)
//                                continue;

//                            float dif = volumes[i].data[checkIndex].max - baseMax;

//                            if (doHalfCover && dif > halfCover)
//                                cover = 1;

//                            if (dif > fullCover) {
//                                volume.coverType[baseIndex] = 2;
//                                goto CONTINUE;
//                            }
//                        }
//                        volume.coverType[baseIndex] = Math.Max(volume.coverType[baseIndex], cover);
                        
//                    }
//                }
//                CONTINUE:
//                {
//                    continue;
//                }
//            }
//        }

//        /// <summary>
//        /// amount of volumes stored in container.
//        /// this acessor exist cause i couple of times change it to array then to list then to array again. tired to change Count/Length everywhere
//        /// </summary>
//        public int volumesAmount {
//            get { return volumes.Length; }
//        }

//        //have no use but it's readable and represent how it should work at least
//        //was used in GenerateConnections() function
//        public void SetConnection(int x, int z, int volume, Directions direction, int value) {
//            volumes[volume].connections[(int)direction][x][z] = value;

//            switch (direction) {
//                case Directions.xPlus:
//                    volumes[value].connections[(int)Directions.xMinus][x + 1][z] = volume;
//                    break;
//                case Directions.xMinus:
//                    volumes[value].connections[(int)Directions.xPlus][x - 1][z] = volume;
//                    break;
//                case Directions.zPlus:
//                    volumes[value].connections[(int)Directions.zMinus][x][z + 1] = volume;
//                    break;
//                case Directions.zMinus:
//                    volumes[value].connections[(int)Directions.zPlus][x][z - 1] = volume;
//                    break;
//            }
//        }

//        // most of this code are about proper alighment in world
//        // it's calculate all grid shifts in world space and then shift this grid to proper distance
//        // (actualy code is far from optimal but it's not that bad so i never touch it)
//        private void BattleGrid() {
//            int density = template.battleGridDensity;
//            var chunkPos = template.gridPosition;

//            int fPosX = template.lengthX_central * chunkPos.x;
//            int fPosZ = template.lengthZ_central * chunkPos.z;

//            int lastGridLeftX = fPosX - (fPosX / template.battleGridDensity * template.battleGridDensity);
//            int lastGridLeftZ = fPosZ - (fPosZ / template.battleGridDensity * template.battleGridDensity);

//            int lastChunkLeftX = lastGridLeftX == 0 ? 0 : template.battleGridDensity - lastGridLeftX;
//            int lastChunkLeftZ = lastGridLeftZ == 0 ? 0 : template.battleGridDensity - lastGridLeftZ;

//            if (lastChunkLeftX > template.battleGridDensity) //negative chunk position
//                lastChunkLeftX -= template.battleGridDensity;

//            if (lastChunkLeftZ > template.battleGridDensity) //negative chunk position
//                lastChunkLeftZ -= template.battleGridDensity;

//            int lengthX = ((template.lengthX_central - lastChunkLeftX - 1) / template.battleGridDensity) + 1;
//            int lengthZ = ((template.lengthZ_central - lastChunkLeftZ - 1) / template.battleGridDensity) + 1;

//            int offsetX = template.extraOffset + lastChunkLeftX;
//            int offsetZ = template.extraOffset + lastChunkLeftZ;

//            Dictionary<VolumePos, VolumePos?[]> gridDic = new Dictionary<VolumePos, VolumePos?[]>();
//            Dictionary<VolumePos, BattleGridPoint> bgpDic = new Dictionary<VolumePos, BattleGridPoint>();

//            foreach (var volume in volumes) {
//                VolumeDataPoint[] data = volume.data;

//                for (int x = 0; x < lengthX; x++) {
//                    for (int z = 0; z < lengthZ; z++) {
//                        VolumePos curPos = new VolumePos(volume.id, offsetX + (x * density), offsetZ + (z * density));

//                        if (volume.Exist(curPos) == false || volume.Passability(curPos) < Passability.Crouchable)
//                            continue;

//                        gridDic.Add(curPos, new VolumePos?[4]);
//                        bgpDic.Add(curPos, new BattleGridPoint(GetRealMax(curPos), volume.Passability(curPos), new VectorInt.Vector2Int(x, z)));
//                    }
//                }
//            }

//            //x
//            foreach (var volume in volumes) {
//                for (int x = 0; x < lengthX - 1; x++) {
//                    for (int z = 0; z < lengthZ; z++) {
//                        VolumePos curPos = new VolumePos(volume.id, offsetX + (x * density), offsetZ + (z * density));     //volume pos 

//                        if (volume.Exist(curPos) == false || volume.Passability(curPos) < Passability.Crouchable)
//                            continue;

//                        VolumePos curChangedPos = curPos;

//                        for (int i = 0; i < density; i++) {
//                            if (TryGetLeveled(curChangedPos, Directions.xPlus, out curChangedPos) == false || PassabilityVol(curChangedPos) < K_PathFinder.Passability.Crouchable)
//                                goto NEXT;//exit from nexted loop
//                        }
//                        gridDic[curPos][(int)Directions.xPlus] = curChangedPos;
//                        gridDic[curChangedPos][(int)Directions.xMinus] = curPos;

//                        NEXT: { continue; }
//                    }
//                }
//            }


//            //z
//            foreach (var volume in volumes) {
//                for (int x = 0; x < lengthX; x++) {
//                    for (int z = 0; z < lengthZ - 1; z++) {
//                        VolumePos curPos = new VolumePos(volume.id, offsetX + (x * density), offsetZ + (z * density));     //volume pos 

//                        if (volume.Exist(curPos) == false || volume.Passability(curPos) < K_PathFinder.Passability.Crouchable)
//                            continue;

//                        VolumePos curChangedPos = curPos;

//                        for (int i = 0; i < density; i++) {
//                            if (TryGetLeveled(curChangedPos, Directions.zPlus, out curChangedPos) == false || PassabilityVol(curChangedPos) < K_PathFinder.Passability.Crouchable)
//                                goto NEXT;//exit from nexted loop
//                        }
//                        gridDic[curPos][(int)Directions.zPlus] = curChangedPos;
//                        gridDic[curChangedPos][(int)Directions.zMinus] = curPos;

//                        NEXT: { continue; }
//                    }
//                }
//            }


//            //transfer connections
//            foreach (var fragKeyValue in gridDic) {
//                var bgp = bgpDic[fragKeyValue.Key];
//                var ar = fragKeyValue.Value;

//                for (int i = 0; i < 4; i++) {
//                    if (ar[i].HasValue)
//                        bgp.neighbours[i] = bgpDic[ar[i].Value];
//                }
//            }

//            battleGrid = new BattleGrid(lengthX, lengthZ, bgpDic.Values);

//#if UNITY_EDITOR
//            //debug battle grid
//            if (PFDebuger.Debuger_K.doDebug) {
//                //since it's just bunch of lines i transfer it as list of vector 3
//                List<Vector3> debugList = new List<Vector3>();
//                foreach (var fragKeyValue in gridDic) {
//                    var f = fragKeyValue.Key;
//                    var v = fragKeyValue.Value;

//                    for (int i = 0; i < 4; i++) {
//                        if (v[i].HasValue) {
//                            debugList.Add(bgpDic[f].positionV3);
//                            debugList.Add(bgpDic[v[i].Value].positionV3);
//                        }
//                    }
//                }
//                //Debuger3.AddBattleGridConnection(template.chunk, template.properties, debugList);
//            }
//#endif
//        }

//        //public for debug purpose
//        //max
//        public Vector3 GetRealPos(int x, float y, int z) {
//            return new Vector3(
//                template.realOffsetedPositionX + (x * template.voxelSize) + template.halfVoxelOffset.x,
//                y, 
//                template.realOffsetedPositionZ + (z * template.voxelSize) + template.halfVoxelOffset.z);        
//        }
//        public Vector3 GetRealMax(int x, int z, Volume volume) {
//            return GetRealPos(x, volume.data[GetIndex(x, z)].max, z);
//        }
//        public Vector3 GetRealMax(int x, int z, int volume) {
//            return GetRealMax(x, z, volumes[volume]);
//        }
//        public Vector3 GetRealMax(VolumePos pos) {
//            return GetRealMax(pos.x, pos.z, pos.volume);
//        }

//        //min
//        public Vector3 GetRealMin(int x, int z, Volume volume) {
//            return GetRealPos(x, volume.data[GetIndex(x, z)].min, z);
//        }
//        public Vector3 GetRealMin(int x, int z, int volume) {
//            return GetRealMin(x, z, volumes[volume]);
//        }
//        public Vector3 GetRealMin(VolumePos pos) {
//            return GetRealMin(pos.x, pos.z, pos.volume);
//        }

//        public bool TryGetLeveled(Volume volume, int x, int z, Directions direction, out int result) {
//            result = volume.connections[(int)direction][x][z];
//            return result != -1;
//        }
//        public bool TryGetLeveled(Volume volume, int x, int z, Directions direction, out VolumePos result) {
//            int connection = volume.connections[(int)direction][x][z];
//            if (connection != -1) {
//                switch (direction) {
//                    case Directions.xPlus:
//                        result = new VolumePos(connection, x + 1, z);
//                        break;
//                    case Directions.xMinus:
//                        result = new VolumePos(connection, x - 1, z);
//                        break;
//                    case Directions.zPlus:
//                        result = new VolumePos(connection, x, z + 1);
//                        break;
//                    case Directions.zMinus:
//                        result = new VolumePos(connection, x, z - 1);
//                        break;
//                    default:
//                        result = new VolumePos();
//                        break;
//                }
//                return true;
//            }
//            else {
//                result = new VolumePos();
//                return false;
//            }
//        }
//        public bool TryGetLeveled(VolumePos volumePos, Directions direction, out int result) {
//            return TryGetLeveled(volumes[volumePos.volume], volumePos.x, volumePos.z, direction, out result);
//        }
//        public bool TryGetLeveled(VolumePos volumePos, Directions direction, out VolumePos result) {
//            return TryGetLeveled(volumes[volumePos.volume], volumePos.x, volumePos.z, direction, out result);
//        }
//        public bool TryGetLeveled(int volume, int x, int z, Directions direction, out int result) {
//            return TryGetLeveled(volumes[volume], x, z, direction, out result);
//        }
//        public bool TryGetLeveled(int volume, int x, int z, Directions direction, out VolumePos result) {
//            return TryGetLeveled(volumes[volume], x, z, direction, out result);
//        }

//        private Passability PassabilityVol(VolumePos pos) {
//            return volumes[pos.volume].Passability(pos);
//        }

//        public bool GetClosestPos(Vector3 pos, out VolumePos closestPos) {
//            float fragmentSize = template.voxelSize;

//            Vector3 ajustedPos = pos - template.realOffsetedPosition;
//            int x = Mathf.RoundToInt((ajustedPos.x - (fragmentSize * 0.5f)) / fragmentSize);
//            int z = Mathf.RoundToInt((ajustedPos.z - (fragmentSize * 0.5f)) / fragmentSize);

//            float curDist = float.MaxValue;
//            VolumePos? curPos = null;

//            //sample 3x3 an all grid
//            foreach (var volume in volumes) {
//                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x - 1, z - 1);
//                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x - 1, z);
//                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x - 1, z + 1);

//                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x, z - 1);
//                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x, z);
//                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x, z + 1);

//                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x + 1, z - 1);
//                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x + 1, z);
//                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x + 1, z + 1);
//            }

//            if (curPos.HasValue) {
//                closestPos = curPos.Value;
//                return true;
//            }
//            else {
//                closestPos = new VolumePos();
//                return false;
//            }
//        }

//        private void GetClosestPosReadableShortcut(ref Vector3 pos, ref float curDist, ref VolumePos? curPos, Volume volume, int x, int z) {
//            VolumeDataPoint vdp = volume.data[GetIndex(x, z)];
//            if (vdp.exist) {
//                float dist = SomeMath.SqrDistance(GetRealPos(x, vdp.max, z), pos);
//                if (dist < curDist) {
//                    curPos = new VolumePos(volume.id, x, z);
//                    curDist = dist;
//                }
//            }
//        }

//        #region patterns
//        private static CirclePattern GetPattern(int radius) {         
//            CirclePattern result;
//            lock (patterns) {
//                if (!patterns.TryGetValue(radius, out result)) {
//                    result = new CirclePattern(radius);
//                    patterns.Add(radius, result);
//                }
//            }
//            return result;
//        }
//        private class CirclePattern {
//            public int radius;
//            public bool[][] pattern;
//            public int size;

//            public CirclePattern(int radius) {
//                this.radius = radius;
//                size = radius + radius - 1;
//                int sqrRadius = (radius - 1) * (radius - 1);
//                pattern = new bool[size][];
//                for (int x = 0; x < size; x++) {
//                    pattern[x] = new bool[size];
//                    for (int y = 0; y < size; y++) {
//                        pattern[x][y] = SomeMath.SqrDistance(x, y, radius - 1, radius - 1) <= sqrRadius;
//                    }
//                }
//            }
//        }
//        #endregion
//    }
//}