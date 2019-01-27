using K_PathFinder.Settings;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace K_PathFinder {
    public class About : EditorWindow {             
        [MenuItem(PathFinderSettings.UNITY_TOP_MENU_FOLDER + "/Info/About", false, 100)]
        public static void OpenAbout() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Copyright (c) Anton Chechulin, krokozor@gmail.com");
            sb.AppendLine();
            sb.AppendLine("You can change PathFinder");
            sb.AppendLine("You can distribute it only for free");
            sb.AppendLine("You can't distibute it after you change it");
            EditorUtility.DisplayDialog("About Krokozor PathFinder", sb.ToString(), "OK");
        }
    }
}