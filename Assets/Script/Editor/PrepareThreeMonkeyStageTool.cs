using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrepareThreeMonkeyStageTool
{
    private const string BossPrefabPath = "Assets/Prefabs/Boss/3Monkey/3MonkeyBoss.prefab";
    private const string EyePrefabPath = "Assets/Prefabs/Boss/3Monkey/Monkey1Eye.prefab";
    private const string EarPrefabPath = "Assets/Prefabs/Boss/3Monkey/Monkey2Mouth.prefab";

    [MenuItem("Tools/Stage/Prepare ThreeMonkey Boss In Current Scene")]
    public static void PrepareCurrentScene()
    {
        var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        var eyePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EyePrefabPath);
        var earPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EarPrefabPath);

        if (bossPrefab == null)
        {
            Debug.LogError($"[PrepareThreeMonkeyStageTool] Boss prefab not found: {BossPrefabPath}");
            return;
        }

        var stages = Object.FindObjectsOfType<Stage>(true);
        if (stages == null || stages.Length == 0)
        {
            Debug.LogWarning("[PrepareThreeMonkeyStageTool] No Stage component found in current scene.");
            return;
        }

        int prepared = 0;

        foreach (var stage in stages)
        {
            if (stage == null) continue;
            if (stage.BossSpawnPoint == null)
            {
                Debug.LogWarning($"[PrepareThreeMonkeyStageTool] Skip '{stage.name}': BossSpawnPoint is null.");
                continue;
            }

            GameObject bossObj = stage.bossOBJ;
            bool needCreate = bossObj == null || bossObj.GetComponent<ThreeMonkeyBoss>() == null;

            if (needCreate)
            {
                var created = PrefabUtility.InstantiatePrefab(bossPrefab) as GameObject;
                if (created == null)
                {
                    Debug.LogError($"[PrepareThreeMonkeyStageTool] Failed to instantiate boss prefab for '{stage.name}'.");
                    continue;
                }

                created.name = "3MonkeyBoss_Instance";
                created.transform.SetParent(stage.transform, true);
                bossObj = created;
            }

            bossObj.transform.position = stage.BossSpawnPoint.position;
            bossObj.SetActive(false);

            stage.bossOBJ = bossObj;
            stage.BossBase = bossObj.GetComponent<BossBase>();

            var monkeyBoss = bossObj.GetComponent<ThreeMonkeyBoss>();
            if (monkeyBoss != null)
            {
                if (eyePrefab != null)
                {
                    monkeyBoss.prefab1TowerEye = eyePrefab;
                    monkeyBoss.eyeDetachedPrefab = eyePrefab;
                }

                if (earPrefab != null)
                {
                    monkeyBoss.prefab2TowerMouse = earPrefab;
                    monkeyBoss.earDetachedPrefab = earPrefab;
                    monkeyBoss.mouthDetachedPrefab = earPrefab;
                }
            }

            EditorUtility.SetDirty(stage);
            EditorUtility.SetDirty(bossObj);
            if (stage.BossBase != null)
                EditorUtility.SetDirty(stage.BossBase);
            if (monkeyBoss != null)
                EditorUtility.SetDirty(monkeyBoss);

            prepared++;
        }

        if (prepared > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[PrepareThreeMonkeyStageTool] Prepared {prepared} Stage object(s) in scene '{EditorSceneManager.GetActiveScene().name}'.");
        }
        else
        {
            Debug.LogWarning("[PrepareThreeMonkeyStageTool] No Stage object was prepared.");
        }
    }
}
