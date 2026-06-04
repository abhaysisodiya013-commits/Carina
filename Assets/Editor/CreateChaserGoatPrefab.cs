using UnityEditor;
using UnityEngine;

public class CreateChaserGoatPrefab
{
    [MenuItem("Tools/Create Chaser Goat Prefab")]
    public static void CreatePrefab()
    {
        string sourcePath = "Assets/CorgiEngine/Demos/SuperHipsterBros/Prefabs/AI/BadassDude.prefab";
        string targetDir = "Assets/Prefabs/Enemies";
        string targetPath = targetDir + "/ChaserGoat.prefab";

        if (!AssetDatabase.IsValidFolder(targetDir))
        {
            string[] folders = targetDir.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder(currentPath + "/" + folders[i]))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath += "/" + folders[i];
            }
        }

        bool success = AssetDatabase.CopyAsset(sourcePath, targetPath);
        if (success)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
            if (prefab != null)
            {
                // Unpack the prefab and save as a new one to break the connection to the demo prefab
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "ChaserGoat";
                PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
                GameObject.DestroyImmediate(instance);

                Debug.Log("Successfully created ChaserGoat prefab at " + targetPath);
            }
        }
        else
        {
            Debug.LogError("Failed to copy prefab from " + sourcePath);
        }
    }
}