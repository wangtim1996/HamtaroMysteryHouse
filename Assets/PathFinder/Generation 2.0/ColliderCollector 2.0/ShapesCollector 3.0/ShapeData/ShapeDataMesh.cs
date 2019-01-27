using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using K_PathFinder.Pool;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.Collector {
    public class ShapeDataMesh : ShapeDataAbstract {
        public Vector3[] verts;
        public int[] tris;
        public Matrix4x4 matrix;
        public bool convex;

        //just for solid
        public ShapeDataMesh(MeshCollider collider, Area area) : base(collider, area) {
            Mesh curMesh = collider.sharedMesh;
            verts = curMesh.vertices;
            tris = curMesh.triangles;
            matrix = collider.transform.localToWorldMatrix;
            convex = collider.convex;
        }

        public ShapeDataMesh(Mesh mesh, GameObject target, Bounds bounds, Area area, bool convex) : base(target.name, area, bounds, ColliderInfoMode.Solid) {     
            verts = mesh.vertices;
            tris = mesh.triangles;
            matrix = target.transform.localToWorldMatrix;
            this.convex = convex;
        }
    }
}