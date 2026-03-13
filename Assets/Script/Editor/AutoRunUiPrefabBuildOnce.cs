using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AutoRunUiPrefabBuildOnce
{
    private const string PendingKey = "CreatureTower.AutoRunUiPrefabBuildOnce.Pending";

    static AutoRunUiPrefabBuildOnce()
    {
        EditorApplication.delayCall += TryRun;
    }

    [MenuItem("Tools/UI/Queue Build From PlayerHUD + Bind Once")]
    public static void QueueRunOnce()
    {
        EditorPrefs.SetBool(PendingKey, true);
        Debug.Log("[AutoRunUiPrefabBuildOnce] Queued: BuildFromPlayerHUD+Bind will run once after scripts reload/editor is ready.");
    }

    private static void TryRun()
    {
        if (!EditorPrefs.GetBool(PendingKey, false))
            return;

        EditorPrefs.SetBool(PendingKey, false);

        try
        {
            BuildGameplayUiPrefabsTool.BuildCurrentPlayerInfoUIFromPlayerHUD();
            BindAndSaveTargetScenes();
            Debug.Log("[AutoRunUiPrefabBuildOnce] Completed: BuildFromPlayerHUD+Bind(MainScene+MapMakeSetting).");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AutoRunUiPrefabBuildOnce] Failed: " + e.Message + "\n" + e.StackTrace);
        }
    }

    private static void BindAndSaveTargetScenes()
    {
        string[] scenePaths =
        {
            "Assets/Scenes/MainScene.unity",
            "Assets/Scenes/MapMakeSetting.unity"
        };

        string originalScenePath = EditorSceneManager.GetActiveScene().path;

        for (int i = 0; i < scenePaths.Length; i++)
        {
            string scenePath = scenePaths[i];
            if (string.IsNullOrEmpty(scenePath) || !System.IO.File.Exists(scenePath))
                continue;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            BuildGameplayUiPrefabsTool.BindPlayerUiManagerInOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        if (!string.IsNullOrEmpty(originalScenePath) && System.IO.File.Exists(originalScenePath))
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
    }
}
