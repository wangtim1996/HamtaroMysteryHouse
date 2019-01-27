using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif
 

//namespace K_PathFinder {
//    public class ColliderCollectorPrimitivesCPU : ColliderCollectorPrimitivesAbstract {
//        static ColliderCollectorPrimitivesCPU() {
//            //to make shure it's loaded
//            ColliderCollectorPrimitivesAbstract.EmptyStaticMethodToShureBaseStaticAreLOaded();
//        }


//        //construtor are called in main thread but execute Collect(FragmentContainer) in own thread
//        public ColliderCollectorPrimitivesCPU(NavMeshTemplateCreation template) : base(template) {}       

//        //threaded
//        public override void Collect(VolumeContainer container) {
//            Debug.LogError("this code no longer used bug you still get here");
//            //            float maxSlopeCos = Mathf.Cos(template.maxSlope * Mathf.PI / 180f);

//            //            //oh just rewrite this later this list is bad thing
//            //            List<Volume> solid = new List<Volume>();
//            //            List<VolumeSimple> modifyArea = new List<VolumeSimple>();
//            //            List<VolumeSimple> makeHole = new List<VolumeSimple>();

//            //            List<Vector3> verts = new List<Vector3>();

//            //            foreach (var colliderTemplate in templates) {
//            //                if (profiler != null) profiler.AddLogFormat("Current primitive: {0}", colliderTemplate.name);
//            //                Area area = colliderTemplate.area;

//            //                Vector3[] templateVerts = colliderTemplate.verts;
//            //                verts.Clear();
//            //                //Vector3[] verts = new Vector3[templateVerts.Length];
//            //                Matrix4x4 matrix = colliderTemplate.matrix;
//            //                for (int i = 0; i < templateVerts.Length; i++) {
//            //                    verts.Add(matrix.MultiplyPoint3x4(templateVerts[i]));
//            //                }
//            //                int[] tris = colliderTemplate.tris;
//            //                Vector3 A, B, C;

//            //                if (colliderTemplate.infoMode == ColliderInfoMode.Solid) {
//            //                    #region normal collection of solid volumes
//            //                    Volume volume = new Volume(template.lengthX_extra, template.lengthZ_extra, area);

//            //                    volume.tag = colliderTemplate.tag;
//            //                    volume.layer = colliderTemplate.layer;

//            //                    for (int t = 0; t < colliderTemplate.tris.Length; t += 3) {
//            //                        A = verts[tris[t]];
//            //                        B = verts[tris[t + 1]];
//            //                        C = verts[tris[t + 2]];

//            //                        bool unwalkableBySlope = !CalculateWalk(A, B, C, maxSlopeCos);
//            //                        Passability currentPassability;

//            //                        if (area.id == 1)//id of clear Area all time
//            //                            currentPassability = Passability.Unwalkable;
//            //                        else if (unwalkableBySlope)
//            //                            currentPassability = Passability.Slope;
//            //                        else
//            //                            currentPassability = Passability.Walkable;

//            //#if UNITY_EDITOR
//            //                        if (currentPassability > Passability.Slope && Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false)
//            //                            Debuger_K.AddWalkablePolygon(template.gridPosX, template.gridPosZ, template.properties, A, B, C);
//            //#endif

//            //                        base.RasterizeTriangle(
//            //                            volume, A, B, C,
//            //                            template.voxelSize,
//            //                            template.startX_extra,
//            //                            template.endX_extra,
//            //                            template.startZ_extra,
//            //                            template.endZ_extra,
//            //                            area, currentPassability);
//            //                    }
//            //                    solid.Add(volume);
//            //                    #endregion
//            //                }
//            //                else{
//            //                    #region other collection
//            //                    VolumeSimple volume = VolumeSimple.GetFromPool(template.lengthX_extra, template.lengthZ_extra);
//            //                    volume.area = area;
//            //                    volume.mode = colliderTemplate.infoMode;                    
//            //                    volume.priority = colliderTemplate.priority;
//            //                    volume.filter = colliderTemplate.filter;
//            //                    volume.tag = colliderTemplate.tag;
//            //                    volume.layer = colliderTemplate.layer;

//            //                    for (int t = 0; t < colliderTemplate.tris.Length; t += 3) {
//            //                        A = verts[tris[t]];
//            //                        B = verts[tris[t + 1]];
//            //                        C = verts[tris[t + 2]];

//            //                        bool unwalkableBySlope = !CalculateWalk(A, B, C, maxSlopeCos, true);
//            //                        Passability currentPassability;

//            //                        if (area.id == 1)//id of clear Area all time
//            //                            currentPassability = Passability.Unwalkable;
//            //                        else if (unwalkableBySlope)
//            //                            currentPassability = Passability.Slope;
//            //                        else
//            //                            currentPassability = Passability.Walkable;

//            //#if UNITY_EDITOR
//            //                        if (currentPassability > Passability.Slope && Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false)
//            //                            Debuger_K.AddWalkablePolygon(template.gridPosX, template.gridPosZ, template.properties, A, B, C);
//            //#endif

//            //                        base.RasterizeTriangle(
//            //                            volume, A, B, C,
//            //                            template.voxelSize,
//            //                            template.startX_extra,
//            //                            template.endX_extra,
//            //                            template.startZ_extra,
//            //                            template.endZ_extra,
//            //                            currentPassability);
//            //                    }

//            //                    if (colliderTemplate.infoMode == ColliderInfoMode.MakeHoleApplyArea | colliderTemplate.infoMode == ColliderInfoMode.MakeHoleRetainArea)
//            //                        makeHole.Add(volume);
//            //                    else if(colliderTemplate.infoMode == ColliderInfoMode.ModifyArea)
//            //                        modifyArea.Add(volume);
//            //                    #endregion
//            //                }    
//            //            }

//            //            foreach (var item in solid) {
//            //                container.AddSolidVolume(item);
//            //            }
//        }
//    }
//}

