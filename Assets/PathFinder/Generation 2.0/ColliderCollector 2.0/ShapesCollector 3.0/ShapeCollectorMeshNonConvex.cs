using K_PathFinder.Collector;
using K_PathFinder.PFDebuger;
using K_PathFinder.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Collector3 {
    public partial class ShapeCollector {
        public void AppendMeshNonConvex(ShapeDataMesh data) {
            AppendMeshNonConvex(data.verts, data.tris, data.matrix, GetAreaValue(data.area));
        }

        public void AppendMeshNonConvex(Vector3[] verts, int[] tris, Matrix4x4 matrix, byte area) {
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

                float crossY = Vector3.Cross(B - A, C - A).normalized.y;
                sbyte passability = -1;

                if (crossY > 0) {
                    bool unwalkableBySlope = !(crossY >= maxSlopeCos);
                    if (area == 1)//id of clear Area all time
                        passability = (sbyte)Passability.Unwalkable;
                    else if (unwalkableBySlope)
                        passability = (sbyte)Passability.Slope;
                    else
                        passability = (sbyte)Passability.Walkable;

#if UNITY_EDITOR
                    if (!unwalkableBySlope && Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false)
                        Debuger_K.AddWalkablePolygon(template.gridPosX, template.gridPosZ, template.properties, A, B, C);
#endif
                }

                triangleRasterizator.RasterizeTriangle(
                    A, B, C,
                    voxelSize,
                    startX_extra, endX_extra,
                    startZ_extra, endZ_extra,
                    this,
                    passability,
                    area);
            }

            GenericPool<ShapeDataHelperTriangleRasterization>.ReturnToPool(ref triangleRasterizator);
            GenericPoolArray<Vector3>.ReturnToPool(ref tempVerts);
        }
    }
}
