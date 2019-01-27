using K_PathFinder.PFDebuger;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Collector {
    public enum VolumeCollectionPassabilityOption {
        CollectAsActive,
        CollectAsPassive,
        CollectAsInverted,
        CollectAsNotPassable
    }

    ////spheres are common
    //public class ShapeDataHelperSphereCollector {
    //    //VolumeSimple volume;
    //    NavMeshTemplateCreation template;
    //    float voxelSize;

    //    Vector3 spherePos;
    //    float sphereRadius, sphereRadiusSqr;
    //    float sphereY;
    //    float maxSlopeY;

    //    float sphereLocalX;
    //    float sphereLocalZ;

    //    float heightDelta = 0f;

    //    int volumeSizeX, volumeSizeZ;

    //    delegate void SetVoxelDelegate(int x, int z, float height);
    //    SetVoxelDelegate targetDelegate; //VERY IMPORTANT THING

    //    //private ShapeDataHelperSphereCollector(Vector3 spherePos, float sphereRadius, VolumeSimple volume, NavMeshTemplateCreation template) {
    //    //    this.volume = volume;
    //    //    this.template = template;
    //    //    this.spherePos = spherePos;
    //    //    this.sphereRadius = sphereRadius;
    //    //    volumeSizeX = volume.sizeX;
    //    //    volumeSizeZ = volume.sizeZ;
    //    //}

    //    public static Bounds2DInt GetVolumeBounds(Vector3 spherePos, float sphereRadius, NavMeshTemplateCreation template) {
    //        float voxelSize = template.voxelSize;
    //        Vector3 chunkReal = template.realOffsetedPosition;
    //        Vector3 sphereLocal = spherePos - chunkReal;

    //        return new Bounds2DInt(
    //            Mathf.Max((int)((sphereLocal.x - sphereRadius) / voxelSize), 0),
    //            Mathf.Max((int)((sphereLocal.z - sphereRadius) / voxelSize), 0),
    //            Mathf.Min((int)((sphereLocal.x + sphereRadius) / voxelSize) + 1, template.lengthX_extra),
    //            Mathf.Min((int)((sphereLocal.z + sphereRadius) / voxelSize) + 1, template.lengthZ_extra));
    //    }

    //    //public static void CollectStatic(Vector3 spherePos, float sphereRadius, VolumeSimple volume, NavMeshTemplateCreation template, VolumeCollectionPassabilityOption option, float heightDelta = 0f) {
    //    //    ShapeDataHelperSphereCollector instance = new ShapeDataHelperSphereCollector(spherePos, sphereRadius, volume, template);
    //    //    instance.heightDelta = heightDelta;
    //    //    switch (option) {
    //    //        case VolumeCollectionPassabilityOption.CollectAsActive:
    //    //            if(heightDelta == 0f) 
    //    //                instance.targetDelegate = instance.SetVoxelCollectAsActive;                    
    //    //            else
    //    //                instance.targetDelegate = instance.SetVoxelCollectAsActiveWithHeightDelta;
    //    //            break;
    //    //        case VolumeCollectionPassabilityOption.CollectAsPassive:
    //    //            if (heightDelta == 0f)
    //    //                instance.targetDelegate = instance.SetVoxelCollectAsPassive;
    //    //            else
    //    //                instance.targetDelegate = instance.SetVoxelCollectAsPassiveWithHeightDelta;
    //    //            break;
    //    //        case VolumeCollectionPassabilityOption.CollectAsInverted:
    //    //            break;
    //    //        case VolumeCollectionPassabilityOption.CollectAsNotPassable:
    //    //            if (heightDelta == 0f)
    //    //                instance.targetDelegate = instance.SetVoxelCollectAsNotPassable;
    //    //            else
    //    //                instance.targetDelegate = instance.SetVoxelCollectAsNotPassableWithHeightDelta;
    //    //            break; 
    //    //    }
    //    //    instance.CollectSphere();
    //    //}

    //    //#region different delegates
    //    //private void SetVoxelCollectAsActive(int x, int z, float height) {
    //    //    volume.SetVoxel(x, z, sphereY + height, sphereY - height, height >= maxSlopeY ? (sbyte)Passability.Walkable : (sbyte)Passability.Slope);
    //    //}
    //    //private void SetVoxelCollectAsActiveWithHeightDelta(int x, int z, float height) {
    //    //    volume.SetVoxel(x, z, sphereY + height + heightDelta, sphereY - height - heightDelta, height >= maxSlopeY ? (sbyte)Passability.Walkable : (sbyte)Passability.Slope);
    //    //}

    //    //private void SetVoxelCollectAsPassive(int x, int z, float height) {
    //    //    volume.SetVoxelPassive(x, z, sphereY + height, sphereY - height, height >= maxSlopeY ? (sbyte)Passability.Walkable : (sbyte)Passability.Slope);
    //    //}
    //    //private void SetVoxelCollectAsPassiveWithHeightDelta(int x, int z, float height) {
    //    //    volume.SetVoxelPassive(x, z, sphereY + height + heightDelta, sphereY - height - heightDelta, height >= maxSlopeY ? (sbyte)Passability.Walkable : (sbyte)Passability.Slope);
    //    //}

    //    //private void SetVoxelCollectAsNotPassable(int x, int z, float height) {
    //    //    volume.SetVoxel(x, z, sphereY + height, sphereY - height);
    //    //}
    //    //private void SetVoxelCollectAsNotPassableWithHeightDelta(int x, int z, float height) {
    //    //    volume.SetVoxel(x, z, sphereY + height + heightDelta, sphereY - height - heightDelta);
    //    //}
    //    //#endregion

    //    private void CollectSphere() { 
    //        voxelSize = template.voxelSize;
    //        sphereRadiusSqr = sphereRadius * sphereRadius;

    //        maxSlopeY = Mathf.Sin(template.maxSlope * Mathf.PI / 180) * sphereRadius;

    //        Vector3 chunkReal = template.realOffsetedPosition;
    //        Vector3 sphereLocal = spherePos - chunkReal;
    //        sphereY = spherePos.y;
    //        sphereLocalX = spherePos.x - chunkReal.x;
    //        sphereLocalZ = spherePos.z - chunkReal.z;

    //        int xCenter = (int)(sphereLocalX / voxelSize);
    //        int zCenter = (int)(sphereLocalZ / voxelSize);

        
    //        float xRemain = sphereLocalX % voxelSize;
    //        float zRemain = sphereLocalZ % voxelSize;

    //        int xMin = (int)((sphereLocal.x - sphereRadius) / voxelSize);
    //        int xMax = (int)((sphereLocal.x + sphereRadius) / voxelSize) + 1;
    //        int zMin = (int)((sphereLocal.z - sphereRadius) / voxelSize);
    //        int zMax = (int)((sphereLocal.z + sphereRadius) / voxelSize) + 1;

    //        if(xCenter >= 0 && xCenter < volumeSizeX && zCenter >= 0 && zCenter < volumeSizeZ)
    //            targetDelegate(xCenter, zCenter, sphereRadius);      

    //        //volume.SetVoxel(xCenter, zCenter, sphereY + sphereRadius, sphereY - sphereRadius, (int)Passability.Walkable);

    //        //x mid
    //        for (int x = xMin; x < xCenter; x++) {
    //            SetVoxel(x, zCenter, voxelSize, zRemain);
    //            for (int z = zMin; z < zCenter; z++) {
    //                SetVoxel(x, z, voxelSize, voxelSize);
    //            }

    //            for (int z = zCenter + 1; z < zMax; z++) {
    //                SetVoxel(x, z, voxelSize, 0);
    //            }
    //        }

    //        for (int x = xCenter + 1; x < xMax; x++) {
    //            SetVoxel(x, zCenter, 0, zRemain);

    //            for (int z = zMin; z < zCenter; z++) {
    //                SetVoxel(x, z, 0, voxelSize);
    //            }

    //            for (int z = zCenter + 1; z < zMax; z++) {
    //                SetVoxel(x, z, 0, 0);
    //            }
    //        }

    //        //z mid
    //        for (int z = zMin; z < zCenter; z++) {
    //            SetVoxel(xCenter, z, xRemain, voxelSize);
    //        }

    //        for (int z = zCenter + 1; z < zMax; z++) {
    //            SetVoxel(xCenter, z, xRemain, 0);
    //        }
    //    }

    //    private void SetVoxel(int x, int z, float offsetX, float offsetZ) {
    //        if (x >= 0 && x < volumeSizeX && z >= 0 && z < volumeSizeZ) {
    //            float xPos = (x * voxelSize) + offsetX;
    //            float zPos = (z * voxelSize) + offsetZ;
    //            float distSqr = SomeMath.SqrDistance(xPos, zPos, sphereLocalX, sphereLocalZ);

    //            if (distSqr < sphereRadiusSqr) {
    //                float height = Mathf.Sqrt(sphereRadiusSqr - distSqr);
    //                targetDelegate(x, z, height);
    //            }
    //        }
    //    }
        
    //    private void DrawGrid(int x, float y, int z, NavMeshTemplateCreation template, Color color) {
    //        float voxelSize = template.voxelSize;
    //        Vector3 pos1 = new Vector3(x * voxelSize, y, z * voxelSize) + template.realOffsetedPosition;
    //        Vector3 pos2 = new Vector3(pos1.x + voxelSize, y, pos1.z);
    //        Vector3 pos3 = new Vector3(pos1.x, y, pos1.z + voxelSize);
    //        Vector3 pos4 = new Vector3(pos1.x + voxelSize, y, pos1.z + voxelSize);

    //        Debuger_K.AddLine(color, 0, 0.001f, true, pos1, pos2, pos4, pos3);
    //    }

    //    static Vector3[] DrawCircle(int value, Vector3 position, float radius) {
    //        Vector3[] result = new Vector3[value];
    //        for (int i = 0; i < value; ++i) {
    //            result[i] = new Vector3(
    //                Mathf.Cos(i * 2.0f * Mathf.PI / value) * radius + position.x,
    //                position.y,
    //                Mathf.Sin(i * 2.0f * Mathf.PI / value) * radius + position.z);
    //        }
    //        return result;
    //    }
    //}

    public class ShapeDataHelperTriangleRasterization {
        Vector3[] _polyListXTemp = new Vector3[16];
        Vector3[] _polyListXFinal = new Vector3[16];
        Vector3[] _polyListZTemp = new Vector3[16];
        Vector3[] _polyListZFinal = new Vector3[16];
        float[] _polyDistances = new float[16];

        //public void RasterizeTriangle(VolumeSimple volumeSimple, Vector3 A, Vector3 B, Vector3 C, float voxelSize, int startX, int endX, int startZ, int endZ, Passability passability) {
        //    int minX = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Min(A.x, B.x, C.x) / voxelSize) - 1, startX, endX);
        //    int minZ = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Min(A.z, B.z, C.z) / voxelSize) - 1, startZ, endZ);
        //    int maxX = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Max(A.x, B.x, C.x) / voxelSize) + 1, startX, endX);
        //    int maxZ = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Max(A.z, B.z, C.z) / voxelSize) + 1, startZ, endZ);

        //    if (minX == maxX || minZ == maxZ)
        //        return; //if too small return

        //    Vector3[] vectorsStart = new Vector3[3] { A, B, C };

        //    for (int x = minX; x < maxX; ++x) {
        //        int vertsInLength1 = ClipPolyToPlane(1f, 0.0f, -x * voxelSize, vectorsStart, 3, ref _polyListXTemp);

        //        if (vertsInLength1 >= 3) {
        //            int vertsInLength2 = ClipPolyToPlane(-1f, 0.0f, (x + 1) * voxelSize, _polyListXTemp, vertsInLength1, ref _polyListXFinal);

        //            if (vertsInLength2 >= 3) {
        //                for (int z = minZ; z < maxZ; ++z) {
        //                    int vertsInLength3 = ClipPolyToPlane(0.0f, 1f, -z * voxelSize, _polyListXFinal, vertsInLength2, ref _polyListZTemp);

        //                    if (vertsInLength3 >= 3) {
        //                        int vertsInLength4 = ClipPolyToPlane(0.0f, -1f, (z + 1) * voxelSize, _polyListZTemp, vertsInLength3, ref _polyListZFinal);
        //                        if (vertsInLength4 >= 3) {
        //                            float min = _polyListZFinal[0].y;
        //                            float max = _polyListZFinal[0].y;

        //                            for (int index = 1; index < vertsInLength4; ++index) {
        //                                min = Mathf.Min(min, _polyListZFinal[index].y);
        //                                max = Mathf.Max(max, _polyListZFinal[index].y);
        //                            }

        //                            int indexX = Mathf.Abs(x - startX);
        //                            int indexZ = Mathf.Abs(z - startZ);

        //                            volumeSimple.SetVoxel(indexX, indexZ, max, min, (sbyte)passability);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //public void RasterizeTriangle(Vector3 A, Vector3 B, Vector3 C, float voxelSize, int startX, int endX, int startZ, int endZ, VolumeLinked volume, sbyte pass, int area) {
        //    int minX = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Min(A.x, B.x, C.x) / voxelSize) - 1, startX, endX);
        //    int minZ = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Min(A.z, B.z, C.z) / voxelSize) - 1, startZ, endZ);
        //    int maxX = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Max(A.x, B.x, C.x) / voxelSize) + 1, startX, endX);
        //    int maxZ = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Max(A.z, B.z, C.z) / voxelSize) + 1, startZ, endZ);

        //    if (minX == maxX || minZ == maxZ)
        //        return; //if too small return

        //    Vector3[] vectorsStart = new Vector3[3] { A, B, C };

        //    for (int x = minX; x < maxX; ++x) {
        //        int vertsInLength1 = ClipPolyToPlane(1f, 0.0f, -x * voxelSize, vectorsStart, 3, ref _polyListXTemp);

        //        if (vertsInLength1 >= 3) {
        //            int vertsInLength2 = ClipPolyToPlane(-1f, 0.0f, (x + 1) * voxelSize, _polyListXTemp, vertsInLength1, ref _polyListXFinal);

        //            if (vertsInLength2 >= 3) {
        //                for (int z = minZ; z < maxZ; ++z) {
        //                    int vertsInLength3 = ClipPolyToPlane(0.0f, 1f, -z * voxelSize, _polyListXFinal, vertsInLength2, ref _polyListZTemp);

        //                    if (vertsInLength3 >= 3) {
        //                        int vertsInLength4 = ClipPolyToPlane(0.0f, -1f, (z + 1) * voxelSize, _polyListZTemp, vertsInLength3, ref _polyListZFinal);
        //                        if (vertsInLength4 >= 3) {
        //                            float min = _polyListZFinal[0].y;
        //                            float max = _polyListZFinal[0].y;

        //                            for (int index = 1; index < vertsInLength4; ++index) {
        //                                min = Mathf.Min(min, _polyListZFinal[index].y);
        //                                max = Mathf.Max(max, _polyListZFinal[index].y);
        //                            }

        //                            int indexX = Mathf.Abs(x - startX);
        //                            int indexZ = Mathf.Abs(z - startZ);

        //                            volume.SetVoxel(indexX, indexZ, min, max, pass, area);
                                    
        //                            //volumeBizzare.AddData(indexX, indexZ, (int)((max + (fragmentSize * 0.5f)) / fragmentSize), passability);
        //                            //volumeBizzare.AddData(indexX, indexZ, (int)((min + (fragmentSize * 0.5f)) / fragmentSize), passability);

        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        public void RasterizeTriangle(Vector3 A, Vector3 B, Vector3 C, float voxelSize, int startX, int endX, int startZ, int endZ, Collector3.ShapeCollector.DataCompact[] volume, sbyte pass, int sizeX) {
            int minX = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Min(A.x, B.x, C.x) / voxelSize) - 1, startX, endX);
            int minZ = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Min(A.z, B.z, C.z) / voxelSize) - 1, startZ, endZ);
            int maxX = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Max(A.x, B.x, C.x) / voxelSize) + 1, startX, endX);
            int maxZ = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Max(A.z, B.z, C.z) / voxelSize) + 1, startZ, endZ);

            if (minX == maxX || minZ == maxZ)
                return; //if too small return

            Vector3[] vectorsStart = new Vector3[3] { A, B, C };

            for (int x = minX; x < maxX; ++x) {
                int vertsInLength1 = ClipPolyToPlane(1f, 0.0f, -x * voxelSize, vectorsStart, 3, ref _polyListXTemp);

                if (vertsInLength1 >= 3) {
                    int vertsInLength2 = ClipPolyToPlane(-1f, 0.0f, (x + 1) * voxelSize, _polyListXTemp, vertsInLength1, ref _polyListXFinal);

                    if (vertsInLength2 >= 3) {
                        for (int z = minZ; z < maxZ; ++z) {
                            int vertsInLength3 = ClipPolyToPlane(0.0f, 1f, -z * voxelSize, _polyListXFinal, vertsInLength2, ref _polyListZTemp);

                            if (vertsInLength3 >= 3) {
                                int vertsInLength4 = ClipPolyToPlane(0.0f, -1f, (z + 1) * voxelSize, _polyListZTemp, vertsInLength3, ref _polyListZFinal);
                                if (vertsInLength4 >= 3) {
                                    float min = _polyListZFinal[0].y;
                                    float max = _polyListZFinal[0].y;

                                    for (int index = 1; index < vertsInLength4; ++index) {
                                        min = Mathf.Min(min, _polyListZFinal[index].y);
                                        max = Mathf.Max(max, _polyListZFinal[index].y);
                                    }

                                    int indexX = Mathf.Abs(x - startX);
                                    int indexZ = Mathf.Abs(z - startZ);

                                    volume[(indexZ * sizeX) + indexX].Update(min, max, pass);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        public void RasterizeTriangle(Vector3 A, Vector3 B, Vector3 C, float voxelSize, int startX, int endX, int startZ, int endZ, Collector3.ShapeCollector volume, sbyte pass, byte area) {
            int minX = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Min(A.x, B.x, C.x) / voxelSize) - 1, startX, endX);
            int minZ = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Min(A.z, B.z, C.z) / voxelSize) - 1, startZ, endZ);
            int maxX = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Max(A.x, B.x, C.x) / voxelSize) + 1, startX, endX);
            int maxZ = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Max(A.z, B.z, C.z) / voxelSize) + 1, startZ, endZ);

            if (minX == maxX || minZ == maxZ)
                return; //if too small return

            Vector3[] vectorsStart = new Vector3[3] { A, B, C };

            for (int x = minX; x < maxX; ++x) {
                int vertsInLength1 = ClipPolyToPlane(1f, 0.0f, -x * voxelSize, vectorsStart, 3, ref _polyListXTemp);

                if (vertsInLength1 >= 3) {
                    int vertsInLength2 = ClipPolyToPlane(-1f, 0.0f, (x + 1) * voxelSize, _polyListXTemp, vertsInLength1, ref _polyListXFinal);

                    if (vertsInLength2 >= 3) {
                        for (int z = minZ; z < maxZ; ++z) {
                            int vertsInLength3 = ClipPolyToPlane(0.0f, 1f, -z * voxelSize, _polyListXFinal, vertsInLength2, ref _polyListZTemp);

                            if (vertsInLength3 >= 3) {
                                int vertsInLength4 = ClipPolyToPlane(0.0f, -1f, (z + 1) * voxelSize, _polyListZTemp, vertsInLength3, ref _polyListZFinal);
                                if (vertsInLength4 >= 3) {
                                    float min = _polyListZFinal[0].y;
                                    float max = _polyListZFinal[0].y;

                                    for (int index = 1; index < vertsInLength4; ++index) {
                                        min = Mathf.Min(min, _polyListZFinal[index].y);
                                        max = Mathf.Max(max, _polyListZFinal[index].y);
                                    }

                                    volume.SetVoxel(
                                        Mathf.Abs(x - startX),
                                        Mathf.Abs(z - startZ),
                                        min, max, pass, area);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private int ClipPolyToPlane(float aPlaneNormalX, float aPlaneNormalZ, float aPlaneNormalDistance, Vector3[] aVerticesIn, int aVerticesInLength, ref Vector3[] aVerticesOut) {
            if (_polyDistances.Length < aVerticesInLength)
                Array.Resize(ref _polyDistances, aVerticesInLength * 2);

            for (int index = 0; index < aVerticesInLength; ++index)
                _polyDistances[index] = (aPlaneNormalX * aVerticesIn[index].x + aPlaneNormalZ * aVerticesIn[index].z) + aPlaneNormalDistance;

            if (aVerticesOut.Length < aVerticesInLength * 2)
                Array.Resize(ref aVerticesOut, aVerticesInLength * 2);

            int num = 0;

            int index2 = aVerticesInLength - 1;
            for (int index1 = 0; index1 < aVerticesInLength; ++index1) {
                bool flag1 = _polyDistances[index2] >= 0f;
                bool flag2 = _polyDistances[index1] >= 0f;
                if (flag1 != flag2)
                    aVerticesOut[num++] = aVerticesIn[index2] + ((aVerticesIn[index1] - aVerticesIn[index2]) * (_polyDistances[index2] / (_polyDistances[index2] - _polyDistances[index1])));
                if (flag2)
                    aVerticesOut[num++] = aVerticesIn[index1];
                index2 = index1;
            }
            return num;
        }

        public bool CalculateWalk(Vector3 A, Vector3 B, Vector3 C, float aMaxSlopeCos, bool flipY = false) {
            if (flipY)
                return (Vector3.Cross(B - A, C - A).normalized.y * -1) >= aMaxSlopeCos;
            else
                return Vector3.Cross(B - A, C - A).normalized.y >= aMaxSlopeCos;
        }
    }
}