using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
namespace K_PathFinder {
        //[ExecuteInEditMode()]
        public class CellTester : MonoBehaviour {
        public AgentProperties properties;
        public void Do() {
            RaycastHit raycastHit;
            if (!Physics.Raycast(transform.position, Vector3.down, out raycastHit, 10))
                return;
            PathFinder.CellTester(raycastHit.point, properties);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CellTester))]
    public class CellTesterEditor : Editor {
        public override void OnInspectorGUI() {
            CellTester myTarget = (CellTester)target;
            DrawDefaultInspector();
            if (GUILayout.Button("Do"))
                myTarget.Do();
        }
    }
#endif
}