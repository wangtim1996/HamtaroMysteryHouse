using K_PathFinder.Rasterization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Collector3 {
    public partial class ShapeCollector {
        //public void AppendTerrain(CSRasterization2DResult rasterizationResult, TerrainColliderInfoMesh terrainInfo) {
        //    var voxels = rasterizationResult.voxels;
        //    for (int x = 0; x < sizeX; x++) {
        //        for (int z = 0; z < sizeZ; z++) {
        //            var curVoxel = voxels[x + (z * sizeX)];
        //            if (curVoxel.exist)
        //                SetVoxel(x, z, curVoxel.height - 20f, curVoxel.height, (sbyte)curVoxel.passability);
        //        }
        //    }
        //}

        private  DataCompact[] GetTerrainCompactData(Vector3[] vrts, int[] trs) {
            var compactData = TakeCompactData();

            float maxSlopeCos = Mathf.Cos(template.maxSlope * Mathf.PI / 180.0f);
            float voxelSize = template.voxelSize;

            Vector3 realChunkPos = template.realOffsetedPosition;
            float chunkPosX = realChunkPos.x;
            float chunkPosZ = realChunkPos.z;

            int offsetX = Mathf.RoundToInt(chunkPosX / voxelSize);
            int offsetZ = Mathf.RoundToInt(chunkPosZ / voxelSize);

            int sizeX = template.lengthX_extra;
            int sizeZ = template.lengthZ_extra;

            //actual rasterization
            for (int i = 0; i < trs.Length; i += 3) {
                Vector3 A = vrts[trs[i]];
                Vector3 B = vrts[trs[i + 1]];
                Vector3 C = vrts[trs[i + 2]];

                sbyte passability = CalculateWalk(A, B, C, maxSlopeCos) ? (sbyte)Passability.Walkable : (sbyte)Passability.Slope;//if true then walkable else slope;

                int minX = Mathf.Clamp(Mathf.FloorToInt(SomeMath.Min(A.x, B.x, C.x) / voxelSize) - offsetX, 0, sizeX);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(SomeMath.Max(A.x, B.x, C.x) / voxelSize) - offsetX, 0, sizeX);
                int minZ = Mathf.Clamp(Mathf.FloorToInt(SomeMath.Min(A.z, B.z, C.z) / voxelSize) - offsetZ, 0, sizeZ);
                int maxZ = Mathf.Clamp(Mathf.CeilToInt(SomeMath.Max(A.z, B.z, C.z) / voxelSize) - offsetZ, 0, sizeZ);

                for (int x = minX; x < maxX; x++) {
                    for (int z = minZ; z < maxZ; z++) {
                        float pointX = (x * voxelSize) + chunkPosX;
                        float pointZ = (z * voxelSize) + chunkPosZ;
                        if (SomeMath.LineSide(A.x, A.z, B.x, B.z, pointX, pointZ) <= 0.001 &
                            SomeMath.LineSide(B.x, B.z, C.x, C.z, pointX, pointZ) <= 0.001 &
                            SomeMath.LineSide(C.x, C.z, A.x, A.z, pointX, pointZ) <= 0.001) {
                            float height = SomeMath.CalculateHeight(A, B, C, pointX, pointZ);

                            compactData[GetIndex(x, z)].Update(height - 20, height, passability);
                        }
                    }
                }
            }

            return compactData;
        }

        public void AppendTerrain(Vector3[] vrts, int[] trs, Area area) {
            //rasterization preparings
            var compactData = GetTerrainCompactData(vrts, trs);
            AppendCompactData(compactData, GetAreaValue(area));
        }

        public void AppendTerrain(Vector3[] vrts, int[] trs, byte[] area) {
            //rasterization preparings
            var compactData = GetTerrainCompactData(vrts, trs);
            AppendCompactData(compactData, area);
        }
    }
}