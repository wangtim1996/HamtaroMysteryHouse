using System;
using System.Linq;
using System.Collections.Generic;
using K_PathFinder.Pool;

//namespace K_PathFinder {
//    //TODO: find betterway to store connections other than int[][][]. it's just to many objects. maybe some byte shifting?

//    /// <summary>
//    /// class for storing all data about volume. it stores uper and lower height, flags, maps all good stuff. 
//    /// right now arrays are visible but later on they probably wount be. before you take some data check if it are even exist using Exist() function
//    /// </summary>
//    public class Volume {
//        //*********POOL*********//
//        static Dictionary<VectorInt.Vector2Int, Stack<Volume>> poolDictionary = new Dictionary<VectorInt.Vector2Int, Stack<Volume>>();
//        const int INITIAL_POOL_SIZE = 15;
//        //*********POOL*********//     
               
//        //general
//        public readonly VectorInt.Vector2Int size;
//        public readonly int sizeX, sizeZ, flatenedLength;

//        public int id;
//        public bool dead = false; //if true than dont use it anymore. trees puted instantly to that state for example.
//        public bool isTerrain = false; //flag to prevent it connection with other volumes cause it's waste of time. lots of time nothing can connect to terrain.
//        public bool isTree = false; //flag to prevent it connection with other volumes cause it's waste of time. lots of time nothing can connect to terrain.

//        public string tag; //really only valid at very begining and used to filter volumes for mods
//        public int layer;  //really only valid at very begining and used to filter volumes for mods

//        //formula (z * sizeX) + x

//        //areas
//        public HashSet<Area> containsAreas = new HashSet<Area>();//what areas it contains
//        public Area[] area;//area map

//        public VolumeDataPoint[] data;

//        //maps
//        public bool[]
//            //existance, //if true there some data
//            heightInterest, //if true there some height data that may be from nearby volumes
//            coverHeightInterest;//if true here height might be sampled for cover height. this done cause mixing with heightInterest array are not working that great for that purpose

//        //public float[] //in world space
//        //    max, //highest sample in that volume
//        //    min; //lowest sample in that volume 



//        public int[]
//            //passability, //values from Enums/Passability. right now: Unwalkable = 0, Slope = 1, Crouchable = 2, Walkable = 3 
//            flags, //flags are stored in bytes. bytes taken from Enums/VoxelState and there lots of flags. you might wanna add some!
//            hashMap, //hash are just convenient word. here are numbers for marching squares. number represent area and passability. it used PathFinder.GetAreaHash to get number and PathFinder.GetAreaByHash to return values
//            coverType, //0: none, 1: low, 2: high
//            coverHashMap; //share function of hashmap but here is only 3 possible numbers: MarchingSquaresIterator.COVER_HASH are positive flag, -1 negative flag and 0 is "no data" flag. marching squares are target only first

//        public int[][][] connections;//stored connected id on that volume. side, x, z. sides are from Enums/Directions. wich is xPlus = 0, xMinus = 1, zPlus = 2, zMinus = 3


//        //private int[] connections;
//        //int filter0, filter1, filter2, filter3;

//        //not really a map but share that purpose. position and VolumeArea here;
//        public Dictionary<int, HashSet<VolumeArea>> volumeArea = new Dictionary<int, HashSet<VolumeArea>>();
        

//        private Volume(VectorInt.Vector2Int size) {
//            this.size = size;
//            this.sizeX = size.x;
//            this.sizeZ = size.y;
//            flatenedLength = sizeX * sizeZ;
//        }

//        public int GetIndex(int x, int z) {
//            return (z * sizeX) + x;
//        }
        
//        #region POOL
//        public static Volume GetFromPool(int sizeX, int sizeZ, params Area[] areas) {
//            return GetFromPool(new VectorInt.Vector2Int(sizeX, sizeZ), areas);
//        }

//        public static Volume GetFromPool(VectorInt.Vector2Int size, params Area[] areas) {
//            if (areas.Length == 0)
//                throw new ArgumentException("Pulled volume must contain at least 1 Area as input");

//            Volume result = null;

//            lock (poolDictionary) {
//                Stack<Volume> stack;
//                if (poolDictionary.TryGetValue(size, out stack) == false) {
//                    stack = new Stack<Volume>();
//                    poolDictionary.Add(size, stack);

//                    for (int i = 0; i < INITIAL_POOL_SIZE; i++) {
//                        stack.Push(new Volume(size));
//                    }
//                }

//                result = stack.Count > 0 ? stack.Pop() : new Volume(size);
//            }

//            foreach (var item in areas) {
//                result.containsAreas.Add(item);
//            }           

//            int targetSize = size.x * size.y;


//            result.data = GenericPoolArray<VolumeDataPoint>.Take(targetSize);
//            result.flags = GenericPoolArray<int>.Take(targetSize);   
//            result.area = GenericPoolArray<Area>.Take(targetSize);

//            //result.connections = GenericPoolArray<int>.Take(targetSize);

//            //extra maps
//            result.hashMap = GenericPoolArray<int>.Take(targetSize);
//            result.heightInterest = GenericPoolArray<bool>.Take(targetSize);

//            result.coverHashMap = GenericPoolArray<int>.Take(targetSize);
//            result.coverHeightInterest = GenericPoolArray<bool>.Take(targetSize);
//            result.coverType = GenericPoolArray<int>.Take(targetSize);

//            result.InitConnections();
//            return result;
//        }

//        //return volume from pool and copy it's parameters
//        public static Volume GetFromPool(Volume volume, params Area[] areas) {
//            Volume result = GetFromPool(volume.size, areas);
//            foreach (var item in volume.containsAreas) {
//                result.containsAreas.Add(item);
//            }

//            result.tag = volume.tag;
//            result.layer = volume.layer;
//            result.dead = volume.dead;
//            result.isTerrain = volume.isTerrain;
//            result.isTree = volume.isTree;
//            return result;
//        }


//        public void ReturnToPool() {
//            GenericPoolArray<VolumeDataPoint>.ReturnToPool(ref data);
//            GenericPoolArray<int>.ReturnToPool(ref flags);
//            GenericPoolArray<Area>.ReturnToPool(ref area);

//            //GenericPoolArray<int>.ReturnToPool(ref connections);

//            //extra maps
//            GenericPoolArray<int>.ReturnToPool(ref hashMap);
//            GenericPoolArray<bool>.ReturnToPool(ref heightInterest);

//            GenericPoolArray<int>.ReturnToPool(ref coverHashMap);
//            GenericPoolArray<bool>.ReturnToPool(ref coverHeightInterest);
//            GenericPoolArray<int>.ReturnToPool(ref coverType);      


//            id = layer = -1;        
//            dead = isTerrain = isTree = false;
//            tag = null;
//            containsAreas.Clear();

//            lock (poolDictionary) {
//                poolDictionary[size].Push(this);
//            }
//        }

//        public static Volume Convert(VolumeSimple volume) {
//            Area volumeArea = volume.area;

//            VolumeDataPoint[] data = volume.data;
//            volume.data = null;
//            volume.ReturnToPool();

//            Volume result = GetFromPool(volume.size, volumeArea);        
//            result.data = data; 

//            Area[] area = result.area;
//            int length = result.flatenedLength;

//            for (int i = 0; i < length; i++) {
//                if (data[i].exist) 
//                    area[i] = volumeArea;                
//            }
  
//            return result;
//        }
//        #endregion

//        ////Connections Related
//        //private void InitConnections() {
//        //    filter0 = filter1 = filter2 = filter3 = -1;
//        //    int one8 = 255; //= 1111 1111
//        //    filter0 = filter0 ^ one8;
//        //    filter1 = filter1 ^ (one8 << 8);
//        //    filter2 = filter2 ^ (one8 << 16);
//        //    filter3 = filter3 ^ (one8 << 24);

//        //    SetConnection(0, Directions.xPlus, -1);
//        //    SetConnection(0, Directions.xMinus, -1);
//        //    SetConnection(0, Directions.zPlus, -1);
//        //    SetConnection(0, Directions.zMinus, -1);

//        //    int c = connections[0];

//        //    for (int i = 1; i < flatenedLength; i++) {
//        //        connections[i] = c;
//        //    }
//        //}

//        //public void SetConnection(int x, int z, Directions dir, sbyte value) {
//        //    SetConnection(GetIndex(x, z), dir, value);
//        //}
//        //public void SetConnection(int index, Directions dir, sbyte value) {
//        //    int intValue = value;
//        //    switch (dir) {
//        //        case Directions.xPlus:
//        //            connections[index] = (connections[index] & filter0) | intValue;
//        //            break;
//        //        case Directions.xMinus:
//        //            intValue = intValue << 8;
//        //            connections[index] = (connections[index] & filter1) | intValue;
//        //            break;
//        //        case Directions.zPlus:
//        //            intValue = intValue << 16;
//        //            connections[index] = (connections[index] & filter2) | intValue;
//        //            break;
//        //        case Directions.zMinus:
//        //            intValue = intValue << 24;
//        //            connections[index] = (connections[index] & filter3) | intValue;
//        //            break;
//        //    }
//        //}
//        //public sbyte GetConnection(int x, int z, Directions dir) {
//        //    return GetConnection(GetIndex(x, z), dir);
//        //}
//        //public sbyte GetConnection(int index, Directions dir) {
//        //    switch (dir) {
//        //        case Directions.xPlus:
//        //            return (sbyte)connections[index];
//        //        case Directions.xMinus:
//        //            return (sbyte)(connections[index] >> 8);
//        //        case Directions.zPlus:
//        //            return (sbyte)(connections[index] >> 16);
//        //        case Directions.zMinus:
//        //            return (sbyte)(connections[index] >> 24);
//        //        default:
//        //            return 0;
//        //    }
//        //}



//        //public void SetConnection(int x, int z, Directions direction, int value) {
//        //    connectionsOld[(int)direction][x][z] = value;
//        //}
//        //public int GetConnection(int x, int z, Directions direction) {
//        //    return connectionsOld[(int)direction][x][z];
//        //}




//        //create connection array on demand. cause not all volumes need it
//        public void InitConnections() {
//            connections = new int[4][][];
//            for (int i = 0; i < 4; i++) {
//                int[][] array = new int[sizeX][];

//                for (int x = 0; x < sizeX; x++) {
//                    array[x] = new int[sizeZ];
//                    for (int z = 0; z < sizeZ; z++) {
//                        array[x][z] = -1;
//                    }
//                }

//                connections[i] = array;
//            }
//        }

//        ///// <summary>
//        ///// for setting voxel data. this data ADDED to existed
//        ///// </summary>
//        //public void SetVoxel(int x, int z, float max, float min, Area area, int passability) {
//        //    int index = GetIndex(x, z);
//        //    if (existance[index]) {
//        //        this.max[index] = Math.Max(max, this.max[index]);
//        //        this.min[index] = Math.Min(min, this.min[index]);
//        //    }
//        //    else {
//        //        existance[index] = true;
//        //        this.max[index] = max;
//        //        this.min[index] = min;
//        //    }
//        //    this.area[index] = area;
//        //    this.passability[index] = Math.Max(passability, this.passability[index]);
//        //}
//        //public void SetVoxel(int index, float max, float min, Area area, int passability) {         
//        //    if (existance[index]) {
//        //        this.max[index] = Math.Max(max, this.max[index]);
//        //        this.min[index] = Math.Min(min, this.min[index]);
//        //    }
//        //    else {
//        //        existance[index] = true;
//        //        this.max[index] = max;
//        //        this.min[index] = min;
//        //    }
//        //    this.area[index] = area;
//        //    this.passability[index] = Math.Max(passability, this.passability[index]);
//        //}
//        public void SetVoxel(int x, int z, float min, float max, sbyte Passability, Area Area) {
//            int index = GetIndex(x, z);
//            data[index] = new VolumeDataPoint(min, max, Passability);
//            area[index] = Area;
//        }

//        public void SetVoxel(int x, int z, float height, sbyte Passability) {
//            int index = GetIndex(x, z);

//            if (data[index].exist)
//                data[index].UpdateUsual(height, Passability);
//            else
//                data[index] = new VolumeDataPoint(height, Passability);
//        }
//        public void SetVolumeMinimum(float value) {
//            for (int i = 0; i < flatenedLength; i++) {
//                data[i].min = value;
//            }    
//        }

//        public void SetArea(int x, int z, Area a) {
//            area[GetIndex(x, z)] = a;
//        }   
//        public void SetArea(Area a) {
//            for (int i = 0; i < flatenedLength; i++) {
//                area[i] = a;
//            }        
//        }
//        public void SetPassability(int x, int z, Passability p) {
//            data[GetIndex(x, z)].passability = (sbyte)p;
//        }

//        //existance
//        public bool Exist(VolumePos pos) {
//            return data[GetIndex(pos.x, pos.z)].exist;
//        }
//        public Passability Passability(VolumePos pos) {
//            return (Passability)data[GetIndex(pos.x, pos.z)].passability;
//        }

//        //flag data. flag stored in byte
//        public void SetState(int x, int z, VoxelState state, bool value) {
//            SetState(GetIndex(x, z), state, value);
//        }
//        public bool GetState(int x, int z, VoxelState state) {
//            return GetState(GetIndex(x, z), state);
//        }

//        public void SetState(int index, VoxelState state, bool value) {
//            flags[index] = value ? (flags[index] | (int)state) : (flags[index] & ~(int)state);
//        }
//        public bool GetState(int index, VoxelState state) {
//            return (flags[index] & (int)state) != 0;
//        }

//        public void ConnectToItself() {
//            //CreateConnectionsArray();

//            //int[][] xPlus = connections[(int)Directions.xPlus];
//            //int[][] zPlus = connections[(int)Directions.zPlus];
//            //int[][] xMinus = connections[(int)Directions.xMinus];
//            //int[][] zMinus = connections[(int)Directions.zMinus];
            
//            //for (int z = 0; z < sizeZ; z++) {
//            //    bool temp = data[GetIndex(0, z)].exist;
//            //    for (int x = 1; x < sizeX; x++) {
//            //        bool val = data[GetIndex(x, z)].exist;
//            //        if (temp && val) {
//            //            xPlus[x - 1][z] = id;
//            //            xMinus[x][z] = id;
//            //        }
//            //        temp = val;
//            //    }
//            //}

//            //for (int x = 0; x < sizeZ; x++) {
//            //    bool temp = data[GetIndex(x, 0)].exist;
//            //    for (int z = 1; z < sizeX; z++) {
//            //        bool val = data[GetIndex(x, z)].exist;
//            //        if (temp && val) {
//            //            zPlus[x][z - 1] = id;
//            //            zMinus[x][z] = id;
//            //        }
//            //        temp = val;
//            //    }
//            //}
//        }
//    }
//}