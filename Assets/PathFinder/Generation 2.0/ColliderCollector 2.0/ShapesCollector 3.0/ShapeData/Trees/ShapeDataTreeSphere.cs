using K_PathFinder.PFDebuger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace K_PathFinder.Collector {
    public class ShapeDataTreeSphere : ShapeDataTreeAbstract {
        public float radius;
        Matrix4x4 local2world;
        Matrix4x4 sphereMatrix;

        public ShapeDataTreeSphere(Matrix4x4 local2world, SphereCollider sc) {   
            this.local2world = local2world;
            float radius = sc.radius;
            sphereMatrix = Matrix4x4.TRS(sc.center, Quaternion.identity, new Vector3(radius, radius, radius));
        }

        public override ShapeDataAbstract ReturnShapeConstructor(Vector3 worldPos, Vector3 worldScale) {
            Matrix4x4 treeMatrix = Matrix4x4.TRS(worldPos, Quaternion.identity, worldScale);
            Matrix4x4 matrix = treeMatrix * local2world * sphereMatrix;
            Vector3 pos = matrix.MultiplyPoint3x4(Vector3.zero);
            Vector3 size = matrix.MultiplyPoint3x4(Vector3.one) - pos;
            float radius = SomeMath.Max(size.x, size.y, size.z);
            Bounds bounds = new Bounds(pos, new Vector3(radius, radius, radius) * 2);
            return new ShapeDataSphere(bounds, PathFinder.getUnwalkableArea);
        }
    }
}