using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using K_PathFinder.VectorInt ;
using System;
using UnityEngine.Networking;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

//namespace K_PathFinder {
//    public class ColliderCollectorTerrainCPU : ColliderCollectorTerrainMeshAbstract {
//        public ColliderCollectorTerrainCPU(NavMeshTemplateCreation template) : base(template) {}

//        public override int collectedCount {
//            get { return terrainsInfo.Count; }
//        }

//        //not main thread
//        public override void Collect(VolumeContainer container) {
//            Area defaultArea = PathFinder.GetArea(0);
//            float maxSlopeCos = Mathf.Cos((float)((double)template.maxSlope * Math.PI / 180.0));
//            float voxelSize = template.voxelSize;

//            Vector3 realChunkPos = template.realOffsetedPosition;
//            float chunkPosX = realChunkPos.x;
//            float chunkPosZ = realChunkPos.z;

//            int offsetX = Mathf.RoundToInt(chunkPosX / voxelSize);
//            int offsetZ = Mathf.RoundToInt(chunkPosZ / voxelSize);

//            int sizeX = template.lengthX_extra;
//            int sizeZ = template.lengthZ_extra;          

//            foreach (var terrain in terrainsInfo) {
//                Volume terrainVolume;
//                if (terrain.alphaMap != null)
//                    terrainVolume = Volume.GetFromPool(template.lengthX_extra, template.lengthZ_extra, terrain.possibleArea);
//                else
//                    terrainVolume = Volume.GetFromPool(template.lengthX_extra, template.lengthZ_extra, defaultArea);

//                terrainVolume.isTerrain = true;

//                if (profiler != null) profiler.AddLog("Start rasterization of current terrain");
//                Vector3[] vrts;
//                int[] trs;
//                GetTerrainMesh(terrain, out vrts, out trs);

//                //actual rasterization
//                for (int i = 0; i < trs.Length; i += 3) {
//                    Vector3 A = vrts[trs[i]];
//                    Vector3 B = vrts[trs[i + 1]];
//                    Vector3 C = vrts[trs[i + 2]];

//                    sbyte passability = CalculateWalk(A, B, C, maxSlopeCos) ? (sbyte)Passability.Walkable : (sbyte)Passability.Slope;//if true then walkable else slope;

//                    int minX = Mathf.Clamp(Mathf.FloorToInt(SomeMath.Min(A.x, B.x, C.x) / voxelSize) - offsetX, 0, sizeX);
//                    int maxX = Mathf.Clamp(Mathf.CeilToInt(SomeMath.Max(A.x, B.x, C.x) / voxelSize) - offsetX, 0, sizeX);
//                    int minZ = Mathf.Clamp(Mathf.FloorToInt(SomeMath.Min(A.z, B.z, C.z) / voxelSize) - offsetZ, 0, sizeZ);
//                    int maxZ = Mathf.Clamp(Mathf.CeilToInt(SomeMath.Max(A.z, B.z, C.z) / voxelSize) - offsetZ, 0, sizeZ);

//                    for (int x = minX; x < maxX; x++) {
//                        for (int z = minZ; z < maxZ; z++) {
//                            float pointX = (x * voxelSize) + chunkPosX;
//                            float pointZ = (z * voxelSize) + chunkPosZ;
//                            if (SomeMath.LineSide(A.x, A.z, B.x, B.z, pointX, pointZ) <= 0.001 & 
//                                SomeMath.LineSide(B.x, B.z, C.x, C.z, pointX, pointZ) <= 0.001 & 
//                                SomeMath.LineSide(C.x, C.z, A.x, A.z, pointX, pointZ) <= 0.001) {                   
//                                terrainVolume.SetVoxel(x, z, SomeMath.CalculateHeight(A, B, C, pointX, pointZ), passability);
//                            }
//                        }
//                    }
//                }
//                terrainVolume.SetVolumeMinimum(-1000f);
//                if (profiler != null) profiler.AddLog("End rasterization of current terrain");

//                if (profiler != null) profiler.AddLog("Start apply terrain area");
//                SetTerrainArea(terrainVolume, terrain, defaultArea); //apply terrain area info if it exist 
//                if (profiler != null) profiler.AddLog("End apply terrain area");

//                //trees
//                if (profiler != null) profiler.AddLog("Start collecting trees");
//                var trees = CollectTrees(terrain);
//                if (profiler != null) profiler.AddLogFormat("End collecting trees. Collected shapes: {0}", shapeData.Count);

//                //container.AddTerrainVolumes(terrainVolume, trees);              
//            }
//        }       


//    }
//}
