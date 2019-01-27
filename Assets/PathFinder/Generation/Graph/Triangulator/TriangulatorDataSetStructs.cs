using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using K_PathFinder;

using K_PathFinder.EdgesNameSpace;
using K_PathFinder.GraphGeneration;
using K_PathFinder.Graphs;
using K_PathFinder.NodesNameSpace;
#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.GraphGeneration {
    public struct TriangulatorEdge : IEquatable<TriangulatorEdge> {
        public readonly int id, a, b;

        public TriangulatorEdge(int a, int b, int id) {
            this.a = a;
            this.b = b;
            this.id = id;
        }

        public bool Contains(int val1, int val2) {
            return a == val1 | b == val1 | a == val2 | b == val2;
        }

        public bool Contains(int val) {
            return a == val | b == val;
        }

        public override int GetHashCode() {
            return id;
        }

        public override bool Equals(object obj) {
            if (obj is TriangulatorEdge)
                return Equals((TriangulatorEdge)obj);
            else
                return false;
        }

        public bool Equals(TriangulatorEdge other) {
            return this == other;
        }

        internal int GetOtherNode(int nodeCurrent) {
            if (nodeCurrent == a)
                return b;
            if (nodeCurrent == b)
                return a;
            Debug.LogError("node not presented");
            return 0;
        }

        public static bool operator ==(TriangulatorEdge edgeA, TriangulatorEdge edgeB) {
            return (edgeA.a == edgeB.a && edgeA.b == edgeB.b) | (edgeA.a == edgeB.b && edgeA.b == edgeB.a);
        }
        public static bool operator !=(TriangulatorEdge edgeA, TriangulatorEdge edgeB) {
            return !(edgeA == edgeB);
        }

        public override string ToString() {
            return string.Format("id: {0}, a: {1}, b: {2}", id, a, b);
        }

    }
    public struct TriangulatorNode {
        public readonly int id;
        public readonly float x, y, z;

        public TriangulatorNode(float x, float y, float z, int id) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.id = id;
        }

        public TriangulatorNode(NodeAbstract node, int id) {
            this.x = node.x;
            this.y = node.y;
            this.z = node.z;
            this.id = id;
        }

        public Vector2 positionV2 {
            get { return new Vector2(x, z); }
        }
        public Vector3 positionV3 {
            get { return new Vector3(x, y, z); }
        }

        public override int GetHashCode() {
            return id;
        }

        public override bool Equals(object obj) {
            return obj is TriangulatorNode && (TriangulatorNode)obj == this;
        }

        public static bool operator ==(TriangulatorNode nodeA, TriangulatorNode nodeB) {
            return nodeA.id == nodeB.id && nodeA.x == nodeB.x && nodeA.y == nodeB.y && nodeA.z == nodeB.z;
        }
        public static bool operator !=(TriangulatorNode nodeA, TriangulatorNode nodeB) {
            return !(nodeA == nodeB);
        }
    }
    public struct TriangulatorNodeData {
        public readonly float cross, angle, normalX, normalZ;

        public TriangulatorNodeData(float cross, float angle, Vector2 normal) {
            this.cross = cross;
            this.angle = angle;
            this.normalX = normal.x;
            this.normalZ = normal.y;
        }

        public Vector2 normal {
            get { return new Vector2(normalX, normalZ); }
        }
    }
}