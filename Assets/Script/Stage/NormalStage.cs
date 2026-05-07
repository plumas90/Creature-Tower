using UnityEngine;

public class NormalStage : Stage
{
    protected override bool DefaultBossFlow => false;

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

    [Header("Monster Case")]
    [SerializeField] private Transform normalMonsterRoot;
    [SerializeField] private int requiredMonsterCount = 0;

    [Header("Optional Content")]
    [SerializeField] private GameObject shopRoot;

    private int remainingMonsterCount;
    private bool monsterGateActive;

    public override void ReadyStage()
    {
        base.ReadyStage();

        ConfigureOptionalContents();
        SetupMonsterGate();
    }

    public override void InCheckClear(GameObject player)
    {
        base.InCheckClear(player);

        // 몬스터 처치형 노말 스테이지는 입장 후 뒤쪽 문을 닫아 전투 공간을 고정한다.
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
