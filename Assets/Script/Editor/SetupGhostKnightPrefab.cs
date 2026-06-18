using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class SetupGhostKnightPrefab
{
    static SetupGhostKnightPrefab()
    {
        EditorApplication.delayCall += DoSetup;
    }

    static void DoSetup()
    {
        if (EditorPrefs.GetBool("SetupGhostKnightPrefabDone_DecagonPattern", false))
            return;

        string bossPrefabPath = "Assets/Prefabs/Boss/GhostKnight/GhostKnight.prefab";
        string soDir = "Assets/SOData/Boss/GhostKnight";
        string soPath = "Assets/SOData/Boss/GhostKnight/GhostKnightSO.asset";

        // 1. Create SO Data Directory if not exists
        if (!Directory.Exists(soDir))
        {
            Directory.CreateDirectory(soDir);
            AssetDatabase.Refresh();
        }

        // 2. Create or Load SO
        GhostKnightSO so = AssetDatabase.LoadAssetAtPath<GhostKnightSO>(soPath);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<GhostKnightSO>();
            so.enemyName = "Ghost Knight";
            so.hp = 1200;
            so.atk = 15;
            so.speed = 3;
            so.bossCount = 1;
            so.IntroAnimationTime = 3f;
            AssetDatabase.CreateAsset(so, soPath);
        }

        // Set default values for sword pattern
        if (so.swordDamage == 0f) so.swordDamage = 15f;
        if (so.swordSwingPeriod == 0f || so.swordSwingPeriod == 2f) so.swordSwingPeriod = 3f;
        if (so.swordRotationSpeed == 0f || so.swordRotationSpeed == 1f) so.swordRotationSpeed = 360f;
        if (so.swordSwingCount == 0 || so.swordSwingCount == 2) so.swordSwingCount = 3;

        // Set default values for hexagon pattern
        if (so.hexagonPatternRadius == 0f) so.hexagonPatternRadius = 4f;
        if (so.hexagonSwordSpeed == 0f) so.hexagonSwordSpeed = 4f;
        if (so.hexagonSwordRotationSpeed == 0f) so.hexagonSwordRotationSpeed = 360f;
        if (so.hexagonSwordDamage == 0f) so.hexagonSwordDamage = 15f;
        if (so.hexagonPatternCount == 0) so.hexagonPatternCount = 4;
        if (so.hexagonWaveInterval == 0f) so.hexagonWaveInterval = 1.5f;
        if (so.hexagonSwordScale == 0f) so.hexagonSwordScale = 1.5f;
        so.hexagonVertexCount = 10;

        // 3. Assign Sprites to SO
        string sheetPath = "Assets/sprite/Boss/GhostKnight/ghost_knight_fix.png";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        System.Collections.Generic.List<Sprite> idleSpritesList = new System.Collections.Generic.List<Sprite>();
        foreach (Object asset in assets)
        {
            if (asset is Sprite s)
            {
                if (s.name.ToLower().Contains("idle"))
                    idleSpritesList.Add(s);
            }
        }

        idleSpritesList.Sort((a, b) => a.name.CompareTo(b.name));
        so.idleSprites = idleSpritesList.ToArray();

        EditorUtility.SetDirty(so);

        // 4. Edit Prefab
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bossPrefabPath);
        if (bossPrefab != null)
        {
            // Remove missing scripts
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(bossPrefab);

            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(bossPrefabPath))
            {
                var prefabRoot = editingScope.prefabContentsRoot;

                // Set layer and tag of root and all children recursively
                SetLayerAndTagRecursively(prefabRoot.transform, 9, "Enemy");

                // Get or Add GhostKnight component
                GhostKnight gk = prefabRoot.GetComponent<GhostKnight>();
                if (gk == null)
                {
                    gk = prefabRoot.AddComponent<GhostKnight>();
                }

                // Get or Add BoxCollider2D component
                BoxCollider2D col = prefabRoot.GetComponent<BoxCollider2D>();
                if (col == null)
                {
                    col = prefabRoot.AddComponent<BoxCollider2D>();
                }
                col.size = new Vector2(2f, 4.2f);
                col.offset = new Vector2(0f, 0f);
                col.isTrigger = false;

                // Assign SO and SpriteRenderer fields via SerializedObject
                SerializedObject serializedBoss = new SerializedObject(gk);
                SerializedProperty soProp = serializedBoss.FindProperty("ghostKnightSO");
                if (soProp != null)
                {
                    soProp.objectReferenceValue = so;
                }

                SerializedProperty brProp = serializedBoss.FindProperty("bodyRenderer");
                if (brProp != null)
                {
                    brProp.objectReferenceValue = prefabRoot.GetComponent<SpriteRenderer>();
                }

                serializedBoss.ApplyModifiedProperties();
            }

            // 5. Edit Sword Prefab
            string swordPrefabPath = "Assets/Prefabs/Boss/GhostKnight/ghost_knight_Sword.prefab";
            GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(swordPrefabPath);
            if (swordPrefab != null)
            {
                using (var editingScope = new PrefabUtility.EditPrefabContentsScope(swordPrefabPath))
                {
                    var prefabRoot = editingScope.prefabContentsRoot;

                    // Set layer to 9 and tag to "Untagged" recursively
                    SetLayerAndTagRecursively(prefabRoot.transform, 9, "Untagged");

                    // Ensure GhostKnightSword component exists
                    GhostKnightSword swordComp = prefabRoot.GetComponent<GhostKnightSword>();
                    if (swordComp == null)
                    {
                        swordComp = prefabRoot.AddComponent<GhostKnightSword>();
                    }

                    // Set all colliders to trigger
                    Collider2D[] colliders = prefabRoot.GetComponentsInChildren<Collider2D>(true);
                    foreach (var col in colliders)
                    {
                        col.isTrigger = true;
                    }
                }

                so.swordPrefab = swordPrefab;
                EditorUtility.SetDirty(so);
                Debug.Log("GhostKnightSword prefab set up and assigned to SO!");
            }
            else
            {
                Debug.LogError($"[SetupGhostKnightPrefab] Sword prefab not found at {swordPrefabPath}");
            }

            Debug.Log("GhostKnight prefab and SO setup completed automatically!");
            EditorPrefs.SetBool("SetupGhostKnightPrefabDone_DecagonPattern", true);
        }

        AssetDatabase.SaveAssets();
    }

    static void SetLayerAndTagRecursively(Transform trans, int layer, string tag)
    {
        trans.gameObject.layer = layer;
        // Only tag the root GameObject as Enemy, child components Untagged
        if (trans.parent == null)
        {
            trans.gameObject.tag = tag;
        }
        else
        {
            trans.gameObject.tag = "Untagged";
        }

        foreach (Transform child in trans)
        {
            SetLayerAndTagRecursively(child, layer, tag);
        }
    }
}
