using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using K_PathFinder.VectorInt;

using System;
using K_PathFinder.Graphs;
using K_PathFinder.NodesNameSpace;
using K_PathFinder.EdgesNameSpace;
using K_PathFinder;
using K_PathFinder.CoverNamespace;
using System.Runtime.InteropServices;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.GraphGeneration {
    struct GraphIteratorValueNavmesh {
        public int voxelIndex;
        public short hash;
        public float y;
    }
    struct GraphIteratorValueCover {
        public int voxelIndex;
        public short hash;
        public sbyte coverheight;
        public float y;
    }

    public partial class GraphGeneratorNew {
        //marching squares usage
        private void GenerateNavMeshCountourNotNeat(bool checkAreas) {
            Vector3 offset = template.halfVoxelOffset;
            Vector3 realChunkPos = template.realOffsetedPosition;
            float voxelSize = template.voxelSize;
            ///
            ///
            ///

       

            VolumeContainerNew.DataCollum[] volumeCollums = volumeContainer.collums;
            VolumeContainerNew.Data[] volumeData = volumeContainer.data;
            CoolTools.StackedList<VolumeArea> areaSet = volumeContainer.areaSet;
            HashSet<VolumeArea> collectedArea = new HashSet<VolumeArea>();

            short[] dataHashes = new short[volumeData.Length];
            for (int i = 0; i < dataHashes.Length; i++) {
                dataHashes[i] = AreaPassabilityHashData.INVALID_HASH_NUMBER;
            }
            int sizeX = volumeContainer.sizeX;
            int sizeZ = volumeContainer.sizeZ;
            int extra = template.extraOffset;
            sbyte minPass = (sbyte)Passability.Crouchable;
            for (int x = extra; x < sizeX - extra; x++) {
                for (int z = extra; z < sizeZ - extra; z++) {
                    var dc = volumeCollums[(z * sizeX) + x];
                    for (int i2 = 0; i2 < dc.count; i2++) {
                        var curValue = volumeData[dc.index + i2];
                        if (curValue.area != 1 & curValue.pass >= minPass)
                            dataHashes[dc.index + i2] = AreaPassabilityHashData.GetAreaHash(curValue.area, curValue.pass);
                    }
                }
            }
            
            int start = template.extraOffset - 1;
            int endX = template.extraOffset + template.lengthX_central;
            int endZ = template.extraOffset + template.lengthZ_central;
            //int layerIndex = dataLayer.layerIndex;            

            //DataLayerPoint[] data = dataLayer.data;

            //HashSet<short> tempHashes = new HashSet<short>();

            //short[] layers = new short[4];
            //int layersCount = 0;
            short[] hashes = new short[4];
            int hashesCount = 0;

            int layersCount = volumeContainer.layersCount;
            GraphIteratorValueNavmesh[][] iteratorData = new GraphIteratorValueNavmesh[layersCount][];
            int flattenedSize = volumeContainer.flattenedSize;

            GraphIteratorValueNavmesh defaultValue = new GraphIteratorValueNavmesh() {
                voxelIndex = -1,
                hash = AreaPassabilityHashData.INVALID_HASH_NUMBER,
                y = 0f
            };
            for (int layerIndex = 0; layerIndex < layersCount; layerIndex++) {             
                GraphIteratorValueNavmesh[] layer = Pool.GenericPoolArray<GraphIteratorValueNavmesh>.Take(flattenedSize);
                for (int i = 0; i < flattenedSize; i++) {
                    layer[i] = defaultValue;                   
                }
                iteratorData[layerIndex] = layer;
            }
            
            for (int x = start + 1; x < endX; x++) {
                for (int z = start + 1; z < endZ; z++) {
                    int collumIndex = (z * sizeX) + x;
                    var collum = volumeCollums[collumIndex];
                    for (int i = 0; i < collum.count; i++) {
                        var dataValue = volumeData[collum.index + i];
                        iteratorData[dataValue.layer][collumIndex] = new GraphIteratorValueNavmesh() {
                            voxelIndex = collum.index + i,
                            hash = dataValue.area != 1 && dataValue.pass >= minPass ? AreaPassabilityHashData.GetAreaHash(dataValue.area, dataValue.pass) : AreaPassabilityHashData.INVALID_HASH_NUMBER,
                            y = dataValue.y
                        };
                    }
                }
            }

            for (int layerIndex = 0; layerIndex < layersCount; layerIndex++) {
                GraphIteratorValueNavmesh[] layer = iteratorData[layerIndex];

                //if (layerIndex != 1)
                //    continue;

                for (int x = start; x < endX; x++) {
                    for (int z = start; z < endZ; z++) {
                        GraphIteratorValueNavmesh value1 = layer[(z * sizeX) + x];
                        GraphIteratorValueNavmesh value2 = layer[(z * sizeX) + (x + 1)];
                        GraphIteratorValueNavmesh value4 = layer[((z + 1) * sizeX) + x];
                        GraphIteratorValueNavmesh value8 = layer[((z + 1) * sizeX) + (x + 1)];
                        
                        if (value1.voxelIndex != -1) {
                            if (checkAreas)
                                areaSet.Read(value1.voxelIndex, collectedArea);        
                        }
                        if (value2.voxelIndex != -1) {
                            if (checkAreas)
                                areaSet.Read(value2.voxelIndex, collectedArea);
                        }
                        if (value4.voxelIndex != -1) {
                            if (checkAreas)
                                areaSet.Read(value4.voxelIndex, collectedArea);
                        }
                        if (value8.voxelIndex != -1) {
                            if (checkAreas)
                                areaSet.Read(value8.voxelIndex, collectedArea);
                        }

                        hashesCount = 1;
                        hashes[0] = value1.hash;
                        if (value1.hash != value2.hash)
                            hashes[hashesCount++] = value2.hash;
                        if (value4.hash != value2.hash && value4.hash != value1.hash)
                            hashes[hashesCount++] = value4.hash;
                        if (value8.hash != value1.hash && value8.hash != value2.hash && value8.hash != value4.hash)
                            hashes[hashesCount++] = value8.hash;

                        if (hashesCount != 1) {
                            for (int i = 0; i < hashesCount; i++) {
                                short targetHash = hashes[i];
                                if (targetHash == -1)
                                    continue;

                                int dataType = 0;
                                if (value1.hash == targetHash) dataType |= 1;
                                if (value2.hash == targetHash) dataType |= 2;
                                if (value4.hash == targetHash) dataType |= 4;
                                if (value8.hash == targetHash) dataType |= 8;

                                //4 z+ 8
                                //x-   x+  
                                //1 z- 2
                                int divider;
                                float height;
                                divider = 0;
                                height = 0;
                                if (value1.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value1.y; }
                                if (value4.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value4.y; }
                                if (divider == 2) height *= 0.5f;
                                Vector2Int1Float xMinus = new Vector2Int1Float(x * 2, height, (z * 2) + 1);

                                divider = 0;
                                height = 0;
                                if (value2.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value2.y; }
                                if (value8.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value8.y; }
                                if (divider == 2) height *= 0.5f;
                                Vector2Int1Float xPlus = new Vector2Int1Float(((x + 1) * 2), height, (z * 2) + 1);

                                divider = 0;
                                height = 0;
                                if (value1.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value1.y; }
                                if (value2.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value2.y; }
                                if (divider == 2) height *= 0.5f;
                                Vector2Int1Float zMinus = new Vector2Int1Float((x * 2) + 1, height, z * 2);

                                divider = 0;
                                height = 0;
                                if (value4.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value4.y; }
                                if (value8.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value8.y; }
                                if (divider == 2) height *= 0.5f;
                                Vector2Int1Float zPlus = new Vector2Int1Float((x * 2) + 1, height, (z + 1) * 2);



                                //Vector3 p1 = realChunkPos + offset + new Vector3(x * voxelSize, value1.y, z * voxelSize);
                                //Vector3 p2 = realChunkPos + offset + new Vector3((x + 1) * voxelSize, value2.y, z * voxelSize);
                                //Vector3 p4 = realChunkPos + offset + new Vector3(x * voxelSize, value4.y, (z + 1) * voxelSize);
                                //Vector3 p8 = realChunkPos + offset + new Vector3((x + 1) * voxelSize, value8.y, (z + 1) * voxelSize);
                                //Vector3 mid = SomeMath.MidPoint(p1, p2, p4, p8);
                                //Debuger_K.AddLine(mid, xMinus, Color.red);
                                //Debuger_K.AddLine(mid, xPlus, Color.green);
                                //Debuger_K.AddLine(mid, zMinus, Color.blue);
                                //Debuger_K.AddLine(mid, zPlus, Color.magenta);
                                //Debuger_K.AddDot(xMinus, Color.red, 0.01f);
                                //Debuger_K.AddDot(xPlus, Color.green, 0.01f);
                                //Debuger_K.AddDot(zMinus, Color.blue, 0.01f);
                                //Debuger_K.AddDot(zPlus, Color.magenta, 0.01f);
                                //4 z+ 8
                                //x-   x+  
                                //1 z- 2

                                switch (dataType) {
                                    //corners
                                    case 1:
                                        SetEdge(xMinus, zMinus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(xMinus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;
                                    case 2:
                                        SetEdge(zMinus, xPlus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(zMinus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;
                                    case 4:
                                        SetEdge(zPlus, xMinus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(zPlus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;
                                    case 8:
                                        SetEdge(xPlus, zPlus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(xPlus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;

                                    //throgh middle
                                    case 3:
                                        SetEdge(xMinus, xPlus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(xMinus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;
                                    case 5:
                                        SetEdge(zPlus, zMinus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(zPlus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;
                                    case 10:
                                        SetEdge(zMinus, zPlus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(zMinus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;
                                    case 12:
                                        SetEdge(xPlus, xMinus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(xPlus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;

                                    //L - shape
                                    case 7:
                                        SetEdge(zPlus, xPlus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(zPlus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;
                                    case 11:
                                        SetEdge(xMinus, zPlus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(xMinus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;
                                    case 13:
                                        SetEdge(xPlus, zMinus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(xPlus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;
                                    case 14:
                                        SetEdge(zMinus, xMinus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(zMinus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;

                                    //diagonals
                                    case 6:
                                        SetEdge(zMinus, xPlus, layerIndex, targetHash, collectedArea);
                                        SetEdge(zPlus, xMinus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(zMinus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        //Debuger_K.AddLine(NodePos(zPlus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;
                                    case 9:
                                        SetEdge(xMinus, zMinus, layerIndex, targetHash, collectedArea);
                                        SetEdge(xPlus, zPlus, layerIndex, targetHash, collectedArea);
                                        //Debuger_K.AddLine(NodePos(xMinus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        //Debuger_K.AddLine(NodePos(xPlus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                        break;
                                }
                            }
                        }
                    }
                }
                #region hide
                //}
                //for (int collumLayer = 0; collumLayer < collumMaxCount; collumLayer++) {
                //    //4 z+ 8
                //    //x-   x+  
                //    //1 z- 2
                //    short layer1, layer2, layer3, layer4;
                //    short hash1, hash2, hash4, hash8;
                //    float p1y, p2y, p4y, p8y;
                //    int index;
                //    hash1 = hash2 = hash4 = hash8 = AreaPassabilityHashData.INVALID_HASH_NUMBER;
                //    p1y = p2y = p4y = p8y = 0f;
                //    if (checkAreas) {
                //        collectedArea.Clear();
                //    }


                //    if (collum.count > collumLayer) {
                //        index = collum.index + collumLayer;
                //        hash1 = dataHashes[index];
                //        p1y = volumeData[index].y;

                //        if (checkAreas && hash1 != AreaPassabilityHashData.INVALID_HASH_NUMBER)
                //            areaSet.Read(index, collectedArea);
                //    }

                //    if (collum2.count > collumLayer) {
                //        index = collum2.index + collumLayer;
                //        hash2 = dataHashes[index];
                //        p2y = volumeData[index].y;

                //        if (checkAreas && hash2 != AreaPassabilityHashData.INVALID_HASH_NUMBER)
                //            areaSet.Read(index, collectedArea);
                //    }

                //    if (collum4.count > collumLayer) {
                //        index = collum4.index + collumLayer;
                //        hash4 = dataHashes[index];
                //        p4y = volumeData[index].y;

                //        if (checkAreas && hash4 != AreaPassabilityHashData.INVALID_HASH_NUMBER)
                //            areaSet.Read(index, collectedArea);
                //    }

                //    if (collum8.count > collumLayer) {
                //        index = collum8.index + collumLayer;
                //        hash8 = dataHashes[index];
                //        p8y = volumeData[index].y;

                //        if (checkAreas && hash8 != AreaPassabilityHashData.INVALID_HASH_NUMBER)
                //            areaSet.Read(index, collectedArea);
                //    }

                //    //Vector3 p1 = realChunkPos + offset + new Vector3(x * voxelSize, p1y, z * voxelSize);
                //    //Vector3 p2 = realChunkPos + offset + new Vector3((x + 1) * voxelSize, p2y, z * voxelSize);
                //    //Vector3 p4 = realChunkPos + offset + new Vector3(x * voxelSize, p4y, (z + 1) * voxelSize);
                //    //Vector3 p8 = realChunkPos + offset + new Vector3((x + 1) * voxelSize, p8y, (z + 1) * voxelSize);

                //    //Vector3 mid = SomeMath.MidPoint(p1, p2, p4, p8);
                //    //Debuger_K.AddLine(mid, p1);
                //    //Debuger_K.AddLine(mid, p2);
                //    //Debuger_K.AddLine(mid, p4);
                //    //Debuger_K.AddLine(mid, p8);

                //    hashesCount = 1;
                //    hashes[0] = hash1;

                //    if (hash1 != hash2)
                //        hashes[hashesCount++] = hash2;
                //    if (hash4 != hash2 & hash4 != hash1)
                //        hashes[hashesCount++] = hash4;
                //    if (hash8 != hash1 & hash8 != hash2 & hash8 != hash4)
                //        hashes[hashesCount++] = hash8;

                //    //string s = "";
                //    //for (int i = 0; i < hashesCount; i++) {
                //    //    s += hashes[i] + " : "; 
                //    //}              
                //    //Debuger_K.AddLabel(mid, s);

                //    if (hashesCount != 1) {
                //        for (int i = 0; i < hashesCount; i++) {
                //            short targetHash = hashes[i];
                //            if (targetHash == -1)
                //                continue;

                //            int dataType = 0;
                //            if (hash1 == targetHash) dataType |= 1;
                //            if (hash2 == targetHash) dataType |= 2;
                //            if (hash4 == targetHash) dataType |= 4;
                //            if (hash8 == targetHash) dataType |= 8;

                //            //4 z+ 8
                //            //x-   x+  
                //            //1 z- 2
                //            int divider;
                //            float height;
                //            divider = 0;
                //            height = 0;
                //            if (hash1 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p1y; }
                //            if (hash4 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p4y; }
                //            if (divider == 2) height *= 0.5f;
                //            Vector2Int1Float xMinus = new Vector2Int1Float(x * 2, height, (z * 2) + 1);

                //            divider = 0;
                //            height = 0;
                //            if (hash2 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p2y; }
                //            if (hash8 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p8y; }
                //            if (divider == 2) height *= 0.5f;
                //            Vector2Int1Float xPlus = new Vector2Int1Float(((x + 1) * 2), height, (z * 2) + 1);

                //            divider = 0;
                //            height = 0;
                //            if (hash1 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p1y; }
                //            if (hash2 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p2y; }
                //            if (divider == 2) height *= 0.5f;
                //            Vector2Int1Float zMinus = new Vector2Int1Float((x * 2) + 1, height, z * 2);

                //            divider = 0;
                //            height = 0;
                //            if (hash4 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p4y; }
                //            if (hash8 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p8y; }
                //            if (divider == 2) height *= 0.5f;
                //            Vector2Int1Float zPlus = new Vector2Int1Float((x * 2) + 1, height, (z + 1) * 2);


                //            //Debuger_K.AddLine(mid, xMinus, Color.red);
                //            //Debuger_K.AddLine(mid, xPlus, Color.green);
                //            //Debuger_K.AddLine(mid, zMinus, Color.blue);
                //            //Debuger_K.AddLine(mid, zPlus, Color.magenta);
                //            //Debuger_K.AddDot(xMinus, Color.red, 0.01f);
                //            //Debuger_K.AddDot(xPlus, Color.green, 0.01f);
                //            //Debuger_K.AddDot(zMinus, Color.blue, 0.01f);
                //            //Debuger_K.AddDot(zPlus, Color.magenta, 0.01f);
                //            //4 z+ 8
                //            //x-   x+  
                //            //1 z- 2

                //            switch (dataType) {
                //                //corners
                //                case 1:
                //                    SetEdge(xMinus, zMinus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(xMinus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;
                //                case 2:
                //                    SetEdge(zMinus, xPlus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(zMinus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;
                //                case 4:
                //                    SetEdge(zPlus, xMinus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(zPlus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;
                //                case 8:
                //                    SetEdge(xPlus, zPlus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(xPlus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;

                //                //throgh middle
                //                case 3:
                //                    SetEdge(xMinus, xPlus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(xMinus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;
                //                case 5:
                //                    SetEdge(zPlus, zMinus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(zPlus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;
                //                case 10:
                //                    SetEdge(zMinus, zPlus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(zMinus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;
                //                case 12:
                //                    SetEdge(xPlus, xMinus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(xPlus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;

                //                //L - shape
                //                case 7:
                //                    SetEdge(zPlus, xPlus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(zPlus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;
                //                case 11:
                //                    SetEdge(xMinus, zPlus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(xMinus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;
                //                case 13:
                //                    SetEdge(xPlus, zMinus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(xPlus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;
                //                case 14:
                //                    SetEdge(zMinus, xMinus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(zMinus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;

                //                //diagonals
                //                case 6:
                //                    SetEdge(zMinus, xPlus, collumLayer, targetHash, collectedArea);
                //                    SetEdge(zPlus, xMinus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(zMinus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    //Debuger_K.AddLine(NodePos(zPlus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;
                //                case 9:
                //                    SetEdge(xMinus, zMinus, collumLayer, targetHash, collectedArea);
                //                    SetEdge(xPlus, zPlus, collumLayer, targetHash, collectedArea);
                //                    //Debuger_K.AddLine(NodePos(xMinus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    //Debuger_K.AddLine(NodePos(xPlus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                //                    break;
                //            }
                //        }
                //    }
                #endregion
            }

            for (int layerIndex = 0; layerIndex < layersCount; layerIndex++) {
                Pool.GenericPoolArray<GraphIteratorValueNavmesh>.ReturnToPool(ref iteratorData[layerIndex]);
            }
        }


        private void GenerateCoversNotNeat() {
            Vector3 offset = template.halfVoxelOffset;
            Vector3 realChunkPos = template.realOffsetedPosition;
            float voxelSize = template.voxelSize;
            ///
            ///
            ///


            VolumeContainerNew.DataCollum[] volumeCollums = volumeContainer.collums;
            VolumeContainerNew.Data[] volumeData = volumeContainer.data;
            VolumeContainerNew.DataCover[] coverData = volumeContainer.coverData;


            short[] dataHashes = new short[volumeData.Length];
            for (int i = 0; i < dataHashes.Length; i++) {
                dataHashes[i] = AreaPassabilityHashData.INVALID_HASH_NUMBER;
            }
            int sizeX = volumeContainer.sizeX;
            int sizeZ = volumeContainer.sizeZ;
            int extra = template.extraOffset;
            sbyte minPass = (sbyte)Passability.Crouchable;
            for (int x = extra; x < sizeX - extra; x++) {
                for (int z = extra; z < sizeZ - extra; z++) {
                    var dc = volumeCollums[(z * sizeX) + x];
                    for (int i2 = 0; i2 < dc.count; i2++) {
                        var curValue = volumeData[dc.index + i2];
                        if (curValue.area != 1 & curValue.pass >= minPass)
                            dataHashes[dc.index + i2] = AreaPassabilityHashData.GetAreaHash(curValue.area, curValue.pass);
                    }
                }
            }

            int start = template.extraOffset - 1;
            int endX = template.extraOffset + template.lengthX_central;
            int endZ = template.extraOffset + template.lengthZ_central;

            int layersCount = volumeContainer.layersCount;
            GraphIteratorValueCover[][] iteratorData = new GraphIteratorValueCover[layersCount][];
            int flattenedSize = volumeContainer.flattenedSize;

            GraphIteratorValueCover defaultValue = new GraphIteratorValueCover() {
                voxelIndex = -1,
                hash = AreaPassabilityHashData.INVALID_HASH_NUMBER
            };

            for (int layerIndex = 0; layerIndex < layersCount; layerIndex++) {
                GraphIteratorValueCover[] layer = Pool.GenericPoolArray<GraphIteratorValueCover>.Take(flattenedSize);
                for (int i = 0; i < flattenedSize; i++) {
                    layer[i] = defaultValue;
                }
                iteratorData[layerIndex] = layer;
            }

            for (int x = start + 1; x < endX; x++) {
                for (int z = start + 1; z < endZ; z++) {
                    int collumIndex = (z * sizeX) + x;
                    var collum = volumeCollums[collumIndex];
                    for (int i = 0; i < collum.count; i++) {
                        var dataValue = volumeData[collum.index + i];
                        var coverValue = coverData[collum.index + i];
                        iteratorData[dataValue.layer][collumIndex] = new GraphIteratorValueCover() {
                            voxelIndex = collum.index + i,
                            hash = coverValue.hash,
                            coverheight = coverValue.coverHeight,
                            y = dataValue.y
                        };
                    }
                }
            }

            for (int layerIndex = 0; layerIndex < layersCount; layerIndex++) {
                GraphIteratorValueCover[] layer = iteratorData[layerIndex];



                //if (layerIndex != 1)
                //    continue;

                for (int x = start; x < endX; x++) {
                    for (int z = start; z < endZ; z++) {
                        GraphIteratorValueCover value1 = layer[(z * sizeX) + x];
                        GraphIteratorValueCover value2 = layer[(z * sizeX) + (x + 1)];
                        GraphIteratorValueCover value4 = layer[((z + 1) * sizeX) + x];
                        GraphIteratorValueCover value8 = layer[((z + 1) * sizeX) + (x + 1)];


                        //Vector3 p1 = volumeContainer.GetPos(x, z, value1.y);
                        //Vector3 p2 = volumeContainer.GetPos(x + 1, z, value1.y);
                        //Vector3 p4 = volumeContainer.GetPos(x, z + 1, value1.y);
                        //Vector3 p8 = volumeContainer.GetPos(x + 1, z + 1, value1.y);
                        //Debuger_K.AddLabel(p1, value1.coverheight);
                        //Debuger_K.AddDot(p1, Debuger_K.IntegerToColor(value1.hash));
                        //Debuger_K.AddDot(p2, Debuger_K.IntegerToColor(value2.hash));
                        //Debuger_K.AddDot(p4, Debuger_K.IntegerToColor(value4.hash));
                        //Debuger_K.AddDot(p8, Debuger_K.IntegerToColor(value8.hash));

                        sbyte coverHeigh = 0;
                        if (value1.voxelIndex != -1)
                            coverHeigh = value1.coverheight;                        
                        if (value2.voxelIndex != -1 & value2.coverheight > coverHeigh)
                            coverHeigh = value2.coverheight;
                        if (value4.voxelIndex != -1 & value4.coverheight > coverHeigh)
                            coverHeigh = value4.coverheight;
                        if (value8.voxelIndex != -1 & value8.coverheight > coverHeigh)
                            coverHeigh = value8.coverheight;

                        int dataType = 0;
                        if (value1.hash == AreaPassabilityHashData.COVER_HASH) dataType |= 1;
                        if (value2.hash == AreaPassabilityHashData.COVER_HASH) dataType |= 2;
                        if (value4.hash == AreaPassabilityHashData.COVER_HASH) dataType |= 4;
                        if (value8.hash == AreaPassabilityHashData.COVER_HASH) dataType |= 8;

                        if (dataType == 0 | dataType == 15)
                            continue;

                        //4 z+ 8
                        //x-   x+  
                        //1 z- 2
                        int divider;
                        float height;
                        divider = 0;
                        height = 0;
                        if (value1.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value1.y; }
                        if (value4.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value4.y; }
                        if (divider == 2) height *= 0.5f;
                        Vector2Int1Float xMinus = new Vector2Int1Float(x * 2, height, (z * 2) + 1);

                        divider = 0;
                        height = 0;
                        if (value2.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value2.y; }
                        if (value8.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value8.y; }
                        if (divider == 2) height *= 0.5f;
                        Vector2Int1Float xPlus = new Vector2Int1Float(((x + 1) * 2), height, (z * 2) + 1);

                        divider = 0;
                        height = 0;
                        if (value1.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value1.y; }
                        if (value2.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value2.y; }
                        if (divider == 2) height *= 0.5f;
                        Vector2Int1Float zMinus = new Vector2Int1Float((x * 2) + 1, height, z * 2);

                        divider = 0;
                        height = 0;
                        if (value4.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value4.y; }
                        if (value8.hash != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += value8.y; }
                        if (divider == 2) height *= 0.5f;
                        Vector2Int1Float zPlus = new Vector2Int1Float((x * 2) + 1, height, (z + 1) * 2);

                        //4 z+ 8
                        //x-   x+  
                        //1 z- 2

                        switch (dataType) {
                            //corners
                            case 1:
                                SetCoverEdge(xMinus, zMinus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(xMinus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;
                            case 2:
                                SetCoverEdge(zMinus, xPlus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(zMinus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;
                            case 4:
                                SetCoverEdge(zPlus, xMinus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(zPlus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;
                            case 8:
                                SetCoverEdge(xPlus, zPlus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(xPlus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;

                            //throgh middle
                            case 3:
                                SetCoverEdge(xMinus, xPlus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(xMinus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;
                            case 5:
                                SetCoverEdge(zPlus, zMinus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(zPlus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;
                            case 10:
                                SetCoverEdge(zMinus, zPlus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(zMinus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;
                            case 12:
                                SetCoverEdge(xPlus, xMinus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(xPlus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;

                            //L - shape
                            case 7:
                                SetCoverEdge(zPlus, xPlus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(zPlus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;
                            case 11:
                                SetCoverEdge(xMinus, zPlus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(xMinus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;
                            case 13:
                                SetCoverEdge(xPlus, zMinus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(xPlus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;
                            case 14:
                                SetCoverEdge(zMinus, xMinus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(zMinus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;

                            //diagonals
                            case 6:
                                SetCoverEdge(zMinus, xPlus, coverHeigh);
                                SetCoverEdge(zPlus, xMinus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(zMinus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                //Debuger_K.AddLine(NodePos(zPlus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;
                            case 9:
                                SetCoverEdge(xMinus, zMinus, coverHeigh);
                                SetCoverEdge(xPlus, zPlus, coverHeigh);
                                //Debuger_K.AddLine(NodePos(xMinus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                //Debuger_K.AddLine(NodePos(xPlus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
                                break;
                        }
                    }
                }
            }

            for (int layerIndex = 0; layerIndex < layersCount; layerIndex++) {
                Pool.GenericPoolArray<GraphIteratorValueCover>.ReturnToPool(ref iteratorData[layerIndex]);
            }
        }




        //copy pasted code for sake of perfomance
        //private void GenerateCoversNotNeat() {
        //    Vector3 offset = template.halfVoxelOffset;
        //    Vector3 realChunkPos = template.realOffsetedPosition;
        //    float voxelSize = template.voxelSize;
        //    //\\ debug here //\\

        //    VolumeContainerNew.DataCollum[] volumeCollums = volumeContainer.collums;
        //    VolumeContainerNew.Data[] volumeData = volumeContainer.data;
        //    VolumeContainerNew.DataCover[] coverData = volumeContainer.coverData;


        //    int sizeX = volumeContainer.sizeX;
        //    //int sizeZ = volumeContainer.sizeZ;
        //    int extra = template.extraOffset;


        //    int start = template.extraOffset - 1;
        //    int endX = template.extraOffset + template.lengthX_central;
        //    int endZ = template.extraOffset + template.lengthZ_central;
        //    //int layerIndex = dataLayer.layerIndex;            

        //    //DataLayerPoint[] data = dataLayer.data;

        //    //HashSet<short> tempHashes = new HashSet<short>();

        //    for (int x = start; x < endX; x++) {
        //        for (int z = start; z < endZ; z++) {
        //            var collum1 = volumeCollums[(z * sizeX) + x];
        //            var collum2 = volumeCollums[(z * sizeX) + (x + 1)];
        //            var collum4 = volumeCollums[((z + 1) * sizeX) + x];
        //            var collum8 = volumeCollums[((z + 1) * sizeX) + (x + 1)];

        //            int collumMaxCount = collum1.count;
        //            if (collum2.count > collumMaxCount) collumMaxCount = collum2.count;
        //            if (collum4.count > collumMaxCount) collumMaxCount = collum4.count;
        //            if (collum8.count > collumMaxCount) collumMaxCount = collum8.count;

        //            //for (int i = 0; i < collum1.count; i++) {
        //            //    Vector3 pTest = realChunkPos + offset + new Vector3(x * voxelSize, volumeData[collum1.index + i].y, z * voxelSize);
        //            //    Debuger_K.AddLabel(pTest, dataHashes[collum1.index + i]);
        //            //}


        //            for (int layerIndex = 0; layerIndex < collumMaxCount; layerIndex++) {
        //                //4 z+ 8
        //                //x-   x+  
        //                //1 z- 2

        //                sbyte hash1, hash2, hash4, hash8;
        //                float p1y, p2y, p4y, p8y;
        //                int index;

        //                hash1 = hash2 = hash4 = hash8 = AreaPassabilityHashData.INVALID_COVER_HASH;
        //                p1y = p2y = p4y = p8y = 0f;

        //                sbyte coverHeigh = 0;

        //                if (collum1.count > layerIndex) {
        //                    index = collum1.index + layerIndex;
        //                    hash1 = coverData[index].hash;                       
        //                    p1y = volumeData[index].y;
        //                    coverHeigh = Math.Max(coverData[index].coverHeight, coverHeigh);               
        //                }

        //                if (collum2.count > layerIndex) {
        //                    index = collum2.index + layerIndex;
        //                    hash2 = coverData[index].hash;
        //                    p2y = volumeData[index].y;
        //                    coverHeigh = Math.Max(coverData[index].coverHeight, coverHeigh);
        //                }

        //                if (collum4.count > layerIndex) {
        //                    index = collum4.index + layerIndex;
        //                    hash4 = coverData[index].hash;
        //                    p4y = volumeData[index].y;
        //                    coverHeigh = Math.Max(coverData[index].coverHeight, coverHeigh);
        //                }

        //                if (collum8.count > layerIndex) {
        //                    index = collum8.index + layerIndex;
        //                    hash8 = coverData[index].hash;
        //                    p8y = volumeData[index].y;
        //                    coverHeigh = Math.Max(coverData[index].coverHeight, coverHeigh);
        //                }

        //                //Vector3 p1 = realChunkPos + offset + new Vector3(x * voxelSize, p1y, z * voxelSize);
        //                //Vector3 p2 = realChunkPos + offset + new Vector3((x + 1) * voxelSize, p2y, z * voxelSize);
        //                //Vector3 p4 = realChunkPos + offset + new Vector3(x * voxelSize, p4y, (z + 1) * voxelSize);
        //                //Vector3 p8 = realChunkPos + offset + new Vector3((x + 1) * voxelSize, p8y, (z + 1) * voxelSize);

        //                //Vector3 mid = SomeMath.MidPoint(p1, p2, p4, p8);
        //                //Debuger_K.AddLine(mid, p1);
        //                //Debuger_K.AddLine(mid, p2);
        //                //Debuger_K.AddLine(mid, p4);
        //                //Debuger_K.AddLine(mid, p8);


        //                if (hash1 != hash2 | hash2 != hash4 | hash4 != hash8 | hash1 != hash8 | hash2 != hash4) {
        //                    int dataType = 0;
        //                    if (hash1 == AreaPassabilityHashData.COVER_HASH) dataType |= 1;
        //                    if (hash2 == AreaPassabilityHashData.COVER_HASH) dataType |= 2;
        //                    if (hash4 == AreaPassabilityHashData.COVER_HASH) dataType |= 4;
        //                    if (hash8 == AreaPassabilityHashData.COVER_HASH) dataType |= 8;

        //                    //4 z+ 8
        //                    //x-   x+  
        //                    //1 z- 2
        //                    int divider;
        //                    float height;
        //                    divider = 0;
        //                    height = 0;
        //                    if (hash1 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p1y; }
        //                    if (hash4 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p4y; }
        //                    if (divider == 2) height *= 0.5f;
        //                    Vector2Int1Float xMinus = new Vector2Int1Float(x * 2, height, (z * 2) + 1);

        //                    divider = 0;
        //                    height = 0;
        //                    if (hash2 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p2y; }
        //                    if (hash8 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p8y; }
        //                    if (divider == 2) height *= 0.5f;
        //                    Vector2Int1Float xPlus = new Vector2Int1Float(((x + 1) * 2), height, (z * 2) + 1);

        //                    divider = 0;
        //                    height = 0;
        //                    if (hash1 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p1y; }
        //                    if (hash2 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p2y; }
        //                    if (divider == 2) height *= 0.5f;
        //                    Vector2Int1Float zMinus = new Vector2Int1Float((x * 2) + 1, height, z * 2);

        //                    divider = 0;
        //                    height = 0;
        //                    if (hash4 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p4y; }
        //                    if (hash8 != AreaPassabilityHashData.INVALID_HASH_NUMBER) { divider++; height += p8y; }
        //                    if (divider == 2) height *= 0.5f;
        //                    Vector2Int1Float zPlus = new Vector2Int1Float((x * 2) + 1, height, (z + 1) * 2);


        //                    //Debuger_K.AddLine(mid, xMinus, Color.red);
        //                    //Debuger_K.AddLine(mid, xPlus, Color.green);
        //                    //Debuger_K.AddLine(mid, zMinus, Color.blue);
        //                    //Debuger_K.AddLine(mid, zPlus, Color.magenta);
        //                    //Debuger_K.AddDot(xMinus, Color.red, 0.01f);
        //                    //Debuger_K.AddDot(xPlus, Color.green, 0.01f);
        //                    //Debuger_K.AddDot(zMinus, Color.blue, 0.01f);
        //                    //Debuger_K.AddDot(zPlus, Color.magenta, 0.01f);
        //                    //4 z+ 8
        //                    //x-   x+  
        //                    //1 z- 2

        //                    switch (dataType) {
        //                        //corners
        //                        case 1:
        //                            SetCoverEdge(xMinus, zMinus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(xMinus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;
        //                        case 2:
        //                            SetCoverEdge(zMinus, xPlus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(zMinus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;
        //                        case 4:
        //                            SetCoverEdge(zPlus, xMinus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(zPlus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;
        //                        case 8:
        //                            SetCoverEdge(xPlus, zPlus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(xPlus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;

        //                        //throgh middle
        //                        case 3:
        //                            SetCoverEdge(xMinus, xPlus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(xMinus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;
        //                        case 5:
        //                            SetCoverEdge(zPlus, zMinus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(zPlus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;
        //                        case 10:
        //                            SetCoverEdge(zMinus, zPlus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(zMinus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;
        //                        case 12:
        //                            SetCoverEdge(xPlus, xMinus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(xPlus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;

        //                        //L - shape
        //                        case 7:
        //                            SetCoverEdge(zPlus, xPlus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(zPlus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;
        //                        case 11:
        //                            SetCoverEdge(xMinus, zPlus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(xMinus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;
        //                        case 13:
        //                            SetCoverEdge(xPlus, zMinus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(xPlus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;
        //                        case 14:
        //                            SetCoverEdge(zMinus, xMinus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(zMinus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;

        //                        //diagonals
        //                        case 6:
        //                            SetCoverEdge(zMinus, xPlus, coverHeigh);
        //                            SetCoverEdge(zPlus, xMinus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(zMinus), NodePos(xPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            //Debuger_K.AddLine(NodePos(zPlus), NodePos(xMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;
        //                        case 9:
        //                            SetCoverEdge(xMinus, zMinus, coverHeigh);
        //                            SetCoverEdge(xPlus, zPlus, coverHeigh);
        //                            //Debuger_K.AddLine(NodePos(xMinus), NodePos(zMinus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            //Debuger_K.AddLine(NodePos(xPlus), NodePos(zPlus), Color.cyan, Color.blue, addOnTop: 0.01f);
        //                            break;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}







        private Vector3 NodePos(Vector2Int1Float pos) {
            return new Vector3(
                chunkRealPosition.x + (pos.x * 0.5f * fragmentSize) - (fragmentSize * template.extraOffset) + halfFragmentSize,
                pos.y,
                chunkRealPosition.z + (pos.z * 0.5f * fragmentSize) - (fragmentSize * template.extraOffset) + halfFragmentSize);
        }
    }
}