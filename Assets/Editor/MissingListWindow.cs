//https://gist.github.com/SAM-tak/cde3db3c7f4fdba424e39d325c806fb8
//http://developer.wonderpla.net/entry/blog/engineer/Unity5_Search_MissingAssets/
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// Editor Extension : listing assets has missing link up
/// </summary>
public class MissingListWindow : EditorWindow
{
    class AssetParameterData
    {
        public UnityEngine.Object obj { get; set; } //!< Assets has missing link.
        public string path { get; set; }            //!< path of asset
        public string propertyName { get; set; }    //!< property name
        public string propertyType { get; set; }    //!< property type
    }

    private static List<AssetParameterData> missingList = new List<AssetParameterData>();
    private Vector2 scrollPos;

    /// <summary>
    /// Show window listing missing links.
    /// </summary>
    [MenuItem("Assets/Show Missing List")]
    private static void ShowMissingList()
    {
        missingList.Clear();

        Search();

        var window = GetWindow<MissingListWindow>();
        window.minSize = new Vector2(900, 300);
    }

    const string filter = "t:Scene t:Prefab t:Material t:AnimatorController t:Script t:Shader t:AvatarMask t:ScriptableObject";

    /// <summary>
    /// Search missing links by selected folders.
    /// </summary>
    private static void Search()
    {
        // make targer place for searching by selections.
        var folderPathes = new List<string>();
        var guids = new List<string>();
        foreach (var i in Selection.objects)
        {
            string selectionPath = AssetDatabase.GetAssetPath(i); // relative path
            if (Directory.Exists(selectionPath)) folderPathes.Add(selectionPath);
            else guids.Add(AssetDatabase.AssetPathToGUID(selectionPath));
        }

        if (folderPathes.Count > 0)
        {
            // add assets under selected folders
            guids.AddRange(AssetDatabase.FindAssets(filter, folderPathes.ToArray()));
            SearchMissing(guids);
        }
        else if (guids.Count == 0)
        {
            if (EditorUtility.DisplayDialog("Searches from all assets.", "Are you sure you want to search from all assets?", "Ok", "No"))
            {
                // search from all assets.
                SearchMissing(AssetDatabase.FindAssets(filter).ToList());
            }
        }
        else
        {
            SearchMissing(guids);
        }
    }

    private static void SearchMissing(List<string> guids)
    {
        for (int i = 0, imax = guids.Count; i < imax; ++i)
        {
            EditorUtility.DisplayProgressBar("Search Missing", string.Format("{0}/{1}", i + 1, imax), (float)(i + 1) / imax);

            SearchMissing(AssetDatabase.GUIDToAssetPath(guids[i]));
        }

        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// Search missing links on a specified asset.
    /// </summary>
    /// <param name="path">Path.</param>
    private static void SearchMissing(string path)
    {
        // get all assets (contains subassets)
        IEnumerable<UnityEngine.Object> assets = AssetDatabase.LoadAllAssetsAtPath(path);

        foreach (UnityEngine.Object obj in assets)
        {
            if (obj == null)
            {
                continue;
            }
            if (obj.name == "Deprecated EditorExtensionImpl")
            {
                continue;
            }

            SerializedObject sobj = new SerializedObject(obj);
            SerializedProperty property = sobj.GetIterator();

            do
            {
                // below condition means missing link.
                // if objectReferenceValue is null and objectReferenceInstanceIDValue equals 0, it means refer NONE. not missing.)
                if (property.propertyType == SerializedPropertyType.ObjectReference
                    && property.objectReferenceValue == null
                    && property.objectReferenceInstanceIDValue != 0)
                {

                    missingList.Add(new AssetParameterData()
                    {
                        obj = obj,
                        path = path,
                        propertyName = property.name,
                        propertyType = property.type
                    });
                }
            } while (property.Next(true));
        }
    }

    /// <summary>
    /// Display missing link list
    /// </summary>
    private void OnGUI()
    {
        // Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Asset", GUILayout.Width(200));
        EditorGUILayout.LabelField("Property", GUILayout.Width(200));
        EditorGUILayout.LabelField("Path");
        EditorGUILayout.EndHorizontal();

        // Missing List
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (AssetParameterData data in missingList)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(data.obj, data.obj.GetType(), true, GUILayout.Width(200));
            EditorGUILayout.TextField(data.propertyName + ":" + data.propertyType, GUILayout.Width(200));
            EditorGUILayout.TextField(data.path);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
}