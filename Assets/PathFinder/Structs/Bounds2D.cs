using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder {
    [Serializable]
    public struct Bounds2D : IEquatable<Bounds2D> {
        public float minX, minY, maxX, maxY;

        public Bounds2D(float MinX, float MinY, float MaxX, float MaxY) {
            minX = MinX;
            minY = MinY;
            maxX = MaxX;
            maxY = MaxY;
        }

        public Bounds2D(float centerX, float centerY, float expansion) {
            minX = centerX - expansion;
            minY = centerY - expansion;
            maxX = centerX + expansion;
            maxY = centerY + expansion;
        }


        public float centerX {
            get { return (minX + maxX) * 0.5f; }
        }
        public float centerY {
            get { return (minY + maxY) * 0.5f; }
        }

        public Vector2 center {
            get { return new Vector2(centerX, centerY); }
        }
        public Vector3 centerVector3 {
            get { return new Vector3(centerX, 0, centerY); }
        }

        public Vector2 min {
            get { return new Vector2(minX, minY); }
        }
        public Vector3 minVector3 {
            get { return new Vector3(minX, 0, minY); }
        }

        public Vector2 max {
            get { return new Vector2(maxX, maxY); }
        }
        public Vector3 maxVector3 {
            get { return new Vector3(maxX, 0, maxY); }
        }

        public static bool Overlap(Bounds2D a, Bounds2D b) {
            if (a.minX <= b.maxX && a.maxX >= b.minX)
                return a.minY <= b.maxY && a.maxY >= b.minY;
            return false;
        }

        public bool Overlap(Bounds2D other) {
            if (minX <= other.maxX && maxX >= other.minX)
                return minY <= other.maxY && maxY >= other.minY;
            return false;
        }

        public bool Overlap(float x, float y) {
            return x >= minX && x <= maxX && y >= minY && y <= maxY;
        }

        public bool Enclose(Bounds2D other) {
            return minX <= other.minX && minY <= other.minY && maxX >= other.maxX && maxY >= other.maxY;
        }

        public static bool operator ==(Bounds2D a, Bounds2D b) {
            return
                a.minX == b.minX &&
                a.minY == b.minY &&
                a.maxX == b.maxX &&
                a.maxY == b.maxY;
        }

        public static bool operator !=(Bounds2D a, Bounds2D b) {
            return !(a == b);
        }

        public bool Equals(Bounds2D other) {
            return other == this;
        }

        public override bool Equals(object obj) {
            if (!(obj is Bounds2D))
                return false;

            return (Bounds2D)obj == this;
        }

        public override int GetHashCode() {
            return (int)(minX + minY + maxX + maxY);
        }

#if UNITY_EDITOR
        public void Draw(Color color, float height) {
            Debuger_K.AddLine(minVector3, new Vector3(maxX, height, minY), color);
            Debuger_K.AddLine(minVector3, new Vector3(minX, height, maxY), color);
            Debuger_K.AddLine(maxVector3, new Vector3(maxX, height, minY), color);
            Debuger_K.AddLine(maxVector3, new Vector3(minX, height, maxY), color);
        }


#endif
    }
}
