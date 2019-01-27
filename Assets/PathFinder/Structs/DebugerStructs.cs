using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace K_PathFinder.PFDebuger {
    //data that go to shader buffer
    public struct PointData {
        public Vector3 pos;
        public Color color;
        public float size;

        public PointData(Vector3 Pos, Color Color, float Size) {
            pos = Pos;
            color = Color;
            size = Size;
        }
    }
    //data that go to shader buffer
    public struct LineData {
        public Vector3 a;
        public Vector3 b;
        public Color color;
        public float width;

        public LineData(Vector3 A, Vector3 B, Color Color, float Width) {
            a = A;
            b = B;
            color = Color;
            width = Width;
        }
    }
    //data that go to shader buffer
    public struct TriangleData {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public Color color;

        public TriangleData(Vector3 A, Vector3 B, Vector3 C, Color Color) {
            a = A;
            b = B;
            c = C;
            color = Color;
        }
    }
}