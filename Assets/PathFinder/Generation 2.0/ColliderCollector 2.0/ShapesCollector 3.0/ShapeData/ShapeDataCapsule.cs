using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using K_PathFinder.Graphs;
using K_PathFinder.Pool;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif


namespace K_PathFinder.Collector {
    /// <summary>
    /// this class used cor collection capsules. it create elipse and rasterize it while moving through grid
    /// </summary>
    public class ShapeDataCapsule : ShapeDataAbstract, IShapeDataClonable {        
        enum C_Axis {x, z}//collection axis. so if elipse inclined to some axis this axis is iced to reduce error
        public Vector3 sphereA, sphereB;
        public float capsileRadius;
        //C_Axis alighment;

        //runtime values 
        //bool doDebug = false;

        public ShapeDataCapsule(CapsuleCollider collider, Area area) : base(collider, area) {
            Transform transfrom = collider.transform;
            Vector3 lossyScale = transfrom.lossyScale;
            float height = collider.height * lossyScale.y;
            capsileRadius = collider.radius * Mathf.Max(lossyScale.x, lossyScale.z);
            //float size = Mathf.Max(1f, height / (capsileRadius * 2f)) - 2f;

            //Matrix4x4 scaleMatrix = Matrix4x4.Scale(new Vector3(capsileRadius * 2f, capsileRadius * 2f, capsileRadius * 2f));

            Matrix4x4 generalMatrix = Matrix4x4.TRS(
                collider.bounds.center,//actual world position
                transfrom.rotation * //world rotation
                Quaternion.Euler(//plus capsule orientation
                    collider.direction == 2 ? 90f : 0.0f,
                    0.0f,
                    collider.direction == 0 ? 90f : 0.0f),
                Vector3.one);

            float dif = (height * 0.5f) - capsileRadius;
            sphereA = generalMatrix.MultiplyPoint3x4(new Vector3(0, dif, 0));
            sphereB = generalMatrix.MultiplyPoint3x4(new Vector3(0, dif * -1, 0));
        }

        public ShapeDataCapsule(AreaWorldModMagicValue value, Area area, Quaternion modRotation, ColliderInfoMode infoMode) : base(value, area, infoMode) {
            Matrix4x4 G2L = value.container.localToWorldMatrix;
            Matrix4x4 L2W = value.localToWorldMatrix;
            Vector3 capsulePos = G2L.MultiplyPoint(value.position);
            sphereA = capsulePos + (G2L.rotation * L2W.rotation * new Vector3(0, (-value.height * 0.5f) + value.radius, 0));
            sphereB = capsulePos + (G2L.rotation * L2W.rotation * new Vector3(0, (value.height * 0.5f) - value.radius, 0));
            capsileRadius = value.radius;
            //Debuger_K.AddLine(sphereA, sphereB, Color.blue);
        }

        public ShapeDataCapsule(Vector3 sphereA, Vector3 sphereB, float capsileRadius, Bounds bounds, Area area) : base(string.Empty, area, bounds, ColliderInfoMode.Solid) {
            this.sphereA = sphereA;
            this.sphereB = sphereB;
            this.capsileRadius = capsileRadius;
        }

        //for cloning
        private ShapeDataCapsule(ShapeDataCapsule dataCapsule) : base(dataCapsule) {
            sphereA = dataCapsule.sphereA;
            sphereB = dataCapsule.sphereB;  
            capsileRadius = dataCapsule.capsileRadius;
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

        //public override VolumeSimple GetVolume(NavMeshTemplateCreation template) {
        //    //VolumeSimple benchmark = GetVolumeOld(collector);
        //    //System.Diagnostics.Stopwatch totalTime = new System.Diagnostics.Stopwatch();
        //    //System.Diagnostics.Stopwatch beforeRasterization = new System.Diagnostics.Stopwatch();
        //    //System.Diagnostics.Stopwatch afterRasterization = new System.Diagnostics.Stopwatch();
        //    //System.Diagnostics.Stopwatch VolumeSetTime = new System.Diagnostics.Stopwatch();
        //    //System.Diagnostics.Stopwatch clipLineXTime = new System.Diagnostics.Stopwatch();
        //    //System.Diagnostics.Stopwatch maskGenerationTime = new System.Diagnostics.Stopwatch();
        //    //totalTime.Start();
        //    //beforeRasterization.Start();

         

        //    VolumeSimple volume = GetSimpleVolume(template);
         
        //    float voxelSize = template.voxelSize;

        //    //in case spheres are above each others
        //    #region spheres on top fix
        //    int gridAX = (int)(sphereA.x / voxelSize);
        //    int gridAZ = (int)(sphereA.z / voxelSize);
        //    int gridBX = (int)(sphereB.x / voxelSize);
        //    int gridBZ = (int)(sphereB.z / voxelSize);
  
        //    if ((int)(sphereA.x / voxelSize) == (int)(sphereB.x / voxelSize) & (int)(sphereA.z / voxelSize) == (int)(sphereB.z / voxelSize)) {
        //        ShapeDataHelperSphereCollector.CollectStatic(SomeMath.MidPoint(sphereA, sphereB), capsileRadius, volume, template, collectionInstruction, Mathf.Abs(sphereA.y - sphereB.y) * 0.5f);
        //        return volume;
        //    }

        //    #endregion

        //    #region values setup
        //    Vector3 AB = sphereB - sphereA;
        //    Vector3 AB_normalized = AB.normalized;

        //    Vector2 sphereA_v2 = new Vector2(sphereA.x, sphereA.z);
        //    Vector2 sphereB_v2 = new Vector2(sphereB.x, sphereB.z);
        //    Vector2 AB_v2 = sphereB_v2 - sphereA_v2;


        //    float alighmentAxis = Vector2.Angle(AB_v2, new Vector2(0, 1));
        //    Vector3 axisPlaneNormal;
          
        //    if (alighmentAxis >= 45 & alighmentAxis <= 135) {
        //        axisPlaneNormal = new Vector3(1, 0, 0);
        //        alighment = C_Axis.x;
        //    }
        //    else {          
        //        axisPlaneNormal = new Vector3(0, 0, 1);
        //        alighment = C_Axis.z;
        //    }

        //    Vector3 v3 = Math3d.ProjectVectorOnPlane(axisPlaneNormal, AB);
        //    Vector3 v3normalized = v3.normalized;        

        //    float angle = Vector3.Angle(AB, v3);
        //    float outerRadius = capsileRadius / Mathf.Sin(angle * Mathf.Deg2Rad);
        //    float radiusDifference = outerRadius - capsileRadius;

        //    Quaternion q = new Quaternion();

        //    switch (alighment) {
        //        case C_Axis.x:
        //            q = Quaternion.Euler(0, 0, Mathf.Atan2(v3.z, v3.y) * Mathf.Rad2Deg);
        //            break;
        //        case C_Axis.z:
        //            q = Quaternion.Euler(0, 0, Mathf.Atan2(v3.y, v3.x) * Mathf.Rad2Deg);
        //            break;
        //    }


        //    //int startX = Mathf.Max((int)(Mathf.Min(sphereA.x - capsileRadius, sphereB.x - capsileRadius) / voxelSize) - 1, template.startX_extra);
        //    //int startZ = Mathf.Max((int)(Mathf.Min(sphereA.z - capsileRadius, sphereB.z - capsileRadius) / voxelSize) - 1, template.startZ_extra);
        //    //int endX = Mathf.Min((int)(Mathf.Max(sphereA.x + capsileRadius, sphereB.x + capsileRadius) / voxelSize) + 2, template.endX_extra);
        //    //int endZ = Mathf.Min((int)(Mathf.Max(sphereA.z + capsileRadius, sphereB.z + capsileRadius) / voxelSize) + 2, template.endZ_extra);

        //    //Debug.Log(volumeBoundsCombined);

        //    Bounds2DInt volumeBoundsSphereA = ShapeDataHelperSphereCollector.GetVolumeBounds(sphereA, capsileRadius, template);
        //    Bounds2DInt volumeBoundsSphereB = ShapeDataHelperSphereCollector.GetVolumeBounds(sphereB, capsileRadius, template);
        //    Bounds2DInt volumeBoundsCombined = Bounds2DInt.GetIncluded(volumeBoundsSphereA, volumeBoundsSphereB);
        //    int startX = volumeBoundsCombined.minX + template.startX_extra;
        //    int startZ = volumeBoundsCombined.minY + template.startZ_extra;
        //    int endX = volumeBoundsCombined.maxX + template.startX_extra;
        //    int endZ = volumeBoundsCombined.maxY + template.startZ_extra;

        //    //if (doDebug) {
        //    //    Vector3 dA = new Vector3(sphereA.x, 0, sphereA.z);
        //    //    Debuger_K.AddLine(dA, dA + new Vector3(AB_v2.x, 0, AB_v2.y), Color.green);
        //    //    Debuger_K.AddLine(dA, dA + new Vector3(0, 0, 10), Color.blue);
        //    //    Debuger_K.AddLabel(dA, alighmentAxis + " : " + alighment);

        //    //    Debuger_K.AddLine(sphereA, sphereA + AB, Color.blue);
        //    //    Debuger_K.AddLine(sphereA, sphereA + (v3.normalized * outerRadius), Color.magenta);

        //    //    switch (alighment) {
        //    //        case C_Axis.x:
        //    //            Debuger_K.AddLine(SomeMath.DrawCircle(SomeMath.Axises.yz, 360, sphereA, capsileRadius), Color.blue, true);
        //    //            Debuger_K.AddLine(SomeMath.DrawCircle(SomeMath.Axises.yz, 360, sphereA, outerRadius), Color.magenta, true);
        //    //            break;
        //    //        case C_Axis.z:
        //    //            Debuger_K.AddLine(SomeMath.DrawCircle(SomeMath.Axises.xy, 360, sphereA, capsileRadius), Color.blue, true);
        //    //            Debuger_K.AddLine(SomeMath.DrawCircle(SomeMath.Axises.xy, 360, sphereA, outerRadius), Color.magenta, true);
        //    //            break;
        //    //    }
        //    //}
        //    #endregion

        //    //generating elipse
        //    #region elipse generation
        //    Vector2[] generatedElipse = MakeElipse(capsileRadius, outerRadius, 6);
        //    for (int i = 0; i < generatedElipse.Length; i++) {
        //        generatedElipse[i] = q * generatedElipse[i];
        //    }

 

        //    //generating ordered lines
        //    List<ElipseLine> elipseLines = new List<ElipseLine>();
        //    for (int i = 0; i < generatedElipse.Length - 1; i++) {
        //        Vector2 p1 = generatedElipse[i];
        //        Vector2 p2 = generatedElipse[i + 1];

        //        Vector3 p1valid = GetValidVector3(p1);
        //        Vector3 p2valid = GetValidVector3(p2);
        //        Vector3 mid = SomeMath.MidPoint(p1valid, p2valid);
        //        Vector3 nearest = SomeMath.NearestPointOnLine(new Vector3(), AB, mid);
        //        Vector3 normal = mid - nearest;

        //        if (base.infoMode != ColliderInfoMode.Solid)
        //            normal *= -1;

        //        sbyte pass = -1;
        //        float normalAngle = Vector3.Angle(Vector3.up, normal);

        //        if (normal.y >= 0) {
        //            if (normalAngle <= template.maxSlope)
        //                pass = (int)Passability.Walkable;
        //            else
        //                pass = (int)Passability.Slope;
        //        }

        //        //get line itself
        //        switch (alighment) {
        //            case C_Axis.x:
        //                if (p1.y < p2.y) { elipseLines.Add(new ElipseLine(p1, p2, pass)); }
        //                else
        //                if (p1.y > p2.y) { elipseLines.Add(new ElipseLine(p2, p1, pass)); }
        //                break;
        //            case C_Axis.z:
        //                if (p1.x < p2.x) { elipseLines.Add(new ElipseLine(p1, p2, pass)); }
        //                else
        //                if (p1.x > p2.x) { elipseLines.Add(new ElipseLine(p2, p1, pass)); }
        //                break;
        //        }

        //        //if (doDebug) {
        //        //    Color dColor = Color.black;

        //        //    if (normal.y >= 0) {
        //        //        dColor = Color.magenta;
        //        //        if (normalAngle <= template.maxSlope)
        //        //            dColor = Color.green;
        //        //    }

        //        //    Debuger_K.AddLine(p1valid, p2valid, dColor);
        //        //    Debuger_K.AddLine(nearest, mid + normal, Color.cyan);
        //        //    Debuger_K.AddLabel(mid, normalAngle);
        //        //}
        //    }

        //    GenericPoolArray<Vector2>.ReturnToPool(ref generatedElipse);      
        //    #endregion

        //    //create mask
        //    //maskGenerationTime.Start();
        //    var intMask = GenericPoolArray2D<int>.Take(volume.size);
        
        //    Vector3 offset = template.realOffsetedPosition;
        //    float voxelSizeHalf = voxelSize * 0.5f;

        //    int startX_local = startX - template.startX_extra;
        //    int startZ_local = startZ - template.startZ_extra;
        //    int endX_local = endX - template.startX_extra;
        //    int endZ_local = endZ - template.startZ_extra;

        //    Vector2 clampSphereA = new Vector2();
        //    Vector2 clampSphereB = new Vector2();

        //    switch (alighment) {
        //        case C_Axis.x:
        //            clampSphereA = SomeMath.ClipLineToPlaneX(sphereA_v2, AB_v2.normalized, startX * voxelSize);
        //            clampSphereB = SomeMath.ClipLineToPlaneX(sphereA_v2, AB_v2.normalized, endX * voxelSize);
        //            break;
        //        case C_Axis.z:
        //            clampSphereA = SomeMath.ClipLineToPlaneY(sphereA_v2, AB_v2.normalized, startZ * voxelSize);
        //            clampSphereB = SomeMath.ClipLineToPlaneY(sphereA_v2, AB_v2.normalized, endZ * voxelSize);
        //            break;
        //    }

        //    //if (doDebug) {
        //    //    Debuger_K.AddLine(
        //    //        new Vector3(clampSphereA.x, 0, clampSphereA.y),
        //    //        new Vector3(clampSphereB.x, 0, clampSphereB.y), Color.magenta, 0.1f);
        //    //}
        //    //for (int x = startX_local; x < endX_local; x++) {
        //    //    for (int z = startZ_local; z < endZ_local; z++) {
        //    //        intMask[x][z] = 1;
        //    //    }
        //    //}
        //    //DDARasterization.DrawLine(
        //    //    clampSphereA.x - offset.x,
        //    //    clampSphereA.y - offset.z,
        //    //    clampSphereB.x - offset.x,
        //    //    clampSphereB.y - offset.z,
        //    //    voxelSize,
        //    //    (int x, int z) => {
        //    //        x = SomeMath.Clamp(0, volume.sizeX - 1, x);
        //    //        z = SomeMath.Clamp(0, volume.sizeZ - 1, z);

        //    //        intMask[x][z] = 2;
        //    //    });

        //    //DDARasterization.DrawLine(
        //    //    sphereA.x - offset.x,
        //    //    sphereA.z - offset.z,
        //    //    sphereB.x - offset.x,
        //    //    sphereB.z - offset.z,
        //    //    voxelSize, 
        //    //    (int x, int z) => {
        //    //        x = SomeMath.Clamp(0, volume.sizeX - 1, x);
        //    //        z = SomeMath.Clamp(0, volume.sizeZ - 1, z);

        //    //        intMask[x][z] = 3;                    
        //    //    });

        //    ////return volume;
        //    //if (doDebug) {
        //    //    //foreach (var item in elipseLines) {
        //    //    //    Vector3 vp1 = GetValidVector3(item.point1);
        //    //    //    Vector3 vp2 = GetValidVector3(item.point2);

        //    //    //    Vector3 mid = SomeMath.MidPoint(vp1, vp2);

        //    //    //    Debuger_K.AddLine(vp1, mid, Color.green);
        //    //    //    Debuger_K.AddLine(mid, vp2, Color.blue);

        //    //    //    Debuger_K.AddLine(vp1 + sphereA, vp2 + sphereA, Color.green);
        //    //    //}
        //    //}


        //    //afterRasterization.Start();





        //    //Debug.Log(startX_local);
        //    //Debug.Log(endX_local);
        //    //Debug.Log(startZ_local);
        //    //Debug.Log(endZ_local);

        //    //switch (alighment) {
        //    //    case C_Axis.x:
        //    //        for (int x = startX_local; x < endX_local; x++) {
        //    //            bool flag = false;
        //    //            for (int z = startZ_local; z < endZ_local; z++) {
        //    //                int value = intMask[x][z];
        //    //                if (value == 2) {
        //    //                    flag = true;
        //    //                }
        //    //                else {
        //    //                    if (flag)
        //    //                        intMask[x][z] = 4;
        //    //                    else
        //    //                        intMask[x][z] = 5;
        //    //                }
                            
        //    //            }
        //    //        }
        //    //        break;
        //    //    case C_Axis.z:
        //    //        for (int z = startZ_local; z < endZ_local; z++) {
        //    //            bool flag = false;
        //    //            for (int x = startX_local; x < endX_local; x++) {
        //    //                int value = intMask[x][z];
        //    //                if (value == 2) {
        //    //                    flag = true;
        //    //                }
        //    //                else {
        //    //                    if (flag)
        //    //                        intMask[x][z] = 4;
        //    //                    else
        //    //                        intMask[x][z] = 5;
        //    //                }
        //    //            }
        //    //        }
        //    //        break;
        //    //}
        //    //for (int x = 0; x < intMask.Length; x++) {
        //    //    for (int z = 0; z < intMask[x].Length; z++) {
        //    //        if (intMask[x][z] == 0)
        //    //            continue;

        //    //        Color dCol = Color.gray;
                    
        //    //        if (intMask[x][z] == 1) dCol = Color.white;
        //    //        if (intMask[x][z] == 2) dCol = Color.cyan;
        //    //        if (intMask[x][z] == 3) dCol = Color.blue;
        //    //        if (intMask[x][z] == 4) dCol = Color.green;
        //    //        if (intMask[x][z] == 5) dCol = Color.yellow;

        //    //        float tx = (x * voxelSize) + offset.x;
        //    //        float tz = (z * voxelSize) + offset.z;

        //    //        Debuger_K.AddQuad(
        //    //            new Vector3(tx, 0, tz),
        //    //            new Vector3(tx, 0, tz + voxelSize),
        //    //            new Vector3(tx + voxelSize, 0, tz),
        //    //            new Vector3(tx + voxelSize, 0, tz + voxelSize),
        //    //            new Color(dCol.r, dCol.g, dCol.b, 0.1f),
        //    //            true);

        //    //        Debuger_K.AddDot(new Vector3(
        //    //            (x * voxelSize) + offset.x + voxelSizeHalf, 0,
        //    //            (z * voxelSize) + offset.z + voxelSizeHalf), dCol);
        //    //    }
        //    //}

        //    if (alighment == C_Axis.z) {
        //        for (int currentZ = startZ; currentZ < endZ; currentZ++) {
        //            Vector3 intersection = SomeMath.ClipLineToPlaneZ(sphereA, AB_normalized, (currentZ * voxelSize) + voxelSizeHalf);
        //            float targetZ = intersection.z;
        //            for (int i = 0; i < elipseLines.Count; i++) {
        //                ElipseLine line = elipseLines[i];

        //                float p1x = line.point1x + intersection.x;
        //                float p1y = line.point1y + intersection.y;
        //                float p2x = line.point2x + intersection.x;

        //                sbyte pass = line.passability;
                
        //                //if (pass != -1)
        //                //    pass += 10;

        //                //Vector3 p1 = GetValidVector3(line.point1) + intersection;
        //                //Vector3 p2 = GetValidVector3(line.point2) + intersection;
        //                //Debuger_K.AddLine(p1, p2, Color.blue);

        //                for (int currentX = (int)(p1x / voxelSize) - 1; currentX < (int)(p2x / voxelSize) + 1; currentX++) {
        //                    if (currentX >= startX && currentX < endX) { 
        //                        int vx = currentX - template.startX_extra;
        //                        int vz = currentZ - template.startZ_extra;

        //                        //float actualX;
        //                        //switch (intMask[vx][vz]) {
        //                        //    case 3: actualX = currentX * voxelSize + voxelSizeHalf; break;
        //                        //    case 4: actualX = currentX * voxelSize; break;
        //                        //    case 5: actualX = currentX * voxelSize + voxelSize; break;
        //                        //    default: actualX = currentX * voxelSize; break;
        //                        //}
        //                        float actualX = currentX * voxelSize + voxelSizeHalf;

        //                        float dx = (actualX - p1x) / line.normalizedX;//determinant

        //                        //Vector3 px = new Vector3(actualX, p1y + (line.normalizedY * dx), targetZ);
        //                        //Debuger_K.AddDot(px, Color.magenta, 0.01f);
        //                        //Debuger_K.AddLabelFormat(px, "{0}", dx, line.length);

        //                        if (dx >= 0f && dx <= line.length) {
        //                            float targetY = p1y + (line.normalizedY * dx);

        //                            //VolumeSetTime.Start();
        //                            if (Mathf.Sign(SomeMath.Dot(AB.x, AB.y, AB.z, actualX - sphereA.x, targetY - sphereA.y, targetZ - sphereA.z)) !=
        //                                Mathf.Sign(SomeMath.Dot(AB.x, AB.y, AB.z, actualX - sphereB.x, targetY - sphereB.y, targetZ - sphereB.z))) {
        //                                if (pass == -1)
        //                                    volume.SetVoxel(vx, vz, targetY);
        //                                else
        //                                    volume.SetVoxel(vx, vz, targetY, pass);
        //                            }
        //                            //VolumeSetTime.Stop();
        //                        }
        //                    }        
        //                }
        //            }
        //        }
        //    }
        //    else
        //    if (alighment == C_Axis.x) {
        //        for (int currentX = startX; currentX < endX; currentX++) {
        //            Vector3 intersection = SomeMath.ClipLineToPlaneX(sphereA, AB_normalized, (currentX * voxelSize) + voxelSizeHalf);
        //            float targetX = intersection.x;

        //            for (int i = 0; i < elipseLines.Count; i++) {
        //                ElipseLine line = elipseLines[i];

        //                float p1y = line.point1x + intersection.y;
        //                float p1z = line.point1y + intersection.z;
        //                float p2z = line.point2y + intersection.z;

        //                sbyte pass = line.passability;


        //                //if (pass != -1)
        //                //    pass += 10;

        //                //Vector3 p1 = GetValidVector3(line.point1) + intersection;
        //                //Vector3 p2 = GetValidVector3(line.point2) + intersection;
        //                //Debuger_K.AddLine(p1, p2, Color.blue);


        //                for (int currentZ = (int)(p1z / voxelSize) - 1; currentZ < (int)(p2z / voxelSize) + 1; currentZ++) {
        //                    if (currentZ >= startZ && currentZ < endZ) {
        //                        int vx = currentX - template.startX_extra;
        //                        int vz = currentZ - template.startZ_extra;

        //                        //float actualZ;
        //                        //switch (intMask[vx][vz]) {
        //                        //    case 3: actualZ = currentZ * voxelSize + voxelSizeHalf; break;
        //                        //    case 4: actualZ = currentZ * voxelSize; break;
        //                        //    case 5: actualZ = currentZ * voxelSize + voxelSize; break;
        //                        //    default: actualZ = currentZ * voxelSize; break;
        //                        //}

                         
        //                        float actualZ = currentZ * voxelSize + voxelSizeHalf;
        //                        float dz = (actualZ - p1z) / line.normalizedY;//determinant

        //                        //Vector3 px = new Vector3(targetX, p1y + (line.normalizedY * dz), actualZ);
        //                        //Debuger_K.AddDot(px, Color.magenta, 0.01f);

                           
        //                        if (dz >= 0f && dz <= line.length) {
        //                            float targetY = p1y + (line.normalizedX * dz);

        //                            //VolumeSetTime.Start();
        //                            if (Mathf.Sign(SomeMath.Dot(AB.x, AB.y, AB.z, targetX - sphereA.x, targetY - sphereA.y, actualZ - sphereA.z)) !=
        //                                Mathf.Sign(SomeMath.Dot(AB.x, AB.y, AB.z, targetX - sphereB.x, targetY - sphereB.y, actualZ - sphereB.z))) {
        //                                if (pass == -1)
        //                                    volume.SetVoxel(vx, vz, targetY);
        //                                else
        //                                    volume.SetVoxel(vx, vz, targetY, pass);
        //                            }
        //                            //VolumeSetTime.Stop();
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    if (collectionInstruction == VolumeCollectionPassabilityOption.CollectAsNotPassable) {
        //        ShapeDataHelperSphereCollector.CollectStatic(sphereA, capsileRadius, volume, template, VolumeCollectionPassabilityOption.CollectAsNotPassable);
        //        ShapeDataHelperSphereCollector.CollectStatic(sphereB, capsileRadius, volume, template, VolumeCollectionPassabilityOption.CollectAsNotPassable);
        //    }
        //    else {
        //        if (sphereA.y == sphereB.y) {
        //            ShapeDataHelperSphereCollector.CollectStatic(sphereA, capsileRadius, volume, template, collectionInstruction);
        //            ShapeDataHelperSphereCollector.CollectStatic(sphereB, capsileRadius, volume, template, collectionInstruction);
        //        }
        //        else if (base.infoMode == ColliderInfoMode.Solid) {
        //            if (sphereA.y > sphereB.y) {
        //                ShapeDataHelperSphereCollector.CollectStatic(sphereA, capsileRadius, volume, template, VolumeCollectionPassabilityOption.CollectAsActive);
        //                ShapeDataHelperSphereCollector.CollectStatic(sphereB, capsileRadius, volume, template, VolumeCollectionPassabilityOption.CollectAsPassive);
        //            }
        //            else if (sphereA.y < sphereB.y) {
        //                ShapeDataHelperSphereCollector.CollectStatic(sphereA, capsileRadius, volume, template, VolumeCollectionPassabilityOption.CollectAsPassive);
        //                ShapeDataHelperSphereCollector.CollectStatic(sphereB, capsileRadius, volume, template, VolumeCollectionPassabilityOption.CollectAsActive);
        //            }
        //        }
        //        else {
        //            if (sphereA.y < sphereB.y) {
        //                ShapeDataHelperSphereCollector.CollectStatic(sphereA, capsileRadius, volume, template, VolumeCollectionPassabilityOption.CollectAsActive);
        //                ShapeDataHelperSphereCollector.CollectStatic(sphereB, capsileRadius, volume, template, VolumeCollectionPassabilityOption.CollectAsPassive);
        //            }
        //            else if (sphereA.y > sphereB.y) {
        //                ShapeDataHelperSphereCollector.CollectStatic(sphereA, capsileRadius, volume, template, VolumeCollectionPassabilityOption.CollectAsPassive);
        //                ShapeDataHelperSphereCollector.CollectStatic(sphereB, capsileRadius, volume, template, VolumeCollectionPassabilityOption.CollectAsActive);
        //            }
        //        }
        //    }

        //    GenericPoolArray2D<int>.ReturnToPool(ref intMask);

        //    //afterRasterization.Stop();
        //    //totalTime.Stop();
        //    //Debug.Log("New: " + totalTime.Elapsed);
        //    //Debug.Log("Before Rasterization: " + beforeRasterization.Elapsed);        
        //    //Debug.Log("Rasterization: " + afterRasterization.Elapsed);
        //    //Debug.Log("Set to volume " + VolumeSetTime.Elapsed);
        //    //Debug.Log("Clip Line " + clipLineXTime.Elapsed);
        //    //Debug.Log("Mask time " + maskGenerationTime.Elapsed); 
        //    return volume;
        //}

        //private Vector3 GetValidVector3(Vector2 vector2) {
        //    if(alighment == C_Axis.z) {
        //        return vector2;
        //    }
        //    else {
        //        return new Vector3(0, vector2.x, vector2.y);
        //    }
        //}

        //private Vector3 GetValidVector3(Vector2 vector2, Vector3 delta) {
        //    if (alighment == C_Axis.z) {
        //        return new Vector3(vector2.x + delta.x, vector2.y + delta.y, delta.z);
        //    }
        //    else {
        //        return new Vector3(delta.x, vector2.x + delta.y, vector2.y + delta.z);
        //    }
        //}

        //public static Vector2 GetTargetVector(float radians) {
        //    return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        //}

        public ShapeDataAbstract Clone() {
            return new ShapeDataCapsule(this);
        }

        //public VolumeSimple GetVolumeNewOld(ShapeCollector collector) {
        //    //var v = GetVolumeOld(collector);

        //    List<Vector3> vectors = new List<Vector3>();

        //    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        //    watch.Start();

        //    VolumeSimple volume = base.GetBasicVolume(collector);
        //    var template = collector.template;

        //    float voxelSize = template.voxelSize;
        //    int startX_extra = template.startX_extra;
        //    int endX_extra = template.endX_extra;
        //    int startZ_extra = template.startZ_extra;
        //    int endZ_extra = template.endZ_extra;




        //    Vector3 chunkReal = template.realOffsetedPosition;

        //    float add = voxelSize * 0.5f;
        //    Vector3 AB = sphereB - sphereA;
        //    float abDot = SomeMath.Dot(AB, AB);

        //    int startX = Mathf.Max((int)(Mathf.Min(sphereA.x - capsileRadius, sphereB.x - capsileRadius) / voxelSize), startX_extra);
        //    int startZ = Mathf.Min((int)(Mathf.Min(sphereA.z - capsileRadius, sphereB.z - capsileRadius) / voxelSize), endX_extra);
        //    int endX = Mathf.Max((int)(Mathf.Max(sphereA.x + capsileRadius, sphereB.x + capsileRadius) / voxelSize) + 1, startZ_extra);
        //    int endZ = Mathf.Min((int)(Mathf.Max(sphereA.z + capsileRadius, sphereB.z + capsileRadius) / voxelSize) + 1, endZ_extra);

        //    for (int x = startX; x < endX; x++) {
        //        float xPos = (x * voxelSize) + add;

        //        for (int z = startZ; z < endZ; z++) {
        //            float zPos = (z * voxelSize) + add;

        //            Vector3 AO = new Vector3(xPos - sphereA.x, -sphereA.y, zPos - sphereA.z);
        //            Vector3 AOxAB = SomeMath.Cross(AO, AB);
        //            Vector3 VxAB = new Vector3(-AB.z, 0, AB.x); //Vector3 VxAB = SomeMath.Cross(new Vector3(0, -1, 0), AB);

        //            float a = SomeMath.Dot(VxAB, VxAB);
        //            float b = 2 * SomeMath.Dot(VxAB, AOxAB);
        //            float c = SomeMath.Dot(AOxAB, AOxAB) - (capsileRadius * capsileRadius * abDot);
        //            float d = b * b - 4 * a * c;

        //            if (d >= 0) {
        //                float time = (-b - Mathf.Sqrt(d)) / (2 * a);
        //                Vector3 intersection = new Vector3(xPos, time * -1, zPos);/// intersection point
        //                float p = Vector3.Dot(AB, intersection - sphereA) / abDot;
        //                Vector3 projection = sphereA + p * AB; /// intersection projected onto cylinder axis
        //                Vector3 normal = intersection - projection;

        //                if (p >= 0 & p <= 1f) {
        //                    vectors.Add(intersection);
        //                }
        //            }
        //        }
        //    }


        //    //Debuger_K.AddDot(Color.magenta, sphereA, sphereB);

        //    watch.Stop();
        //    Debug.Log("New: " + watch.Elapsed);

        //    //Debuger_K.AddDot(vectors);



        //    //watch.Reset();
        //    //watch.Start();

        //    //for (int i = 0; i < 1000; i++) {
        //    //    for (int i2 = 0; i2 < 1000; i2++) {
        //    //        i2* i2;
        //    //    }
        //    //}
        //    //watch.Stop();
        //    //Debug.Log("Pow: " + watch.Elapsed);

        //    return volume;
        //}

        //        public VolumeSimple GetVolumeOld(ShapeCollector collector) {
        //            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        //            watch.Start();


        //            VolumeSimple volume = base.GetBasicVolume(collector);
        //            var template = collector.template;

        //            float voxelSize = template.voxelSize;
        //            int startX_extra = template.startX_extra;
        //            int endX_extra = template.endX_extra;
        //            int startZ_extra = template.startZ_extra;
        //            int endZ_extra = template.endZ_extra;

        //            Vector3[] verts = new Vector3[_capsuleVerts.Length];

        //            for (int index = 0; index < verts.Length; ++index) {
        //                if (_capsuleVerts[index].y > 0.0)
        //                    verts[index] = scaleMatrix.MultiplyPoint(
        //                        new Vector3(
        //                            _capsuleVerts[index].x,
        //                            Mathf.Max((float)(_capsuleVerts[index].y + size * 0.5), 0.0f),
        //                            _capsuleVerts[index].z));

        //                else if (_capsuleVerts[index].y < 0.0)
        //                    verts[index] = scaleMatrix.MultiplyPoint(
        //                        new Vector3(
        //                            _capsuleVerts[index].x,
        //                            Mathf.Min((float)(_capsuleVerts[index].y - size * 0.5), 0.0f),
        //                            _capsuleVerts[index].z));
        //            }

        //            for (int i = 0; i < _capsuleVerts.Length; i++) {
        //                verts[i] = generalMatrix.MultiplyPoint3x4(verts[i]);

        //            }

        //            Debuger_K.AddDot(verts);

        //            //rasterization
        //            float maxSlopeCos = Mathf.Cos(template.maxSlope * Mathf.PI / 180f);
        //            for (int t = 0; t < _capsuleTris.Length; t += 3) {
        //                Vector3 A = verts[_capsuleTris[t]];
        //                Vector3 B = verts[_capsuleTris[t + 1]];
        //                Vector3 C = verts[_capsuleTris[t + 2]];

        //                bool unwalkableBySlope = !CalculateWalk(A, B, C, maxSlopeCos, infoMode != ColliderInfoMode.Solid);
        //                Passability currentPassability;

        //                if (area.id == 1)//id of clear Area all time
        //                    currentPassability = Passability.Unwalkable;
        //                else if (unwalkableBySlope)
        //                    currentPassability = Passability.Slope;
        //                else
        //                    currentPassability = Passability.Walkable;

        //#if UNITY_EDITOR
        //                if (currentPassability > Passability.Slope && Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false)
        //                    Debuger_K.AddWalkablePolygon(template.gridPosX, template.gridPosZ, template.properties, A, B, C);
        //#endif

        //                base.RasterizeTriangle(
        //                    volume,
        //                    A, B, C,
        //                    voxelSize,
        //                    startX_extra, endX_extra,
        //                    startZ_extra, endZ_extra,
        //                    currentPassability);
        //            }

        //            watch.Stop();
        //            Debug.Log("Old: " + watch.Elapsed);

        //            return volume;
        //        }


        //public override VolumeSimple GetVolume(ShapeCollector collector) {
        //    VolumeSimple benchmark = GetVolumeOld(collector);

        //    System.Diagnostics.Stopwatch totalTime = new System.Diagnostics.Stopwatch();
        //    System.Diagnostics.Stopwatch beforeRasterization = new System.Diagnostics.Stopwatch();
        //    System.Diagnostics.Stopwatch afterRasterization = new System.Diagnostics.Stopwatch();
        //    System.Diagnostics.Stopwatch VolumeSetTime = new System.Diagnostics.Stopwatch();
        //    System.Diagnostics.Stopwatch clipLineXTime = new System.Diagnostics.Stopwatch();
        //    System.Diagnostics.Stopwatch maskGenerationTime = new System.Diagnostics.Stopwatch();

        //    totalTime.Start();
        //    beforeRasterization.Start();

        //    VolumeSimple volume = GetBasicVolume(collector);

        //    #region values setup
        //    Vector3 AB = sphereB - sphereA;
        //    Vector3 AB_normalized = AB.normalized;

        //    Vector2 sphereA_v2 = new Vector2(sphereA.x, sphereA.z);
        //    Vector2 sphereB_v2 = new Vector2(sphereB.x, sphereB.z);
        //    Vector2 AB_v2 = sphereB_v2 - sphereA_v2;

        //    var template = collector.template;
        //    float voxelSize = template.voxelSize;

        //    float alighmentAxis = Vector2.Angle(AB_v2, new Vector2(0, 1));
        //    Vector3 axisPlaneNormal;

        //    if (alighmentAxis >= 45 & alighmentAxis <= 135) {
        //        axisPlaneNormal = new Vector3(1, 0, 0);
        //        alighment = C_Axis.x;
        //    }
        //    else {
        //        axisPlaneNormal = new Vector3(0, 0, 1);
        //        alighment = C_Axis.z;
        //    }

        //    Vector3 v3 = Math3d.ProjectVectorOnPlane(axisPlaneNormal, AB);
        //    Vector3 v3normalized = v3.normalized;

        //    float angle = Vector3.Angle(AB, v3);
        //    float outerRadius = capsileRadius / Mathf.Sin(angle * Mathf.Deg2Rad);
        //    float radiusDifference = outerRadius - capsileRadius;

        //    Quaternion q = new Quaternion();

        //    switch (alighment) {
        //        case C_Axis.x:
        //            q = Quaternion.Euler(0, 0, Mathf.Atan2(v3.z, v3.y) * Mathf.Rad2Deg);
        //            break;
        //        case C_Axis.z:
        //            q = Quaternion.Euler(0, 0, Mathf.Atan2(v3.y, v3.x) * Mathf.Rad2Deg);
        //            break;
        //    }

        //    int startX = Mathf.Max((int)(Mathf.Min(sphereA.x - capsileRadius, sphereB.x - capsileRadius) / voxelSize) - 1, template.startX_extra);
        //    int startZ = Mathf.Max((int)(Mathf.Min(sphereA.z - capsileRadius, sphereB.z - capsileRadius) / voxelSize) - 1, template.startZ_extra);
        //    int endX = Mathf.Min((int)(Mathf.Max(sphereA.x + capsileRadius, sphereB.x + capsileRadius) / voxelSize) + 1, template.endX_extra);
        //    int endZ = Mathf.Min((int)(Mathf.Max(sphereA.z + capsileRadius, sphereB.z + capsileRadius) / voxelSize) + 1, template.endZ_extra);

        //    if (doDebug) {
        //        Vector3 dA = new Vector3(sphereA.x, 0, sphereA.z);
        //        Debuger_K.AddLine(dA, dA + new Vector3(AB_v2.x, 0, AB_v2.y), Color.green);
        //        Debuger_K.AddLine(dA, dA + new Vector3(0, 0, 10), Color.blue);
        //        Debuger_K.AddLabel(dA, alighmentAxis + " : " + alighment);

        //        Debuger_K.AddLine(sphereA, sphereA + AB, Color.blue);
        //        Debuger_K.AddLine(sphereA, sphereA + (v3.normalized * outerRadius), Color.magenta);

        //        switch (alighment) {
        //            case C_Axis.x:
        //                Debuger_K.AddLine(SomeMath.DrawCircle(SomeMath.Axises.yz, 360, sphereA, capsileRadius), Color.blue, true);
        //                Debuger_K.AddLine(SomeMath.DrawCircle(SomeMath.Axises.yz, 360, sphereA, outerRadius), Color.magenta, true);
        //                break;
        //            case C_Axis.z:
        //                Debuger_K.AddLine(SomeMath.DrawCircle(SomeMath.Axises.xy, 360, sphereA, capsileRadius), Color.blue, true);
        //                Debuger_K.AddLine(SomeMath.DrawCircle(SomeMath.Axises.xy, 360, sphereA, outerRadius), Color.magenta, true);
        //                break;
        //        }
        //    }
        //    #endregion

        //    //generating elipse
        //    #region elipse generation
        //    Vector2[] generatedElipse = MakeElipse(capsileRadius, outerRadius, 6);
        //    for (int i = 0; i < generatedElipse.Length; i++) {
        //        generatedElipse[i] = q * generatedElipse[i];
        //    }

        //    //generating ordered lines
        //    List<ElipseLine> elipseLines = new List<ElipseLine>();
        //    for (int i = 0; i < generatedElipse.Length - 1; i++) {
        //        Vector2 p1 = generatedElipse[i];
        //        Vector2 p2 = generatedElipse[i + 1];

        //        Vector3 p1valid = GetValidVector3(p1);
        //        Vector3 p2valid = GetValidVector3(p2);
        //        Vector3 mid = SomeMath.MidPoint(p1valid, p2valid);
        //        Vector3 nearest = SomeMath.NearestPointOnLine(new Vector3(), AB, mid);
        //        Vector3 normal = mid - nearest;
        //        float normalAngle = Vector3.Angle(Vector3.up, normal);


        //        //get line passability
        //        sbyte pass = -1;
        //        if (normal.y >= 0) {
        //            if (normalAngle <= template.maxSlope)
        //                pass = (int)Passability.Walkable;
        //            else
        //                pass = (int)Passability.Slope;
        //        }

        //        //get line itself
        //        switch (alighment) {
        //            case C_Axis.x:
        //                if (p1.y < p2.y) { elipseLines.Add(new ElipseLine(p1, p2, pass)); }
        //                else
        //                if (p1.y > p2.y) { elipseLines.Add(new ElipseLine(p2, p1, pass)); }
        //                break;
        //            case C_Axis.z:
        //                if (p1.x < p2.x) { elipseLines.Add(new ElipseLine(p1, p2, pass)); }
        //                else
        //                if (p1.x > p2.x) { elipseLines.Add(new ElipseLine(p2, p1, pass)); }
        //                break;
        //        }

        //        if (doDebug) {
        //            Color dColor = Color.black;

        //            if (normal.y >= 0) {
        //                dColor = Color.magenta;
        //                if (normalAngle <= template.maxSlope)
        //                    dColor = Color.green;
        //            }

        //            Debuger_K.AddLine(p1valid, p2valid, dColor);
        //            Debuger_K.AddLine(nearest, mid + normal, Color.cyan);
        //            Debuger_K.AddLabel(mid, normalAngle);
        //        }
        //    }

        //    ArrayPool.ReturnToPoolVector2(generatedElipse, false);
        //    #endregion

        //    //create mask
        //    maskGenerationTime.Start();
        //    var intMask = ArrayPool.GetFromPoolInt(volume.size);

        //    Vector3 offset = template.realOffsetedPosition;
        //    float voxelSizeHalf = voxelSize * 0.5f;

        //    int startX_local = startX - template.startX_extra;
        //    int startZ_local = startZ - template.startZ_extra;
        //    int endX_local = endX - template.startX_extra;
        //    int endZ_local = endZ - template.startZ_extra;

        //    Vector2 clampSphereA = new Vector2();
        //    Vector2 clampSphereB = new Vector2();

        //    switch (alighment) {
        //        case C_Axis.x:
        //            clampSphereA = SomeMath.ClipLineToPlaneX(sphereA_v2, AB_v2.normalized, startX * voxelSize);
        //            clampSphereB = SomeMath.ClipLineToPlaneX(sphereA_v2, AB_v2.normalized, endX * voxelSize);
        //            break;
        //        case C_Axis.z:
        //            clampSphereA = SomeMath.ClipLineToPlaneY(sphereA_v2, AB_v2.normalized, startZ * voxelSize);
        //            clampSphereB = SomeMath.ClipLineToPlaneY(sphereA_v2, AB_v2.normalized, endZ * voxelSize);
        //            break;
        //    }

        //    if (doDebug) {
        //        Debuger_K.AddLine(
        //            new Vector3(clampSphereA.x, 0, clampSphereA.y),
        //            new Vector3(clampSphereB.x, 0, clampSphereB.y), Color.magenta, 0.1f);
        //    }


        //    for (int x = startX_local; x < endX_local; x++) {
        //        for (int z = startZ_local; z < endZ_local; z++) {
        //            intMask[x][z] = 1;
        //        }
        //    }
        //    DDARasterization.DrawLine(
        //        clampSphereA.x - offset.x,
        //        clampSphereA.y - offset.z,
        //        clampSphereB.x - offset.x,
        //        clampSphereB.y - offset.z,
        //        voxelSize,
        //        (int x, int z) => {
        //            x = SomeMath.Clamp(0, volume.sizeX - 1, x);
        //            z = SomeMath.Clamp(0, volume.sizeZ - 1, z);

        //            intMask[x][z] = 2;
        //        });

        //    DDARasterization.DrawLine(
        //        sphereA.x - offset.x,
        //        sphereA.z - offset.z,
        //        sphereB.x - offset.x,
        //        sphereB.z - offset.z,
        //        voxelSize,
        //        (int x, int z) => {
        //            x = SomeMath.Clamp(0, volume.sizeX - 1, x);
        //            z = SomeMath.Clamp(0, volume.sizeZ - 1, z);

        //            intMask[x][z] = 3;
        //        });

        //    switch (alighment) {
        //        case C_Axis.x:
        //            for (int x = startX_local; x < endX_local; x++) {
        //                bool flag = false;
        //                for (int z = startZ_local; z < endZ_local; z++) {
        //                    if (intMask[x][z] == 1) {
        //                        if (flag)
        //                            intMask[x][z] = 4;
        //                        else
        //                            intMask[x][z] = 5;
        //                    }
        //                    else {
        //                        flag = true;
        //                    }
        //                }
        //            }
        //            break;
        //        case C_Axis.z:
        //            for (int z = startZ_local; z < endZ_local; z++) {
        //                bool flag = false;
        //                for (int x = startX_local; x < endX_local; x++) {
        //                    if (intMask[x][z] == 1) {
        //                        if (flag)
        //                            intMask[x][z] = 4;
        //                        else
        //                            intMask[x][z] = 5;
        //                    }
        //                    else {
        //                        flag = true;
        //                    }
        //                }
        //            }
        //            break;
        //    }
        //    if (doDebug) {
        //        for (int x = 0; x < intMask.Length; x++) {
        //            for (int z = 0; z < intMask[x].Length; z++) {
        //                if (intMask[x][z] == 0)
        //                    continue;

        //                Color dCol = Color.gray;

        //                if (intMask[x][z] == 1)
        //                    dCol = Color.white;

        //                if (intMask[x][z] == 2)
        //                    dCol = Color.cyan;

        //                if (intMask[x][z] == 3)
        //                    dCol = Color.blue;

        //                if (intMask[x][z] == 4)
        //                    dCol = Color.green;

        //                if (intMask[x][z] == 5)
        //                    dCol = Color.yellow;

        //                float tx = (x * voxelSize) + offset.x;
        //                float tz = (z * voxelSize) + offset.z;

        //                Debuger_K.AddQuad(
        //                    new Vector3(tx, 0, tz),
        //                    new Vector3(tx, 0, tz + voxelSize),
        //                    new Vector3(tx + voxelSize, 0, tz),
        //                    new Vector3(tx + voxelSize, 0, tz + voxelSize),
        //                    new Color(dCol.r, dCol.g, dCol.b, 0.1f),
        //                    true);

        //                //Debuger_K.AddDot(new Vector3(
        //                //    (x * voxelSize) + offset.x + voxelSizeHalf, 0,
        //                //    (z * voxelSize) + offset.z + voxelSizeHalf), dCol);
        //            }
        //        }
        //    }

        //    maskGenerationTime.Stop();
        //    beforeRasterization.Stop();

        //    //return volume;
        //    if (doDebug) {
        //        //foreach (var item in elipseLines) {
        //        //    Vector3 vp1 = GetValidVector3(item.point1);
        //        //    Vector3 vp2 = GetValidVector3(item.point2);

        //        //    Vector3 mid = SomeMath.MidPoint(vp1, vp2);

        //        //    Debuger_K.AddLine(vp1, mid, Color.green);
        //        //    Debuger_K.AddLine(mid, vp2, Color.blue);

        //        //    Debuger_K.AddLine(vp1 + sphereA, vp2 + sphereA, Color.green);
        //        //}
        //    }


        //    afterRasterization.Start();

        //    if (alighment == C_Axis.z) {
        //        for (int currentZ = startZ; currentZ < endZ; currentZ++) {
        //            Vector3 intersection = SomeMath.ClipLineToPlaneZ(sphereA, AB_normalized, currentZ * voxelSize);
        //            float targetZ = intersection.z;
        //            //if (doDebug) {Debuger_K.AddDot(intersection, Color.red);}
        //            for (int i = 0; i < elipseLines.Count; i++) {
        //                ElipseLine line = elipseLines[i];
        //                //Vector3 p1 = GetValidVector3(line.point1, intersection);
        //                //Vector3 p2 = GetValidVector3(line.point2, intersection);

        //                float p1x = line.point1x + intersection.x;
        //                float p1y = line.point1y + intersection.y;
        //                float p1z = intersection.z;

        //                float p2x = line.point2x + intersection.x;
        //                //float p2y = line.point2y + intersection.y;
        //                //float p2z = intersection.z;

        //                Vector3 curNormalized = GetValidVector3(line.normalized);
        //                float curLength = line.length;
        //                int pass = line.passability;

        //                int sX = (int)(p1x / voxelSize);
        //                int eX = (int)(p2x / voxelSize) + 1;

        //                //Vector3 m = SomeMath.MidPoint(p1, p2);
        //                //Debuger_K.AddLine(p1, m, Color.blue);
        //                //Debuger_K.AddLine(p2, m, Color.red);

        //                for (int currentX = sX; currentX < eX; currentX++) {
        //                    if (SomeMath.InRangeInclusive(currentX, startX, endX)) {
        //                        int vx = currentX - template.startX_extra;
        //                        int vz = currentZ - template.startZ_extra;

        //                        float actualX;
        //                        switch (intMask[vx][vz]) {
        //                            case 3:
        //                                actualX = currentX * voxelSize + voxelSizeHalf;
        //                                //if (doDebug) Debuger_K.AddDot(new Vector3(actualX, 0, currentZ * voxelSize + voxelSizeHalf), Color.blue, 0.01f);
        //                                break;
        //                            case 4:
        //                                actualX = currentX * voxelSize;
        //                                //if (doDebug) Debuger_K.AddDot(new Vector3(actualX, 0, currentZ * voxelSize + voxelSizeHalf), Color.green, 0.01f);
        //                                break;
        //                            case 5:
        //                                actualX = currentX * voxelSize + voxelSize;
        //                                //if (doDebug) Debuger_K.AddDot(new Vector3(actualX, 0, currentZ * voxelSize + voxelSizeHalf), Color.yellow, 0.01f);
        //                                break;
        //                            default:
        //                                actualX = currentX * voxelSize;
        //                                break;
        //                        }

        //                        float dx = (actualX - p1x) / curNormalized.x;

        //                        if (dx >= 0f && dx <= curLength) {
        //                            float targetY = p1y + (curNormalized.y * dx);
        //                            //Vector3 awesome = SomeMath.ClipLineToPlaneX(p1, curNormalized, currentX * voxelSize);
        //                            //Vector3 resultVector = new Vector3(actualX, targetY, targetZ);
        //                            //Debuger_K.AddDot(resultVector);

        //                            VolumeSetTime.Start();
        //                            if (System.Math.Sign(SomeMath.Dot(AB.x, AB.y, AB.z, actualX - sphereA.x, targetY - sphereA.y, targetZ - sphereA.z)) !=
        //                                System.Math.Sign(SomeMath.Dot(AB.x, AB.y, AB.z, actualX - sphereB.x, targetY - sphereB.y, targetZ - sphereB.z))) {
        //                                if (pass == -1)
        //                                    volume.SetVoxel(vx, vz, targetY);
        //                                else
        //                                    volume.SetVoxel(vx, vz, targetY, pass);
        //                            }
        //                            VolumeSetTime.Stop();
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else
        //    if (alighment == C_Axis.x) {
        //        for (int currentX = startX; currentX < endX; currentX++) {
        //            Vector3 intersection = SomeMath.ClipLineToPlaneX(sphereA, AB_normalized, currentX * voxelSize + voxelSizeHalf);
        //            float targetX = intersection.x;
        //            //if (doDebug) {Debuger_K.AddDot(intersection, Color.red);}
        //            for (int i = 0; i < elipseLines.Count; i++) {
        //                ElipseLine line = elipseLines[i];
        //                //Vector3 p1 = GetValidVector3(line.point1, intersection);
        //                //Vector3 p2 = GetValidVector3(line.point2, intersection);

        //                float p1x = intersection.x;
        //                float p1y = line.point1x + intersection.y;
        //                float p1z = line.point1y + intersection.z;

        //                //float p2x = intersection.x;
        //                //float p2y = line.point2x + intersection.y;
        //                float p2z = line.point2y + intersection.z;


        //                Vector3 curNormalized = GetValidVector3(line.normalized);
        //                float curLength = line.length;
        //                int pass = line.passability;

        //                //int sZ = (int)(p1.z / voxelSize) - 1;
        //                //int eZ = (int)(p2.z / voxelSize);

        //                int sZ = (int)(p1z / voxelSize) - 1;
        //                int eZ = (int)(p2z / voxelSize);

        //                //Vector3 m = SomeMath.MidPoint(p1, p2);
        //                //Debuger_K.AddLine(p1, m, Color.blue);
        //                //Debuger_K.AddLine(p2, m, Color.red);

        //                for (int currentZ = sZ; currentZ < eZ; currentZ++) {
        //                    if (SomeMath.InRangeInclusive(currentZ, startZ, endZ)) {
        //                        int vx = currentX - template.startX_extra;
        //                        int vz = currentZ - template.startZ_extra;

        //                        float actualZ;
        //                        switch (intMask[vx][vz]) {
        //                            case 3:
        //                                actualZ = currentZ * voxelSize + voxelSizeHalf;
        //                                //if(doDebug)Debuger_K.AddDot(new Vector3(currentX * voxelSize + voxelSizeHalf, 0, actualZ), Color.blue, 0.01f);
        //                                break;
        //                            case 4:
        //                                actualZ = currentZ * voxelSize;
        //                                //if(doDebug)Debuger_K.AddDot(new Vector3(currentX * voxelSize + voxelSizeHalf, 0, actualZ), Color.green, 0.01f);
        //                                break;
        //                            case 5:
        //                                actualZ = currentZ * voxelSize + voxelSize;
        //                                //if(doDebug)Debuger_K.AddDot(new Vector3(currentX * voxelSize + voxelSizeHalf, 0, actualZ), Color.yellow, 0.01f);
        //                                break;
        //                            default:
        //                                actualZ = currentZ * voxelSize;
        //                                break;
        //                        }

        //                        float dz = (actualZ - p1z) / curNormalized.z;
        //                        if (dz >= 0f && dz <= curLength) {
        //                            float targetY = p1y + (curNormalized.y * dz);

        //                            //Vector3 awesome = SomeMath.ClipLineToPlaneZ(p1, curNormalized, currentZ * voxelSize);
        //                            //Vector3 resultVector = new Vector3(targetX, targetY, actualZ);
        //                            //Debuger_K.AddDot(resultVector, Color.yellow);
        //                            //Debuger_K.AddLabel(resultVector, dz);

        //                            VolumeSetTime.Start();
        //                            if (System.Math.Sign(SomeMath.Dot(AB.x, AB.y, AB.z, targetX - sphereA.x, targetY - sphereA.y, actualZ - sphereA.z)) !=
        //                                System.Math.Sign(SomeMath.Dot(AB.x, AB.y, AB.z, targetX - sphereB.x, targetY - sphereB.y, actualZ - sphereB.z))) {
        //                                if (pass == -1)
        //                                    volume.SetVoxel(vx, vz, targetY);
        //                                else
        //                                    volume.SetVoxel(vx, vz, targetY, pass);
        //                            }
        //                            VolumeSetTime.Stop();
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    if (sphereA.y == sphereB.y) {
        //        ShapeDataHelperSphereCollector.CollectStaticApplyPassability(sphereA, capsileRadius, volume, collector.template);
        //        ShapeDataHelperSphereCollector.CollectStaticApplyPassability(sphereB, capsileRadius, volume, collector.template);
        //    }
        //    else if (sphereA.y > sphereB.y) {
        //        ShapeDataHelperSphereCollector.CollectStaticApplyPassability(sphereA, capsileRadius, volume, collector.template);
        //        ShapeDataHelperSphereCollector.CollectStaticApplyPassabilityPassive(sphereB, capsileRadius, volume, collector.template);
        //    }
        //    else if (sphereA.y < sphereB.y) {
        //        ShapeDataHelperSphereCollector.CollectStaticApplyPassabilityPassive(sphereA, capsileRadius, volume, collector.template);
        //        ShapeDataHelperSphereCollector.CollectStaticApplyPassability(sphereB, capsileRadius, volume, collector.template);
        //    }

        //    afterRasterization.Stop();
        //    totalTime.Stop();

        //    Debug.Log("New: " + totalTime.Elapsed);
        //    Debug.Log("Before Rasterization: " + beforeRasterization.Elapsed);
        //    Debug.Log("Rasterization: " + afterRasterization.Elapsed);
        //    Debug.Log("Set to volume " + VolumeSetTime.Elapsed);
        //    Debug.Log("Clip Line " + clipLineXTime.Elapsed);
        //    Debug.Log("Mask time " + maskGenerationTime.Elapsed);

        //    Vector3 chunkReal = template.realOffsetedPosition;
        //    return volume;
        //}

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
    }
}