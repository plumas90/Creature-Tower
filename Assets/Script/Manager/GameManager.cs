using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public event UnityAction OnStageStartEvent;
    private const string AugmentManagerPrefabPath = "Prefabs/AugmentManager";
    private const string MakeAugmentListManagerPrefabPath = "Prefabs/MakeAugmentListManager";
    private const string ResultManagerPrefabPath = "Prefabs/ResultManager";

    public PlayerUiManager playerUiManager;
    // TODO: 게임 상태 분기/초기화 구조를 정리할 필요가 있음.
    // 현재는 Stage/Player/UI 초기화가 한곳에 모여 있으므로, 추후 책임 분리 필요.


    /*
    public enum GameStates
    {
        Init,
        UIPlaying,
        Start,
        Playing,
        End,
        AugmentListing,
    }
    */

    [Header("PlayerData")]
    public GameObject playerOBJ;
    public GameObject clientPlayer { get { return playerOBJ; } }
    //public PlayerDataSetting characterSetting; // 플레이어 데이터
    public bool isDie; // 플레이어 사망 여부
    [Header("Economy")]
    [SerializeField] private int startGold = 0;
    public int Gold { get; private set; }  // 골드
    public event UnityAction<int> OnGoldChanged;
    public int Life; // 라이프

    [Header("GameData")]
    //public StageData stageData; // 필요 시 스테이지 데이터 구조로 분리
    private int TowerLevelCount; // 현재 층 인덱스
    //public int currentMonsterCount; // 현재 몬스터 수
    public int bossCount;
    private bool isTransitioningStage;
    //public bool clearRoom; // 방 클리어 여부
    private int EndingTowerStage = 0; // 생성된 총 스테이지 수
    private enum ProgressionState
    {
        BossStage,
        ChoosingNormalStage,
        NormalStage
    }
    private ProgressionState progressionState = ProgressionState.BossStage;
    private readonly List<Stage> pendingNormalStageChoices = new List<Stage>();


    [SerializeField] public List<List<GameObject>> stageList = new List<List<GameObject>>();
    #region stagelist
    public List<GameObject> stageLevel1;
    public List<GameObject> stageLevel2;
    public List<GameObject> stageLevel3;  
    public List<GameObject> stageLevel4;
    public List<GameObject> stageLevel5;
    public List<GameObject> stageLevel6;
    public List<GameObject> stageLevel7;
    public List<GameObject> stageLevel8;
    public List<GameObject> stageLevel9;
    public List<GameObject> stageLevel10;
    public List<GameObject> stageLevel11;
    public List<GameObject> stageLevel12;
    public List<GameObject> stageLevel13;
    public List<GameObject> stageLevel14;
    public List<GameObject> stageLevel15;
    [Header("Normal Stage")]
    [SerializeField] private List<GameObject> normalStageCandidates = new List<GameObject>();
    [SerializeField] [Range(1, 3)] private int normalChoiceCount = 3;
    #endregion

    [Header("UI")] // UI 참조
    public GameObject StageInfoUI; // 스테이지 정보 UI
    public GameObject thankDemoUI; // 데모 종료 안내 UI

    #region Legacy
    /*
    [Serializable]
    public struct StageData // ���������� ������
    {
        //public int currentArea;
        //public int currentStage;
        //public bool isFarmingRoom;

        public bool isEventRoom;
        public bool isBossRoom;
        public bool isShopRoom;
    }*/
    /* 이벤트 정의(보관용)
    public event Action OnGameStartedEvent; // 게임 시작 이벤트 (미사용)
    public event Action OnGameEndedEvent; // 게임 종료 이벤트 (미사용)
    public event Action OnGameClearedEvent; // 게임 클리어 이벤트
    public event Action OnPlayerDieEvent; // 플레이어 사망 이벤트
    public event Action OnGameOverEvent; // 게임 오버 이벤트


    //public event Action OnOverCheckEvent; // 생존 플레이어 확인 이벤트
    public event Action OnUIPlayingStateChanged; // UI 플레이 상태 변경 이벤트
    public event Action OnStartStateChanged; // 시작 상태 변경 이벤트
    public event Action OnPlayingStateChanged; // 플레이 상태 변경 이벤트
    public event Action OnEndStateChanged; // 종료 상태 변경 이벤트
    public event Action OnAugmentListingStateChanged; // 증강 선택 상태 변경 이벤트
    */
    #endregion


    [HideInInspector]public List<Stage> StageTree = new List<Stage>();
    [HideInInspector]public Stage CurrentStage ;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (progressionState != ProgressionState.ChoosingNormalStage)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            SelectNormalStageChoice(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            SelectNormalStageChoice(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            SelectNormalStageChoice(2);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // MainScene 진입 시 씬 종속 참조를 재연결
        if (scene.name != "MainScene")
            return;

        if (playerUiManager == null)
            playerUiManager = FindFirstObjectByType<PlayerUiManager>(FindObjectsInactive.Include);

        if (StageInfoUI == null)
        {
            var uiManager = playerUiManager != null ? playerUiManager.gameObject : null;
            if (uiManager != null)
                StageInfoUI = uiManager;
        }

        if (thankDemoUI == null)
            thankDemoUI = GameObject.Find("ThankDemoUI");

        // 메인 메뉴 경유 진입 시 누락될 수 있는 보상/증강 매니저를 보장한다.
        EnsureRewardManagers();
    }

    public void MakeStageTree() 
    {
        bossCount = 0;
        TowerLevelCount = 0;
        EndingTowerStage = 0;
        progressionState = ProgressionState.BossStage;
        ClearPendingNormalChoices();
        stageList.Clear();
        StageTree.Clear();
        // 각 층 리스트에서 랜덤으로 1개만 뽑아 stageList에 등록한다.
        AddRandomStageFromLevel(stageLevel1);
        AddRandomStageFromLevel(stageLevel2);
        AddRandomStageFromLevel(stageLevel3);
        AddRandomStageFromLevel(stageLevel4);
        AddRandomStageFromLevel(stageLevel5);
        AddRandomStageFromLevel(stageLevel6);
        AddRandomStageFromLevel(stageLevel7);
        AddRandomStageFromLevel(stageLevel8);
        AddRandomStageFromLevel(stageLevel9);
        AddRandomStageFromLevel(stageLevel10);
        AddRandomStageFromLevel(stageLevel11);
        AddRandomStageFromLevel(stageLevel12);
        AddRandomStageFromLevel(stageLevel13);
        AddRandomStageFromLevel(stageLevel14);
        AddRandomStageFromLevel(stageLevel15);

    }

    private void AddRandomStageFromLevel(List<GameObject> levelCandidates)
    {
        if (levelCandidates == null || levelCandidates.Count == 0)
            return;

        int randomIndex = UnityEngine.Random.Range(0, levelCandidates.Count);
        GameObject selectedStage = levelCandidates[randomIndex];
        if (selectedStage == null)
            return;

        stageList.Add(new List<GameObject> { selectedStage });
        EndingTowerStage++;
    }
    public void SetStageTree() 
    {
        for (int i = 0; i < EndingTowerStage ; i++)
        {
            List<GameObject> curStageList = stageList[i];
            if (curStageList == null || curStageList.Count == 0)
                continue;

            int j = UnityEngine.Random.Range(0, curStageList.Count);
            GameObject Room = Instantiate(curStageList[j], Vector3.zero, Quaternion.identity);
            Stage stage = Room.GetComponent<Stage>();
            stage.roomNumber = i;
            stage.SetRuntimeBossFlow(true);
            Room.transform.position = new Vector3(0 + (i * 100), 0, 0);
            if (i == 0) 
            {
                CurrentStage = stage;
            }
            StageTree.Add(stage);
        }
    }
    public void Init(GameObject player) 
    {
        playerOBJ = player;
        Life = 0;
        Gold = Mathf.Max(0, startGold);
        OnGoldChanged?.Invoke(Gold);
        EnsureRewardManagers();

        // 카메라 Target 설정 (플레이어가 설정되면 즉시 카메라에 알림)
        MainCamera mainCamera = FindFirstObjectByType<MainCamera>();
        if (mainCamera != null && mainCamera.Target == null)
        {
            mainCamera.Target = player;
            Debug.Log("[GameManager] Camera target set to player");
        }

        // 총알 풀은 스테이지 초기화보다 먼저 생성 (이후 코드 예외와 무관하게 보장)
        WeaponSystem weaponSystem = player.GetComponent<WeaponSystem>();
        if (weaponSystem != null)
        {
            weaponSystem.StartObjectPOOL();
        }

        // 증강/보상 매니저 초기화 (캐릭터 선택 직후, 스테이지 생성 전)
        if (AugmentManager.Instance != null)
            AugmentManager.Instance.startset(player);
        if (MakeAugmentListManager.Instance != null)
            MakeAugmentListManager.Instance.startset(player); // 캐릭터 타입에 맞는 증강 리스트 빌드
        if (ResultManager.Instance != null)
        {
            ResultManager.Instance.startset(player);
            ResultManager.Instance.StartSet(); // MakeLisk 완료 후 리스트 복사
        }

        //tower ui on
        MakeStageTree();
        SetStageTree();
        StageLevelSet();
        if (playerUiManager != null)
            playerUiManager.SetupData();
        else
            Debug.LogWarning("[GameManager] playerUiManager is null. SetupData skipped.");
    }
    public void BossCountSet(int i) 
    {
        bossCount = Mathf.Max(0, i);
    }

    public void BossCountAdd(int i)
    {
        if (i <= 0) return;
        bossCount += i;
    }

    public void BossCountMinus(int i)
    {
        if (i <= 0) return;
        bossCount = Mathf.Max(0, bossCount - i);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (Gold < amount)
            return false;

        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        return true;
    }

    public void NextLevel() 
    {
        if (isTransitioningStage)
            return;

        // 스테이지 전환 시점에 결과/선택 UI가 열려 있으면 미확정 상태를 취소하고 닫는다.
        if (ResultManager.Instance != null)
            ResultManager.Instance.CancelPendingSelection();

        Debug.Log("1체크");
        if (CurrentStage == null || StageTree == null || StageTree.Count == 0)
        {
            Debug.LogWarning("[GameManager] NextLevel aborted: stage data is not initialized.");
            return;
        }
        Debug.Log("2체크");

        if (progressionState == ProgressionState.ChoosingNormalStage)
        {
            Debug.Log("[GameManager] NextLevel ignored: waiting for normal stage selection (press 1~3).");
            return;
        }

        if (progressionState == ProgressionState.NormalStage)
        {
            TransitionToNextBossStage();
            return;
        }

        // 현재 보스 스테이지에서 계단을 탔을 때 처리
        if (TowerLevelCount >= StageTree.Count - 1)
        {
            if (thankDemoUI != null)
                thankDemoUI.SetActive(true);
            Debug.Log("3체크");
            return;
        }

        BeginNormalStageChoice();
    }

    private void BeginNormalStageChoice()
    {
        if (normalStageCandidates == null || normalStageCandidates.Count == 0)
        {
            // 일반 스테이지 풀이 없으면 기존처럼 바로 다음 보스 스테이지로 진행
            TransitionToNextBossStage();
            return;
        }

        ClearPendingNormalChoices();

        int count = Mathf.Min(normalChoiceCount, normalStageCandidates.Count);
        List<int> usedIndices = new List<int>(count);
        float baseX = (StageTree.Count + 1) * 100f;

        for (int i = 0; i < count; i++)
        {
            int pickIndex = -1;
            int safety = 0;
            while (pickIndex < 0 && safety < 32)
            {
                int candidate = UnityEngine.Random.Range(0, normalStageCandidates.Count);
                if (!usedIndices.Contains(candidate))
                    pickIndex = candidate;
                safety++;
            }

            if (pickIndex < 0)
                continue;

            usedIndices.Add(pickIndex);
            GameObject prefab = normalStageCandidates[pickIndex];
            if (prefab == null)
                continue;

            GameObject room = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            Stage stage = room.GetComponent<Stage>();
            if (stage == null)
            {
                Debug.LogWarning($"[GameManager] Normal stage prefab has no Stage component: {prefab.name}");
                Destroy(room);
                continue;
            }

            stage.roomNumber = -(i + 1);
            stage.SetRuntimeBossFlow(false);
            room.transform.position = new Vector3(baseX + (i * 100f), 0f, 0f);
            pendingNormalStageChoices.Add(stage);
        }

        if (pendingNormalStageChoices.Count == 0)
        {
            TransitionToNextBossStage();
            return;
        }

        progressionState = ProgressionState.ChoosingNormalStage;
        Debug.Log($"[GameManager] Normal stage choices ready. Press 1~{pendingNormalStageChoices.Count} to select.");
        for (int i = 0; i < pendingNormalStageChoices.Count; i++)
            Debug.Log($"[GameManager] Choice {i + 1}: {pendingNormalStageChoices[i].name}");
    }

    private void SelectNormalStageChoice(int choiceIndex)
    {
        if (progressionState != ProgressionState.ChoosingNormalStage)
            return;

        if (choiceIndex < 0 || choiceIndex >= pendingNormalStageChoices.Count)
            return;

        isTransitioningStage = true;
        Stage previousStage = CurrentStage;
        Stage selected = pendingNormalStageChoices[choiceIndex];

        for (int i = 0; i < pendingNormalStageChoices.Count; i++)
        {
            if (i == choiceIndex)
                continue;

            Stage stage = pendingNormalStageChoices[i];
            if (stage != null)
                Destroy(stage.gameObject);
        }

        pendingNormalStageChoices.Clear();
        CurrentStage = selected;
        progressionState = ProgressionState.NormalStage;

        if (previousStage != null)
            previousStage.ObjActiveFalse();

        StageLevelSet();
        isTransitioningStage = false;
    }

    private void TransitionToNextBossStage()
    {
        if (TowerLevelCount >= StageTree.Count - 1)
        {
            if (thankDemoUI != null)
                thankDemoUI.SetActive(true);
            return;
        }

        isTransitioningStage = true;
        Stage previousStage = CurrentStage;
        TowerLevelCount++;
        CurrentStage = StageTree[TowerLevelCount];

        if (previousStage != null)
            previousStage.ObjActiveFalse();

        progressionState = ProgressionState.BossStage;
        StageLevelSet();
        isTransitioningStage = false;
    }

    private void ClearPendingNormalChoices()
    {
        for (int i = 0; i < pendingNormalStageChoices.Count; i++)
        {
            Stage stage = pendingNormalStageChoices[i];
            if (stage != null)
                Destroy(stage.gameObject);
        }
        pendingNormalStageChoices.Clear();
    }
    public void StageLevelSet() 
    {
        if (CurrentStage == null)
        {
            Debug.LogWarning("[GameManager] StageLevelSet aborted: CurrentStage is null.");
            return;
        }
        if (playerOBJ == null)
        {
            Debug.LogWarning("[GameManager] StageLevelSet aborted: playerOBJ is null.");
            return;
        }
        if (CurrentStage.PlayerSpawnPoint == null)
        {
            Debug.LogWarning($"[GameManager] StageLevelSet aborted: PlayerSpawnPoint is null on '{CurrentStage.name}'.");
            return;
        }

        // 루트가 비활성 저장된 스테이지는 첫 SetActive(true)에서 Awake가 한 번 더 비활성화할 수 있어
        // Stage 시작 전에 한 번 미리 활성화해 Awake 사이드이펙트를 소거한다.
        CurrentStage.ObjActiveTrue();
        CurrentStage.ReadyStage();
        playerOBJ.transform.position = CurrentStage.PlayerSpawnPoint.position;
        OnStageStartEvent?.Invoke();
    }

    private void EnsureRewardManagers()
    {
        EnsureManagerInstance(
            AugmentManager.Instance,
            AugmentManagerPrefabPath,
            "[GameManager] AugmentManager prefab not found at Resources/Prefabs/AugmentManager"
        );
        EnsureManagerInstance(
            MakeAugmentListManager.Instance,
            MakeAugmentListManagerPrefabPath,
            "[GameManager] MakeAugmentListManager prefab not found at Resources/Prefabs/MakeAugmentListManager"
        );
        EnsureManagerInstance(
            ResultManager.Instance,
            ResultManagerPrefabPath,
            "[GameManager] ResultManager prefab not found at Resources/Prefabs/ResultManager"
        );
    }

    private void EnsureManagerInstance(MonoBehaviour existing, string resourcePath, string notFoundLog)
    {
        if (existing != null)
            return;

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogWarning(notFoundLog);
            return;
        }

        Instantiate(prefab);
    }
}
