using UnityEngine;
using System.Collections;
using UnityEditor;

//namespace K_PathFinder {
//    [CustomEditor(typeof(AreaGameObject))]
//    public class GameObjectAreaEditor : Editor {
//        public override void OnInspectorGUI() {
//            AreaGameObject myTarget = (AreaGameObject)target;

//            if (myTarget == null)
//                return;

//            EditorGUI.BeginChangeCheck();

//            int value = myTarget.areaInt;
//            if (value > PathFinder.settings.areasMaxID)
//                value = 0;
//            value = PathFinder.DrawAreaSellector(value);

//            if (EditorGUI.EndChangeCheck()) {
//                Undo.RecordObject(target, "Chenge area");
//                myTarget.areaInt = value;
//            }
//        }
//    }
//}
