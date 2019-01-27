using K_PathFinder.VectorInt;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//namespace K_PathFinder {
//    public abstract class ColliderCollectorTerrainMeshAbstract : TerrainCollectorAbstract {
//        protected List<TerrainColliderInfoMesh> terrainsInfo = new List<TerrainColliderInfoMesh>();

//        public ColliderCollectorTerrainMeshAbstract(NavMeshTemplateCreation template) : base(template) {}

//        public override void AddColliders(Collider[] colliders) {
//            List<Terrain> allTerrains = new List<Terrain>();
//            for (int i = 0; i < colliders.Length; i++) {
//                if (IsValid(colliders[i]))
//                    allTerrains.Add(colliders[i].GetComponent<Terrain>());
//            }

//            if (template.profiler != null)
//                template.profiler.AddLog("collecting terrain using fast collector. valid colliders:" + allTerrains.Count);

//            float voxelSize = template.voxelSize;
//            Bounds offsetedBounds = template.chunkOffsetedBounds;
//            Vector3 boundsMin = offsetedBounds.min;
//            Vector3 boundsMax = offsetedBounds.max;

//            float minSize = PathFinder.settings.terrainFastMinimalSize;

//            foreach (var curTerrain in allTerrains) {
//                TerrainColliderInfoMesh info = new TerrainColliderInfoMesh(curTerrain);

//                if (template.profiler != null)
//                    template.profiler.AddLog("start collecting tree bounds");

//                //info.treeData = GenerateTreeBounds(curTerrain);


//                CollectTreeData(curTerrain);

//                //if (template.profiler != null)
//                //    template.profiler.AddLog("end collecting tree bounds. collected bounds: " + info.treeData.Count);

//                TerrainData data = curTerrain.terrainData;
//                Vector3 position = curTerrain.transform.position;
//                Vector3 scale = data.size;

//                //height map
//                int resolution = 1;

//                int heightMapSizeX = data.heightmapWidth;
//                int heightMapSizeZ = data.heightmapHeight;

//                float hScaleX = scale.x / (heightMapSizeX - 1);
//                float hScaleZ = scale.z / (heightMapSizeZ - 1);

//                for (int i = 0; i < 4; i++) {
//                    if (minSize > hScaleX * resolution)
//                        resolution = (int)Mathf.Pow(2, i);
//                    else
//                        break;
//                }

//                int hTargetMinX = Mathf.Clamp(((Mathf.RoundToInt((boundsMin.x - position.x) / hScaleX) / resolution) - 1) * resolution, 0, heightMapSizeX);
//                int hTargetMinZ = Mathf.Clamp(((Mathf.RoundToInt((boundsMin.z - position.z) / hScaleZ) / resolution) - 1) * resolution, 0, heightMapSizeX);

//                int hTargetMaxX = Mathf.Clamp(((Mathf.RoundToInt((boundsMax.x - position.x) / hScaleX) / resolution) + 1) * resolution + 1, 0, heightMapSizeX);
//                int hTargetMaxZ = Mathf.Clamp(((Mathf.RoundToInt((boundsMax.z - position.z) / hScaleZ) / resolution) + 1) * resolution + 1, 0, heightMapSizeX);

//                int hSizeX = hTargetMaxX - hTargetMinX;
//                int hSizeZ = hTargetMaxZ - hTargetMinZ;

//                int hTargetSizeX = hSizeX / resolution + 1;
//                int hTargetSizeZ = hSizeZ / resolution + 1;

//                if (resolution == 1) {
//                    hTargetSizeX = hSizeX;
//                    hTargetSizeZ = hSizeZ;
//                }

//                info.heightMap = data.GetHeights(hTargetMinX, hTargetMinZ, hSizeX, hSizeZ);
//                info.heightMatrix = Matrix4x4.TRS(
//                    position + new Vector3(hScaleX * hTargetMinX, 0, hScaleZ * hTargetMinZ),
//                    Quaternion.identity,
//                    new Vector3(hScaleX * resolution, scale.y, hScaleZ * resolution));

//                info.hSizeX = hTargetSizeX;
//                info.hSizeZ = hTargetSizeZ;
//                info.resolution = resolution;

//                //rest
//                VectorInt.Vector3Int terrainStartInt = new VectorInt.Vector3Int((position / voxelSize) + template.halfVoxelOffset);
//                VectorInt.Vector3Int terrainEndInt = new VectorInt.Vector3Int((position + data.size) / voxelSize + template.halfVoxelOffset);

//                int startXClamp = Mathf.Clamp(terrainStartInt.x, template.startX_extra, template.endX_extra);
//                int startZClamp = Mathf.Clamp(terrainStartInt.z, template.startZ_extra, template.endZ_extra);

//                int endXClamp = Mathf.Clamp(terrainEndInt.x, template.startX_extra, template.endX_extra);
//                int endZClamp = Mathf.Clamp(terrainEndInt.z, template.startZ_extra, template.endZ_extra);

//                int terrainStartX = terrainStartInt.x;
//                int terrainStartZ = terrainStartInt.z;

//                float terrainSizeX = terrainEndInt.x - terrainStartX;
//                float terrainSizeZ = terrainEndInt.z - terrainStartZ;

//                SetTerrainSettings(info, curTerrain,
//                    startXClamp, startZClamp,
//                    endXClamp, endZClamp,
//                    terrainStartX, terrainStartZ,
//                    terrainSizeX, terrainSizeZ,
//                    position);


//                terrainsInfo.Add(info);
//            }
//        }

//        protected void GetTerrainMesh(TerrainColliderInfoMesh terrain, out Vector3[] verts, out int[] tris) {
//            int hSizeX = terrain.hSizeX;
//            int hSizeZ = terrain.hSizeZ;
//            int resolution = terrain.resolution;
//            float[,] heightMap = terrain.heightMap;
//            Matrix4x4 heightMatrix = terrain.heightMatrix;

//            verts = new Vector3[hSizeX * hSizeZ];
//            tris = new int[(hSizeX - 1) * (hSizeZ - 1) * 6];

//            for (int x = 0; x < hSizeX; x++) {
//                for (int z = 0; z < hSizeZ; z++) {
//                    verts[z * hSizeX + x] = heightMatrix.MultiplyPoint3x4(new Vector3(x, heightMap[z * resolution, x * resolution], z));
//                }
//            }

//            var index = 0;
//            for (int y = 0; y < hSizeZ - 1; y++) {
//                for (int x = 0; x < hSizeX - 1; x++) {
//                    tris[index++] = (y * hSizeX) + x;
//                    tris[index++] = ((y + 1) * hSizeX) + x + 1;
//                    tris[index++] = (y * hSizeX) + x + 1;

//                    tris[index++] = (y * hSizeX) + x;
//                    tris[index++] = ((y + 1) * hSizeX) + x;
//                    tris[index++] = ((y + 1) * hSizeX) + x + 1;
//                }
//            }
//        }
//    }
//}