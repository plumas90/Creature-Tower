using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class BuildGameplayUiPrefabsTool
{
    private const string OutputFolder = "Assets/Prefabs/UI/Gameplay";
    private const string PlayerInfoUiPrefabPath = "Assets/Prefabs/UI/PlayerInfoUI.prefab";
    private const string LegacyPlayerHudPrefabPath = "Assets/Resources/Prefabs/PlayerHUD/PlayerHUD.prefab";
    private const string ActiveHudPrefabPath = LegacyPlayerHudPrefabPath;

    private const string HpPrefabPath = OutputFolder + "/UIPlayerHP.prefab";
    private const string RollPrefabPath = OutputFolder + "/UiPlayerRoll.prefab";
    private const string SkillPrefabPath = OutputFolder + "/UIPlayerSkill.prefab";
    private const string ReloadPrefabPath = OutputFolder + "/UIReloadHUD.prefab";

    private const string HpMpBarSpritePath = "Assets/sprite/UI/hpmpbar.png";
    private const string ReloadBarSpritePath = "Assets/sprite/UI/reloadbar.png";
    private const string SkillSpritePath = "Assets/sprite/UI/SKill.png";

    [MenuItem("Tools/UI/Build Gameplay UI Prefabs")]
    public static void BuildPrefabs()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/UI");
        EnsureFolder(OutputFolder);

        Sprite hpBgSprite = LoadSpriteByName(HpMpBarSpritePath, "hpmpbar_0");
        Sprite hpGaugeSprite = LoadSpriteByName(HpMpBarSpritePath, "hpmpbar_1");
        Sprite staminaGaugeSprite = LoadSpriteByName(HpMpBarSpritePath, "hpmpbar_2");

        Sprite reloadBgSprite = LoadSpriteByName(ReloadBarSpritePath, "HOME");
        Sprite reloadGaugeSprite = LoadSpriteByName(ReloadBarSpritePath, "BAR");

        Sprite skillSprite = LoadSpriteByName(SkillSpritePath, "SKill_0");

        if (hpBgSprite == null || hpGaugeSprite == null || staminaGaugeSprite == null)
            Debug.LogWarning($"[BuildGameplayUiPrefabsTool] hpmpbar sprite slices missing in: {HpMpBarSpritePath}");
        if (reloadBgSprite == null || reloadGaugeSprite == null)
            Debug.LogWarning($"[BuildGameplayUiPrefabsTool] reloadbar sprite slices missing in: {ReloadBarSpritePath}");
        if (skillSprite == null)
            Debug.LogWarning($"[BuildGameplayUiPrefabsTool] Missing sprite: {SkillSpritePath}");

        BuildUIPlayerHPPrefab(hpBgSprite, hpGaugeSprite);
        BuildUiPlayerRollPrefab(hpBgSprite, staminaGaugeSprite);
        BuildUIPlayerSkillPrefab(skillSprite);
        BuildUIReloadHUDPrefab(reloadBgSprite, reloadGaugeSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BuildGameplayUiPrefabsTool] Gameplay UI prefabs generated under Assets/Prefabs/UI/Gameplay. Re-run 'Apply Gameplay UI To PlayerInfoUI' to sync scene HUD.");
    }

    [MenuItem("Tools/UI/Upgrade PlayerHUD For Current UI")]
    public static void BuildCurrentPlayerInfoUIFromPlayerHUD()
    {
        if (!System.IO.File.Exists(ActiveHudPrefabPath))
        {
            Debug.LogError($"[BuildGameplayUiPrefabsTool] PlayerHUD prefab not found: {ActiveHudPrefabPath}");
            return;
        }

        Sprite hpBgSprite = LoadSpriteByName(HpMpBarSpritePath, "hpmpbar_0");
        Sprite hpGaugeSprite = LoadSpriteByName(HpMpBarSpritePath, "hpmpbar_1");
        Sprite staminaGaugeSprite = LoadSpriteByName(HpMpBarSpritePath, "hpmpbar_2");
        Sprite reloadBgSprite = LoadSpriteByName(ReloadBarSpritePath, "HOME");
        Sprite reloadGaugeSprite = LoadSpriteByName(ReloadBarSpritePath, "BAR");
        Sprite skillSprite = LoadSpriteByName(SkillSpritePath, "SKill_0");

        GameObject root = PrefabUtility.LoadPrefabContents(ActiveHudPrefabPath);
        if (root == null)
        {
            Debug.LogError("[BuildGameplayUiPrefabsTool] Failed to load PlayerHUD prefab contents.");
            return;
        }

        try
        {
            RemoveMissingScriptsRecursive(root.transform);

            if (root.GetComponent<PlayerUiManager>() == null)
                root.AddComponent<PlayerUiManager>();

            if (root.GetComponent<UIPlayerHUD>() == null)
                root.AddComponent<UIPlayerHUD>();

            Transform playerInfo = root.transform.Find("PlayerInfo");
            Transform hp = playerInfo != null ? playerInfo.Find("HP") : null;
            Transform dodge = playerInfo != null ? playerInfo.Find("Dodge") : null;
            Transform skill = playerInfo != null ? playerInfo.Find("SkillUI") : null;
            Transform ammo = root.transform.Find("Ammo");

            UIPlayerHP hpComp = EnsureComponent<UIPlayerHP>(hp);
            UiPlayerRoll rollComp = EnsureComponent<UiPlayerRoll>(dodge);
            UIPlayerSkill skillComp = EnsureComponent<UIPlayerSkill>(skill);
            AmmoUpdate ammoComp = EnsureComponent<AmmoUpdate>(ammo);

            if (hpComp != null)
            {
                SetObjectField(hpComp, "hpBackgroundSprite", hpBgSprite);
                SetObjectField(hpComp, "hpGaugeSprite", hpGaugeSprite);
                SetObjectField(hpComp, "hpGauge", FindImage(root.transform, "PlayerInfo/HP/FillParent/Fill"));
            }

            if (rollComp != null)
            {
                SetObjectField(rollComp, "hpBackgroundSprite", hpBgSprite);
                SetObjectField(rollComp, "staminaGaugeSprite", staminaGaugeSprite);
                SetObjectField(rollComp, "dodgeGauge", FindImage(root.transform, "PlayerInfo/Dodge/FillParent/Fill"));
            }

            if (skillComp != null)
            {
                SetObjectField(skillComp, "skillSprite", skillSprite);
                SetObjectField(skillComp, "skillIcon", FindImage(root.transform, "PlayerInfo/SkillUI/Icon"));
                Image skillGauge = FindImage(root.transform, "PlayerInfo/SkillUI/ForeGround");
                if (skillGauge != null)
                {
                    skillGauge.type = Image.Type.Filled;
                    skillGauge.fillMethod = Image.FillMethod.Radial360;
                    skillGauge.fillOrigin = (int)Image.Origin360.Top;
                    skillGauge.fillClockwise = true;
                }
                SetObjectField(skillComp, "skillGauge", skillGauge);
            }

            if (ammoComp != null)
            {
                SetObjectField(ammoComp, "ammo", FindText(root.transform, "Ammo/AmmoText"));
            }

            UIReloadHUD reloadComp = root.GetComponentInChildren<UIReloadHUD>(true);
            if (reloadComp == null)
            {
                GameObject reloadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReloadPrefabPath);
                if (reloadPrefab != null)
                {
                    GameObject instance = PrefabUtility.InstantiatePrefab(reloadPrefab) as GameObject;
                    if (instance != null)
                    {
                        instance.transform.SetParent(root.transform, false);
                        instance.name = "UIReloadHUD";
                        reloadComp = instance.GetComponent<UIReloadHUD>();
                    }
                }
            }

            if (reloadComp != null)
            {
                SetObjectField(reloadComp, "reloadBackgroundSprite", reloadBgSprite);
                SetObjectField(reloadComp, "reloadGaugeSprite", reloadGaugeSprite);
            }

            PrefabUtility.SaveAsPrefabAsset(root, ActiveHudPrefabPath);
            Debug.Log("[BuildGameplayUiPrefabsTool] Updated PlayerHUD prefab for current runtime UI scripts.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Tools/UI/Apply Gameplay UI To PlayerInfoUI")]
    public static void ApplyToPlayerInfoUI()
    {
        GameObject hpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HpPrefabPath);
        GameObject rollPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RollPrefabPath);
        GameObject skillPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkillPrefabPath);
        GameObject reloadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReloadPrefabPath);

        if (hpPrefab == null || rollPrefab == null || skillPrefab == null || reloadPrefab == null)
        {
            Debug.LogError("[BuildGameplayUiPrefabsTool] Missing one or more gameplay UI prefabs. Run 'Build Gameplay UI Prefabs' first.");
            return;
        }

        if (!System.IO.File.Exists(PlayerInfoUiPrefabPath))
        {
            Debug.LogError($"[BuildGameplayUiPrefabsTool] PlayerInfoUI prefab not found: {PlayerInfoUiPrefabPath}");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PlayerInfoUiPrefabPath);
        if (root == null)
        {
            Debug.LogError("[BuildGameplayUiPrefabsTool] Failed to load PlayerInfoUI prefab contents.");
            return;
        }

        try
        {
            RemoveOldHudNodes(root);

            AddChildPrefab(root.transform, hpPrefab, "UIPlayerHP");
            AddChildPrefab(root.transform, rollPrefab, "UiPlayerRoll");
            AddChildPrefab(root.transform, skillPrefab, "UIPlayerSkill");
            AddChildPrefab(root.transform, reloadPrefab, "UIReloadHUD");

            PrefabUtility.SaveAsPrefabAsset(root, PlayerInfoUiPrefabPath);
            Debug.Log("[BuildGameplayUiPrefabsTool] Applied gameplay HUD prefabs to PlayerInfoUI.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Tools/UI/Bind PlayerUiManager In Open Scene")]
    public static void BindPlayerUiManagerInOpenScene()
    {
        GameManager gameManager = Object.FindObjectOfType<GameManager>(true);
        if (gameManager == null)
        {
            Debug.LogError("[BuildGameplayUiPrefabsTool] GameManager not found in open scene.");
            return;
        }

        PlayerUiManager playerUi = Object.FindObjectOfType<PlayerUiManager>(true);
        if (playerUi == null)
        {
            GameObject playerInfoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActiveHudPrefabPath);
            if (playerInfoPrefab == null)
            {
                Debug.LogError($"[BuildGameplayUiPrefabsTool] PlayerHUD prefab not found: {ActiveHudPrefabPath}");
                return;
            }

            Canvas canvas = Object.FindObjectOfType<Canvas>(true);
            if (canvas == null)
            {
                Debug.LogError("[BuildGameplayUiPrefabsTool] Canvas not found in open scene. Cannot instantiate PlayerInfoUI.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(playerInfoPrefab) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[BuildGameplayUiPrefabsTool] Failed to instantiate PlayerHUD prefab.");
                return;
            }

            instance.name = "PlayerHUD";
            instance.transform.SetParent(canvas.transform, false);
            playerUi = instance.GetComponent<PlayerUiManager>();
            if (playerUi == null)
            {
                playerUi = instance.GetComponentInChildren<PlayerUiManager>(true);
            }

            if (playerUi == null)
            {
                Debug.LogError("[BuildGameplayUiPrefabsTool] Instantiated PlayerInfoUI but PlayerUiManager component was not found.");
                return;
            }
        }

        SerializedObject gmSo = new SerializedObject(gameManager);
        SerializedProperty prop = gmSo.FindProperty("playerUiManager");
        if (prop == null)
        {
            Debug.LogError("[BuildGameplayUiPrefabsTool] Could not find field: playerUiManager");
            return;
        }

        prop.objectReferenceValue = playerUi;
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(gameManager);
        EditorSceneManager.MarkSceneDirty(gameManager.gameObject.scene);

        Debug.Log("[BuildGameplayUiPrefabsTool] Bound GameManager.playerUiManager to PlayerHUD instance.");
    }

    [MenuItem("Tools/UI/Upgrade Imported PlayerInfoUI (Preserve Layout)")]
    public static void UpgradeImportedPlayerInfoUIPreserveLayout()
    {
        Sprite hpBgSprite = LoadSpriteByName(HpMpBarSpritePath, "hpmpbar_0");
        Sprite hpGaugeSprite = LoadSpriteByName(HpMpBarSpritePath, "hpmpbar_1");
        Sprite staminaGaugeSprite = LoadSpriteByName(HpMpBarSpritePath, "hpmpbar_2");
        Sprite reloadBgSprite = LoadSpriteByName(ReloadBarSpritePath, "HOME");
        Sprite reloadGaugeSprite = LoadSpriteByName(ReloadBarSpritePath, "BAR");
        Sprite skillSprite = LoadSpriteByName(SkillSpritePath, "SKill_0");

        GameObject root = PrefabUtility.LoadPrefabContents(ActiveHudPrefabPath);
        if (root == null)
        {
            Debug.LogError("[BuildGameplayUiPrefabsTool] Failed to load PlayerHUD prefab contents.");
            return;
        }

        try
        {
            UIPlayerHP hp = root.GetComponentInChildren<UIPlayerHP>(true);
            if (hp != null)
            {
                SetObjectField(hp, "hpBackgroundSprite", hpBgSprite);
                SetObjectField(hp, "hpGaugeSprite", hpGaugeSprite);

                Image bgImage = FindImage(hp.transform, "HP_BG");
                Image gauge = FindImage(hp.transform, "HP_BG/HP_Gauge");
                TMP_Text hpText = FindText(hp.transform, "HP_BG/HP_Text");

                if (bgImage != null && hpBgSprite != null)
                    bgImage.sprite = hpBgSprite;

                if (gauge != null)
                {
                    if (hpGaugeSprite != null)
                        gauge.sprite = hpGaugeSprite;
                    gauge.type = Image.Type.Filled;
                    gauge.fillMethod = Image.FillMethod.Horizontal;
                    gauge.fillOrigin = (int)Image.OriginHorizontal.Left;
                    SetObjectField(hp, "hpGauge", gauge);
                }

                if (hpText != null)
                    SetObjectField(hp, "hpText", hpText);
            }

            UiPlayerRoll roll = root.GetComponentInChildren<UiPlayerRoll>(true);
            if (roll != null)
            {
                SetObjectField(roll, "hpBackgroundSprite", hpBgSprite);
                SetObjectField(roll, "staminaGaugeSprite", staminaGaugeSprite);

                Image bgImage = FindImage(roll.transform, "Stamina_BG");
                Image gauge = FindImage(roll.transform, "Stamina_BG/Stamina_Gauge");

                if (bgImage != null && hpBgSprite != null)
                    bgImage.sprite = hpBgSprite;

                if (gauge != null)
                {
                    if (staminaGaugeSprite != null)
                        gauge.sprite = staminaGaugeSprite;
                    gauge.type = Image.Type.Filled;
                    gauge.fillMethod = Image.FillMethod.Horizontal;
                    gauge.fillOrigin = (int)Image.OriginHorizontal.Left;
                    SetObjectField(roll, "dodgeGauge", gauge);
                }
            }

            UIPlayerSkill skill = root.GetComponentInChildren<UIPlayerSkill>(true);
            if (skill != null)
            {
                SetObjectField(skill, "skillSprite", skillSprite);

                Image icon = FindImage(skill.transform, "SkillIcon");
                Image gauge = FindImage(skill.transform, "SkillIcon/SkillGauge");

                if (icon != null)
                {
                    if (skillSprite != null)
                        icon.sprite = skillSprite;
                    SetObjectField(skill, "skillIcon", icon);
                }

                if (gauge != null)
                {
                    gauge.type = Image.Type.Filled;
                    gauge.fillMethod = Image.FillMethod.Radial360;
                    gauge.fillOrigin = (int)Image.Origin360.Top;
                    gauge.fillClockwise = true;
                    SetObjectField(skill, "skillGauge", gauge);
                }
            }

            UIReloadHUD reload = root.GetComponentInChildren<UIReloadHUD>(true);
            if (reload != null)
            {
                SetObjectField(reload, "reloadBackgroundSprite", reloadBgSprite);
                SetObjectField(reload, "reloadGaugeSprite", reloadGaugeSprite);

                Image bgImage = FindImage(reload.transform, "Reload_BG");
                Image gauge = FindImage(reload.transform, "Reload_BG/Reload_Gauge");

                if (bgImage != null && reloadBgSprite != null)
                    bgImage.sprite = reloadBgSprite;

                if (gauge != null)
                {
                    if (reloadGaugeSprite != null)
                        gauge.sprite = reloadGaugeSprite;
                    gauge.type = Image.Type.Simple;
                    SetObjectField(reload, "reloadGauge", gauge);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, ActiveHudPrefabPath);
            Debug.Log("[BuildGameplayUiPrefabsTool] Upgraded PlayerHUD while preserving existing layout/anchors.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void BuildUIPlayerHPPrefab(Sprite hpBgSprite, Sprite hpGaugeSprite)
    {
        GameObject go = NewUiRoot("UIPlayerHP");
        UIPlayerHP comp = go.AddComponent<UIPlayerHP>();

        GameObject bgObj = CreateChildImage(go.transform, "HP_BG", hpBgSprite);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        GameObject gaugeObj = CreateChildImage(bgObj.transform, "HP_Gauge", hpGaugeSprite);
        Image gauge = gaugeObj.GetComponent<Image>();
        gauge.type = Image.Type.Filled;
        gauge.fillMethod = Image.FillMethod.Horizontal;
        gauge.fillOrigin = (int)Image.OriginHorizontal.Left;

        GameObject textObj = CreateChildText(bgObj.transform, "HP_Text", "0 / 0");

        if (hpBgSprite != null)
            SetObjectField(comp, "hpBackgroundSprite", hpBgSprite);
        if (hpGaugeSprite != null)
            SetObjectField(comp, "hpGaugeSprite", hpGaugeSprite);
        SetObjectField(comp, "hpGauge", gauge);
        SetObjectField(comp, "hpText", textObj.GetComponent<TMP_Text>());

        SaveAsPrefab(go, OutputFolder + "/UIPlayerHP.prefab");
    }

    private static void BuildUiPlayerRollPrefab(Sprite hpBgSprite, Sprite staminaGaugeSprite)
    {
        GameObject go = NewUiRoot("UiPlayerRoll");
        UiPlayerRoll comp = go.AddComponent<UiPlayerRoll>();

        GameObject bgObj = CreateChildImage(go.transform, "Stamina_BG", hpBgSprite);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        GameObject gaugeObj = CreateChildImage(bgObj.transform, "Stamina_Gauge", staminaGaugeSprite);
        Image gauge = gaugeObj.GetComponent<Image>();
        gauge.type = Image.Type.Filled;
        gauge.fillMethod = Image.FillMethod.Horizontal;
        gauge.fillOrigin = (int)Image.OriginHorizontal.Left;

        if (hpBgSprite != null)
            SetObjectField(comp, "hpBackgroundSprite", hpBgSprite);
        if (staminaGaugeSprite != null)
            SetObjectField(comp, "staminaGaugeSprite", staminaGaugeSprite);
        SetObjectField(comp, "dodgeGauge", gauge);

        SaveAsPrefab(go, OutputFolder + "/UiPlayerRoll.prefab");
    }

    private static void BuildUIPlayerSkillPrefab(Sprite skillSprite)
    {
        GameObject go = NewUiRoot("UIPlayerSkill");
        UIPlayerSkill comp = go.AddComponent<UIPlayerSkill>();

        GameObject iconObj = CreateChildImage(go.transform, "SkillIcon", skillSprite);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;

        GameObject gaugeObj = CreateChildImage(iconObj.transform, "SkillGauge", null);
        RectTransform gaugeRt = gaugeObj.GetComponent<RectTransform>();
        gaugeRt.anchorMin = Vector2.zero;
        gaugeRt.anchorMax = Vector2.one;
        gaugeRt.offsetMin = Vector2.zero;
        gaugeRt.offsetMax = Vector2.zero;
        Image gauge = gaugeObj.GetComponent<Image>();
        gauge.color = new Color(0f, 0f, 0f, 0.55f);
        gauge.type = Image.Type.Filled;
        gauge.fillMethod = Image.FillMethod.Radial360;
        gauge.fillOrigin = (int)Image.Origin360.Top;
        gauge.fillClockwise = true;

        if (skillSprite != null)
            SetObjectField(comp, "skillSprite", skillSprite);
        SetObjectField(comp, "skillIcon", iconObj.GetComponent<Image>());
        SetObjectField(comp, "skillGauge", gauge);

        SaveAsPrefab(go, OutputFolder + "/UIPlayerSkill.prefab");
    }

    private static void BuildUIReloadHUDPrefab(Sprite reloadBgSprite, Sprite reloadGaugeSprite)
    {
        GameObject go = NewUiRoot("UIReloadHUD");
        UIReloadHUD comp = go.AddComponent<UIReloadHUD>();

        GameObject bgObj = CreateChildImage(go.transform, "Reload_BG", reloadBgSprite);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        GameObject gaugeObj = CreateChildImage(bgObj.transform, "Reload_Gauge", reloadGaugeSprite);
        Image gauge = gaugeObj.GetComponent<Image>();
        gauge.type = Image.Type.Filled;
        gauge.fillMethod = Image.FillMethod.Horizontal;
        gauge.fillOrigin = (int)Image.OriginHorizontal.Left;

        if (reloadBgSprite != null)
            SetObjectField(comp, "reloadBackgroundSprite", reloadBgSprite);
        if (reloadGaugeSprite != null)
            SetObjectField(comp, "reloadGaugeSprite", reloadGaugeSprite);
        SetObjectField(comp, "reloadGauge", gauge);

        SaveAsPrefab(go, OutputFolder + "/UIReloadHUD.prefab");
    }

    private static GameObject NewUiRoot(string name)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject CreateChildImage(Transform parent, string name, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        Image image = go.AddComponent<Image>();
        image.color = Color.white;
        image.sprite = sprite;
        return go;
    }

    private static GameObject CreateChildText(Transform parent, string name, string text)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20f;
        tmp.alignment = TextAlignmentOptions.Right;
        tmp.color = Color.white;
        return go;
    }

    private static void RemoveMissingScriptsRecursive(Transform root)
    {
        if (root == null) return;

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root.gameObject);
        for (int i = 0; i < root.childCount; i++)
            RemoveMissingScriptsRecursive(root.GetChild(i));
    }

    private static T EnsureComponent<T>(Transform target) where T : Component
    {
        if (target == null) return null;

        T comp = target.GetComponent<T>();
        if (comp == null)
            comp = target.gameObject.AddComponent<T>();

        return comp;
    }

    private static Image FindImage(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t == null) return null;
        return t.GetComponent<Image>();
    }

    private static TMP_Text FindText(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t == null) return null;
        return t.GetComponent<TMP_Text>();
    }

    private static void RemoveOldHudNodes(GameObject root)
    {
        RemoveByComponent<UIPlayerHP>(root);
        RemoveByComponent<UiPlayerRoll>(root);
        RemoveByComponent<UIPlayerSkill>(root);
        RemoveByComponent<UIReloadHUD>(root);
    }

    private static void RemoveByComponent<T>(GameObject root) where T : Component
    {
        T[] comps = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            T comp = comps[i];
            if (comp == null) continue;
            if (comp.gameObject == root) continue;

            Object.DestroyImmediate(comp.gameObject);
        }
    }

    private static void AddChildPrefab(Transform parent, GameObject prefab, string fallbackName)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            instance = Object.Instantiate(prefab);
            instance.name = fallbackName;
        }

        instance.transform.SetParent(parent, false);
    }

    private static void SetObjectField(Object target, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning($"[BuildGameplayUiPrefabsTool] Field not found: {fieldName} on {target.GetType().Name}");
        }
    }

    private static Sprite LoadSpriteByName(string texturePath, string spriteName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath);
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite sprite = assets[i] as Sprite;
            if (sprite != null && sprite.name == spriteName)
                return sprite;
        }

        return null;
    }

    private static void SaveAsPrefab(GameObject instance, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        int lastSlash = folderPath.LastIndexOf('/');
        if (lastSlash <= 0)
            return;

        string parent = folderPath.Substring(0, lastSlash);
        string folderName = folderPath.Substring(lastSlash + 1);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
