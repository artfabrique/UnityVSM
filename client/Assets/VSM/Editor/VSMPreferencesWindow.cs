using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Revenga.VSM
{

    public class VSMPreferencesWindow : EditorWindow
    {
        private string RssFolder;
        private string TrashFolder;
        private string NewRssFolder;
        private string NewTrashFolder;

        private string DefaultRssFolder = "Assets/Resources/VSM";
        private string DefaultTrashFolder = "Assets/VSM/Deleted";


        [MenuItem("Window/VSM Preferences...", false, 300)]
        static public void Init()
        {
            // Get existing open window or if none, make a new one:
            VSMPreferencesWindow window = (VSMPreferencesWindow)EditorWindow.GetWindow(typeof(VSMPreferencesWindow),false, "VSM Setup");
            window.Show();
        }

        private void OnEnable()
        {
            RssFolder = (EditorPrefs.HasKey("VSMCONFIG_RssFolder"))
                ? EditorPrefs.GetString("VSMCONFIG_RssFolder")
                : DefaultRssFolder;
            TrashFolder = (EditorPrefs.HasKey("VSMCONFIG_TrashFolder"))
                ? EditorPrefs.GetString("VSMCONFIG_TrashFolder")
                : DefaultTrashFolder;
            NewRssFolder = RssFolder;
            NewTrashFolder = TrashFolder;
        }

        private void OnGUI()
        {
            GUILayout.Space(10);


            GUILayout.Label("Where to save all data? This should be inside 'Assets/Resources' folder", new GUIStyle(GUI.skin.label) { wordWrap = true});
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            NewRssFolder = EditorGUILayout.TextField(NewRssFolder);
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Browse..."))
            {
                var tmpPath = Browse();
                if (!string.IsNullOrEmpty(tmpPath))
                {
                    if (tmpPath.StartsWith(Application.dataPath))
                    {
                        tmpPath = "Assets" + tmpPath.Substring(Application.dataPath.Length);
                    }
                    NewRssFolder = string.IsNullOrEmpty(tmpPath) ? NewRssFolder : tmpPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUILayout.Label("Where to save deleted state managers?", new GUIStyle(GUI.skin.label) {wordWrap = true});
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            NewTrashFolder = EditorGUILayout.TextField(NewTrashFolder);
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Browse..."))
            {
                var tmpPath = Browse();
                if (!string.IsNullOrEmpty(tmpPath))
                {
                    if (tmpPath.StartsWith(Application.dataPath))
                    {
                        tmpPath = "Assets" + tmpPath.Substring(Application.dataPath.Length);
                    }
                    NewTrashFolder = string.IsNullOrEmpty(tmpPath) ? NewTrashFolder : tmpPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            
            if (GUILayout.Button("Save")) Save();
        }

        private void Save()
        {
            EditorPrefs.SetString("VSMCONFIG_RssFolder", NewRssFolder);
            EditorPrefs.SetString("VSMCONFIG_TrashFolder", NewTrashFolder);
        }

        private string Browse()
        {
            var path = EditorUtility.OpenFolderPanel("Select folder...", "Assets/", "Resouces");
            return path;
        }
    }
}