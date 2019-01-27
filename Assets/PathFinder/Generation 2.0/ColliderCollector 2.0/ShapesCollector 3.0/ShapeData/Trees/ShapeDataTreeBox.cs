using K_PathFinder.PFDebuger;
using UnityEngine;

namespace K_PathFinder.Collector {
    public class ShapeDataTreeBox : ShapeDataTreeAbstract {
        Matrix4x4 local2world;
        Matrix4x4 boxMatrix;

        private static Vector3[] box =
            new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, 0.5f)
            };

        public ShapeDataTreeBox(Matrix4x4 local2world, BoxCollider bc) {
            this.local2world = local2world;
            boxMatrix = Matrix4x4.TRS(bc.center, Quaternion.identity, bc.size);
        }

        public override ShapeDataAbstract ReturnShapeConstructor(Vector3 treeWorldPos, Vector3 treeWorldScale) {
            Matrix4x4 treeMatrix = Matrix4x4.TRS(treeWorldPos, Quaternion.identity, treeWorldScale);
            Matrix4x4 matrix = treeMatrix * boxMatrix * local2world;

            Vector3 cubePos = matrix.MultiplyPoint3x4(new Vector3(0f, 0f, 0f));
            Quaternion cubeRot = local2world.rotation;

            matrix = Matrix4x4.TRS(cubePos, cubeRot, Vector3.Scale(treeMatrix.lossyScale, local2world.lossyScale)) * Matrix4x4.Scale(boxMatrix.lossyScale);

            float
                minX, minY, minZ,
                maxX, maxY, maxZ;

            Vector3 vector = matrix.MultiplyPoint3x4(box[0]);

            minX = maxX = vector.x;
            minY = maxY = vector.y;
            minZ = maxZ = vector.z;

            for (int i = 1; i < box.Length; i++) {
                vector = matrix.MultiplyPoint3x4(box[i]);
                minX = Mathf.Min(vector.x, minX);
                minY = Mathf.Min(vector.y, minY);
                minZ = Mathf.Min(vector.z, minZ);
                maxX = Mathf.Max(vector.x, maxX);
                maxY = Mathf.Max(vector.y, maxY);
                maxZ = Mathf.Max(vector.z, maxZ);
            }

            Bounds bounds = new Bounds(
                new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f),
                new Vector3(maxX - minX, maxY - minY, maxZ - minZ));          

            return new ShapeDataBox(matrix, bounds, PathFinder.getUnwalkableArea);
        }
    }
}