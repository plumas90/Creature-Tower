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
            stage.ObjActiveTrue();
            stage.ReadyStage();
        }
    }
}
