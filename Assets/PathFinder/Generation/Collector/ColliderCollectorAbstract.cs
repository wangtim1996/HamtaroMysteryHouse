using System;
using System.Collections.Generic;
using UnityEngine;
using K_PathFinder;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

//namespace K_PathFinder {
//    public abstract class ColliderCollectorTerrainAbstract {
//        public NavmeshProfiler profiler = null;
//        protected NavMeshTemplateCreation template;
//        //protected List<Collider> validColliders = new List<Collider>();

//        protected ColliderCollectorTerrainAbstract(NavMeshTemplateCreation template) {
//            this.template = template;
//            this.profiler = template.profiler;
//            //foreach (var collider in colliders)
//            //    if (IsValid(collider))
//            //        validColliders.Add(collider);
//        }

//        public abstract void AddColliders(Collider[] colliders);

//        //abstract
//        public abstract void Collect(VolumeContainer container);
//        public abstract int collectedCount { get; }
//        protected abstract bool IsValid(Collider collider);
        
//        //oh now i know how it works. smart thing. siquentialy clip triangle sides to pixel sides and do all 4
//        //max vectors are actulay 14 but this will do
//        //actualy strange that it only do one side cause it might just do two oposite sides at same time
//        //since it work and i dont want to spend time with it then here it is
//        #region rasterization
//        Vector3[] _polyListXTemp = new Vector3[16];
//        Vector3[] _polyListXFinal = new Vector3[16];
//        Vector3[] _polyListZTemp = new Vector3[16];
//        Vector3[] _polyListZFinal = new Vector3[16];
//        float[] _polyDistances = new float[16];

//        //protected void RasterizeTriangle(Volume volume, Vector3 A, Vector3 B, Vector3 C, float fragmentSize, int startX, int endX, int startZ, int endZ, Area area, Passability passability, params VoxelState[] trueStates) {
//        //    int minX = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Min(A.x, B.x, C.x) / fragmentSize) - 1, startX, endX);
//        //    int minZ = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Min(A.z, B.z, C.z) / fragmentSize) - 1, startZ, endZ);
//        //    int maxX = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Max(A.x, B.x, C.x) / fragmentSize) + 1, startX, endX);        
//        //    int maxZ = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Max(A.z, B.z, C.z) / fragmentSize) + 1, startZ, endZ);

//        //    if (minX == maxX || minZ == maxZ)
//        //        return; //if too small return

//        //    Vector3[] vectorsStart = new Vector3[3] { A, B, C };

//        //    for (int x = minX; x < maxX; ++x) {
//        //        int vertsInLength1 = ClipPolyToPlane(1f, 0.0f, -x * fragmentSize, vectorsStart, 3, ref _polyListXTemp);

//        //        if (vertsInLength1 >= 3) {
//        //            int vertsInLength2 = ClipPolyToPlane(-1f, 0.0f, (x + 1) * fragmentSize, _polyListXTemp, vertsInLength1, ref _polyListXFinal);

//        //            if (vertsInLength2 >= 3) {
//        //                for (int z = minZ; z < maxZ; ++z) {
//        //                    int vertsInLength3 = ClipPolyToPlane(0.0f, 1f, -z * fragmentSize, _polyListXFinal, vertsInLength2, ref _polyListZTemp);

//        //                    if (vertsInLength3 >= 3) {
//        //                        int vertsInLength4 = ClipPolyToPlane(0.0f, -1f, (z + 1) * fragmentSize, _polyListZTemp, vertsInLength3, ref _polyListZFinal);
//        //                        if (vertsInLength4 >= 3) {
//        //                            float min = _polyListZFinal[0].y;
//        //                            float max = _polyListZFinal[0].y;

//        //                            for (int index = 1; index < vertsInLength4; ++index) {
//        //                                min = Math.Min(min, _polyListZFinal[index].y);
//        //                                max = Math.Max(max, _polyListZFinal[index].y);
//        //                            }

//        //                            int indexX = Math.Abs(x - startX);
//        //                            int indexZ = Math.Abs(z - startZ);

//        //                            volume.SetVoxel(indexX, indexZ, max, min, area, (sbyte)passability);
//        //                        }
//        //                    }
//        //                }
//        //            }
//        //        }
//        //    }
//        //}

//        protected void RasterizeTriangle(VolumeSimple volumeSimple, Vector3 A, Vector3 B, Vector3 C, float fragmentSize, int startX, int endX, int startZ, int endZ, Passability passability) {
//            int minX = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Min(A.x, B.x, C.x) / fragmentSize) - 1, startX, endX);
//            int minZ = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Min(A.z, B.z, C.z) / fragmentSize) - 1, startZ, endZ);
//            int maxX = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Max(A.x, B.x, C.x) / fragmentSize) + 1, startX, endX);
//            int maxZ = Mathf.Clamp(Mathf.RoundToInt(SomeMath.Max(A.z, B.z, C.z) / fragmentSize) + 1, startZ, endZ);

//            if (minX == maxX || minZ == maxZ)
//                return; //if too small return

//            Vector3[] vectorsStart = new Vector3[3] { A, B, C };

//            for (int x = minX; x < maxX; ++x) {
//                int vertsInLength1 = ClipPolyToPlane(1f, 0.0f, -x * fragmentSize, vectorsStart, 3, ref _polyListXTemp);

//                if (vertsInLength1 >= 3) {
//                    int vertsInLength2 = ClipPolyToPlane(-1f, 0.0f, (x + 1) * fragmentSize, _polyListXTemp, vertsInLength1, ref _polyListXFinal);

//                    if (vertsInLength2 >= 3) {
//                        for (int z = minZ; z < maxZ; ++z) {
//                            int vertsInLength3 = ClipPolyToPlane(0.0f, 1f, -z * fragmentSize, _polyListXFinal, vertsInLength2, ref _polyListZTemp);

//                            if (vertsInLength3 >= 3) {
//                                int vertsInLength4 = ClipPolyToPlane(0.0f, -1f, (z + 1) * fragmentSize, _polyListZTemp, vertsInLength3, ref _polyListZFinal);
//                                if (vertsInLength4 >= 3) {
//                                    float min = _polyListZFinal[0].y;
//                                    float max = _polyListZFinal[0].y;

//                                    for (int index = 1; index < vertsInLength4; ++index) {
//                                        min = Math.Min(min, _polyListZFinal[index].y);
//                                        max = Math.Max(max, _polyListZFinal[index].y);
//                                    }

//                                    int indexX = Math.Abs(x - startX);
//                                    int indexZ = Math.Abs(z - startZ);

//                                    volumeSimple.SetVoxel(indexX, indexZ, max, min, (sbyte)passability);
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//        }

//        private int ClipPolyToPlane(float aPlaneNormalX, float aPlaneNormalZ, float aPlaneNormalDistance, Vector3[] aVerticesIn, int aVerticesInLength, ref Vector3[] aVerticesOut) {
//            if (_polyDistances.Length < aVerticesInLength)
//                Array.Resize(ref _polyDistances, aVerticesInLength * 2);

//            for (int index = 0; index < aVerticesInLength; ++index)
//                _polyDistances[index] = (float)((double)aPlaneNormalX * aVerticesIn[index].x + (double)aPlaneNormalZ * aVerticesIn[index].z) + aPlaneNormalDistance;

//            if (aVerticesOut.Length < aVerticesInLength * 2)
//                Array.Resize(ref aVerticesOut, aVerticesInLength * 2);

//            int num = 0;

//            int index2 = aVerticesInLength - 1;
//            for (int index1 = 0; index1 < aVerticesInLength; ++index1) {
//                bool flag1 = _polyDistances[index2] >= 0f;
//                bool flag2 = _polyDistances[index1] >= 0f;
//                if (flag1 != flag2)
//                    aVerticesOut[num++] = aVerticesIn[index2] + ((aVerticesIn[index1] - aVerticesIn[index2]) * (_polyDistances[index2] / (_polyDistances[index2] - _polyDistances[index1])));
//                if (flag2)
//                    aVerticesOut[num++] = aVerticesIn[index1];
//                index2 = index1;
//            }
//            return num;
//        }

//        protected static bool CalculateWalk(Vector3 A, Vector3 B, Vector3 C, float aMaxSlopeCos, bool flipY = false) {
//            if (flipY)   
//                return (Vector3.Cross(B - A, C - A).normalized.y * -1) >= aMaxSlopeCos;            
//            else
//                return Vector3.Cross(B - A, C - A).normalized.y >= aMaxSlopeCos;
//        }

//        #endregion
//    }
//}
