using K_PathFinder.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace K_PathFinder.Instruction {
    public class ManualData : ScriptableObject {
        public GUISkin skin;
        // General
        public TextAsset[] generalTexts;
        public Texture2D[] generalPics;

        // AgentAndProperties
        public TextAsset[] agentAndPropertiesTexts;
        public Texture2D[] agentAndPropertiesPics;

        // Result
        public TextAsset[] resultTexts;
        public Texture2D[] resultPics;

        // MainMenu
        public TextAsset[] mainMenuTexts;
        public Texture2D[] mainMenuPics;

        // DynamicObstacles
        public TextAsset[] dynamicObstaclesTexts;
        public Texture2D[] dynamicObstaclesPics;

        // Features
        public TextAsset[] featuresTexts;
        public Texture2D[] featuresPics;

        //public TextAsset general;
        //public TextAsset agentAndProperties1, agentAndProperties2, agentAndProperties3;
        //public Texture2D agentAndPropertiesPic1;

        //public TextAsset PathText1, PathText2, PathText3, PathText4;
        //public Texture2D PathPic1, PathPic2;

        //public TextAsset mainMenuText;

        //public TextAsset FeaturesText1, FeaturesText2, FeaturesText3;
        //public Texture2D FeaturesPic1, FeaturesPic2, FeaturesPic3;


        public static ManualData LoadData() {
            string path = string.Format("{0}/{1}/{2}/{3}.asset", new string[] {
                    PathFinderSettings.ASSETS_FOLDER,
                    PathFinderSettings.PROJECT_FOLDER,
                    PathFinderSettings.EDITOR_FOLDER,
                    PathFinderSettings.MANUAL_ASSET_NAME });

            ManualData result = (ManualData)AssetDatabase.LoadAssetAtPath(path, typeof(ManualData));

            if (result == null) {
                result = CreateInstance<ManualData>();
                AssetDatabase.CreateAsset(result, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return result;
        }
    }
}