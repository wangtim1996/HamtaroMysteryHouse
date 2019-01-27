using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace K_PathFinder {
    public enum ColliderInfoMode : int {
        Solid = 0,
        ModifyArea = 1,
        MakeHoleApplyArea = 2,
        MakeHoleRetainArea = 3
    }

    public enum ColliderInfoModeHidden : int {
        Trees = -1,
        Solid = 0,
        ModifyArea = 1,
        MakeHoleApplyArea = 2,
        MakeHoleRetainArea = 3
    }

    public enum AreaWorldModMagicValueType : int {
        Sphere = 1,    //value here represent amount of floats 
        Capsule = 2,   //value here represent amount of floats
        Cuboid = 3,    //value here represent amount of floats
    }

    //heavy junkyard
    [Serializable]
    public class AreaWorldModMagicValue {
        [SerializeField] public int id = -1;
        [SerializeField] public string name;    
        [SerializeField] public Vector3 position;
        [SerializeField] public Quaternion rotation;
        [SerializeField] public Bounds bounds;
        [SerializeField] protected bool dirty = true;

        [SerializeField] public AreaWorldModMagicValueType myType;
        [SerializeField] public float value1, value2, value3;

#if UNITY_EDITOR
        [SerializeField] public bool expanded = false;
        [NonSerialized] public Mesh capsuleMesh;//for drawing
#endif

        [NonSerialized] public AreaWorldMod container;

        public const float MIN_VALUE = 0.01f;

        public AreaWorldModMagicValue() {
            rotation = Quaternion.identity;
        }

        #region Ugh...
        public void SetValuesAsSphere(float radius) {
            this.radius = radius;
        }

        public void SetValuesAsCapsule(float radius, float height) {
            this.radius = radius;
            this.height = height;
        }

        public void SetValuesAsCuboid(float sizeX, float sizeY, float sizeZ) {
            value1 = sizeX;
            value2 = sizeY;
            value3 = sizeZ;
        }
        #endregion
        
        #region Ugh... x2
        public float radius {
            get { return value1; }
            set {
                value1 = value;
                if (value1 < MIN_VALUE)
                    value1 = MIN_VALUE;

                if (value2 < value1 * 2)
                    value2 = value1 * 2;
            }
        }

        public float height {
            get { return value2; }
            set {
                value2 = value;
                if (value2 < value1 * 2)
                    value2 = value1 * 2;
            }
        }

        public Vector3 cubeSize {
            get { return new Vector3(value1, value2, value3); }
            set {
                value1 = value.x;
                value2 = value.y;
                value3 = value.z;

                if (value1 < MIN_VALUE)
                    value1 = MIN_VALUE;
                if (value2 < MIN_VALUE)
                    value2 = MIN_VALUE;
                if (value3 < MIN_VALUE)
                    value3 = MIN_VALUE;
            }
        }
        #endregion

        public Matrix4x4 localToWorldMatrix {
            get { return Matrix4x4.TRS(position, rotation, Vector3.one); }
        }
        public Matrix4x4 worldToLocalMatrix {
            get { return localToWorldMatrix.inverse; }
        }

        public void SetDirty() {
            dirty = true;
        }

        public bool CheckDirty() {
            if (dirty) {
                dirty = false;
                RecalculateBounds();

#if UNITY_EDITOR
                switch (myType) {
                    case AreaWorldModMagicValueType.Cuboid:
                        break;
                    case AreaWorldModMagicValueType.Sphere:
                        break;
                    case AreaWorldModMagicValueType.Capsule:
                        UpdateCapsuleMesh();
                        break;
                }
#endif
                return true;
            }
            else
                return false;
        }

        public void RecalculateBounds() {
            Matrix4x4 G2L = container.localToWorldMatrix;
            Matrix4x4 L2W = localToWorldMatrix;
            float boundsMinX, boundsMinY, boundsMinZ, boundsMaxX, boundsMaxY, boundsMaxZ;

            switch (myType) {
                case AreaWorldModMagicValueType.Cuboid:
                    Vector3 size = cubeSize;

                    Vector3 point;
                    point = G2L.MultiplyPoint3x4(L2W.MultiplyPoint3x4(new Vector3(-size.x, -size.y, -size.z)));//back
                    boundsMinX = boundsMaxX = point.x;
                    boundsMinY = boundsMaxY = point.y;
                    boundsMinZ = boundsMaxZ = point.z;

                    point = G2L.MultiplyPoint3x4(L2W.MultiplyPoint3x4(new Vector3(-size.x, size.y, -size.z)));//back
                    if (point.x < boundsMinX) boundsMinX = point.x;
                    if (point.x > boundsMaxX) boundsMaxX = point.x;
                    if (point.y < boundsMinY) boundsMinY = point.y;
                    if (point.y > boundsMaxY) boundsMaxY = point.y;
                    if (point.z < boundsMinZ) boundsMinZ = point.z;
                    if (point.z > boundsMaxZ) boundsMaxZ = point.z;

                    point = G2L.MultiplyPoint3x4(L2W.MultiplyPoint3x4(new Vector3(-size.x, -size.y, size.z)));//back
                    if (point.x < boundsMinX) boundsMinX = point.x;
                    if (point.x > boundsMaxX) boundsMaxX = point.x;
                    if (point.y < boundsMinY) boundsMinY = point.y;
                    if (point.y > boundsMaxY) boundsMaxY = point.y;
                    if (point.z < boundsMinZ) boundsMinZ = point.z;
                    if (point.z > boundsMaxZ) boundsMaxZ = point.z;

                    point = G2L.MultiplyPoint3x4(L2W.MultiplyPoint3x4(new Vector3(-size.x, size.y, size.z)));//back
                    if (point.x < boundsMinX) boundsMinX = point.x;
                    if (point.x > boundsMaxX) boundsMaxX = point.x;
                    if (point.y < boundsMinY) boundsMinY = point.y;
                    if (point.y > boundsMaxY) boundsMaxY = point.y;
                    if (point.z < boundsMinZ) boundsMinZ = point.z;
                    if (point.z > boundsMaxZ) boundsMaxZ = point.z;

                    point = G2L.MultiplyPoint3x4(L2W.MultiplyPoint3x4(new Vector3(size.x, -size.y, -size.z)));//forward
                    if (point.x < boundsMinX) boundsMinX = point.x;
                    if (point.x > boundsMaxX) boundsMaxX = point.x;
                    if (point.y < boundsMinY) boundsMinY = point.y;
                    if (point.y > boundsMaxY) boundsMaxY = point.y;
                    if (point.z < boundsMinZ) boundsMinZ = point.z;
                    if (point.z > boundsMaxZ) boundsMaxZ = point.z;

                    point = G2L.MultiplyPoint3x4(L2W.MultiplyPoint3x4(new Vector3(size.x, size.y, -size.z)));//forward
                    if (point.x < boundsMinX) boundsMinX = point.x;
                    if (point.x > boundsMaxX) boundsMaxX = point.x;
                    if (point.y < boundsMinY) boundsMinY = point.y;
                    if (point.y > boundsMaxY) boundsMaxY = point.y;
                    if (point.z < boundsMinZ) boundsMinZ = point.z;
                    if (point.z > boundsMaxZ) boundsMaxZ = point.z;

                    point = G2L.MultiplyPoint3x4(L2W.MultiplyPoint3x4(new Vector3(size.x, -size.y, size.z)));//forward
                    if (point.x < boundsMinX) boundsMinX = point.x;
                    if (point.x > boundsMaxX) boundsMaxX = point.x;
                    if (point.y < boundsMinY) boundsMinY = point.y;
                    if (point.y > boundsMaxY) boundsMaxY = point.y;
                    if (point.z < boundsMinZ) boundsMinZ = point.z;
                    if (point.z > boundsMaxZ) boundsMaxZ = point.z;

                    point = G2L.MultiplyPoint3x4(L2W.MultiplyPoint3x4(new Vector3(size.x, size.y, size.z)));//forward
                    if (point.x < boundsMinX) boundsMinX = point.x;
                    if (point.x > boundsMaxX) boundsMaxX = point.x;
                    if (point.y < boundsMinY) boundsMinY = point.y;
                    if (point.y > boundsMaxY) boundsMaxY = point.y;
                    if (point.z < boundsMinZ) boundsMinZ = point.z;
                    if (point.z > boundsMaxZ) boundsMaxZ = point.z;

                    //haha
                    //much optimisation

                    bounds = new Bounds(
                        new Vector3((boundsMinX + boundsMaxX) * 0.5f, (boundsMinY + boundsMaxY) * 0.5f, (boundsMinZ + boundsMaxZ) * 0.5f),
                        new Vector3(boundsMaxX - boundsMinX, boundsMaxY - boundsMinY, boundsMaxZ - boundsMinZ));
                    break;
                case AreaWorldModMagicValueType.Sphere:
                    bounds = new Bounds(container.position + (container.rotation * position), new Vector3(radius * 2, radius * 2, radius * 2));
                    break;
                case AreaWorldModMagicValueType.Capsule:
                    Vector3 capsulePos = G2L.MultiplyPoint(position);
                    Vector3 posLow = capsulePos + (G2L.rotation * L2W.rotation * new Vector3(0, (-height * 0.5f) + radius, 0));
                    Vector3 posHigh = capsulePos + (G2L.rotation * L2W.rotation * new Vector3(0, (height * 0.5f) - radius, 0));

                    boundsMinX = posLow.x - radius;
                    boundsMinY = posLow.y - radius;
                    boundsMinZ = posLow.z - radius;

                    boundsMaxX = posLow.x + radius;
                    boundsMaxY = posLow.y + radius;
                    boundsMaxZ = posLow.z + radius;

                    boundsMinX = SomeMath.Min(boundsMinX, posHigh.x - radius);
                    boundsMinY = SomeMath.Min(boundsMinY, posHigh.y - radius);
                    boundsMinZ = SomeMath.Min(boundsMinZ, posHigh.z - radius);

                    boundsMaxX = SomeMath.Max(boundsMaxX, posHigh.x + radius);
                    boundsMaxY = SomeMath.Max(boundsMaxY, posHigh.y + radius);
                    boundsMaxZ = SomeMath.Max(boundsMaxZ, posHigh.z + radius);

                    bounds = new Bounds(
                        new Vector3((boundsMinX + boundsMaxX) * 0.5f, (boundsMinY + boundsMaxY) * 0.5f, (boundsMinZ + boundsMaxZ) * 0.5f),
                        new Vector3(boundsMaxX - boundsMinX, boundsMaxY - boundsMinY, boundsMaxZ - boundsMinZ));
                    break;    
            }
        }

#if UNITY_EDITOR
        public void DrawGizmos(float alpha) {
            Matrix4x4 matrixG2L = container.localToWorldMatrix;
            Gizmos.color = SetColorAlpha(Gizmos.color, alpha);
            switch (myType) {
                case AreaWorldModMagicValueType.Cuboid:
                    Gizmos.DrawMesh(Shapes.cubeMesh, matrixG2L.MultiplyPoint(position), matrixG2L.rotation * rotation, cubeSize * 2);
                    break;
                case AreaWorldModMagicValueType.Sphere:             
                    Gizmos.DrawSphere(matrixG2L.MultiplyPoint(position), radius);
                    break;
                case AreaWorldModMagicValueType.Capsule:               
                    if (capsuleMesh != null && capsuleMesh.vertices.Length != 0)
                        Gizmos.DrawMesh(capsuleMesh, matrixG2L.MultiplyPoint(position), matrixG2L.rotation * rotation, Vector3.one);
                    break;
            }
        }

        private static Color SetColorAlpha(Color color, float alpha) {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private void UpdateCapsuleMesh() {
            Vector3[] _capsuleVerts = Shapes.capsuleMesh.vertices;
            int[] _capsuleTris = Shapes.capsuleMesh.triangles;
            Vector3[] verts = new Vector3[_capsuleVerts.Length];

            float size = Mathf.Max(1f, height / (radius * 2f)) - 2f;
            Matrix4x4 capsuleShapeMatrix = Matrix4x4.Scale(new Vector3(radius * 2f, radius * 2f, radius * 2f));

            for (int index = 0; index < verts.Length; ++index) {
                if (_capsuleVerts[index].y > 0.0)
                    verts[index] = capsuleShapeMatrix.MultiplyPoint(
                        new Vector3(
                            _capsuleVerts[index].x,
                            Mathf.Max((float)(_capsuleVerts[index].y + size * 0.5), 0.0f),
                            _capsuleVerts[index].z));

                else if (_capsuleVerts[index].y < 0.0)
                    verts[index] = capsuleShapeMatrix.MultiplyPoint(
                        new Vector3(
                            _capsuleVerts[index].x,
                            Mathf.Min((float)(_capsuleVerts[index].y - size * 0.5), 0.0f),
                            _capsuleVerts[index].z));
            }

            if (capsuleMesh == null)
                capsuleMesh = new Mesh();

            capsuleMesh.vertices = verts;
            capsuleMesh.triangles = _capsuleTris;
            capsuleMesh.RecalculateNormals();
        }

#endif

        public AreaWorldModMagicValue Copy() {
            AreaWorldModMagicValue result = new AreaWorldModMagicValue();    
            result.name = name;
            //result.mode = mode;
            result.myType = myType;
            result.position = position;
            result.rotation = rotation;
            result.bounds = bounds;      
            result.value1 = value1;
            result.value2 = value2;
            result.value3 = value3;
            return result;
        }




        public static class Shapes {
            public static Mesh cubeMesh, sphereMesh, capsuleMesh;

            static Shapes() {
                CreatePrimitive(PrimitiveType.Cube, out cubeMesh);
                CreatePrimitive(PrimitiveType.Sphere, out sphereMesh);
                CreatePrimitive(PrimitiveType.Capsule, out capsuleMesh);
            }

            private static void CreatePrimitive(PrimitiveType type, out Mesh mesh) {
                GameObject primitive = GameObject.CreatePrimitive(type);
                mesh = primitive.GetComponent<MeshFilter>().sharedMesh;
                UnityEngine.Object.DestroyImmediate(primitive);
            }
        }
    }
}