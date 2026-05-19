using UnityEngine;

/// <summary>
/// MapMakeSetting 테스트씬 전용 게임매니저.
/// 기존 GameManager / Stage 를 수정하지 않고 씬 내 오브젝트를 초기화한다.
/// 
/// 동작 순서:
///  1. Start() → 기존 MainPlayerCharacter 비활성화, 캐릭터 선택 UI 표시
///  2. 버튼 클릭(SelectTV/SelectCharlie/SelectKimKilWhan) → StartTest()
///  3. StartTest() → 선택 프리팹 소환 → 카메라 연결 → 매니저 초기화 → Stage 활성화
/// </summary>
public class TestGameManager : MonoBehaviour
{
    [Header("테스트 플레이어 프리팹")]
    public GameObject prefabTV;
    public GameObject prefabCharlie;
    public GameObject prefabKimKilWhan;

    [Header("씬 레퍼런스")]
    public Stage stage;
    public MainCamera mainCamera;
    /// <summary>씬에 미리 배치된 MainPlayerCharacter - Start 시 비활성화</summary>
    public GameObject existingPlayer;

    [Header("캐릭터 선택 UI")]
    public GameObject characterSelectUI;

    [Header("물음표(?) 방 및 몬스터 그룹 테스트 설정")]
    [Tooltip("물음표(?) 방에 입장한 것으로 가정하여 테스트할지 여부")]
    public bool simulateMysteryRoom = true;
    [Tooltip("물음표(?) 방 20% 확률 보상 스킵(즉시 보상 스폰)을 100% 강제 적용할지 여부")]
    public bool forceMysteryRoomRewardSkip = false;
    [Tooltip("테스트 층 번호 (1~5층 몬스터 소환을 위해 -2(3층) 등으로 설정)")]
    public int testRoomNumber = -2;

    [Header("테스트 코인 설정")]
    public GameObject coinPrefab;

    public static TestGameManager Instance { get; private set; }

    [Header("테스트 골드 시스템")]
    public int Gold { get; private set; } = 1000; // 테스트 상점 이용을 위해 넉넉한 초기 1000골드 증정!
    public event UnityEngine.Events.UnityAction<int> OnGoldChanged;

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
        Debug.Log($"[TestGameManager] Gold Added: {amount}. Total: {Gold}");
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return false;
        if (Gold < amount) return false;
        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        Debug.Log($"[TestGameManager] Gold Spent: {amount}. Total: {Gold}");
        return true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// GameManager가 없는 테스트룸에서 코인을 소환할 수 있도록 대행하는 메서드입니다.
    /// </summary>
    public void SpawnCoinsForAmount(Vector3 position, int totalAmount)
    {
        if (coinPrefab == null || totalAmount <= 0)
        {
            Debug.LogWarning("[TestGameManager] coinPrefab이 할당되지 않았거나 totalAmount <= 0 입니다.");
            return;
        }

        int remaining = totalAmount;

        // 10원짜리
        int count10 = remaining / 10;
        remaining  %= 10;
        // 5원짜리
        int count5  = remaining / 5;
        remaining  %= 5;
        // 1원짜리
        int count1  = remaining;

        for (int i = 0; i < count10; i++) SpawnOneCoin(position, CoinItem.CoinType.Won10);
        for (int i = 0; i < count5;  i++) SpawnOneCoin(position, CoinItem.CoinType.Won5);
        for (int i = 0; i < count1;  i++) SpawnOneCoin(position, CoinItem.CoinType.Won1);
    }

    private void SpawnOneCoin(Vector3 position, CoinItem.CoinType type)
    {
        GameObject coin = Instantiate(coinPrefab, position, Quaternion.identity);
        CoinItem coinItem = coin.GetComponent<CoinItem>();
        if (coinItem != null)
            coinItem.Init(type);
    }

    private void Start()
    {
        // 씬에 있던 기본 플레이어 비활성화 (선택 후 교체)
        if (existingPlayer != null)
            existingPlayer.SetActive(false);

        if (characterSelectUI != null)
            characterSelectUI.SetActive(true);

        // // 테스트 편의성: 플레이 모드 진입 시 0.5초 후 자동으로 TV 캐릭터를 선택하여 테스트를 시작합니다.
        // StartCoroutine(AutoStartTestRoutine());
    }

    // private System.Collections.IEnumerator AutoStartTestRoutine()
    // {
    //     yield return new WaitForSeconds(0.5f);
    //     Debug.Log("[TestGameManager] 0.5초 대기 후 자동으로 TV 캐릭터를 선택하여 테스트룸을 실행합니다.");
    //     SelectTV();
    // }

    // UI 버튼 OnClick에 연결
    public void SelectTV()         => StartTest(prefabTV);
    public void SelectCharlie()    => StartTest(prefabCharlie);
    public void SelectKimKilWhan() => StartTest(prefabKimKilWhan);

    private void StartTest(GameObject prefab)
    {
        if (characterSelectUI != null)
            characterSelectUI.SetActive(false);

        // 선택한 캐릭터 소환
        Transform spawnPoint = stage != null ? stage.GetPlayerSpawnPoint() : null;
        Vector3 spawnPos = spawnPoint != null
            ? spawnPoint.position
            : Vector3.zero;
        GameObject player = Instantiate(prefab, spawnPos, Quaternion.identity);

        // 테스트 씬에서도 HUD/매니저가 현재 플레이어를 참조하도록 동기화
        if (GameManager.Instance != null)
            GameManager.Instance.playerOBJ = player;

        // 비활성 프리팹 (Charlie, KimKilWhan 등)은 Instantiate 후 Awake()가 실행되지 않으므로
        // SetActive(true)로 먼저 Awake()를 강제 실행 후 풀 초기화
        if (!player.activeSelf)
            player.SetActive(true);

        // 카메라가 플레이어를 따라가도록
        if (mainCamera != null)
            mainCamera.Target = player;

        // 총알 풀 부모 오브젝트 생성 (하이라키 정리용)
        GameObject bulletPoolParent = new GameObject("BulletPool");

        // 무기 오브젝트 풀 초기화
        WeaponSystem ws = player.GetComponent<WeaponSystem>();
        if (ws != null)
        {
            ws.poolParent = bulletPoolParent;
            ws.StartObjectPOOL();
        }

        // 어그멘트 / 결과 매니저 초기화
        if (AugmentManager.Instance != null)
            AugmentManager.Instance.startset(player);
        if (MakeAugmentListManager.Instance != null)
            MakeAugmentListManager.Instance.startset(player);

        // 증강 목록 갱신
        if (TestAugmentUI.Instance != null)
            TestAugmentUI.Instance.RefreshList();

        // Stage 활성화
        // ReadyStage() 내부에서 GameManager.Instance.bossCount를 세팅하므로
        // 씬에 GameManager 오브젝트가 반드시 존재해야 한다.
        if (stage != null)
        {
            if (stage is NormalStage normalStage)
            {
                if (simulateMysteryRoom)
                {
                    normalStage.InitStage(testRoomNumber, NormalStage.NormalStageCase.MonsterWithReward, NormalStage.RoomTheme.Mystery);
                    if (forceMysteryRoomRewardSkip)
                    {
                        normalStage.SetStageCase(NormalStage.NormalStageCase.ChoiceResult);
                        Debug.Log("[TestGameManager] 물음표 방 20% 보상 스킵 테스트 강제 성공 설정!");
                    }
                }
                else
                {
                    normalStage.roomNumber = testRoomNumber;
                }
            }
            else if (stage is BossStage bossStage)
            {
                bossStage.PrepareBossForRuntime();
            }

            stage.ObjActiveTrue();
            stage.ReadyStage();
        }

        // 테스트룸도 메인 게임과 동일한 HUD 흐름을 사용한다.
        // 씬 레퍼런스가 비어 있어도 런타임에서 PlayerUiManager를 찾아 연결한다.
        PlayerUiManager uiManager = null;
        if (GameManager.Instance != null)
        {
            uiManager = GameManager.Instance.playerUiManager;
            if (uiManager == null)
            {
                uiManager = Object.FindObjectOfType<PlayerUiManager>(true);
                if (uiManager != null)
                    GameManager.Instance.playerUiManager = uiManager;
            }
        }
        else
        {
            uiManager = Object.FindObjectOfType<PlayerUiManager>(true);
        }

        if (uiManager == null)
        {
            // 씬에 PlayerUiManager가 없으면 실제 PlayerHUD 프리팹 로드를 우선 시도한다.
            Canvas canvas = Object.FindObjectOfType<Canvas>(true);
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            GameObject hudRoot = null;
            GameObject hudPrefab = null;
#if UNITY_EDITOR
            hudPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PlayerHUD/PlayerHUD.prefab");
#endif
            if (hudPrefab == null)
                hudPrefab = Resources.Load<GameObject>("Prefabs/PlayerHUD/PlayerHUD");

            if (hudPrefab != null)
            {
                hudRoot = Instantiate(hudPrefab, canvas.transform);
                hudRoot.name = "PlayerHUD";
                RectTransform hudRt = hudRoot.GetComponent<RectTransform>();
                if (hudRt != null)
                {
                    hudRt.anchorMin = Vector2.zero;
                    hudRt.anchorMax = Vector2.one;
                    hudRt.offsetMin = Vector2.zero;
                    hudRt.offsetMax = Vector2.zero;
                }

                uiManager = hudRoot.GetComponent<PlayerUiManager>();
                if (uiManager == null)
                    uiManager = hudRoot.GetComponentInChildren<PlayerUiManager>(true);
            }

            // 프리팹 로드 실패 시에만 최소 런타임 HUD 루트로 폴백
            if (uiManager == null)
            {
                hudRoot = new GameObject("PlayerHUD", typeof(RectTransform), typeof(PlayerUiManager));
                hudRoot.transform.SetParent(canvas.transform, false);
                RectTransform hudRt = hudRoot.GetComponent<RectTransform>();
                hudRt.anchorMin = Vector2.zero;
                hudRt.anchorMax = Vector2.one;
                hudRt.offsetMin = Vector2.zero;
                hudRt.offsetMax = Vector2.zero;
                uiManager = hudRoot.GetComponent<PlayerUiManager>();
                Debug.LogWarning("[TestGameManager] PlayerHUD 프리팹 로드 실패로 런타임 HUD를 생성했습니다. 경로: Resources/Prefabs/PlayerHUD/PlayerHUD");
            }

            if (GameManager.Instance != null)
                GameManager.Instance.playerUiManager = uiManager;
        }

        if (uiManager != null)
            uiManager.SetupData();
        else
            Debug.LogWarning("[TestGameManager] PlayerUiManager를 찾지 못해 HUD를 초기화하지 못했습니다.");
    }
}
