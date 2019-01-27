using UnityEngine;
using System.Collections;
using K_PathFinder.Settings;
using UnityEditor;
using K_PathFinder.PFDebuger;
using K_PathFinder;
using K_PathFinder.Graphs;

//debuger and settings
namespace K_PathFinder {
    public class PathFinderMenu : EditorWindow {
        bool sellectorMove = false;

        #region properties
        SerializedObject settingsObject;

        #region upper tab
        SerializedProperty targetProperties;
        static string targetPropertiesString = "targetProperties";
        static GUIContent targetPropertiesContent = new GUIContent("Properties", "Build navmesh using this properties");

        SerializedProperty drawBuilder;
        static string drawBuilderString = "drawBuilder";
        static GUIContent drawBuilderContent = new GUIContent("General", "");

        static string buildAreaString = "Build Area Sellector";

        //SerializedProperty startX;
        //static string startXString = "startX";
        static GUIContent startXContent = new GUIContent("X:", "");

        //SerializedProperty startZ;
        //static string startZString = "startZ";
        static GUIContent startZContent = new GUIContent("Z:", "");

        //SerializedProperty sizeX;
        //static string sizeXString = "sizeX";
        static GUIContent sizeXContent = new GUIContent("X:", "");

        //SerializedProperty sizeZ;
        //static string sizeZString = "sizeZ";
        static GUIContent sizeZContent = new GUIContent("Z:", "");


        string forgotToAddPropertiesWarning = "Put some Properties into Properties object tab so PathFinder know what Properties it should use to build NavMesh";



        //static GUIContent sellectStartButton = new GUIContent("Sellect Start", "Sellect area where navmesh builder will work");
        //static GUIContent sellectStartLabel = new GUIContent("Sellect Start", "Sellect area where navmesh builder will work. To sellect click somwhere in scene");

        //static GUIContent sellectSizeButton = new GUIContent("Sellect Size", "Sellect area where navmesh builder will end work");
        //static GUIContent sellectSizeLabel = new GUIContent("Sellect Size", "Sellect area where navmesh builder will end work. To sellect click somwhere in scene");

        static GUIContent leftBoxContent = new GUIContent("NavMesh Building");
        static GUIContent rightBoxContent = new GUIContent("NavMesh Saving");

        static GUIContent buildContent = new GUIContent("Build", "Build navmesh in sellected area");
        static GUIContent removeContent = new GUIContent("Remove", "Remove navmesh from sellected area. Only target area will be removed");
        static GUIContent removeAndRebuildContent = new GUIContent("Remove & Rebuild", "Remove navmesh from sellected area and rebuild after. Only target area will be removed");
        static GUIContent rebuildToggleContent = new GUIContent("", "Queue removed again? If true then we refresh sellected chunks");
        static GUIContent clearContent = new GUIContent("Clear", "Remove all NavMesh. Also stop all work");

        static GUIContent saveContent = new GUIContent("Save", "Save all current navmesh into SceneNavmeshData. If it not exist then suggest to create one and pass reference to it into scene helper.");
        static GUIContent loadContent = new GUIContent("Load", "Load current SceneNavmeshData from scene helper");
        static GUIContent deleteContent = new GUIContent("Delete", "Remove all serialized data from current NavMesh data. Scriptable object remain in project");
        #endregion


        #region settings tab

        #endregion
        SerializedProperty helperName;
        static string helperNameString = "helperName";
        static GUIContent helperNameContent = new GUIContent("Helper name", "pathfinder need object in scene in order to use unity API. you can specify it's name here");


        SerializedProperty useMultithread;
        static string useMultithreadString = "useMultithread";
        static GUIContent useMultithreadContent = new GUIContent("Multithread", "you can on/off multithreading for debug purpose. cause debuging threads is pain");

        SerializedProperty maxThreads;
        static string maxThreadsString = "maxThreads";
        static GUIContent maxThreadsContent = new GUIContent("Max Threads", "limit how much threads are used");

        SerializedProperty terrainCollectionType;
        static string terrainCollectionTypeString = "terrainCollectionType";
        static GUIContent terrainCollectionTypeContent = new GUIContent("Terrain Collector", "UnityWay - Collect data from terrain using Terrain.SampleHeight and TerrainData.GetSteepness. It's fast but it's all in main thread.\nCPU - Collect data by some fancy math using CPU. Not that fast but fully threaded.\nComputeShader - Superfast but in big chunks can be slow cause moving data from GPU is not that fast.");

        SerializedProperty colliderCollectionType;
        static string colliderCollectionTypeString = "colliderCollectionType";
        static GUIContent colliderCollectionTypeContent = new GUIContent("Collider Collector", "CPU - Collect data using CPU rasterization. It's threaded so no FPS drops here. \nComputeShader - Collect data by ComputeShader. Superfast but in big chunks can be slow cause moving data from GPU is not that fast.");

        SerializedProperty gridSize;
        static string gridSizeString = "gridSize";
        static GUIContent gridSizeContent = new GUIContent("World grid size", "Chunk size in world space. Good values are 10, 15, 20 etc.");


        //static GUIContent gridMinMaxContent = new GUIContent("Chunk Height Range", "For autocreating chunks. World space value is grid size * this value.");

        SerializedProperty gridHighest;
        static string gridHighestString = "gridHighest";
        static GUIContent gridHighestContent = new GUIContent("Max", "For autocreating chunks. World space value is grid size * this value.");

        SerializedProperty gridLowest;
        static string gridLowestString = "gridLowest";
        static GUIContent gridLowestContent = new GUIContent("Chunk Height Min", "For autocreating chunks. World space value is grid size * this value.");
        #endregion
        
        const float LABEL_WIDTH = 105f;

        Vector2 scroll;

        //settings
        PathFinderSettings settings;
        private float desiredLabelWidth;

        [SerializeField]
        bool _showSettings = true;

        //debuger
        Vector3 start, end;
        bool sellectStart, sellectEnd;
        //Vector3 pointer = Vector3.zero;

        [SerializeField]
        bool _showDebuger = true;
        //[SerializeField]
        //bool _redoRemovedGraphs = true;

        GUILayoutOption[] guiLayoutForNiceLine = new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) };
        
        Vector3 s_Center;
        Vector3 s_p_Right;

        SettingsDrawer settingsDrawer;

        [MenuItem(PathFinderSettings.UNITY_TOP_MENU_FOLDER + "/Menu", false, 0)]
        public static void OpenWindow() {
            GetWindow<PathFinderMenu>("PathFinderMenu").Show();
        }

        void OnEnable() {
            Debuger_K.Init();
            PathFinder.Init("PathFinderMenu");
            settings = PathFinder.settings;
            settingsDrawer = new SettingsDrawer(settings);
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
            Repaint();
            this.autoRepaintOnSceneChange = true;

            settingsObject = new SerializedObject(settings);
            targetProperties = settingsObject.FindProperty(targetPropertiesString);
            drawBuilder = settingsObject.FindProperty(drawBuilderString);

            helperName = settingsObject.FindProperty(helperNameString);
            useMultithread = settingsObject.FindProperty(useMultithreadString);
            maxThreads = settingsObject.FindProperty(maxThreadsString);

            terrainCollectionType = settingsObject.FindProperty(terrainCollectionTypeString);
            colliderCollectionType = settingsObject.FindProperty(colliderCollectionTypeString);
            
            gridSize = settingsObject.FindProperty(gridSizeString);
            gridHighest = settingsObject.FindProperty(gridHighestString);
            gridLowest = settingsObject.FindProperty(gridLowestString);


            //float gs = settings.gridSize;
            //s_Center = new Vector3((settings.startX + (settings.sizeX * 0.5f)) * gs, settings.pointerY, (settings.startX + (settings.sizeX * 0.5f)) * gs);
        }

        void OnDestroy() {
            EditorUtility.SetDirty(settings);
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            Debuger_K.SetSettingsDirty();
        }
        
        void OnGUI() {
            scroll = GUILayout.BeginScrollView(scroll);
            float curLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            settingsObject.Update();

            try {         
                ImportantButtons();
            }
            catch (System.Exception e) {
                GUILayout.Label(string.Format("Exception has ocured in importand buttons.\n\nException:\n{0}", e));
            }

            UITools.Line();

            _showSettings = EditorGUILayout.Foldout(_showSettings, "Settings");
            if (_showSettings) {
                try {           
                    ShowSettings();
                }
                catch (System.Exception e) {
                    GUILayout.Label(string.Format("Exception has ocured while showing settings.\n\nException:\n{0}", e));      
                }
            }

            UITools.Line();
            

            _showDebuger = EditorGUILayout.Foldout(_showDebuger, "Debuger");
            if (_showDebuger) {
                try {
                    ShowDebuger();
                }
                catch (System.Exception e) {
                    GUILayout.Label(string.Format("Exception has ocured while showing debuger.\n\nException:\n{0}", e));
                }
            }

            UITools.Line();

            EditorGUIUtility.labelWidth = curLabelWidth;

            GUILayout.EndScrollView();

            if (GUI.changed) {
                EditorUtility.SetDirty(settings);
                Debuger_K.SetSettingsDirty();
                Repaint();
            }

            settingsObject.ApplyModifiedProperties();
        }
        

        void OnSceneGUI(SceneView sceneView) {
            Event curEvent = Event.current;
            Color col = Handles.color;
            Handles.color = Color.red;
            //float gs = settings.gridSize;

 

            AreaPointer targetData = settings.areaPointer * settings.gridSize;
            bool isMoved;
            targetData = MyHandles.DrawData(targetData, 2.5f, settings.gridSize, out isMoved);

            if (PathFinderSettings.isAreaPointerMoving) {
                settings.areaPointer = targetData / settings.gridSize;
                EditorUtility.SetDirty(settings);
                SceneView.RepaintAll();
            }

            if(curEvent.type == EventType.Used) {
                PathFinderSettings.isAreaPointerMoving = isMoved;
            }

            Handles.color = col;





            if (sellectorMove) {
                XZPosInt pointerPos;
                RaycastHit hit;
                Ray ray = HandleUtility.GUIPointToWorldRay(curEvent.mousePosition);
                if (Physics.Raycast(ray, out hit)) {
                    pointerPos = PathFinder.ToChunkPosition(hit.point);                              
                }
                else {
                    Vector3 intersection;
                    if (Math3d.LinePlaneIntersection(out intersection, ray.origin, ray.direction, Vector3.up, new Vector3())) {
                        pointerPos = PathFinder.ToChunkPosition(intersection);
                    }
                    else {
                        pointerPos = new XZPosInt();
                    }
                }

                settingsDrawer.MovePointer(new AreaPointer(pointerPos.x, pointerPos.z, pointerPos.x + 1, pointerPos.z + 1, hit.point.y));

                if (curEvent.type == EventType.MouseDown && curEvent.button == 0) {
                    sellectorMove = false;
                }

                Repaint();
                SceneView.RepaintAll();
            }





            //if (RVOPF.RVOSimulator.instance.debug) {
            //    foreach (var cone in RVOPF.RVOSimulator.instance.collisionCones) {
            //        if (cone.agent == null | cone.other == null)
            //            continue;

            //        Vector3 offset = new Vector3(0, cone.heightOffset, 0);
            //        Vector3 agentPos = cone.agent.positionV3 + offset;
            //        float agentY = agentPos.y;
            //        Vector3 conePos = new Vector3(cone.position.x, agentY, cone.position.y);
            //        Vector3 coneDir = new Vector3(cone.direction.x, agentY, cone.direction.y);

            //        Handles.color = Color.green;
            //        Handles.DrawLine(agentPos, conePos);
            //        Handles.color = Color.red;
            //        Handles.DrawLine(cone.other.positionV3, conePos);

            //        Handles.color = new Color(cone.color.r, cone.color.g, cone.color.b, 0.1f);
            //        Handles.DrawLine(conePos, conePos + coneDir);

            //        float v = Vector2.SignedAngle(Vector2.up, cone.direction);

            //        Quaternion q1 = Quaternion.AngleAxis(v, new Vector3(0, 0, 1));
            //        Quaternion q2 = Quaternion.AngleAxis(v + cone.angle, new Vector3(0, 0, 1));
            //        Quaternion q3 = Quaternion.AngleAxis(v - cone.angle, new Vector3(0, 0, 1));

            //        float targetDistance = Vector2.Distance(cone.agent.positionV2, cone.other.positionV2) + cone.other.radius + cone.agent.radius;

            //        Vector2 P1 = q1 * Vector2.up * targetDistance;
            //        Vector2 P2 = q2 * Vector2.up * targetDistance;
            //        Vector2 P3 = q3 * Vector2.up * targetDistance;

            //        Handles.DrawSolidArc(conePos, Vector3.up, new Vector3(P2.x, 0, P2.y), cone.angle * 2, targetDistance);

            //        Handles.color = cone.color;
            //        Handles.DrawLine(conePos, conePos + new Vector3(P2.x, 0, P2.y));
            //        Handles.DrawLine(conePos, conePos + new Vector3(P3.x, 0, P3.y));

            //        Handles.color = Color.black;
            //        Handles.DrawWireDisc(cone.other.positionV3 + offset, Vector3.up, cone.other.radius);
            //        Handles.DrawWireDisc(cone.other.positionV3 + offset, Vector3.up, cone.other.radius + cone.agent.radius);
            //        Handles.DrawLine(cone.other.positionV3, cone.other.positionV3 + offset);

            //    }
            //}

            Debuger_K.DrawDebugLabels();

            Handles.BeginGUI();
            Debuger_K.DrawSceneGUI();
            Handles.EndGUI();
        }

        private void ImportantButtons() {
            //properties      
            
            EditorGUI.BeginChangeCheck();
            bool someBool = EditorGUILayout.Foldout(drawBuilder.boolValue, drawBuilderContent);
            if (EditorGUI.EndChangeCheck()) {
                drawBuilder.boolValue = someBool;
            }

            

            if (drawBuilder.boolValue) {
                float rightOffset = 30;
                float singleLineHeight = EditorGUIUtility.singleLineHeight;

                EditorGUILayout.PropertyField(targetProperties, targetPropertiesContent);

                //sellected chunks
                #region sellector
                Rect buildAreaRect = GUILayoutUtility.GetRect(Screen.width - rightOffset, singleLineHeight * 3);       

                Rect baLeftRect = new Rect(buildAreaRect.x, buildAreaRect.y + singleLineHeight, buildAreaRect.width * 0.5f, buildAreaRect.height - singleLineHeight);
                Rect baRightRect = new Rect(buildAreaRect.x + (buildAreaRect.width * 0.5f), buildAreaRect.y + singleLineHeight, buildAreaRect.width * 0.5f, buildAreaRect.height - singleLineHeight);

                GUI.Box(buildAreaRect, buildAreaString);
                //GUI.Box(baLeftRect, string.Empty);
                //GUI.Box(baRightRect, string.Empty);

                AreaPointer areaPointer = settings.areaPointer;

                float tLabelSize = 40;
                float tRemainSize = Mathf.Max(baRightRect.width - tLabelSize, 0);
                int pStartX, pStartZ, pSizeX, pSizeZ;
                Rect rectStartLablel = new Rect(baRightRect.x, baRightRect.y, tLabelSize, singleLineHeight);
                Rect rectStartX = new Rect(baRightRect.x + tLabelSize, baRightRect.y, tRemainSize * 0.5f, singleLineHeight);
                Rect rectStartZ = new Rect(baRightRect.x + tLabelSize + (tRemainSize * 0.5f), baRightRect.y, tRemainSize * 0.5f, singleLineHeight);

                Rect rectSizeLablel = new Rect(baRightRect.x, baRightRect.y + singleLineHeight, tLabelSize, singleLineHeight);
                Rect rectSizeX = new Rect(baRightRect.x + tLabelSize, baRightRect.y + singleLineHeight, tRemainSize * 0.5f, singleLineHeight);
                Rect rectSizeZ = new Rect(baRightRect.x + tLabelSize + (tRemainSize * 0.5f), baRightRect.y + singleLineHeight, tRemainSize * 0.5f, singleLineHeight);

                EditorGUIUtility.labelWidth = tLabelSize;
                EditorGUI.LabelField(rectStartLablel, "Start");
                EditorGUI.LabelField(rectSizeLablel, "Size");

                EditorGUIUtility.labelWidth = 15;
                EditorGUI.BeginChangeCheck();
                pStartX = EditorGUI.IntField(rectStartX, startXContent, areaPointer.roundStartX);
                pStartZ = EditorGUI.IntField(rectStartZ, startZContent, areaPointer.roundStartZ);
                pSizeX = EditorGUI.IntField(rectSizeX, sizeXContent, areaPointer.roundSizeX);
                pSizeZ = EditorGUI.IntField(rectSizeZ, sizeZContent, areaPointer.roundSizeZ);
                if (EditorGUI.EndChangeCheck()) {
                    settings.areaPointer = new AreaPointer(pStartX, pStartZ, pStartX + pSizeX, pStartZ + pSizeZ, settings.areaPointer.y);
                }
                EditorGUIUtility.labelWidth = LABEL_WIDTH;

                if (sellectorMove) {
                    GUI.Box(new Rect(baLeftRect.x, baLeftRect.y, baLeftRect.width, singleLineHeight), "Move");
                }
                else {
                    if (GUI.Button(new Rect(baLeftRect.x, baLeftRect.y, baLeftRect.width, singleLineHeight), "Move")) {
                        sellectorMove = true;
                    }
                }

                if (GUI.Button(new Rect(baLeftRect.x, baLeftRect.y + singleLineHeight, baLeftRect.width, singleLineHeight), "Reset")) {
                    settingsDrawer.ResetAreaPointer();
                }


                #endregion

                GUILayout.Space(5);

                //control buttons
                #region control buttons
                Rect things = GUILayoutUtility.GetRect(Screen.width - rightOffset, singleLineHeight * 4);

                Rect left = new Rect(things.x, things.y, things.width * 0.5f, things.height);
                Rect right = new Rect(things.x + (things.width * 0.5f), things.y, things.width * 0.5f, things.height);
            
                GUI.Box(left, leftBoxContent);
                GUI.Box(right, rightBoxContent);

                #region navmesh building
                Rect rectBuild = new Rect(left.x, left.y + singleLineHeight, left.width, singleLineHeight);

                if (GUI.Button(rectBuild, buildContent)) {
                    if (settings.targetProperties != null) 
                        PathFinder.QueueGraph(
                            settings.areaPointer.roundStartX, 
                            settings.areaPointer.roundStartZ, 
                            settings.targetProperties,
                            settings.areaPointer.roundSizeX, 
                            settings.areaPointer.roundSizeZ);          
                    else
                        Debug.LogWarning(forgotToAddPropertiesWarning);
                }


                GUIContent targetRemoveContent = settings.removeAndRebuild ? removeAndRebuildContent : removeContent;

                Rect rectRemove = new Rect(left.x, left.y + (singleLineHeight * 2), left.width - singleLineHeight, singleLineHeight);
                Rect rectRemoveToggle = new Rect(left.x + (left.width - singleLineHeight), left.y + (singleLineHeight * 2), singleLineHeight, singleLineHeight);

                if (GUI.Button(rectRemove, targetRemoveContent)) {
                    if (settings.targetProperties != null)
                        PathFinder.RemoveGraph(
                            settings.areaPointer.roundStartX,
                            settings.areaPointer.roundStartZ,
                            settings.targetProperties,
                            settings.areaPointer.roundSizeX,
                            settings.areaPointer.roundSizeZ, 
                            settings.removeAndRebuild);
                    else
                        Debug.LogWarning(forgotToAddPropertiesWarning);           
                }

                EditorGUI.BeginChangeCheck();
                someBool = GUI.Toggle(rectRemoveToggle, settings.removeAndRebuild, rebuildToggleContent);
                if (EditorGUI.EndChangeCheck()) settings.removeAndRebuild = someBool;

                Rect rectClear = new Rect(left.x, left.y + (singleLineHeight * 3), left.width, singleLineHeight);

                if (GUI.Button(rectClear, clearContent)) {
                    PathFinder.ClearAllWork();
                    Debuger_K.ClearChunksDebug();
                }
                #endregion

                if (GUI.Button(new Rect(right.x, right.y + singleLineHeight, right.width, singleLineHeight), saveContent))
                    PathFinder.SaveCurrentSceneData();
                if (GUI.Button(new Rect(right.x, right.y + (singleLineHeight * 2), right.width, singleLineHeight), loadContent))
                    PathFinder.LoadCurrentSceneData();
                if (GUI.Button(new Rect(right.x, right.y + (singleLineHeight * 3), right.width, singleLineHeight), deleteContent))
                    PathFinder.ClearCurrenSceneData();
                #endregion
            }  
        }

        private void ShowSettings() {
            if(settings == null)
                settings = PathFinderSettings.LoadSettings();

            EditorGUILayout.PropertyField(helperName, helperNameContent);

            if (useMultithread.boolValue) {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(useMultithread, useMultithreadContent);
                EditorGUIUtility.labelWidth = 80;
                EditorGUILayout.PropertyField(maxThreads, maxThreadsContent);
                EditorGUIUtility.labelWidth = LABEL_WIDTH;
                GUILayout.EndHorizontal();
            }
            else {
                EditorGUILayout.PropertyField(useMultithread, useMultithreadContent);
            }

            EditorGUILayout.PropertyField(terrainCollectionType, terrainCollectionTypeContent);
            EditorGUILayout.PropertyField(colliderCollectionType, colliderCollectionTypeContent);

            float someFloat;
            EditorGUI.BeginChangeCheck();
            someFloat = EditorGUILayout.FloatField(gridSizeContent, gridSize.floatValue);
            if (EditorGUI.EndChangeCheck()) {
                settings.gridSize = someFloat;
                PathFinder.gridSize = someFloat;
            }

            GUILayout.BeginHorizontal();    
            EditorGUILayout.PropertyField(gridLowest, gridLowestContent);
            EditorGUIUtility.labelWidth = 30;
            EditorGUILayout.PropertyField(gridHighest, gridHighestContent);          
            EditorGUIUtility.labelWidth = LABEL_WIDTH;
            GUILayout.EndHorizontal();

            if (gridHighest.intValue < gridLowest.intValue)
                gridHighest.intValue = gridLowest.intValue;

            if (gridLowest.intValue > gridHighest.intValue)
                gridLowest.intValue = gridHighest.intValue;

            UITools.Line();
            settingsDrawer.DrawAreaEditor();
        }

        private void ShowDebuger() {
            Debuger_K.settings.doDebug = EditorGUILayout.Toggle(new GUIContent("Do debug", "enable debuging. debuged values you can enable down here. generic values will be debuged anyway"), Debuger_K.settings.doDebug);
            if (Debuger_K.settings.doDebug) {
                Debuger_K.settings.debugOnlyNavmesh = EditorGUILayout.Toggle(new GUIContent("Full Debug", "if false will debug only resulted navmesh. prefer debuging only navmesh. and do not use unity profiler if you enable this option or else unity will die in horribly way. also do not enable it if area are too big. memory expensive stuff here!"), Debuger_K.settings.debugOnlyNavmesh);
            }

            Debuger_K.settings.doProfilerStuff = EditorGUILayout.Toggle(new GUIContent("Do profiler", "are we using some simple profiling? cause unity dont really profile threads. if true will write lots of stuff to console"), Debuger_K.settings.doProfilerStuff);
            Debuger_K.settings.doDebugPaths = EditorGUILayout.Toggle(new GUIContent("Debug Paths", "If true then pathfinder will put lot's of info into paths debug. Like cell path or cost of some other info"), Debuger_K.settings.doDebugPaths);
            Debuger_K.settings.showSceneGUI = EditorGUILayout.Toggle(new GUIContent("Scene GUI", "Enable or disable checkboxes in scene to on/off debug of certain chunks and properties. To apply changes push Update button"), Debuger_K.settings.showSceneGUI);
            Debuger_K.settings.clearGenericOnUpdate = EditorGUILayout.Toggle(new GUIContent("Clear Generic on Update", "Things listed below like Dots, Lines, Meshes, or even Path considered as Generic. if you want you can disable or enable clearing it on Update"), Debuger_K.settings.clearGenericOnUpdate);
            
            GUILayout.Box(string.Empty, guiLayoutForNiceLine);
            Debuger_K.settings.debugRVO = EditorGUILayout.Toggle(new GUIContent("Debug Velocity Obstacles"), Debuger_K.settings.debugRVO);
            if (Debuger_K.settings.debugRVO) {
                Debuger_K.settings.debugRVObasic = EditorGUILayout.Toggle(new GUIContent("VO Basic"), Debuger_K.settings.debugRVObasic);
                Debuger_K.settings.debugRVOvelocityObstacles = EditorGUILayout.Toggle(new GUIContent("VO Neighbours Info"), Debuger_K.settings.debugRVOvelocityObstacles);
                Debuger_K.settings.debugRVOconvexShape = EditorGUILayout.Toggle(new GUIContent("VO Velocity Shape"), Debuger_K.settings.debugRVOconvexShape);
                Debuger_K.settings.debugRVOplaneIntersections = EditorGUILayout.Toggle(new GUIContent("VO Plane Intersections"), Debuger_K.settings.debugRVOplaneIntersections);
            }
            GUILayout.Box(string.Empty, guiLayoutForNiceLine);

            Debuger_K.GenericGUI();

            Debuger_K.settings.showSelector = EditorGUILayout.Foldout(Debuger_K.settings.showSelector, "Debug options");
            if (Debuger_K.settings.showSelector) {
                Debuger_K.SellectorGUI2();
                //Debuger_K.SellectorGUI();
            }
        }
    }
}
