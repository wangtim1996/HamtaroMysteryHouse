using UnityEngine;
using System.Collections;
using System;

namespace K_PathFinder.VectorInt  {
    public struct Vector4Int : IEquatable<Vector4Int> {
        readonly int _x, _y, _z, _w;
        #region constructors
        public Vector4Int(int x, int y, int z, int w) {
            _x = x;
            _y = y;
            _z = z;
            _w = w;
        }

        public Vector4Int(float x, float y, int z, int w) {
            _x = Mathf.RoundToInt(x);
            _y = Mathf.RoundToInt(y);
            _z = Mathf.RoundToInt(z);
            _w = Mathf.RoundToInt(w);
        }

        public Vector4Int(Vector4 v4) {
            _x = Mathf.RoundToInt(v4.x);
            _y = Mathf.RoundToInt(v4.y);
            _z = Mathf.RoundToInt(v4.z);
            _w = Mathf.RoundToInt(v4.w);
        }
        #endregion

        #region acessors
        public int x {
            get { return _x; }
        }
        public int y {
            get { return _y; }
        }
        public int z {
            get { return _z; }
        }
        public int w {
            get { return _w; }
        }
        #endregion
        public override int GetHashCode() {
            return _x ^ _y ^ _z ^ _w;
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is Vector4Int))
                return false;

            return Equals((Vector4Int)obj);
        }

        public bool Equals(Vector4Int other) {
            return other.x == x && other.y == y && other.z == z && other.w == w;
        }
    }
}
