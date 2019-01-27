using K_PathFinder.Collector;
using K_PathFinder.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Collector3 {
    public partial class ShapeCollector {
        private struct ElipseLine {
            public readonly float point1x, point1y, point2x, point2y, normalizedX, normalizedY, length;
            public readonly sbyte passability;

            public ElipseLine(float Point1x, float Point1y, float Point2x, float Point2y, sbyte Passability) {
                point1x = Point1x;
                point1y = Point1y;
                point2x = Point2x;
                point2y = Point2y;
                passability = Passability;

                float dirX = Point2x - Point1x;
                float dirY = Point2y - Point1y;

                length = SomeMath.Magnitude(dirX, dirY);
                normalizedX = dirX / length;
                normalizedY = dirY / length;
            }

            public ElipseLine(Vector2 p1, Vector2 p2, sbyte Passability) : this(p1.x, p1.y, p2.x, p2.y, Passability) { }

            public Vector2 point1 {
                get { return new Vector2(point1x, point1y); }
            }
            public Vector2 point2 {
                get { return new Vector2(point2x, point2y); }
            }

            public Vector2 direction {
                get { return new Vector2(point2x - point1x, point2y - point1y); }
            }

            public Vector2 normalized {
                get { return new Vector2(normalizedX, normalizedY); }
            }
        }
        enum C_Axis { x, z }//collection axis. so if elipse inclined to some axis this axis is iced to reduce error

        public void AppendCapsule(ShapeDataCapsule capsule) {
            AppendCapsule(capsule.sphereA, capsule.sphereB, capsule.capsileRadius, capsule.infoMode != ColliderInfoMode.Solid, GetAreaValue(capsule.area));
        }

        private void AppendCapsule(Vector3 sphereA, Vector3 sphereB, float capsileRadius, bool FLIP_Y, byte area) {
            DataCompact[] compactData = TakeCompactData();
            AppendCapsulePrivate(compactData, sphereA, sphereB, capsileRadius, FLIP_Y, area);
            AppendCompactData(compactData, area);
            GenericPoolArray<DataCompact>.ReturnToPool(ref compactData);
        }
        
        private void AppendCapsulePrivate(DataCompact[] compactData, Vector3 sphereA, Vector3 sphereB, float capsileRadius, bool FLIP_Y, byte area) {
            bool IS_WALKABLE = area != 1;

            float voxelSize = template.voxelSize;
            float voxelSizeHalf = voxelSize * 0.5f;

            //if sphere is on top
            if ((int)(sphereA.x / voxelSize) == (int)(sphereB.x / voxelSize) & (int)(sphereA.z / voxelSize) == (int)(sphereB.z / voxelSize)) {
                AppendSpherePrivate(compactData,
                    SomeMath.MidPoint(sphereA, sphereB),
                    capsileRadius,
                    Mathf.Abs(sphereA.y - sphereB.y) * 0.5f,
                    true,
                    IS_WALKABLE);
                return;
            }

            #region values setup
            Vector3 AB = sphereB - sphereA;
            Vector3 AB_normalized = AB.normalized;

            Vector2 sphereA_v2 = new Vector2(sphereA.x, sphereA.z);
            Vector2 sphereB_v2 = new Vector2(sphereB.x, sphereB.z);
            Vector2 AB_v2 = sphereB_v2 - sphereA_v2;


            float alighmentAxis = Vector2.Angle(AB_v2, new Vector2(0, 1));
            Vector3 axisPlaneNormal;
            C_Axis alighment;

            if (alighmentAxis >= 45 & alighmentAxis <= 135) {
                axisPlaneNormal = new Vector3(1, 0, 0);
                alighment = C_Axis.x;
            }
            else {
                axisPlaneNormal = new Vector3(0, 0, 1);
                alighment = C_Axis.z;
            }

            Vector3 v3 = Math3d.ProjectVectorOnPlane(axisPlaneNormal, AB);
            Vector3 v3normalized = v3.normalized;

            float angle = Vector3.Angle(AB, v3);
            float outerRadius = capsileRadius / Mathf.Sin(angle * Mathf.Deg2Rad);
            //float radiusDifference = outerRadius - capsileRadius;

            Quaternion q = new Quaternion();

            switch (alighment) {
                case C_Axis.x:
                    q = Quaternion.Euler(0, 0, Mathf.Atan2(v3.z, v3.y) * Mathf.Rad2Deg);
                    break;
                case C_Axis.z:
                    q = Quaternion.Euler(0, 0, Mathf.Atan2(v3.y, v3.x) * Mathf.Rad2Deg);
                    break;
            }

            Bounds2DInt volumeBoundsSphereA = GetVolumeBounds(sphereA, capsileRadius, template);
            Bounds2DInt volumeBoundsSphereB = GetVolumeBounds(sphereB, capsileRadius, template);
            Bounds2DInt volumeBoundsCombined = Bounds2DInt.GetIncluded(volumeBoundsSphereA, volumeBoundsSphereB);
            int startX = volumeBoundsCombined.minX + template.startX_extra;
            int startZ = volumeBoundsCombined.minY + template.startZ_extra;
            int endX = volumeBoundsCombined.maxX + template.startX_extra;
            int endZ = volumeBoundsCombined.maxY + template.startZ_extra;
            #endregion

            //generating elipse
            #region elipse generation
            Vector2[] generatedElipse = MakeElipse(capsileRadius, outerRadius, 6);
            for (int i = 0; i < generatedElipse.Length; i++) {
                generatedElipse[i] = q * generatedElipse[i];
            }
            
            //generating ordered lines
            List<ElipseLine> elipseLines = new List<ElipseLine>();
            for (int i = 0; i < generatedElipse.Length - 1; i++) {
                Vector2 p1 = generatedElipse[i];
                Vector2 p2 = generatedElipse[i + 1];
                sbyte pass = -1;

                if (IS_WALKABLE) {
                    Vector3 p1valid = GetValidVector3(p1, alighment);
                    Vector3 p2valid = GetValidVector3(p2, alighment);
                    Vector3 mid = SomeMath.MidPoint(p1valid, p2valid);
                    Vector3 nearest = SomeMath.NearestPointOnLine(new Vector3(), AB, mid);
                    Vector3 normal = mid - nearest;

                    if (FLIP_Y)
                        normal *= -1;
                
                    float normalAngle = Vector3.Angle(Vector3.up, normal);

                    if (normal.y >= 0) {
                        if (normalAngle <= template.maxSlope)
                            pass = (int)Passability.Walkable;
                        else
                            pass = (int)Passability.Slope;
                    }
                }
                else {
                    pass = (int)Passability.Unwalkable;
                }

                //get line itself
                switch (alighment) {
                    case C_Axis.x:
                        if (p1.y < p2.y) { elipseLines.Add(new ElipseLine(p1, p2, pass)); }
                        else
                        if (p1.y > p2.y) { elipseLines.Add(new ElipseLine(p2, p1, pass)); }
                        break;
                    case C_Axis.z:
                        if (p1.x < p2.x) { elipseLines.Add(new ElipseLine(p1, p2, pass)); }
                        else
                        if (p1.x > p2.x) { elipseLines.Add(new ElipseLine(p2, p1, pass)); }
                        break;
                }
            }

            GenericPoolArray<Vector2>.ReturnToPool(ref generatedElipse);
            #endregion
            
            if (alighment == C_Axis.z) {
                for (int currentZ = startZ; currentZ < endZ; currentZ++) {
                    Vector3 intersection = SomeMath.ClipLineToPlaneZ(sphereA, AB_normalized, (currentZ * voxelSize) + voxelSizeHalf);
                    float targetZ = intersection.z;
                    for (int i = 0; i < elipseLines.Count; i++) {
                        ElipseLine line = elipseLines[i];

                        float p1x = line.point1x + intersection.x;
                        float p1y = line.point1y + intersection.y;
                        float p2x = line.point2x + intersection.x;

                        sbyte pass = line.passability;

                        //if (pass != -1)
                        //    pass += 10;

                        //Vector3 p1 = GetValidVector3(line.point1) + intersection;
                        //Vector3 p2 = GetValidVector3(line.point2) + intersection;
                        //Debuger_K.AddLine(p1, p2, Color.blue);

                        for (int currentX = (int)(p1x / voxelSize) - 1; currentX < (int)(p2x / voxelSize) + 1; currentX++) {
                            if (currentX >= startX && currentX < endX) {
                                int vx = currentX - template.startX_extra;
                                int vz = currentZ - template.startZ_extra;

                                //float actualX;
                                //switch (intMask[vx][vz]) {
                                //    case 3: actualX = currentX * voxelSize + voxelSizeHalf; break;
                                //    case 4: actualX = currentX * voxelSize; break;
                                //    case 5: actualX = currentX * voxelSize + voxelSize; break;
                                //    default: actualX = currentX * voxelSize; break;
                                //}
                                float actualX = currentX * voxelSize + voxelSizeHalf;

                                float dx = (actualX - p1x) / line.normalizedX;//determinant

                                //Vector3 px = new Vector3(actualX, p1y + (line.normalizedY * dx), targetZ);
                                //Debuger_K.AddDot(px, Color.magenta, 0.01f);
                                //Debuger_K.AddLabelFormat(px, "{0}", dx, line.length);

                                if (dx >= 0f && dx <= line.length) {
                                    float targetY = p1y + (line.normalizedY * dx);

                                    //VolumeSetTime.Start();
                                    if (Mathf.Sign(SomeMath.Dot(AB.x, AB.y, AB.z, actualX - sphereA.x, targetY - sphereA.y, targetZ - sphereA.z)) !=
                                        Mathf.Sign(SomeMath.Dot(AB.x, AB.y, AB.z, actualX - sphereB.x, targetY - sphereB.y, targetZ - sphereB.z))) {
                                        if (pass == -1)
                                            compactData[GetIndex(vx, vz)].Update(targetY);
                                        else
                                            compactData[GetIndex(vx, vz)].Update(targetY, pass);
                                    }
                                    //VolumeSetTime.Stop();
                                }
                            }
                        }
                    }
                }
            }
            else
            if (alighment == C_Axis.x) {
                for (int currentX = startX; currentX < endX; currentX++) {
                    Vector3 intersection = SomeMath.ClipLineToPlaneX(sphereA, AB_normalized, (currentX * voxelSize) + voxelSizeHalf);
                    float targetX = intersection.x;

                    for (int i = 0; i < elipseLines.Count; i++) {
                        ElipseLine line = elipseLines[i];

                        float p1y = line.point1x + intersection.y;
                        float p1z = line.point1y + intersection.z;
                        float p2z = line.point2y + intersection.z;

                        sbyte pass = line.passability;


                        //if (pass != -1)
                        //    pass += 10;

                        //Vector3 p1 = GetValidVector3(line.point1) + intersection;
                        //Vector3 p2 = GetValidVector3(line.point2) + intersection;
                        //Debuger_K.AddLine(p1, p2, Color.blue);


                        for (int currentZ = (int)(p1z / voxelSize) - 1; currentZ < (int)(p2z / voxelSize) + 1; currentZ++) {
                            if (currentZ >= startZ && currentZ < endZ) {
                                int vx = currentX - template.startX_extra;
                                int vz = currentZ - template.startZ_extra;

                                //float actualZ;
                                //switch (intMask[vx][vz]) {
                                //    case 3: actualZ = currentZ * voxelSize + voxelSizeHalf; break;
                                //    case 4: actualZ = currentZ * voxelSize; break;
                                //    case 5: actualZ = currentZ * voxelSize + voxelSize; break;
                                //    default: actualZ = currentZ * voxelSize; break;
                                //}


                                float actualZ = currentZ * voxelSize + voxelSizeHalf;
                                float dz = (actualZ - p1z) / line.normalizedY;//determinant

                                //Vector3 px = new Vector3(targetX, p1y + (line.normalizedY * dz), actualZ);
                                //Debuger_K.AddDot(px, Color.magenta, 0.01f);


                                if (dz >= 0f && dz <= line.length) {
                                    float targetY = p1y + (line.normalizedX * dz);

                                    //VolumeSetTime.Start();
                                    if (Mathf.Sign(SomeMath.Dot(AB.x, AB.y, AB.z, targetX - sphereA.x, targetY - sphereA.y, actualZ - sphereA.z)) !=
                                        Mathf.Sign(SomeMath.Dot(AB.x, AB.y, AB.z, targetX - sphereB.x, targetY - sphereB.y, actualZ - sphereB.z))) {
                                        if (pass == -1)
                                            compactData[GetIndex(vx, vz)].Update(targetY);
                                        else
                                            compactData[GetIndex(vx, vz)].Update(targetY, pass);
                                    }
                                    //VolumeSetTime.Stop();
                                }
                            }
                        }
                    }
                }
            }

            if (IS_WALKABLE == false) {
                AppendSpherePrivate(compactData, sphereA, capsileRadius, 0, false, false);
                AppendSpherePrivate(compactData, sphereB, capsileRadius, 0, false, false);
            }
            else {
                if (sphereA.y == sphereB.y) {
                    AppendSpherePrivate(compactData, sphereA, capsileRadius, 0, false, true);
                    AppendSpherePrivate(compactData, sphereB, capsileRadius, 0, false, true);
                }
                else if (FLIP_Y == false) {
                    if (sphereA.y > sphereB.y) {
                        AppendSpherePrivate(compactData, sphereA, capsileRadius, 0, false, true);
                        AppendSpherePrivate(compactData, sphereB, capsileRadius, 0, true, true);
                    }
                    else if (sphereA.y < sphereB.y) {
                        AppendSpherePrivate(compactData, sphereA, capsileRadius, 0, true, true);
                        AppendSpherePrivate(compactData, sphereB, capsileRadius, 0, false, true);
                    }
                }
                else {
                    if (sphereA.y < sphereB.y) {
                        AppendSpherePrivate(compactData, sphereA, capsileRadius, 0, true, true);
                        AppendSpherePrivate(compactData, sphereB, capsileRadius, 0, false, true);
                    }
                    else if (sphereA.y > sphereB.y) {
                        AppendSpherePrivate(compactData, sphereA, capsileRadius, 0, false, true);
                        AppendSpherePrivate(compactData, sphereB, capsileRadius, 0, true, true);
                    }
                }
            }
        }

        private static Vector2[] MakeElipse(float radiusInner, float radiusOuter, int elipseQuadLength) {
            Vector2[] array = GenericPoolArray<Vector2>.Take((elipseQuadLength * 4) + 1);
            float radiusDifference = radiusOuter - radiusInner;
            float elipseQuadStep = Mathf.PI / elipseQuadLength * 0.5f;

            for (int i = 0; i < elipseQuadLength; i++) {
                float xValue = Mathf.Cos(i * elipseQuadStep);
                float yValue = Mathf.Sin(i * elipseQuadStep);

                float xInner = xValue * radiusInner;
                float yInner = yValue * radiusInner;

                float xOuter = xValue * radiusOuter;
                float yOuter = yValue * radiusOuter;

                float lerp1 = xOuter / radiusOuter;
                float lerp2 = yOuter / radiusOuter;

                array[i] = new Vector2(xInner + (radiusDifference * lerp1), yInner);
                array[elipseQuadLength + i] = new Vector2(-yInner - (radiusDifference * lerp2), xInner);
                array[(elipseQuadLength * 2) + i] = new Vector2(-xInner - (radiusDifference * lerp1), -yInner);
                array[(elipseQuadLength * 3) + i] = new Vector2(yInner + (radiusDifference * lerp2), -xInner);
            }

            array[array.Length - 1] = array[0];
            return array;
        }

        private Vector3 GetValidVector3(Vector2 vector2, C_Axis alighment) {
            if (alighment == C_Axis.z) {
                return vector2;
            }
            else {
                return new Vector3(0, vector2.x, vector2.y);
            }
        }

        public static Bounds2DInt GetVolumeBounds(Vector3 spherePos, float sphereRadius, NavMeshTemplateCreation template) {
            float voxelSize = template.voxelSize;
            Vector3 chunkReal = template.realOffsetedPosition;
            Vector3 sphereLocal = spherePos - chunkReal;

            return new Bounds2DInt(
                Mathf.Max((int)((sphereLocal.x - sphereRadius) / voxelSize), 0),
                Mathf.Max((int)((sphereLocal.z - sphereRadius) / voxelSize), 0),
                Mathf.Min((int)((sphereLocal.x + sphereRadius) / voxelSize) + 1, template.lengthX_extra),
                Mathf.Min((int)((sphereLocal.z + sphereRadius) / voxelSize) + 1, template.lengthZ_extra));
        }
    }
}
