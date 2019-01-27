using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using K_PathFinder.Settings;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace K_PathFinder {
    //this class describes agent parameters
    [System.Serializable]
    public class AgentProperties : ScriptableObject {
        public float radius = 0.5f;       //agent radius
        public float height = 2f;         //agent height   
        [Range(0f, 89.9f)]
        public float maxSlope = 45f;      //maximum slope in degrees
        public float maxStepHeight = 0.4f;//maximum step height in world units

        public int voxelsPerChunk = 100;  //amount of voxels per chunk side. describe resolution of navmesh
        public LayerMask includedLayers;  //layers that included into navmesh generation
        [Tag(false)]
        public List<string> ignoredTags;  //tags that ignored in navmesh generation. recoment to put here stuff like "item" or "player" or whatever
        public bool checkHierarchyTag = false;
        public bool doNavMesh = true;     //if you want to gent navmesh at all. cause pathfinder generate not just navmesh

        //jumps
        public bool canJump = false;      //if true then generate jump spots
        public float JumpDown = 1f;       //max jump down distance in world units
        public float JumpUp = 1f;         //max jump up distance in world units

        //crouch
        public bool canCrouch = false;    //if true then additional area will be generated for crouching
        public float crouchHeight = 1f;   //agent height when it crouch

        //cover
        public bool canCover = false;     //if true then cover map will be generated
        public bool canHalfCover = true;  //if true then half covers will be generated
        public float fullCover = 2f;      //height of full cover in world units
        public float halfCover = 1f;      //height of half cover in world units
        public int coverExtraSamples = 0; //if agent to thin then this will fix cover fragmentation

        //grid
        public bool battleGrid = false;
        public int battleGridDensity = 5;

        //cost mods
        public float walkMod = 1f;        //base move cost. if it 1f then cove cost equal to move distance
        public float crouchMod = 3f;      //mase crouch cost. area where agent crouching will cost this much to avoid this areas
        public float jumpUpMod = 10f;     //jump up cost
        public float jumpDownMod = 5f;    //jump down cost  
        [Range(0f, 2f)]
        public float offsetMultiplier = 1f;//value to change navmesh offset

#if UNITY_EDITOR
        [MenuItem(PathFinderSettings.UNITY_TOP_MENU_FOLDER + "/Create Agent Properties", false, 1)]
        public static void Create() {
            string path = EditorUtility.SaveFilePanel("Create Agent Properties",
                                                      "Assets/" + PathFinderSettings.PROJECT_FOLDER + "/" + PathFinderSettings.PROPERTIES_FOLDER,
                                                      "AgentName Properties.asset",
                                                      "asset");
            if (path == "")
                return;

            path = FileUtil.GetProjectRelativePath(path);

            AgentProperties ap = CreateInstance<AgentProperties>();
            ap.includedLayers.value = 1;

            AssetDatabase.CreateAsset(ap, path);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}



