using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossStage : Stage
{
    [Header("Boss")]
    public GameObject bossPrefab;
    public GameObject bossOBJ;
    [SerializeField] public BossBase BossBase;

    [Header("Choice Result")]
    public RandomHealPoint randomHealPoint;
    public ResultBox resultBox;
    public BloodTransfusionDevice bloodTransfusionDevice;

    [Header("Spawn Point")]
    public GameObject spawnPointStairs;
    public Transform PlayerSpawnPoint;
    public Transform PlayerStartPosition;
    public NextStageStairs NextStageStairs;
    public Transform BossSpawnPoint;
    public SpriteRenderer bossSpawnSprite;
    public SpriteRenderer PlayerMovePointSprite;
    public SpriteRenderer PlayerSpawnPointSprite;

    [Header("Battle Zone")]
    public BoxCollider2D nxnZone;

    [Header("Boss Entry Sequence")]
    [Min(0f)] public float bossNameShowDuration = 1.2f;

    [Header("Boss Reward Rule")]
    [SerializeField, Min(1)] private int transfusionMinFloor = 5;
    [SerializeField, Range(0f, 1f)] private float transfusionSpawnChance = 0.1f;

    private int aliveBossCount;
    private bool hasDefaultResultPosition;
    private Vector3 defaultResultPosition;

    private void OnValidate()
    {
        ConvertAssetReferenceToBossPrefab();

        if (bossPrefab != null && bossPrefab.scene.IsValid())
        {
            Debug.LogWarning($"[BossStage] '{name}' bossPrefab must be a prefab asset, not a scene object. Clearing invalid reference.");
            bossPrefab = null;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        SyncBossReference();

        if (bossOBJ != null)
        {
            if (BossSpawnPoint != null)
                bossOBJ.transform.position = BossSpawnPoint.position;
            bossOBJ.SetActive(false);
        }

        if (bossSpawnSprite != null)
            bossSpawnSprite.color = new Color(0f, 0f, 0f, 0f);

        if (PlayerMovePointSprite != null)
            PlayerMovePointSprite.color = new Color(0f, 0f, 0f, 0f);

        if (PlayerSpawnPointSprite != null)
            PlayerSpawnPointSprite.color = new Color(0f, 0f, 0f, 0f);

        if (resultBox != null)
        {
            defaultResultPosition = resultBox.transform.position;
            hasDefaultResultPosition = true;
        }

    }

    public override Transform GetPlayerSpawnPoint()
    {
        return PlayerSpawnPoint;
    }

    public override void ReadyStage()
    {
        base.ReadyStage();
        aliveBossCount = 0;
        ResultSummon();

        PrepareBossForRuntime();
        if (topDoor != null)
            topDoor.Lock();

        if (GameManager.Instance != null)
            GameManager.Instance.BossCountSet(0);
    }

    public override void InCheckClear(GameObject player)
    {
        if (!TryConsumeFirstIn())
            return;

        Debug.Log($"[BossStage] InCheckClear on '{name}' | player={player?.name}");

        CloseBotDoor();
        StartCoroutine(BossRoomEnterSequence(player));
    }

    private void ConvertAssetReferenceToBossPrefab()
    {
        if (bossOBJ != null && !bossOBJ.scene.IsValid())
        {
            if (bossPrefab == null)
                bossPrefab = bossOBJ;

            Debug.LogWarning($"[BossStage] '{name}' bossOBJ had a prefab asset reference. Moved it to bossPrefab and cleared bossOBJ.");
            bossOBJ = null;
            BossBase = null;
        }
    }

    private void CreateBossInstanceFromPrefab()
    {
        if (bossOBJ != null || bossPrefab == null)
            return;

        Vector3 spawnPos = BossSpawnPoint != null ? BossSpawnPoint.position : transform.position;
        bossOBJ = Instantiate(bossPrefab, spawnPos, Quaternion.identity, transform);
        bossOBJ.name = bossPrefab.name;
        BossBase = bossOBJ.GetComponent<BossBase>();

        if (BossBase == null)
            Debug.LogWarning($"[BossStage] '{name}' instantiated boss prefab has no BossBase: {bossOBJ.name}");

        Debug.Log($"[BossStage] Created runtime boss instance on '{name}' from prefab '{bossPrefab.name}'.");
    }

    public void PrepareBossForRuntime()
    {
        SyncBossReference();

        if (bossOBJ == null)
            CreateBossInstanceFromPrefab();

        SyncBossReference();

        if (bossOBJ != null)
        {
            if (BossSpawnPoint != null)
                bossOBJ.transform.position = BossSpawnPoint.position;

            if (BossBase != null)
                BossBase.StageOwner = this;

            bossOBJ.SetActive(false);
        }
    }

    private void SyncBossReference()
    {
        ConvertAssetReferenceToBossPrefab();

        if (bossOBJ != null)
        {
            BossBase = bossOBJ.GetComponent<BossBase>();

            if (BossBase == null)
            {
                BossBase childBoss = GetComponentInChildren<BossBase>(true);
                if (childBoss != null)
                {
                    BossBase = childBoss;
                    bossOBJ = childBoss.gameObject;
                }
            }

            return;
        }

        BossBase fallbackBoss = GetComponentInChildren<BossBase>(true);
        if (fallbackBoss != null)
        {
            BossBase = fallbackBoss;
            bossOBJ = fallbackBoss.gameObject;
        }
        else
        {
            BossBase = null;
        }
    }

    private IEnumerator BossRoomEnterSequence(GameObject player)
    {
        BossBase summonedBoss = SummonBossObjectOnly();
        MovePlayerInside(player);

        if (summonedBoss == null)
        {
            Debug.LogError("[BossStage] SummonBossObjectOnly returned null!");
            yield break;
        }

        summonedBoss.StatSet();

        float nameDuration = summonedBoss.IntroTime > 0f ? summonedBoss.IntroTime : bossNameShowDuration;
        StartCoroutine(ShowBossNameRoutine(summonedBoss.GetDisplayName(), nameDuration));
        BeginBossBattle(summonedBoss);
    }

    private void MovePlayerInside(GameObject player)
    {
        if (player == null || PlayerStartPosition == null)
            return;

        player.transform.position = PlayerStartPosition.position;

        MainCamera mc = Camera.main != null ? Camera.main.GetComponent<MainCamera>() : null;
        if (mc != null)
        {
            mc.FocusOnPlayerInstant();
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    public void SummonBoss()
    {
        BossBase summonedBoss = SummonBossObjectOnly();
        if (summonedBoss == null)
            return;

        summonedBoss.StatSet();
        BeginBossBattle(summonedBoss);
    }

    private BossBase SummonBossObjectOnly()
    {
        PrepareBossForRuntime();
        if (BossBase == null || bossOBJ == null)
        {
            Debug.LogWarning($"[BossStage] SummonBoss failed on '{name}': bossOBJ/BossBase is null. Assign bossPrefab or a boss scene instance.");
            return null;
        }

        if (BossSpawnPoint != null)
            bossOBJ.transform.position = BossSpawnPoint.position;

        BossBase.StageOwner = this;
        bossOBJ.SetActive(true);
        BossBase.OnBossActivatedBeforeIntro();

        Debug.Log($"[BossStage] Boss object activated on '{name}' => {bossOBJ.name}");
        return BossBase;
    }

    private void BeginBossBattle(BossBase boss)
    {
        if (boss == null)
            return;

        RegisterBossSpawnCount(boss.bossCount);

        // 보스 인트로 연출 카메라 추적 및 플레이어 조작 차단
        MainCamera mc = Camera.main != null ? Camera.main.GetComponent<MainCamera>() : null;
        if (mc != null && boss.IntroTime > 0f)
        {
            if (boss is TheWorm && BossSpawnPoint != null)
            {
                mc.StartBossIntroTracking(BossSpawnPoint.gameObject, boss.IntroTime);
            }
            else
            {
                mc.StartBossIntroTracking(boss.gameObject, boss.IntroTime);
            }
        }

        Debug.Log($"[BossStage] Boss battle started on '{name}' => {boss.name}");
    }

    public override void RegisterBossSpawnCount(int count)
    {
        if (count <= 0)
            return;

        aliveBossCount += count;

        if (GameManager.Instance != null)
            GameManager.Instance.BossCountAdd(count);
    }

    public override void NotifyBossDied(BossBase deadBoss, int count)
    {
        if (count <= 0)
            count = 1;

        aliveBossCount = Mathf.Max(0, aliveBossCount - count);

        if (aliveBossCount <= 0)
            OpenTopDoor();
    }

    private IEnumerator ShowBossNameRoutine(string bossDisplayName, float duration)
    {
        if (duration <= 0f)
            yield break;

        Canvas canvas = FindObjectOfType<Canvas>(true);
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject titleObj = new GameObject("BossNameTitle", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = titleObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.2f); // 중앙 하단으로 내려 보스와 겹치지 않게 방지
        rt.anchorMax = new Vector2(0.5f, 0.2f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1000f, 120f);

        Text txt = titleObj.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 44;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = string.IsNullOrEmpty(bossDisplayName) ? "BOSS" : bossDisplayName;

        yield return new WaitForSeconds(duration);

        if (titleObj != null)
            Destroy(titleObj);
    }

    public void ResultSummon()
    {
        if (randomHealPoint != null)
            randomHealPoint.MakePotion();
        else
            Debug.LogWarning($"[BossStage] randomHealPoint is null on '{name}'.");

        bool shouldShowTransfusion = ShouldShowTransfusionForFloor();

        if (bloodTransfusionDevice != null)
        {
            bloodTransfusionDevice.gameObject.SetActive(shouldShowTransfusion);
            if (shouldShowTransfusion)
                bloodTransfusionDevice.Init($"Stage-{roomNumber}", bloodTransfusionDevice.name);
        }

        if (resultBox != null)
        {
            if (!shouldShowTransfusion && bloodTransfusionDevice != null)
                resultBox.transform.position = bloodTransfusionDevice.transform.position;
            else if (hasDefaultResultPosition)
                resultBox.transform.position = defaultResultPosition;
            
            resultBox.forceDNA = true; // 보스방 확정 DNA
            resultBox.gameObject.SetActive(true);
        }
        else
            Debug.LogWarning($"[BossStage] resultBox is null on '{name}'.");
    }

    private bool ShouldShowTransfusionForFloor()
    {
        int floor = Mathf.Max(1, roomNumber + 1);
        if (floor < transfusionMinFloor)
            return false;

        return Random.value <= transfusionSpawnChance;
    }

    public override Vector2 GetRandomPositionInZone()
    {
        if (nxnZone == null)
        {
            Debug.LogWarning($"[BossStage] '{name}' nxnZone is not assigned. Returning stage center.");
            return transform.position;
        }

        Bounds bounds = nxnZone.bounds;
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);
        return new Vector2(randomX, randomY);
    }

    public override bool IsPositionInZone(Vector2 position)
    {
        if (nxnZone == null)
            return false;

        return nxnZone.bounds.Contains(position);
    }

    public override Vector2 GetZoneCenter()
    {
        if (nxnZone == null)
            return transform.position;

        return nxnZone.bounds.center;
    }

    public override Bounds GetZoneBounds()
    {
        if (nxnZone == null)
            return new Bounds(transform.position, new Vector3(10f, 10f, 0f));

        return nxnZone.bounds;
    }
}
