using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ResultManager : MonoBehaviour//vs�ڵ�
{
    private enum ResultSelectionMode
    {
        Immediate,
        TransfusionPendingConfirm
    }

    public ChoiceSlot[] picklist;
    public static ResultManager Instance;
    List<SpecialAugment> tempList = new List<SpecialAugment>();
    private bool IsStat;
    public List<IAugment> stat1;
    public List<IAugment> stat2;
    public List<IAugment> stat3;
    public GameObject MySpecialList;
    bool SeeNowMyList;
    PlayerInput playerinput;

    public bool readycheck;

    public List<SpecialAugment> SpecialAugment1 = new List<SpecialAugment>();
    public List<SpecialAugment> SpecialAugment2 = new List<SpecialAugment>();
    public List<SpecialAugment> SpecialAugment3 = new List<SpecialAugment>();
    public List<SpecialAugment> ProtoList = new List<SpecialAugment>();
    public GameObject Player;


    public MySpecialListSocket Socketprefab;
    public Transform ViewListContent;


    public bool statChance;

    bool testsetting;
    public bool SetActiveCheck;
    private bool controlsLocked;
    private ResultSelectionMode selectionMode = ResultSelectionMode.Immediate;
    private ChoiceSlot pendingChoiceSlot;
    private Action<bool, int> transfusionCloseCallback;

    [Header("Transfusion UI")]
    [SerializeField] private Button transfusionConfirmButton;
    [SerializeField] private Button transfusionCancelButton;
    [SerializeField] private GameObject transfusionCostRoot;
    [SerializeField] private TextMeshProUGUI transfusionCostText;
    [SerializeField] [Range(0.01f, 0.9f)] private float transfusionHpCostPercent = 0.1f;
    [SerializeField] private Color transfusionSelectedColor = Color.white;
    [SerializeField] private Color transfusionUnselectedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    private readonly Dictionary<string, List<IAugment>> transfusionOptionCache = new Dictionary<string, List<IAugment>>();
    private string currentTransfusionCacheKey;

    public void OpenSpecialResult(GameObject playerObj)
    {
        if (playerObj == null)
        {
            Debug.LogWarning("[ResultManager] OpenSpecialResult failed: player is null.");
            return;
        }

        if (AugmentManager.Instance == null || MakeAugmentListManager.Instance == null)
        {
            Debug.LogWarning("[ResultManager] OpenSpecialResult failed: required manager instance is missing.");
            return;
        }

        // ResultDNA 획득 시점에도 항상 현재 플레이어 기준으로 매니저를 재동기화
        AugmentManager.Instance.startset(playerObj);
        MakeAugmentListManager.Instance.startset(playerObj);
        startset(playerObj);
        StartSet();
        SpecialResult();
    }

    public void OpenTransfusionResult(GameObject playerObj, string cacheKey, Action<bool, int> onClosed)
    {
        if (playerObj == null || AugmentManager.Instance == null || MakeAugmentListManager.Instance == null)
            return;

        if (string.IsNullOrEmpty(cacheKey))
            cacheKey = "default";

        AugmentManager.Instance.startset(playerObj);
        MakeAugmentListManager.Instance.startset(playerObj);
        startset(playerObj);
        StartSet();

        selectionMode = ResultSelectionMode.TransfusionPendingConfirm;
        pendingChoiceSlot = null;
        transfusionCloseCallback = onClosed;
        currentTransfusionCacheKey = cacheKey;

        if (!transfusionOptionCache.TryGetValue(cacheKey, out List<IAugment> options) || options == null || options.Count == 0)
        {
            options = BuildTransfusionOptions();
            transfusionOptionCache[cacheKey] = options;
        }

        ShowTransfusionOptions(options);
        LockPlayerControls();
    }

    public void startset(GameObject playerObj)
    {
        Player = playerObj;
        IsStat = false;
        SetActiveCheck = false;
        //if (MainGameManager.Instance != null) TO DEL��� ���� �κ� if�� ��ü�� ������ �ȴٰ� �Ǵܵ�
        //{
        //    gameManager = MainGameManager.Instance;
        //    gameManager.OnGameEndedEvent += Result;
        //}
        //GameManager.Instance.OnRoomEndEvent += CallStatResult;
        //GameManager.Instance.OnStageEndEvent += SpecialResult;
        //GameManager.Instance.OnBossStageEndEvent += SpecialResult;
        SeeNowMyList = false;
        //pv = GetComponent<PhotonView>();
        playerinput = Player.GetComponent<PlayerInput>();
    }
    void Awake()
    {
        if (null == Instance)
        {
            Instance = this;

            //DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
        }
        testsetting = false;
        SetTransfusionUiVisible(false);

        if (transfusionConfirmButton != null)
            transfusionConfirmButton.onClick.AddListener(ConfirmTransfusionSelection);

        if (transfusionCancelButton != null)
            transfusionCancelButton.onClick.AddListener(CancelTransfusionSelection);

        EnsureTransfusionUiReferences();
        }

    private void OnDisable()
    {
        if (selectionMode == ResultSelectionMode.TransfusionPendingConfirm)
            FinalizeTransfusionSession(false, -1);
    }
    public void StartSet()
    {
        stat1 = MakeAugmentListManager.Instance.stat1;
        stat2 = MakeAugmentListManager.Instance.stat2;
        stat3 = MakeAugmentListManager.Instance.stat3;

        SpecialAugment1 = MakeAugmentListManager.Instance.SpecialAugment1;
        SpecialAugment2 = MakeAugmentListManager.Instance.SpecialAugment2;
        SpecialAugment3 = MakeAugmentListManager.Instance.SpecialAugment3;
        
        //GameManager.Instance.OnBossStageStartEvent += ReadyCheck; ��Ƽ���� ������ �� üũ�ؾ� �Ѿ�ºκ� �̱��̶� ��������
        //GameManager.Instance.OnStageStartEvent += ReadyCheck;
        ProtoList = MakeAugmentListManager.Instance.Prototype;
        statChance = false;
    }
    public void ReadyCheck() 
    {
        readycheck = false;
    }

    public void SpecialResult()
    {
        selectionMode = ResultSelectionMode.Immediate;
        pendingChoiceSlot = null;
        transfusionCloseCallback = null;
        currentTransfusionCacheKey = null;
        SetTransfusionUiVisible(false);
        if (!testsetting)//���� �׽�Ʈ�� �����ũ �׽�Ʈ true�� ������Ÿ�Ը���Ʈ��������
        {
            CallSpecialResult();
        }
        else 
        {
            CallProtoResult();//�ְ� �׽�Ʈ ����� ����
        }
    }
    public void CallProtoResult()//������Ÿ�Կ� ���� �θ��� ����Ʈ�� ������� �ʱ� ������ ����ִ�
    {
        PickSpecialList(ProtoList);
    }
    private int RandomTier() 
    {
        //int tier = GameManager.Instance.curStage;
        int tier = 1; //�ӽ�
        int random = Random.Range(1, 12); // ���� ������ ����Ͽ� Ƽ�� ����ġ Ÿ��3�� �־��µ� �ʿ��� ���� 10-������ ��
        int target1 = 4;
        int target2 = 3;
        int target3 = 2;
        if (tier <= 6 && tier >= 4)
        {
            target1 = 3;
            target2 = 4;
            target3 = 2;
        }
        else if (tier >= 6)
        {
            target1 = 2;
            target2 = 3;
            target3 = 4;
        }
        int type = 0;
        if (random <= target1)
        {
            type = 1;
        }
        else if (random <= target1 + target2)
        {
            type = 2;
        }
        else if (random <= target1 + target2 + target3)
        {
            type = 3;
        }
        else 
        {
            type = 4;
        }
        return type;
    }
    public void CallStatResult() 
    {
        Invoke("CallStatResultWindow",0.5f);
    }
    public void CallStatResultWindow() 
    {
        int tier = RandomTier();
        if (tier <= 3)
        {
            switch (tier)
            {
                case 1:
                    PickStatList(stat1);
                    break;

                case 2:
                    PickStatList(stat2);
                    break;

                case 3:
                    PickStatList(stat3);
                    break;
            }
        }
        else 
        {
            int chance = Random.Range(1, 11);
            statChance = true;
            if (chance > 6)
            {
                PickSpecialList(SpecialAugment2);
            }
            else 
            {
                PickSpecialList(SpecialAugment1);
            }
        }
    }
    public void CallSpecialResult()
    {
        PickSpecialListBySlotTier();
    }
  
    void PickStatList(List<IAugment> origin)// ������ �Ȼ縮���� Ÿ�� = �Ϲݽ���
    {
        LockPlayerControls();
        int Count = picklist.Length;
        //���⼭ ������������ Ư�� ���������� ����������Ʈ���� �׳� ������
        List<IAugment> list = origin.ToList();
        if (list.Count == 0)
        {
            Debug.LogWarning("[ResultManager] PickStatList failed: source list is empty.");
            return;
        }

        int selectableCount = Mathf.Min(Count, list.Count);
        for (int i = 0; i < selectableCount; ++i)
        {
            int a = Random.Range(0, list.Count);
            picklist[i].Parent = this;
            picklist[i].stat = list[a];
            picklist[i].SetSelected(false, transfusionUnselectedColor);
            picklist[i].gameObject.SetActive(true);
            list.RemoveAt(a);
        }
        for (int i = selectableCount; i < Count; ++i)
            picklist[i].gameObject.SetActive(false);

        IsStat = true;// �̰ɷ� ����Ʈ���� �������� �״������ ������
        SetActiveCheck = true;
    }

    void PickSpecialList(List<SpecialAugment> origin) // ������ ������� Ÿ�� == �÷��̺�ȭ ����
    {
        LockPlayerControls();
        int Count = picklist.Length;
        List<SpecialAugment> list = origin.ToList();
        if (list.Count == 0)
        {
            Debug.LogWarning("[ResultManager] PickSpecialList failed: source list is empty.");
            return;
        }

        tempList = origin;
        int selectableCount = Mathf.Min(Count, list.Count);
        for (int i = 0; i < selectableCount; ++i)
        {
            int a = Random.Range(0, list.Count);
            picklist[i].Parent = this;
            picklist[i].stat = list[a];
            picklist[i].SetSelected(false, transfusionUnselectedColor);
            picklist[i].gameObject.SetActive(true);
            list.RemoveAt(a);
        }
        for (int i = selectableCount; i < Count; ++i)
            picklist[i].gameObject.SetActive(false);

        SetActiveCheck = true;
        IsStat = false;

    }

    void PickSpecialListBySlotTier()
    {
        LockPlayerControls();

        int count = picklist.Length;
        HashSet<int> usedCodes = new HashSet<int>();

        for (int i = 0; i < count; ++i)
        {
            SpecialAugment pickedAugment = null;

            // 각 슬롯마다 독립적으로 티어를 굴린다.
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int tier = RandomTier();
                List<SpecialAugment> source = ResolveSpecialTierList(tier);
                if (source == null || source.Count == 0)
                    continue;

                int index = Random.Range(0, source.Count);
                SpecialAugment candidate = source[index];
                if (candidate == null || usedCodes.Contains(candidate.Code))
                    continue;

                pickedAugment = candidate;
                break;
            }

            // 중복/빈 리스트로 못 고른 경우, 남은 전체 풀에서 보정
            if (pickedAugment == null)
            {
                List<SpecialAugment> fallbackPool = BuildSpecialFallbackPool(usedCodes);
                if (fallbackPool.Count > 0)
                    pickedAugment = fallbackPool[Random.Range(0, fallbackPool.Count)];
            }

            if (pickedAugment == null)
            {
                picklist[i].gameObject.SetActive(false);
                continue;
            }

            usedCodes.Add(pickedAugment.Code);
            picklist[i].Parent = this;
            picklist[i].stat = pickedAugment;
            picklist[i].SetSelected(false, transfusionUnselectedColor);
            picklist[i].gameObject.SetActive(true);
        }

        SetActiveCheck = true;
        IsStat = false;
    }

    private List<SpecialAugment> ResolveSpecialTierList(int tier)
    {
        switch (tier)
        {
            case 1:
                return SpecialAugment1;
            case 2:
                return SpecialAugment2;
            case 3:
            case 4:
                return SpecialAugment3;
            default:
                return SpecialAugment1;
        }
    }

    private List<SpecialAugment> BuildSpecialFallbackPool(HashSet<int> usedCodes)
    {
        List<SpecialAugment> pool = new List<SpecialAugment>();
        AddSpecialPoolCandidates(pool, SpecialAugment1, usedCodes);
        AddSpecialPoolCandidates(pool, SpecialAugment2, usedCodes);
        AddSpecialPoolCandidates(pool, SpecialAugment3, usedCodes);
        return pool;
    }

    private void AddSpecialPoolCandidates(List<SpecialAugment> pool, List<SpecialAugment> source, HashSet<int> usedCodes)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            SpecialAugment aug = source[i];
            if (aug == null)
                continue;

            if (usedCodes.Contains(aug.Code))
                continue;

            pool.Add(aug);
        }
    }
    public void close()//��Ͽ��� ����ٸ� ��� ui�� �ݾ���
    {
        if (selectionMode == ResultSelectionMode.TransfusionPendingConfirm)
        {
            CancelTransfusionSelection();
            return;
        }

        int Count = picklist.Length;
        for (int i = 0; i < Count; ++i)
        {
            if (picklist[i].Ispick && !IsStat)
            {
                int target= picklist[i].stat.Code;
                List<SpecialAugment> selectedPool = FindSpecialPoolByCode(target);
                if (selectedPool == null)
                    selectedPool = tempList;

                int index = selectedPool != null ? selectedPool.FindIndex(x => x.Code.Equals(target)) : -1;
                if (index >= 0)
                {
                    //����Ʈ���� �̸� ã�Ƽ� ����
                    MySpecialListSocket newSocket = Instantiate(Socketprefab);//
                    newSocket.transform.SetParent(ViewListContent,false);//���������� �����ϸ鼭 �������� �����ɼ��� ���� �¾��� �����������޽��ϴϱ��ذ��
                    newSocket.Init(selectedPool[index].Name, selectedPool[index].func, selectedPool[index].Rare, selectedPool[index].Code);

                    selectedPool.Remove(selectedPool[index]);
                    if (selectedPool.Count <= 2 && !selectedPool.Exists(x => x.Code == 999))
                    {
                        SpecialAugment AllStat = new SpecialAugment("All Stat",999,"���ذ� ���� �ý���", 3);
                        selectedPool.Add(AllStat);
                    }
                }
            }
            picklist[i].gameObject.SetActive(false);
             
        }
        if (!IsStat && !statChance)
        {
            Ready();
        }
        statChance = false;
        SetActiveCheck = false;
        UnlockPlayerControls();
    }

    public void CancelPendingSelection()
    {
        if (selectionMode == ResultSelectionMode.TransfusionPendingConfirm)
        {
            CancelTransfusionSelection();
            return;
        }

        int count = picklist != null ? picklist.Length : 0;
        for (int i = 0; i < count; i++)
        {
            if (picklist[i] == null)
                continue;

            picklist[i].Ispick = false;
            picklist[i].gameObject.SetActive(false);
        }

        SetActiveCheck = false;
        statChance = false;
        IsStat = false;

        if (MySpecialList != null)
            MySpecialList.SetActive(false);
        SeeNowMyList = false;

        UnlockPlayerControls();
    }

    public void OnChoiceSlotClicked(ChoiceSlot slot)
    {
        if (slot == null || slot.stat == null)
            return;

        if (selectionMode != ResultSelectionMode.TransfusionPendingConfirm)
        {
            int code = slot.stat.Code;
            AugmentManager.Instance.AugmentCall(code);
            slot.Ispick = true;
            close();
            if (slot.Parent != null)
                slot.Parent.SetActiveCheck = false;
            return;
        }

        pendingChoiceSlot = slot;
        for (int i = 0; i < picklist.Length; i++)
        {
            ChoiceSlot current = picklist[i];
            if (current == null || !current.gameObject.activeSelf)
                continue;

            bool selected = current == slot;
            current.Ispick = selected;
            current.SetSelected(selected, selected ? transfusionSelectedColor : transfusionUnselectedColor);
        }

        UpdateTransfusionConfirmInteractable();
    }

    private List<SpecialAugment> FindSpecialPoolByCode(int code)
    {
        if (SpecialAugment1 != null && SpecialAugment1.Exists(x => x.Code == code))
            return SpecialAugment1;
        if (SpecialAugment2 != null && SpecialAugment2.Exists(x => x.Code == code))
            return SpecialAugment2;
        if (SpecialAugment3 != null && SpecialAugment3.Exists(x => x.Code == code))
            return SpecialAugment3;
        if (ProtoList != null && ProtoList.Exists(x => x.Code == code))
            return ProtoList;

        return null;
    }
    public void Ready() 
    {
        if (!readycheck) 
        {
            //GameManager.Instance.PV.RPC("EndPlayerCheck",RpcTarget.All);
            readycheck = true;
        }
    }
    public void OnOffGetList()
    {
        if (SeeNowMyList)
        {
            MySpecialList.SetActive(false);
            SeeNowMyList = false;
        }
        else 
        {
            MySpecialList.SetActive(true);
            SeeNowMyList = true;
        }
    }

    private void LockPlayerControls()
    {
        if (controlsLocked)
            return;

        if (playerinput == null && Player != null)
            playerinput = Player.GetComponent<PlayerInput>();

        if (Player != null && Player.TryGetComponent(out TopDownCharacterController controller))
            controller.ForceStopAttackInput();

        TrySetActionEnabled("Move", false);
        TrySetActionEnabled("Move2", false);
        TrySetActionEnabled("Attack", false);
        TrySetActionEnabled("Skill", false);
        TrySetActionEnabled("Roll", false);
        TrySetActionEnabled("Flash", false);
        TrySetActionEnabled("SiegeMode", false);
        TrySetActionEnabled("Reload", false);
        TrySetActionEnabled("AugmentCheck", false);

        controlsLocked = true;
    }

    private void UnlockPlayerControls()
    {
        if (!controlsLocked)
            return;

        if (Player != null && Player.TryGetComponent(out PlayerInputController inputController))
            inputController.ResetSetting();

        // 메뉴 토글 입력은 ResetSetting에서 제어하지 않으므로 명시적으로 복원
        TrySetActionEnabled("AugmentCheck", true);
        controlsLocked = false;
    }

    private void TrySetActionEnabled(string actionName, bool enabled)
    {
        if (playerinput == null || playerinput.actions == null || string.IsNullOrEmpty(actionName))
            return;

        InputAction action = playerinput.actions.FindAction(actionName);
        if (action == null)
            return;

        if (enabled)
            action.Enable();
        else
            action.Disable();
    }

    private void ShowTransfusionOptions(List<IAugment> options)
    {
        if (options == null)
            options = new List<IAugment>();

        int count = picklist != null ? picklist.Length : 0;
        for (int i = 0; i < count; i++)
        {
            ChoiceSlot slot = picklist[i];
            if (slot == null)
                continue;

            bool hasOption = i < options.Count && options[i] != null;
            if (!hasOption)
            {
                slot.gameObject.SetActive(false);
                continue;
            }

            slot.Parent = this;
            slot.stat = options[i];
            slot.Ispick = false;
            slot.gameObject.SetActive(true);
            slot.SetSelected(false, transfusionUnselectedColor);
        }

        SetActiveCheck = true;
        IsStat = true;
        statChance = false;
        SetTransfusionUiVisible(true);
        UpdateTransfusionCostText();
        UpdateTransfusionConfirmInteractable();
    }

    private List<IAugment> BuildTransfusionOptions()
    {
        List<IAugment> result = new List<IAugment>();
        HashSet<int> usedCodes = new HashSet<int>();
        int count = picklist != null ? picklist.Length : 0;

        for (int i = 0; i < count; i++)
        {
            IAugment picked = null;

            for (int attempt = 0; attempt < 12; attempt++)
            {
                List<IAugment> tierList = ResolveStatTierList(RandomTier());
                if (tierList == null || tierList.Count == 0)
                    continue;

                IAugment candidate = tierList[Random.Range(0, tierList.Count)];
                if (candidate == null || usedCodes.Contains(candidate.Code))
                    continue;

                picked = candidate;
                break;
            }

            if (picked == null)
            {
                List<IAugment> fallback = BuildStatFallbackPool(usedCodes);
                if (fallback.Count > 0)
                    picked = fallback[Random.Range(0, fallback.Count)];
            }

            if (picked == null)
                continue;

            usedCodes.Add(picked.Code);
            result.Add(picked);
        }

        return result;
    }

    private List<IAugment> ResolveStatTierList(int tier)
    {
        switch (tier)
        {
            case 1:
                return stat1;
            case 2:
                return stat2;
            case 3:
            case 4:
                return stat3;
            default:
                return stat1;
        }
    }

    private List<IAugment> BuildStatFallbackPool(HashSet<int> usedCodes)
    {
        List<IAugment> pool = new List<IAugment>();
        AddStatPoolCandidates(pool, stat1, usedCodes);
        AddStatPoolCandidates(pool, stat2, usedCodes);
        AddStatPoolCandidates(pool, stat3, usedCodes);
        return pool;
    }

    private void AddStatPoolCandidates(List<IAugment> pool, List<IAugment> source, HashSet<int> usedCodes)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            IAugment item = source[i];
            if (item == null || usedCodes.Contains(item.Code))
                continue;
            pool.Add(item);
        }
    }

    private void ConfirmTransfusionSelection()
    {
        if (selectionMode != ResultSelectionMode.TransfusionPendingConfirm || pendingChoiceSlot == null || pendingChoiceSlot.stat == null)
            return;

        PlayerStatControl playerStat = Player != null ? Player.GetComponent<PlayerStatControl>() : null;
        if (playerStat == null)
            return;

        float hpCost = GetTransfusionHpCost(playerStat);
        if (playerStat.CurHP <= hpCost)
        {
            UpdateTransfusionConfirmInteractable();
            return;
        }

        playerStat.CurHP -= hpCost;
        int selectedCode = pendingChoiceSlot.stat.Code;
        AugmentManager.Instance.AugmentCall(selectedCode);

        FinalizeTransfusionSession(true, selectedCode);
    }

    private void CancelTransfusionSelection()
    {
        if (selectionMode != ResultSelectionMode.TransfusionPendingConfirm)
            return;

        FinalizeTransfusionSession(false, -1);
    }

    private void FinalizeTransfusionSession(bool confirmed, int selectedCode)
    {
        int count = picklist != null ? picklist.Length : 0;
        for (int i = 0; i < count; i++)
        {
            ChoiceSlot slot = picklist[i];
            if (slot == null)
                continue;

            slot.Ispick = false;
            slot.SetSelected(false, transfusionUnselectedColor);
            slot.gameObject.SetActive(false);
        }

        SetTransfusionUiVisible(false);
        SetActiveCheck = false;
        IsStat = false;
        statChance = false;

        pendingChoiceSlot = null;
        selectionMode = ResultSelectionMode.Immediate;

        Action<bool, int> callback = transfusionCloseCallback;
        transfusionCloseCallback = null;
        string cacheKey = currentTransfusionCacheKey;
        currentTransfusionCacheKey = null;

        // 결제(확정) 완료 시에는 같은 장치라도 다음 진입에서 새 목록을 뽑는다.
        if (confirmed && !string.IsNullOrEmpty(cacheKey))
            transfusionOptionCache.Remove(cacheKey);

        UnlockPlayerControls();
        callback?.Invoke(confirmed, selectedCode);
    }

    private void SetTransfusionUiVisible(bool visible)
    {
        EnsureTransfusionUiReferences();

        if (transfusionCostRoot != null)
            transfusionCostRoot.SetActive(visible);
        if (transfusionCostText != null)
            transfusionCostText.gameObject.SetActive(visible);
        if (transfusionConfirmButton != null)
            transfusionConfirmButton.gameObject.SetActive(visible);
        if (transfusionCancelButton != null)
            transfusionCancelButton.gameObject.SetActive(visible);
    }

    private void UpdateTransfusionCostText()
    {
        EnsureTransfusionUiReferences();

        if (transfusionCostText == null)
            return;

        transfusionCostText.text = $"HP {Mathf.RoundToInt(transfusionHpCostPercent * 100f)}%";
    }

    private void UpdateTransfusionConfirmInteractable()
    {
        if (transfusionConfirmButton == null)
            return;

        bool canConfirm = pendingChoiceSlot != null && pendingChoiceSlot.stat != null;
        PlayerStatControl playerStat = Player != null ? Player.GetComponent<PlayerStatControl>() : null;
        if (canConfirm && playerStat != null)
        {
            float hpCost = GetTransfusionHpCost(playerStat);
            canConfirm = playerStat.CurHP > hpCost;
        }

        transfusionConfirmButton.interactable = canConfirm;
    }

    private float GetTransfusionHpCost(PlayerStatControl playerStat)
    {
        if (playerStat == null)
            return 0f;

        float maxHp = playerStat.HP != null ? playerStat.HP.total : 0f;
        return Mathf.Max(1f, maxHp * transfusionHpCostPercent);
    }

    private void EnsureTransfusionUiReferences()
    {
        if (transfusionCostRoot == null && transfusionCostText != null)
            transfusionCostRoot = transfusionCostText.gameObject;

        if (transfusionCostText == null && transfusionCostRoot != null)
            transfusionCostText = transfusionCostRoot.GetComponentInChildren<TextMeshProUGUI>(true);

        if (transfusionCostRoot == null && transfusionConfirmButton != null)
        {
            Transform siblingText = transfusionConfirmButton.transform.parent != null
                ? transfusionConfirmButton.transform.parent.Find("CostText")
                : null;
            if (siblingText != null)
                transfusionCostRoot = siblingText.gameObject;
        }
    }
}
