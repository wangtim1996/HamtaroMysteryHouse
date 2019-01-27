using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace K_PathFinder {
    public static class MyHandles {
        private static Vector2 s_StartPlaneOffset;
        private static Vector2 s_StartMousePosition;
        private static Vector2 s_CurrentMousePosition;
        private static Vector3 s_StartPosition;
        private static float s_StartSliderValue;
        private static Vector2 s_StartSlider2DValue;
        private static int s_SliderHash = "SliderHash".GetHashCode();
        internal static int s_Slider2DHash = "Slider2DHash".GetHashCode();
        private static int s_AreaControlDataHash = "AreaControlData".GetHashCode();

        private static Vector3[] verts4 = new Vector3[4];

        public static AreaPointer DrawData(AreaPointer input, float size, float snap, out bool isDrag) {
            return DrawData(GUIUtility.GetControlID(s_AreaControlDataHash, FocusType.Keyboard), input, size, snap, out isDrag);
        }

        public static AreaPointer DrawData(int id, AreaPointer input, float handleSizeFactor, float snap, out bool isDrag) {
            isDrag = false;
            verts4[0] = new Vector3(input.startX, input.y, input.startZ);
            verts4[1] = new Vector3(input.startX, input.y, input.endZ);
            verts4[2] = new Vector3(input.endX, input.y, input.endZ);
            verts4[3] = new Vector3(input.endX, input.y, input.startZ);
            //SetAlpha(Handles.color, 0.1f)
            Handles.DrawSolidRectangleWithOutline(verts4, Color.clear, Handles.color);

            float y = input.y;
            float sizeX = input.endX - input.startX;
            float sizeZ = input.endZ - input.startZ;
            float sizeHalfX = sizeX * 0.5f;
            float sizeHalfZ = sizeZ * 0.5f;

            float resultStartX = input.startX;
            float resultStartZ = input.startZ;
            float resultEndX = input.endX;
            float resultEndZ = input.endZ;
            float resultY = input.y;

            float centerX = (resultStartX + resultEndX) * 0.5f;
            float centerZ = (resultStartZ + resultEndZ) * 0.5f;

            EventType handleEventType;
            float value;
            value = SliderFloat(new Vector3(centerX, resultY, centerZ), new Vector3(0, 1, 0), resultY, GetHandleSize(new Vector3(centerX, resultY, centerZ), handleSizeFactor * 15), Handles.ArrowHandleCap, 0.05f, out handleEventType);
            if (handleEventType == EventType.MouseDrag) {
                isDrag = true;
                resultY = value;
            }
            value = SliderFloat(new Vector3(centerX, resultY, centerZ), new Vector3(0, -1, 0), -resultY, GetHandleSize(new Vector3(centerX, resultY, centerZ), handleSizeFactor * 15), Handles.ArrowHandleCap, 0.05f, out handleEventType);
            if (handleEventType == EventType.MouseDrag) {
                isDrag = true;
                resultY = -value;
            }

            resultStartX = SliderFloat(new Vector3(input.startX, y, input.startZ + sizeHalfZ), new Vector3(1, 0, 0), resultStartX, GetHandleSize(new Vector3(input.startX, y, input.startZ + sizeHalfZ), handleSizeFactor), Handles.DotHandleCap, snap, out handleEventType);
            if (handleEventType == EventType.MouseDrag) {
                isDrag = true;
                if ((int)Mathf.Floor((resultStartX - resultEndX) / snap) > -1)
                    resultStartX = resultEndX - snap;
            }

            resultStartZ = SliderFloat(new Vector3(input.startX + sizeHalfX, y, input.startZ), new Vector3(0, 0, 1), resultStartZ, GetHandleSize(new Vector3(input.startX + sizeHalfX, y, input.startZ), handleSizeFactor), Handles.DotHandleCap, snap, out handleEventType);
            if (handleEventType == EventType.MouseDrag) {
                isDrag = true;
                if ((int)Mathf.Floor((resultStartZ - resultEndZ) / snap) > -1)
                    resultStartZ = resultEndZ - snap;
            }

            resultEndX = SliderFloat(new Vector3(input.endX, y, input.startZ + sizeHalfZ), new Vector3(1, 0, 0), resultEndX, GetHandleSize(new Vector3(input.endX, y, input.startZ + sizeHalfZ), handleSizeFactor), Handles.DotHandleCap, snap, out handleEventType);
            if (handleEventType == EventType.MouseDrag) {
                isDrag = true;
                if ((int)Mathf.Floor((resultEndX - resultStartX) / snap) < 1)
                    resultEndX = resultStartX + snap;
            }

            resultEndZ = SliderFloat(new Vector3(input.startX + sizeHalfX, y, input.endZ), new Vector3(0, 0, 1), resultEndZ, GetHandleSize(new Vector3(input.startX + sizeHalfX, y, input.endZ), handleSizeFactor), Handles.DotHandleCap, snap, out handleEventType);
            if (handleEventType == EventType.MouseDrag) {
                isDrag = true;
                if ((int)Mathf.Floor((resultEndZ - resultStartZ) / snap) < 1)
                    resultEndZ = resultStartZ + snap;
            }

            Vector2 vector2, delta;
            Vector2 snap2d = new Vector2(snap, snap);

            vector2 = Slider2D(new Vector2(resultStartX, resultStartZ), new Vector3(resultStartX, y, resultStartZ), Vector3.up, Vector3.right, Vector3.forward, GetHandleSize(new Vector3(resultStartX, y, resultStartZ), handleSizeFactor), Handles.DotHandleCap, snap2d, out handleEventType);
            resultStartX = vector2.x;
            resultStartZ = vector2.y;

            if (handleEventType == EventType.MouseDrag) {
                isDrag = true;
                if ((int)Mathf.Floor((resultStartX - resultEndX) / snap) > -1)
                    resultStartX = resultEndX - snap;
                if ((int)Mathf.Floor((resultStartZ - resultEndZ) / snap) > -1)
                    resultStartZ = resultEndZ - snap;
            }

            vector2 = Slider2D(new Vector2(resultEndX, resultEndZ), new Vector3(resultEndX, y, resultEndZ), Vector3.up, Vector3.right, Vector3.forward, GetHandleSize(new Vector3(resultEndX, y, resultEndZ), handleSizeFactor), Handles.DotHandleCap, snap2d, out handleEventType);
            resultEndX = vector2.x;
            resultEndZ = vector2.y;

            if (handleEventType == EventType.MouseDrag) {
                isDrag = true;
                if ((int)Mathf.Floor((resultEndX - resultStartX) / snap) < 1)
                    resultEndX = resultStartX + snap;
                if ((int)Mathf.Floor((resultEndZ - resultStartZ) / snap) < 1)
                    resultEndZ = resultStartZ + snap;
            }

            vector2 = Slider2D(new Vector2(resultStartX, resultEndZ), new Vector3(resultStartX, y, resultEndZ), Vector3.up, Vector3.right, Vector3.forward, GetHandleSize(new Vector3(resultStartX, y, resultEndZ), handleSizeFactor), Handles.DotHandleCap, snap2d, out handleEventType);
            resultStartX = vector2.x;
            resultEndZ = vector2.y;

            if (handleEventType == EventType.MouseDrag) {
                isDrag = true;
                if ((int)Mathf.Floor((resultStartX - resultEndX) / snap) > -1)
                    resultStartX = resultEndX - snap;
                if ((int)Mathf.Floor((resultEndZ - resultStartZ) / snap) < 1)
                    resultEndZ = resultStartZ + snap;
            }

            vector2 = Slider2D(new Vector2(resultEndX, resultStartZ), new Vector3(resultEndX, y, resultStartZ), Vector3.up, Vector3.right, Vector3.forward, GetHandleSize(new Vector3(resultEndX, y, resultStartZ), handleSizeFactor), Handles.DotHandleCap, snap2d, out handleEventType);
            resultEndX = vector2.x;
            resultStartZ = vector2.y;

            if (handleEventType == EventType.MouseDrag) {
                isDrag = true;
                if ((int)Mathf.Floor((resultEndX - resultStartX) / snap) < 1)
                    resultEndX = resultStartX + snap;
                if ((int)Mathf.Floor((resultStartZ - resultEndZ) / snap) > -1)
                    resultStartZ = resultEndZ - snap;
            }

            float quadSize = GetHandleSize(new Vector3(resultEndX, resultY, resultStartZ), handleSizeFactor * 7.5f);
            quadSize = Mathf.Min(sizeHalfX - 1f, quadSize);
            quadSize = Mathf.Min(sizeHalfZ - 1f, quadSize);

            vector2 = Slider2D(new Vector2(centerX, centerZ), new Vector3(centerX, y, centerZ), Vector3.up, Vector3.right, Vector3.forward, quadSize, Handles.RectangleHandleCap, snap2d);
            delta = new Vector2(vector2.x - centerX, vector2.y - centerZ);

            resultStartX += delta.x;
            resultStartZ += delta.y;
            resultEndX += delta.x;
            resultEndZ += delta.y;

            resultStartX = SnapValue(resultStartX, snap);
            resultStartZ = SnapValue(resultStartZ, snap);
            resultEndX = SnapValue(resultEndX, snap);
            resultEndZ = SnapValue(resultEndZ, snap);

            return new AreaPointer(resultStartX, resultStartZ, resultEndX, resultEndZ, resultY);
        }

        #region sliderFloat
        public static float SliderFloat(Vector3 position, Vector3 direction, float floatIn, float size, Handles.CapFunction drawFunc, float snap) {
            return SliderFloat(GUIUtility.GetControlID(s_SliderHash, FocusType.Keyboard), position, direction, floatIn, size, drawFunc, snap);
        }

        public static float SliderFloat(Vector3 position, Vector3 direction, float floatIn, float size, Handles.CapFunction drawFunc, float snap, out EventType handleEvent) {
            return SliderFloat(GUIUtility.GetControlID(s_SliderHash, FocusType.Keyboard), position, direction, floatIn, size, drawFunc, snap, out handleEvent);
        }

        internal static float SliderFloat(int id, Vector3 position, Vector3 handleDirection, float floatIn, float size, Handles.CapFunction drawFunc, float snap, out EventType handleEvent) {
            return DoFloat(id, position, handleDirection, handleDirection, floatIn, size, drawFunc, snap, out handleEvent);
        }

        internal static float SliderFloat(int id, Vector3 position, Vector3 handleDirection, float floatIn, float size, Handles.CapFunction drawFunc, float snap) {
            EventType handleEvent;
            return DoFloat(id, position, handleDirection, handleDirection, floatIn, size, drawFunc, snap, out handleEvent);
        }

        internal static float DoFloat(int id, Vector3 position, Vector3 handleDirection, Vector3 slideDirection, float floatIn, float size, Handles.CapFunction capFunction, float snap, out EventType handleEvent) {
            Event current = Event.current;
            float result = floatIn;
            handleEvent = current.GetTypeForControl(id);

            switch (handleEvent) {
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && current.button == 0 && GUIUtility.hotControl == 0) {
                        GUIUtility.hotControl = id;
                        s_CurrentMousePosition = s_StartMousePosition = current.mousePosition;
                        s_StartPosition = position;
                        s_StartSliderValue = floatIn;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        break;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (current.button == 0 || current.button == 2)) {
                        GUIUtility.hotControl = 0;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        break;
                    }
                    break;
                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl) {
                        HandleUtility.Repaint();
                        break;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id) {
                        s_CurrentMousePosition += current.delta;
                        result = s_StartSliderValue + SnapValue(HandleUtility.CalcLineTranslation(s_StartMousePosition, s_CurrentMousePosition, s_StartPosition, slideDirection), snap);
                        GUI.changed = true;
                        current.Use();
                        break;
                    }
                    break;
                case EventType.Repaint:
                    Color color = Color.white;
                    if (id == GUIUtility.hotControl) {
                        color = Handles.color;
                        Handles.color = Handles.selectedColor;
                    }
                    else if (id == HandleUtility.nearestControl && GUIUtility.hotControl == 0) {
                        color = Handles.color;
                        Handles.color = Handles.preselectionColor;
                    }
                    capFunction(id, position, Quaternion.LookRotation(handleDirection), size, EventType.Repaint);
                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0) {
                        Handles.color = color;
                        break;
                    }
                    break;
                case EventType.Layout:
                    if (capFunction != null) {
                        capFunction(id, position, Quaternion.LookRotation(handleDirection), size, EventType.Layout);
                        break;
                    }
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position, size * 0.2f));
                    break;
            }
            return result;
        }
        #endregion

        #region slider2D
        public static Vector2 Slider2D(Vector2 initialValue, Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, Handles.CapFunction drawFunc, Vector2 snap, bool drawHelper = false) {
            EventType handleEvent;
            return Slider2D(initialValue, GUIUtility.GetControlID(s_Slider2DHash, FocusType.Keyboard), handlePos, handleDir, slideDir1, slideDir2, handleSize, drawFunc, snap, out handleEvent, drawHelper);
        }

        public static Vector2 Slider2D(Vector2 initialValue, int id, Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, Handles.CapFunction drawFunc, Vector2 snap, bool drawHelper = false) {
            EventType handleEvent;
            return Slider2D(initialValue, id, handlePos, handleDir, slideDir1, slideDir2, handleSize, drawFunc, snap, out handleEvent, drawHelper);
        }

        public static Vector2 Slider2D(Vector2 initialValue, Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, Handles.CapFunction drawFunc, Vector2 snap, out EventType handleEvent, bool drawHelper = false) {
            return Slider2D(initialValue, GUIUtility.GetControlID(s_Slider2DHash, FocusType.Keyboard), handlePos, handleDir, slideDir1, slideDir2, handleSize, drawFunc, snap, out handleEvent, drawHelper);
        }

        public static Vector2 Slider2D(Vector2 initialValue, int id, Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, Handles.CapFunction drawFunc, Vector2 snap, out EventType handleEvent, bool drawHelper = false) {
            bool changed = GUI.changed;
            GUI.changed = false;
            Vector2 result = CalcDeltaAlongDirections(initialValue, id, handlePos, handleDir, slideDir1, slideDir2, handleSize, drawFunc, snap, drawHelper, out handleEvent);
            GUI.changed |= changed;
            return result;
        }


        private static Vector2 CalcDeltaAlongDirections(Vector2 initialValue, int id, Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, Handles.CapFunction capFunction, Vector2 snap, bool drawHelper, out EventType handleEvent) {
            Vector3 position = handlePos;
            Quaternion rotation = Quaternion.LookRotation(handleDir, slideDir1);
            Vector2 vector2 = initialValue;
            Event current = Event.current;

            handleEvent = current.GetTypeForControl(id);

            switch (handleEvent) {
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && current.button == 0 && GUIUtility.hotControl == 0) {
                        bool success = true;
                        Vector3 vector3 = Handles.inverseMatrix.MultiplyPoint(GetMousePosition(handleDir, handlePos, ref success));
                        if (success) {
                            GUIUtility.hotControl = id;
                            s_CurrentMousePosition = current.mousePosition;
                            s_StartPosition = handlePos;
                            s_StartSlider2DValue = initialValue;
                            Vector3 lhs = vector3 - handlePos;
                            s_StartPlaneOffset.x = Vector3.Dot(lhs, slideDir1);
                            s_StartPlaneOffset.y = Vector3.Dot(lhs, slideDir2);
                            current.Use();
                            EditorGUIUtility.SetWantsMouseJumping(1);
                        }
                        break;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (current.button == 0 || current.button == 2)) {
                        GUIUtility.hotControl = 0;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        break;
                    }
                    break;
                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl) {
                        HandleUtility.Repaint();
                        break;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id) {
                        s_CurrentMousePosition += current.delta;
                        bool success = true;
                        Vector3 point = Handles.inverseMatrix.MultiplyPoint(GetMousePosition(handleDir, handlePos, ref success));
                        if (success) {
                            vector2.x = HandleUtility.PointOnLineParameter(point, s_StartPosition, slideDir1);
                            vector2.y = HandleUtility.PointOnLineParameter(point, s_StartPosition, slideDir2);

                            vector2.x = SnapValue(vector2.x, snap.x);
                            vector2.y = SnapValue(vector2.y, snap.y);

                            vector2 += s_StartSlider2DValue;
                            GUI.changed = true;
                        }
                        current.Use();
                        break;
                    }
                    break;
                case EventType.Repaint:
                    if (capFunction != null) {
                        Color color1 = Color.white;
                        if (id == GUIUtility.hotControl) {
                            color1 = Handles.color;
                            Handles.color = Handles.selectedColor;
                        }
                        else if (id == HandleUtility.nearestControl && GUIUtility.hotControl == 0) {
                            color1 = Handles.color;
                            Handles.color = Handles.preselectionColor;
                        }
                        capFunction(id, position, rotation, handleSize, EventType.Repaint);
                        if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                            Handles.color = color1;
                        if (drawHelper && GUIUtility.hotControl == id) {
                            Vector3[] verts = new Vector3[4];
                            float num1 = handleSize * 10f;
                            verts[0] = position + slideDir1 * num1 + slideDir2 * num1;
                            verts[1] = verts[0] - slideDir1 * num1 * 2f;
                            verts[2] = verts[1] - slideDir2 * num1 * 2f;
                            verts[3] = verts[2] + slideDir1 * num1 * 2f;
                            Color color2 = Handles.color;
                            Handles.color = Color.white;
                            float num2 = 0.6f;
                            Handles.DrawSolidRectangleWithOutline(verts, new Color(1f, 1f, 1f, 0.05f), new Color(num2, num2, num2, 0.4f));
                            Handles.color = color2;
                            break;
                        }
                        break;
                    }
                    break;
                case EventType.Layout:
                    if (capFunction != null) {
                        capFunction(id, position, rotation, handleSize, EventType.Layout);
                        break;
                    }
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(handlePos, handleSize * 0.5f));
                    break;
            }
            return vector2;
        }

        #endregion

        private static Vector3 GetMousePosition(Vector3 handleDirection, Vector3 handlePosition, ref bool success) {
            if (Camera.current != null) {
                Plane plane = new Plane(Handles.matrix.MultiplyVector(handleDirection), Handles.matrix.MultiplyPoint(handlePosition));
                Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                float enter = 0.0f;
                success = plane.Raycast(worldRay, out enter);
                return worldRay.GetPoint(enter);
            }
            success = true;
            return Event.current.mousePosition;
        }

        public static float SnapValue(float val, float snap) {
            return Mathf.Round(val / snap) * snap;
        }



        public static float GetHandleSize(Vector3 position, float factor = 80f) {
            Camera current = Camera.current;
            position = Handles.matrix.MultiplyPoint(position);
            if (current == false)
                return 20f;
            Transform transform = current.transform;
            Vector3 position1 = transform.position;
            float z = Vector3.Dot(position - position1, transform.TransformDirection(new Vector3(0.0f, 0.0f, 1f)));
            return factor / Mathf.Max((current.WorldToScreenPoint(position1 + transform.TransformDirection(new Vector3(0.0f, 0.0f, z))) - current.WorldToScreenPoint(position1 + transform.TransformDirection(new Vector3(1f, 0.0f, z)))).magnitude, 0.0001f) * EditorGUIUtility.pixelsPerPoint;
        }

    }
}