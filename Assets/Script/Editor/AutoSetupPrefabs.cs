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
    private static void FixResultDNAPrefab()
    {
        string prefabPath = "Assets/Prefabs/result/ResultDNA.prefab";
        Sprite normalDnaSprite = null;
        Object[] normalAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/sprite/RoomOption/RoomOption.png");
        foreach (Object obj in normalAssets)
        {
            if (obj is Sprite sprite && sprite.name == "result_dna")
            {
                normalDnaSprite = sprite;
                break;
            }
        }

        Sprite redDnaSprite = null;
        Object[] redAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/sprite/RoomOption/RedDna.png");
        foreach (Object obj in redAssets)
        {
            if (obj is Sprite sprite && sprite.name.StartsWith("RedDna", System.StringComparison.OrdinalIgnoreCase))
            {
                redDnaSprite = sprite;
                break;
            }
        }

        GameObject dnaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        bool modified = false;

        if (dnaPrefab == null)
        {
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
            modified = true;
            Debug.Log("[AutoSetup] Created ResultDNA prefab at " + prefabPath);
        }
        else
        {
            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var prefabRoot = editingScope.prefabContentsRoot;
                var dna = prefabRoot.GetComponent<ResultDNA>();
                if (dna != null)
                {
                    if (dna.resultdna != normalDnaSprite)
                    {
                        dna.resultdna = normalDnaSprite;
                        modified = true;
                    }
                    if (dna.resultdna_red != redDnaSprite)
                    {
                        dna.resultdna_red = redDnaSprite;
                        modified = true;
                    }
                }

                var sr = prefabRoot.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (sr.sprite != normalDnaSprite)
                    {
                        sr.sprite = normalDnaSprite;
                        modified = true;
                    }
                    if (sr.sortingLayerName != "World_Dynamic" || sr.sortingOrder != 3)
                    {
                        sr.sortingLayerName = "World_Dynamic";
                        sr.sortingOrder = 3;
                        modified = true;
                    }
                }
            }
        }

        if (modified)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[AutoSetup] Verified and fixed ResultDNA prefab sprites!");
        }
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

        string[] searchFolders = new string[] { "Assets/Prefabs/Map", "Assets/Prefabs/NormalStage" };
        string[] guids = AssetDatabase.FindAssets("t:Prefab", searchFolders);

        GameObject randomBox = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/RandomBox.prefab");
        GameObject coinBox = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/CoinBox.prefab");
        GameObject augmentBox = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/AugmentBox.prefab");
        GameObject shop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/ShopRoot.prefab");
        GameObject transfusion = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/BloodTransfusionDevice.prefab");
        GameObject potion = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/Pothon.prefab");
        GameObject myTved = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/result/my_tved.prefab");

        System.Type t = typeof(NormalStage);
        
        var fields = new (string fieldName, GameObject targetObj)[]
        {
            ("randomBoxPrefab", randomBox),
            ("coinBoxPrefab", coinBox),
            ("augmentBoxPrefab", augmentBox),
            ("shopPrefab", shop),
            ("bloodTransfusionPrefab", transfusion),
            ("potionPrefab", potion),
            ("myTvedPrefab", myTved)
        };

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null) continue;

            NormalStage ns = prefabAsset.GetComponent<NormalStage>();
            if (ns == null) continue;

            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var prefabRoot = editingScope.prefabContentsRoot;
                NormalStage normalStage = prefabRoot.GetComponent<NormalStage>();
                if (normalStage == null) continue;

                bool modified = false;

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
                            Debug.Log($"[AutoSetup] Assigned {item.fieldName} -> {item.targetObj.name} in {prefabAsset.name}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[AutoSetup] Field {item.fieldName} not found on NormalStage script.");
                    }
                }
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

    [InitializeOnLoadMethod]
    private static void FixBloodTransfusionDevice()
    {
        string basePrefabPath = "Assets/Prefabs/result/BloodTransfusionDevice.prefab";
        string pumpTexturePath = "Assets/sprite/RoomOption/pump128.png";

        // Load all sprites from sheet
        Object[] pumpObjects = AssetDatabase.LoadAllAssetRepresentationsAtPath(pumpTexturePath);
        Sprite[] pumpSprites = new Sprite[5];
        foreach (Object obj in pumpObjects)
        {
            if (obj is Sprite sprite)
            {
                if (sprite.name == "pump128_0") pumpSprites[0] = sprite;
                else if (sprite.name == "pump128_1") pumpSprites[1] = sprite;
                else if (sprite.name == "pump128_2") pumpSprites[2] = sprite;
                else if (sprite.name == "pump128_3") pumpSprites[3] = sprite;
                else if (sprite.name == "pump128_4") pumpSprites[4] = sprite;
            }
        }

        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
        if (basePrefab == null)
        {
            Debug.LogWarning("[AutoSetup] BloodTransfusionDevice prefab not found at " + basePrefabPath);
            return;
        }

        // 1. Setup the base prefab
        bool prefabModified = false;
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(basePrefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            var device = prefabRoot.GetComponent<BloodTransfusionDevice>();
            if (device != null)
            {
                // Ensure SpriteRenderer exists on the root
                SpriteRenderer sr = prefabRoot.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    sr = prefabRoot.AddComponent<SpriteRenderer>();
                    prefabModified = true;
                }

                // Set default sprite to pump128_0
                if (sr.sprite != pumpSprites[0])
                {
                    sr.sprite = pumpSprites[0];
                    prefabModified = true;
                }

                // Reset color to default (white)
                if (sr.color != Color.white)
                {
                    sr.color = Color.white;
                    prefabModified = true;
                }

                // Ensure sorting layer / order is set to match other devices
                sr.sortingLayerName = "World_Dynamic";
                sr.sortingOrder = 2;

                // Ensure localScale is (1.5f, 1.5f, 1.0f) as requested
                if (prefabRoot.transform.localScale != new Vector3(1.5f, 1.5f, 1f))
                {
                    prefabRoot.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                    prefabModified = true;
                }

                // Set the serialized fields via reflection
                System.Type t = typeof(BloodTransfusionDevice);
                
                var srField = t.GetField("sr", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (srField != null && (SpriteRenderer)srField.GetValue(device) != sr)
                {
                    srField.SetValue(device, sr);
                    prefabModified = true;
                }

                var animSpritesField = t.GetField("animSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (animSpritesField != null)
                {
                    Sprite[] currentVal = animSpritesField.GetValue(device) as Sprite[];
                    bool arrayNeedsUpdate = false;
                    if (currentVal == null || currentVal.Length != 5)
                    {
                        arrayNeedsUpdate = true;
                    }
                    else
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            if (currentVal[i] != pumpSprites[i])
                            {
                                arrayNeedsUpdate = true;
                                break;
                            }
                        }
                    }

                    if (arrayNeedsUpdate)
                    {
                        animSpritesField.SetValue(device, pumpSprites);
                        prefabModified = true;
                    }
                }
            }
        }

        if (prefabModified)
        {
            Debug.Log("[AutoSetup] Successfully verified and updated base BloodTransfusionDevice prefab!");
        }

        // 2. Scan and fix all Map prefabs for pre-placed pumps
        string[] stageGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Map" });
        bool anyStageModified = false;

        foreach (string guid in stageGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var prefabRoot = editingScope.prefabContentsRoot;
                var devicesInStage = prefabRoot.GetComponentsInChildren<BloodTransfusionDevice>(true);
                if (devicesInStage.Length == 0) continue;

                bool modified = false;
                foreach (var device in devicesInStage)
                {
                    // Ensure SpriteRenderer exists
                    SpriteRenderer sr = device.GetComponent<SpriteRenderer>();
                    if (sr == null)
                    {
                        sr = device.gameObject.AddComponent<SpriteRenderer>();
                    }

                    // Set default sprite to pump128_0 and color to white
                    sr.sprite = pumpSprites[0];
                    sr.color = Color.white;
                    sr.sortingLayerName = "World_Dynamic";
                    sr.sortingOrder = 2;

                    // Ensure scale is (1.5f, 1.5f, 1.0f)
                    device.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

                    // Assign references to the device component
                    System.Type t = typeof(BloodTransfusionDevice);
                    var srField = t.GetField("sr", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (srField != null)
                    {
                        srField.SetValue(device, sr);
                    }

                    var animSpritesField = t.GetField("animSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (animSpritesField != null)
                    {
                        animSpritesField.SetValue(device, pumpSprites);
                    }

                    modified = true;
                }

                if (modified)
                {
                    anyStageModified = true;
                    Debug.Log($"[AutoSetup] Automatically applied pump settings to BloodTransfusionDevice in stage prefab: {path}");
                }
            }
        }

        // 3. Scan and fix Active Scene instances
        var sceneDevices = Object.FindObjectsByType<BloodTransfusionDevice>(FindObjectsSortMode.None);
        if (sceneDevices.Length > 0)
        {
            bool sceneModified = false;
            foreach (var device in sceneDevices)
            {
                Undo.RecordObject(device, "Update BloodTransfusionDevice settings");

                SpriteRenderer sr = device.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    sr = device.gameObject.AddComponent<SpriteRenderer>();
                    Undo.RegisterCreatedObjectUndo(sr, "Create BloodTransfusionDevice SpriteRenderer");
                }
                else
                {
                    Undo.RecordObject(sr, "Update BloodTransfusionDevice SpriteRenderer");
                }

                sr.sprite = pumpSprites[0];
                sr.color = Color.white;
                sr.sortingLayerName = "World_Dynamic";
                sr.sortingOrder = 2;

                Undo.RecordObject(device.transform, "Update BloodTransfusionDevice Scale");
                device.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

                System.Type t = typeof(BloodTransfusionDevice);
                var srField = t.GetField("sr", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (srField != null)
                {
                    srField.SetValue(device, sr);
                }

                var animSpritesField = t.GetField("animSprites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (animSpritesField != null)
                {
                    animSpritesField.SetValue(device, pumpSprites);
                }

                sceneModified = true;
            }

            if (sceneModified)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                Debug.Log("[AutoSetup] Successfully synchronized BloodTransfusionDevice objects in the active scene!");
            }
        }

        if (prefabModified || anyStageModified)
        {
            AssetDatabase.SaveAssets();
        }
    }

    [InitializeOnLoadMethod]
    private static void FixMapSelectionUI()
    {
        string spritePath = "Assets/sprite/RoomOption/map_symbol.png";
        Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(spritePath);
        
        Sprite skullStampSprite = null;
        Object[] skullAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/sprite/RoomOption/skull_stamp.png");
        if (skullAssets != null)
        {
            foreach (var sub in skullAssets)
            {
                if (sub is Sprite s) { skullStampSprite = s; break; }
            }
        }
        if (skullStampSprite == null)
            skullStampSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprite/RoomOption/skull_stamp.png");
        
        Sprite verticalMapBgSprite = null;
        Object[] bgAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/sprite/RoomOption/vertical_map_background_transparent.png");
        if (bgAssets != null)
        {
            foreach (var sub in bgAssets)
            {
                if (sub is Sprite s) { verticalMapBgSprite = s; break; }
            }
        }
        if (verticalMapBgSprite == null)
            verticalMapBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprite/RoomOption/vertical_map_background_transparent.png");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning("[AutoSetup] No assets found in map_symbol.png!");
            return;
        }

        Debug.Log($"[AutoSetup] map_symbol.png contains {assets.Length} sprites.");
        
        string mapUiPrefabPath = "Assets/Prefabs/UI/MapSelectionUI.prefab";
        GameObject mapUiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mapUiPrefabPath);
        
        Sprite GetSprite(string name)
        {
            foreach (Object obj in assets)
            {
                if (obj is Sprite sprite && sprite.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return sprite;
                }
            }
            return null;
        }

        System.Type t = typeof(MapSelectionUI);
        var fields = new (string fieldName, string spriteName)[]
        {
            ("mysterySprite", "Mystery"),
            ("shopSprite", "shop"),
            ("transfusionSprite", "blood"),
            ("dnaSprite", "DNA"),
            ("coinSprite", "Gold"),
            ("boxSprite", "Box"),
            ("potionSprite", "potion"),
        };

        bool prefabModified = false;

        if (mapUiPrefab != null)
        {
            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(mapUiPrefabPath))
            {
                var prefabRoot = editingScope.prefabContentsRoot;
                MapSelectionUI mapUI = prefabRoot.GetComponent<MapSelectionUI>();
                if (mapUI != null)
                {
                    foreach (var item in fields)
                    {
                        var fieldInfo = t.GetField(item.fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fieldInfo != null)
                        {
                            var currentSprite = fieldInfo.GetValue(mapUI) as Sprite;
                            Sprite targetSprite = GetSprite(item.spriteName);
                            if (targetSprite != null && currentSprite != targetSprite)
                            {
                                fieldInfo.SetValue(mapUI, targetSprite);
                                prefabModified = true;
                                Debug.Log($"[AutoSetup] MapSelectionUI Assigned {item.fieldName} -> {targetSprite.name}");
                            }
                        }
                    }

                    var bossField = t.GetField("bossSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (bossField != null)
                    {
                        var currentSprite = bossField.GetValue(mapUI) as Sprite;
                        Sprite bossTarget = skullStampSprite;
                        if (bossTarget == null)
                        {
                            bossTarget = GetSprite("boss") ?? GetSprite("skull") ?? GetSprite("map_symbol_2");
                            if (bossTarget == null)
                            {
                                foreach (Object obj in assets)
                                {
                                    if (obj is Sprite sprite)
                                    {
                                        if (sprite.name.Contains("boss") || sprite.name.Contains("skull") || sprite.name.Contains("death"))
                                        {
                                            bossTarget = sprite;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (bossTarget != null && currentSprite != bossTarget)
                        {
                            bossField.SetValue(mapUI, bossTarget);
                            prefabModified = true;
                            Debug.Log($"[AutoSetup] MapSelectionUI Assigned bossSprite -> {bossTarget.name}");
                        }
                    }

                    var bgSpriteField = t.GetField("mapBackgroundSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (bgSpriteField != null && verticalMapBgSprite != null)
                    {
                        var currentBgSprite = bgSpriteField.GetValue(mapUI) as Sprite;
                        if (currentBgSprite != verticalMapBgSprite)
                        {
                            bgSpriteField.SetValue(mapUI, verticalMapBgSprite);
                            prefabModified = true;
                            Debug.Log($"[AutoSetup] MapSelectionUI Assigned mapBackgroundSprite -> {verticalMapBgSprite.name}");
                        }
                    }

                    var bgImageField = t.GetField("mapBackgroundImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (bgImageField != null)
                    {
                        var currentBgImage = bgImageField.GetValue(mapUI) as UnityEngine.UI.Image;
                        Transform scrollViewTrans = prefabRoot.transform.Find("Scroll View");
                        if (scrollViewTrans != null)
                        {
                            var scrollViewImg = scrollViewTrans.GetComponent<UnityEngine.UI.Image>();
                            if (scrollViewImg != null && currentBgImage != scrollViewImg)
                            {
                                bgImageField.SetValue(mapUI, scrollViewImg);
                                prefabModified = true;
                                Debug.Log($"[AutoSetup] MapSelectionUI Assigned mapBackgroundImage -> Scroll View Image component");
                            }
                        }
                    }

                    // Scale spacing and padding by 1.5x
                    var vSpacingField = t.GetField("verticalSpacing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (vSpacingField != null && (float)vSpacingField.GetValue(mapUI) != 240f)
                    {
                        vSpacingField.SetValue(mapUI, 240f);
                        prefabModified = true;
                        Debug.Log("[AutoSetup] MapSelectionUI scaled verticalSpacing to 240f");
                    }
                    var hSpacingField = t.GetField("horizontalSpacing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (hSpacingField != null && (float)hSpacingField.GetValue(mapUI) != 270f)
                    {
                        hSpacingField.SetValue(mapUI, 270f);
                        prefabModified = true;
                        Debug.Log("[AutoSetup] MapSelectionUI scaled horizontalSpacing to 270f");
                    }
                    var paddingTopField = t.GetField("mapPaddingTop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (paddingTopField != null && (float)paddingTopField.GetValue(mapUI) != 150f)
                    {
                        paddingTopField.SetValue(mapUI, 150f);
                        prefabModified = true;
                        Debug.Log("[AutoSetup] MapSelectionUI scaled mapPaddingTop to 150f");
                    }
                    var paddingBotField = t.GetField("mapPaddingBottom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (paddingBotField != null && (float)paddingBotField.GetValue(mapUI) != 150f)
                    {
                        paddingBotField.SetValue(mapUI, 150f);
                        prefabModified = true;
                        Debug.Log("[AutoSetup] MapSelectionUI scaled mapPaddingBottom to 150f");
                    }
                }
            }
        }

        // Fix Active Scene instances
        var sceneUIs = Object.FindObjectsByType<MapSelectionUI>(FindObjectsSortMode.None);
        if (sceneUIs.Length > 0)
        {
            bool sceneModified = false;
            foreach (var mapUI in sceneUIs)
            {
                Undo.RecordObject(mapUI, "Update MapSelectionUI settings");

                foreach (var item in fields)
                {
                    var fieldInfo = t.GetField(item.fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fieldInfo != null)
                    {
                        var currentSprite = fieldInfo.GetValue(mapUI) as Sprite;
                        Sprite targetSprite = GetSprite(item.spriteName);
                        if (targetSprite != null && currentSprite != targetSprite)
                        {
                            fieldInfo.SetValue(mapUI, targetSprite);
                            sceneModified = true;
                            Debug.Log($"[AutoSetup] Scene MapSelectionUI Assigned {item.fieldName} -> {targetSprite.name}");
                        }
                    }
                }

                var bossField = t.GetField("bossSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (bossField != null)
                {
                    var currentSprite = bossField.GetValue(mapUI) as Sprite;
                    Sprite bossTarget = skullStampSprite;
                    if (bossTarget == null)
                    {
                        bossTarget = GetSprite("boss") ?? GetSprite("skull") ?? GetSprite("map_symbol_2");
                        if (bossTarget == null)
                        {
                            foreach (Object obj in assets)
                            {
                                if (obj is Sprite sprite)
                                {
                                    if (sprite.name.Contains("boss") || sprite.name.Contains("skull") || sprite.name.Contains("death"))
                                    {
                                        bossTarget = sprite;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (bossTarget != null && currentSprite != bossTarget)
                    {
                        bossField.SetValue(mapUI, bossTarget);
                        sceneModified = true;
                        Debug.Log($"[AutoSetup] Scene MapSelectionUI Assigned bossSprite -> {bossTarget.name}");
                    }
                }

                var bgSpriteField = t.GetField("mapBackgroundSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (bgSpriteField != null && verticalMapBgSprite != null)
                {
                    var currentBgSprite = bgSpriteField.GetValue(mapUI) as Sprite;
                    if (currentBgSprite != verticalMapBgSprite)
                    {
                        bgSpriteField.SetValue(mapUI, verticalMapBgSprite);
                        sceneModified = true;
                        Debug.Log($"[AutoSetup] Scene MapSelectionUI Assigned mapBackgroundSprite -> {verticalMapBgSprite.name}");
                    }
                }

                var bgImageField = t.GetField("mapBackgroundImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (bgImageField != null)
                {
                    var currentBgImage = bgImageField.GetValue(mapUI) as UnityEngine.UI.Image;
                    Transform scrollViewTrans = mapUI.transform.Find("Scroll View");
                    if (scrollViewTrans != null)
                    {
                        var scrollViewImg = scrollViewTrans.GetComponent<UnityEngine.UI.Image>();
                        if (scrollViewImg != null && currentBgImage != scrollViewImg)
                        {
                            bgImageField.SetValue(mapUI, scrollViewImg);
                            sceneModified = true;
                            Debug.Log($"[AutoSetup] Scene MapSelectionUI Assigned mapBackgroundImage -> Scroll View Image component");
                        }
                    }
                }

                // Scale spacing and padding by 1.5x in active scene
                var vSpacingField = t.GetField("verticalSpacing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (vSpacingField != null && (float)vSpacingField.GetValue(mapUI) != 240f)
                {
                    vSpacingField.SetValue(mapUI, 240f);
                    sceneModified = true;
                }
                var hSpacingField = t.GetField("horizontalSpacing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (hSpacingField != null && (float)hSpacingField.GetValue(mapUI) != 270f)
                {
                    hSpacingField.SetValue(mapUI, 270f);
                    sceneModified = true;
                }
                var paddingTopField = t.GetField("mapPaddingTop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (paddingTopField != null && (float)paddingTopField.GetValue(mapUI) != 150f)
                {
                    paddingTopField.SetValue(mapUI, 150f);
                    sceneModified = true;
                }
                var paddingBotField = t.GetField("mapPaddingBottom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (paddingBotField != null && (float)paddingBotField.GetValue(mapUI) != 150f)
                {
                    paddingBotField.SetValue(mapUI, 150f);
                    sceneModified = true;
                }
            }

            if (sceneModified)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                Debug.Log("[AutoSetup] Successfully synchronized MapSelectionUI objects in the active scene!");
            }
        }

        if (prefabModified)
        {
            AssetDatabase.SaveAssets();
        }
    }

    [InitializeOnLoadMethod]
    private static void InspectAndFixMapNodePrefab()
    {
        System.IO.StringWriter sw = new System.IO.StringWriter();
        sw.WriteLine("=== InspectAndFixMapNodePrefab Log ===");
        
        string prefabPath = "Assets/Prefabs/UI/MapNodePrefab.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("[AutoSetup] ERROR: MapNodePrefab not found at " + prefabPath);
            return;
        }
        
        sw.WriteLine("Loaded MapNodePrefab successfully.");
        
        // Print original hierarchy
        sw.WriteLine("--- Original Hierarchy ---");
        PrintTransformToLog(prefab.transform, "", sw);
        
        // We will modify the prefab inside EditPrefabContentsScope
        bool modified = false;
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            var nodeUI = prefabRoot.GetComponent<MapRoomNodeUI>();
            if (nodeUI == null)
            {
                sw.WriteLine("ERROR: MapRoomNodeUI component not found on prefab root.");
            }
            else
            {
                System.Type t = typeof(MapRoomNodeUI);
                
                // 1. Scale MapNodePrefab root sizeDelta to 135x135 (1.5x from 90x90)
                RectTransform rootRect = prefabRoot.GetComponent<RectTransform>();
                if (rootRect != null)
                {
                    Vector2 newSize = new Vector2(135f, 135f);
                    if (rootRect.sizeDelta != newSize)
                    {
                        rootRect.sizeDelta = newSize;
                        modified = true;
                        sw.WriteLine($"Resized MapNodePrefab root RectTransform to {newSize}");
                    }
                }
                
                // 2. Remove the child SymbolImage GameObject
                Transform symbolImageTrans = prefabRoot.transform.Find("SymbolImage");
                if (symbolImageTrans != null)
                {
                    Object.DestroyImmediate(symbolImageTrans.gameObject, true);
                    modified = true;
                    sw.WriteLine("Destroyed SymbolImage child GameObject.");
                }
                
                // 3. Deactivate child Bgimage GameObject
                Transform bgImageTrans = prefabRoot.transform.Find("Bgimage") ?? prefabRoot.transform.Find("BgImage") ?? prefabRoot.transform.Find("Background");
                if (bgImageTrans != null)
                {
                    if (bgImageTrans.gameObject.activeSelf)
                    {
                        bgImageTrans.gameObject.SetActive(false);
                        modified = true;
                        sw.WriteLine($"Deactivated child background GameObject: {bgImageTrans.name}");
                    }
                }
                
                // 4. Bind bgImage field of MapRoomNodeUI to null (so it is skipped at runtime)
                var bgImageField = t.GetField("bgImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (bgImageField != null)
                {
                    var currentBgVal = bgImageField.GetValue(nodeUI);
                    if (currentBgVal != null)
                    {
                        bgImageField.SetValue(nodeUI, null);
                        modified = true;
                        sw.WriteLine("Cleared bgImage field (set to null).");
                    }
                }
                
                // 5. Bind symbolImage field to root Image component
                var symImgField = t.GetField("symbolImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (symImgField != null)
                {
                    var rootImg = prefabRoot.GetComponent<UnityEngine.UI.Image>();
                    var currentVal = symImgField.GetValue(nodeUI) as UnityEngine.UI.Image;
                    if (currentVal != rootImg)
                    {
                        symImgField.SetValue(nodeUI, rootImg);
                        modified = true;
                        sw.WriteLine("Assigned symbolImage field to root Image component.");
                    }
                    
                    if (rootImg != null)
                    {
                        if (rootImg.raycastTarget == false)
                        {
                            rootImg.raycastTarget = true;
                            modified = true;
                            sw.WriteLine("Set root Image raycastTarget to true.");
                        }
                        if (!rootImg.preserveAspect)
                        {
                            rootImg.preserveAspect = true;
                            modified = true;
                            sw.WriteLine("Set root Image preserveAspect to true.");
                        }
                    }
                }
            }
        }
        sw.WriteLine($"Modified: {modified}");
        Debug.Log(sw.ToString());
    }

    private static void PrintTransformToLog(Transform t, string indent, System.IO.StringWriter sw)
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (var c in t.GetComponents<Component>())
        {
            if (c != null) list.Add(c.GetType().Name);
        }
        sw.WriteLine($"{indent}- {t.name} (Components: {string.Join(", ", list)})");
        for (int i = 0; i < t.childCount; i++)
        {
            PrintTransformToLog(t.GetChild(i), indent + "  ", sw);
        }
    }
}
