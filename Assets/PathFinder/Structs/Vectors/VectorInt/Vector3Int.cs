using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace K_PathFinder.VectorInt {  
	public struct Vector3Int : IEquatable<Vector3Int> {
        readonly int _x;
        readonly int _y;
        readonly int _z;

        #region constructors
        public Vector3Int(int x, int y, int z) {
            _x = x;
            _y = y;
            _z = z;
        }

        public Vector3Int(float x, float y, int z) {
            _x = Mathf.RoundToInt(x);
            _y = Mathf.RoundToInt(y);
            _z = Mathf.RoundToInt(z);
        }

        public Vector3Int(Vector3 v3) {
            _x = Mathf.RoundToInt(v3.x);
            _y = Mathf.RoundToInt(v3.y);
            _z = Mathf.RoundToInt(v3.z);
        }
		#endregion

		#region acessors
		public int x{
			get{return _x;}
		}
		public int y{
			get{return _y;}
		}
		public int z{
			get{return _z;}
		}
		#endregion

		#region math
		public float Length{
			get{return Mathf.Sqrt(SqrLength);}
		}		
		public int SqrLength{
			get{return Math.Abs((x * x) + (y * y) + (z * z));}
		}

		public static Vector3Int Max(Vector3Int a, Vector3Int b){
			return new Vector3Int(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z));
		}

		public static Vector3Int Min(Vector3Int a, Vector3Int b){
			return new Vector3Int(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z));
		}

		public static float Distance(Vector3Int a, Vector3Int b){
			return (a - b).Length;
		}

        #endregion

        public static Vector3Int ToVector3Int(Vector3 vector){
			return new Vector3Int(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y), Mathf.RoundToInt(vector.z));
		}

		#region operators
		public static explicit operator Vector3 (Vector3Int vector3){
			return new Vector3(vector3.x, vector3.y, vector3.z);
		}

        public static Vector3Int operator +(Vector3Int a, Vector3Int b){
			return new Vector3Int(a.x + b.x, a.y + b.y,  a.z + b.z);
		}
		public static Vector3Int operator -(Vector3Int a, Vector3Int b){
			return new Vector3Int(a.x - b.x, a.y - b.y,  a.z - b.z);
		}

		public static Vector3Int operator *(Vector3Int a, int val){
			return new Vector3Int(a.x * val, a.y * val,  a.z * val);
		}
		public static Vector3Int operator /(Vector3Int a, int val){
			return new Vector3Int(a.x / val, a.y / val,  a.z / val);
		}

		public static Vector3Int operator *(Vector3Int a, float val){
			return new Vector3Int(Mathf.RoundToInt((float)a.x * val), Mathf.RoundToInt((float)a.y * val),  Mathf.RoundToInt((float)a.z * val));
		}
		public static Vector3Int operator /(Vector3Int a, float val){
			return new Vector3Int(Mathf.RoundToInt((float)a.x / val), Mathf.RoundToInt((float)a.y / val),  Mathf.RoundToInt((float)a.z / val));
		}

		public static bool operator ==(Vector3Int a, Vector3Int b){
			if (System.Object.ReferenceEquals(a, b))
				return true;			

			if (((object)a == null) || ((object)b == null))
				return false;

			return a.x == b.x && a.y == b.y && a.z == b.z;
		}
		
		public static bool operator !=(Vector3Int a, Vector3Int b){
			return !(a == b);
		}

		
		public override int GetHashCode(){
			return _x ^ _y ^ _z;
		}
        #endregion

        public override bool Equals(object obj) {
            if (obj == null || !(obj is Vector3Int))
                return false;

            return Equals((Vector3Int)obj);
        }

        public bool Equals(Vector3Int other) {
            return other.x == x && other.y == y && other.z == z;
        }

        public override string ToString(){
            return string.Format("({0}, {1}, {2})", _x, _y, _z);
		}

        public static Vector3Int zero{
			get{return new Vector3Int(0,0,0);}
		}

		public static Vector3Int one{
			get{return new Vector3Int(1,1,1);}
		}
	}
}