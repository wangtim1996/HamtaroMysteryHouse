using K_PathFinder.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace K_PathFinder {
    public class SettingsDrawer{
        PathFinderSettings settings;

        public SettingsDrawer(PathFinderSettings settings) {
            this.settings = settings;
            CheckPointer();
        }

        public void CheckPointer() {
            if (settings.areaPointer.sizeX < 1 | settings.areaPointer.sizeZ < 1) {
                ResetAreaPointer();
            }
        }
        public void ResetAreaPointer() {
            settings.areaPointer = new AreaPointer(0, 0, 1, 1, 0);
            EditorUtility.SetDirty(settings);
        }

        public void MovePointer(AreaPointer pointer) {
            settings.areaPointer = pointer;
            EditorUtility.SetDirty(settings);
        }

        public void DrawAreaEditor() {
            settings.drawAreaEditor = EditorGUILayout.Foldout(settings.drawAreaEditor, "Area Editor");

            if (!settings.drawAreaEditor)
                return;

            if (GUILayout.Button("Add Area")) {
                settings.AddArea();
            }

            for (int i = 0; i < settings.areaLibrary.Count; i++) {
                bool remove = false;
                Area area = settings.areaLibrary[i];

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(area.id.ToString(), GUILayout.MaxWidth(10f));
                if (area.id == 0 || area.id == 1)  //cant choose name to default areas to avoit confussion
                    EditorGUILayout.LabelField(new GUIContent(area.name, "area name"), GUILayout.MaxWidth(80));
                else
                    area.name = EditorGUILayout.TextField(area.name, GUILayout.MaxWidth(80));

                area.color = EditorGUILayout.ColorField(GUIContent.none, area.color, false, false, false, GUILayout.MaxWidth(12f));

                EditorGUILayout.LabelField(new GUIContent("Cost", "move cost"), GUILayout.MaxWidth(30));

                if (area.id == 1)
                    EditorGUILayout.LabelField("max", GUILayout.MaxWidth(30f));
                else
                    area.cost = EditorGUILayout.FloatField(area.cost, GUILayout.MaxWidth(30f));


                EditorGUILayout.LabelField(new GUIContent("Priority", "z-fighting cure. if one layer on another than this number matter"), GUILayout.MaxWidth(45f));

                if (area.id == 1)
                    EditorGUILayout.LabelField(new GUIContent("-1", "clear area it always should be -1"), GUILayout.MaxWidth(30f));
                else {
                    area.overridePriority = EditorGUILayout.IntField(area.overridePriority, GUILayout.MaxWidth(30f));
                    if (area.overridePriority < 0)
                        area.overridePriority = 0;
                }

                if (area.id != 0 && area.id != 1)
                    remove = GUILayout.Button("X", GUILayout.MaxWidth(18f));

                EditorGUILayout.EndHorizontal();

                if (remove) {
                    settings.RemoveArea(i);
                    break;
                }


            }
            UITools.Line();
            settings.drawUnityAssociations = EditorGUILayout.Foldout(settings.drawUnityAssociations, "Tag Associations");

            if (settings.drawUnityAssociations) {
                settings.checkRootTag = EditorGUILayout.ToggleLeft("Use Root Tag", settings.checkRootTag);

                settings.CheckTagAssociations();

                TextAnchor curAnchor = GUI.skin.box.alignment;
                GUI.skin.box.alignment = TextAnchor.MiddleLeft;
                Color curColor = GUI.color;
                foreach (var pair in PathFinderSettings.tagAssociations) {
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Box(pair.Key, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
                    GUI.color = settings.areaLibrary[pair.Value.id].color;
                    GUILayout.Box(string.Empty, GUILayout.MaxWidth(15));
                    GUI.color = curColor;

                    EditorGUI.BeginChangeCheck();
                    int tempValue = EditorGUILayout.IntPopup(
                        pair.Value.id, PathFinder.settings.areaNames, PathFinder.settings.areaIDs);
                    if (EditorGUI.EndChangeCheck()) {
                        PathFinderSettings.tagAssociations[pair.Key] = settings.areaLibrary[tempValue];
                    }
                    EditorGUILayout.EndHorizontal();
                }
                GUI.color = curColor;
                GUI.skin.box.alignment = curAnchor;
            }
        }

    }
}
