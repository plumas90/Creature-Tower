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

    [InitializeOnLoadMethod]
    private static void FixTheWormPrefabSprites()
    {
        string wormPrefabPath = "Assets/Prefabs/Boss/TheWorm/TheWorm.prefab";
        GameObject wormPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(wormPrefabPath);
        if (wormPrefab == null)
        {
            Debug.LogWarning("[AutoSetup] TheWorm prefab not found at " + wormPrefabPath);
            return;
        }

        string caterpillarTexturePath = "Assets/sprite/Boss/worm/caterpillar_unified_fix.png";
        string wormDropTexturePath = "Assets/sprite/Boss/worm/worm_drop.png";

        // Load all sprites from caterpillar_unified_fix
        Object[] caterpillarObjects = AssetDatabase.LoadAllAssetRepresentationsAtPath(caterpillarTexturePath);
        Sprite idleSprite = null;
        Sprite charge1 = null;
        Sprite charge2 = null;
        Sprite charge3 = null;

        foreach (Object obj in caterpillarObjects)
        {
            if (obj is Sprite sprite)
            {
                if (sprite.name == "worm_idle") idleSprite = sprite;
                else if (sprite.name == "worm_charge1") charge1 = sprite;
                else if (sprite.name == "worm_charge2") charge2 = sprite;
                else if (sprite.name == "worm_charge3") charge3 = sprite;
            }
        }

        // Load all sprites from worm_drop
        Object[] dropObjects = AssetDatabase.LoadAllAssetRepresentationsAtPath(wormDropTexturePath);
        Sprite dropInAir = null;
        Sprite dropEnd1 = null;
        Sprite dropEnd2 = null;
        Sprite dropEnd3 = null;
        Sprite dropEnd4 = null;

        foreach (Object obj in dropObjects)
        {
            if (obj is Sprite sprite)
            {
                if (sprite.name == "worm_drop_inair") dropInAir = sprite;
                else if (sprite.name == "worm_drop_end1") dropEnd1 = sprite;
                else if (sprite.name == "worm_drop_end2") dropEnd2 = sprite;
                else if (sprite.name == "worm_drop_end3") dropEnd3 = sprite;
                else if (sprite.name == "worm_drop_end4") dropEnd4 = sprite;
            }
        }

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(wormPrefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            TheWorm worm = prefabRoot.GetComponent<TheWorm>();
            if (worm == null) return;

            bool modified = false;

            System.Type t = typeof(TheWorm);

            var fields = new (string fieldName, Sprite targetSprite)[]
            {
                ("wormIdleSprite", idleSprite),
                ("dropInAirSprite", dropInAir),
                ("charge1Sprite", charge1),
                ("charge2Sprite", charge2),
                ("charge3Sprite", charge3)
            };

            foreach (var item in fields)
            {
                var fieldInfo = t.GetField(item.fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    var currentVal = fieldInfo.GetValue(worm) as Sprite;
                    if (currentVal != item.targetSprite)
                    {
                        fieldInfo.SetValue(worm, item.targetSprite);
                        modified = true;
                        Debug.Log($"[AutoSetup] Assigned Worm Sprite {item.fieldName} -> {item.targetSprite?.name}");
                    }
                }
            }

            // Set dropEndSprites array
            var dropEndField = t.GetField("dropEndSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (dropEndField != null)
            {
                Sprite[] currentVal = dropEndField.GetValue(worm) as Sprite[];
                bool arrayNeedsUpdate = false;
                if (currentVal == null || currentVal.Length != 4)
                {
                    arrayNeedsUpdate = true;
                }
                else
                {
                    if (currentVal[0] != dropEnd1 || currentVal[1] != dropEnd2 || currentVal[2] != dropEnd3 || currentVal[3] != dropEnd4)
                    {
                        arrayNeedsUpdate = true;
                    }
                }

                if (arrayNeedsUpdate)
                {
                    Sprite[] newArray = new Sprite[] { dropEnd1, dropEnd2, dropEnd3, dropEnd4 };
                    dropEndField.SetValue(worm, newArray);
                    modified = true;
                    Debug.Log("[AutoSetup] Assigned dropEndSprites array [worm_drop_end1 ~ end4]");
                }
            }

            if (modified)
            {
                Debug.Log("[AutoSetup] Successfully verified and updated all Sprite fields inside TheWorm.prefab!");
            }
        }
    }

    [InitializeOnLoadMethod]
    private static void FixHauntedCrystalBall()
    {
        string basePrefabPath = "Assets/Prefabs/Boss/HauntedCrystalBall/HauntedCrystalBallGhost.prefab";
        string blackPrefabPath = "Assets/Prefabs/Boss/HauntedCrystalBall/HauntedCrystalBallGhostBlack.prefab";
        string whitePrefabPath = "Assets/Prefabs/Boss/HauntedCrystalBall/HauntedCrystalBallGhostWhite.prefab";
        string circlePrefabPath = "Assets/Prefabs/Boss/HauntedCrystalBall/HauntedCrystalBallGhostCircle.prefab";
        string tilePrefabPath = "Assets/Prefabs/Boss/HauntedCrystalBall/HauntedCrystalBallTile.prefab";
        string crystalBallSOPath = "Assets/SOData/Boss/HauntedCrystalBall/HauntedCrystalBallSO.asset";
        string spritesheetPath = "Assets/sprite/Boss/crystal_ball/blackGhost-sheet.png";

        // Load all sprites from blackGhost-sheet
        Object[] sheetObjects = AssetDatabase.LoadAllAssetRepresentationsAtPath(spritesheetPath);
        Sprite[] blackGhostSprites = new Sprite[6];
        Sprite[] whiteGhostSprites = new Sprite[6];
        Sprite[] dokkabiSprites = new Sprite[4];
        Sprite floorSircleSprite = null;

        foreach (Object obj in sheetObjects)
        {
            if (obj is Sprite sprite)
            {
                string name = sprite.name;
                if (name.StartsWith("blackghost"))
                {
                    int index = int.Parse(name.Substring(10)) - 1;
                    if (index >= 0 && index < 6) blackGhostSprites[index] = sprite;
                }
                else if (name.StartsWith("whiteghost"))
                {
                    int index = int.Parse(name.Substring(10)) - 1;
                    if (index >= 0 && index < 6) whiteGhostSprites[index] = sprite;
                }
                else if (name.StartsWith("dokkabi"))
                {
                    int index = int.Parse(name.Substring(7)) - 1;
                    if (index >= 0 && index < 4) dokkabiSprites[index] = sprite;
                }
                else if (name == "floor_sircle")
                {
                    floorSircleSprite = sprite;
                }
            }
        }

        // 1. Create black/white ghost prefabs by copying base ghost prefab if they don't exist
        bool prefabsCreatedOrUpdated = false;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(blackPrefabPath) == null)
        {
            AssetDatabase.CopyAsset(basePrefabPath, blackPrefabPath);
            prefabsCreatedOrUpdated = true;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(whitePrefabPath) == null)
        {
            AssetDatabase.CopyAsset(basePrefabPath, whitePrefabPath);
            prefabsCreatedOrUpdated = true;
        }

        if (prefabsCreatedOrUpdated)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // 2. Setup Black Ghost Prefab
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(blackPrefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            var ghost = prefabRoot.GetComponent<HauntedCrystalBallGhost>();
            if (ghost != null)
            {
                var field = typeof(HauntedCrystalBallGhost).GetField("idleSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(ghost, blackGhostSprites);
                }
            }
        }

        // 3. Setup White Ghost Prefab
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(whitePrefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            var ghost = prefabRoot.GetComponent<HauntedCrystalBallGhost>();
            if (ghost != null)
            {
                var field = typeof(HauntedCrystalBallGhost).GetField("idleSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(ghost, whiteGhostSprites);
                }
            }
        }

        // 4. Setup Ghost Circle Prefab (Dokkabi)
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(circlePrefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            var circle = prefabRoot.GetComponent<HauntedCrystalBallGhostCircle>();
            if (circle != null)
            {
                var field = typeof(HauntedCrystalBallGhostCircle).GetField("idleSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(circle, dokkabiSprites);
                }
            }
        }

        // 5. Setup Tile Prefab (Floor Circle)
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(tilePrefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            var tile = prefabRoot.GetComponent<HauntedCrystalBallTile>();
            if (tile != null)
            {
                tile.warningSprite = floorSircleSprite;
                tile.activeSprite = floorSircleSprite;
            }
        }

        // 6. Setup HauntedCrystalBallSO Asset
        var crystalSO = AssetDatabase.LoadAssetAtPath<HauntedCrystalBallSO>(crystalBallSOPath);
        if (crystalSO != null)
        {
            GameObject blackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(blackPrefabPath);
            GameObject whitePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(whitePrefabPath);

            bool soModified = false;
            if (crystalSO.blackGhostPrefab != blackPrefab)
            {
                crystalSO.blackGhostPrefab = blackPrefab;
                soModified = true;
            }
            if (crystalSO.whiteGhostPrefab != whitePrefab)
            {
                crystalSO.whiteGhostPrefab = whitePrefab;
                soModified = true;
            }

            if (soModified)
            {
                EditorUtility.SetDirty(crystalSO);
                AssetDatabase.SaveAssets();
                Debug.Log("[AutoSetup] Assigned black/white ghost prefabs to HauntedCrystalBallSO");
            }
        }

        // 7. Setup HauntedCrystalBallBoss Prefab YSortPivot
        string bossPrefabPath = "Assets/Prefabs/Boss/HauntedCrystalBall/HauntedCrystalBallBoss.prefab";
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(bossPrefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            var boss = prefabRoot.GetComponent<HauntedCrystalBall>();
            if (boss != null)
            {
                Transform ySortPivotTrans = prefabRoot.transform.Find("YSortPivot");
                if (ySortPivotTrans == null)
                {
                    GameObject go = new GameObject("YSortPivot");
                    ySortPivotTrans = go.transform;
                    ySortPivotTrans.SetParent(prefabRoot.transform);
                }
                
                ySortPivotTrans.localPosition = new Vector3(0f, -0.8f, 0f);
                
                var field = typeof(CreatureBase).GetField("ySortPivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(boss, ySortPivotTrans);
                }

                // 2D SortingGroup 설정 추가 (자식들의 개별 order인 0, 1, 2가 보존되며 하나의 덩어리로 월드 다이나믹 레이어 소팅을 타도록 함)
                var sortingGroup = prefabRoot.GetComponent<UnityEngine.Rendering.SortingGroup>();
                if (sortingGroup == null)
                {
                    sortingGroup = prefabRoot.AddComponent<UnityEngine.Rendering.SortingGroup>();
                }
                sortingGroup.sortingLayerName = "World_Dynamic";

                Debug.Log("[AutoSetup] Setup HauntedCrystalBallBoss YSortPivot at localPosition y=-0.8 and verified SortingGroup is configured on World_Dynamic.");
            }
        }
    }

    [InitializeOnLoadMethod]
    private static void FixChestsInStages()
    {
        string resultBoxPrefabPath = "Assets/Prefabs/result/ResultBox.prefab";
        string augmentBoxPrefabPath = "Assets/Prefabs/result/AugmentBox.prefab";
        string coinBoxPrefabPath = "Assets/Prefabs/result/CoinBox.prefab";
        string randomBoxPrefabPath = "Assets/Prefabs/result/RandomBox.prefab";

        GameObject resultBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(resultBoxPrefabPath);
        GameObject augmentBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(augmentBoxPrefabPath);
        GameObject coinBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(coinBoxPrefabPath);
        GameObject randomBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(randomBoxPrefabPath);

        if (resultBoxPrefab == null || augmentBoxPrefab == null || coinBoxPrefab == null || randomBoxPrefab == null)
        {
            Debug.LogWarning("[AutoSetup] One or more ResultBox target prefabs not found. Skipping chest sync.");
            return;
        }

        ResultBox resultBoxRef = resultBoxPrefab.GetComponent<ResultBox>();
        ResultBox augmentBoxRef = augmentBoxPrefab.GetComponent<ResultBox>();
        ResultBox coinBoxRef = coinBoxPrefab.GetComponent<ResultBox>();
        ResultBox randomBoxRef = randomBoxPrefab.GetComponent<ResultBox>();

        // 1. Scan and fix all Map prefabs
        string[] stageGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Map" });
        bool anyModified = false;

        foreach (string guid in stageGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var prefabRoot = editingScope.prefabContentsRoot;
                var childResultBoxes = prefabRoot.GetComponentsInChildren<ResultBox>(true);
                if (childResultBoxes.Length == 0) continue;

                bool modified = false;
                foreach (var rb in childResultBoxes)
                {
                    ResultBox refBox = null;
                    string name = rb.gameObject.name.ToLower();

                    if (name.Contains("coinbox")) refBox = coinBoxRef;
                    else if (name.Contains("augmentbox")) refBox = augmentBoxRef;
                    else if (name.Contains("randombox")) refBox = randomBoxRef;
                    else if (name.Contains("resultbox")) refBox = resultBoxRef;
                    else refBox = rb.isRareBox ? augmentBoxRef : resultBoxRef;

                    if (refBox == null) continue;

                    rb.closedSprite = refBox.closedSprite;
                    rb.openedSprite = refBox.openedSprite;
                    
                    if (refBox.openingSprites != null)
                    {
                        rb.openingSprites = new Sprite[refBox.openingSprites.Length];
                        System.Array.Copy(refBox.openingSprites, rb.openingSprites, refBox.openingSprites.Length);
                    }
                    else
                    {
                        rb.openingSprites = null;
                    }

                    rb.isRareBox = refBox.isRareBox;
                    rb.coinDropChance = refBox.coinDropChance;
                    rb.dnaPrefab = refBox.dnaPrefab;
                    rb.animationFps = refBox.animationFps;
                    rb.pushForce = refBox.pushForce;

                    SpriteRenderer sr = rb.GetComponent<SpriteRenderer>();
                    if (sr == null)
                    {
                        sr = rb.gameObject.AddComponent<SpriteRenderer>();
                    }

                    sr.sprite = refBox.closedSprite;
                    
                    SpriteRenderer refSr = refBox.GetComponent<SpriteRenderer>();
                    if (refSr != null)
                    {
                        sr.sortingLayerID = refSr.sortingLayerID;
                        sr.sortingOrder = refSr.sortingOrder;
                    }

                    rb.boxSpriteRenderer = sr;

                    // 프리팹 설정대로 localScale 및 BoxCollider2D size/offset 동기화
                    rb.transform.localScale = refBox.transform.localScale;

                    BoxCollider2D col = rb.GetComponent<BoxCollider2D>();
                    BoxCollider2D refCol = refBox.GetComponent<BoxCollider2D>();
                    if (col != null && refCol != null)
                    {
                        col.size = refCol.size;
                        col.offset = refCol.offset;
                        col.isTrigger = true;
                    }
                    else if (col == null && refCol != null)
                    {
                        col = rb.gameObject.AddComponent<BoxCollider2D>();
                        col.size = refCol.size;
                        col.offset = refCol.offset;
                        col.isTrigger = true;
                    }

                    modified = true;
                }

                if (modified)
                {
                    anyModified = true;
                    Debug.Log($"[AutoSetup] Automatically re-applied chest prefab settings to ResultBox in stage prefab: {path}");
                }
            }
        }

        // 2. Scan and fix Active Scene instances
        var sceneResultBoxes = Object.FindObjectsByType<ResultBox>(FindObjectsSortMode.None);
        if (sceneResultBoxes.Length > 0)
        {
            bool sceneModified = false;
            foreach (var rb in sceneResultBoxes)
            {
                ResultBox refBox = null;
                string name = rb.gameObject.name.ToLower();

                if (name.Contains("coinbox")) refBox = coinBoxRef;
                else if (name.Contains("augmentbox")) refBox = augmentBoxRef;
                else if (name.Contains("randombox")) refBox = randomBoxRef;
                else if (name.Contains("resultbox")) refBox = resultBoxRef;
                else refBox = rb.isRareBox ? augmentBoxRef : resultBoxRef;

                if (refBox == null) continue;

                Undo.RecordObject(rb, "Update ResultBox settings");
                
                rb.closedSprite = refBox.closedSprite;
                rb.openedSprite = refBox.openedSprite;
                
                if (refBox.openingSprites != null)
                {
                    rb.openingSprites = new Sprite[refBox.openingSprites.Length];
                    System.Array.Copy(refBox.openingSprites, rb.openingSprites, refBox.openingSprites.Length);
                }
                else
                {
                    rb.openingSprites = null;
                }

                rb.isRareBox = refBox.isRareBox;
                rb.coinDropChance = refBox.coinDropChance;
                rb.dnaPrefab = refBox.dnaPrefab;
                rb.animationFps = refBox.animationFps;
                rb.pushForce = refBox.pushForce;

                SpriteRenderer sr = rb.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    sr = rb.gameObject.AddComponent<SpriteRenderer>();
                }
                Undo.RecordObject(sr, "Update ResultBox SpriteRenderer");
                sr.sprite = refBox.closedSprite;
                
                SpriteRenderer refSr = refBox.GetComponent<SpriteRenderer>();
                if (refSr != null)
                {
                    sr.sortingLayerID = refSr.sortingLayerID;
                    sr.sortingOrder = refSr.sortingOrder;
                }

                rb.boxSpriteRenderer = sr;

                // 씬 오브젝트의 localScale & BoxCollider2D size/offset 동기화
                Undo.RecordObject(rb.transform, "Update ResultBox Transform Scale");
                rb.transform.localScale = refBox.transform.localScale;

                BoxCollider2D col = rb.GetComponent<BoxCollider2D>();
                BoxCollider2D refCol = refBox.GetComponent<BoxCollider2D>();
                if (col != null && refCol != null)
                {
                    Undo.RecordObject(col, "Update ResultBox Collider");
                    col.size = refCol.size;
                    col.offset = refCol.offset;
                    col.isTrigger = true;
                }
                else if (col == null && refCol != null)
                {
                    col = rb.gameObject.AddComponent<BoxCollider2D>();
                    Undo.RegisterCreatedObjectUndo(col, "Create ResultBox Collider");
                    col.size = refCol.size;
                    col.offset = refCol.offset;
                    col.isTrigger = true;
                }

                sceneModified = true;
            }

            if (sceneModified)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                Debug.Log("[AutoSetup] Successfully synchronized ResultBox objects in the active scene!");
            }
        }
    }

    [InitializeOnLoadMethod]
    private static void FixPeaPodBoss()
    {
        string bossPrefabPath = "Assets/Prefabs/Boss/PeaPod/PeaPodBoss.prefab";
        string spritesheetPath = "Assets/sprite/Boss/pea/peapod_shell_case 1.png";

        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bossPrefabPath);
        if (bossPrefab == null)
        {
            Debug.LogWarning("[AutoSetup] PeaPodBoss prefab not found.");
            return;
        }

        // Load all sprites from sheet
        Object[] sheetObjects = AssetDatabase.LoadAllAssetRepresentationsAtPath(spritesheetPath);
        Sprite[] angryHeadSprites = new Sprite[4];
        Sprite[] sadHeadSprites = new Sprite[4];
        Sprite[] happyHeadSprites = new Sprite[4];
        Sprite[] caseInSprites = new Sprite[8];
        Sprite[] caseOutSprites = new Sprite[8];

        foreach (Object obj in sheetObjects)
        {
            if (obj is Sprite sprite)
            {
                string name = sprite.name;
                if (name.StartsWith("pea_angry_head"))
                {
                    int index = int.Parse(name.Substring(14)) - 1;
                    if (index >= 0 && index < 4) angryHeadSprites[index] = sprite;
                }
                else if (name.StartsWith("pea_sad_head"))
                {
                    int index = int.Parse(name.Substring(12)) - 1;
                    if (index >= 0 && index < 4) sadHeadSprites[index] = sprite;
                }
                else if (name.StartsWith("pea_happy_head"))
                {
                    int index = int.Parse(name.Substring(14)) - 1;
                    if (index >= 0 && index < 4) happyHeadSprites[index] = sprite;
                }
                else if (name.StartsWith("peapod_case_in"))
                {
                    int index = int.Parse(name.Substring(14)) - 1;
                    if (index >= 0 && index < 8) caseInSprites[index] = sprite;
                }
                else if (name.StartsWith("peapod_case_out"))
                {
                    int index = int.Parse(name.Substring(15)) - 1;
                    if (index >= 0 && index < 8) caseOutSprites[index] = sprite;
                }
            }
        }

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(bossPrefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            var boss = prefabRoot.GetComponent<PeaPodBoss>();
            if (boss != null)
            {
                System.Type t = typeof(PeaPodBoss);
                
                // Find child GameObjects and get SpriteRenderers
                Transform angryHeadTrans = prefabRoot.transform.Find("angry_head");
                Transform sadHeadTrans = prefabRoot.transform.Find("sad_head");
                Transform normalHeadTrans = prefabRoot.transform.Find("normal_head");
                if (normalHeadTrans == null)
                    normalHeadTrans = prefabRoot.transform.Find("happy_head");
                Transform hitboxInTrans = prefabRoot.transform.Find("hitbox(in)");
                Transform outTrans = prefabRoot.transform.Find("out");

                var setField = new System.Action<string, object>((fieldName, value) => {
                    var fieldInfo = t.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fieldInfo != null)
                    {
                        fieldInfo.SetValue(boss, value);
                    }
                });

                if (angryHeadTrans != null) setField("angryHeadSr", angryHeadTrans.GetComponent<SpriteRenderer>());
                if (sadHeadTrans != null) setField("sadHeadSr", sadHeadTrans.GetComponent<SpriteRenderer>());
                if (normalHeadTrans != null) setField("happyHeadSr", normalHeadTrans.GetComponent<SpriteRenderer>());
                if (hitboxInTrans != null) setField("hitboxInSr", hitboxInTrans.GetComponent<SpriteRenderer>());
                if (outTrans != null) setField("outSr", outTrans.GetComponent<SpriteRenderer>());

                setField("angryHeadSprites", angryHeadSprites);
                setField("sadHeadSprites", sadHeadSprites);
                setField("happyHeadSprites", happyHeadSprites);
                setField("caseInSprites", caseInSprites);
                setField("caseOutSprites", caseOutSprites);

                Debug.Log("[AutoSetup] Successfully set up PeaPodBoss prefab sprite components and animation arrays.");
            }
        }

        // Setup PeaPodDeathPea bomb visual (assigned pea_happy_head1 sprite)
        string peaPrefabPath = "Assets/Prefabs/Boss/PeaPod/PeaPodDeathPea.prefab";
        GameObject peaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(peaPrefabPath);
        if (peaPrefab != null)
        {
            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(peaPrefabPath))
            {
                var prefabRoot = editingScope.prefabContentsRoot;
                var deathPea = prefabRoot.GetComponent<PeaPodDeathPea>();
                if (deathPea != null && happyHeadSprites.Length > 0 && happyHeadSprites[0] != null)
                {
                    var field = typeof(PeaPodDeathPea).GetField("peaSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(deathPea, happyHeadSprites[0]);
                    }

                    var sr = prefabRoot.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = happyHeadSprites[0];
                    }
                    Debug.Log("[AutoSetup] Successfully assigned pea_happy_head1 to PeaPodDeathPea prefab and component.");
                }
            }
        }
    }

    [InitializeOnLoadMethod]
    private static void FixPeaPodVineSegment()
    {
        string prefabPath = "Assets/Prefabs/Boss/PeaPod/PeaPodVineSegment.prefab";
        string spritesheetPath = "Assets/sprite/Boss/pea/pea_vin.png";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[AutoSetup] PeaPodVineSegment prefab not found.");
            return;
        }

        // Load all sprites from sheet
        Object[] sheetObjects = AssetDatabase.LoadAllAssetRepresentationsAtPath(spritesheetPath);
        Sprite[] growSprites = new Sprite[7];
        Sprite growEndSprite = null;
        Sprite[] dieSprites = new Sprite[5];

        foreach (Object obj in sheetObjects)
        {
            if (obj is Sprite sprite)
            {
                string name = sprite.name;
                if (name.StartsWith("pea_vine_grow"))
                {
                    if (name == "pea_vine_grow_end")
                    {
                        growEndSprite = sprite;
                    }
                    else
                    {
                        // parse frame index
                        string numPart = name.Substring(13);
                        if (int.TryParse(numPart, out int frameNum))
                        {
                            int index = frameNum - 1;
                            if (index >= 0 && index < 7)
                                growSprites[index] = sprite;
                        }
                    }
                }
                else if (name.StartsWith("pea_vine_die"))
                {
                    string numPart = name.Substring(12);
                    if (int.TryParse(numPart, out int frameNum))
                    {
                        int index = frameNum - 1;
                        if (index >= 0 && index < 5)
                            dieSprites[index] = sprite;
                    }
                }
            }
        }

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            var segment = prefabRoot.GetComponent<PeaPodVineSegment>();
            if (segment != null)
            {
                System.Type t = typeof(PeaPodVineSegment);
                var setField = new System.Action<string, object>((fieldName, value) => {
                    var fieldInfo = t.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fieldInfo != null)
                    {
                        fieldInfo.SetValue(segment, value);
                    }
                });

                setField("growSprites", growSprites);
                setField("growEndSprite", growEndSprite);
                setField("dieSprites", dieSprites);

                var sr = prefabRoot.GetComponent<SpriteRenderer>();
                if (sr != null && growSprites.Length > 0 && growSprites[0] != null)
                {
                    sr.sprite = growSprites[0];
                }

                Debug.Log("[AutoSetup] Successfully set up PeaPodVineSegment prefab sprite arrays.");
            }
        }
    }
}
