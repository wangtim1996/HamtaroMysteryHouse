using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using K_PathFinder;
using K_PathFinder.PFDebuger;
using UnityEditorInternal;

namespace K_PathFinder {
    [CustomEditor(typeof(AreaWorldMod))]
    public class AreaWorldModEditor : Editor {
        static bool editShape = true;
        static bool drawShapeList = true;
        static bool drawGameObjectList = true;
        AreaWorldModMagicValueType curShape = AreaWorldModMagicValueType.Cuboid;

        IndexedFlag mouseOver = IndexedFlag.nope;
        
        Color colorSellectionMouseOver = new Color(0.8f, 0.8f, 0.8f, 1f);

        GenericMenu onShapeClick;
        
        bool cursorInsideSellectedArea = false;
        bool tempFlagCursorInsideSellectedArea;
        
        static string allModsString = "allMods";
        //static string filterString = "filter";

        SerializedProperty mode;
        static string modeString = "mode";
        static GUIContent modeContent = new GUIContent("Mode", "");

        SerializedProperty areaInt;
        static string areaIntString = "areaInt";

        SerializedProperty priority;
        static string priorityString = "priority";
        static GUIContent priorityContent = new GUIContent("Priority", "Value that describe order of applying Area modifications. Biggest number applyed first, smallest last. When values same then used priority from actual Area");
        
        SerializedProperty drawGizmos;
        static string drawGizmosString = "drawGizmos";
        static GUIContent drawGizmosContent = new GUIContent("Draw Gizmos", "");

        SerializedProperty drawGizmosAlpha;
        static string drawGizmosAlphaString = "drawGizmosAlpha";
        static GUIContent drawGizmosAlphaContent = new GUIContent("Gizmos Alpha", "Gizmos alpha in scene in case it is to thick color or opposite");

        SerializedProperty drawBounds;
        static string drawBoundsString = "drawBounds";
        static GUIContent drawBoundsContent = new GUIContent("Draw Bounds", "In case you wandering: This is how Pathfinder check if this thing are inside chunk or not. So avoid too big clusters of shapes inside one modifyer if they are far apart from each others");

        //area advanced
        static GUIContent useAdvancedAreaContent = new GUIContent("Use Advanced Area", "Very important case. If this option used then this thing will generate it's own area and use it. This unlock additional options that can be performed with area");

        SerializedProperty advancedArea;
        static string advancedAreaString = "advancedArea";

        static GUIContent cellUsabilityStateContent = new GUIContent("CellUsabilityState", "");


        private void OnEnable() {
            mode = serializedObject.FindProperty(modeString);
            areaInt = serializedObject.FindProperty(areaIntString);
            priority = serializedObject.FindProperty(priorityString);
            drawGizmos = serializedObject.FindProperty(drawGizmosString);
            drawGizmosAlpha = serializedObject.FindProperty(drawGizmosAlphaString);
            drawBounds = serializedObject.FindProperty(drawBoundsString);

            advancedArea = serializedObject.FindProperty(advancedAreaString);            
        }
        
        public override void OnInspectorGUI() {
            serializedObject.Update(); 

            AreaWorldMod currentTarget = (target as AreaWorldMod);

            Event e = Event.current;
            EditorGUILayout.PropertyField(priority, priorityContent);


            //this one is important
            EditorGUI.BeginChangeCheck();
            bool useAA = EditorGUILayout.ToggleLeft(useAdvancedAreaContent, currentTarget.useAdvancedArea);
            if (EditorGUI.EndChangeCheck()) {
                //SceneView.RepaintAll();
                currentTarget.SetUseAdvancedArea(useAA);
            }

            if (currentTarget.useAdvancedArea) {
                EditorGUILayout.PropertyField(advancedArea);

                EditorGUI.BeginChangeCheck();
                bool startState = EditorGUILayout.ToggleLeft(cellUsabilityStateContent, currentTarget.cellUsabilityState);
                if (EditorGUI.EndChangeCheck()) {    
                    currentTarget.SetCellsState(startState);
                }

            }
            else {
                EditorGUILayout.PropertyField(areaInt);
            }

            UITools.Line();

            EditorGUILayout.PropertyField(drawGizmos, drawGizmosContent);

            if (drawGizmos.boolValue) {
                EditorGUILayout.PropertyField(drawGizmosAlpha, drawGizmosAlphaContent);
                EditorGUILayout.PropertyField(drawBounds, drawBoundsContent);
            }
            UITools.Line();
            EditorGUILayout.PropertyField(mode, modeContent);
            UITools.Line();
            //EditorGUILayout.PropertyField(serializedObject.FindProperty(filterString));
            //SmallUITools.Line();

            drawShapeList = EditorGUILayout.Foldout(drawShapeList, new GUIContent("Shape List"));

            if (drawShapeList) {
                GUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                editShape = EditorGUILayout.ToggleLeft("Edit Shape", editShape, GUILayout.MaxWidth(80));
                if (EditorGUI.EndChangeCheck()) {
                    Repaint();
                    SceneView.RepaintAll();
                }

                curShape = (AreaWorldModMagicValueType)EditorGUILayout.EnumPopup(curShape);
                bool flag = false;
                if (GUILayout.Button("Add", GUILayout.MaxHeight(14))) {
                    Undo.RecordObject(target, "Add");
                    currentTarget.ShapeAdd(curShape);
                    flag = true;
                }
                if (flag) {
                    SceneView.RepaintAll();
                }            
                GUILayout.EndHorizontal();

                Rect lastRect = GUILayoutUtility.GetLastRect();
                float lastHeight = lastRect.y + lastRect.height;
                SerializedProperty allMods = serializedObject.FindProperty(allModsString);

                mouseOver = IndexedFlag.nope;
                cursorInsideSellectedArea = false;

                for (int i = 0; i < allMods.arraySize; i++) {
                    SerializedProperty arrayElement = allMods.GetArrayElementAtIndex(i);

                    float height = EditorGUI.GetPropertyHeight(arrayElement) + 2;
                    Rect boxRect = new Rect(0, lastHeight, Screen.width, height);

                    Color curColor = GUI.color;

                    if (boxRect.Contains(e.mousePosition)) {
                        mouseOver = new IndexedFlag(true, i);

                        GUI.color = colorSellectionMouseOver;
                        cursorInsideSellectedArea = true;

                        if (e.type == EventType.MouseDown) {
                            if (e.button == 1) {
                                int tempI = i;
                                onShapeClick = new GenericMenu();

                                if (tempI > 0) {
                                    onShapeClick.AddItem(new GUIContent("Move Up"), false, () => {
                                        currentTarget.ShapeDecreaseValueID(tempI);
                                        mouseOver = IndexedFlag.nope;
                                        Repaint();
                                        SceneView.RepaintAll();
                                    });
                                }

                                if (tempI < allMods.arraySize - 1) {
                                    onShapeClick.AddItem(new GUIContent("Move Down"), false, () => {
                                        currentTarget.ShapeIncreaseValueID(tempI);
                                        mouseOver = IndexedFlag.nope;
                                        Repaint();
                                        SceneView.RepaintAll();
                                    });
                                }

                                onShapeClick.AddItem(new GUIContent("Copy"), false, () => {
                                    currentTarget.ShapeCopyAt(tempI);
                                    mouseOver = IndexedFlag.nope;
                                    Repaint();
                                    SceneView.RepaintAll();
                                });

                                onShapeClick.AddItem(new GUIContent("Remove"), false, () => {
                                    currentTarget.ShapeRemoveAt(tempI);
                                    mouseOver = IndexedFlag.nope;
                                    Repaint();
                                    SceneView.RepaintAll();
                                });

                                onShapeClick.ShowAsContext();
                            }
                        }
                    }

                    GUI.Box(boxRect, string.Empty);

                    GUI.color = curColor;

                    EditorGUILayout.PropertyField(arrayElement);
                    lastHeight += height;
                }

                if (cursorInsideSellectedArea) {
                    Repaint();
                    SceneView.RepaintAll();
                }
                if (cursorInsideSellectedArea != tempFlagCursorInsideSellectedArea) {
                    tempFlagCursorInsideSellectedArea = cursorInsideSellectedArea;
                    Repaint();
                    SceneView.RepaintAll();
                }
            }
            UITools.Line();


            drawGameObjectList = EditorGUILayout.Foldout(drawGameObjectList, new GUIContent("GameObject List"));

            //Rect lastRec = GUILayoutUtility.GetLastRect();

            if (drawGameObjectList) {
                if (GUILayout.Button("Update Childs")) {
                    currentTarget.RebuildChilds(false);
                }

                TextAnchor curBoxAnchor = GUI.skin.box.alignment;
                GUI.skin.box.alignment = TextAnchor.MiddleLeft;
                foreach (var item in currentTarget.childsAndBounds.Keys) {
                    GUILayout.Box(item.name, GUILayout.MinWidth(200));
                }
                GUI.skin.box.alignment = curBoxAnchor;

            }

            if (currentTarget.CheckDirty()) {
                Repaint();
                SceneView.RepaintAll();
            }

            if (serializedObject.ApplyModifiedProperties()) {
                currentTarget.DirtyAll();
            }
        }

        void GuiLine(int i_height = 1, bool startInstantly = true) {
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);

            if(startInstantly)
                rect = new Rect(0, rect.y, Screen.width, i_height);

            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        public void OnSceneGUI() {
            AreaWorldMod currentTarget = (target as AreaWorldMod);

            Quaternion targetRotation = currentTarget.rotation;
            Matrix4x4 targetMatrixLocalToWorld = currentTarget.localToWorldMatrix;
            Matrix4x4 targetMatrixWorldToLocal = currentTarget.worldToLocalMatrix;

            Color handlesColor = Handles.color;
            Color curColor = currentTarget.color;

            Handles.BeginGUI();
            Color color = GUI.color;
            GUI.color = Color.black;
            for (int i = 0; i < currentTarget.allMods.Count; i++) {
                var current = currentTarget.allMods[i];
                if (current.name != string.Empty)
                    Handles.Label(targetMatrixLocalToWorld.MultiplyPoint(current.position), current.name);
            }
            GUI.color = color;
            Handles.EndGUI();

            Vector3 vector;
            for (int i = 0; i < currentTarget.allMods.Count; i++) {
                var current = currentTarget.allMods[i];

                Matrix4x4 areaModMatrix = current.localToWorldMatrix;
                Matrix4x4 areaModMatrixInversed = areaModMatrix.inverse;

                switch (Tools.current) {
                    case Tool.Move:
                        EditorGUI.BeginChangeCheck();
                        if (Tools.pivotRotation == PivotRotation.Local)
                            vector = Handles.PositionHandle(targetMatrixLocalToWorld.MultiplyPoint(current.position), targetRotation * current.rotation);
                        else
                            vector = Handles.PositionHandle(targetMatrixLocalToWorld.MultiplyPoint(current.position), Quaternion.identity);
                        if (EditorGUI.EndChangeCheck()) {
                            current.position = targetMatrixWorldToLocal.MultiplyPoint(vector);
                            current.SetDirty();
                       
                            if (currentTarget.CheckDirty()) {
                                Repaint();
                                SceneView.RepaintAll();
                            }
                        }
                        break;
                    case Tool.Rotate:
                        EditorGUI.BeginChangeCheck();
                        Quaternion quat = Handles.RotationHandle(targetRotation * current.rotation, targetMatrixLocalToWorld.MultiplyPoint(current.position));
                        if (EditorGUI.EndChangeCheck()) {
                            current.rotation = Quaternion.Inverse(targetRotation) * quat;
                            current.SetDirty();

                            if (currentTarget.CheckDirty()) {
                                Repaint();
                                SceneView.RepaintAll();
                            }
                        }
                        break;
                }

                //if (mouseOver && mouseOver == i) Handles.color = ShiftColor(currentTarget.color);
                //else Handles.color = currentTarget.color;

                if (mouseOver && mouseOver == i) Handles.color = Color.blue;
                else Handles.color = Color.green;

                switch (current.myType) {
                    case AreaWorldModMagicValueType.Sphere:
                        #region sphere scene handles
                        if (editShape) {
                            EditorGUI.BeginChangeCheck();
                            float value = Handles.RadiusHandle(targetMatrixLocalToWorld.rotation * areaModMatrix.rotation, targetMatrixLocalToWorld.MultiplyPoint(current.position), current.radius);
                            if (EditorGUI.EndChangeCheck()) {
                                current.radius = value;
                                current.SetDirty();
                            }
                        }
                        #endregion
                        break;
                    case AreaWorldModMagicValueType.Capsule:
                        #region capsule scene handles
                        if (editShape) {
                            Vector3 discNormals = targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(0, 1, 0);
                            Vector3 capsulePos = targetMatrixLocalToWorld.MultiplyPoint(current.position);

                            Handles.DrawWireDisc(capsulePos, discNormals, current.radius);

                            Vector3 posLow = targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(0, (-current.height * 0.5f) + current.radius, 0);
                            Vector3 posHigh = targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(0, (current.height * 0.5f) - current.radius, 0);

                            Handles.DrawLine(capsulePos + posLow, capsulePos + posHigh);

                            Handles.DrawWireDisc(capsulePos + posLow, discNormals, current.radius);
                            Handles.DrawWireDisc(capsulePos + posHigh, discNormals, current.radius);
                            
                            float capSize = 0.025f;
                            float snap = 0.1f;
                     
                            //height+               
                            EditorGUI.BeginChangeCheck();
                            vector = Handles.Slider(
                                targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(0, current.height * 0.5f, 0))),
                                targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(0, 1, 0), capSize, Handles.DotHandleCap, snap);
                            vector = areaModMatrixInversed.MultiplyPoint(targetMatrixWorldToLocal.MultiplyPoint(vector));
                            if (EditorGUI.EndChangeCheck()) {
                                current.height = vector.y * 2;
                                current.SetDirty();
                            }

                            //height-
                            EditorGUI.BeginChangeCheck();
                            vector = Handles.Slider(
                                targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(0, current.height * -0.5f, 0))),
                                targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(0, 1, 0), capSize, Handles.DotHandleCap, snap);
                            vector = areaModMatrixInversed.MultiplyPoint(targetMatrixWorldToLocal.MultiplyPoint(vector));
                            if (EditorGUI.EndChangeCheck()) {
                                current.height = -vector.y * 2;
                                current.SetDirty();

                                if (currentTarget.CheckDirty()) {
                                    Repaint();
                                    SceneView.RepaintAll();
                                }
                            }

                            //radius
                            EditorGUI.BeginChangeCheck();
                            vector = Handles.Slider(
                                targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(current.radius, 0, 0))),
                                targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(1, 0, 0), capSize, Handles.DotHandleCap, snap);
                            vector = areaModMatrixInversed.MultiplyPoint(targetMatrixWorldToLocal.MultiplyPoint(vector));
                            if (EditorGUI.EndChangeCheck()) {
                                current.radius = vector.x;
                                current.SetDirty();

                                if (currentTarget.CheckDirty()) {
                                    Repaint();
                                    SceneView.RepaintAll();
                                }
                            }

                            //radius
                            EditorGUI.BeginChangeCheck();
                            vector = Handles.Slider(
                                targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(-current.radius, 0, 0))),
                                targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(-1, 0, 0), capSize, Handles.DotHandleCap, snap);
                            vector = areaModMatrixInversed.MultiplyPoint(targetMatrixWorldToLocal.MultiplyPoint(vector));
                            if (EditorGUI.EndChangeCheck()) {
                                current.radius = -vector.x;
                                current.SetDirty();

                                if (currentTarget.CheckDirty()) {
                                    Repaint();
                                    SceneView.RepaintAll();
                                }
                            }

                            //radius
                            EditorGUI.BeginChangeCheck();
                            vector = Handles.Slider(
                                targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(0, 0, current.radius))),
                                targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(0, 0, 1), capSize, Handles.DotHandleCap, snap);
                            vector = areaModMatrixInversed.MultiplyPoint(targetMatrixWorldToLocal.MultiplyPoint(vector));
                            if (EditorGUI.EndChangeCheck()) {
                                current.radius = vector.z;
                                current.SetDirty();

                                if (currentTarget.CheckDirty()) {
                                    Repaint();
                                    SceneView.RepaintAll();
                                }
                            }

                            //radius
                            EditorGUI.BeginChangeCheck();
                            vector = Handles.Slider(
                                targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(0, 0, -current.radius))),
                                targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(0, 0, -1), capSize, Handles.DotHandleCap, snap);
                            vector = areaModMatrixInversed.MultiplyPoint(targetMatrixWorldToLocal.MultiplyPoint(vector));
                            if (EditorGUI.EndChangeCheck()) {
                                current.radius = -vector.z;
                                current.SetDirty();

                                if (currentTarget.CheckDirty()) {
                                    Repaint();
                                    SceneView.RepaintAll();
                                }
                            }                     
                        }
                        #endregion
                        break;
                    case AreaWorldModMagicValueType.Cuboid:
                        #region cube scene handles
                        if (editShape) {
                            float capSize = 0.05f;
                            float snap = 0.1f;
                            Vector3 curSize = current.cubeSize;
                            //Vector3 curPos = current.position;

                            Vector3 min = -curSize;
                            Vector3 max = curSize;

                            //lines    
                            Vector3 A1 = targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(min.x, min.y, min.z)));
                            Vector3 B1 = targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(min.x, max.y, min.z)));
                            Vector3 C1 = targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(min.x, min.y, max.z)));
                            Vector3 D1 = targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(min.x, max.y, max.z)));

                            Vector3 A2 = targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(max.x, min.y, min.z)));
                            Vector3 B2 = targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(max.x, max.y, min.z)));
                            Vector3 C2 = targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(max.x, min.y, max.z)));
                            Vector3 D2 = targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(max.x, max.y, max.z)));

                            Handles.DrawLine(A1, B1);
                            Handles.DrawLine(D1, C1);
                            Handles.DrawLine(A1, C1);
                            Handles.DrawLine(D1, B1);

                            Handles.DrawLine(A2, B2);
                            Handles.DrawLine(D2, C2);
                            Handles.DrawLine(A2, C2);
                            Handles.DrawLine(D2, B2);

                            Handles.DrawLine(A2, A1);
                            Handles.DrawLine(B1, B2);
                            Handles.DrawLine(C1, C2);
                            Handles.DrawLine(D1, D2);
                            
                            //x+
                            EditorGUI.BeginChangeCheck();
                            vector = Handles.Slider(
                                targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(max.x, 0, 0))),
                                targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(1, 0, 0), capSize, Handles.DotHandleCap, snap);
                            vector = areaModMatrixInversed.MultiplyPoint(targetMatrixWorldToLocal.MultiplyPoint(vector));
                            if (EditorGUI.EndChangeCheck()) {
                                max.x = Mathf.Max(vector.x, AreaWorldModMagicValue.MIN_VALUE);

                                Vector3 worldMin = areaModMatrix.MultiplyPoint3x4(min);
                                Vector3 worldMax = areaModMatrix.MultiplyPoint3x4(max);
                          
                                current.position = (worldMin + worldMax) * 0.5f;
                                current.cubeSize = (max - min) * 0.5f;
                                current.SetDirty();

                                if (currentTarget.CheckDirty()) {
                                    Repaint();
                                    SceneView.RepaintAll();
                                }
                            }



                            //x-
                            EditorGUI.BeginChangeCheck();
                            vector = Handles.Slider(
                                targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(min.x, 0, 0))),
                                targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(-1, 0, 0), capSize, Handles.DotHandleCap, snap);
                            vector = areaModMatrixInversed.MultiplyPoint(targetMatrixWorldToLocal.MultiplyPoint(vector));
                            if (EditorGUI.EndChangeCheck()) {
                                min.x = Mathf.Min(vector.x, -AreaWorldModMagicValue.MIN_VALUE);

                                Vector3 worldMin = areaModMatrix.MultiplyPoint3x4(min);
                                Vector3 worldMax = areaModMatrix.MultiplyPoint3x4(max);

                                current.position = (worldMin + worldMax) * 0.5f;
                                current.cubeSize = (max - min) * 0.5f;
                                current.SetDirty();

                                if (currentTarget.CheckDirty()) {
                                    Repaint();
                                    SceneView.RepaintAll();
                                }
                            }


                            //y+
                            EditorGUI.BeginChangeCheck();
                            vector = Handles.Slider(
                                targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(0, max.y, 0))),
                                targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(0, 1, 0), capSize, Handles.DotHandleCap, snap);
                            vector = areaModMatrixInversed.MultiplyPoint(targetMatrixWorldToLocal.MultiplyPoint(vector));
                            if (EditorGUI.EndChangeCheck()) {
                                max.y = Mathf.Max(vector.y, AreaWorldModMagicValue.MIN_VALUE);

                                Vector3 worldMin = areaModMatrix.MultiplyPoint3x4(min);
                                Vector3 worldMax = areaModMatrix.MultiplyPoint3x4(max);

                                current.position = (worldMin + worldMax) * 0.5f;
                                current.cubeSize = (max - min) * 0.5f;
                                current.SetDirty();

                                if (currentTarget.CheckDirty()) {
                                    Repaint();
                                    SceneView.RepaintAll();
                                }
                            }



                            //y-
                            EditorGUI.BeginChangeCheck();
                            vector = Handles.Slider(
                                targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(0, min.y, 0))),
                                targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(0, -1, 0), capSize, Handles.DotHandleCap, snap);
                            vector = areaModMatrixInversed.MultiplyPoint(targetMatrixWorldToLocal.MultiplyPoint(vector));
                            if (EditorGUI.EndChangeCheck()) {
                                min.y = Mathf.Min(vector.y, -AreaWorldModMagicValue.MIN_VALUE);

                                Vector3 worldMin = areaModMatrix.MultiplyPoint3x4(min);
                                Vector3 worldMax = areaModMatrix.MultiplyPoint3x4(max);

                                current.position = (worldMin + worldMax) * 0.5f;
                                current.cubeSize = (max - min) * 0.5f;
                                current.SetDirty();

                                if (currentTarget.CheckDirty()) {
                                    Repaint();
                                    SceneView.RepaintAll();
                                }
                            }

                            //y+
                            EditorGUI.BeginChangeCheck();
                            vector = Handles.Slider(
                                targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(0, 0, max.z))),
                                targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(0, 0, 1), capSize, Handles.DotHandleCap, snap);
                            vector = areaModMatrixInversed.MultiplyPoint(targetMatrixWorldToLocal.MultiplyPoint(vector));
                            if (EditorGUI.EndChangeCheck()) {
                                max.z = Mathf.Max(vector.z, AreaWorldModMagicValue.MIN_VALUE);

                                Vector3 worldMin = areaModMatrix.MultiplyPoint3x4(min);
                                Vector3 worldMax = areaModMatrix.MultiplyPoint3x4(max);

                                current.position = (worldMin + worldMax) * 0.5f;
                                current.cubeSize = (max - min) * 0.5f;
                                current.SetDirty();

                                if (currentTarget.CheckDirty()) {
                                    Repaint();
                                    SceneView.RepaintAll();
                                }
                            }



                            //y-
                            EditorGUI.BeginChangeCheck();
                            vector = Handles.Slider(
                                targetMatrixLocalToWorld.MultiplyPoint(areaModMatrix.MultiplyPoint(new Vector3(0, 0, min.z))),
                                targetMatrixLocalToWorld.rotation * areaModMatrix.rotation * new Vector3(0, 0, -1), capSize, Handles.DotHandleCap, snap);
                            vector = areaModMatrixInversed.MultiplyPoint(targetMatrixWorldToLocal.MultiplyPoint(vector));
                            if (EditorGUI.EndChangeCheck()) {
                                min.z = Mathf.Min(vector.z, -AreaWorldModMagicValue.MIN_VALUE);

                                Vector3 worldMin = areaModMatrix.MultiplyPoint3x4(min);
                                Vector3 worldMax = areaModMatrix.MultiplyPoint3x4(max);

                                current.position = (worldMin + worldMax) * 0.5f;
                                current.cubeSize = (max - min) * 0.5f;
                                current.SetDirty();

                                if (currentTarget.CheckDirty()) {
                                    Repaint();
                                    SceneView.RepaintAll();
                                }
                            }
                        }
                        #endregion
                        break;                 
                }
            }
            Handles.color = handlesColor;

            //if (currentTarget.CheckDirty()) {
            //    Repaint();
            //    SceneView.RepaintAll();
            //}

            //if (GUI.changed)
            //    Repaint();
        }

        /// <summary>
        /// little toy that simplify storage of combination int and bool
        /// </summary>
        private struct IndexedFlag {
            public readonly int value;
            public readonly bool flag;

            public IndexedFlag(bool Flag, int Value) {
                flag = Flag;
                value = Value;
            }

            public static IndexedFlag nope {
                get { return new IndexedFlag(false, 0); }
            }

            public static implicit operator bool(IndexedFlag value) {
                return value.flag;
            }

            public static implicit operator int(IndexedFlag value) {
                return value.value;
            }
        }

        private static Color SetColorAlpha(Color color, float alpha) {
            return new Color(color.r, color.g, color.b, alpha);
        }

        static Color ShiftColor(Color color) {
            return new Color(color.g, color.b, color.r, color.a);
        }
    }
}