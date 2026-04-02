using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Stage : MonoBehaviour
{
    public int roomNumber;
    [Header("Boss")]
    public GameObject bossPrefab;
    public GameObject bossOBJ;
    [SerializeField]public BossBase BossBase;

    [Header("Door")]
    public Door botDoor;
    public Door topDoor;

    [Header("Choice Result")]
    public RandomHealPoint randomHealPoint;
    public ResultDNA resultDNA;

    [Header("Spawn Point")]
    public GameObject spawnPointStairs;
    public Transform PlayerSpawnPoint;
    public Transform PlayerStartPosition;
    public NextStageStairs NextStageStairs;
    public Transform BossSpawnPoint;
    public SpriteRenderer bossSpawnSprite;
    public SpriteRenderer PlayerMovePointSprite;

    [Header("Boss Entry Sequence")]
    [Min(0f)] public float bossNameShowDuration = 1.2f;


    private bool firstIn;
    private int aliveBossCount;

    private void OnValidate()
    {
        ConvertAssetReferenceToBossPrefab();

        if (bossPrefab != null && bossPrefab.scene.IsValid())
        {
            Debug.LogWarning($"[Stage] '{name}' bossPrefab must be a prefab asset, not a scene object. Clearing invalid reference.");
            bossPrefab = null;
        }
    }

    private void ConvertAssetReferenceToBossPrefab()
    {
        // 실수로 bossOBJ 슬롯에 프리팹 에셋을 넣은 경우 bossPrefab으로 이관한다.
        if (bossOBJ != null && !bossOBJ.scene.IsValid())
        {
            if (bossPrefab == null)
                bossPrefab = bossOBJ;

            Debug.LogWarning($"[Stage] '{name}' bossOBJ had a prefab asset reference. Moved it to bossPrefab and cleared bossOBJ.");
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
            Debug.LogWarning($"[Stage] '{name}' instantiated boss prefab has no BossBase: {bossOBJ.name}");

        Debug.Log($"[Stage] Created runtime boss instance on '{name}' from prefab '{bossPrefab.name}'.");
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

            // bossOBJ가 잘못 연결되어 BossBase가 없는 경우, 자식에서 재탐색
            if (BossBase == null)
            {
                var childBoss = GetComponentInChildren<BossBase>(true);
                if (childBoss != null)
                {
                    BossBase = childBoss;
                    bossOBJ = childBoss.gameObject;
                }
            }

            return;
        }

        // Stage 프리팹에서 bossOBJ가 비어있는 경우 대비
        var fallbackBoss = GetComponentInChildren<BossBase>(true);
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


    private void Awake()
    {
        SyncBossReference();

        if (bossOBJ != null)
        {
            if (BossSpawnPoint != null)
                bossOBJ.transform.position = BossSpawnPoint.position;
            bossOBJ.SetActive(false);
        }

        if (bossSpawnSprite != null)
            bossSpawnSprite.color = new Color(0, 0, 0, 0);

        if (PlayerMovePointSprite != null)
            PlayerMovePointSprite.color = new Color(0, 0, 0, 0);

        ResultSummon();
        firstIn = true;
        ObjActiveFalse();
    }
    public void ReadyStage() 
    {
        PrepareBossForRuntime();
        ObjActiveTrue();
        aliveBossCount = 0;
        // 위 문은 보스 처치 전까지 잠금 (근접 감지 비활성)
        topDoor.Lock();
        // botDoor는 잠금 없이 활성 상태 유지 → 근접 시 자동 열림
        if (GameManager.Instance != null)
            GameManager.Instance.BossCountSet(0);
    }
    //public void NextGo(GameObject player)  ���� ��¼�ٺ��� �ؽ�Ʈ�����������׾�� ���ӸŴ��� ȣ���ؼ� ������
    //{
    //    player.transform.position = GameManager.Instance.StageTree[roomNumber + 1].PlayerSpawnPoint.position;
    //}

    public void InCheckClear(GameObject player)
    {
        if (firstIn)
        {
            Debug.Log($"[Stage] InCheckClear on '{name}' | player={player?.name}");
            // 문 잠금을 먼저 처리 — 보스 세팅 예외와 무관하게 반드시 실행
            CloseBotDoor();
            firstIn = false;

            StartCoroutine(BossRoomEnterSequence(player));
        }
    }

    private IEnumerator BossRoomEnterSequence(GameObject player)
    {
        BossBase summonedBoss = SummonBossObjectOnly();
        MovePlayerInside(player);

        if (summonedBoss == null)
            yield break;

        yield return ShowBossNameRoutine(summonedBoss.GetDisplayName(), bossNameShowDuration);

        BeginBossBattle(summonedBoss);
    }

    private void MovePlayerInside(GameObject player)
    {
        if (player == null || PlayerStartPosition == null) return;

        player.transform.position = PlayerStartPosition.position;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    public void SummonBoss()
    {
        BossBase summonedBoss = SummonBossObjectOnly();
        if (summonedBoss == null)
            return;

        BeginBossBattle(summonedBoss);
    }

    private BossBase SummonBossObjectOnly()
    {
        PrepareBossForRuntime();
        if (BossBase == null || bossOBJ == null)
        {
            Debug.LogWarning($"[Stage] SummonBoss failed on '{name}': bossOBJ/BossBase is null. Assign bossPrefab or a boss scene instance.");
            return null;
        }

        if (BossSpawnPoint != null)
            bossOBJ.transform.position = BossSpawnPoint.position;

        BossBase.StageOwner = this;
        bossOBJ.SetActive(true);
        BossBase.OnBossActivatedBeforeIntro();

        Debug.Log($"[Stage] Boss object activated on '{name}' => {bossOBJ.name}");
        return BossBase;
    }

    private void BeginBossBattle(BossBase boss)
    {
        if (boss == null)
            return;

        boss.StatSet();
        RegisterBossSpawnCount(boss.bossCount);

        Debug.Log($"[Stage] Boss battle started on '{name}' => {boss.name}");
    }

    public void RegisterBossSpawnCount(int count)
    {
        if (count <= 0)
            return;

        aliveBossCount += count;

        if (GameManager.Instance != null)
            GameManager.Instance.BossCountAdd(count);
    }

    public void NotifyBossDied(BossBase deadBoss, int count)
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
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
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
            Debug.LogWarning($"[Stage] randomHealPoint is null on '{name}'.");

        if (resultDNA != null)
            resultDNA.Init();
        else
            Debug.LogWarning($"[Stage] resultDNA is null on '{name}'.");
    }
    public void ObjActiveTrue() 
    {
        this.gameObject.SetActive(true);
    }
    public void ObjActiveFalse() 
    {
        this.gameObject.SetActive(false);
    }

    public void OpenBotDoor() 
    {
        botDoor.UnLock();
    }
    public void CloseBotDoor() 
    {
        botDoor.Lock();
    }
    public void OpenTopDoor() 
    {
        topDoor.UnLock();
    }
    public void CloseTopDoor() 
    {
        topDoor.Lock();
    }

}
