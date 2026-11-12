using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class UnusedAssetCleaner : EditorWindow
{
    private Vector2 scrollPos;
    
    // Tracks asset paths and whether the user has checked them to be ignored/kept
    private Dictionary<string, bool> scanResults = new Dictionary<string, bool>();
    private bool hasScanned = false;

    [MenuItem("Tools/Unused Asset Cleaner")]
    public static void ShowWindow()
    {
        GetWindow<UnusedAssetCleaner>("Unused Asset Cleaner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Scene & UI Asset Usage Cleaner", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Scans scenes, UXML layouts, USS stylesheets, and script fields. Use 'Ignore All' to protect everything, then uncheck individual assets to test deletion.", MessageType.Info);

        if (GUILayout.Button("Scan Project & Scenes", GUILayout.Height(40)))
        {
            ScanAssets();
        }

        if (hasScanned)
        {
            EditorGUILayout.Space();
            int totalUnused = scanResults.Count;
            int ignoredCount = scanResults.Values.Count(keep => keep);
            int pendingDeletion = totalUnused - ignoredCount;

            GUILayout.Label($"Unused Assets Found: {totalUnused} (Ignored: {ignoredCount}, To Delete: {pendingDeletion})", EditorStyles.boldLabel);

            // Master "Ignore All" toggle checkbox for safe testing
            EditorGUILayout.BeginHorizontal();
            bool allIgnored = scanResults.Values.All(v => v);
            bool newAllIgnored = EditorGUILayout.Toggle("Ignore All", allIgnored, GUILayout.Width(80));
            if (newAllIgnored != allIgnored)
            {
                var keys = scanResults.Keys.ToList();
                foreach (var k in keys)
                {
                    scanResults[k] = newAllIgnored;
                }
            }
            EditorGUILayout.LabelField("Check to protect all assets, then uncheck just one to test deletion.");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
            
            // Create a temporary list of keys to safely modify dictionary values in the loop
            var scrollKeys = scanResults.Keys.ToList();
            foreach (var assetPath in scrollKeys)
            {
                EditorGUILayout.BeginHorizontal();

                // Checkbox to ignore/keep this specific asset
                bool keep = EditorGUILayout.Toggle(scanResults[assetPath], GUILayout.Width(20));
                scanResults[assetPath] = keep;

                // Display asset path (dimmed out if ignored)
                GUI.enabled = !keep;
                EditorGUILayout.TextField(assetPath);
                GUI.enabled = true;

                // Select/Ping button in Project window
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button($"Delete Unchecked Unused Assets ({pendingDeletion})", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm Deletion", $"Are you sure you want to delete {pendingDeletion} unused assets? (Ignored assets will be skipped). Make sure you have committed your progress to Git!", "Yes, Delete", "Cancel"))
                {
                    DeleteUnusedAssets();
                }
            }
            GUI.backgroundColor = Color.white;
        }
    }

    private void ScanAssets()
    {
        scanResults.Clear();

        // 1. Gather all project assets
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture");
        string[] fontGuids = AssetDatabase.FindAssets("t:Font");

        HashSet<string> allProjectPrefabs = new HashSet<string>(prefabGuids.Select(g => AssetDatabase.GUIDToAssetPath(g)));
        HashSet<string> allProjectMaterials = new HashSet<string>(materialGuids.Select(g => AssetDatabase.GUIDToAssetPath(g)));
        HashSet<string> allProjectTextures = new HashSet<string>(textureGuids.Select(g => AssetDatabase.GUIDToAssetPath(g)));
        HashSet<string> allProjectFonts = new HashSet<string>(fontGuids.Select(g => AssetDatabase.GUIDToAssetPath(g)));

        HashSet<string> usedPrefabs = new HashSet<string>();
        HashSet<string> usedMaterials = new HashSet<string>();
        HashSet<string> usedTextures = new HashSet<string>();
        HashSet<string> usedFonts = new HashSet<string>();

        string originalScenePath = SceneManager.GetActiveScene().path;

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        if (scenes.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No scenes found in Build Settings! Add your 4 scenes first.", "OK");
            return;
        }

        // 2. Scan Scenes
        foreach (var scenePath in scenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    GameObject go = t.gameObject;

                    // Prefab check
                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        usedPrefabs.Add(prefabPath);
                        AddDependencies(prefabPath, usedMaterials, usedTextures, usedFonts, usedPrefabs);
                    }

                    // Renderer check
                    Renderer renderer = go.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        foreach (var mat in renderer.sharedMaterials)
                        {
                            if (mat != null)
                            {
                                string mPath = AssetDatabase.GetAssetPath(mat);
                                if (!string.IsNullOrEmpty(mPath))
                                {
                                    usedMaterials.Add(mPath);
                                    AddDependencies(mPath, usedMaterials, usedTextures, usedFonts, usedPrefabs);
                                }
                            }
                        }
                    }

                    // Component Inspector fields check
                    Component[] components = go.GetComponents<Component>();
                    foreach (var comp in components)
                    {
                        if (comp == null) continue;
                        try
                        {
                            SerializedObject serializedObj = new SerializedObject(comp);
                            SerializedProperty prop = serializedObj.GetIterator();
                            while (prop.NextVisible(true))
                            {
                                if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue != null)
                                {
                                    string assetPath = AssetDatabase.GetAssetPath(prop.objectReferenceValue);
                                    if (!string.IsNullOrEmpty(assetPath))
                                    {
                                        CategorizeAndAddAsset(assetPath, usedPrefabs, usedMaterials, usedTextures, usedFonts);
                                        AddDependencies(assetPath, usedMaterials, usedTextures, usedFonts, usedPrefabs);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Ignore serialization locks safely
                        }
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        // 3. Scan all UI Toolkit .uxml and .uss files textually
        string[] uxmlGuids = AssetDatabase.FindAssets("t:VisualTreeAsset");
        string[] ussGuids = AssetDatabase.FindAssets("t:StyleSheet");

        List<string> uiAssetFiles = new List<string>();
        foreach (var g in uxmlGuids) uiAssetFiles.Add(AssetDatabase.GUIDToAssetPath(g));
        foreach (var g in ussGuids) uiAssetFiles.Add(AssetDatabase.GUIDToAssetPath(g));

        foreach (var filePath in uiAssetFiles)
        {
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);

                foreach (var fontPath in allProjectFonts)
                {
                    if (fileContent.Contains(Path.GetFileName(fontPath)) || fileContent.Contains(fontPath))
                        usedFonts.Add(fontPath);
                }
                foreach (var texPath in allProjectTextures)
                {
                    if (fileContent.Contains(Path.GetFileName(texPath)) || fileContent.Contains(texPath))
                        usedTextures.Add(texPath);
                }
                foreach (var matPath in allProjectMaterials)
                {
                    if (fileContent.Contains(Path.GetFileName(matPath)) || fileContent.Contains(matPath))
                        usedMaterials.Add(matPath);
                }
            }
        }

        // 4. Populate unused dictionary (default value: false -> not ignored)
        foreach (var p in allProjectPrefabs)
            if (!usedPrefabs.Contains(p) && !p.Contains("Packages/")) scanResults[p] = false;
        foreach (var m in allProjectMaterials)
            if (!usedMaterials.Contains(m) && !m.Contains("Packages/")) scanResults[m] = false;
        foreach (var tx in allProjectTextures)
            if (!usedTextures.Contains(tx) && !tx.Contains("Packages/")) scanResults[tx] = false;
        foreach (var f in allProjectFonts)
            if (!usedFonts.Contains(f) && !f.Contains("Packages/")) scanResults[f] = false;

        hasScanned = true;
        Debug.Log($"[UnusedAssetCleaner] Scan complete! Found {scanResults.Count} unused assets.");
    }

    private void CategorizeAndAddAsset(string assetPath, HashSet<string> prefabs, HashSet<string> materials, HashSet<string> textures, HashSet<string> fonts)
    {
        if (assetPath.EndsWith(".prefab")) prefabs.Add(assetPath);
        else if (assetPath.EndsWith(".mat")) materials.Add(assetPath);
        else if (IsTexture(assetPath)) textures.Add(assetPath);
        else if (IsFont(assetPath)) fonts.Add(assetPath);
    }

    private void AddDependencies(string assetPath, HashSet<string> materials, HashSet<string> textures, HashSet<string> fonts, HashSet<string> prefabs)
    {
        string[] deps = AssetDatabase.GetDependencies(assetPath, true);
        foreach (var dep in deps)
        {
            CategorizeAndAddAsset(dep, prefabs, materials, textures, fonts);
        }
    }

    private bool IsTexture(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".psd" || ext == ".tif" || ext == ".tiff";
    }

    private bool IsFont(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext == ".ttf" || ext == ".otf";
    }

    private void DeleteUnusedAssets()
    {
        int deletedCount = 0;
        List<string> toRemove = new List<string>();
        List<string> deletedAssetPaths = new List<string>();

        foreach (var pair in scanResults)
        {
            // Only delete if the user did NOT check the ignore box
            if (!pair.Value)
            {
                if (AssetDatabase.DeleteAsset(pair.Key))
                {
                    deletedCount++;
                    toRemove.Add(pair.Key);
                    deletedAssetPaths.Add(pair.Key);
                }
            }
        }

        // Clean up deleted items from the list view
        foreach (var key in toRemove)
        {
            scanResults.Remove(key);
        }

        // Generate a text log file of deleted assets in the project root folder
        if (deletedAssetPaths.Count > 0)
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string logFileName = $"DeletedAssetsLog_{timestamp}.txt";
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string logFilePath = Path.Combine(projectRoot, logFileName);

            List<string> logLines = new List<string>
            {
                "# Unused Asset Cleaner - Deletion Log",
                $"# Generated on: {System.DateTime.Now}",
                $"# Total assets deleted: {deletedAssetPaths.Count}",
                "# Use this list to find paths if you need to restore or re-import anything via Git or backups.",
                ""
            };
            logLines.AddRange(deletedAssetPaths);

            File.WriteAllLines(logFilePath, logLines);
            Debug.Log($"[UnusedAssetCleaner] Deletion report text file generated at: {logFilePath}");
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Cleanup Complete", $"Successfully deleted {deletedCount} unused assets.\nA deletion log text file has been created in your project's root folder.", "OK");
        hasScanned = scanResults.Count > 0;
    }
}