using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using K_PathFinder.Serialization;
using K_PathFinder.Rasterization;
//using K_PathFinder.RVOPF;
using System.Linq;
using K_PathFinder.Settings;


#if UNITY_EDITOR
using UnityEditor;
using K_PathFinder.PFDebuger;
#endif


namespace K_PathFinder {
    //also this thing responsible for compute shader rasterization
    [ExecuteInEditMode()]
    public class PathFinderScene : MonoBehaviour {
        [SerializeField]public SceneNavmeshData sceneNavmeshData;      
        bool _areInit = false;
        Dictionary<int, IEnumerator> coroutineDictionary = new Dictionary<int, IEnumerator>();

#if UNITY_EDITOR
        WaitForEndOfFrame wfeof = new WaitForEndOfFrame();
#endif


        //compute shader stuff
        CSRasterization3D CSR3D;
        CSRasterization2D CSR2D;

        //SERIALIZED DATA
        //cause unity cant serialize it other way around
        [SerializeField] public GameObject[] gameObjectLibrary;
        //SERIALIZED DATA

#if UNITY_EDITOR
        [NonSerialized] Vector3[] gizmosVerts = new Vector3[8];
        [NonSerialized] int[] gizmosTris = new int[] {
            0,6,2,0,4,6, //side 1
            0,2,6,0,6,4, //side 1 inside
            2,7,3,2,6,7, //side 2
            2,3,7,2,7,6, //side 2 inside
            3,5,1,3,7,5, //side 3
            3,1,5,3,5,7, //side 3 inside
            1,4,0,1,5,4, //side 4
            1,0,4,1,4,5, //side 4 inside
        };
        [NonSerialized] Mesh gizmosMesh = null;
#endif

#if UNITY_EDITOR
        void OnEditorUpdate() {
            if (_areInit == false)
                return;

            foreach (var item in coroutineDictionary.Values) {
                item.MoveNext();
            }       
        }

        void OnEnable() {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            DebugerOnEnable();


        }
        void OnDisable() {
            DebugerOnDisable();
        }

        void OnRenderObject() {
            DebugerOnRenderObject();
        }
#endif

        void OnDestroy() {
#if UNITY_EDITOR
            EditorApplication.update -= OnEditorUpdate;
            DebugerOnDestroy();
#endif
                        
            PathFinder.CallThisWhenSceneObjectWasGone();

            CSR3D = null;
            CSR2D = null;
        }
#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (PathFinderSettings.isAreaPointerMoving) {
                float gs = PathFinder.gridSize;
                AreaPointer areaPointer = PathFinder.settings.areaPointer;
                float y = areaPointer.y;

                Color gColor = Gizmos.color;
                Gizmos.color = SetAlpha(Color.yellow, 0.3f);
                
                int startX = areaPointer.roundStartX;
                int startZ = areaPointer.roundStartZ;
                int endX = areaPointer.roundEndX;
                int endZ = areaPointer.roundEndZ;
                int sizeX = areaPointer.roundSizeX;
                int sizeZ = areaPointer.roundSizeZ;

                if (sizeX < 100 && sizeZ > 1) {
                    for (int x = startX; x < endX; x++) {
                        Gizmos.DrawLine(new Vector3(x * gs, y, startZ * gs), new Vector3(x * gs, y, endZ * gs));
                    }
                }

                if (sizeZ < 100 && sizeX > 1) {
                    for (int z = startZ; z < endZ; z++) {
                        Gizmos.DrawLine(new Vector3(startX * gs, y, z * gs), new Vector3(endX * gs, y, z * gs));
                    }
                }

                Gizmos.color = SetAlpha(Color.red, 0.2f);

                gizmosVerts[0] = new Vector3(startX * gs, 0, startZ * gs);
                gizmosVerts[1] = new Vector3(startX * gs, 0, endZ * gs);
                gizmosVerts[2] = new Vector3(endX * gs, 0, startZ * gs);
                gizmosVerts[3] = new Vector3(endX * gs, 0, endZ * gs);

                float gY = y >= 0 ? y + 1 : y - 1;
                gizmosVerts[4] = new Vector3(startX * gs, gY, startZ * gs);
                gizmosVerts[5] = new Vector3(startX * gs, gY, endZ * gs);
                gizmosVerts[6] = new Vector3(endX * gs, gY, startZ * gs);
                gizmosVerts[7] = new Vector3(endX * gs, gY, endZ * gs);

                gizmosMesh.vertices = gizmosVerts;
                gizmosMesh.RecalculateBounds();

                Gizmos.DrawMesh(gizmosMesh);
                Gizmos.color = gColor;
            }
        }
#endif
        public static Color SetAlpha(Color color, float alpha) {
            return new Color(color.r, color.g, color.b, alpha);
        }

        public void AddCoroutine(int key, IEnumerator iEnumerator) {
            coroutineDictionary[key] = iEnumerator;
        }

        public void Init() {
            if (_areInit)
                return;

            _areInit = true;

            //Debug.Log("PathFinderScene init");

            int maxCount = PathFinder.settings.areaLibrary.Count;
            foreach (var item in FindObjectsOfType<TerrainNavmeshSettings>()) {
                int[] data = item.data;

                for (int i = 0; i < item.data.Length; i++) {
                    if (data[i] > maxCount) {
                        Debug.LogWarningFormat("on {0} terrain in data index of area was higher than it possible can be. fix it! for now it will be default/", item.gameObject.name);
                        data[i] = 0;
                    }
                }
            }
                 
            StopAllCoroutines();

            foreach (var item in coroutineDictionary.Values) {
                StartCoroutine(item);
            }   
        }

        public void InitComputeShaderRasterization3D(ComputeShader shader) {
            if (CSR3D != null)
                return;
            CSR3D = new CSRasterization3D(shader);
        }
        public void InitComputeShaderRasterization2D(ComputeShader shader) {
            if (CSR2D != null)
                return;
            CSR2D = new CSRasterization2D(shader);
        }
        
        public CSRasterization3DResult Rasterize3D(Vector3[] verts, int[] tris, Bounds bounds, Matrix4x4 matrix, int volumeSizeX, int volumeSizeZ, float chunkPosX, float chunkPosZ, float voxelSize, float maxSlopeCos, bool flipY, bool debug) {
            return CSR3D.Rasterize(verts, tris, bounds, matrix, volumeSizeX, volumeSizeZ, chunkPosX, chunkPosZ, voxelSize, maxSlopeCos, flipY, debug);
        }
        public CSRasterization2DResult Rasterize2D(Vector3[] verts, int[] tris, int volumeSizeX, int volumeSizeZ, float chunkPosX, float chunkPosZ, float voxelSize, float maxSlopeCos, bool debug = false) {
            return CSR2D.Rasterize(verts, tris, volumeSizeX, volumeSizeZ, chunkPosX, chunkPosZ, voxelSize, maxSlopeCos, debug);
        }

        public void StopAll() {
            StopAllCoroutines();
        }
        public void Shutdown() {
            StopAll();
            _areInit = false;
        }
        
#if UNITY_EDITOR
        //*************************************************************//
        //************************DEBUGER STUFF************************//
        //*************************************************************//

        //buffer holders
        MaterialAndBufferHolder[] data;
        Action onDebugerUpdateDelegate;

        enum Indexes : int {
            ImportantDot = 0,
            ImportantLine = 1,
            ImportantTris = 2,
            Path = 3,
            GenericDot = 4,
            GenericLine = 5,
            GenericMesh = 6,
        }

        //name of parameters in shader
        private const string SHADER_DOT_PARAMETER_NAME = "point_data";
        private const string SHADER_LINE_PARAMETER_NAME = "line_data";
        private const string SHADER_TRIANGLE_PARAMETER_NAME = "triangle_data";

        //size of struct
        private const int STRIDE_DOT = (sizeof(float) * (3 + 4 + 1));
        private const int STRIDE_LINE = (sizeof(float) * (3 + 3 + 4 + 1));
        private const int STRIDE_TRIS = (sizeof(float) * (3 + 3 + 3 + 4));

        //buffer control
        public void InitDebugBuffers() {
            if (data != null && data.All(x => x.validate)) return;

            Shader dotShader = PFDSettings.GetDotShader();
            Shader lineShader = PFDSettings.GetLineShader();
            Shader trisShader = PFDSettings.GetTrisShader();

            data = new MaterialAndBufferHolder[7];
            data[(int)Indexes.ImportantDot] = new MaterialAndBufferHolder(dotShader, SHADER_DOT_PARAMETER_NAME, STRIDE_DOT);
            data[(int)Indexes.ImportantLine] = new MaterialAndBufferHolder(lineShader, SHADER_LINE_PARAMETER_NAME, STRIDE_LINE);
            data[(int)Indexes.ImportantTris] = new MaterialAndBufferHolder(trisShader, SHADER_TRIANGLE_PARAMETER_NAME, STRIDE_TRIS);
            data[(int)Indexes.Path] = new MaterialAndBufferHolder(lineShader, SHADER_LINE_PARAMETER_NAME, STRIDE_LINE);
            data[(int)Indexes.GenericDot] = new MaterialAndBufferHolder(dotShader, SHADER_DOT_PARAMETER_NAME, STRIDE_DOT);
            data[(int)Indexes.GenericLine] = new MaterialAndBufferHolder(lineShader, SHADER_LINE_PARAMETER_NAME, STRIDE_LINE);
            data[(int)Indexes.GenericMesh] = new MaterialAndBufferHolder(trisShader, SHADER_TRIANGLE_PARAMETER_NAME, STRIDE_TRIS);
        }
        private void ReleaseBuffer() {
            if (data != null) {
                foreach (var d in data) {
                    d.ReleaseBuffer();
                }
            }
        }
        
        //important changes in big patches and dont changes very often
        public void UpdateImportantData(List<PointData> dotList, List<LineData> lineList, List<TriangleData> trisList) {
            lock (dotList)
                data[(int)Indexes.ImportantDot].UpdateBuffer(dotList.ToArray());

            lock (lineList)
                data[(int)Indexes.ImportantLine].UpdateBuffer(lineList.ToArray());

            lock (trisList)
                data[(int)Indexes.ImportantTris].UpdateBuffer(trisList.ToArray());
        }

        //path and generic in smaller patches
        public void UpdatePathData(List<LineData> lineList) {
            lock (lineList)
                data[(int)Indexes.Path].UpdateBuffer(lineList.ToArray());
        }
        public void UpdateGenericDots(List<PointData> dotList) {
            lock (dotList)
                data[(int)Indexes.GenericDot].UpdateBuffer(dotList.ToArray());

        }
        public void UpdateGenericLines(List<LineData> lineList) {
            lock (lineList)
                data[(int)Indexes.GenericLine].UpdateBuffer(lineList.ToArray());
        }
        public void UpdateGenericTris(List<TriangleData> trisList) {
            lock (trisList)
                data[(int)Indexes.GenericMesh].UpdateBuffer(trisList.ToArray());
        }

        //on unity something
        void DebugerOnEnable() {
            Debuger_K.ForceInit();
            InitDebugBuffers();

             gizmosMesh = new Mesh() {
                 vertices = gizmosVerts,
                 triangles = gizmosTris
             };
            gizmosMesh.RecalculateNormals();

            if (Application.isPlaying == false) {
                EditorApplication.update -= DebugerUpdate;
                EditorApplication.update += DebugerUpdate;
            }



            StartCoroutine(DebugerUpdateCoroutine());
        }
        void DebugerOnDisable() {
            ReleaseBuffer();
            EditorApplication.update -= DebugerUpdate;
        }
        void DebugerOnDestroy() {
            ReleaseBuffer();
            EditorApplication.update -= DebugerUpdate;
        }
        void DebugerOnRenderObject() {
            if (data == null)
                return;

            for (int i = 0; i < 7; i++) {
                if (data[i].bufferLength > 0) {
                    data[i].material.SetPass(0);
                    Graphics.DrawProcedural(MeshTopology.Points, data[i].bufferLength);
                }
            }
        }

        public void DebugerSetUpdateDelegate(Action UpdateDelegate) {
            onDebugerUpdateDelegate = UpdateDelegate;
        }

        IEnumerator DebugerUpdateCoroutine() {
            while (true) {
                yield return wfeof;
                DebugerUpdate();
                if (Debuger_K.settings.clearGenericOnUpdate)
                    Debuger_K.ClearGeneric();
            }
        }

        void DebugerUpdate() {
            if (onDebugerUpdateDelegate != null)
                onDebugerUpdateDelegate.Invoke();
        }

        class MaterialAndBufferHolder {
            public Material material;
            public ComputeBuffer buffer;
            public int bufferLength = 0;
            public string parameterName;
            public int stride;

            public MaterialAndBufferHolder(Shader shader, string ParameterName, int ParameterSize) {
                material = new Material(shader);
                parameterName = ParameterName;
                stride = ParameterSize;
            }

            public void UpdateBuffer(Array array) {
                bufferLength = array.Length;

                if (buffer != null)
                    buffer.Release();

                if (bufferLength > 0) {
                    buffer = new ComputeBuffer(array.Length, stride);
                    buffer.SetData(array);
                    material.SetBuffer(parameterName, buffer);
                }
            }

            public void ReleaseBuffer() {
                if (buffer != null) {
                    buffer.Release();
                    buffer = null;
                }
            }

            public bool validate {
                get { return material != null && parameterName != null; }
            }
        }
        //*************************************************************//
        //************************DEBUGER STUFF************************//
        //*************************************************************//
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PathFinderScene))]
    public class LevelScriptEditor : Editor {
        public override void OnInspectorGUI() {
            PathFinderScene myTarget = (PathFinderScene)target;

            myTarget.sceneNavmeshData = (SceneNavmeshData)EditorGUILayout.ObjectField(new GUIContent("Navmesh Data",
                    "Scriptable object with serialized NavMesh data. You can save all current data to it in pathfinder menu. Later it will be loaded from here"), 
                    myTarget.sceneNavmeshData, typeof(SceneNavmeshData), false);            

            if (GUILayout.Button("Load")) {
                PathFinder.LoadCurrentSceneData();
            }
        }
    }
#endif
}