using UnityEngine;
using System.Collections;
using System;

namespace K_PathFinder.VectorDouble {
    public class Vector2Double {
        double[] vector;

        public Vector2Double(double x, double y) {
            vector = new double[2] { x, y };
        }

        public Vector2Double(Vector2 v2) : this(v2.x, v2.y) { }

        #region acessors
        public double x {
            get { return vector[0]; }
            set { vector[0] = value; }
        }
        public double y {
            get { return vector[1]; }
            set { vector[1] = value; }
        }
        #endregion

        #region math
        public double SqrLength {
            get { return Math.Abs((x * x) + (y * y)); }
        }

        public double Length {
            get { return Math.Sqrt(SqrLength); }
        }

        public Vector2Double normalized {
            get { return Normalize(this); }
        }

        public static Vector2Double Normalize(Vector2Double input) {
            double length = input.Length;
            return new Vector2Double(input.x / length, input.y / length);
        }
        #endregion

        #region operators
        public static explicit operator Vector2(Vector2Double v2) {
            return new Vector2((float)v2.x, (float)v2.y);
        }

        public static Vector2Double operator +(Vector2Double a, Vector2Double b) {
            return new Vector2Double(a.x + b.x, a.y + b.y);
        }
        public static Vector2Double operator -(Vector2Double a, Vector2Double b) {
            return new Vector2Double(a.x - b.x, a.y - b.y);
        }

        public static Vector2Double operator *(Vector2Double a, float val) {
            return new Vector2Double(a.x * val, a.y * val);
        }

        public static Vector2Double operator /(Vector2Double a, float val) {
            return new Vector2Double(a.x / val, a.y / val);
        }

        public static Vector2Double operator *(Vector2Double a, double val) {
            return new Vector2Double(a.x * val, a.y * val);
        }

        public static Vector2Double operator /(Vector2Double a, double val) {
            return new Vector2Double(a.x / val, a.y / val);
        }


        public static bool operator ==(Vector2Double a, Vector2Double b) {
            if (ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null))
                return false;

            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(Vector2Double a, Vector2Double b) {
            return !(a == b);
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is Vector2Double))
                return false;

            Vector2Double v3 = (Vector2Double)obj;
            return v3.x == x && v3.y == y;
        }

        public override int GetHashCode() {
            return (int)x ^ (int)y ^ (int)(x + y);
        }
        #endregion
        
        public static Vector2Double TwoVertexNormal(Vector2Double first, Vector2Double second) {
            return (first.normalized + second.normalized).normalized * Mathf.Sign((float)((first.y * second.x) - (first.x * second.y)));
        }
        
        public static Vector2Double TwoVertexNormalExpensive(Vector2 first, Vector2 second) {
            return TwoVertexNormalExpensive(new Vector2Double(first), new Vector2Double(second));
        }

        public static Vector2Double TwoVertexNormalExpensive(Vector2Double first, Vector2Double second) {
            var sum = first.normalized + second.normalized;
            if (sum != zero)
                return (sum.normalized * Mathf.Sign((float)((first.y * second.x) - (first.x * second.y)))).normalized;
            else 
                return new Vector2Double(first.y, -first.x);
        }

        public static Vector2Double Round(Vector2Double vector2, int dec) {
            return new Vector2Double(Math.Round(vector2.x, dec), Math.Round(vector2.y, dec));
        }

        public static Vector2Double zero {
            get { return new Vector2Double(0, 0); }
        }
    }
}