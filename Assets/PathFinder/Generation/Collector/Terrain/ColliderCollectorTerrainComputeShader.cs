using K_PathFinder.Rasterization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//namespace K_PathFinder {
//    public class ColliderCollectorTerrainComputeShader : ColliderCollectorTerrainMeshAbstract {
//        private List<TerrainInfoCSR> _collectedTerrainUsingComputeShader = new List<TerrainInfoCSR>();

//        public ColliderCollectorTerrainComputeShader(NavMeshTemplateCreation template) : base(template) {}


//        //public override void AddColiders(Collider[] colliders) {
//        //    throw new NotImplementedException();
//        //}

//        public override int collectedCount {
//            get { return _collectedTerrainUsingComputeShader.Count; }
//        }

//        public void CollectUsingComputeShader() { 
//            float maxSlopeCos = Mathf.Cos((float)((double)template.maxSlope * Math.PI / 180.0));
//            int vSizeX = template.lengthX_extra;
//            int vSizeZ = template.lengthZ_extra;

//            Vector3 realChunkPos = template.realOffsetedPosition;
//            float chunkPosX = realChunkPos.x;
//            float chunkPosZ = realChunkPos.z;

//            foreach (var terrain in terrainsInfo) {
//                //int hSizeX = terrain.hSizeX;
//                //int hSizeZ = terrain.hSizeZ;
//                //int resolution = terrain.resolution;
//                //float[,] heightMap = terrain.heightMap;
//                //Matrix4x4 heightMatrix = terrain.heightMatrix;

//                Vector3[] verts;
//                int[] tris;
//                base.GetTerrainMesh(terrain, out verts, out tris);

//                //Volume terrainVolume;
//                //if (terrain.alphaMap != null)
//                //    terrainVolume = new Volume(template.lengthX_extra, template.lengthZ_extra, terrain.possibleArea);
//                //else
//                //    terrainVolume = new Volume(template.lengthX_extra, template.lengthZ_extra, defaultArea);

//                //terrainVolume.terrain = true;

            
//                CSRasterization2DResult resultTerrain = PathFinder.sceneInstance.Rasterize2D(verts, tris, vSizeX, vSizeZ, chunkPosX, chunkPosZ, template.voxelSize, maxSlopeCos);


//                CSRasterization3DResult[] resultTrees = null;

//                //List<Bounds> treeData = terrain.treeData;

//                Debug.LogError("Developer forgot to fix compute shader trees collection");

//                //if (treeData != null && treeData.Count > 0) {
//                //    resultTrees = new CSRasterization3DResult[treeData.Count];

//                //    for (int i = 0; i < treeData.Count; i++) {
//                //        Bounds bound = treeData[i];
//                //        Matrix4x4 m = Matrix4x4.TRS(bound.center, Quaternion.identity, new Vector3(bound.size.x, bound.size.y * 0.5f, bound.size.z));             
//                //        resultTrees[i] = PathFinder.sceneInstance.Rasterize3D(fancyVerts, fancyTris, bound, m, vSizeX, vSizeZ, chunkPosX, chunkPosZ, template.voxelSize, maxSlopeCos, false, false);
//                //    }     
//                //}
//                _collectedTerrainUsingComputeShader.Add(new TerrainInfoCSR(terrain, resultTerrain, resultTrees));
//            }
//        }


//        public override void Collect(VolumeContainer container) { 
//            Area defaultArea = PathFinder.GetArea(0);
//            //Area clearArea = PathFinder.GetArea(1);

//            for (int i = 0; i < _collectedTerrainUsingComputeShader.Count; i++) {
//                TerrainInfoCSR info = _collectedTerrainUsingComputeShader[i];
//                TerrainColliderInfoMesh terrainInfo = info.colliderInfo;

//                Volume terrainVolume;
//                if (terrainInfo.alphaMap != null)
//                    terrainVolume = Volume.GetFromPool(template.lengthX_extra, template.lengthZ_extra, terrainInfo.possibleArea);
//                else
//                    terrainVolume = Volume.GetFromPool(template.lengthX_extra, template.lengthZ_extra, defaultArea);

//                terrainVolume.isTerrain = true;
//                info.voxelsTerrain.Read(terrainVolume);
//                terrainVolume.SetVolumeMinimum(-1000f);
//                SetTerrainArea(terrainVolume, terrainInfo, defaultArea); //apply terrain area info if it exist

//                //if(info.voxelsTrees != null) {
//                //    //trees
//                //    Volume treeVolume = new Volume(template.lengthX_extra, template.lengthZ_extra, defaultArea);
//                //    treeVolume.SetVolumeMinimum(-1000f);
//                //    foreach (var treeVoxls in info.voxelsTrees) {
//                //        if(treeVoxls != null)
//                //            treeVoxls.Read(treeVolume, clearArea);
//                //    }
                    
//                //    //connecting terrain and trees to single volume
//                //    terrainVolume.Subtract(treeVolume);
//                //    terrainVolume.ConnectToItself();
//                //    terrainVolume.Override(treeVolume);
//                //}
//                //else {
//                //    terrainVolume.ConnectToItself();
//                //}

//                container.AddTerrainVolumes(terrainVolume, null);
//            }
//        }


//        private class TerrainInfoCSR {
//            public TerrainColliderInfoMesh colliderInfo;
//            public CSRasterization2DResult voxelsTerrain;
//            public CSRasterization3DResult[] voxelsTrees;

//            public TerrainInfoCSR(TerrainColliderInfoMesh colliderInfo, CSRasterization2DResult voxelsTerrain, CSRasterization3DResult[] voxelsTrees) {
//                this.colliderInfo = colliderInfo;
//                this.voxelsTerrain = voxelsTerrain;
//                this.voxelsTrees = voxelsTrees;
//            }
//        }
//    }
//}
