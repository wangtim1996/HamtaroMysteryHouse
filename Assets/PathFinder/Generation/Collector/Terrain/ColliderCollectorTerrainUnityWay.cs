using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using K_PathFinder.VectorInt ;
using System;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

//namespace K_PathFinder {

//    public class ColliderCollectorTerrainUnityWay : TerrainCollectorAbstract {
//        //terrains
//        private List<TerrainColliderInfoPrecise> _terrainsInfo = new List<TerrainColliderInfoPrecise>();

//        //main thread
//        public ColliderCollectorTerrainUnityWay(NavMeshTemplateCreation template) : base(template) {

//        }

//        public override void AddColliders(Collider[] colliders) {
//            List<Terrain> terrains = new List<Terrain>();
//            for (int i = 0; i < colliders.Length; i++) {
//                if (IsValid(colliders[i]))
//                    terrains.Add(colliders[i].GetComponent<Terrain>());
//            }

//            float fragmentSize = template.voxelSize;
//            float halfFragment = fragmentSize * 0.5f;


//            foreach (var curTerrain in terrains) {
//                TerrainData data = curTerrain.terrainData;
//                Vector3 position = curTerrain.transform.position;
//                VectorInt.Vector3Int terrainStartInt = new VectorInt.Vector3Int((position / fragmentSize) + template.halfFragmentOffset);
//                VectorInt.Vector3Int terrainEndInt = new VectorInt.Vector3Int((position + data.size) / fragmentSize + template.halfFragmentOffset);

//                int startXClamp = Mathf.Clamp(terrainStartInt.x, template.startX_extra, template.endX_extra);
//                int startZClamp = Mathf.Clamp(terrainStartInt.z, template.startZ_extra, template.endZ_extra);

//                int endXClamp = Mathf.Clamp(terrainEndInt.x, template.startX_extra, template.endX_extra);
//                int endZClamp = Mathf.Clamp(terrainEndInt.z, template.startZ_extra, template.endZ_extra);

//                int terrainStartX = terrainStartInt.x;
//                int terrainStartZ = terrainStartInt.z;

//                int offset = terrainStartInt.y;

//                float terrainSizeX = terrainEndInt.x - terrainStartX;
//                float terrainSizeZ = terrainEndInt.z - terrainStartZ;

//                float[][] heightMap = new float[template.lengthX_extra][];
//                int[][] passabilityMap = new int[template.lengthX_extra][]; //angle map

//                for (int x = 0; x < template.lengthX_extra; x++) {
//                    heightMap[x] = new float[template.lengthZ_extra];
//                    passabilityMap[x] = new int[template.lengthZ_extra];
//                }

//                //some cashed values
//                int startX_extra = template.startX_extra;
//                int startZ_extra = template.startZ_extra;
//                float maxSlope = template.maxSlope;

//                //lets do bazzilion times Terrain.SampleHeight and TerrainData.GetSteepness!
//                //it cant be done in thread cause terrain API is "threadsafe"!
//                //actualy it can be done. but in order to do this we need to recreate this part of terrain mesh in thread but no. i dont want to do this. no.
//                //maybe later


//                for (int x = startXClamp; x < endXClamp; x++) {
//                    for (int z = startZClamp; z < endZClamp; z++) {
//                        heightMap[x - startX_extra][z - startZ_extra] = curTerrain.SampleHeight(new Vector3(x * fragmentSize + halfFragment, 0, z * fragmentSize + halfFragment)) + offset;
//                        passabilityMap[x - startX_extra][z - startZ_extra] = data.GetSteepness((x - terrainStartX) / terrainSizeX, (z - terrainStartZ) / terrainSizeZ) < maxSlope ? (int)Passability.Walkable : (int)Passability.Slope;
//                    }
//                }

//                TerrainColliderInfoPrecise info = new TerrainColliderInfoPrecise(curTerrain, heightMap, passabilityMap);

//                if (template.profiler != null)
//                    template.profiler.AddLog("start collecting tree bounds");

//                //info.treeData = GenerateTreeBounds(curTerrain);
//                GenerateTreeColliders(curTerrain);

//                if (template.profiler != null)
//                    template.profiler.AddLog("end collecting tree bounds. collected bounds: " + info.treeData.Count);

//                SetTerrainSettings(info, curTerrain,
//                    startXClamp, startZClamp,
//                    endXClamp, endZClamp,
//                    terrainStartX, terrainStartZ,
//                    terrainSizeX, terrainSizeZ,
//                    position);

//                _terrainsInfo.Add(info);
//            }
//        }

//        //not main thread
//        public override void Collect(VolumeContainer container) {
//            Area defaultArea = PathFinder.GetArea(0);

//            foreach (var terrain in _terrainsInfo) {
//                //terrain
//                Volume terrainVolume;

//                if (terrain.alphaMap != null)
//                    terrainVolume = new Volume(template.lengthX_extra, template.lengthZ_extra, terrain.possibleArea);
//                else
//                    terrainVolume = new Volume(template.lengthX_extra, template.lengthZ_extra, defaultArea);

//                terrainVolume.terrain = true;

//                float[][] heightMap = terrain.heightMap;
//                int[][] passabilityMap = terrain.passabilityMap;

//                var areaLibrary = PathFinder.settings.areaLibrary;

//                //apply terrain area info if it exist
//                //SetTerrainArea(terrainVolume, terrain, defaultArea); 
//                if (terrain.alphaMap != null) {
//                    int[][] areaMap = ProcessAlphaMap(terrain);

//                    for (int x = 0; x < template.lengthX_extra; x++) {
//                        for (int z = 0; z < template.lengthZ_extra; z++) { 
//                            terrainVolume.SetVoxel(x, z, heightMap[x][z], areaLibrary[areaMap[x][z]], passabilityMap[x][z]);

//                            if (areaMap[x][z] == 1)
//                                terrainVolume.SetPassability(x, z, Passability.Unwalkable);
//                        }
//                    }
//                }
//                else {
//                    for (int x = 0; x < template.lengthX_extra; x++) {
//                        for (int z = 0; z < template.lengthZ_extra; z++) {
//                            terrainVolume.SetVoxel(x, z, heightMap[x][z], defaultArea, passabilityMap[x][z]);
//                        }
//                    }
//                }

//                terrainVolume.SetVolumeMinimum(-1000f);

//                //trees
//                Volume treeVolume = base.CollectTrees(terrain);

//                //connecting terrain and trees to single volume
//                if (treeVolume != null) {
//                    terrainVolume.Subtract(treeVolume);
//                    terrainVolume.ConnectToItself();
//                    terrainVolume.Override(treeVolume);
//                }

//                //sent terrain to container
//                container.AddSolidVolume(terrainVolume);
//                //container.AddVolume(treeVolume);
//            }
//        }

//        public override int collectedCount {
//            get { return _terrainsInfo.Count; }
//        }
//    }
//}