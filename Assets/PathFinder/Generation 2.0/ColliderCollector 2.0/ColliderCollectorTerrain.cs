using K_PathFinder.Collector;
using K_PathFinder.Rasterization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace K_PathFinder {
    public partial class ColliderCollector {
        struct TerrainResultGPU {
            public CSRasterization2DResult result;
            public TerrainColliderInfoMesh info;
        }

        List<TerrainResultGPU> terrainGPUResuts = new List<TerrainResultGPU>();
        List<TerrainColliderInfoMesh> terrainsInfoForCPU = new List<TerrainColliderInfoMesh>();

        private void CollectTerrainOnGPU(TerrainColliderInfoMesh terrain) {
            float maxSlopeCos = Mathf.Cos((float)((double)template.maxSlope * Mathf.PI / 180.0));
            int vSizeX = template.lengthX_extra;
            int vSizeZ = template.lengthZ_extra;

            Vector3 realChunkPos = template.realOffsetedPosition;
            float chunkPosX = realChunkPos.x;
            float chunkPosZ = realChunkPos.z;

            Vector3[] verts;
            int[] tris;
            GetTerrainMesh(terrain, out verts, out tris);

            terrainGPUResuts.Add(new TerrainResultGPU() {
                result = PathFinder.sceneInstance.Rasterize2D(verts, tris, vSizeX, vSizeZ, chunkPosX, chunkPosZ, template.voxelSize, maxSlopeCos),
                info = terrain
            });

            Pool.GenericPoolArray<Vector3>.ReturnToPool(ref verts);
            Pool.GenericPoolArray<int>.ReturnToPool(ref tris);
            //Debug.LogWarning("Developer forgot to fix compute shader trees collection");
        }


        private void CollectTerrainGPU(Collector3.ShapeCollector shape) {
            foreach (var terrainGPU in terrainGPUResuts) {
                if (terrainGPU.info.alphaMap != null) {
                    byte[] areaSplatMap = Pool.GenericPoolArray<byte>.Take(shape.flattenSize);
                    GenerateTerrainAreaMapFromSplatMap(terrainGPU.info, shape, ref areaSplatMap);
                    shape.AppendComputeShaderResult(terrainGPU.result, areaSplatMap);
                    Pool.GenericPoolArray<byte>.ReturnToPool(ref areaSplatMap);
                }
                else {
                    shape.AppendComputeShaderResult(terrainGPU.result, 0);
                }

                foreach (var tree in terrainGPU.info.trees) {
                    shape.Append(tree);
                }
            }
        }

        private void CollectTerrainCPU(Collector3.ShapeCollector shape) {
            foreach (TerrainColliderInfoMesh terrainInfo in terrainsInfoForCPU) {
                Vector3[] verts;
                int[] tris;
                GetTerrainMesh(terrainInfo, out verts, out tris);

                if (terrainInfo.alphaMap == null) {
                    shape.AppendTerrain(verts, tris, PathFinder.getDefaultArea);
                }
                else {
                    byte[] areaSplatMap = Pool.GenericPoolArray<byte>.Take(shape.flattenSize);
                    GenerateTerrainAreaMapFromSplatMap(terrainInfo, shape, ref areaSplatMap);
                    shape.AppendTerrain(verts, tris, areaSplatMap);
                    Pool.GenericPoolArray<byte>.ReturnToPool(ref areaSplatMap);                 
                }

                Pool.GenericPoolArray<Vector3>.ReturnToPool(ref verts);
                Pool.GenericPoolArray<int>.ReturnToPool(ref tris);

                foreach (var tree in terrainInfo.trees) {
                    shape.Append(tree);
                }
            }
        }

        private void AddColliderTerrain(Collider collider, TerrainCollectorType terrainCollectionType) {
            Terrain terrain = collider.GetComponent<Terrain>();

            if (terrain == null | terrain.enabled == false)
                return;

            if (profiler != null) profiler.AddLogFormat("collecting terrain {0}", terrain.gameObject.name);

            TerrainColliderInfoMesh info = new TerrainColliderInfoMesh(terrain);

            info.trees = CollectTreeData(terrain);

            //general stuff
            float voxelSize = template.voxelSize;
            Bounds offsetedBounds = template.chunkOffsetedBounds;
            Vector3 boundsMin = offsetedBounds.min;
            Vector3 boundsMax = offsetedBounds.max;

            float minSize = PathFinder.settings.terrainFastMinimalSize;

            //terrain stuff
            TerrainData data = terrain.terrainData;
            Vector3 position = terrain.transform.position;
            Vector3 scale = data.size;

            //height map
            int resolution = 1;

            int heightMapSizeX = data.heightmapWidth;
            int heightMapSizeZ = data.heightmapHeight;

            float hScaleX = scale.x / (heightMapSizeX - 1);
            float hScaleZ = scale.z / (heightMapSizeZ - 1);

            for (int i = 0; i < 4; i++) {
                if (minSize > hScaleX * resolution)
                    resolution = (int)Mathf.Pow(2, i);
                else
                    break;
            }

            int hTargetMinX = Mathf.Clamp(((Mathf.RoundToInt((boundsMin.x - position.x) / hScaleX) / resolution) - 1) * resolution, 0, heightMapSizeX);
            int hTargetMinZ = Mathf.Clamp(((Mathf.RoundToInt((boundsMin.z - position.z) / hScaleZ) / resolution) - 1) * resolution, 0, heightMapSizeX);

            int hTargetMaxX = Mathf.Clamp(((Mathf.RoundToInt((boundsMax.x - position.x) / hScaleX) / resolution) + 1) * resolution + 1, 0, heightMapSizeX);
            int hTargetMaxZ = Mathf.Clamp(((Mathf.RoundToInt((boundsMax.z - position.z) / hScaleZ) / resolution) + 1) * resolution + 1, 0, heightMapSizeX);

            int hSizeX = hTargetMaxX - hTargetMinX;
            int hSizeZ = hTargetMaxZ - hTargetMinZ;

            int hTargetSizeX = hSizeX / resolution + 1;
            int hTargetSizeZ = hSizeZ / resolution + 1;

            if (resolution == 1) {
                hTargetSizeX = hSizeX;
                hTargetSizeZ = hSizeZ;
            }


            info.heightMap = data.GetHeights(hTargetMinX, hTargetMinZ, hSizeX, hSizeZ);
            info.heightMatrix = Matrix4x4.TRS(
                position + new Vector3(hScaleX * hTargetMinX, 0, hScaleZ * hTargetMinZ),
                Quaternion.identity,
                new Vector3(hScaleX * resolution, scale.y, hScaleZ * resolution));

            info.hSizeX = hTargetSizeX;
            info.hSizeZ = hTargetSizeZ;
            info.resolution = resolution;

            //rest
            VectorInt.Vector3Int terrainStartInt = new VectorInt.Vector3Int((position / voxelSize) + template.halfVoxelOffset);
            VectorInt.Vector3Int terrainEndInt = new VectorInt.Vector3Int((position + data.size) / voxelSize + template.halfVoxelOffset);

            int startXClamp = Mathf.Clamp(terrainStartInt.x, template.startX_extra, template.endX_extra);
            int startZClamp = Mathf.Clamp(terrainStartInt.z, template.startZ_extra, template.endZ_extra);

            int endXClamp = Mathf.Clamp(terrainEndInt.x, template.startX_extra, template.endX_extra);
            int endZClamp = Mathf.Clamp(terrainEndInt.z, template.startZ_extra, template.endZ_extra);

            int terrainStartX = terrainStartInt.x;
            int terrainStartZ = terrainStartInt.z;

            float terrainSizeX = terrainEndInt.x - terrainStartX;
            float terrainSizeZ = terrainEndInt.z - terrainStartZ;

            var navmeshSettings = terrain.gameObject.GetComponent<TerrainNavmeshSettings>();

            if (navmeshSettings != null && navmeshSettings.isActiveAndEnabled && navmeshSettings.data.Any(x => x != 0)) {//0 is default so if there is settings full of deffault areas then dont need that    
                Vector3 size = data.size;
                info.settings = navmeshSettings;
                info.startXClamp = startXClamp;
                info.startZClamp = startZClamp;

                info.endXClamp = endXClamp;
                info.endZClamp = endZClamp;

                info.terrainStartX = terrainStartX;
                info.terrainStartZ = terrainStartZ;

                info.terrainSizeX = terrainSizeX;
                info.terrainSizeZ = terrainSizeZ;

                //all this values needed in 2 places. here to use data.GetAlphamaps in main thread and later in not main thread
                //so we just store it in terrain collider info
                info.alphaWidth = data.alphamapWidth;
                info.alphaHeight = data.alphamapHeight;

                //normalized start and end
                //dont needed later
                float terNormStartX = Mathf.Clamp01((template.chunkData.realX - template.properties.radius - position.x) / size.x);
                float terNormStartZ = Mathf.Clamp01((template.chunkData.realZ - template.properties.radius - position.z) / size.z);

                float terNormEndX = Mathf.Clamp01((template.chunkData.realX + PathFinder.gridSize + template.properties.radius - position.x) / size.x);
                float terNormEndZ = Mathf.Clamp01((template.chunkData.realZ + PathFinder.gridSize + template.properties.radius - position.z) / size.z);

                //alpha map position of chunk
                info.alphaStartX = Mathf.RoundToInt(terNormStartX * info.alphaWidth);
                info.alphaStartZ = Mathf.RoundToInt(terNormStartZ * info.alphaHeight);

                //alpha map size of chunk
                //size needed only now
                info.alphaSizeX = Mathf.Min(Mathf.RoundToInt((terNormEndX - terNormStartX) * info.alphaWidth) + 1, info.alphaWidth - info.alphaStartX);
                info.alphaSizeZ = Mathf.Min(Mathf.RoundToInt((terNormEndZ - terNormStartZ) * info.alphaHeight) + 1, info.alphaHeight - info.alphaStartZ);

                info.alphaMap = data.GetAlphamaps(info.alphaStartX, info.alphaStartZ, info.alphaSizeX, info.alphaSizeZ);
                info.possibleArea = (from areaID in navmeshSettings.data select PathFinder.GetArea(areaID)).ToArray(); //else if doCollectAreaInfo == false than later it became defaul area
            }

            switch (terrainCollectionType) {
                case TerrainCollectorType.CPU:
                    terrainsInfoForCPU.Add(info);
                    break;
                case TerrainCollectorType.ComputeShader:
                    CollectTerrainOnGPU(info);
                    break;
            }
     
        }

        public List<ShapeDataAbstract> CollectTreeData(Terrain terrain) {
            Transform terrainTransform = terrain.transform;
            PathFinderTerrainMetaData metaData = terrainTransform.GetComponent<PathFinderTerrainMetaData>();

            if (metaData == null) {
                metaData = terrainTransform.gameObject.AddComponent<PathFinderTerrainMetaData>();
            }
            metaData.Refresh();

            List<ShapeDataAbstract> shapeData = new List<ShapeDataAbstract>();
            metaData.GetTreeShapes(template, ref shapeData);
            return shapeData;
        }


        public static bool CalculateWalk(Vector3 A, Vector3 B, Vector3 C, float aMaxSlopeCos, bool flipY = false) {
            if (flipY)
                return (Vector3.Cross(B - A, C - A).normalized.y * -1) >= aMaxSlopeCos;
            else
                return Vector3.Cross(B - A, C - A).normalized.y >= aMaxSlopeCos;
        }

        //it rents arrays from pool so dont forget to return it back
        public static void GetTerrainMesh(TerrainColliderInfoMesh terrain, out Vector3[] verts, out int[] tris) {
            int hSizeX = terrain.hSizeX;
            int hSizeZ = terrain.hSizeZ;
            int resolution = terrain.resolution;
            float[,] heightMap = terrain.heightMap;
            Matrix4x4 heightMatrix = terrain.heightMatrix;

            int vertsLength = hSizeX * hSizeZ;
            int trisLength = (hSizeX - 1) * (hSizeZ - 1) * 6;

            verts = Pool.GenericPoolArray<Vector3>.Take(vertsLength);
            tris = Pool.GenericPoolArray<int>.Take(trisLength);

            for (int x = 0; x < hSizeX; x++) {
                for (int z = 0; z < hSizeZ; z++) {
                    verts[z * hSizeX + x] = heightMatrix.MultiplyPoint3x4(new Vector3(x, heightMap[z * resolution, x * resolution], z));
                }
            }

            var index = 0;
            for (int y = 0; y < hSizeZ - 1; y++) {
                for (int x = 0; x < hSizeX - 1; x++) {
                    tris[index++] = (y * hSizeX) + x;
                    tris[index++] = ((y + 1) * hSizeX) + x + 1;
                    tris[index++] = (y * hSizeX) + x + 1;

                    tris[index++] = (y * hSizeX) + x;
                    tris[index++] = ((y + 1) * hSizeX) + x;
                    tris[index++] = ((y + 1) * hSizeX) + x + 1;
                }
            }
        }

        /// <summary>
        /// ref cause it should be pooled and returned outside this function
        /// </summary>
        private void GenerateTerrainAreaMapFromSplatMap(TerrainColliderInfoMesh terrainInfo, Collector3.ShapeCollector shape, ref byte[] result) {
            int size = shape.flattenSize;
            //var areaLibrary = PathFinder.settings.areaLibrary;

            float[,,] alpha = terrainInfo.alphaMap;
            int alphaCount = alpha.GetLength(2);
            int[] alphaToArea = terrainInfo.settings.data;

            //shome stored values
            int startXClamp = terrainInfo.startXClamp;
            int startZClamp = terrainInfo.startZClamp;

            int endXClamp = terrainInfo.endXClamp;
            int endZClamp = terrainInfo.endZClamp;

            int terrainStartX = terrainInfo.terrainStartX;
            int terrainStartZ = terrainInfo.terrainStartZ;

            float terrainSizeX = terrainInfo.terrainSizeX;
            float terrainSizeZ = terrainInfo.terrainSizeZ;

            int alphaWidth = terrainInfo.alphaWidth;
            int alphaHeight = terrainInfo.alphaHeight;

            int alphaStartX = terrainInfo.alphaStartX;
            int alphaStartZ = terrainInfo.alphaStartZ;

            int alphaSizeXIndexLimit = terrainInfo.alphaSizeX - 1;
            int alphaSizeZIndexLimit = terrainInfo.alphaSizeZ - 1;

            int shapeSizeX = shape.sizeX;

            //this done this way to take into account that alpha map not aligned with chunk start
            for (int x = startXClamp; x < endXClamp; x++) {
                int curX = Mathf.Clamp(Mathf.RoundToInt((x - terrainStartX) / terrainSizeX * alphaWidth) - alphaStartX, 0, alphaSizeXIndexLimit);

                for (int z = startZClamp; z < endZClamp; z++) {
                    int curZ = Mathf.Clamp(Mathf.RoundToInt((z - terrainStartZ) / terrainSizeZ * alphaHeight) - alphaStartZ, 0, alphaSizeZIndexLimit);

                    int highest = 0;
                    for (int i = 0; i < alphaCount; i++) {
                        if (alpha[curZ, curX, i] > alpha[curZ, curX, highest])
                            highest = i;
                    }

                    //int targetX = x - template.startX_extra;
                    //int targetZ = z - template.startZ_extra;
                    //areaMap[shape.GetIndex(targetX, targetZ)] = (byte)alphaToArea[highest];

                    result[((z - template.startZ_extra) * shapeSizeX) + (x - template.startX_extra)] = (byte)alphaToArea[highest];
                }
            }
        }
    }
}