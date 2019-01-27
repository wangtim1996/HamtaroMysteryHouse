using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using K_PathFinder.Rasterization;

//namespace K_PathFinder {
//    public class ColliderCollectorPrimitivesComputeShader : ColliderCollectorPrimitivesAbstract {
//        List<RData> templatesAfterComputeShader = new List<RData>();

//        static ColliderCollectorPrimitivesComputeShader() {
//            //to make shure it's loaded
//            ColliderCollectorPrimitivesAbstract.EmptyStaticMethodToShureBaseStaticAreLOaded();
//        }

//        public ColliderCollectorPrimitivesComputeShader(NavMeshTemplateCreation template) : base(template) {}

//        //in main thread
//        //generate voxels
//        public void CollectUsingComputeShader() {
//            float maxSlopeCos = Mathf.Cos(template.maxSlope * Mathf.PI / 180f);

//            for (int i = 0; i < templates.Count; i++) {
//                MeshColliderInfo curInfo = templates[i];
//                Vector3 offsetedPos = template.realOffsetedPosition;

//                bool flipY = curInfo.infoMode != ColliderInfoMode.Solid;

//                CSRasterization3DResult result = PathFinder.sceneInstance.Rasterize3D(
//                    curInfo.verts,
//                    curInfo.tris,
//                    curInfo.bounds,
//                    curInfo.matrix,
//                    template.lengthX_extra, template.lengthZ_extra,
//                    offsetedPos.x, offsetedPos.z, 
//                    template.voxelSize,
//                    maxSlopeCos,
//                    flipY, 
//                    false);

//                if(result != null)
//                    templatesAfterComputeShader.Add(new RData(curInfo, result));
//            }
//        }
        
//        //in not main thread
//        //read voxels to volume
//        public override void Collect(VolumeContainer container) {
//            if (templatesAfterComputeShader == null) {
//                Debug.LogWarning("expecting to recive things from compute shader but list was null");
//                return;
//            }

//            //List<Volume> solid = new List<Volume>();
//            //List<VolumeSimple> modifyArea = new List<VolumeSimple>();
//            //List<VolumeSimple> makeHole = new List<VolumeSimple>();

//            //Debug.Log(templatesAfterComputeShader.Count);

//            for (int i = 0; i < templatesAfterComputeShader.Count; i++) {
//                var cur = templatesAfterComputeShader[i];
//                var info = cur.info;

//                VolumeSimple volume = VolumeSimple.GetFromPool(template.lengthX_extra, template.lengthZ_extra);
//                volume.area = info.area;
//                volume.infoMode = info.infoMode;
//                //volume.priority = info.priority;
//                //volume.filter = info.filter;
//                //volume.tag = info.tag;
//                //volume.layer = info.layer;
//                cur.result.Read(volume);

//                container.AddSimpleVolume(volume);
//            }
//        }

//        private class RData {
//            public MeshColliderInfo info;
//            public CSRasterization3DResult result;

//            public RData(MeshColliderInfo info, CSRasterization3DResult result) {
//                this.info = info;
//                this.result = result;
//            }
//        }
//    }
//}
