using UnityEngine;
using UnityEditor;
using TMPro;

public class ShopSetupEditor
{
    [MenuItem("Tools/Setup ShopRoot Prefab")]
    public static void DoSetup()
    {
        // 1. 폴더 확인
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) 
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs")) 
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

        // 2. 기본 스프라이트 로드
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Sprite squareSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        // 3. ShopRoot 생성
        GameObject shopRoot = new GameObject("ShopRoot");
        var shopController = shopRoot.AddComponent<ShopController>();

        // 상인(Merchant) 시각적 요소 (네모난 배경 돗자리와 상인)
        GameObject matObj = new GameObject("Mat");
        matObj.transform.SetParent(shopRoot.transform);
        matObj.transform.localPosition = Vector3.zero;
        var matSR = matObj.AddComponent<SpriteRenderer>();
        matSR.sprite = squareSprite;
        matSR.color = new Color(0.6f, 0.4f, 0.2f); // 갈색 돗자리
        matObj.transform.localScale = new Vector3(8f, 5f, 1f);
        matSR.sortingOrder = -100; // 바닥 장판은 YSort를 받지 않고 무조건 가장 아래(바닥)에 렌더링

        GameObject merchantObj = new GameObject("Merchant");
        merchantObj.transform.SetParent(shopRoot.transform);
        merchantObj.transform.localPosition = new Vector3(0f, 1f, 0f);
        var merchantSR = merchantObj.AddComponent<SpriteRenderer>();
        merchantSR.sprite = squareSprite;
        merchantSR.color = Color.blue;
        merchantObj.transform.localScale = new Vector3(1f, 2f, 1f);
        merchantObj.AddComponent<WorldDynamicYSort>(); // 상인은 동적 YSort 적용

        // 4. 슬롯 3개 생성
        shopController.shopItems = new ShopItem[3];
        float[] xOffsets = { -2f, 0f, 2f };

        for (int i = 0; i < 3; i++)
        {
            GameObject slotObj = new GameObject($"ShopItem_{i}");
            slotObj.transform.SetParent(shopRoot.transform);
            slotObj.transform.localPosition = new Vector3(xOffsets[i], -1f, 0f);

            var sr = slotObj.AddComponent<SpriteRenderer>();
            sr.sprite = circleSprite;
            sr.color = Color.white;
            slotObj.transform.localScale = new Vector3(1f, 1f, 1f);
            slotObj.AddComponent<WorldDynamicYSort>(); // 아이템들도 동적 YSort 적용

            var col = slotObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var shopItem = slotObj.AddComponent<ShopItem>();
            shopItem.spriteRenderer = sr;

            // 임시로 기본 스프라이트들을 전부 circle로 할당 (실제로는 인스펙터나 런타임에 재할당됨)
            shopItem.normalDNASprite = circleSprite;
            shopItem.rareDNASprite = circleSprite;
            shopItem.potion10Sprite = circleSprite;
            shopItem.potion25Sprite = circleSprite;
            shopItem.potion50Sprite = circleSprite;
            shopItem.potion100Sprite = circleSprite;

            // 텍스트(TMP) 생성
            GameObject textObj = new GameObject("PriceText");
            textObj.transform.SetParent(slotObj.transform);
            textObj.transform.localPosition = new Vector3(0f, -0.8f, 0f);
            
            var textTMP = textObj.AddComponent<TextMeshPro>();
            textTMP.text = "0 G";
            textTMP.fontSize = 3;
            textTMP.alignment = TextAlignmentOptions.Center;
            textTMP.color = Color.yellow;
            // Sorting layer order for text to render above sprite
            textTMP.sortingOrder = 5;

            shopItem.priceText = textTMP;
            shopController.shopItems[i] = shopItem;
        }

        // 5. 프리팹 저장
        string prefabPath = "Assets/Resources/Prefabs/ShopRoot.prefab";
        PrefabUtility.SaveAsPrefabAsset(shopRoot, prefabPath);
        GameObject.DestroyImmediate(shopRoot);

        Debug.Log("[ShopSetupEditor] ShopRoot Prefab saved at " + prefabPath);
    }
}
