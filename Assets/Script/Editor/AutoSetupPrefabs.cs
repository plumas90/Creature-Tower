using UnityEngine;
using UnityEditor;

public class AutoSetupPrefabs
{
    [InitializeOnLoadMethod]
    private static void RunFixesPhase3()
    {
        SessionState.SetBool("FixResultBoxPhase3Ran", true);
    }

    [InitializeOnLoadMethod]
    private static void RunFixesPhase4()
    {
        SessionState.SetBool("FixResultBoxPhase4Ran", true);
    }

    [InitializeOnLoadMethod]
    private static void RunFixesPhase5_SimplifyToSingleObject()
    {
        if (SessionState.GetBool("FixResultBoxPhase5Ran", false)) return;
        SessionState.SetBool("FixResultBoxPhase5Ran", true);

        string dnaPrefabPath = "Assets/Prefabs/result/ResultDNA.prefab";
        GameObject dnaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(dnaPrefabPath);
        if (dnaPrefab == null)
        {
            Debug.LogWarning("[AutoSetup] ResultDNA prefab not found, skipping Phase 5 simplify.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("Stage t:Prefab", new[] { "Assets/Prefabs/Map" });
        bool anyModified = false;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null) continue;

            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var prefabRoot = editingScope.prefabContentsRoot;
                bool modified = false;

                Transform resultBoxObj = FindChildRecursive(prefabRoot.transform, "ResultBoxNeedFix") ?? FindChildRecursive(prefabRoot.transform, "ResultBox");
                if (resultBoxObj != null)
                {
                    ResultBox rb = resultBoxObj.GetComponent<ResultBox>();
                    if (rb != null)
                    {
                        // 1. Move SpriteRenderer back to parent
                        Transform boxChild = resultBoxObj.Find("Box");
                        if (boxChild != null)
                        {
                            SpriteRenderer childSr = boxChild.GetComponent<SpriteRenderer>();
                            if (childSr != null)
                            {
                                SpriteRenderer parentSr = resultBoxObj.GetComponent<SpriteRenderer>();
                                if (parentSr == null) parentSr = resultBoxObj.gameObject.AddComponent<SpriteRenderer>();

                                parentSr.sprite = childSr.sprite;
                                parentSr.color = childSr.color;
                                parentSr.sortingOrder = childSr.sortingOrder;
                                parentSr.sortingLayerID = childSr.sortingLayerID;
                                rb.boxSpriteRenderer = parentSr;
                                modified = true;
                            }

                            // 2. Move BoxCollider2D back to parent
                            BoxCollider2D childCol = boxChild.GetComponent<BoxCollider2D>();
                            if (childCol != null)
                            {
                                BoxCollider2D parentCol = resultBoxObj.GetComponent<BoxCollider2D>();
                                if (parentCol == null) parentCol = resultBoxObj.gameObject.AddComponent<BoxCollider2D>();

                                parentCol.isTrigger = childCol.isTrigger;
                                parentCol.size = childCol.size;
                                parentCol.offset = childCol.offset;
                                modified = true;
                            }

                            // Destroy child Box
                            Object.DestroyImmediate(boxChild.gameObject, true);
                            modified = true;
                        }

                        // 3. Destroy child Square (DNA)
                        Transform squareChild = resultBoxObj.Find("Square");
                        if (squareChild != null)
                        {
                            Object.DestroyImmediate(squareChild.gameObject, true);
                            modified = true;
                        }

                        // 4. Setup single-object fields
                        rb.childDNA = null;
                        rb.dnaPrefab = dnaPrefab;
                        
                        // Set up sprites
                        if (rb.closedSprite == null)
                        {
                            Sprite squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Square.png");
                            rb.closedSprite = squareSprite;
                            rb.openedSprite = squareSprite;
                        }

                        modified = true;
                    }
                }

                if (modified)
                {
                    anyModified = true;
                }
            }
        }

        if (anyModified)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[AutoSetup] Phase 5: Simplified all ResultBox objects in map prefabs into single-objects, destroyed Box/Square children, and assigned dnaPrefab!");
        }
    }

    [InitializeOnLoadMethod]
    private static void CreateResultDNAPrefab()
    {
        string prefabPath = "Assets/Prefabs/result/ResultDNA.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        // Create folder if it doesn't exist
        string folder = "Assets/Prefabs/result";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "result");
        }

        GameObject go = new GameObject("ResultDNA");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        ResultDNA dna = go.AddComponent<ResultDNA>();

        Sprite normalDnaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprite/RoomOption/RoomOption.png");
        Sprite redDnaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprite/RoomOption/RedDna.png");

        dna.resultdna = normalDnaSprite;
        dna.resultdna_red = redDnaSprite;

        sr.sprite = normalDnaSprite;
        sr.color = new Color(0.405571f, 0.722131f, 0.924528f, 1f);
        sr.sortingLayerName = "World_Dynamic";
        sr.sortingOrder = 3;

        col.isTrigger = true;
        col.size = new Vector2(1.0f, 1.6f);

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        Debug.Log("[AutoSetup] Created ResultDNA prefab at " + prefabPath);
    }

    [InitializeOnLoadMethod]
    private static void FixSingleResultBoxPrefabs()
    {
        // Forced execution for verification
        SessionState.SetBool("FixSingleResultBoxPrefabsRan", true);

        // 씬 내 TestGameManager 자동 코인 프리팹 셋업
        TestGameManager testGameManager = Object.FindFirstObjectByType<TestGameManager>();
        if (testGameManager != null && testGameManager.coinPrefab == null)
        {
            string coinItemPrefabPath = "Assets/Prefabs/result/CoinItem.prefab";
            GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(coinItemPrefabPath);
            if (coinPrefab != null)
            {
                testGameManager.coinPrefab = coinPrefab;
                EditorUtility.SetDirty(testGameManager);
                Debug.Log($"[AutoSetup] Automatically assigned coinPrefab to TestGameManager in current scene: {coinPrefab.name}");
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(testGameManager.gameObject.scene);
            }
        }

        string dnaPrefabPath = "Assets/Prefabs/result/ResultDNA.prefab";
        GameObject dnaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(dnaPrefabPath);
        if (dnaPrefab == null)
        {
            Debug.LogWarning("[AutoSetup] ResultDNA prefab not found, skipping single box prefabs fix.");
            return;
        }

        string[] boxPrefabNames = { "ResultBox", "AugmentBox", "CoinBox", "RandomBox" };
        bool anyModified = false;

        foreach (string name in boxPrefabNames)
        {
            string path = $"Assets/Prefabs/result/{name}.prefab";
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null) continue;

            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var prefabRoot = editingScope.prefabContentsRoot;
                ResultBox rb = prefabRoot.GetComponent<ResultBox>();
                if (rb == null) continue;

                bool modified = false;

                // 1. Assign dnaPrefab if missing
                if (rb.dnaPrefab == null)
                {
                    rb.dnaPrefab = dnaPrefab;
                    modified = true;
                }

                // 2. Set boxSpriteRenderer to root SpriteRenderer if missing
                SpriteRenderer rootSr = prefabRoot.GetComponent<SpriteRenderer>();
                if (rootSr != null && rb.boxSpriteRenderer == null)
                {
                    rb.boxSpriteRenderer = rootSr;
                    modified = true;
                }

                // 3. Ensure BoxCollider2D is trigger
                BoxCollider2D rootCol = prefabRoot.GetComponent<BoxCollider2D>();
                if (rootCol != null && !rootCol.isTrigger)
                {
                    rootCol.isTrigger = true;
                    modified = true;
                }

                if (modified)
                {
                    anyModified = true;
                }
            }
        }

        if (anyModified)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[AutoSetup] Successfully set up dnaPrefab, boxSpriteRenderer, and Colliders on single box prefabs!");
        }
    }

    [InitializeOnLoadMethod]
    private static void SetupNormalStageRewards()
    {
        // Forced execution for verification
        SessionState.SetBool("SetupNormalStageRewardsRan", true);

        string stagePrefabPath = "Assets/Prefabs/Map/NormalStageBases.prefab";
        GameObject stagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(stagePrefabPath);
        if (stagePrefab == null)
        {
            Debug.LogWarning("[AutoSetup] NormalStageBases prefab not found at " + stagePrefabPath);
            return;
        }

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(stagePrefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            NormalStage normalStage = prefabRoot.GetComponent<NormalStage>();
            if (normalStage == null) return;

            bool modified = false;

            // Load and assign prefabs
            GameObject randomBox = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/RandomBox.prefab");
            GameObject coinBox = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/CoinBox.prefab");
            GameObject augmentBox = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/AugmentBox.prefab");
            GameObject shop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/ShopRoot.prefab");
            GameObject transfusion = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/BloodTransfusionDevice.prefab");
            GameObject potion = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/Pothon.prefab");

            System.Type t = typeof(NormalStage);
            
            var fields = new (string fieldName, GameObject targetObj)[]
            {
                ("randomBoxPrefab", randomBox),
                ("coinBoxPrefab", coinBox),
                ("augmentBoxPrefab", augmentBox),
                ("shopPrefab", shop),
                ("bloodTransfusionPrefab", transfusion),
                ("potionPrefab", potion)
            };

            foreach (var item in fields)
            {
                var fieldInfo = t.GetField(item.fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    var currentVal = fieldInfo.GetValue(normalStage) as GameObject;
                    if (currentVal == null && item.targetObj != null)
                    {
                        fieldInfo.SetValue(normalStage, item.targetObj);
                        modified = true;
                        Debug.Log($"[AutoSetup] Assigned {item.fieldName} -> {item.targetObj.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[AutoSetup] Field {item.fieldName} not found on NormalStage script.");
                }
            }

            if (modified)
            {
                Debug.Log("[AutoSetup] Successfully set up all dynamic Reward Prefabs in NormalStageBases!");
            }
        }
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    [InitializeOnLoadMethod]
    private static void FixShopRootPrefabs()
    {
        string shopRootPath = "Assets/Prefabs/result/ShopRoot.prefab";
        GameObject shopRoot = AssetDatabase.LoadAssetAtPath<GameObject>(shopRootPath);
        if (shopRoot == null)
        {
            Debug.LogWarning("[AutoSetup] ShopRoot prefab not found at " + shopRootPath);
            return;
        }

        string dnaPrefabPath = "Assets/Prefabs/result/ResultDNA.prefab";
        GameObject dnaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(dnaPrefabPath);
        Sprite normalDNASprite = null;
        Sprite rareDNASprite = null;
        if (dnaPrefab != null)
        {
            ResultDNA rd = dnaPrefab.GetComponent<ResultDNA>();
            if (rd != null)
            {
                normalDNASprite = rd.resultdna;
                rareDNASprite = rd.resultdna_red;
            }
        }

        Sprite potion1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprite/RoomOption/Posion/potion.png");
        Sprite potion2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprite/RoomOption/Posion/potion2.png");

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(shopRootPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            ShopItem[] shopItems = prefabRoot.GetComponentsInChildren<ShopItem>(true);
            bool modified = false;

            foreach (var item in shopItems)
            {
                if (item == null) continue;

                // 1. Assign spriteRenderer if missing
                if (item.spriteRenderer == null)
                {
                    item.spriteRenderer = item.GetComponent<SpriteRenderer>();
                    modified = true;
                }

                // 2. Assign priceText if missing
                if (item.priceText == null)
                {
                    Transform ptTransform = FindChildRecursive(item.transform, "PriceText");
                    if (ptTransform != null)
                    {
                        item.priceText = ptTransform.GetComponent<TMPro.TextMeshPro>();
                        modified = true;
                    }
                }

                // 3. Assign DNA Sprites if missing
                if (item.normalDNASprite == null && normalDNASprite != null)
                {
                    item.normalDNASprite = normalDNASprite;
                    modified = true;
                }
                if (item.rareDNASprite == null && rareDNASprite != null)
                {
                    item.rareDNASprite = rareDNASprite;
                    modified = true;
                }

                // 4. Assign Potion Sprites if missing
                if (item.potion10Sprite == null && potion1 != null)
                {
                    item.potion10Sprite = potion1;
                    modified = true;
                }
                if (item.potion25Sprite == null && potion1 != null)
                {
                    item.potion25Sprite = potion1;
                    modified = true;
                }
                if (item.potion50Sprite == null && potion2 != null)
                {
                    item.potion50Sprite = potion2;
                    modified = true;
                }
                if (item.potion100Sprite == null && potion2 != null)
                {
                    item.potion100Sprite = potion2;
                    modified = true;
                }
            }

            if (modified)
            {
                Debug.Log("[AutoSetup] Successfully verified and fixed ShopItem components inside ShopRoot.prefab!");
            }
        }
    }
}
