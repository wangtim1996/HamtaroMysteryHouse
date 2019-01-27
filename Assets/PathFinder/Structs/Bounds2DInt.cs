using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace K_PathFinder {
    [Serializable]
    public struct Bounds2DInt : IEquatable<Bounds2DInt> {
        public int minX, minY, maxX, maxY;

        public Bounds2DInt(int minX, int minY, int maxX, int maxY) {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
        }

        public int sizeX {
            get { return maxX - minX; }
        }
        public int sizeZ {
            get { return maxY - minY; }
        }

        public override int GetHashCode() {
            return minX ^ (minY * 10) ^ (maxX * 100) ^ (maxY * 1000);
        }

        public bool Equals(Bounds2DInt other) {
            return other == this;
        }

        public override bool Equals(object obj) {
            if (!(obj is Bounds2DInt))
                return false;

            return (Bounds2DInt)obj == this;
        }

        public static Bounds2DInt zero {
            get { return new Bounds2DInt(); }
        }

        public bool isZero {
            get { return minX == 0 && maxX == 0 && minY == 0 && maxY == 0; }
        }

        public static bool operator ==(Bounds2DInt a, Bounds2DInt b) {
            return
                a.minX == b.minX &&
                a.minY == b.minY &&
                a.maxX == b.maxX &&
                a.maxY == b.maxY;
        }

        public static bool operator !=(Bounds2DInt a, Bounds2DInt b) {
            return !(a == b);
        }

        public static Bounds2DInt GetIncluded(Bounds2DInt a, Bounds2DInt b) {
            return new Bounds2DInt(
              Math.Min(a.minX, b.minX),
              Math.Min(a.minY, b.minY),
              Math.Max(a.maxX, b.maxX),
              Math.Max(a.maxY, b.maxY));
        }

        public static bool Overlap(Bounds2DInt a, Bounds2DInt b) {
            if (a.minX <= b.maxX && a.maxX >= b.minX)
                return a.minY <= b.maxY && a.maxY >= b.minY;
            return false;
        }

        public bool Overlap(Bounds2DInt other) {
            if (minX <= other.maxX && maxX >= other.minX)
                return minY <= other.maxY && maxY >= other.minY;
            return false;
        }

        public bool Overlap(int x, int y) {
            return x >= minX && x <= maxX && y >= minY && y <= maxY;
        }

        public void Clamp() {

        }


        public override string ToString() {
            return string.Format("X: from {0} to {1}, Z: from {2} to {3}", minX, maxX, minY, maxY);
        }
    }
}

