using UnityEngine;
using UnityEditor;
using System.IO;

public class MeleeSwingEnemySetupTool
{
    [MenuItem("Tools/Setup Melee Swing Enemy Sprites")]
    public static void SetupSprites()
    {
        string[] prefabPaths = new string[]
        {
            "Assets/Prefabs/Enemy/6~10enemy/MeleeSwingEnemy_Spear.prefab",
            "Assets/Prefabs/Enemy/6~10enemy/MeleeSwingEnemy_Swing.prefab"
        };

        string spritePath = "Assets/sprite/monster/normal_walk_enemy.png";
 
        // 1. 스프라이트들 로드 및 분류
        Object[] spriteObjects = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        System.Collections.Generic.List<Sprite> walkSprites = new System.Collections.Generic.List<Sprite>();
        Sprite idleSprite = null;
 
        foreach (var obj in spriteObjects)
        {
            if (obj is Sprite sprite)
            {
                if (sprite.name == "walk_robot_walk1" || 
                    sprite.name == "walk_robot_walk2" || 
                    sprite.name == "walk_robot_walk3")
                {
                    walkSprites.Add(sprite);
                    if (sprite.name == "walk_robot_walk3")
                    {
                        idleSprite = sprite;
                    }
                }
            }
        }
 
        // 이름 순 정렬 (walk_robot_walk1 ~ walk_robot_walk5)
        walkSprites.Sort((a, b) => string.Compare(a.name, b.name));
 
        if (walkSprites.Count == 0 || idleSprite == null)
        {
            Debug.LogError($"Failed to load sprites. WalkCount: {walkSprites.Count}, IdleFound: {idleSprite != null}");
            return;
        }

        // 3. 프리팹을 하나씩 열어 스프라이트 데이터 주입
        foreach (var path in prefabPaths)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            if (prefab != null)
            {
                MeleeSwingEnemy enemy = prefab.GetComponent<MeleeSwingEnemy>();
                if (enemy != null)
                {
                    // 리플렉션을 통해 private serialized field에 직접 주입
                    var type = typeof(MeleeSwingEnemy);
                    var walkField = type.GetField("walkSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var idleField = type.GetField("idleSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var rateField = type.GetField("walkFrameRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
 
                    if (walkField != null && idleField != null && rateField != null)
                    {
                        walkField.SetValue(enemy, walkSprites.ToArray());
                        idleField.SetValue(enemy, idleSprite);
                        rateField.SetValue(enemy, 0.2f); // 프레임 재생 간격 0.2초로 설정
                        
                        PrefabUtility.SaveAsPrefabAsset(prefab, path);
                        Debug.Log($"Successfully setup walk & idle sprites and frame rate on prefab: {path}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to find fields in {type.Name} via reflection. walkField: {walkField != null}, idleField: {idleField != null}, rateField: {rateField != null}");
                    }
                }
                else
                {
                    Debug.LogError($"MeleeSwingEnemy component not found in prefab: {path}");
                }
                PrefabUtility.UnloadPrefabContents(prefab);
            }
            else
            {
                Debug.LogError($"Failed to load prefab contents: {path}");
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Melee Swing Enemy sprite setup sequence finished.");
    }
}
