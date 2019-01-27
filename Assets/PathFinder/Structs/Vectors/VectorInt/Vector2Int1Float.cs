using UnityEngine;
using System.Collections;
using System;

namespace K_PathFinder.VectorInt  {
    public struct Vector2Int1Float : IEquatable<Vector2Int1Float> {
        readonly int _x, _z;
        readonly float _y;

        public Vector2Int1Float(float x, float y, float z){
            _x = Mathf.RoundToInt(x);
            _y = y;
            _z = Mathf.RoundToInt(z);
        }

        public Vector2Int1Float(int x, float y, int z) {
            _x = x;
            _y = y;
            _z = z;
        }

        public Vector2Int1Float(Vector2Int XZ, float y) {
            _x = XZ.x;
            _y = y;
            _z = XZ.y;
        }

        #region acessors
        public int x {
            get { return _x; }
        }
        public float y {
            get { return _y; }
        }
        public int z {
            get { return _z; }
        }
        public static Vector2Int1Float zero {
            get { return new Vector2Int1Float(0,0,0); }
        }
        #endregion
        
        public override int GetHashCode() {
            return _x ^ (int)_y ^ _z;
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is Vector2Int1Float))
                return false;

            return Equals((Vector2Int1Float)obj);
        }

        public bool Equals(Vector2Int1Float other) {
            return other.x == x && other.y == y && other.z == z;
        }

        public override string ToString() {
            return "(x int: " + x + ", float y: " + y + ", int z: " + z + ")";
        }
    }
}
