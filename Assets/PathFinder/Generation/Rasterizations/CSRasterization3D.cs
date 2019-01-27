using K_PathFinder;
using K_PathFinder.PFDebuger;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace K_PathFinder.Rasterization {
    //class that stores compute shader rasterization thing and dont mix it with other stuff
    //TODO: rasterization actualy could do flat surfaces of any kind. make here some primitives that are made from quads or so
    //TODO: Mupltiple inputs?
    public class CSRasterization3D {
        const int cellSizeX = 8;
        const int cellSizeY = 8;
        const int cellSizeZ = 8;

        ComputeShader CS;     

        public CSRasterization3D(ComputeShader shader) {
            CS = shader;
        }


        public CSRasterization3DResult Rasterize(Vector3[] verts, int[] tris, Bounds bounds, Matrix4x4 matrix, int volumeSizeX, int volumeSizeZ, float chunkPosX, float chunkPosZ, float voxelSize, float maxSlopeCos, bool flipY, bool debug) {
            float cellSizeLengthX = cellSizeX * voxelSize;
            float cellSizeLengthY = cellSizeY * voxelSize;

            int sheetSizeX = Mathf.CeilToInt((float)volumeSizeX / cellSizeX);
            int sheetSizeZ = Mathf.CeilToInt((float)volumeSizeZ / cellSizeY);

            //int tSizeX = Mathf.Max(sheetSizeX, 1) * cellSizeX;
            //int tSizeY = Mathf.Max(sheetSizeZ, 1) * cellSizeY;
    
            Vector3 minBounds = bounds.min;
            Vector3 maxBounds = bounds.max;

            int startX = Mathf.Clamp(Mathf.FloorToInt((minBounds.x - chunkPosX) / cellSizeLengthX), 0, sheetSizeX);
            int startZ = Mathf.Clamp(Mathf.FloorToInt((minBounds.z - chunkPosZ) / cellSizeLengthY), 0, sheetSizeZ);

            int endX = Mathf.Clamp(Mathf.CeilToInt((maxBounds.x - chunkPosX) / cellSizeLengthX), 1, sheetSizeX);
            int endZ = Mathf.Clamp(Mathf.CeilToInt((maxBounds.z - chunkPosZ) / cellSizeLengthY), 1, sheetSizeZ);

            int sizeX = endX - startX;
            int sizeZ = endZ - startZ;

            int voxelsX = sizeX * cellSizeX;
            int voxelsY = sizeZ * cellSizeY;
            int voxelsTotal = voxelsX * voxelsY;

            if(voxelsTotal == 0) 
                return null;                       

            float ajustedChunkX = (startX * cellSizeLengthX) + chunkPosX;
            float ajustedChunkZ = (startZ * cellSizeLengthY) + chunkPosZ;
            Vector3 ajustedChunk = new Vector3(ajustedChunkX, 0, ajustedChunkZ);

            #region debug of sheet
            //Debuger_K.AddRay(new Vector3(ajustedChunkX, 0, ajustedChunkZ), Vector3.up, Color.red);

            //Vector3 chunkPos = new Vector3(chunkPosX, 0, chunkPosZ);
            //Debuger_K.AddQuad(
            //    chunkPos,
            //    chunkPos + (Vector3.right * tSizeX * voxelSize),
            //    chunkPos + (Vector3.forward * tSizeY * voxelSize),
            //    chunkPos + (Vector3.right * tSizeX * voxelSize) + (Vector3.forward * tSizeY * voxelSize),
            //    new Color(0, 0, 1, 0.1f));


            //Debuger_K.AddQuad(
            //    chunkPos,
            //    chunkPos + (Vector3.right * volume.sizeX * voxelSize),
            //    chunkPos + (Vector3.forward * volume.sizeZ * voxelSize),
            //    chunkPos + (Vector3.right * volume.sizeX * voxelSize) + (Vector3.forward * volume.sizeZ * voxelSize),
            //    new Color(0, 1, 1, 0.1f));

            //for (int x = startX; x < startX + sizeX; x++) {
            //    for (int z = startZ; z < startZ + sizeZ; z++) {
            //        Debuger_K.AddQuad(
            //            chunkPos + new Vector3(cellSizeLengthX * x, 0, cellSizeLengthY * z),
            //            chunkPos + new Vector3(cellSizeLengthX * (x + 1), 0, cellSizeLengthY * z),
            //            chunkPos + new Vector3(cellSizeLengthX * x, 0, cellSizeLengthY * (z + 1)),
            //            chunkPos + new Vector3(cellSizeLengthX * (x + 1), 0, cellSizeLengthY * (z + 1)),
            //            new Color(0, 1, 0, 0.1f));
            //    }
            //}

            //Debug.Log(sizeX + " : " + sizeZ);
            #endregion
            
            ComputeBuffer vertsBuffer = new ComputeBuffer(verts.Length, sizeof(float) * 3);
            ComputeBuffer trisBuffer = new ComputeBuffer(tris.Length, sizeof(int));
            ComputeBuffer voxelBuffer = new ComputeBuffer(voxelsTotal, Voxel3D.stride);          
            ComputeBuffer dataSegmentBuffer = new ComputeBuffer(cellSizeZ, DataSegment3D.stride);

            CS.SetInt("SizeX", voxelsX);
            CS.SetInt("SizeZ", voxelsY);
            CS.SetVector("ChunkPos", ajustedChunk);
            CS.SetFloat("VoxelSize", voxelSize);
            
            int kernel = CS.FindKernel("Rasterize");
            CS.SetBuffer(kernel, "CurTris", trisBuffer);
            CS.SetBuffer(kernel, "CurVerts", vertsBuffer);
            CS.SetBuffer(kernel, "Result", voxelBuffer);
            CS.SetBuffer(kernel, "TargetSegments", dataSegmentBuffer);

            //creating new array in case referensed are reused elsewhere
            Vector3[] newVerts = new Vector3[verts.Length];

            for (int i = 0; i < verts.Length; i++) {
                newVerts[i] = matrix.MultiplyPoint3x4(verts[i]);
            }
            DataSegment3D[] dataSegmentArray = new DataSegment3D[cellSizeZ];

            vertsBuffer.SetData(newVerts);
            trisBuffer.SetData(tris);            

            Voxel3D[] voxels3D = new Voxel3D[voxelsTotal];


            for (int i = 0; i < voxelsTotal; i++) {
                voxels3D[i].passability = -1;
            }
            
            voxelBuffer.SetData(voxels3D);

            int fullDataArrays = tris.Length / (3 * cellSizeZ);   

            for (int fp_index = 0; fp_index < fullDataArrays; fp_index++) {
                int t = fp_index * cellSizeZ * 3;

                for (int index = 0; index < cellSizeZ; index++) {
                    int tIndex = t + (index * 3);
                    int passability = CalculateWalk(newVerts[tris[tIndex]], newVerts[tris[tIndex + 1]], newVerts[tris[tIndex + 2]], maxSlopeCos, flipY) ? 3 : 1;//if true then walkable else slope;
                    dataSegmentArray[index] = new DataSegment3D(tIndex, 3, passability);
                    //Debuger_K.AddTriangle(newVerts[tris[tIndex]], newVerts[tris[tIndex + 1]], newVerts[tris[tIndex + 2]], new Color(1, 0, 1, 0.1f));
                }
                dataSegmentBuffer.SetData(dataSegmentArray);
                CS.Dispatch(kernel, sizeX, sizeZ, 1);
            }

            int endIndex = fullDataArrays * cellSizeZ * 3;
            int remainTriangles = tris.Length - endIndex;
            if (remainTriangles > 0) {
                int remainSize = remainTriangles / 3;
                int passabilityDefault = CalculateWalk(newVerts[tris[0]], newVerts[tris[1]], newVerts[tris[2]], maxSlopeCos) ? 3 : 1;//if true then walkable else slope;
                DataSegment3D defaulSegment = new DataSegment3D(0, 3, passabilityDefault);

                for (int index = 0; index < cellSizeZ; index++) {
                    if (index < remainSize) {
                        int tIndex = endIndex + (index * 3);
                        int passability = CalculateWalk(newVerts[tris[tIndex]], newVerts[tris[tIndex + 1]], newVerts[tris[tIndex + 2]], maxSlopeCos) ? 3 : 1;//if true then walkable else slope;
                        dataSegmentArray[index] = new DataSegment3D(tIndex, 3, passability);
                        //Debuger_K.AddTriangle(newVerts[tris[tIndex]], newVerts[tris[tIndex + 1]], newVerts[tris[tIndex + 2]], new Color(1, 0, 1, 0.1f));
                    }
                    else {
                        dataSegmentArray[index] = defaulSegment; //set here first
                    }
           
                }
                dataSegmentBuffer.SetData(dataSegmentArray);
                CS.Dispatch(kernel, sizeX, sizeZ, 1);
            }

            int newVolumeStartX = startX * cellSizeX;
            int newVolumeStartZ = startZ * cellSizeY;
            int newVolumeSizeX = Mathf.Min(voxelsX, volumeSizeX - newVolumeStartX);
            int newVolumeSizeZ = Mathf.Min(voxelsY, volumeSizeZ - newVolumeStartZ);

            voxelBuffer.GetData(voxels3D);

            vertsBuffer.Dispose();
            trisBuffer.Dispose();
            voxelBuffer.Dispose();
            dataSegmentBuffer.Dispose();

            return new CSRasterization3DResult(voxels3D, voxelsX, voxelsY, newVolumeStartX, newVolumeStartZ, newVolumeSizeX, newVolumeSizeZ);
        }

        //for testing cause it's just taking mesh data not collider data
        public CSRasterization3DResult Rasterize(Collider collider, Matrix4x4 matrix, int volumeSizeX, int volumeSizeZ, float chunkPosX, float chunkPosZ, float voxelSize, float maxSlopeCos, bool flipY, bool debug) {
            Mesh mesh = collider.gameObject.GetComponent<MeshFilter>().sharedMesh; //TEMP
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;

            return Rasterize(verts, tris, collider.bounds, matrix, volumeSizeX, volumeSizeZ, chunkPosX, chunkPosZ, voxelSize, maxSlopeCos, flipY, debug);
        }

        //static bool CalculateWalk(Vector3 A, Vector3 B, Vector3 C, float aMaxSlopeCos) {
        //    return Vector3.Cross(B - A, C - A).normalized.y >= aMaxSlopeCos;
        //}


        static bool CalculateWalk(Vector3 A, Vector3 B, Vector3 C, float aMaxSlopeCos, bool flipY = false) {
            if (flipY)
                return (Vector3.Cross(B - A, C - A).normalized.y * -1) >= aMaxSlopeCos;
            else
                return Vector3.Cross(B - A, C - A).normalized.y >= aMaxSlopeCos;
        }
    }
}
