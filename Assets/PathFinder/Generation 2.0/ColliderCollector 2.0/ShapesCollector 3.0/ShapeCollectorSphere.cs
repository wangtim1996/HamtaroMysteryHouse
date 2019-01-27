using K_PathFinder.Collector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Collector3 {
    public partial class ShapeCollector {
        public void AppendSphere(ShapeDataSphere sphere) {
            if(sphere.area.id != 1)
                AppendSphereWalkable(sphere.bounds.center, sphere.bounds.extents.x, GetAreaValue(sphere.area));
            else
                AppendSphereNotWalkable(sphere.bounds.center, sphere.bounds.extents.x, GetAreaValue(sphere.area));
        }

        //general purpose for internal usage
        private void AppendSpherePrivate(DataCompact[] compactData, Vector3 spherePos, float sphereRadius, float heightDelta, bool appentPassive, bool isWalkable) {
            float voxelSize = template.voxelSize;
            float voxelSizeHalf = voxelSize * 0.5f;
            float sphereRadiusSqr = sphereRadius * sphereRadius;
            float maxSlopeY = Mathf.Sin(template.maxSlope * Mathf.PI / 180) * sphereRadius;

            Vector3 chunkReal = template.realOffsetedPosition;
            Vector3 sphereLocal = spherePos - chunkReal;

            float sphereY = spherePos.y;
            float sphereLocalX = sphereLocal.x;
            float sphereLocalZ = sphereLocal.z;

            int xMin = Mathf.Clamp((int)((sphereLocalX - sphereRadius) / voxelSize), 0, template.lengthX_extra);
            int xMax = Mathf.Clamp((int)((sphereLocalX + sphereRadius) / voxelSize) + 1, 0, template.lengthX_extra);
            int zMin = Mathf.Clamp((int)((sphereLocalZ - sphereRadius) / voxelSize), 0, template.lengthZ_extra);
            int zMax = Mathf.Clamp((int)((sphereLocalZ + sphereRadius) / voxelSize) + 1, 0, template.lengthZ_extra);

            if (appentPassive) {
                if (isWalkable) {
                    for (int x = xMin; x < xMax; x++) {
                        for (int z = zMin; z < zMax; z++) {
                            float distSqr = SomeMath.SqrMagnitude(
                                sphereLocalX - (x * voxelSize) - voxelSizeHalf,
                                sphereLocalZ - (z * voxelSize) - voxelSizeHalf);

                            if (distSqr < sphereRadiusSqr) {
                                float height = Mathf.Sqrt(sphereRadiusSqr - distSqr);
                                sbyte pass = height >= maxSlopeY ? (sbyte)Passability.Walkable : (sbyte)Passability.Slope;
                                compactData[GetIndex(x, z)].UpdatePassive(sphereY - height - heightDelta, sphereY + height + heightDelta, pass);
                            }
                        }
                    }
                }
                else {
                    for (int x = xMin; x < xMax; x++) {
                        for (int z = zMin; z < zMax; z++) {
                            float distSqr = SomeMath.SqrMagnitude(
                                sphereLocalX - (x * voxelSize) - voxelSizeHalf,
                                sphereLocalZ - (z * voxelSize) - voxelSizeHalf);

                            if (distSqr < sphereRadiusSqr) {
                                float height = Mathf.Sqrt(sphereRadiusSqr - distSqr);
                                compactData[GetIndex(x, z)].UpdatePassive(sphereY - height - heightDelta, sphereY + height + heightDelta, (sbyte)Passability.Unwalkable);
                            }
                        }
                    }
                }

                int centerX = (int)(sphereLocalX / voxelSize);
                int centerZ = (int)(sphereLocalZ / voxelSize);

                if (SomeMath.InRangeArrayLike(centerX, 0, template.lengthX_extra) &&
                   SomeMath.InRangeArrayLike(centerZ, 0, template.lengthZ_extra)) {
                    compactData[GetIndex(
                        (int)(sphereLocalX / voxelSize),
                        (int)(sphereLocalZ / voxelSize))].UpdatePassive(
                        sphereY - sphereRadius - heightDelta,
                        sphereY + sphereRadius + heightDelta,
                        isWalkable ? (sbyte)Passability.Walkable : (sbyte)Passability.Unwalkable);
                }


            }
            else {
                if (isWalkable) {
                    for (int x = xMin; x < xMax; x++) {
                        for (int z = zMin; z < zMax; z++) {
                            float distSqr = SomeMath.SqrMagnitude(
                                sphereLocalX - (x * voxelSize) - voxelSizeHalf,
                                sphereLocalZ - (z * voxelSize) - voxelSizeHalf);

                            if (distSqr < sphereRadiusSqr) {
                                float height = Mathf.Sqrt(sphereRadiusSqr - distSqr);
                                sbyte pass = height >= maxSlopeY ? (sbyte)Passability.Walkable : (sbyte)Passability.Slope;
                                compactData[GetIndex(x, z)].Update(sphereY - height - heightDelta, sphereY + height + heightDelta, pass);
                            }
                        }
                    }
                }
                else {
                    for (int x = xMin; x < xMax; x++) {
                        for (int z = zMin; z < zMax; z++) {
                            float distSqr = SomeMath.SqrMagnitude(
                                sphereLocalX - (x * voxelSize) - voxelSizeHalf,
                                sphereLocalZ - (z * voxelSize) - voxelSizeHalf);

                            if (distSqr < sphereRadiusSqr) {
                                float height = Mathf.Sqrt(sphereRadiusSqr - distSqr);
                                compactData[GetIndex(x, z)].Update(sphereY - height - heightDelta, sphereY + height + heightDelta, (sbyte)Passability.Unwalkable);
                            }
                        }
                    }
                }


                int centerX = (int)(sphereLocalX / voxelSize);
                int centerZ = (int)(sphereLocalZ / voxelSize);

                if (SomeMath.InRangeArrayLike(centerX, 0, template.lengthX_extra) &&
                   SomeMath.InRangeArrayLike(centerZ, 0, template.lengthZ_extra)) {
                    compactData[GetIndex(
                        (int)(sphereLocalX / voxelSize),
                        (int)(sphereLocalZ / voxelSize))].Update(
                        sphereY - sphereRadius - heightDelta,
                        sphereY + sphereRadius + heightDelta,
                        isWalkable ? (sbyte)Passability.Walkable : (sbyte)Passability.Unwalkable);
                }
            }
        }

        //2 versions for fast adding spheres
        public void AppendSphereWalkable(Vector3 spherePos, float sphereRadius, byte area) {
            float voxelSize = template.voxelSize;
            float voxelSizeHalf = voxelSize * 0.5f;
            float sphereRadiusSqr = sphereRadius * sphereRadius;
            float maxSlopeY = Mathf.Sin(template.maxSlope * Mathf.PI / 180) * sphereRadius;

            Vector3 chunkReal = template.realOffsetedPosition;
            Vector3 sphereLocal = spherePos - chunkReal;

            float sphereY = spherePos.y;
            float sphereLocalX = sphereLocal.x;
            float sphereLocalZ = sphereLocal.z;

            int xMin = Mathf.Clamp((int)((sphereLocalX - sphereRadius) / voxelSize), 0, template.lengthX_extra);
            int xMax = Mathf.Clamp((int)((sphereLocalX + sphereRadius) / voxelSize) + 1, 0, template.lengthX_extra);
            int zMin = Mathf.Clamp((int)((sphereLocalZ - sphereRadius) / voxelSize), 0, template.lengthZ_extra);
            int zMax = Mathf.Clamp((int)((sphereLocalZ + sphereRadius) / voxelSize) + 1, 0, template.lengthZ_extra);

            for (int x = xMin; x < xMax; x++) {
                for (int z = zMin; z < zMax; z++) {           
                    float distSqr = SomeMath.SqrMagnitude(
                        sphereLocalX - (x * voxelSize) - voxelSizeHalf,
                        sphereLocalZ - (z * voxelSize) - voxelSizeHalf);

                    if (distSqr < sphereRadiusSqr) {
                        float height = Mathf.Sqrt(sphereRadiusSqr - distSqr);
                        sbyte pass = height >= maxSlopeY ? (sbyte)Passability.Walkable : (sbyte)Passability.Slope;
                        SetVoxel(x, z, sphereY - height, sphereY + height, pass, area);
                    }
                }
            }


            int centerX = (int)(sphereLocalX / voxelSize);
            int centerZ = (int)(sphereLocalZ / voxelSize);

            if (SomeMath.InRangeArrayLike(centerX, 0, template.lengthX_extra) &&
                SomeMath.InRangeArrayLike(centerZ, 0, template.lengthZ_extra)) {
                SetVoxel(centerX, centerX, sphereY - sphereRadius, sphereY + sphereRadius, (sbyte)Passability.Walkable, area);
            }
        }
        public void AppendSphereNotWalkable(Vector3 spherePos, float sphereRadius, byte area) {
            float voxelSize = template.voxelSize;
            float voxelSizeHalf = voxelSize * 0.5f;
            float sphereRadiusSqr = sphereRadius * sphereRadius;

            Vector3 chunkReal = template.realOffsetedPosition;
            Vector3 sphereLocal = spherePos - chunkReal;

            float sphereY = spherePos.y;
            float sphereLocalX = sphereLocal.x;
            float sphereLocalZ = sphereLocal.z;

            int xMin = Mathf.Clamp((int)((sphereLocalX - sphereRadius) / voxelSize), 0, template.lengthX_extra);
            int xMax = Mathf.Clamp((int)((sphereLocalX + sphereRadius) / voxelSize) + 1, 0, template.lengthX_extra);
            int zMin = Mathf.Clamp((int)((sphereLocalZ - sphereRadius) / voxelSize), 0, template.lengthZ_extra);
            int zMax = Mathf.Clamp((int)((sphereLocalZ + sphereRadius) / voxelSize) + 1, 0, template.lengthZ_extra);

            sbyte pass = (sbyte)Passability.Unwalkable;

            for (int x = xMin; x < xMax; x++) {
                for (int z = zMin; z < zMax; z++) {
                    float distSqr = SomeMath.SqrMagnitude(
                        sphereLocalX - (x * voxelSize) - voxelSizeHalf,
                        sphereLocalZ - (z * voxelSize) - voxelSizeHalf);

                    if (distSqr < sphereRadiusSqr) {
                        float height = Mathf.Sqrt(sphereRadiusSqr - distSqr);
                        SetVoxel(x, z, sphereY - height, sphereY + height, pass, area);
                    }
                }
            }

            int centerX = (int)(sphereLocalX / voxelSize);
            int centerZ = (int)(sphereLocalZ / voxelSize);

            if (SomeMath.InRangeArrayLike(centerX, 0, template.lengthX_extra) &&
                SomeMath.InRangeArrayLike(centerZ, 0, template.lengthZ_extra)) {
                SetVoxel(centerX, centerX, sphereY - sphereRadius, sphereY + sphereRadius, pass, area);
            }
        }  
        

    }
}