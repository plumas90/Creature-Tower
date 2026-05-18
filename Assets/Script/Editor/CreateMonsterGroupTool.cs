using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CreateMonsterGroupTool
{
    [MenuItem("Tools/Create Basic Monster Group")]
    public static void CreateBasicMonsterGroup()
    {
        // 1. Ensure target folder exists
        string folderPath = "Assets/SOData/Enemy";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/SOData", "Enemy");
        }

        // 2. Load Enemy Prefabs
        GameObject meleePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/MeleeEnemy_Basic.prefab");
        GameObject rangedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/RangedEnemy_Basic.prefab");

        if (meleePrefab == null || rangedPrefab == null)
        {
            Debug.LogError("[CreateMonsterGroupTool] Could not find basic enemy prefabs in 'Assets/Prefabs/Enemy/'. Make sure MeleeEnemy_Basic.prefab and RangedEnemy_Basic.prefab exist.");
            return;
        }

        // 3. Create or load the MonsterGroupSO asset
        string assetPath = $"{folderPath}/MonsterGroup_Basic_1_5.asset";
        MonsterGroupSO group = AssetDatabase.LoadAssetAtPath<MonsterGroupSO>(assetPath);
        bool isNew = false;

        if (group == null)
        {
            group = ScriptableObject.CreateInstance<MonsterGroupSO>();
            isNew = true;
        }

        // 4. Configure the MonsterGroupSO
        group.groupName = "Floors 1-5 Normal Basic Group (3 Melee, 2 Ranged)";
        group.targetFloorMin = 1;
        group.targetFloorMax = 5;
        group.waves.Clear();

        // Single Wave containing 3 Melee and 2 Ranged Enemies
        MonsterWaveInfo singleWave = new MonsterWaveInfo();
        singleWave.waveName = "Floor 1-5 Wave (3 Melee, 2 Ranged)";
        singleWave.delayBeforeWave = 1f;
        singleWave.spawnList = new List<MonsterSpawnData>
        {
            // 3 Melee Enemies
            new MonsterSpawnData { monsterPrefab = meleePrefab, spawnOffset = new Vector2(-2.5f, -1f) },
            new MonsterSpawnData { monsterPrefab = meleePrefab, spawnOffset = new Vector2(2.5f, -1f) },
            new MonsterSpawnData { monsterPrefab = meleePrefab, spawnOffset = new Vector2(0f, 0f) },
            // 2 Ranged Enemies
            new MonsterSpawnData { monsterPrefab = rangedPrefab, spawnOffset = new Vector2(-3.5f, 2f) },
            new MonsterSpawnData { monsterPrefab = rangedPrefab, spawnOffset = new Vector2(3.5f, 2f) }
        };

        group.waves.Add(singleWave);

        if (isNew)
        {
            AssetDatabase.CreateAsset(group, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(group);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[CreateMonsterGroupTool] Successfully created/updated MonsterGroupSO at {assetPath}");

        // 5. Update NormalStageBases prefab to include this group in availableMonsterGroups
        string stagePrefabPath = "Assets/Prefabs/Map/NormalStageBases.prefab";
        GameObject stagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(stagePrefabPath);
        if (stagePrefab == null)
        {
            Debug.LogError($"[CreateMonsterGroupTool] Could not load stage prefab at {stagePrefabPath}");
            return;
        }

        // Open prefab, edit component, save prefab
        GameObject prefabRoot = PrefabUtility.InstantiatePrefab(stagePrefab) as GameObject;
        if (prefabRoot != null)
        {
            NormalStage normalStage = prefabRoot.GetComponent<NormalStage>();
            if (normalStage != null)
            {
                // Access private availableMonsterGroups field using Reflection to avoid compiler errors
                var fieldInfo = typeof(NormalStage).GetField("availableMonsterGroups", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    List<MonsterGroupSO> list = fieldInfo.GetValue(normalStage) as List<MonsterGroupSO>;
                    if (list == null)
                    {
                        list = new List<MonsterGroupSO>();
                    }

                    // Clear previous groups to ensure only the new 1-wave group is used
                    list.Clear();
                    
                    // Add the new group
                    list.Add(group);
                    fieldInfo.SetValue(normalStage, list);
                    Debug.Log("[CreateMonsterGroupTool] Cleared old groups and added MonsterGroup_Basic_1_5 to availableMonsterGroups in NormalStageBases prefab.");
                }
                else
                {
                    Debug.LogError("[CreateMonsterGroupTool] Could not find field 'availableMonsterGroups' on NormalStage component.");
                }

                // Apply changes to the prefab
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, stagePrefabPath);
                Debug.Log($"[CreateMonsterGroupTool] Saved changes back to prefab: {stagePrefabPath}");
            }
            else
            {
                Debug.LogError("[CreateMonsterGroupTool] NormalStage component not found on prefab root.");
            }
            
            // Cleanup instantiated prefab
            GameObject.DestroyImmediate(prefabRoot);
        }
    }
}
