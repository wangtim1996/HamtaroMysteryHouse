using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace K_PathFinder {
    public static class UITools {
        public static void Line(int i_height = 1, bool startInstantly = true) {
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);

            if (startInstantly)
                rect = new Rect(0, rect.y, Screen.width, i_height);

            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
    }
}