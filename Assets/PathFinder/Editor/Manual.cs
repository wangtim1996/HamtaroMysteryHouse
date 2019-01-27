using K_PathFinder.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace K_PathFinder.Instruction {
    public class Manual : EditorWindow {
        ManualData data;
        int sellected = 0;
        string[] s = new string[]{"General", "Agent and Properties", "Results", "Main menu", "Local Avoidance", "Features and Limitations"};
        Vector2 generalScroll, agentAndPropertiesScroll, pathScroll, settingsScroll, dynamicObstaclesScroll, featuresAndLimitationsScroll;
                   
        void OnEnable() {
            data = ManualData.LoadData();
        }

        [MenuItem(PathFinderSettings.UNITY_TOP_MENU_FOLDER + "/Info/Manual", false, 5)]
        public static void OpenWindow() {
            GetWindow<Manual>("PF Manual").Show();
        }
        
        void OnGUI() {
            GUISkin curSkin = GUI.skin;
            GUI.skin = data.skin;

            sellected = GUILayout.SelectionGrid(sellected, s, 6);
 
            switch (sellected) {
                case 0:
                    GeneralManual();
                    break;
                case 1:
                    AgentAndProperties();
                    break;
                case 2:
                    PathStuff();
                    break;
                case 3:
                    MainMenu();
                    break;
                case 4:
                    DynamicObstacles();
                    break;
                case 5:
                    FeaturesAndLimitations();
                    break;
                default:
                    break;
            }
            GUI.skin = curSkin;
        }

        void GeneralManual() {
            generalScroll = EditorGUILayout.BeginScrollView(generalScroll);
            GUILayout.Label(data.generalTexts[0].text);
            GUILayout.Label(data.generalPics[0], GUILayout.ExpandWidth(false));

            GUILayout.Label(data.generalTexts[1].text);
            GUILayout.Label(data.generalPics[1], GUILayout.ExpandWidth(false));

            GUILayout.Label(data.generalTexts[2].text);
            GUILayout.Label(data.generalPics[2], GUILayout.ExpandWidth(false));

            GUILayout.Label(data.generalTexts[3].text);
            GUILayout.Label(data.generalPics[3], GUILayout.ExpandWidth(false));

            GUILayout.Label(data.generalTexts[4].text);
            EditorGUILayout.EndScrollView();
        }

        void AgentAndProperties() {
            agentAndPropertiesScroll = EditorGUILayout.BeginScrollView(agentAndPropertiesScroll);
            GUILayout.Label(data.agentAndPropertiesTexts[0].text);
            GUILayout.Label(data.agentAndPropertiesPics[0], GUILayout.ExpandWidth(false));

            GUILayout.Label(data.agentAndPropertiesTexts[1].text);
            EditorGUILayout.EndScrollView();
        }

        void PathStuff() {
            pathScroll = EditorGUILayout.BeginScrollView(pathScroll);
            GUILayout.Label(data.resultTexts[0].text);

            GUILayout.BeginHorizontal();
            GUILayout.Label(data.resultPics[0], GUILayout.ExpandWidth(false));
            GUILayout.Label(data.resultTexts[1].text);
            GUILayout.EndHorizontal();

            GUILayout.Label(data.resultTexts[2].text);
            GUILayout.BeginHorizontal();
            GUILayout.Label(data.resultPics[1], GUILayout.ExpandWidth(false));
            GUILayout.Label(data.resultTexts[3].text);
            GUILayout.EndHorizontal();

            GUILayout.Label(data.resultTexts[4].text);
            GUILayout.Label(data.resultPics[2], GUILayout.ExpandWidth(false));
            GUILayout.Label(data.resultTexts[5].text);


            GUILayout.Label(data.resultTexts[6].text);
            GUILayout.Label(data.resultPics[3], GUILayout.ExpandWidth(false));
            GUILayout.Label(data.resultTexts[7].text);

            EditorGUILayout.EndScrollView();
        }

        void MainMenu() {
            settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll);
            GUILayout.Label(data.mainMenuTexts[0].text);
            GUILayout.Label(data.mainMenuPics[0], GUILayout.ExpandWidth(false));

            GUILayout.Label(data.mainMenuTexts[1].text);
            GUILayout.Label(data.mainMenuPics[1], GUILayout.ExpandWidth(false));

            GUILayout.Label(data.mainMenuTexts[2].text);
            GUILayout.Label(data.mainMenuPics[2], GUILayout.ExpandWidth(false));

            GUILayout.Label(data.mainMenuTexts[3].text);
            GUILayout.Label(data.mainMenuPics[3], GUILayout.ExpandWidth(false));

            GUILayout.Label(data.mainMenuTexts[4].text);
            GUILayout.Label(data.mainMenuPics[4], GUILayout.ExpandWidth(false));

            GUILayout.Label(data.mainMenuTexts[5].text);
            EditorGUILayout.EndScrollView();
        }

        void DynamicObstacles() {
            settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll);
            GUILayout.Label(data.dynamicObstaclesTexts[0].text);
            GUILayout.Label(data.dynamicObstaclesPics[0], GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            GUILayout.Label(data.dynamicObstaclesTexts[1].text);
            EditorGUILayout.EndScrollView();
        }

        void FeaturesAndLimitations() {
            featuresAndLimitationsScroll = EditorGUILayout.BeginScrollView(featuresAndLimitationsScroll);

            GUILayout.Label(data.featuresTexts[0].text);
            GUILayout.Label(data.featuresPics[0], GUILayout.ExpandWidth(false));

            GUILayout.Label(data.featuresTexts[1].text);
            GUILayout.Label(data.featuresPics[1], GUILayout.ExpandWidth(false));
            EditorGUILayout.EndScrollView();
        }
    }
}