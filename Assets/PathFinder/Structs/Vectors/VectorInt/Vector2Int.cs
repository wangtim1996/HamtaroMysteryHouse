using UnityEngine;
using System;
using System.Collections;

namespace K_PathFinder.VectorInt {
    [Serializable]
	public struct Vector2Int : IEquatable<Vector2Int> {
        [SerializeField]
        readonly int _x, _y;

        #region constructors
        public Vector2Int(int x, int y){
            _x = x;
            _y = y;
		}

		public Vector2Int(float x, float y){
            _x = Mathf.RoundToInt(x);
            _y = Mathf.RoundToInt(y);
        }

        public Vector2Int(Vector2 v2){
            _x = Mathf.RoundToInt(v2.x);
            _y = Mathf.RoundToInt(v2.y);
        }
		
		#endregion

		#region acessors
		public int x{
			get{return _x;}
		}
		public int y{
			get{return _y;}
		}

		public int MinValue{
			get{return x > y ? y : x;}
		}

		public int MaxValue{
			get{return x > y ? x : y;}
		}

		public int ValueDifference{
			get{return Dif(this);}
		}
		#endregion

		#region math
		public float Length{
			get{return Mathf.Sqrt(LengthSquared);}
		}		
		public int LengthSquared{
			get{return (x * x) + (y * y);}
		}

		public static Vector2Int LowerToZero(Vector2Int value){
			return new Vector2Int(0, Dif(value));
		}

		public static int Dif(Vector2Int value){
			return Mathf.Abs(value.x - value.y);
		}

		public static Vector2Int Max(Vector2Int a, Vector2Int b){
			return new Vector2Int(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
		}
		public static Vector2Int Min(Vector2Int a, Vector2Int b){
			return new Vector2Int(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
		}

        public static float DistanceSqr(Vector2Int a, Vector2Int b) {
            return ((float)(b.x - a.x) * (float)(b.x - a.x)) + ((float)(b.y - a.y) * (float)(b.y - a.y));
        }

        public static float Distance(Vector2Int a, Vector2Int b) {
            return (float)Math.Sqrt(DistanceSqr(a, b));
        }
        #endregion

        #region operators
        public static explicit operator Vector2 (Vector2Int v2){
			return new Vector2(v2.x, v2.y);
		}

		public static Vector2Int operator +(Vector2Int a, Vector2Int b){
			return new Vector2Int(a.x + b.x, a.y + b.y);
		}
		public static Vector2Int operator -(Vector2Int a, Vector2Int b){
			return new Vector2Int(a.x - b.x, a.y - b.y);
		}

		public static Vector2Int operator *(Vector2Int a, int val){
			return new Vector2Int(a.x * val, a.y * val);
		}
		public static Vector2Int operator /(Vector2Int a, int val){
			return new Vector2Int(a.x / val, a.y / val);
		}

		public static Vector2Int operator *(Vector2Int a, float val){
			return new Vector2Int(Mathf.RoundToInt((float)a.x * val), Mathf.RoundToInt((float)a.y * val));
		}
		public static Vector2Int operator /(Vector2Int a, float val){
			return new Vector2Int(Mathf.RoundToInt((float)a.x / val), Mathf.RoundToInt((float)a.y / val));
		}

		public static bool operator ==(Vector2Int a, Vector2Int b){
            return a.x == b.x && a.y == b.y;
		}
		
		public static bool operator !=(Vector2Int a, Vector2Int b){
			return !(a == b);
		}
		
		public override int GetHashCode(){
			return _x ^ _y;
		}
		#endregion

        public bool HaveAny(int value) {
            return _x == value || _y == value;
        }

        public bool HaveAny(int value1, int value2) {
            return HaveAny(value1) || HaveAny(value2);
        }

		public override bool Equals(object obj){
			if (obj == null || !(obj is Vector2Int))
				return false;

            return Equals((Vector2Int)obj);
		}

        public bool Equals(Vector2Int other) {
            return other.x == x && other.y == y;
        }

        public override string ToString(){
            return string.Format("({0}, {1})", x, y);
		}

		public static Vector2Int zero{
			get { return new Vector2Int(0, 0); }
		}

		public static Vector2Int one{
            get {return new Vector2Int(1, 1);}
		}
    }
}