using UnityEngine;
using UnityEditor;

public class AutoSetupPrefabs
{
    [InitializeOnLoadMethod]
    private static void RunFixesPhase3()
    {
        if (SessionState.GetBool("FixResultBoxPhase3Ran", false)) return;
        SessionState.SetBool("FixResultBoxPhase3Ran", true);

        string[] guids = AssetDatabase.FindAssets("Stage t:Prefab", new[] { "Assets/Prefabs/Map" });
        bool anyModified = false;

        Sprite squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Square.png");

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
                        // Set up sprites
                        if (squareSprite != null)
                        {
                            rb.closedSprite = squareSprite;
                            rb.openedSprite = squareSprite;
                            modified = true;
                        }

                        // Get or Create "Box" child
                        Transform boxChild = resultBoxObj.Find("Box");
                        if (boxChild == null)
                        {
                            GameObject boxGo = new GameObject("Box");
                            boxGo.transform.SetParent(resultBoxObj, false);
                            boxChild = boxGo.transform;
                            modified = true;
                        }

                        // Move SpriteRenderer to Box child
                        SpriteRenderer parentSr = resultBoxObj.GetComponent<SpriteRenderer>();
                        SpriteRenderer boxSr = boxChild.GetComponent<SpriteRenderer>();
                        if (boxSr == null) boxSr = boxChild.gameObject.AddComponent<SpriteRenderer>();
                        
                        if (parentSr != null)
                        {
                            boxSr.sprite = parentSr.sprite;
                            boxSr.color = parentSr.color;
                            boxSr.sortingOrder = parentSr.sortingOrder;
                            boxSr.sortingLayerID = parentSr.sortingLayerID;
                            Object.DestroyImmediate(parentSr, true);
                            modified = true;
                        }
                        
                        if (boxSr.sprite == null || boxSr.sprite != squareSprite)
                        {
                            boxSr.sprite = squareSprite;
                            modified = true;
                        }

                        rb.boxSpriteRenderer = boxSr;

                        // Move BoxCollider2D to Box child
                        BoxCollider2D parentCol = resultBoxObj.GetComponent<BoxCollider2D>();
                        BoxCollider2D boxCol = boxChild.GetComponent<BoxCollider2D>();
                        if (parentCol != null)
                        {
                            if (boxCol == null) boxCol = boxChild.gameObject.AddComponent<BoxCollider2D>();
                            boxCol.isTrigger = parentCol.isTrigger;
                            boxCol.size = parentCol.size;
                            boxCol.offset = parentCol.offset;
                            Object.DestroyImmediate(parentCol, true);
                            modified = true;
                        }
                        else if (boxCol == null)
                        {
                            boxCol = boxChild.gameObject.AddComponent<BoxCollider2D>();
                            boxCol.isTrigger = true;
                            modified = true;
                        }

                        // Set up "Square" child (DNA)
                        Transform square = resultBoxObj.Find("Square");
                        if (square != null)
                        {
                            rb.childDNA = square.gameObject;
                            square.gameObject.SetActive(false); // Ensure it's hidden initially
                            modified = true;
                        }
                    }
                }

                // Fix BossStage references
                BossStage bossStage = prefabRoot.GetComponent<BossStage>();
                if (bossStage != null)
                {
                    if (resultBoxObj != null && bossStage.resultBox == null)
                    {
                        bossStage.resultBox = resultBoxObj.GetComponent<ResultBox>();
                        modified = true;
                    }

                    if (bossStage.bloodTransfusionDevice == null)
                    {
                        Transform blood = FindChildRecursive(prefabRoot.transform, "BloodTransfusionDevice") ?? FindChildRecursive(prefabRoot.transform, "수혈기");
                        if (blood != null)
                        {
                            bossStage.bloodTransfusionDevice = blood.GetComponent<BloodTransfusionDevice>();
                            modified = true;
                        }
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
            Debug.Log("[AutoSetup] Phase 3: Rearranged ResultBoxNeedFix hierarchy to parent-child structure and fixed Knockback.");
        }
    }

    [InitializeOnLoadMethod]
    private static void RunFixesPhase4()
    {
        if (SessionState.GetBool("FixResultBoxPhase4Ran", false)) return;
        SessionState.SetBool("FixResultBoxPhase4Ran", true);

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
                    // 부모에 잘못 붙어있는 ResultDNA 제거
                    ResultDNA parentDNA = resultBoxObj.GetComponent<ResultDNA>();
                    if (parentDNA != null)
                    {
                        Object.DestroyImmediate(parentDNA, true);
                        modified = true;
                    }

                    // 부모에 잘못 붙어있는 Relay 제거
                    ResultDNATriggerRelay parentRelay = resultBoxObj.GetComponent<ResultDNATriggerRelay>();
                    if (parentRelay != null)
                    {
                        Object.DestroyImmediate(parentRelay, true);
                        modified = true;
                    }

                    // Box 자식 처리: DNA Relay 제거 후 Box Relay 부착
                    Transform boxChild2 = resultBoxObj.Find("Box");
                    if (boxChild2 != null)
                    {
                        // 잘못된 DNA Relay 제거
                        ResultDNATriggerRelay boxDnaRelay = boxChild2.GetComponent<ResultDNATriggerRelay>();
                        if (boxDnaRelay != null)
                        {
                            Object.DestroyImmediate(boxDnaRelay, true);
                            modified = true;
                        }

                        // ResultBoxTriggerRelay 부착 (없으면)
                        ResultBoxTriggerRelay boxRelay = boxChild2.GetComponent<ResultBoxTriggerRelay>();
                        if (boxRelay == null)
                        {
                            boxChild2.gameObject.AddComponent<ResultBoxTriggerRelay>();
                            modified = true;
                        }
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
            Debug.Log("[AutoSetup] Phase 4: Cleaned up stray ResultDNA and Relay components from parent and Box!");
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
}
