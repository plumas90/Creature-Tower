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
 
    [MenuItem("Tools/Setup Melee Robot Sprites")]
    public static void SetupMeleeRobotSprites()
    {
        string prefabPath = "Assets/Prefabs/Enemy/1~5enemy/MeleeEnemy_Basic_robot.prefab";
        string spritePath = "Assets/sprite/monster/normal_low_enemy 1.png";
 
        // 1. 스프라이트들 로드 및 분류
        Object[] spriteObjects = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        System.Collections.Generic.List<Sprite> walkSprites = new System.Collections.Generic.List<Sprite>();
        Sprite idleSprite = null;
 
        foreach (var obj in spriteObjects)
        {
            if (obj is Sprite sprite)
            {
                if (sprite.name.StartsWith("normal_low_enemy_idle"))
                {
                    walkSprites.Add(sprite);
                    if (sprite.name == "normal_low_enemy_idle1")
                    {
                        idleSprite = sprite;
                    }
                }
            }
        }
 
        // 이름 순 정렬 (idle1 ~ idle6)
        walkSprites.Sort((a, b) => string.Compare(a.name, b.name));
 
        if (walkSprites.Count == 0 || idleSprite == null)
        {
            Debug.LogError($"Failed to load sprites for Melee Robot. WalkCount: {walkSprites.Count}, IdleFound: {idleSprite != null}");
            return;
        }
 
        // 2. 프리팹 로드 및 주입
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefab != null)
        {
            NormalMeleeEnemy enemy = prefab.GetComponent<NormalMeleeEnemy>();
            if (enemy != null)
            {
                var type = typeof(NormalMeleeEnemy);
                var walkField = type.GetField("walkSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var idleField = type.GetField("idleSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var rateField = type.GetField("walkFrameRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
 
                if (walkField != null && idleField != null && rateField != null)
                {
                    walkField.SetValue(enemy, walkSprites.ToArray());
                    idleField.SetValue(enemy, idleSprite);
                    rateField.SetValue(enemy, 0.2f); // 프레임 재생 간격 0.2초로 설정
 
                    // 스프라이트 렌더러 기본 스프라이트도 1번 스프라이트로 직접 갱신
                    SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = idleSprite;
                    }
 
                    PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                    Debug.Log($"Successfully setup walk & idle sprites and frame rate on prefab: {prefabPath}");
                }
                else
                {
                    Debug.LogError($"Failed to find fields in {type.Name} via reflection.");
                }
            }
            else
            {
                Debug.LogError($"NormalMeleeEnemy component not found in prefab: {prefabPath}");
            }
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        else
        {
            Debug.LogError($"Failed to load prefab contents: {prefabPath}");
        }
 
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Melee Robot sprite setup sequence finished.");
    }
 
    [MenuItem("Tools/Setup Ranged Robot Sprites")]
    public static void SetupRangedRobotSprites()
    {
        string prefabPath = "Assets/Prefabs/Enemy/1~5enemy/RangedWeaponEnemy_robot.prefab";
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
 
        // 이름 순 정렬 (walk1 ~ walk3)
        walkSprites.Sort((a, b) => string.Compare(a.name, b.name));
 
        if (walkSprites.Count == 0 || idleSprite == null)
        {
            Debug.LogError($"Failed to load sprites for Ranged Robot. WalkCount: {walkSprites.Count}, IdleFound: {idleSprite != null}");
            return;
        }
 
        // 2. 프리팹 로드 및 주입
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefab != null)
        {
            RangedWeaponEnemy enemy = prefab.GetComponent<RangedWeaponEnemy>();
            if (enemy != null)
            {
                var type = typeof(RangedWeaponEnemy);
                var walkField = type.GetField("walkSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var idleField = type.GetField("idleSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var rateField = type.GetField("walkFrameRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var scaleYField = type.GetField("weaponBaseScaleY", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
 
                if (walkField != null && idleField != null && rateField != null && scaleYField != null)
                {
                    walkField.SetValue(enemy, walkSprites.ToArray());
                    idleField.SetValue(enemy, idleSprite);
                    rateField.SetValue(enemy, 0.2f); // 프레임 재생 간격 0.2초로 설정
                    scaleYField.SetValue(enemy, 0.5f); // 기본 스케일 Y 크기 보정 0.5f 주입
 
                    // 스프라이트 렌더러 기본 스프라이트도 3번 스프라이트로 직접 갱신
                    SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = idleSprite;
                    }
 
                    PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                    Debug.Log($"Successfully setup walk & idle sprites and frame rate on prefab: {prefabPath}");
                }
                else
                {
                    Debug.LogError($"Failed to find fields in {type.Name} via reflection. walk: {walkField != null}, idle: {idleField != null}, rate: {rateField != null}, scaleY: {scaleYField != null}");
                }
            }
            else
            {
                Debug.LogError($"RangedWeaponEnemy component not found in prefab: {prefabPath}");
            }
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        else
        {
            Debug.LogError($"Failed to load prefab contents: {prefabPath}");
        }
 
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Ranged Robot sprite setup sequence finished.");
    }
 
    [MenuItem("Tools/Setup Robot Bullet Sprite")]
    public static void SetupRobotBulletSprite()
    {
        string prefabPath = "Assets/Prefabs/Enemy/EnemyBullet_Robot.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefab != null)
        {
            Bullet bullet = prefab.GetComponent<Bullet>();
            if (bullet != null)
            {
                var type = typeof(Bullet);
                var offsetField = type.GetField("spriteAngleOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var autoFlipField = type.GetField("autoFlipY", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (offsetField != null && autoFlipField != null)
                {
                    offsetField.SetValue(bullet, 45f); // 우측 하단(-45도) 조준을 우측(0도)으로 맞추기 위해 +45도 설정
                    autoFlipField.SetValue(bullet, false); // 회전이 들어간 못 스프라이트이므로 자동 FlipY 비활성화
                    PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                    Debug.Log($"Successfully setup sprite angle offset to 45 and autoFlipY to false on bullet prefab: {prefabPath}");
                }
                else
                {
                    Debug.LogError($"Failed to find fields via reflection. offsetField: {offsetField != null}, autoFlipField: {autoFlipField != null}");
                }
            }
            else
            {
                Debug.LogError("Bullet component not found on prefab.");
            }
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        else
        {
            Debug.LogError("Failed to load bullet prefab contents.");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
