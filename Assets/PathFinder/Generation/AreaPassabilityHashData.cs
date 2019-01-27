using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace K_PathFinder {
    /// <summary>
    /// this thing stored dictionary of area and passability as int and transfer it back and forth
    /// it exist this way cause PathFinder have one static AreaPassabilityHashData, but when needed it can clone current 
    /// main AreaPassabilityHashData to different threads. cause it was locked big chunk of time it cost some perfomance
    /// so this is way around.
    /// also a bit more readable
    /// </summary>
    public class AreaPassabilityHashData {
        private HashSet<Area> _globalPool, _localPool;

        //private Dictionary<AreaPassabilityPair, int> _areaToHash;
        //private Dictionary<int, AreaPassabilityPair> _hashToArea;

        public Area[] areaByIndex = new Area[0];
        public Dictionary<Area, byte> areaToIndex = new Dictionary<Area, byte>();

        public const sbyte COVER_HASH = 1;
        public const sbyte INVALID_COVER_HASH = -1;

        public const short INVALID_HASH_NUMBER = -1;
        public const short HASH_MASK_PASSABILITY = 15;
        public const short HASH_MASK_AREA = 4080;

        //rules
        //hash stored in single short : 16 bits
        //-1 : 1111111111111111 is invalid number
        //0000 0000 0000 1111 - for storing Passability (4 bits) = 15
        //0000 1111 1111 0000 - for storing Area index (8 bits) = 4080


        //private HashSet<Area> _areaPool;
        //private Dictionary<AreaPassabilityPair, int> _areaToHash;
        //private Dictionary<int, AreaPassabilityPair> _hashToArea;


        public AreaPassabilityHashData() {
            _globalPool = new HashSet<Area>();
            _localPool = new HashSet<Area>();
        }


        private AreaPassabilityHashData(AreaPassabilityHashData origin) {
            _globalPool = new HashSet<Area>(origin._globalPool);
            _localPool = new HashSet<Area>(origin._localPool);

            if (origin._localPool.Count != 0) {
                Debug.LogWarning("Pthfinder expect to not have local area while it makes copy itself but somehow it exists");
            }
        }

        //do it in threads after you finish to adding thread related areas
        public void AssignData() {
            //_areaToHash = new Dictionary<AreaPassabilityPair, int>();
            //_hashToArea = new Dictionary<int, AreaPassabilityPair>();

            ////reson is + 1 cause we can use ID:0 to tell "just do nothing" instead of ID:-1 later on (in layer hashmap for example)
            //int keys = 1;

            //foreach (var area in _globalPool) {
            //    AssignPair(area, Passability.Walkable, keys++);
            //    AssignPair(area, Passability.Crouchable, keys++);
            //}

            //foreach (var area in _localPool) {
            //    AssignPair(area, Passability.Walkable, keys++);
            //    AssignPair(area, Passability.Crouchable, keys++);
            //}

            areaByIndex = new Area[_globalPool.Count + _localPool.Count];
            foreach (var item in _globalPool) {
                areaByIndex[item.id] = item;
            }

            int index = _globalPool.Count;
            foreach (var item in _localPool) {
                if (areaByIndex[index] != null)
                    Debug.LogError("Something is not right. Here you expect to have ordered by index areas but somehow it is not ordered");
                areaByIndex[index++] = item;
            }

            if(index > byte.MaxValue) {
                Debug.LogError("Amount of areas at single chunk are too large");
            }

            areaToIndex = new Dictionary<Area, byte>();

            for (int i = 0; i < areaByIndex.Length; i++) {
                if(areaByIndex[i] == null) {
                    Debug.LogError("Something is not right. Somehpw there is null in area array");
                }
                areaToIndex.Add(areaByIndex[i], (byte)i);
            }

        }

        //private void AssignPair(Area area, Passability pass, int key) {
        //    AreaPassabilityPair pair = new AreaPassabilityPair(area, pass);
        //    _areaToHash.Add(pair, key);
        //    _hashToArea.Add(key, pair);
        //}

        //difference betwin global and not global area
        public void AddAreaHash(Area area, bool isGlobalArea) {
            if (area == null)
                Debug.LogError("you try to create area hash using null");

            if (isGlobalArea) {
                _globalPool.Add(area);
            }
            else {
                _localPool.Add(area);
            }            
        }

        public void RemoveAreaHash(Area area) {
            if (area == null)
                Debug.LogError("you try to remove null from area hash");

            _globalPool.Remove(area);
            _localPool.Remove(area);
        }

        public short GetAreaHash(Area area, Passability passability) {
            return GetAreaHash(areaToIndex[area], (byte)passability);
        }

        //public int GetAreaHash(byte area, sbyte pass) {
        //    return GetAreaHash(new AreaPassabilityPair(areaByIndex[area], (Passability)pass));
        //}

        public static short GetAreaHash(byte area, byte pass) {
            return (short)((area << 4) | (pass));
        }

        public void GetAreaByHash(short hash, out Area area, out Passability passability) {
            int hashInt = hash;
            area = areaByIndex[hashInt >> 4];
            passability = (Passability)(hashInt & 15);
        }

        public short[] GetAllHashes() {
            short[] result = new short[areaByIndex.Length * 4];

            for (int i = 0; i < areaByIndex.Length; i++) {
                result[(i * 4) + 0] = GetAreaHash(areaByIndex[i], Passability.Unwalkable);
                result[(i * 4) + 1] = GetAreaHash(areaByIndex[i], Passability.Slope);
                result[(i * 4) + 2] = GetAreaHash(areaByIndex[i], Passability.Crouchable);
                result[(i * 4) + 3] = GetAreaHash(areaByIndex[i], Passability.Walkable);
            }
            return result;
        }

        //public string DescribeHashes() {
        //    StringBuilder sb = new StringBuilder();
        //    foreach (var item in _areaToHash) {
        //        sb.AppendFormat("area: {0}, passbility: {1}, hash: {2} \n", item.Key.area.name, item.Key.passability, item.Value);
        //    }
        //    return sb.ToString();
        //}

        public AreaPassabilityHashData Clone() {
            //Debug.Log("hash data cloned");
            return new AreaPassabilityHashData(this);
        }


        public struct AreaPassabilityPair : IEqualityComparer<AreaPassabilityPair> {
            public Area area;
            public Passability passability;

            public AreaPassabilityPair(Area area, Passability passability) {
                if (area == null)
                    Debug.LogError("Area Pair cant be created with null");
                this.area = area;
                this.passability = passability;
            }

            public static bool operator ==(AreaPassabilityPair a, AreaPassabilityPair b) {
                return ReferenceEquals(a.area, b.area) && a.passability == b.passability;
            }

            public static bool operator !=(AreaPassabilityPair a, AreaPassabilityPair b) {
                return !(a == b);
            }

            public override bool Equals(object obj) {
                if (obj is AreaPassabilityPair == false)
                    return false;

                return (AreaPassabilityPair)obj == this;
            }

            public bool Equals(AreaPassabilityPair a, AreaPassabilityPair b) {
                return ReferenceEquals(a.area, b.area) && a.passability == b.passability;
            }

            public override int GetHashCode() {
                return area.GetHashCode() ^ ((int)passability * 5000);
            }

            public int GetHashCode(AreaPassabilityPair obj) {
                return obj.GetHashCode();
            }
        }
    }
}
