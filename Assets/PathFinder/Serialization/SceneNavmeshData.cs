using K_PathFinder;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace K_PathFinder.Serialization {
    public class SceneNavmeshData : ScriptableObject {
        [SerializeField]
        public List<AgentProperties> properties;
        [SerializeField]
        public List<SerializedNavmesh> navmesh;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SceneNavmeshData))]
    public class SceneNavmeshDataEditor : Editor {
        public override void OnInspectorGUI() {
            SceneNavmeshData myTarget = (SceneNavmeshData)target;

            for (int i = 0; i < myTarget.properties.Count; i++) {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Remove", GUILayout.MaxWidth(60))) {
                    myTarget.properties.RemoveAt(i);
                    myTarget.navmesh.RemoveAt(i);
                    break;
                }
                GUILayout.Label(myTarget.properties[i].name);
                GUILayout.EndHorizontal();
            }

            DrawDefaultInspector();
        }
    }
#endif
}

