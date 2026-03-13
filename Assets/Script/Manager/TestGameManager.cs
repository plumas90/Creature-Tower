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

    private void Start()
    {
        // 씬에 있던 기본 플레이어 비활성화 (선택 후 교체)
        if (existingPlayer != null)
            existingPlayer.SetActive(false);

        if (characterSelectUI != null)
            characterSelectUI.SetActive(true);
    }

    // UI 버튼 OnClick에 연결
    public void SelectTV()         => StartTest(prefabTV);
    public void SelectCharlie()    => StartTest(prefabCharlie);
    public void SelectKimKilWhan() => StartTest(prefabKimKilWhan);

    private void StartTest(GameObject prefab)
    {
        if (characterSelectUI != null)
            characterSelectUI.SetActive(false);

        // 선택한 캐릭터 소환
        Vector3 spawnPos = stage != null && stage.PlayerSpawnPoint != null
            ? stage.PlayerSpawnPoint.position
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
            // Stage에 bossPrefab만 설정되어 있어도 테스트 시작 시 보스 인스턴스를 준비한다.
            stage.PrepareBossForRuntime();
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
            GameObject hudPrefab = Resources.Load<GameObject>("Prefabs/PlayerHUD/PlayerHUD");
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
