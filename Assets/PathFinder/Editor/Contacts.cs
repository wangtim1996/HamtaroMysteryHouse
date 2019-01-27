using K_PathFinder.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace K_PathFinder.Instruction {
    public class Contacts {
        [MenuItem(PathFinderSettings.UNITY_TOP_MENU_FOLDER + "/Contacts/Email", false, 100)]
        public static void Mail() {
            EditorUtility.DisplayDialog("", "krokozor@gmail.com", "OK");
        }
        [MenuItem(PathFinderSettings.UNITY_TOP_MENU_FOLDER + "/Contacts/Skype", false, 100)]
        public static void Skype() {
            EditorUtility.DisplayDialog("", "kpokepd1", "OK");
        }
        [MenuItem(PathFinderSettings.UNITY_TOP_MENU_FOLDER + "/Contacts/Forum", false, 100)]
        public static void Forum() {
            Application.OpenURL("https://forum.unity3d.com/threads/447929/");
        }
        [MenuItem(PathFinderSettings.UNITY_TOP_MENU_FOLDER + "/Contacts/YouTube", false, 100)]
        public static void YouTube() {
            Application.OpenURL("https://www.youtube.com/channel/UCD7lytK5bnTTc72cPbRqpgQ");
        }
        [MenuItem(PathFinderSettings.UNITY_TOP_MENU_FOLDER + "/Contacts/Patreon", false, 100)]
        public static void Patreon() {
            Application.OpenURL("https://www.patreon.com/Krokozor");
        }
    }
}