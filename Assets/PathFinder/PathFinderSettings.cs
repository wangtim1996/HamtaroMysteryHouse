using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditorInternal; //for checking tag associations. you probably wount change tags list on runtime (if ever can) so that check are excluded from builded projects
using UnityEditor;
#endif

namespace K_PathFinder.Settings {
    [Serializable]
    public class PathFinderSettings : ScriptableObject {
        public const bool DO_NON_CONVEX_COLLIDERS = true;

        public const string PROJECT_FOLDER = "PathFinder";
        public const string ASSETS_FOLDER = "Assets";
        public const string EDITOR_FOLDER = "Editor";
        public const string MANUAL_FOLDER = "Manual";
        public const string SHADERS_FOLDER = "Shaders";
        public const string UNITY_TOP_MENU_FOLDER = "Window/K-PathFinder";
        public const string RESOURSES_FOLDER = "Resources";
        public const string PROPERTIES_FOLDER = "Properties";
        public const string DEBUGER_FOLDER = "Debuger";
        public const string SETTINGS_ASSET_NAME = "PathfinderSettings";
        public const string DEBUGER_ASSET_NAME = "DebugerSettings";
        public const string MANUAL_ASSET_NAME = "ManualSettings";

        [SerializeField]public string helperName = "_pathFinderHelper";
        [SerializeField]public bool useMultithread = true;
        [SerializeField]public int maxThreads = 8;

        [SerializeField]public float gridSize = 10f;
        [SerializeField]public int gridLowest = -100;
        [SerializeField]public int gridHighest = 100;
        [SerializeField]public TerrainCollectorType terrainCollectionType = TerrainCollectorType.CPU;
        [SerializeField]public ColliderCollectorType colliderCollectionType = ColliderCollectorType.CPU;

        [SerializeField]public float terrainFastMinimalSize = 0.1f;

        [SerializeField]public bool drawAreaEditor;
        [SerializeField]public List<Area> areaLibrary;



        [SerializeField]private string lastLaunchedVersion;

        //properties to build
        [SerializeField]public AgentProperties targetProperties;

        //[SerializeField]public ComputeShader ComputeShaderRasterization3D;
        //[SerializeField]public ComputeShader ComputeShaderRasterization2D;

        public GUIContent[] areaNames;
        public int[] areaIDs;


        [SerializeField] public bool drawUnityAssociations = false;
        [SerializeField] public bool checkRootTag = false;

        [Serializable]
        struct TagAssociations {
            [SerializeField] public string tag;
            [SerializeField] public int area;
        }

        [SerializeField] List<TagAssociations> tagAssociationsSerialized = new List<TagAssociations>();
        public static Dictionary<string, Area> tagAssociations = new Dictionary<string, Area>();



        //UI serrings
        #region PathFinderMenu UI settings
#if UNITY_EDITOR
        //area to build
        [SerializeField] public AreaPointer areaPointer;
        [SerializeField] public bool removeAndRebuild = true;
        [SerializeField] public bool drawBuilder = true;

        public static bool isAreaPointerMoving = false;
#endif
        #endregion

        void OnEnable() {
            switch (lastLaunchedVersion) {
                default:
                    //kinda need to know last launched version but if i not use this value anywhere then unity will annoy with it
                    break;
            }
            lastLaunchedVersion = PathFinder.VERSION;
            ResetAreaPublicData();
            DeserializeTags();
        }

#if UNITY_EDITOR
        private void OnDestroy() {
            SerializeTags();
        }

        private void OnDisable() {
            SerializeTags();
        }
#endif

        public static PathFinderSettings LoadSettings() {
            PathFinderSettings result = Resources.Load<PathFinderSettings>(SETTINGS_ASSET_NAME);   
       
#if UNITY_EDITOR
            if (result == null) {
                result = CreateInstance<PathFinderSettings>();

                result.areaLibrary = new List<Area>();
                result.areaLibrary.Add(new Area("Default", 0, Color.green));
                result.areaLibrary.Add(new Area("Not Walkable", 1, Color.red) { cost = float.MaxValue });         

                AssetDatabase.CreateAsset(result, String.Format("{0}/{1}/{2}/{3}.asset", new string[] { "Assets", PROJECT_FOLDER, RESOURSES_FOLDER, SETTINGS_ASSET_NAME }));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#endif
            return result;
        }

        void ResetAreaPublicData() {
            areaNames = new GUIContent[areaLibrary.Count];
            areaIDs = new int[areaLibrary.Count];
            for (int i = 0; i < areaLibrary.Count; i++) {
                areaNames[i] = new GUIContent(areaLibrary[i].name);
                areaIDs[i] = areaLibrary[i].id;
            }
        }




        #region area manage
        public Area getDefaultArea {
            get { return areaLibrary[0]; }
        }        

        public int areasMaxID {
            get { return areaLibrary.Count - 1; }
        }

        public void AddArea() {
            Area newArea = new Area("Area " + areaLibrary.Count, areaLibrary.Count);
            areaLibrary.Add(newArea);
            PathFinder.AddAreaHash(newArea, true);
            ResetAreaPublicData();
        }

        public void RemoveArea(int id) {
            if (id == 0 | id == 1 | areaLibrary.Count - 1 < id)
                return;

            Area removedArea = areaLibrary[id];
            areaLibrary.RemoveAt(id);
            PathFinder.RemoveAreaHash(removedArea);

            for (int i = id; i < areaLibrary.Count; i++) {
                areaLibrary[i].id = i;
            }
          
            ResetAreaPublicData();
        }

#if UNITY_EDITOR
        private void SerializeTags() {
            tagAssociationsSerialized.Clear();
            foreach (var pair in tagAssociations) {
                if (pair.Value.id != -1)
                    tagAssociationsSerialized.Add(new TagAssociations() { tag = pair.Key, area = pair.Value.id });
            }
            //foreach (var item in tagAssociationsSerialized) {
            //    Debug.LogFormat("Serialized {0} : {1}", item.tag, item.area);
            //}

            EditorUtility.SetDirty(this);
        }
#endif

        private void DeserializeTags() {
            //foreach (var item in tagAssociationsSerialized) {
            //    Debug.LogFormat("Deserialize {0} : {1}", item.tag, item.area);
            //}

            foreach (var item in tagAssociationsSerialized) {
                tagAssociations[item.tag] = areaLibrary[item.area];
            }
#if UNITY_EDITOR
            CheckTagAssociations();
#endif
        }

#if UNITY_EDITOR
        public int DrawAreaSellector(int currentValue) {
            if (currentValue > areasMaxID)
                currentValue = 0;

            GUILayout.BeginHorizontal();
            GUILayout.Label(currentValue.ToString() + ":", GUILayout.MaxWidth(15));

            Color curColor = GUI.color;
            GUI.color = areaLibrary[currentValue].color;
            GUILayout.Box("", GUILayout.MaxWidth(15));
            GUI.color = curColor;
            currentValue = EditorGUILayout.IntPopup(currentValue, areaNames, areaIDs);
            GUILayout.EndHorizontal();
            return currentValue;
        }





#if UNITY_EDITOR
        public void CheckTagAssociations() {
            foreach (var tag in InternalEditorUtility.tags) {
                if (tagAssociations.ContainsKey(tag) == false)
                    tagAssociations.Add(tag, areaLibrary[0]);
            }

            List<string> strings = Pool.GenericPool<List<string>>.Take();

            foreach (var key in tagAssociations.Keys) {
                if (InternalEditorUtility.tags.Contains(key) == false)
                    strings.Add(key);
            }

            foreach (var item in strings) {
                tagAssociations.Remove(item);
            }

            Pool.GenericPool<List<string>>.ReturnToPool(ref strings);
        }
#endif


        [CustomEditor(typeof(PathFinderSettings))]
        public class PathFinderSettingsEditor : Editor {
            public override void OnInspectorGUI() {
                EditorGUILayout.LabelField("you probably should not edit this file in inspector");
                //PathFinderSettings s = (PathFinderSettings)target;
                //if (s == null)
                //    return;

                //EditorGUILayout.LabelField("some links to important files:");
                //s.ComputeShaderRasterization2D = (ComputeShader)EditorGUILayout.ObjectField("CS Rasterization 2D", s.ComputeShaderRasterization2D, typeof(ComputeShader), false);
                //s.ComputeShaderRasterization3D = (ComputeShader)EditorGUILayout.ObjectField("CS Rasterization 3D", s.ComputeShaderRasterization3D, typeof(ComputeShader), false);        
            }
        
        }
#endif
        #endregion
    }
}
