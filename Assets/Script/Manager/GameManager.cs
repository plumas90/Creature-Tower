using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public event Action OnStageStartEvent;

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
    //public int Gold;  // 골드
    public int Life; // 라이프

    [Header("GameData")]
    //public StageData stageData; // 필요 시 스테이지 데이터 구조로 분리
    private int TowerLevelCount; // 현재 층 인덱스
    //public int currentMonsterCount; // 현재 몬스터 수
    public int bossCount;
    private bool isTransitioningStage;
    //public bool clearRoom; // 방 클리어 여부
    private int EndingTowerStage = 0; // 생성된 총 스테이지 수


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
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void MakeStageTree() 
    {
        bossCount = 0;
        TowerLevelCount = 0;
        EndingTowerStage = 0;
        stageList.Clear();
        StageTree.Clear();
        // 각 층 리스트가 비어있지 않으면 stageList에 등록한다.
        if (stageLevel1.Count >=1) 
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        #region 2 to 15
        if (stageLevel2.Count >= 1)
        {
            stageList.Add(stageLevel2);
            EndingTowerStage++;
        }
        if (stageLevel3.Count >= 1)
        {
            stageList.Add(stageLevel3);
            EndingTowerStage++;
        }
        if (stageLevel4.Count >= 1)
        {
            stageList.Add(stageLevel4);
            EndingTowerStage++;
        }
        if (stageLevel5.Count >= 1)
        {
            stageList.Add(stageLevel5);
            EndingTowerStage++;
        }
        if (stageLevel6.Count >= 1)
        {
            stageList.Add(stageLevel6);
            EndingTowerStage++;
        }
        if (stageLevel7.Count >= 1)
        {
            stageList.Add(stageLevel7);
            EndingTowerStage++;
        }
        if (stageLevel8.Count >= 1)
        {
            stageList.Add(stageLevel8);
            EndingTowerStage++;
        }
        if (stageLevel9.Count >= 1)
        {
            stageList.Add(stageLevel9);
            EndingTowerStage++;
        }
        if (stageLevel10.Count >= 1)
        {
            stageList.Add(stageLevel10);
            EndingTowerStage++;
        }
        if (stageLevel11.Count >= 1)
        {
            stageList.Add(stageLevel11);
            EndingTowerStage++;
        }
        if (stageLevel12.Count >= 1)
        {
            stageList.Add(stageLevel12);
            EndingTowerStage++;
        }
        if (stageLevel13.Count >= 1)
        {
            stageList.Add(stageLevel13);
            EndingTowerStage++;
        }
        if (stageLevel14.Count >= 1)
        {
            stageList.Add(stageLevel14);
            EndingTowerStage++;
        }
        if (stageLevel15.Count >= 1)
        {
            stageList.Add(stageLevel15);
            EndingTowerStage++;
        }
        #endregion

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
            Room.transform.position = new Vector3(0, 0 + (i * 50), 0);
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
        playerUiManager.SetupData();
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

    public void NextLevel() 
    {
        if (isTransitioningStage)
            return;

        Debug.Log("1체크");
        if (CurrentStage == null || StageTree == null || StageTree.Count == 0)
        {
            Debug.LogWarning("[GameManager] NextLevel aborted: stage data is not initialized.");
            return;
        }
        Debug.Log("2체크");
        // 마지막 스테이지면 종료 분기
        if (TowerLevelCount >= StageTree.Count - 1)
        {
            if (false)// 엔딩 연출 분기(예약)
            {

            }
            else //
            {
                // 데모 종료 UI 표시
                thankDemoUI.SetActive(true);
            }
             Debug.Log("3체크");
        }
        else if (false)  // 분기 예약
        {
             Debug.Log("4체크");
        }
        else
        {
             Debug.Log("5체크");
            isTransitioningStage = true;
            Stage previousStage = CurrentStage;
            TowerLevelCount++;
            CurrentStage = StageTree[TowerLevelCount];
            if (previousStage != null)
                previousStage.ObjActiveFalse();
            StageLevelSet();
            isTransitioningStage = false;
        }
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

        CurrentStage.ReadyStage();
        playerOBJ.transform.position = CurrentStage.PlayerSpawnPoint.position;
        OnStageStartEvent?.Invoke();
    }
}
