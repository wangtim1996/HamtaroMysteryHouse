using K_PathFinder.CoolTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder {
    ////escentialy this is renamed volume with cramped up arrays
    //public class DataLayer {
    //    //general
    //    public readonly VectorInt.Vector2Int size;
    //    public readonly int sizeX, sizeZ, flattenedLength;
    //    public int layerIndex;

    //    public DataLayerPoint[] data;

    //    public HashSet<short> allAreaHashes = new HashSet<short>();
    //    //public HashSet<VolumeArea>[] areaSet;
    //    public StackList<VolumeArea> areaSet;

    //    public DataLayer(int sizeX, int sizeZ, int layerIndex) {
    //        this.size = new VectorInt.Vector2Int(sizeX, sizeZ);
    //        this.sizeX = sizeX;
    //        this.sizeZ = sizeZ;
    //        this.layerIndex = layerIndex; 
    //        flattenedLength = sizeX * sizeZ;
    //        data = new DataLayerPoint[flattenedLength];
    //        areaSet = new StackList<VolumeArea>(flattenedLength, flattenedLength / 2);
    
    //        for (int i = 0; i < flattenedLength; i++) {
    //            data[i].hashNavMesh = AreaPassabilityHashData.INVALID_HASH_NUMBER;
    //        }
    //    }

    //    public int GetIndex(int x, int z) {
    //        return (z * sizeX) + x;
    //    }

    //    public void AddVolumeArea(int x, int z, VolumeArea area) {
    //        areaSet.Add((z * sizeX) + x, area);
    //    }
    //}

    //public struct DataLayerPoint {        
    //    public float y;
    //    public short hashNavMesh;
    //    public int flags;

    //    public void SetData(float Y, short HashNavMesh, int Flags) {
    //        y = Y;
    //        hashNavMesh = HashNavMesh;
    //        flags = flags | Flags;
    //    }

    //    public void SetState(VoxelState state, bool value) {
    //        flags = value ? (flags | (int)state) : (flags & ~(int)state);
    //    }
    //    public bool GetState(VoxelState state) {
    //        return (flags & (int)state) != 0;
    //    }
    //}
}
