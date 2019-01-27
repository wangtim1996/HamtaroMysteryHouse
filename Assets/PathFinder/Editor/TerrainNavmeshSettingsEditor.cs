using UnityEngine;
using UnityEditor;
using System.Collections;
using System;


namespace K_PathFinder {
    [CustomEditor(typeof(TerrainNavmeshSettings))]
    public class TerrainNavmeshSettingsEditor : Editor {
        [SerializeField]
        public float guiSplatSize = 20f;
        public override void OnInspectorGUI() {
            TerrainNavmeshSettings myTarget = (TerrainNavmeshSettings)target;
            if (myTarget == null)
                return;

            guiSplatSize = EditorGUILayout.Slider(guiSplatSize, 20f, 70f);

            TerrainData targetTerrainData = myTarget.terrain.terrainData;
            SplatPrototype[] prototypes = targetTerrainData.splatPrototypes;
            int[] areas = myTarget.data;

            if (areas.Length != prototypes.Length) {
                Debug.Log("Fixing terrain data length");

                int[] newAreas = new int[prototypes.Length];

                for (int i = 0; i < prototypes.Length; i++) {
                    if (i < myTarget.data.Length) {
                        newAreas[i] = myTarget.data[i];
                    }
                }

                Undo.RecordObject(target, "Fix terrain data length");
                myTarget.data = newAreas;
            }

            for (int i = 0; i < prototypes.Length; i++) {
                GUILayout.BeginHorizontal();
                GUILayout.Label(AssetPreview.GetAssetPreview(prototypes[i].texture), GUILayout.MaxHeight(guiSplatSize), GUILayout.MaxWidth(guiSplatSize));
                int value = myTarget.data[i];

                EditorGUI.BeginChangeCheck();
                value = PathFinder.DrawAreaSellector(value);

                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(target, "Chenge area");
                    myTarget.data[i] = value;
                }     
                GUILayout.EndHorizontal();
            }
        }
    }
}
