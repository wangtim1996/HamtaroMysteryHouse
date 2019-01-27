#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using System;
using K_PathFinder.Settings;

namespace K_PathFinder.PFDebuger {
    public class PFDSettings : ScriptableObject {
        //[SerializeField]
        //public string sceneName = "_pathFinderHelper";
        [SerializeField]
        public bool autoUpdateSceneView = true;

        //options of chunks to debug
        [SerializeField]public List<Color> optionColors;
        [SerializeField]public List<bool> optionIsShows;

        //general
        [SerializeField]public bool doDebug;
        [SerializeField]public bool debugOnlyNavmesh;
        [SerializeField]public bool doProfilerStuff;
        [SerializeField]public bool showSelector;
        [SerializeField]public bool showSceneGUI;
        [SerializeField]public bool doDebugPaths;
        [SerializeField]public bool clearGenericOnUpdate;

        //flags to debug
        [SerializeField]public bool drawGenericDots;
        [SerializeField]public bool drawGenericLines;
        [SerializeField]public bool drawGenericLabels;
        [SerializeField]public bool drawGenericMesh;
        [SerializeField]public bool drawErrors;
        [SerializeField]public bool drawPaths;

        [SerializeField]public bool[] debugFlags;

        //RVO flags
        [SerializeField]public bool debugRVO;
        [SerializeField]public bool debugRVObasic;
        [SerializeField]public bool debugRVOvelocityObstacles;
        [SerializeField]public bool debugRVOconvexShape;
        [SerializeField]public bool debugRVOplaneIntersections;


        //shaders
        [SerializeField]public Shader dotShader;
        [SerializeField]public Shader lineShader;
        [SerializeField]public Shader trisShader;

        public static PFDSettings LoadSettings() {
            string path = string.Format("{0}/{1}/{2}/{3}.asset", new string[] {
                    PathFinderSettings.ASSETS_FOLDER,
                    PathFinderSettings.PROJECT_FOLDER,
                    PathFinderSettings.EDITOR_FOLDER,
                    PathFinderSettings.DEBUGER_ASSET_NAME });

            PFDSettings result = (PFDSettings)AssetDatabase.LoadAssetAtPath(path, typeof(PFDSettings));

            if (result == null) {
                result = CreateInstance<PFDSettings>();
                AssetDatabase.CreateAsset(result, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return result;
            //PFDSettings result = Resources.Load<PFDSettings>(PathFinderSettings.DEBUGER_ASSET_NAME);
            //if (result == null) {
            //    result = CreateInstance<PFDSettings>();
            //    AssetDatabase.CreateAsset(result, String.Format("{0}/{1}/{2}/{3}.asset", new string[] { "Assets", PathFinderSettings.PROJECT_FOLDER, PathFinderSettings.RESOURSES_FOLDER, PathFinderSettings.DEBUGER_ASSET_NAME }));
            //    AssetDatabase.SaveAssets();
            //    AssetDatabase.Refresh();
            //}
            //return result;
        }

        //was case when for some reason settings can loose links to shaders so now it eill be separated
        public static Shader GetDotShader() {
            return (Shader)AssetDatabase.LoadAssetAtPath(
                string.Format("{0}/{1}/{2}/{3}/{4}", new string[] {
                    PathFinderSettings.ASSETS_FOLDER,
                    PathFinderSettings.PROJECT_FOLDER,
                    PathFinderSettings.EDITOR_FOLDER,
                    PathFinderSettings.SHADERS_FOLDER,
                    "DotShader.shader"})
                , typeof(Shader));
        }
        public static Shader GetLineShader() {
            return (Shader)AssetDatabase.LoadAssetAtPath(
                string.Format("{0}/{1}/{2}/{3}/{4}", new string[] {
                    PathFinderSettings.ASSETS_FOLDER,
                    PathFinderSettings.PROJECT_FOLDER,
                    PathFinderSettings.EDITOR_FOLDER,
                    PathFinderSettings.SHADERS_FOLDER,
                    "LineShader.shader"})
                , typeof(Shader));
        }
        public static Shader GetTrisShader() {
            return (Shader)AssetDatabase.LoadAssetAtPath(
                string.Format("{0}/{1}/{2}/{3}/{4}", new string[] {
                    PathFinderSettings.ASSETS_FOLDER,
                    PathFinderSettings.PROJECT_FOLDER,
                    PathFinderSettings.EDITOR_FOLDER,
                    PathFinderSettings.SHADERS_FOLDER,
                    "TrisShader.shader"})
                , typeof(Shader));
        }

        [CustomEditor(typeof(PFDSettings))]
        public class PFDSettingsEditor : Editor {
            public override void OnInspectorGUI() {
                EditorGUILayout.LabelField("you probably should not edit this file in inspector at all exept materials", GUILayout.ExpandHeight(true));
                PFDSettings s = (PFDSettings)target;
                if (s == null)
                    return;

                s.dotShader = (Shader)EditorGUILayout.ObjectField("Dot shader", s.dotShader, typeof(Shader), false);
                s.lineShader = (Shader)EditorGUILayout.ObjectField("Line shader", s.lineShader, typeof(Shader), false);
                s.trisShader = (Shader)EditorGUILayout.ObjectField("Tris shader", s.trisShader, typeof(Shader), false);
            }
        }
    }



    [Serializable]
    public class PFD3Option {
        public bool showMe;
        public Color color;

        public PFD3Option() {
            this.showMe = false;
            this.color = Color.white;
        }
    }
}
#endif