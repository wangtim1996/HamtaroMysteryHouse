using K_PathFinder.Collector;
using K_PathFinder.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using K_PathFinder.Rasterization;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder {
    public partial class ColliderCollector {
        #region primitives
        public static Vector3[] cubeVerts;
        public static Vector3[] sphereVerts;
        public static Vector3[] capsuleVerts;
        public static int[] cubeTris;
        public static int[] sphereTris;
        public static int[] capsuleTris;
        
        public static void InitCollector() {
            CreatePrimitive(PrimitiveType.Cube, out cubeVerts, out cubeTris);
            CreatePrimitive(PrimitiveType.Sphere, out sphereVerts, out sphereTris);
            CreatePrimitive(PrimitiveType.Capsule, out capsuleVerts, out capsuleTris);
        }

        private static void CreatePrimitive(PrimitiveType type, out Vector3[] verts, out int[] tris) {
            GameObject primitive = GameObject.CreatePrimitive(type);
            Mesh result = primitive.GetComponent<MeshFilter>().sharedMesh;
            verts = result.vertices;
            tris = result.triangles;
            Object.DestroyImmediate(primitive);
        }
        #endregion

        public struct MeshDataForGPU {
            public string name;
            public Vector3[] verts;
            public int[] tris;
            public Area area;
            public Matrix4x4 matrix;
            public Bounds bounds;
            public ColliderInfoMode infoMode;

            public MeshDataForGPU(
                Vector3[] verts, int[] tris, Area area, Matrix4x4 matrix, Bounds bounds, //very userful
                string name, ColliderInfoMode infoMode) {//semi userful
                this.verts = verts;
                this.tris = tris;
                this.area = area;
                this.matrix = matrix;
                this.bounds = bounds;
                this.name = name;
                this.infoMode = infoMode;
            }
        }

        private struct ComputeShaderResultHolder {     
            public CSRasterization3DResult result;
            public ColliderInfoMode infoMode;
            public Area area;

            public ComputeShaderResultHolder(CSRasterization3DResult result, ColliderInfoMode infoMode, Area area) {    
                this.result = result;
                this.infoMode = infoMode;
                this.area = area;
            }
        }

        List<ComputeShaderResultHolder> collectedComputeShaderData = new List<ComputeShaderResultHolder>();

        //results
        public List<KeyValuePair<AreaWorldMod, Collector3.ShapeCollector>> shapeCollectorGenericGPU_mods;
        
        private void AddColliderGenericGPU(Collider collider) {
            Matrix4x4 matrix = Matrix4x4.identity;
            Transform colliderTransform = collider.transform;
            Vector3[] verts = null;
            int[] tris = null;
            Bounds bounds = collider.bounds;

            if (collider is BoxCollider) {
                verts = cubeVerts;
                tris = cubeTris;
                matrix = Matrix4x4.TRS(bounds.center, colliderTransform.rotation, Vector3.Scale(colliderTransform.lossyScale, (collider as BoxCollider).size));
            }

            else if (collider is SphereCollider) {
                verts = sphereVerts;
                tris = sphereTris;
                float r = bounds.extents.x / 0.5f;
                matrix = Matrix4x4.TRS(bounds.center, Quaternion.identity, new Vector3(r, r, r));
            }

            else if (collider is CapsuleCollider) {
                tris = capsuleTris;
                verts = new Vector3[capsuleVerts.Length];
                CapsuleCollider capsuleCollider = collider as CapsuleCollider;

                Vector3 lossyScale = colliderTransform.lossyScale;
                float height = capsuleCollider.height * lossyScale.y;
                float radius = capsuleCollider.radius * Mathf.Max(lossyScale.x, lossyScale.z);

                float size = Mathf.Max(1f, height / (radius * 2f)) - 2f;
                Matrix4x4 capsuleShapeMatrix = Matrix4x4.Scale(new Vector3(radius * 2f, radius * 2f, radius * 2f));

                for (int index = 0; index < verts.Length; ++index) {
                    if (capsuleVerts[index].y > 0.0)
                        verts[index] = capsuleShapeMatrix.MultiplyPoint(
                            new Vector3(
                                capsuleVerts[index].x,
                                Mathf.Max((float)(capsuleVerts[index].y + size * 0.5), 0.0f),
                                capsuleVerts[index].z));

                    else if (capsuleVerts[index].y < 0.0)
                        verts[index] = capsuleShapeMatrix.MultiplyPoint(
                            new Vector3(
                                capsuleVerts[index].x,
                                Mathf.Min((float)(capsuleVerts[index].y - size * 0.5), 0.0f),
                                capsuleVerts[index].z));
                }

                matrix = Matrix4x4.TRS(
                    capsuleCollider.bounds.center,//actual world position
                    colliderTransform.rotation * //world rotation
                    Quaternion.Euler(//plus capsule orientation
                        capsuleCollider.direction == 2 ? 90f : 0.0f,
                        0.0f,
                        capsuleCollider.direction == 0 ? 90f : 0.0f),
                    Vector3.one);
            }

            else if (collider is CharacterController) {
                tris = capsuleTris;
                verts = new Vector3[capsuleVerts.Length];
                CharacterController charControler = collider as CharacterController;

                Vector3 lossyScale = colliderTransform.lossyScale;
                float height = charControler.height * lossyScale.y;
                float radius = charControler.radius * Mathf.Max(lossyScale.x, lossyScale.z);

                float size = Mathf.Max(1f, height / (radius * 2f)) - 2f;
                Matrix4x4 capsuleShapeMatrix = Matrix4x4.Scale(new Vector3(radius * 2f, radius * 2f, radius * 2f));

                for (int index = 0; index < verts.Length; ++index) {
                    if (capsuleVerts[index].y > 0.0)
                        verts[index] = capsuleShapeMatrix.MultiplyPoint(new Vector3(capsuleVerts[index].x, Mathf.Max((float)(capsuleVerts[index].y + size * 0.5), 0.0f), capsuleVerts[index].z));
                    else if (capsuleVerts[index].y < 0.0)
                        verts[index] = capsuleShapeMatrix.MultiplyPoint(new Vector3(capsuleVerts[index].x, Mathf.Min((float)(capsuleVerts[index].y - size * 0.5), 0.0f), capsuleVerts[index].z));
                }

                matrix = Matrix4x4.TRS(charControler.bounds.center, colliderTransform.rotation, Vector3.one);
            }

            else if (collider is MeshCollider) {
                Mesh curMesh = (collider as MeshCollider).sharedMesh;
                verts = curMesh.vertices;
                tris = curMesh.triangles;
                matrix = colliderTransform.localToWorldMatrix;
            }

            else {
                Debug.LogFormat("hey i dont know wich collider type is on {0}. please tell developer to fix that.", collider.gameObject.name);
                return;
            }

            var gameObjectArea = collider.transform.GetComponent<AreaGameObject>();
            Area area;
            if (gameObjectArea != null) {
                area = PathFinder.GetArea(gameObjectArea.areaInt);
            }
            else {
                if (PathFinder.settings.checkRootTag) {
                    area = PathFinderSettings.tagAssociations[collider.transform.root.tag];
                }
                else {
                    area = PathFinderSettings.tagAssociations[collider.transform.tag];
                }
            }

            if(verts != null & tris != null) {
                float maxSlopeCos = Mathf.Cos(template.maxSlope * Mathf.PI / 180f);
                Vector3 offsetedPos = template.realOffsetedPosition;
   

                CSRasterization3DResult result = PathFinder.sceneInstance.Rasterize3D(
                    verts, tris, bounds, matrix,
                    template.lengthX_extra, template.lengthZ_extra,
                    offsetedPos.x, offsetedPos.z,
                    template.voxelSize,
                    maxSlopeCos,
                    false,
                    false);

                if (result != null)
                    collectedComputeShaderData.Add(new ComputeShaderResultHolder(result, ColliderInfoMode.Solid, area));
            }
        }

        private void CollectCollidersGPU(Collector3.ShapeCollector collector) {
            if (profiler != null) profiler.AddLog("Start shape collecting by GPU");
            int collectedSolidShapes = 0;
            foreach (var resultHolder in collectedComputeShaderData) {
                if(resultHolder.infoMode == ColliderInfoMode.Solid) {
                    collector.AppendComputeShaderResult(resultHolder.result, resultHolder.area);
                    collectedSolidShapes++;
                }
            }
            if (profiler != null) profiler.AddLogFormat("End shape collecting by GPU. Added {0} shapes", collectedSolidShapes);
        }
    }
}