using K_PathFinder;

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.Rasterization {
    public class CSRasterization2D {
        const int cellSizeX = 128;
        const int cellSizeY = 1;
        const int cellSizeZ = 1;

        ComputeShader CS;
        public CSRasterization2D(ComputeShader shader) {
            CS = shader;
        }

        public CSRasterization2DResult Rasterize(Vector3[] verts, int[] tris, int sizeX, int sizeZ, float chunkPosX, float chunkPosZ, float voxelSize, float maxSlopeCos, bool debug = false) {
            if (tris.Length == 0)
                return null;

            int dsLength = tris.Length / 3;
            int dsTargetLength = Mathf.Max(Mathf.CeilToInt((float)dsLength / (float)cellSizeX), 1) * cellSizeX;

            DataSegment2D[] ds = new DataSegment2D[dsTargetLength];
            int sizeTotal = sizeX * sizeZ;

            int offsetX = Mathf.RoundToInt(chunkPosX / voxelSize);
            int offsetZ = Mathf.RoundToInt(chunkPosZ / voxelSize);

            for (int i = 0; i < tris.Length; i += 3) {
                Vector3 A = verts[tris[i]];
                Vector3 B = verts[tris[i + 1]];
                Vector3 C = verts[tris[i + 2]];

                //making data segments
                //data segment represent:
                //triangle index
                //expected passablity based on inclanation
                //min and max indexes in grid to check it presence
                ds[i / 3] = 
                    new DataSegment2D(
                        i,
                        (CalculateWalk(A, B, C, maxSlopeCos) ? 3 : 1),//if true then walkable else slope;
                        Mathf.Clamp(Mathf.FloorToInt(SomeMath.Min(A.x, B.x, C.x) / voxelSize) - offsetX, 0, sizeX),//minX
                        Mathf.Clamp(Mathf.CeilToInt(SomeMath.Max(A.x, B.x, C.x) / voxelSize) - offsetX, 0, sizeX), //maxX
                        Mathf.Clamp(Mathf.FloorToInt(SomeMath.Min(A.z, B.z, C.z) / voxelSize) - offsetZ, 0, sizeZ),//minZ
                        Mathf.Clamp(Mathf.CeilToInt(SomeMath.Max(A.z, B.z, C.z) / voxelSize) - offsetZ, 0, sizeZ)  //maxZ
                     );
            }

            int kernel = CS.FindKernel("Rasterize");

            ComputeBuffer vertsBuffer = new ComputeBuffer(verts.Length, sizeof(float) * 3);
            ComputeBuffer trisBuffer = new ComputeBuffer(tris.Length, sizeof(int));
            ComputeBuffer voxelBuffer = new ComputeBuffer(sizeTotal, Voxel2D.stride);
            ComputeBuffer dataSegmentBuffer = new ComputeBuffer(dsTargetLength, DataSegment2D.stride);

            CS.SetInt("SizeX", sizeX);
            CS.SetInt("SizeZ", sizeZ);
            CS.SetVector("ChunkPos", new Vector4(chunkPosX, 0, chunkPosZ, 0));
            CS.SetFloat("VoxelSize", voxelSize);

            CS.SetBuffer(kernel, "CurTris", trisBuffer);
            CS.SetBuffer(kernel, "CurVerts", vertsBuffer);
            CS.SetBuffer(kernel, "Result", voxelBuffer);
            CS.SetBuffer(kernel, "TargetSegments", dataSegmentBuffer);

            vertsBuffer.SetData(verts);
            trisBuffer.SetData(tris);
            Voxel2D[] voxels = new Voxel2D[sizeTotal];

            for (int i = 0; i < sizeTotal; i++) {
                voxels[i].passability = -1;
            }


            voxelBuffer.SetData(voxels);
            dataSegmentBuffer.SetData(ds);
            CS.Dispatch(kernel, dsTargetLength / cellSizeX, 1, 1);   
            voxelBuffer.GetData(voxels);

            CSRasterization2DResult result = new CSRasterization2DResult(voxels);


            //debug
#if UNITY_EDITOR
            if (debug) {
                Debuger_K.AddMesh(verts, tris, new Color(1, 0, 1, 0.1f));

                //implementation of things goint on in compute shader

                foreach (var dataSet in ds) {
                    Vector3 A = verts[tris[dataSet.index]];
                    Vector3 B = verts[tris[dataSet.index + 1]];
                    Vector3 C = verts[tris[dataSet.index + 2]];

                    for (int x = dataSet.minX; x < dataSet.maxX; x++) {
                        for (int z = dataSet.minZ; z < dataSet.maxZ; z++) {
                            float pointX = (x * voxelSize) + chunkPosX;
                            float pointZ = (z * voxelSize) + chunkPosZ;


                            //if (SomeMath.LineSide(B.x, B.z, A.x, A.z, pointX, pointZ) <= 0.001 &
                            //    SomeMath.LineSide(A.x, A.z, C.x, C.z, pointX, pointZ) <= 0.001 &
                            //    SomeMath.LineSide(C.x, C.z, B.x, B.z, pointX, pointZ) <= 0.001) {

                            //    float height = SomeMath.CalculateHeight(A, B, C, pointX, pointZ);                      
                            //    Debuger_K.AddDot(new Vector3((x * voxelSize) + chunkPosX, height, (z * voxelSize) + chunkPosZ));

                            //    Debuger_K.AddLine(A, new Vector3((x * voxelSize) + chunkPosX, 0, (z * voxelSize) + chunkPosZ), Color.green);
                            //    Debuger_K.AddLine(B, new Vector3((x * voxelSize) + chunkPosX, 0, (z * voxelSize) + chunkPosZ), Color.green);
                            //    Debuger_K.AddLine(C, new Vector3((x * voxelSize) + chunkPosX, 0, (z * voxelSize) + chunkPosZ), Color.green);
                            //}
                            //else {
                            //    Debuger_K.AddLine(A, new Vector3((x * voxelSize) + chunkPosX, 0, (z * voxelSize) + chunkPosZ), Color.red);
                            //    Debuger_K.AddLine(B, new Vector3((x * voxelSize) + chunkPosX, 0, (z * voxelSize) + chunkPosZ), Color.red);
                            //    Debuger_K.AddLine(C, new Vector3((x * voxelSize) + chunkPosX, 0, (z * voxelSize) + chunkPosZ), Color.red);
                            //}


                            if (SomeMath.PointInTriangleSimple(A, B, C, pointX, pointZ)) {
                                float height = SomeMath.CalculateHeight(A, B, C, pointX, pointZ);
                                Debuger_K.AddDot(new Vector3((x * voxelSize) + chunkPosX, height, (z * voxelSize) + chunkPosZ));

                                //Debuger_K.AddLine(A, new Vector3((x * voxelSize) + chunkPosX, 0, (z * voxelSize) + chunkPosZ), Color.green);
                                //Debuger_K.AddLine(B, new Vector3((x * voxelSize) + chunkPosX, 0, (z * voxelSize) + chunkPosZ), Color.green);
                                //Debuger_K.AddLine(C, new Vector3((x * voxelSize) + chunkPosX, 0, (z * voxelSize) + chunkPosZ), Color.green);
                            }
                            else {
                                //Debuger_K.AddLine(A, new Vector3((x * voxelSize) + chunkPosX, 0, (z * voxelSize) + chunkPosZ), Color.red);
                                //Debuger_K.AddLine(B, new Vector3((x * voxelSize) + chunkPosX, 0, (z * voxelSize) + chunkPosZ), Color.red);
                                //Debuger_K.AddLine(C, new Vector3((x * voxelSize) + chunkPosX, 0, (z * voxelSize) + chunkPosZ), Color.red);
                            }                           
                        }
                    }
                }       

                //for (int x = 0; x < sizeX; x++) {
                //    for (int z = 0; z < sizeZ; z++) {
                //        var curVoxel = voxels[x + (z * sizeX)];
                //        if (curVoxel.exist) {
                //            Debuger_K.AddDot(new Vector3((x * voxelSize) + chunkPosX, curVoxel.height, (z * voxelSize) + chunkPosZ));
                //        }
                //    }
                //}
            }
#endif

            vertsBuffer.Dispose();
            trisBuffer.Dispose();
            voxelBuffer.Dispose();
            dataSegmentBuffer.Dispose();
            return result;
        }

        static bool CalculateWalk(Vector3 A, Vector3 B, Vector3 C, float aMaxSlopeCos) {
            return Vector3.Cross(B - A, C - A).normalized.y >= aMaxSlopeCos;
        }
    }
}