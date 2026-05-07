using UnityEngine;

public class NormalStage : Stage
{
    public enum NormalStageCase
    {
        Monster,
        RewardOnly,
        TransfusionOnly,
        ShopOnly,
        ShopAndTransfusion
    }

    [Header("Normal Stage Case")]
    [SerializeField] private NormalStageCase stageCase = NormalStageCase.RewardOnly;

    [Header("Choice Result")]
    public RandomHealPoint randomHealPoint;
    public ResultDNA resultDNA;
    public BloodTransfusionDevice bloodTransfusionDevice;

    [Header("Spawn Point")]
    public GameObject spawnPointStairs;
    public Transform PlayerSpawnPoint;
    public Transform PlayerStartPosition;
    public NextStageStairs NextStageStairs;

    [Header("Monster Case")]
    [SerializeField] private Transform normalMonsterRoot;
    [SerializeField] private int requiredMonsterCount = 0;

    [Header("Optional Content")]
    [SerializeField] private GameObject shopRoot;

    private int remainingMonsterCount;
    private bool monsterGateActive;

    protected override void Awake()
    {
        base.Awake();
        ResultSummon();
    }

    public override Transform GetPlayerSpawnPoint()
    {
        return PlayerSpawnPoint;
    }

    public override void ReadyStage()
    {
        base.ReadyStage();
        ConfigureOptionalContents();
        SetupMonsterGate();

        if (GameManager.Instance != null)
            GameManager.Instance.BossCountSet(0);
    }

    public override void InCheckClear(GameObject player)
    {
        base.InCheckClear(player);

        if (monsterGateActive && botDoor != null)
            CloseBotDoor();
    }

    public void NotifyNormalMonsterDied(int count = 1)
    {
        if (!monsterGateActive)
            return;

        if (count <= 0)
            count = 1;

        remainingMonsterCount = Mathf.Max(0, remainingMonsterCount - count);
        if (remainingMonsterCount == 0)
        {
            OpenTopDoor();
            if (botDoor != null)
                OpenBotDoor();
            monsterGateActive = false;
        }
    }

    public void ResultSummon()
    {
        if (randomHealPoint != null)
            randomHealPoint.MakePotion();
        else
            Debug.LogWarning($"[NormalStage] randomHealPoint is null on '{name}'.");

        if (resultDNA != null)
            resultDNA.Init();
        else
            Debug.LogWarning($"[NormalStage] resultDNA is null on '{name}'.");

        if (bloodTransfusionDevice != null)
            bloodTransfusionDevice.Init($"Stage-{roomNumber}", bloodTransfusionDevice.name);
    }

    private void ConfigureOptionalContents()
    {
        bool enableReward = stageCase == NormalStageCase.RewardOnly;
        bool enableTransfusion = stageCase == NormalStageCase.TransfusionOnly || stageCase == NormalStageCase.ShopAndTransfusion;
        bool enableShop = stageCase == NormalStageCase.ShopOnly || stageCase == NormalStageCase.ShopAndTransfusion;

        if (resultDNA != null)
            resultDNA.gameObject.SetActive(enableReward);

        if (bloodTransfusionDevice != null)
            bloodTransfusionDevice.gameObject.SetActive(enableTransfusion);

        if (shopRoot != null)
            shopRoot.SetActive(enableShop);
    }

    private void SetupMonsterGate()
    {
        if (stageCase != NormalStageCase.Monster)
        {
            remainingMonsterCount = 0;
            monsterGateActive = false;
            OpenTopDoor();
            if (botDoor != null)
                OpenBotDoor();
            return;
        }

        remainingMonsterCount = ResolveRequiredMonsterCount();
        monsterGateActive = remainingMonsterCount > 0;

        if (monsterGateActive)
            CloseTopDoor();
        else
            OpenTopDoor();
    }

    private int ResolveRequiredMonsterCount()
    {
        if (requiredMonsterCount > 0)
            return requiredMonsterCount;

        if (normalMonsterRoot == null)
            return 0;

        int count = 0;
        NormalStageMonsterMarker[] markers = normalMonsterRoot.GetComponentsInChildren<NormalStageMonsterMarker>(true);
        for (int i = 0; i < markers.Length; i++)
        {
            NormalStageMonsterMarker marker = markers[i];
            if (marker == null)
                continue;

            if (marker.gameObject.activeInHierarchy)
                count++;
        }

        return count;
    }
}
