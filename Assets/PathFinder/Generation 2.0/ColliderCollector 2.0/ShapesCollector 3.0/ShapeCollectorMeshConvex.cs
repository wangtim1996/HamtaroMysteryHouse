using K_PathFinder.Collector;
using K_PathFinder.PFDebuger;
using K_PathFinder.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Collector3 {
    public partial class ShapeCollector {
        public void AppendMeshConvex(ShapeDataMesh data) {
            AppendMeshConvex(data.verts, data.tris, data.matrix, GetAreaValue(data.area), data.infoMode != ColliderInfoMode.Solid, data.bounds);
        }
        public void AppendMeshConvex(Vector3[] verts, int[] tris, Matrix4x4 matrix, byte area, bool FLIP_Y, Bounds bounds) {
            Vector3 chunkBoundsMin = template.chunkOffsetedBounds.min;

            Vector3 meshBoundsLocalMin = bounds.min - chunkBoundsMin;
            Vector3 meshBoundsLocalMax = bounds.max - chunkBoundsMin;
            float voxelSize = template.voxelSize;
        
            int gridMinX = (int)(meshBoundsLocalMin.x / voxelSize);
            int gridMinZ = (int)(meshBoundsLocalMin.z / voxelSize);
            int gridMaxX = (int)(meshBoundsLocalMax.x / voxelSize) + 2;
            int gridMaxZ = (int)(meshBoundsLocalMax.z / voxelSize) + 2;

            if (gridMinX < 0) gridMinX = 0;
            if (gridMinZ < 0) gridMinZ = 0;
            if (gridMaxX > sizeX) gridMaxX = sizeX;
            if (gridMaxZ > sizeZ) gridMaxZ = sizeZ;

            DataCompact[] compactData = TakeCompactData();
            AppendMeshConvexPrivate(compactData, verts, tris, matrix, area, FLIP_Y);
            AppendCompactData(compactData, area, gridMinX, gridMaxX, gridMinZ, gridMaxZ);
            GenericPoolArray<DataCompact>.ReturnToPool(ref compactData);
        }


        public void AppendMeshConvexPrivate(DataCompact[] compactData, Vector3[] verts, int[] tris, Matrix4x4 matrix, byte area, bool FLIP_Y) {
            float voxelSize = template.voxelSize;
            int startX_extra = template.startX_extra;
            int endX_extra = template.endX_extra;
            int startZ_extra = template.startZ_extra;
            int endZ_extra = template.endZ_extra;
           
            ShapeDataHelperTriangleRasterization triangleRasterizator = GenericPool<ShapeDataHelperTriangleRasterization>.Take();
            Vector3[] tempVerts = GenericPoolArray<Vector3>.Take(verts.Length);
            for (int i = 0; i < verts.Length; i++) {
                tempVerts[i] = matrix.MultiplyPoint3x4(verts[i]);
            }
  
            //rasterization
            float maxSlopeCos = Mathf.Cos(template.maxSlope * Mathf.PI / 180f);
            for (int t = 0; t < tris.Length; t += 3) {
                Vector3 A = tempVerts[tris[t]];
                Vector3 B = tempVerts[tris[t + 1]];
                Vector3 C = tempVerts[tris[t + 2]];
                
                sbyte passability = -1;
                if (area == 1)//id of clear Area all time
                    passability = (sbyte)Passability.Unwalkable;
                else if (CalculateWalk(A, B, C, maxSlopeCos, FLIP_Y))
                    passability = (sbyte)Passability.Walkable;
                else
                    passability = (sbyte)Passability.Slope;



                //float crossY = Vector3.Cross(B - A, C - A).normalized.y;
                //if (FLIP_Y)
                //    crossY *= -1;
                //                if (crossY > 0) {
                //                    bool unwalkableBySlope = !(crossY >= maxSlopeCos);
                //                    if (area == 1)//id of clear Area all time
                //                        passability = (sbyte)Passability.Unwalkable;
                //                    else if (unwalkableBySlope)
                //                        passability = (sbyte)Passability.Slope;
                //                    else
                //                        passability = (sbyte)Passability.Walkable;

                //#if UNITY_EDITOR
                //                    if (!unwalkableBySlope && Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false)
                //                        Debuger_K.AddWalkablePolygon(template.gridPosX, template.gridPosZ, template.properties, A, B, C);
                //#endif
                //                }

                triangleRasterizator.RasterizeTriangle(
                    A, B, C,
                    voxelSize,
                    startX_extra, endX_extra,
                    startZ_extra, endZ_extra,
                    compactData,
                    passability,
                    sizeX);
            }

            GenericPool<ShapeDataHelperTriangleRasterization>.ReturnToPool(ref triangleRasterizator);
            GenericPoolArray<Vector3>.ReturnToPool(ref tempVerts);
        }



    }
}
