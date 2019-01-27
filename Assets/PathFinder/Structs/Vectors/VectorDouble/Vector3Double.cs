using UnityEngine;
using System.Collections;
using System;

namespace K_PathFinder.VectorDouble {
	public class Vector3Double {
		double[] vector;

		public Vector3Double(float x, float y, float z){
			vector = new double[3]{(double)x, (double)y, (double)z};
		}
		
		public Vector3Double(double x, double y, double z){
			vector = new double[3]{x, y, z};
		}

		
		public Vector3Double(Vector3 v3) : this(v3.x, v3.y, v3.z){}

		#region acessors
		public double x{
			get{return vector[0];}
			set{vector[0] = value;}
		}
		public double y{
			get{return vector[1];}
			set{vector[1] = value;}
		}
		public double z{
			get{return vector[2];}
			set{vector[2] = value;}
		}
		#endregion

		#region math
		public double SqrLength{
			get{return Math.Abs((vector[0] * vector[0]) + (vector[1] * vector[1]) + (vector[2] * vector[2]));}
		}

		public double Length{
			get{return Math.Sqrt(SqrLength);}
		}

		public Vector3Double normalized{
			get{return Normalize(this);}
		}

		public static Vector3Double Normalize(Vector3Double input){
			double length = input.Length;
			return new Vector3Double(input.x / length, input.y / length, input.z / length);
		}
		#endregion

		#region operators
		public static explicit operator Vector3 (Vector3Double v3){
			return new Vector3((float)v3.vector[0], (float)v3.vector[1], (float)v3.vector[2]);
		}
		
		public static Vector3Double operator + (Vector3Double a, Vector3Double b){
			return new Vector3Double(a.x + b.x, a.y + b.y,  a.z + b.z);
		}
		public static Vector3Double operator - (Vector3Double a, Vector3Double b){
			return new Vector3Double(a.x - b.x, a.y - b.y,  a.z - b.z);
		}
		
		public static Vector3Double operator * (Vector3Double a, float val){
			return new Vector3Double(a.x * val, a.y * val,  a.z * val);
		}

		public static Vector3Double operator / (Vector3Double a, float val){
			return new Vector3Double(a.x / val, a.y / val,  a.z / val);
		}	

		public static Vector3Double operator * (Vector3Double a, double val){
			return new Vector3Double(a.x * val, a.y * val,  a.z * val);
		}
		
		public static Vector3Double operator / (Vector3Double a, double val){
			return new Vector3Double(a.x / val, a.y / val,  a.z / val);
		}

		
		public static bool operator == (Vector3Double a, Vector3Double b){
			if (System.Object.ReferenceEquals(a, b))
				return true;			
			
			if (((object)a == null) || ((object)b == null))
				return false;
			
			return a.x == b.x && a.y == b.y && a.z == b.z;
		}
		
		public static bool operator !=(Vector3Double a, Vector3Double b){
			return !(a == b);
		}

		public override bool Equals(object obj){
			if (obj == null || !(obj is Vector3Double))
				return false;

			Vector3Double v3 = (Vector3Double)obj;
			return v3.x == x && v3.y == y && v3.z == z;
		}	

		public override int GetHashCode(){
			return (int)(x + y + z / z);
		}
		#endregion

		public static Vector3Double TwoVertexNormal(Vector3Double first, Vector3Double second){
			return (first.z*second.x) - (first.x*second.z) < 0 ? 
				(first.normalized + second.normalized).normalized * -1 : 
					(first.normalized + second.normalized).normalized;
		}

		public static Vector3Double TwoVertexNormal(Vector3 first, Vector3 second){
			return TwoVertexNormal(new Vector3Double(first), new Vector3Double(second));
		}

		public static Vector3Double Round(Vector3Double v3, int dec){
			return new Vector3Double(Math.Round(v3.x, dec), Math.Round(v3.y, dec), Math.Round(v3.z, dec));
		}
	}
}