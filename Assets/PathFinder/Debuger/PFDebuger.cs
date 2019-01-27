#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Linq;

using K_PathFinder.PFDebuger.Helpers;
using K_PathFinder.NodesNameSpace;
using K_PathFinder.EdgesNameSpace;
using K_PathFinder.Graphs;
using K_PathFinder.CoverNamespace;
using System.Text;

using UnityEditor;
using K_PathFinder.GraphGeneration;

namespace K_PathFinder.PFDebuger {    
    public enum DebugGroup {
        line = 0,
        dot = 1,
        label = 2,
        mesh = 3,
        path = 4
    }    

    public enum DebugOptions : int {
        Cell = 0,
        CellArea = 1,
        CellEdges = 2,
        CellConnection = 3,
        Cover = 4,
        Grid = 5,
        JumpBase = 6,
        Voxels = 7,
        VoxelPos = 8,
        VoxelConnection = 9,
        VoxelLayer = 10,
        VoxelRawMax = 11,
        VoxelRawMin = 12,
        VoxelRawVolume= 13,   
        ChunkBounds = 14,
        ColliderBounds = 15,
        NodesAndConnections = 16,
        NodesAndConnectionsPreRDP = 17,
        WalkablePolygons = 18,
        Triangulator = 19
    }

    public static class Debuger_K{
        public static PFDSettings settings;
        private static Vector2 debugScrollPos;

        private static PathFinderScene sceneObj;
        private static bool areInit = false;

        private static bool
            needPathUpdate,               //overall
            needGenericDotUpdate,         //overall
            needGenericLineUpdate,        //overall
            needGenericTrisUpdate;        //overall        


        //gui stuff and debug arrays
        private static bool[] debugFlags;
        private const int FLAGS_AMOUNT = 20;
        private static GUIContent[] labels;
        private static GUIContent dividerBoxLabel = new GUIContent();
        private static GUILayoutOption[] dividerThing = new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) };
        private static string genericDebugToolTip = "Some options for control stuff added by Debuger.AddSomthing";

        private static Dictionary<GeneralXZData, ChunkDebugInfo> debugData = new Dictionary<GeneralXZData, ChunkDebugInfo>();
        private static Queue<Action> commands = new Queue<Action>();

        //lock
        private static object lockObj = new object();

        #region generic
        private static List<HandleThing> labelsDebug = new List<HandleThing>();
        private static List<PointData> genericDots = new List<PointData>();
        private static List<LineData> genericLines = new List<LineData>();
        private static List<TriangleData> genericTris = new List<TriangleData>();
        private static List<LineData> pathDebug = new List<LineData>();
        #endregion

        private static int cellCounter;
        private static int coversCounter;
        private static int jumpBasesCounter;
        private static int voxelsCounter;

        private static bool _stop = false;

        public static bool UserfulPublicFlag = false;

        static Debuger_K() {
            labels = new GUIContent[FLAGS_AMOUNT];
            labels[(int)DebugOptions.Cell] = new GUIContent("Cell", "Convex area inside navmesh that connected with other Cells");
            labels[(int)DebugOptions.CellArea] = new GUIContent("Cell Area", "Representation of Cell shape");
            labels[(int)DebugOptions.CellConnection] = new GUIContent("Cell Connection", "Representation of Cell connections to ther Cells");
            labels[(int)DebugOptions.CellEdges] = new GUIContent("Cell Edge", "Representation of Cell borders");
            labels[(int)DebugOptions.Cover] = new GUIContent("Cover", "Representation of covers in Scene. Flat surfaces represet Height. lines with dot represent where it connected to existed navmesh");
            labels[(int)DebugOptions.Grid] = new GUIContent("Grid", "Representation of grid");
            labels[(int)DebugOptions.JumpBase] = new GUIContent("Jumb Base", "Representation of spots siutable for jump checks");
            labels[(int)DebugOptions.Voxels] = new GUIContent("Voxels", "Mixels of data that transformed into NavMesh");
            labels[(int)DebugOptions.VoxelPos] = new GUIContent("Voxel Pos", "Upper position of Voxel");  
            labels[(int)DebugOptions.VoxelConnection] = new GUIContent("Voxel Connection", "Connections of voxel grid to each other");
            labels[(int)DebugOptions.VoxelLayer] = new GUIContent("Voxel Layer", "Layer sepparations betwin voxels. All Voxels splited into 2d sheets");
            labels[(int)DebugOptions.VoxelRawMax] = new GUIContent("Voxel Raw Max", "Raw data of voxel maximum height");
            labels[(int)DebugOptions.VoxelRawMin] = new GUIContent("Voxel Raw Min", "Raw data of voxel minimum height");
            labels[(int)DebugOptions.VoxelRawVolume] = new GUIContent("Voxel Raw Volume", "Raw data that shot where minimal and maximal height are layed");
            labels[(int)DebugOptions.ChunkBounds] = new GUIContent("Chunk Bounds", "Chunk bounds. Any object outside chunk bounds are ignored");
            labels[(int)DebugOptions.ColliderBounds] = new GUIContent("Collider Bounds", "Collider bounds that participate into NavMesh generation");
            labels[(int)DebugOptions.NodesAndConnections] = new GUIContent("Nodes Info", "Basic nodes information");
            labels[(int)DebugOptions.NodesAndConnectionsPreRDP] = new GUIContent("Nodes Info Pre RDP", "Basic nodes information before they simplified");
            labels[(int)DebugOptions.WalkablePolygons] = new GUIContent("Walkable Polygons", "Graphical represenation of walkable polygons");
            labels[(int)DebugOptions.Triangulator] = new GUIContent("Triangulator", "Triangulator pipeline debug");
        } 
           
        public static void Init() {
            if (areInit)
                return;

            sceneObj = PathFinder.sceneInstance;
            if (sceneObj == null)
                PathFinder.Init("debug init");
            sceneObj = PathFinder.sceneInstance;

            LoadSettings();

            sceneObj.InitDebugBuffers();

            sceneObj.DebugerSetUpdateDelegate(() => {  
                if (_stop)
                    return;

                //Debug.LogFormat("{0}, {1}, {2}, {3}", needPathUpdate, needGenericDotUpdate, needGenericLineUpdate, needGenericTrisUpdate);
                //path
                if (needPathUpdate) {
                    needPathUpdate = false;   
                    if(settings.doDebugPaths) {
                        sceneObj.UpdatePathData(pathDebug);
                    }
                }

                //generic dots
                if (needGenericDotUpdate) {
                    needGenericDotUpdate = false;
                    if (settings.drawGenericDots) {
                        sceneObj.UpdateGenericDots(genericDots);
                    }
        
                }

                //generic lines
                if (needGenericLineUpdate) {
                    needGenericLineUpdate = false;
                    if (settings.drawGenericLines) {
                        sceneObj.UpdateGenericLines(genericLines);
                    }

                }
          
                //generic tris
                if (needGenericTrisUpdate) {
                    needGenericTrisUpdate = false;
                    if (settings.drawGenericMesh) {
                        sceneObj.UpdateGenericTris(genericTris);
                    }
                }

                lock (commands) {
                    if(commands.Count > 0) {
                        while (commands.Count > 0) {
                            commands.Dequeue().Invoke();
                        }
                    }
                }
            });

            areInit = true;
        }

        public static void ForceInit() {
            areInit = false;
            Init();
        }

        private static void LoadSettings() {
            if(settings == null)
                settings = PFDSettings.LoadSettings();

            var curFlags = settings.debugFlags;
            if (settings.debugFlags == null) {
                settings.debugFlags = new bool[FLAGS_AMOUNT];
            }
            settings.debugFlags = new bool[FLAGS_AMOUNT]; 
            for (int i = 0; i < Math.Min(FLAGS_AMOUNT, curFlags.Length); i++) {
                settings.debugFlags[i] = curFlags[i];
            }

            if (settings.optionColors == null) {
                settings.optionColors = new List<Color>();
                settings.optionIsShows = new List<bool>();
            }

            if (settings.optionColors.Count != settings.optionIsShows.Count) {
                Debug.LogWarning("somehow debug options list count of colors and showings are not equal. fixing it");
                settings.optionColors = new List<Color>();
                settings.optionIsShows = new List<bool>();
            }      
        }

        public static void SetSettingsDirty() {
            EditorUtility.SetDirty(settings);
        }
        
        public static void ClearChunksDebug() {
            cellCounter = 0;
            coversCounter = 0;
            jumpBasesCounter = 0;
            voxelsCounter = 0;

            foreach (var info in debugData.Values) {
                info.Clear();
            }  

            UpdateSceneImportantThings();
        }

        public static void QueueClearChunksDebug(GeneralXZData data) {
            commands.Enqueue(() => { ClearChunksDebug(data); });
        }
        public static void ClearChunksDebug(GeneralXZData data) {
            bool changed = false;
            lock (lockObj) {
                ChunkDebugInfo info;
                if (debugData.TryGetValue(data, out info)) {
                    cellCounter -= info.cellCounter;
                    coversCounter -= info.coversCounter;
                    jumpBasesCounter -= info.jumpBasesCounter;
                    voxelsCounter -= info.voxelsCounter;
                    info.Clear();
                    changed = true;
                }                
            }
            if(changed)
                QueueUpdateSceneImportantThings();
        }


        public static void DrawSceneGUI() {
            //if (PathFinder.activeCreationWork != 0 | PathFinder.haveActiveThreds | settings.showSceneGUI == false)
            //    return;

            //lock (debugData) {
            //    foreach (var chunkDictionary in debugData) {
            //        Vector3 pos = chunkDictionary.Key.centerV3;
            //        Vector3 screenPoint = Camera.current.WorldToViewportPoint(pos);
            //        if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1) {
            //            Vector3 screenPosition = Camera.current.WorldToScreenPoint(pos);

            //            GUILayout.BeginArea(new Rect(new Vector2(screenPosition.x, Screen.height - screenPosition.y), new Vector2(400, 400)));
            //            lock (chunkDictionary.Value) {
            //                foreach (var agentDictionary in chunkDictionary.Value) {
            //                    GUILayout.BeginHorizontal();
            //                    agentDictionary.Value.showMe = GUILayout.Toggle(agentDictionary.Value.showMe, "", GUILayout.MaxWidth(10));
            //                    GUILayout.Box(agentDictionary.Key.name);
            //                    GUILayout.EndHorizontal();
            //                }
            //            }
            //            GUILayout.EndArea();
            //        }
            //    }
            //}
        }

        public static void STOP() {
            _stop = true;
        }

        public static bool doDebug {
            get {return areInit && settings.doDebug;}
        }
        public static bool debugOnlyNavMesh {
            get { return areInit && settings.debugOnlyNavmesh == false; }
        }
        public static bool useProfiler {
            get { return areInit && settings.doProfilerStuff; }
        }        
        public static bool debugPath {
            get { return areInit && settings.doDebugPaths; }
        }

        //rvo
        public static bool debugRVO {
            get { return areInit && settings.debugRVO; }
        }
        public static bool debugRVObasic {
            get { return areInit && settings.debugRVObasic; }
        }
        public static bool debugRVOvelocityObstacles {
            get { return areInit && settings.debugRVOvelocityObstacles; }
        }
        public static bool debugRVOconvexShape {
            get { return areInit && settings.debugRVOconvexShape; }
        }
        public static bool debugRVOplaneIntersections {
            get { return areInit && settings.debugRVOplaneIntersections; }
        }

        public static void DrawDebugLabels() {
            lock (labelsDebug) {
                if (settings.drawGenericLabels) {
                    for (int i = 0; i < labelsDebug.Count; i++) {
                        labelsDebug[i].ShowHandle();
                    }
                }
            }
        }
                        
        public static void SellectorGUI2() {
            settings.autoUpdateSceneView = GUILayout.Toggle(settings.autoUpdateSceneView, "auto update scene view");

            if (GUILayout.Button("Update")) {
                UpdateSceneImportantThings();
            }
            GUILayout.Box(dividerBoxLabel, dividerThing);
            var flags = settings.debugFlags;

            GUILayout.Label(string.Format(
                "Cells: {0}\nVoxels :{1}\nCovers: {2}\nJump Bases: {3}", cellCounter, voxelsCounter, coversCounter, jumpBasesCounter
                ), GUILayout.ExpandWidth(false));
            GUILayout.Box(dividerBoxLabel, dividerThing);
            //cells
            flags[(int)DebugOptions.Cell] = EditorGUILayout.Toggle(labels[(int)DebugOptions.Cell], flags[(int)DebugOptions.Cell]);
            if (flags[(int)DebugOptions.Cell]){
                flags[(int)DebugOptions.CellArea] = EditorGUILayout.Toggle(labels[(int)DebugOptions.CellArea], flags[(int)DebugOptions.CellArea]);
                flags[(int)DebugOptions.CellConnection] = EditorGUILayout.Toggle(labels[(int)DebugOptions.CellConnection], flags[(int)DebugOptions.CellConnection]);
                flags[(int)DebugOptions.CellEdges] = EditorGUILayout.Toggle(labels[(int)DebugOptions.CellEdges], flags[(int)DebugOptions.CellEdges]);
            }

            GUILayout.Box(dividerBoxLabel, dividerThing);

            //voxels
            flags[(int)DebugOptions.Voxels] = EditorGUILayout.Toggle(labels[(int)DebugOptions.Voxels], flags[(int)DebugOptions.Voxels]);
            if (flags[(int)DebugOptions.Voxels]) {
                flags[(int)DebugOptions.VoxelPos] = EditorGUILayout.Toggle(labels[(int)DebugOptions.VoxelPos], flags[(int)DebugOptions.VoxelPos]);         
                flags[(int)DebugOptions.VoxelConnection] = EditorGUILayout.Toggle(labels[(int)DebugOptions.VoxelConnection], flags[(int)DebugOptions.VoxelConnection]);
                flags[(int)DebugOptions.VoxelLayer] = EditorGUILayout.Toggle(labels[(int)DebugOptions.VoxelLayer], flags[(int)DebugOptions.VoxelLayer]);

                flags[(int)DebugOptions.VoxelRawMax] = EditorGUILayout.Toggle(labels[(int)DebugOptions.VoxelRawMax], flags[(int)DebugOptions.VoxelRawMax]);
                flags[(int)DebugOptions.VoxelRawMin] = EditorGUILayout.Toggle(labels[(int)DebugOptions.VoxelRawMin], flags[(int)DebugOptions.VoxelRawMin]);
                flags[(int)DebugOptions.VoxelRawVolume] = EditorGUILayout.Toggle(labels[(int)DebugOptions.VoxelRawVolume], flags[(int)DebugOptions.VoxelRawVolume]);
            }

            GUILayout.Box(dividerBoxLabel, dividerThing);
            flags[(int)DebugOptions.Cover] = EditorGUILayout.Toggle(labels[(int)DebugOptions.Cover], flags[(int)DebugOptions.Cover]);
            flags[(int)DebugOptions.Grid] = EditorGUILayout.Toggle(labels[(int)DebugOptions.Grid], flags[(int)DebugOptions.Grid]);
            flags[(int)DebugOptions.JumpBase] = EditorGUILayout.Toggle(labels[(int)DebugOptions.JumpBase], flags[(int)DebugOptions.JumpBase]);
            flags[(int)DebugOptions.ChunkBounds] = EditorGUILayout.Toggle(labels[(int)DebugOptions.ChunkBounds], flags[(int)DebugOptions.ChunkBounds]);
            flags[(int)DebugOptions.ColliderBounds] = EditorGUILayout.Toggle(labels[(int)DebugOptions.ColliderBounds], flags[(int)DebugOptions.ColliderBounds]);
            flags[(int)DebugOptions.NodesAndConnections] = EditorGUILayout.Toggle(labels[(int)DebugOptions.NodesAndConnections], flags[(int)DebugOptions.NodesAndConnections]);
            flags[(int)DebugOptions.NodesAndConnectionsPreRDP] = EditorGUILayout.Toggle(labels[(int)DebugOptions.NodesAndConnectionsPreRDP], flags[(int)DebugOptions.NodesAndConnectionsPreRDP]);
            flags[(int)DebugOptions.WalkablePolygons] = EditorGUILayout.Toggle(labels[(int)DebugOptions.WalkablePolygons], flags[(int)DebugOptions.WalkablePolygons]);
            flags[(int)DebugOptions.Triangulator] = EditorGUILayout.Toggle(labels[(int)DebugOptions.Triangulator], flags[(int)DebugOptions.Triangulator]);
            GUILayout.Box(dividerBoxLabel, dividerThing);            

            if (GUI.changed && settings.autoUpdateSceneView)
                UpdateSceneImportantThings();
        }

        public static void QueueUpdateSceneImportantThings() {
            commands.Enqueue(UpdateSceneImportantThings);
        }

        public static void UpdateSceneImportantThings() {
            if (settings == null | sceneObj == null)
                ForceInit();

            List<PointData> newPointData = new List<PointData>();
            List<LineData> newLineData = new List<LineData>();
            List<TriangleData> newTrisData = new List<TriangleData>();
            var flags = settings.debugFlags;

            lock (lockObj) {
                foreach (var info in debugData.Values) {
                    if (!info.showMe)
                        continue;


                    if (flags[(int)DebugOptions.Cell]) {
                        if (flags[(int)DebugOptions.CellArea])
                            newTrisData.AddRange(info.cellsArea);
                        if (flags[(int)DebugOptions.CellEdges])
                            newLineData.AddRange(info.cellEdges);
                        if (flags[(int)DebugOptions.CellConnection])
                            newLineData.AddRange(info.cellConnections);
                    }

                    if (flags[(int)DebugOptions.Voxels]) {
                        if (flags[(int)DebugOptions.VoxelPos])
                            newPointData.AddRange(info.voxelPos);     
                        if (flags[(int)DebugOptions.VoxelConnection])
                            newLineData.AddRange(info.voxelConnections);
                        if (flags[(int)DebugOptions.VoxelLayer])
                            newPointData.AddRange(info.voxelLayer);

                        if (flags[(int)DebugOptions.VoxelRawMax])
                            newPointData.AddRange(info.voxelRawMax);
                        if (flags[(int)DebugOptions.VoxelRawMin])
                            newPointData.AddRange(info.voxelRawMin);
                        if (flags[(int)DebugOptions.VoxelRawVolume])
                            newLineData.AddRange(info.voxelRawVolume);
                    }

                    if (flags[(int)DebugOptions.JumpBase]) {
                        newLineData.AddRange(info.jumpBasesLines);
                        newPointData.AddRange(info.jumpBasesDots);
                    }

                    if (flags[(int)DebugOptions.Cover]) {
                        newPointData.AddRange(info.coverDots);
                        newLineData.AddRange(info.coverLines);
                        newTrisData.AddRange(info.coverSheets);
                    }

                    if (flags[(int)DebugOptions.Grid])
                        newLineData.AddRange(info.grid);

                    if (flags[(int)DebugOptions.ChunkBounds])
                        newLineData.AddRange(info.chunkBounds);

                    if (flags[(int)DebugOptions.ColliderBounds])
                        newLineData.AddRange(info.colliderBounds);

                    if (flags[(int)DebugOptions.NodesAndConnections]) {
                        newLineData.AddRange(info.nodesLines);
                        newPointData.AddRange(info.nodesPoints);
                    }

                    if (flags[(int)DebugOptions.NodesAndConnectionsPreRDP]) {
                        newLineData.AddRange(info.nodesLinesPreRDP);
                        newPointData.AddRange(info.nodesPointsPreRDP);
                    }

                    if (flags[(int)DebugOptions.WalkablePolygons]) {
                        newLineData.AddRange(info.walkablePolygonLine);
                        newTrisData.AddRange(info.walkablePolygonSheet);
                    }

                    if (flags[(int)DebugOptions.Triangulator]) {
                        newLineData.AddRange(info.triangulator);
                    }                    
                }
            }
            
            sceneObj.UpdateImportantData(newPointData, newLineData, newTrisData);
        }
           
        public static void GenericGUI() {
            lock (lockObj) {
                bool tempBool;

                tempBool = settings.drawGenericLines;
                settings.drawGenericLines = EditorGUILayout.Toggle(new GUIContent("Lines " + genericLines.Count, genericDebugToolTip), settings.drawGenericLines);
                if (tempBool != settings.drawGenericLines)
                    needGenericLineUpdate = true;

                tempBool = settings.drawGenericDots;
                settings.drawGenericDots = EditorGUILayout.Toggle(new GUIContent("Dots " + genericDots.Count, genericDebugToolTip), settings.drawGenericDots);
                if (tempBool != settings.drawGenericDots)
                    needGenericDotUpdate = true;

                tempBool = settings.drawGenericMesh;
                settings.drawGenericMesh = EditorGUILayout.Toggle(new GUIContent("Meshes " + genericTris.Count, genericDebugToolTip), settings.drawGenericMesh);
                if (tempBool != settings.drawGenericMesh)
                    needGenericTrisUpdate = true;

                tempBool = settings.drawPaths;
                settings.drawPaths = EditorGUILayout.Toggle(new GUIContent("paths " + pathDebug.Count, "this will debug paths. object to change"), settings.drawPaths);
                if (tempBool != settings.drawPaths)
                    needPathUpdate = true; 

                //update on it's own
                settings.drawGenericLabels = EditorGUILayout.Toggle(new GUIContent("labels " + labelsDebug.Count, genericDebugToolTip), settings.drawGenericLabels);
            }
        }
        
        #region generic
        public static void AddLabel(Vector3 pos, string text, DebugGroup group = DebugGroup.label) {
            lock (labelsDebug) {
                labelsDebug.Add(new DebugLabel(pos, text));
            }
        }
        public static void AddLabel(Vector3 pos, double number, int digitsRound = 2, DebugGroup group = DebugGroup.label) {
            AddLabel(pos, Math.Round(number, digitsRound).ToString(), group);
        }
        public static void AddLabel(Vector3 pos, object obj, DebugGroup group = DebugGroup.label) {
            AddLabel(pos, obj.ToString(), group);
        }
        public static void AddLabelFormat(Vector3 pos, string format, params object[] data) {
            AddLabel(pos, string.Format(format, data));
        }

        //add things to lists
        private static void AddGenericDot(PointData data) {
            lock (genericDots)
                genericDots.Add(data);
            if(settings.drawGenericDots)
                needGenericDotUpdate = true;
        }
        private static void AddGenericDot(IEnumerable<PointData> datas) {
            lock (genericDots)
                genericDots.AddRange(datas);
            if (settings.drawGenericDots)
                needGenericDotUpdate = true;
        }
        private static void AddGenericLine(LineData data) {
            lock (genericLines)
                genericLines.Add(data);
            if (settings.drawGenericLines)
                needGenericLineUpdate = true;
        }
        private static void AddGenericLine(IEnumerable<LineData> datas) {
            lock (genericLines)
                genericLines.AddRange(datas);
            if (settings.drawGenericLines)
                needGenericLineUpdate = true;
        }
        private static void AddGenericLine(params LineData[] datas) {
            lock (genericLines)
                genericLines.AddRange(datas);
            if (settings.drawGenericLines)
                needGenericLineUpdate = true;
        }
        private static void AddGenericTriangle(TriangleData data) {
            lock (genericTris)
                genericTris.Add(data);
            if (settings.drawGenericMesh)
                needGenericTrisUpdate = true;

        }
        private static void AddGenericTriangle(IEnumerable<TriangleData> datas) {
            lock (genericTris)
                genericTris.AddRange(datas);
            if (settings.drawGenericMesh)
                needGenericTrisUpdate = true;
        }
        
        //dot
        public static void AddDot(Vector3 pos, Color color, float size = 0.02f) {
            AddGenericDot(new PointData(pos, color, size));
        }
        public static void AddDot(IEnumerable<Vector3> pos, Color color, float size = 0.02f) {
            List<PointData> pd = new List<PointData>();
            foreach (var item in pos) {
                pd.Add(new PointData(item, color, size));
            }
            AddGenericDot(pd);
        }
        public static void AddDot(Vector3 pos, float size = 0.02f) {
            AddGenericDot(new PointData(pos, Color.black, size));
        }

        public static void AddDot(float x, float y, float z, Color color, float size = 0.02f) {
            AddGenericDot(new PointData(new Vector3(x, y, z), color, size));
        }
        public static void AddDot(float x, float y, float z, float size = 0.02f) {
            AddGenericDot(new PointData(new Vector3(x, y, z), Color.black, size));
        }


        public static void AddDot(IEnumerable<Vector3> pos, float size = 0.02f) {
            List<PointData> pd = new List<PointData>();
            foreach (var item in pos) {
                pd.Add(new PointData(item, Color.black, size));
            }
            AddGenericDot(pd);
        }

        public static void AddDot(Color color, float size = 0.02f, params Vector3[] values) {
            AddDot(values, color, size);
        }
        public static void AddDot(float size = 0.02f, params Vector3[] values) {
            AddDot(values, Color.black, size);
        }
        public static void AddDot(Color color, params Vector3[] values) {
            AddDot(values, color, 0.02f);
        }
        public static void AddDot(params Vector3[] values) {
            AddDot(values, Color.black, 0.02f);
        }
        
        //line
        public static void AddLine(Vector3 v1, Vector3 v2, Color color, float addOnTop = 0f, float width = 0.001f) {
            AddGenericLine(new LineData(v1 + V3small(addOnTop), v2 + V3small(addOnTop), color, width));
        }

        public static void AddLine(float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, float addOnTop = 0f, float width = 0.001f) {
            AddLine(new Vector3(v1x, v1y, v1z), new Vector3(v2x, v2y, v2z), Color.black, addOnTop, width);
        }

        public static void AddLine(Vector3 v1, Vector3 v2, float addOnTop = 0f, float width = 0.001f) {
            AddLine(v1, v2, Color.black, addOnTop, width);
        }
        public static void AddLine(CellContentData data, Color color, float addOnTop = 0f, float width = 0.001f) {
            AddLine(data.leftV3 + V3small(addOnTop), data.rightV3 + V3small(addOnTop), color, width);
        }
        public static void AddLine(CellContentData data, float addOnTop = 0f, float width = 0.001f) {
            AddLine(data.leftV3 + V3small(addOnTop), data.rightV3 + V3small(addOnTop), Color.black, width);
        }
        public static void AddLine(Vector3[] chain, Color color, bool chainClosed = false, float addOnTop = 0f, float width = 0.001f) {
            int length = chain.Length;
            if (length < 2)
                return;
            if (chainClosed) {
                LineData[] ld = new LineData[length];
                for (int i = 0; i < length - 1; i++) {
                    ld[i] = new LineData(chain[i] + V3small(addOnTop), chain[i + 1] + V3small(addOnTop), color, width);
                }
                ld[length - 1] = new LineData(chain[length - 1] + V3small(addOnTop), chain[0] + V3small(addOnTop), color, width);
                AddGenericLine(ld);
            }
            else {
                LineData[] ld = new LineData[length - 1];
                for (int i = 0; i < length - 1; i++) {
                    ld[i] = new LineData(chain[i] + V3small(addOnTop), chain[i + 1] + V3small(addOnTop), color, width);
                }
                AddGenericLine(ld);
            }

        }
        public static void AddLine(List<Vector3> chain, bool chainClosed = false, float addOnTop = 0f, float width = 0.001f) {
            AddLine(chain, Color.black, chainClosed, addOnTop, width);
        }



        public static void AddLine(List<Vector3> chain, Color color, bool chainClosed = false, float addOnTop = 0f, float width = 0.001f) {
            int length = chain.Count;
            if (length < 2)
                return;
            if (chainClosed) {
                LineData[] ld = new LineData[length];
                for (int i = 0; i < length - 1; i++) {
                    ld[i] = new LineData(chain[i] + V3small(addOnTop), chain[i + 1] + V3small(addOnTop), color, width);
                }
                ld[length - 1] = new LineData(chain[length - 1] + V3small(addOnTop), chain[0] + V3small(addOnTop), color, width);
                AddGenericLine(ld);
            }
            else {
                LineData[] ld = new LineData[length - 1];
                for (int i = 0; i < length - 1; i++) {
                    ld[i] = new LineData(chain[i] + V3small(addOnTop), chain[i + 1] + V3small(addOnTop), color, width);
                }
                AddGenericLine(ld);
            }
        }

        public static void AddLine(List<Vector2> chain, Color color, bool chainClosed = false, float width = 0.001f) {
            int length = chain.Count;
            if (length < 2)
                return;
            if (chainClosed) {
                LineData[] ld = new LineData[length];
                for (int i = 0; i < length - 1; i++) {
                    ld[i] = new LineData(chain[i], chain[i + 1], color, width);
                }
                ld[length - 1] = new LineData(chain[length - 1], chain[0], color, width);
                AddGenericLine(ld);
            }
            else {
                LineData[] ld = new LineData[length - 1];
                for (int i = 0; i < length - 1; i++) {
                    ld[i] = new LineData(chain[i], chain[i + 1], color, width);
                }
                AddGenericLine(ld);
            }
        }

        public static void AddLine(Vector3[] chain, bool chainClosed = false, float addOnTop = 0f, float width = 0.001f) {
            AddLine(chain, Color.black, chainClosed, addOnTop, width);
        }



        public static void AddLine(float addOnTop = 0f, float width = 0.001f, bool chainClosed = false, params Vector3[] chain) {
            AddLine(chain, Color.black, chainClosed, addOnTop, width);
        }
        public static void AddLine(Color color, float addOnTop = 0f, float width = 0.001f, bool chainClosed = false, params Vector3[] chain) {
            AddLine(chain, color, chainClosed, addOnTop, width);
        }
        //some fancy expensive stuff when no colors left
        public static void AddLine(Vector3 v1, Vector3 v2, Color color1, Color color2, int subdivisions, float addOnTop = 0f, float width = 0.001f) {
            List<LineData> ld = new List<LineData>();
            float step = 1f / subdivisions;
            bool flip = false;
            for (int i = 0; i < subdivisions; i++) {           
                ld.Add(new LineData(Vector3.Lerp(v1, v2, Mathf.Clamp01(step * i)) + V3small(addOnTop), Vector3.Lerp(v1, v2, Mathf.Clamp01(step * (i + 1))) + V3small(addOnTop), flip ? color1 : color2, width));
                flip = !flip;
            }
            AddGenericLine(ld);
        }
        public static void AddLine(Vector3 v1, Vector3 v2, Color color1, Color color2, float subdivisionLength, float addOnTop = 0f, float width = 0.001f) {
            AddLine(v1, v2, color1, color2, Mathf.FloorToInt(Vector3.Distance(v1, v2) / subdivisionLength), addOnTop, width);
        }
        public static void AddLine(Vector3 v1, Vector3 v2, Color color1, Color color2, float addOnTop = 0f, float width = 0.001f) {
            Vector3 mid = SomeMath.MidPoint(v1, v2);
            AddLine(v1, mid, color1, addOnTop, width);
            AddLine(mid, v2, color2, addOnTop, width);
        }
        public static void AddCross(Vector3 v, Color color, float size, float lineWidth = 0.001f) {
            AddGenericLine(
                new LineData(new Vector3(v.x - size, v.y, v.z), new Vector3(v.x + size, v.y, v.z), color, lineWidth),
                new LineData(new Vector3(v.x, v.y - size, v.z), new Vector3(v.x, v.y + size, v.z), color, lineWidth), 
                new LineData(new Vector3(v.x, v.y, v.z - size), new Vector3(v.x, v.y, v.z + size), color, lineWidth));
        }
        public static void AddRay(Vector3 point, Vector3 direction, Color color, float length = 1f, float width = 0.001f) {
            AddLine(point, point + (direction.normalized * length), color, width);
        }
        public static void AddRay(Vector3 point, Vector3 direction, float length = 1f, float width = 0.001f) {
            AddRay(point, direction, Color.black, width);
        }

        public static void AddBounds(Bounds b, Color color, float width = 0.001f) {
            AddGenericLine(BuildParallelepiped(b.center - b.extents, b.center + b.extents, color, width));
        }
        public static void AddBounds(Bounds b, float width = 0.001f) {
            AddBounds(b, Color.blue, width);
        }

        public static void AddBounds(Bounds2D b, Color color, float height = 0f, float width = 0.001f) {
            Vector3 A1 = new Vector3(b.minX, height, b.minY);
            Vector3 A2 = new Vector3(b.minX, height, b.maxY);
            Vector3 A3 = new Vector3(b.maxX, height, b.minY);
            Vector3 A4 = new Vector3(b.maxX, height, b.maxY);

            AddLine(A1, A2, color);
            AddLine(A1, A3, color);
            AddLine(A2, A4, color);
            AddLine(A3, A4, color);
        }
        public static void AddBounds(Bounds2D b, float height = 0f, float width = 0.001f) {
            AddBounds(b, Color.blue, height, width);
        }

        //path
        public static void AddPath(Vector3 v1, Vector3 v2, Color color, float addOnTop = 0f, float width = 0.001f) {
            lock (pathDebug)
                pathDebug.Add(new LineData(v1 + V3small(addOnTop), v2 + V3small(addOnTop), color, width));
            needPathUpdate = true;     
        }

        //geometry
        public static void AddTriangle(Vector3 A, Vector3 B, Vector3 C, Color color, bool outline = true, float outlineWidth = 0.001f) {
            AddGenericTriangle(new TriangleData(A, B, C, color));
            if (outline) {
                Color oColor = new Color(color.r, color.g, color.b, 1f);
                AddGenericLine(new LineData[]{
                    new LineData(A, B, oColor, outlineWidth),
                    new LineData(B, C, oColor, outlineWidth),
                    new LineData(C, A, oColor, outlineWidth)
                });
            }
        }
        public static void AddQuad(Vector3 bottomLeft, Vector3 upperLeft, Vector3 bottomRight, Vector3 upperRight, Color color, bool outline = true, float outlineWidth = 0.001f) {
            AddGenericTriangle(new TriangleData(bottomLeft, upperLeft, bottomRight, color));
            AddGenericTriangle(new TriangleData(upperLeft, bottomRight, upperRight, color));
            if (outline) {
                Color oColor = new Color(color.r, color.g, color.b, 1f);
                AddGenericLine(new LineData[]{
                    new LineData(bottomLeft, upperLeft, oColor, outlineWidth),
                    new LineData(upperLeft, upperRight, oColor, outlineWidth),
                    new LineData(upperRight, bottomRight, oColor, outlineWidth),
                    new LineData(bottomRight, bottomLeft, oColor, outlineWidth)
                });
            }
        }
        public static void AddMesh(Vector3[] verts, int[] tris, Color color, bool outline = true, float outlineWidth = 0.001f) {
            TriangleData[] td = new TriangleData[tris.Length / 3];
            for (int i = 0; i < tris.Length; i += 3) {
                td[i / 3] = new TriangleData(verts[tris[i]], verts[tris[i + 1]], verts[tris[i + 2]], color);
            }
            AddGenericTriangle(td);

            if (outline) {
                Color oColor = new Color(color.r, color.g, color.b, 1f);
                LineData[] ld = new LineData[tris.Length];

                for (int i = 0; i < tris.Length; i += 3) {
                    ld[i] = new LineData(verts[tris[i]], verts[tris[i + 1]], oColor, outlineWidth);
                    ld[i + 1] = new LineData(verts[tris[i + 1]], verts[tris[i + 2]], oColor, outlineWidth);
                    ld[i + 2] = new LineData(verts[tris[i + 2]], verts[tris[i]], oColor, outlineWidth);
                }
                AddGenericLine(ld);
            }
        }

        //public static void AddWireMesh(Vector3[] verts, int[] tris, DebugGroup group = DebugGroup.line) {
        //    AddGeneric(group, BuildWireMesh(verts, tris, Color.white));
        //}

        //public static void AddWireMesh(Vector3[] verts, int[] tris, Color color, DebugGroup group = DebugGroup.line) {
        //    AddGeneric(group, BuildWireMesh(verts, tris, color));
        //}

        //private static List<HandleThing> GenerateCapsule(Vector3 bottom, Vector3 top, float radius, float dotSize, Color color) {
        //    List<HandleThing> result = new List<HandleThing>();

        //    result.Add(new DebugDotColored(bottom, dotSize, color));
        //    result.Add(new DebugDotColored(top, dotSize, color));
        //    result.Add(new DebugLineAAColored(bottom, top, color));

        //    Vector3 normal = (top - bottom).normalized;
        //    result.Add(new DebugWireDisc(top, normal, radius, color));
        //    result.Add(new DebugWireDisc(bottom, normal, radius, color));

        //    Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.LookRotation(normal, Vector3.up), Vector3.one);

        //    result.Add(new DebugLineAAColored(matrix.MultiplyPoint3x4(new Vector3(radius, 0, 0)) + bottom, matrix.MultiplyPoint3x4(new Vector3(radius, 0, 0)) + top, color));
        //    result.Add(new DebugLineAAColored(matrix.MultiplyPoint3x4(new Vector3(-radius, 0, 0)) + bottom, matrix.MultiplyPoint3x4(new Vector3(-radius, 0, 0)) + top, color));

        //    result.Add(new DebugLineAAColored(matrix.MultiplyPoint3x4(new Vector3(0, radius, 0)) + bottom, matrix.MultiplyPoint3x4(new Vector3(0, radius, 0)) + top, color));
        //    result.Add(new DebugLineAAColored(matrix.MultiplyPoint3x4(new Vector3(0, -radius, 0)) + bottom, matrix.MultiplyPoint3x4(new Vector3(0, -radius, 0)) + top, color));

        //    result.Add(new DebugWireArc(top, matrix.MultiplyPoint3x4(new Vector3(radius, 0, 0)), matrix.MultiplyPoint3x4(new Vector3(0, radius, 0)), 180, radius, color));
        //    result.Add(new DebugWireArc(top, matrix.MultiplyPoint3x4(new Vector3(0, radius, 0)), matrix.MultiplyPoint3x4(new Vector3(-radius, 0, 0)), 180, radius, color));

        //    result.Add(new DebugWireArc(bottom, matrix.MultiplyPoint3x4(new Vector3(radius, 0, 0)), matrix.MultiplyPoint3x4(new Vector3(0, -radius, 0)), 180, radius, color));
        //    result.Add(new DebugWireArc(bottom, matrix.MultiplyPoint3x4(new Vector3(0, radius, 0)), matrix.MultiplyPoint3x4(new Vector3(radius, 0, 0)), 180, radius, color));
        //    return result;
        //}

        //public static void AddCapsule(Vector3 bottom, Vector3 top, float radius, Color color, float dotSize = 0.02f, DebugGroup group = DebugGroup.generic) {
        //    AddGeneric(group, GenerateCapsule(bottom, top, radius, dotSize, color));
        //}

        //public static void AddPolyLine(Vector3[] value, Color color, DebugGroup group = DebugGroup.line) {
        //    AddGeneric(group, new DebugPolyLine(color, false, value));
        //}




        //some clears
        public static void ClearGeneric(DebugGroup group) {
            if (_stop)
                return;
            switch (group) {
                case DebugGroup.line:
                    lock (genericLines) {
                        genericLines.Clear();
                    }
                    needGenericLineUpdate = true;
                    break;
                case DebugGroup.dot:
                    lock (genericDots) {
                        genericDots.Clear();
                    }
                    needGenericDotUpdate = true;
                    break;
                case DebugGroup.label: //labels are dont need update
                    lock (labelsDebug) {
                        labelsDebug.Clear();
                    }
                    break;
                case DebugGroup.mesh:
                    lock (genericTris) {
                        genericTris.Clear();
                    }
                    needGenericTrisUpdate = true;
                    break;
                case DebugGroup.path:
                    lock (pathDebug) {
                        pathDebug.Clear();
                    }
                    needPathUpdate = true;
                    break;       
            }
        }

        public static void ClearGeneric() {
            lock (pathDebug) {
                if(pathDebug.Count != 0) {
                    pathDebug.Clear();
                    needPathUpdate = true;
                }
            }
            lock (genericLines) {
                if (genericLines.Count != 0) {
                    genericLines.Clear();
                    needGenericLineUpdate = true;
                }
            }
            lock (genericDots) {
                if (genericDots.Count != 0) {
                    genericDots.Clear();
                    needGenericDotUpdate = true;
                }
            }
            lock (genericTris) {
                if (genericTris.Count != 0) {
                    genericTris.Clear();
                    needGenericTrisUpdate = true;
                }
            }

            lock (labelsDebug) {
                if(labelsDebug.Count != 0)
                    labelsDebug.Clear();
            }
        }     
                
        public static void ClearLabels() {
            ClearGeneric(DebugGroup.label);           
        }
        public static void ClearLines() {
            ClearGeneric(DebugGroup.line);          
        }
        public static void ClearDots() {
            ClearGeneric(DebugGroup.dot);  
        }
        public static void ClearMeshes() {
            ClearGeneric(DebugGroup.mesh);
        }
        public static void ClearPath() {
            ClearGeneric(DebugGroup.path);
        }

        //error shortcuts //currently generic
        public static void AddErrorDot(Vector3 pos, Color color, float size = 0.1f) {
            AddDot(pos, color, size);
        }
        public static void AddErrorDot(Vector3 pos, float size = 0.1f) {
            AddErrorDot(pos, Color.red, 0.1f);
        }

        public static void AddErrorLine(Vector3 v1, Vector3 v2, Color color, float add = 0f) {
            AddLine(v1, v2, color, add);
        }
        public static void AddErrorLine(Vector3 v1, Vector3 v2, float add = 0f) {
            AddErrorLine(v1, v2, Color.red, add);
        }

        public static void AddErrorLabel(Vector3 pos, string text) {
            AddLabel(pos, text);
        }
        public static void AddErrorLabel(Vector3 pos, object text) {
            AddErrorLabel(pos, text.ToString());
        }
        #endregion

        #region add important
        //important
        private static ChunkDebugInfo GetInfo(GeneralXZData key) {
            lock (lockObj) {
                ChunkDebugInfo info;
                if (debugData.TryGetValue(key, out info) == false) {
                    info = new ChunkDebugInfo();
                    debugData.Add(key, info);
                    //Bounds bounds = chunk.bounds;
                    //info.chunkBounds.AddRange(BuildParallelepiped(bounds.center - bounds.size, bounds.center + bounds.size, Color.gray, 0.001f));
                }
                return info;
            }
        }

        private static ChunkDebugInfo GetInfo(int x, int z, AgentProperties properties) {
            return GetInfo(new GeneralXZData(x, z, properties));
        }

        public static void AddCells(int x, int z, AgentProperties properties, IEnumerable<Cell> cells) {
            Vector3 offsetLD = new Vector3(-0.015f, 0f, -0.015f);
            Vector3 offsetRT = new Vector3(0.015f, 0f, 0.015f);

            List<TriangleData> cellsAreaNewData = new List<TriangleData>();
            List<LineData> cellEdgesNewData = new List<LineData>();
            List<LineData> cellConnectionsNewData = new List<LineData>();

            foreach (var cell in cells) {
                Color areaColor = cell.area.color;

                if (cell.passability == Passability.Crouchable)
                    areaColor *= 0.2f;

                areaColor = new Color(areaColor.r, areaColor.g, areaColor.b, 0.1f);

                foreach (var oe in cell.originalEdges) {
                    cellEdgesNewData.Add(new LineData(oe.a, oe.b, Color.black, 0.001f));
                    cellsAreaNewData.Add(new TriangleData(oe.a, oe.b, cell.centerVector3, areaColor));
                }

                lock (cell) {
                    foreach (var cutContent in cell.connections) {
                        if (cutContent is CellContentGenericConnection) {
                            var val = cutContent as CellContentGenericConnection;
                            cellConnectionsNewData.Add(new LineData(cell.centerVector3, val.intersection, Color.white, 0.0008f));
                        }

                        if (cutContent is CellContentPointedConnection) {
                            var val = cutContent as CellContentPointedConnection;

                            Color color;
                            if (val.jumpState == ConnectionJumpState.jumpUp) {
                                color = Color.yellow;
                                cellConnectionsNewData.Add(new LineData(val.enterPoint + offsetLD, val.lowerStandingPoint + offsetLD, color, 0.001f));
                                cellConnectionsNewData.Add(new LineData(val.lowerStandingPoint + offsetLD, val.axis + offsetLD, color, 0.001f));
                                cellConnectionsNewData.Add(new LineData(val.axis + offsetLD, val.exitPoint + offsetLD, color, 0.001f));
                            }
                            else {
                                color = Color.blue;
                                cellConnectionsNewData.Add(new LineData(val.enterPoint + offsetRT, val.axis + offsetRT, color, 0.001f));
                                cellConnectionsNewData.Add(new LineData(val.axis + offsetRT, val.lowerStandingPoint + offsetRT, color, 0.001f));
                                cellConnectionsNewData.Add(new LineData(val.lowerStandingPoint + offsetRT, val.exitPoint + offsetRT, color, 0.001f));
                            }
                        }
                    }
                }
            }

            ChunkDebugInfo info = GetInfo(x, z, properties);

            lock (lockObj) {
                cellCounter += cells.Count();
                info.cellCounter = cells.Count();
                info.cellsArea.AddRange(cellsAreaNewData);
                info.cellEdges.AddRange(cellEdgesNewData);
                info.cellConnections.AddRange(cellConnectionsNewData);
            }

        }

        public static void AddEdgesInterconnected(int x, int z, AgentProperties properties, CellContentGenericConnection connection) {
            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.cellConnections.Add(new LineData(connection.from.centerVector3, connection.intersection, Color.white, 0.0008f));
            }
        }

        public static void AddVolumes(NavMeshTemplateCreation template, VolumeContainerNew volumeContainer) {
            float fragmentSize = 0.02f;

            bool doCover = template.doCover;
            int sizeX = volumeContainer.sizeX;
            int sizeZ = volumeContainer.sizeZ;

            //////////////
            List<PointData> voxelPosNewData = new List<PointData>();
            List<LineData> voxelConnectionsNewData = new List<LineData>();
            List<PointData> voxelLayerNewData = new List<PointData>();

            List<PointData> voxelRawMaxNewData = new List<PointData>();
            List<PointData> voxelRawMinNewData = new List<PointData>();
            List<LineData> voxelRawVolumeNewData = new List<LineData>();
            /////////////
            
            var collums = volumeContainer.collums;
            var data = volumeContainer.data;

            var hashData = template.hashData;

            //for raw data
            var rawShapeData = volumeContainer.shape;
            var rawData = rawShapeData.arrayData;

            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    int index = volumeContainer.GetIndex(x, z);
                    var currentColum = collums[index];

                    for (int collumIndex = 0; collumIndex < currentColum.count; collumIndex++) {
                        var value = data[currentColum.index + collumIndex];

                        Vector3 pos = volumeContainer.GetPos(x, z, value.y);
                        Color color;

                        switch ((Passability)value.pass) {
                            case Passability.Unwalkable:
                                color = Color.red;
                                break;
                            case Passability.Slope:
                                color = Color.magenta;
                                break;
                            case Passability.Crouchable:
                                color = SetAlpha(hashData.areaByIndex[value.area].color * 0.2f, 1f);
                                break;
                            case Passability.Walkable:
                                color = hashData.areaByIndex[value.area].color;
                                break;
                            default:
                                color = new Color();
                                break;
                        }
                        
                        voxelPosNewData.Add(new PointData(pos, color, fragmentSize));
                        voxelLayerNewData.Add(new PointData(pos, IntegerToColor(value.layer), fragmentSize * 0.75f));
             
                        if (value.xPlus != -1) {
                            var connection = data[collums[volumeContainer.GetIndex(x + 1, z)].index + value.xPlus];
                            Vector3 p2 = volumeContainer.GetPos(x + 1, z, connection.y);            
                            voxelConnectionsNewData.Add(new LineData(pos, p2, GetSomeColor(0), 0.001f));
                        }

                        if (value.xMinus != -1) {
                            var connection = data[collums[volumeContainer.GetIndex(x - 1, z)].index + value.xMinus];
                            Vector3 p2 = volumeContainer.GetPos(x - 1, z, connection.y);
                            voxelConnectionsNewData.Add(new LineData(new Vector3(pos.x, pos.y + (0.01f), pos.z), new Vector3(p2.x, p2.y + (0.01f), p2.z), GetSomeColor(1), 0.001f));
                        }

                        if (value.zPlus != -1) {
                            var connection = data[collums[((z + 1) * sizeX) + x].index + value.zPlus];
                            Vector3 p2 = volumeContainer.GetPos(x, z + 1, connection.y);
                            voxelConnectionsNewData.Add(new LineData(new Vector3(pos.x, pos.y + (0.01f * 2), pos.z), new Vector3(p2.x, p2.y + (0.01f * 2), p2.z), GetSomeColor(2), 0.001f));
                        }

                        if (value.zMinus != -1) {
                            var connection = data[collums[volumeContainer.GetIndex(x, z - 1)].index + value.zMinus];
                            Vector3 p2 = volumeContainer.GetPos(x, z - 1, connection.y);
                            voxelConnectionsNewData.Add(new LineData(new Vector3(pos.x, pos.y + (0.01f * 3), pos.z), new Vector3(p2.x, p2.y + (0.01f * 3), p2.z), GetSomeColor(3), 0.001f));
                        }
                    }

                    if (rawData[index].next != -2) {
                        for (; index != -1; index = rawData[index].next) {
                            var arrData = rawData[index];

                            Vector3 posMax = volumeContainer.GetPos(x, z, arrData.max);
                            Vector3 posMin = volumeContainer.GetPos(x, z, arrData.min);
          
                            Color color;

                            switch ((Passability)arrData.pass) {
                                case Passability.Unwalkable:
                                    color = Color.red;
                                    break;
                                case Passability.Slope:
                                    color = Color.magenta;
                                    break;
                                case Passability.Walkable:
                                    color = hashData.areaByIndex[arrData.area].color;
                                    break;
                                case Passability.Crouchable:
                                    color = hashData.areaByIndex[arrData.area].color * 0.2f;
                                    break;
                                default:
                                    color = Color.white;
                                    break;
                            }

                            voxelRawMaxNewData.Add(new PointData(posMax, color, fragmentSize));
                            voxelRawMinNewData.Add(new PointData(posMin, Color.black, fragmentSize));
                            voxelRawVolumeNewData.Add(new LineData(posMax, posMin, Color.gray, 0.001f));
                        }
                    }
                }
            }
                        
            ChunkDebugInfo info = GetInfo(new GeneralXZData(template.gridPosition.x, template.gridPosition.z, template.properties));    

            lock (lockObj) {
                voxelsCounter += voxelPosNewData.Count;
                info.voxelsCounter = voxelPosNewData.Count;
                info.voxelPos.AddRange(voxelPosNewData);
                info.voxelConnections.AddRange(voxelConnectionsNewData);
                info.voxelLayer.AddRange(voxelLayerNewData);                

                info.voxelRawMax.AddRange(voxelRawMaxNewData);
                info.voxelRawMin.AddRange(voxelRawMinNewData);
                info.voxelRawVolume.AddRange(voxelRawVolumeNewData);
            }
        }
        

        public static Color IntegerToColor(int i) {
            return new Color(
                (byte)((i) & 0xFF),
                (byte)((i >> 8) & 0xFF),
                (byte)((i >> 24) & 0xFF));
        }

        public static void AddBattleGrid(int x, int z, AgentProperties properties, BattleGrid bg) {
            List<LineData> gridNewData = new List<LineData>();

            foreach (var p in bg.points) {
                foreach (var n in p.neighbours) {
                    if (n != null)
                        gridNewData.Add(new LineData(p.positionV3, n.positionV3, Color.yellow, 0.001f) );       
                }
            }
            ChunkDebugInfo info = GetInfo(x, z, properties);

            lock (lockObj) {
                info.grid.AddRange(gridNewData);
            }
        }
        public static void AddCovers(int x, int z, AgentProperties properties, IEnumerable<Cover> covers) {
            List<PointData> coverDotsNewData = new List<PointData>();
            List<LineData> coverLinesNewData = new List<LineData>();
            List<TriangleData> coverSheetsNewData = new List<TriangleData>();

            Color hardColor = Color.magenta;
            Color softColor = new Color(hardColor.r, hardColor.g, hardColor.b, 0.2f);

            float slickLine = 0.0008f;
            float fatLine = 0.0015f;
            float dotSize = 0.04f;

            foreach (var cover in covers) {
                if (cover.coverPoints.Count == 0)
                    continue;

                //bootom
                Vector3 BL = cover.right;
                Vector3 BR = cover.left;

                float height = 0;
                switch (cover.coverType) {
                    case 1:
                        height = properties.halfCover;
                        break;
                    case 2:
                        height = properties.fullCover;
                        break;
                    default:
                        break;
                }

                //top
                Vector3 TL = BL + (Vector3.up * height);
                Vector3 TR = BR + (Vector3.up * height);

                //top and bottom
                coverLinesNewData.Add(new LineData(BL, BR, hardColor, fatLine));
                coverLinesNewData.Add(new LineData(TL, TR, hardColor, fatLine));

                //sides
                coverLinesNewData.Add(new LineData(BL, TL, hardColor, slickLine));
                coverLinesNewData.Add(new LineData(BR, TR, hardColor, slickLine));

                coverSheetsNewData.Add(new TriangleData(BL, BR, TR, softColor));
                coverSheetsNewData.Add(new TriangleData(BL, TL, TR, softColor));          

                foreach (var point in cover.coverPoints) {
                    coverDotsNewData.Add(new PointData(point.positionV3, hardColor, dotSize));
                    coverDotsNewData.Add(new PointData(point.cellPos, hardColor, dotSize));

                    coverLinesNewData.Add(new LineData(point.positionV3, point.cellPos, hardColor, slickLine));
                    coverLinesNewData.Add(new LineData(TL, TR, hardColor, slickLine));
                }
            }

            ChunkDebugInfo info = GetInfo(x, z, properties);

            lock (lockObj) {
                coversCounter += covers.Count();
                info.coversCounter = covers.Count();
                info.coverDots.AddRange(coverDotsNewData);
                info.coverLines.AddRange(coverLinesNewData);
                info.coverSheets.AddRange(coverSheetsNewData);
            }
        }
        public static void AddPortalBases(int x, int z, AgentProperties properties, IEnumerable<JumpPortalBase> portalBases) {           
            List<PointData> jumpBasesDotsNewData = new List<PointData>();
            List<LineData> jumpBasesLinesNewData = new List<LineData>();

            foreach (var portalBase in portalBases) {
                foreach (var cellPoint in portalBase.cellMountPoints.Values) {
                    jumpBasesLinesNewData.Add(new LineData(portalBase.positionV3, cellPoint, Color.black, 0.001f));
                    jumpBasesDotsNewData.Add(new PointData(portalBase.positionV3,  Color.black, 0.04f));
                    jumpBasesDotsNewData.Add(new PointData(cellPoint, Color.black, 0.04f));
                }
            }


            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                jumpBasesCounter += portalBases.Count();
                info.jumpBasesCounter = portalBases.Count();
                info.jumpBasesDots.AddRange(jumpBasesDotsNewData);
                info.jumpBasesLines.AddRange(jumpBasesLinesNewData);
            }
        }

        //less important
        private static void GetNodesThings(AgentProperties properties, IEnumerable<NodeTemp> nodes, out List<PointData> nodesPos, out List<LineData> nodesConnectins, out List<HandleThing> nodesLabels) {
            nodesPos = new List<PointData>();
            nodesConnectins = new List<LineData>();
            nodesLabels = new List<HandleThing>();



            foreach (var node in nodes) {
                nodesPos.Add(new PointData(node.positionV3, Color.blue, 0.01f));
                string s = "";
                
                foreach (var item in node.getData) {
                    NodeTemp connection = item.Value.connection;
                    int layer = item.Key.x;
                    int hash = item.Key.y;
                    s += string.Format("L{0}:H{1} ", layer, hash);

                    if (connection == null) {
                        Debug.LogError("NULL");
                        AddErrorLine(node.positionV3, node.positionV3, Color.red);
                        AddErrorLabel(node.positionV3, "NULL" + layer + " : " + hash);
                    }
                    else {
                        Vector3 conPos = connection.positionV3;
                        Vector3 midPoint = SomeMath.MidPoint(node.positionV3, conPos);
                        var edge = item.Value;

                        nodesConnectins.Add(
                            new LineData(node.positionV3, midPoint,
                            edge.GetFlag(EdgeTempFlags.DouglasPeukerMarker) ? Color.green : Color.blue, 0.001f));

                        nodesConnectins.Add(new LineData(midPoint, conPos, Color.red, 0.001f));
                    }
                }

                nodesLabels.Add(new DebugLabel(node.positionV3, s));
            }

            //Debug.Log(nodesLabels.Count);
        }
        public static void AddNodesTemp(int x, int z, AgentProperties properties, IEnumerable<NodeTemp> nodes) {
            List<HandleThing> nodesLabels;
            List<PointData> nodesPos;
            List<LineData> nodesConnectins;
            GetNodesThings(properties, nodes, out nodesPos, out nodesConnectins, out nodesLabels);

            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.nodesPoints.AddRange(nodesPos);
                info.nodesLines.AddRange(nodesConnectins);
            }


            //Debug.Log(nodesLabels.Count);
            //foreach (var item in nodesLabels) {
            //    AddLabel((item as DebugLabel).pos, (item as DebugLabel).text);

            //    //Debug.Log((item as DebugLabel).pos + " : " + (item as DebugLabel).text);
            //}
            //lock (labelsDebug) {
            //    labelsDebug.AddRange(labelsDebug);
            //}
        }
        public static void AddNodesTempPreRDP(int x, int z, AgentProperties properties, IEnumerable<NodeTemp> nodes) {
            List<HandleThing> nodesLabels;
            List<PointData> nodesPos;
            List<LineData> nodesConnectins;
            GetNodesThings(properties, nodes, out nodesPos, out nodesConnectins, out nodesLabels);

            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.nodesPointsPreRDP.AddRange(nodesPos);
                info.nodesLinesPreRDP.AddRange(nodesConnectins);
            }
        }

        //not important
        public static void AddColliderBounds(int x, int z, AgentProperties properties, Collider collider) {
            Bounds bounds = collider.bounds;
            var debugedBounds = BuildParallelepiped(bounds.center - bounds.extents, bounds.center + bounds.extents, Color.green, 0.001f);

            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.colliderBounds.AddRange(debugedBounds);
            }
        }
        public static void AddTreeCollider(int x, int z, AgentProperties properties, Bounds bounds, Vector3[] verts, int[] tris) {
            //AddHandle(chunk, properties, PFDOptionEnum.BoundsCollider, new DebugBounds(bounds));

            //List<HandleThing> mesh = new List<HandleThing>();
            //for (int i = 0; i < tris.Length; i += 3) {
            //    mesh.Add(new DebugLine(verts[tris[i]], verts[tris[i + 1]]));
            //    mesh.Add(new DebugLine(verts[tris[i]], verts[tris[i + 2]]));
            //    mesh.Add(new DebugLine(verts[tris[i + 1]], verts[tris[i + 2]]));
            //}

            //AddHandle(chunk, properties, PFDOptionEnum.TreeWireMesh, mesh);
      
            var debugedBounds = BuildParallelepiped(bounds.center - bounds.size, bounds.center + bounds.size, Color.green, 0.001f);

            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.colliderBounds.AddRange(debugedBounds);
            }
        }
        public static void AddWalkablePolygon(int x, int z, AgentProperties properties, Vector3 a, Vector3 b, Vector3 c) {
            List<LineData> walkablePolygonLineNewData = new List<LineData>();
            List<TriangleData> walkablePolygonSheetNewData = new List<TriangleData>();

            Color solidColor = Color.cyan;
            Color lightColor = new Color(solidColor.r, solidColor.g, solidColor.b, 0.2f);

            walkablePolygonLineNewData.Add(new LineData(a, b, solidColor, 0.001f));
            walkablePolygonLineNewData.Add(new LineData(b, c, solidColor, 0.001f));
            walkablePolygonLineNewData.Add(new LineData(c, a, solidColor, 0.001f));
            walkablePolygonSheetNewData.Add(new TriangleData(a, b, c, lightColor));

            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.walkablePolygonLine.AddRange(walkablePolygonLineNewData);
                info.walkablePolygonSheet.AddRange(walkablePolygonSheetNewData);
            }            
        }

        //triangulator important 
        public static void AddTriangulatorDebugLine(int x, int z, AgentProperties properties, Vector3 v1, Vector3 v2, Color color, float width = 0.001f) {
            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.triangulator.Add(new LineData(v1, v2, color, width));
            }
        }
        #endregion

        #region other
        private static List<LineData> BuildParallelepiped(Vector3 A, Vector3 B, Color color, float width) {
            List<LineData> result = new List<LineData>();
            result.Add(new LineData(new Vector3(A.x, A.y, A.z), new Vector3(A.x, A.y, B.z), color, width));
            result.Add(new LineData(new Vector3(A.x, A.y, B.z), new Vector3(B.x, A.y, B.z), color, width));
            result.Add(new LineData(new Vector3(B.x, A.y, B.z), new Vector3(B.x, A.y, A.z), color, width));
            result.Add(new LineData(new Vector3(B.x, A.y, A.z), new Vector3(A.x, A.y, A.z), color, width));

            result.Add(new LineData(new Vector3(A.x, A.y, A.z), new Vector3(A.x, B.y, A.z), color, width));
            result.Add(new LineData(new Vector3(A.x, A.y, B.z), new Vector3(A.x, B.y, B.z), color, width));
            result.Add(new LineData(new Vector3(B.x, A.y, B.z), new Vector3(B.x, B.y, B.z), color, width));
            result.Add(new LineData(new Vector3(B.x, A.y, A.z), new Vector3(B.x, B.y, A.z), color, width));

            result.Add(new LineData(new Vector3(A.x, B.y, A.z), new Vector3(A.x, B.y, B.z), color, width));
            result.Add(new LineData(new Vector3(A.x, B.y, B.z), new Vector3(B.x, B.y, B.z), color, width));
            result.Add(new LineData(new Vector3(B.x, B.y, B.z), new Vector3(B.x, B.y, A.z), color, width));
            result.Add(new LineData(new Vector3(B.x, B.y, A.z), new Vector3(A.x, B.y, A.z), color, width));
            return result;
        }
        //private static List<HandleThing> BuildWireMesh(Vector3[] verts, int[] tris) {
        //    List<HandleThing> result = new List<HandleThing>();
        //    for (int i = 0; i < tris.Length; i += 3) {
        //        result.Add(new DebugLine(verts[tris[i]], verts[tris[i + 1]]));
        //        result.Add(new DebugLine(verts[tris[i]], verts[tris[i + 2]]));
        //        result.Add(new DebugLine(verts[tris[i + 1]], verts[tris[i + 2]]));
        //    }
        //    return result;
        //}
        //private static List<HandleThing> BuildWireMesh(Vector3[] verts, int[] tris, Color color) {
        //    List<HandleThing> result = new List<HandleThing>();
        //    for (int i = 0; i < tris.Length; i += 3) {
        //        result.Add(new DebugLineAAColored(verts[tris[i]], verts[tris[i + 1]], color));
        //        result.Add(new DebugLineAAColored(verts[tris[i]], verts[tris[i + 2]], color));
        //        result.Add(new DebugLineAAColored(verts[tris[i + 1]], verts[tris[i + 2]], color));
        //    }
        //    return result;
        //}

        public static Color GetSomeColor(int index) {
            switch (index) {
                case 0:
                return Color.blue;
                case 1:
                return Color.red;
                case 2:
                return Color.green;
                case 3:
                return Color.magenta;
                case 4:
                return Color.yellow;
                case 5:
                return Color.cyan;
                default:
                return Color.white;

            }
        }
        private static Vector3 V3small(float val) {
            return new Vector3(0, val, 0);
        }
        private static Vector3 AngleToDirection(float angle, float length) {
            return new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle) * length, 0, Mathf.Cos(Mathf.Deg2Rad * angle) * length);
        }

        private static Color SetAlpha(Color color, float alpha) {
            color.a = alpha;
            return color;
        }
        #endregion


    }
}

namespace K_PathFinder.PFDebuger.Helpers {
    public class ChunkDebugInfo {
        public bool showMe = true;

        #region long list of list with important debuged stuff
        //Cells
        public int cellCounter;
        public List<TriangleData> cellsArea = new List<TriangleData>();
        public List<LineData> cellEdges = new List<LineData>();
        public List<LineData> cellConnections = new List<LineData>();

        //covers
        public int coversCounter;
        public List<PointData> coverDots = new List<PointData>();
        public List<LineData> coverLines = new List<LineData>();
        public List<TriangleData> coverSheets = new List<TriangleData>();

        //grid
        public List<LineData> grid = new List<LineData>();

        //jump bases
        public int jumpBasesCounter;
        public List<LineData> jumpBasesLines = new List<LineData>();
        public List<PointData> jumpBasesDots = new List<PointData>();

        //voxels
        public int voxelsCounter;
        public List<PointData> voxelPos = new List<PointData>();
        public List<LineData> voxelConnections = new List<LineData>();
        public List<PointData> voxelLayer = new List<PointData>();

        public List<PointData> voxelRawMax = new List<PointData>();
        public List<PointData> voxelRawMin = new List<PointData>();
        public List<LineData> voxelRawVolume = new List<LineData>();

        //nodes
        public List<PointData> nodesPoints = new List<PointData>();
        public List<LineData> nodesLines = new List<LineData>();
        public List<PointData> nodesPointsPreRDP = new List<PointData>();
        public List<LineData> nodesLinesPreRDP = new List<LineData>();

        //bounds
        public List<LineData> colliderBounds = new List<LineData>();
        public List<LineData> chunkBounds = new List<LineData>();

        //walkable polygons
        public List<LineData> walkablePolygonLine = new List<LineData>();
        public List<TriangleData> walkablePolygonSheet = new List<TriangleData>();

        //triangulator
        public List<LineData> triangulator = new List<LineData>();
        #endregion

        public void Clear() {
            cellsArea.Clear();
            cellEdges.Clear();
            cellConnections.Clear();
            coverDots.Clear();
            coverLines.Clear();
            coverSheets.Clear();
            grid.Clear();
            jumpBasesLines.Clear();
            jumpBasesDots.Clear();
            voxelPos.Clear();
            voxelConnections.Clear();
            voxelLayer.Clear();
            voxelRawMax.Clear();
            voxelRawMin.Clear();
            voxelRawVolume.Clear();
            nodesPoints.Clear();
            nodesLines.Clear();
            nodesPointsPreRDP.Clear();
            nodesLinesPreRDP.Clear();
            colliderBounds.Clear();
            chunkBounds.Clear();
            walkablePolygonLine.Clear();
            walkablePolygonSheet.Clear();
            triangulator.Clear();
        }
    }

    #region HandlThings
    public abstract class HandleThing {
        public abstract void ShowHandle();
    }
    public class DebugLabel : HandleThing {
        public Vector3 pos;
        public string text;
        public DebugLabel(Vector3 pos, string text) {
            this.pos = pos;
            this.text = text;
        }

        public override void ShowHandle() {
            Handles.BeginGUI();
            Color color = GUI.color;
            GUI.color = Color.black;
            Handles.Label(pos, text);
            GUI.color = color;
            Handles.EndGUI();
        }
    }



    //currently no use outside labels



    //public class DebugLine : HandleThing {
    //    protected Vector3 v1, v2;
    //    public DebugLine(Vector3 v1, Vector3 v2) {
    //        this.v1 = v1;
    //        this.v2 = v2;
    //    }
    //    public override void ShowHandle() {
    //        Handles.DrawLine(v1, v2);
    //    }
    //}
    //public class DebugLineColored : DebugLine {
    //    Color color;
    //    public DebugLineColored(Vector3 from, Vector3 to, Color color) : base(from, to) {
    //        this.color = color;
    //    }
    //    public override void ShowHandle() {
    //        Handles.color = new Color(color.r, color.g, color.b, Handles.color.a);
    //        base.ShowHandle();
    //    }
    //}

    //public class DebugLineAA: DebugLine {
    //    public DebugLineAA(Vector3 from, Vector3 to) : base(from, to) {}
    //    public override void ShowHandle() {
    //        Handles.DrawAAPolyLine(v1, v2);
    //    }
    //}
    //public class DebugLineAAColored : DebugLine {
    //    protected Color color;
    //    public DebugLineAAColored(Vector3 from, Vector3 to, Color color) : base(from, to) {
    //        this.color = color;
    //    }
    //    public override void ShowHandle() {
    //        Handles.color = new Color(color.r, color.g, color.b, Handles.color.a);
    //        Handles.DrawAAPolyLine(v1, v2);
    //    }
    //}

    //public class DebugLineAASolid : DebugLine {
    //    public DebugLineAASolid(Vector3 from, Vector3 to) : base(from, to) { }

    //    public override void ShowHandle() {
    //        Color c = Handles.color;
    //        Handles.color = new Color(c.r, c.g, c.b, 1f);            
    //        Handles.DrawAAPolyLine(v1, v2);
    //        Handles.color = c;
    //    }
    //}
    //public class DebugLineAAColoredSolid : DebugLineAAColored {
    //    public DebugLineAAColoredSolid(Vector3 from, Vector3 to, Color color) : base(from, to, color) { }

    //    public override void ShowHandle() {
    //        Color c = Handles.color;
    //        Handles.color = base.color;
    //        Handles.DrawAAPolyLine(v1, v2);
    //        Handles.color = c;
    //    }
    //}
    //public class DebugBounds : HandleThing {
    //    Bounds bounds;
    //    bool haveColor;
    //    Color color; 

    //    public DebugBounds(Bounds bounds) {
    //        this.bounds = bounds;
    //        haveColor = false;
    //    }
    //    public DebugBounds(Bounds bounds, Color color) {
    //        this.bounds = bounds;
    //        haveColor = false;
    //        this.color = color;
    //    }

    //    public override void ShowHandle() {
    //        Color tColor = Handles.color;
    //        if (haveColor) 
    //            Handles.color = color;
            
    //        DrawParallelepiped(bounds.center - bounds.extents, bounds.center + bounds.extents);

    //        Handles.color = tColor;
    //    }

    //    private static void DrawParallelepiped(Vector3 A, Vector3 B) {    
    //        Handles.DrawAAPolyLine(new Vector3(A.x, A.y, A.z), new Vector3(A.x, A.y, B.z));
    //        Handles.DrawAAPolyLine(new Vector3(A.x, A.y, B.z), new Vector3(B.x, A.y, B.z));
    //        Handles.DrawAAPolyLine(new Vector3(B.x, A.y, B.z), new Vector3(B.x, A.y, A.z));
    //        Handles.DrawAAPolyLine(new Vector3(B.x, A.y, A.z), new Vector3(A.x, A.y, A.z));

    //        Handles.DrawAAPolyLine(new Vector3(A.x, A.y, A.z), new Vector3(A.x, B.y, A.z));
    //        Handles.DrawAAPolyLine(new Vector3(A.x, A.y, B.z), new Vector3(A.x, B.y, B.z));
    //        Handles.DrawAAPolyLine(new Vector3(B.x, A.y, B.z), new Vector3(B.x, B.y, B.z));
    //        Handles.DrawAAPolyLine(new Vector3(B.x, A.y, A.z), new Vector3(B.x, B.y, A.z));

    //        Handles.DrawAAPolyLine(new Vector3(A.x, B.y, A.z), new Vector3(A.x, B.y, B.z));
    //        Handles.DrawAAPolyLine(new Vector3(A.x, B.y, B.z), new Vector3(B.x, B.y, B.z));
    //        Handles.DrawAAPolyLine(new Vector3(B.x, B.y, B.z), new Vector3(B.x, B.y, A.z));
    //        Handles.DrawAAPolyLine(new Vector3(B.x, B.y, A.z), new Vector3(A.x, B.y, A.z));
    //    }
    //}

    //public abstract class DebugPosSize : HandleThing {
    //    protected Vector3 pos;
    //    protected float size;
    //    public DebugPosSize(Vector3 pos, float size) {
    //        this.pos = pos;
    //        this.size = size;
    //    }
    //}
    //public class DebugDotCap : DebugPosSize {
    //    public DebugDotCap(Vector3 pos, float size) : base(pos, size) { }

    //    public override void ShowHandle() {
    //        Handles.DotHandleCap(0, pos, Quaternion.identity, size, EventType.Repaint);
    //    }
    //}

    //public class DebugDotCapSolid : DebugPosSize {
    //    public DebugDotCapSolid(Vector3 pos, float size) : base(pos, size) { }

    //    public override void ShowHandle() {
    //        Color c = Handles.color;
    //        Handles.color = new Color(c.r, c.g, c.b, 1f);
    //        Handles.DotHandleCap(0, pos, Quaternion.identity, size, EventType.Repaint);
    //        Handles.color = c;
    //    }
    //}
    //public class DebugDisc : DebugPosSize {
    //    Vector3 normal;
    //    public DebugDisc(Vector3 pos, Vector3 normal, float radius) : base(pos, radius) {
    //        this.normal = normal;
    //    }
    //    public override void ShowHandle() {
    //        Handles.DrawSolidDisc(pos, normal, size);
    //    }
    //}
    //public class DebugDiscCameraFaced : DebugPosSize {
    //    public DebugDiscCameraFaced(Vector3 pos, float radius) : base(pos, radius) { }
    //    public override void ShowHandle() {
    //        Handles.DrawSolidDisc(pos, (pos - Camera.current.gameObject.transform.position), size);
    //    }
    //}
    //public class DebugSphere : DebugPosSize {
    //    public DebugSphere(Vector3 pos, float size) : base(pos, size) { }
    //    public override void ShowHandle() {
    //        Handles.SphereHandleCap(0, pos, Quaternion.identity, size, EventType.Repaint);
    //    }
    //}
    //public class DebugPolygon : HandleThing {
    //    Vector3 a, b, c;
    //    public DebugPolygon(Vector3 a, Vector3 b, Vector3 c) {
    //        this.a = a;
    //        this.b = b;
    //        this.c = c;
    //    }
    //    public override void ShowHandle() {
    //        Handles.DrawAAPolyLine(a, b, c, a);
    //    }
    //}
    //public class DebugCross3D : DebugPosSize {
    //    public DebugCross3D(Vector3 pos, float size) : base(pos, size) { }
    //    public override void ShowHandle() {
    //        Handles.DrawLine(new Vector3(pos.x - size, pos.y, pos.z), new Vector3(pos.x + size, pos.y, pos.z));
    //        Handles.DrawLine(new Vector3(pos.x, pos.y - size, pos.z), new Vector3(pos.x, pos.y + size, pos.z));
    //        Handles.DrawLine(new Vector3(pos.x, pos.y, pos.z - size), new Vector3(pos.x, pos.y, pos.z + size));
    //    }
    //}
    //public class DebugDotColored : DebugDotCap {
    //    Color color;
    //    public DebugDotColored(Vector3 pos, float size, Color color) : base(pos, size) {
    //        this.color = color;
    //    }

    //    public override void ShowHandle() {
    //        Handles.color = new Color(color.r, color.g, color.b, Handles.color.a);
    //        base.ShowHandle();
    //    }
    //}

    //public class DebugMeshFancy : HandleThing {
    //    Color color;
    //    Vector3[] points;
    //    public DebugMeshFancy(Vector3[] points, Color color) {
    //        this.color = color;
    //        this.points = points;
    //    }

    //    public override void ShowHandle() {
    //        Handles.color = new Color(color.r, color.g, color.b, Handles.color.a);
    //        Handles.DrawAAConvexPolygon(points);
    //    }
    //}
    //public class DebugMesh : HandleThing {
    //    Vector3[] points;
    //    public DebugMesh(params Vector3[] points) {
    //        this.points = points;
    //    }

    //    public override void ShowHandle() {
    //        Handles.DrawAAConvexPolygon(points);
    //    }
    //}

    //public class DebugWireArc : HandleThing {
    //    Vector3 center, normal, from;
    //    float radius, angle;

    //    bool colored;
    //    Color color;

    //    public DebugWireArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius) {
    //        this.center = center;
    //        this.normal = normal;
    //        this.from = from;
    //        this.angle = angle;
    //        this.normal = normal;
    //        this.radius = radius;
    //    }

    //    public DebugWireArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius, Color color) :this(center, normal, from, angle, radius) {
    //        this.colored = true;
    //        this.color = color;
    //    }

    //    public override void ShowHandle() {
    //        if (colored) {
    //            Color handlesColor = Handles.color;
    //            Handles.color = new Color(color.r, color.g, color.b, handlesColor.a);

    //            Handles.DrawWireArc(center, normal, from, angle, radius);
    //            Handles.color = handlesColor;
    //        }
    //        else {
    //            Handles.DrawWireArc(center, normal, from, angle, radius);
    //        }    
    //    }
    //}
    
    //public class DebugWireDisc : HandleThing {
    //    Vector3 position;
    //    Vector3 normal;
    //    float radius;

    //    bool colored;
    //    Color color;

    //    public DebugWireDisc(Vector3 position, Vector3 normal, float radius) {
    //        this.position = position;
    //        this.normal = normal;
    //        this.radius = radius;
    //    }

    //    public DebugWireDisc(Vector3 position, Vector3 normal, float radius, Color color) :this(position, normal, radius) {
    //        this.colored = true;
    //        this.color = color;
    //    }


    //    public override void ShowHandle() {
    //        if (colored) {
    //            Color handlesColor = Handles.color;
    //            Handles.color = new Color(color.r, color.g, color.b, handlesColor.a);

    //            Handles.DrawWireDisc(position, normal, radius);
    //            Handles.color = handlesColor;
    //        }
    //        else {
    //            Handles.DrawWireDisc(position, normal, radius);
    //        }
    //    }
    //}
    //public class DebugPolyLine : HandleThing {
    //    Vector3[] positions;
    //    bool colored, solid;
    //    Color color;

    //    public DebugPolyLine(bool solid = false, params Vector3[] positions) {
    //        this.solid = solid;
    //        this.positions = positions;
    //    }
    //    public DebugPolyLine(Color color, bool solid = false, params Vector3[] positions) {
    //        this.positions = positions;
    //        this.solid = solid;
    //        this.colored = true;
    //        this.color = color;
    //    }

    //    public override void ShowHandle() {
    //        if (colored) {
    //            Color handlesColor = Handles.color;
    //            if (solid)
    //                Handles.color = color;
    //            else
    //                Handles.color = new Color(color.r, color.g, color.b, handlesColor.a);

    //            Handles.DrawPolyLine(positions);
    //            Handles.color = handlesColor;
    //        }
    //        else {
    //            Handles.DrawPolyLine(positions);
    //        }
    //    }
    //}

    //public class DebugLineAAAwesome: HandleThing {
    //    Vector3 a,b;
    //    bool colored, solid;
    //    Color color;

    //    public DebugLineAAAwesome(Vector3 a, Vector3 b) {
    //        this.a = a;
    //        this.b = b;
    //    }
    //    public DebugLineAAAwesome(Vector3 a, Vector3 b, Color color, bool solid = false) {
    //        this.a = a;
    //        this.b = b;
    //        this.solid = solid;
    //        this.colored = true;
    //        this.color = color;
    //    }
        
    //    public override void ShowHandle() {
    //        if (colored) {
    //            Color handlesColor = Handles.color;
    //            if (solid)
    //                Handles.color = color;
    //            else
    //                Handles.color = new Color(color.r, color.g, color.b, handlesColor.a);

    //            Handles.DrawAAPolyLine(a,b);
    //            Handles.color = handlesColor;
    //        }
    //        else {
    //            Handles.DrawAAPolyLine(a, b);
    //        }
    //    }
    //}
    //public class DebugMesh : HandleThing {
    //    //Color color;
    //    Mesh mesh;
    //    Matrix4x4 matrix;
    //    public DebugMesh(Vector3[] verts, int[] tris, Color color) {
    //        //this.color = color;
    //        this.mesh = new Mesh();
    //        mesh.vertices = verts;
    //        mesh.triangles = tris;
    //    }

    //    public DebugMesh(Mesh mesh, Color color) {
    //        //this.color = color;
    //        this.mesh = mesh;
    //        matrix = Matrix4x4.identity;
    //    }

    //    public override void ShowHandle() {
    //        Graphics.DrawMeshNow(mesh, matrix, 2);
    //    }
    //}
    #endregion
}
#endif