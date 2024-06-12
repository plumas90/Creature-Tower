using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    //TO DO 현재 방 스테이지 정보 현재 처음에 층수 X 맵을 다해두고 좌표이동으로 다음 층 가는걸 생각중임 그래서 현재 층 정보에 대한게 필요함 그게 게임데이타 
    // 방클리어 여부를 그거에 연동하는식으로 체크 하는게 좋을거 같다고 생각됨


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
    //public PlayerDataSetting characterSetting; // 플레이어의 정보
    public bool isDie; // 플레이어 죽음 여부
    //public int Gold;  // 골드
    public int Life; //목숨

    [Header("GameData")]
    //public StageData stageData;//스테이지 데이터 필요 없을거 같은데 혹시 모름
    private int TowerLevelCount; // 층수
    //public int currentMonsterCount; // 현재 남은 몬스터수 보스카운터 스셈 그냥
    public int bossCount;
    //public bool clearRoom;// 방클리어여부 방에서 가져감
    private int EndingTowerStage = 0; // 마지막층


    [SerializeField] public List<List<Stage>> stageList;

    public List<Stage> stageLevel1;
    public List<Stage> stageLevel2;
    public List<Stage> stageLevel3;  
    public List<Stage> stageLevel4;
    public List<Stage> stageLevel5;
    public List<Stage> stageLevel6;
    public List<Stage> stageLevel7;
    public List<Stage> stageLevel8;
    public List<Stage> stageLevel9;
    public List<Stage> stageLevel10;
    public List<Stage> stageLevel11;
    public List<Stage> stageLevel12;
    public List<Stage> stageLevel13;
    public List<Stage> stageLevel14;
    public List<Stage> stageLevel15;

    [Header("UI")] //의미 불가
    public GameObject StageInfoUI; //스테이지 UI  // 층 올라가는 모션
    public GameObject thankDemoUI; // 데모 플레이 감사 UI 크레딧 포함

    #region 폐기
    /*
    [Serializable]
    public struct StageData // 스테이지의 데이터
    {
        //public int currentArea;
        //public int currentStage;
        //public bool isFarmingRoom;

        public bool isEventRoom;
        public bool isBossRoom;
        public bool isShopRoom;
    }*/
    /* 이벤트 목록
    public event Action OnGameStartedEvent; // 게임 시작시 이벤트 (증강쪽)
    public event Action OnGameEndedEvent; // 게임 끝날시 이벤트 (증강쪽)
    public event Action OnGameClearedEvent; // 게임 클리어시 이벤트
    public event Action OnPlayerDieEvent; // 플레이어 사망시 이벤트
    public event Action OnGameOverEvent; // 게임 오버시 이벤트


    //public event Action OnOverCheckEvent; // 다른 플레이어 사망시 현재 생존자를 세는 이벤트
    public event Action OnUIPlayingStateChanged; // UI쪽 플레이시 이벤트
    public event Action OnStartStateChanged; // 스타트 스테이트시 이벤트
    public event Action OnPlayingStateChanged; // 플레잉 스테이트시 이벤트
    public event Action OnEndStateChanged; // 끝날 시 이벤트
    public event Action OnAugmentListingStateChanged; // 보상 스테이트시 이벤트
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
        //이거보다 더 나은 방법이 있다고 생각한다 근데 모르겠어 시발 근데 난 지금 이걸 완성해둬야해 시발
        if (stageLevel1.Count >=1) 
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        #region 2 to 15
        if (stageLevel2.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel3.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel4.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel5.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel6.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel7.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel8.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel9.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel10.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel11.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel12.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel13.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel14.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        if (stageLevel15.Count >= 1)
        {
            stageList.Add(stageLevel1);
            EndingTowerStage++;
        }
        #endregion

    }
    public void SetStageTree() 
    {
        for (int i = 0; i < EndingTowerStage ; i++)
        {
            int j = UnityEngine.Random.Range(0, stageList.Count);
            List<Stage> curStageList = stageList[i];
            Stage stage = curStageList[j];
            stage.roomNumber = i;
            stage.transform.position = new Vector3(0, 0 + (i * 50), 0);
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
        MakeStageTree();
        SetStageTree();
        StageLevelSet();
    }
    public void BossCountSet(int i) 
    {
        bossCount = i;
    }
    public void BossCountMinus(int i)
    {
        bossCount -= i;
        if (bossCount <= 0) 
        {
            CurrentStage.OpenTopDoor();
        }
    }

    public void NextLevel() 
    {
        if (CurrentStage.roomNumber > EndingTowerStage)
        {
            if (false)//엔딩 미완성
            {

            }
            else //
            {
                //ui test play 종료 땡큐
                thankDemoUI.SetActive(true);
            }
            //to do end
        }
        else if (false)  // 마지막 보스 연출
        {
            //영상 연출 후 코루틴 돌려서 어캐든 아래꺼 하면 될듯
        }
        else
        {
            TowerLevelCount++;
            CurrentStage = StageTree[TowerLevelCount];
            StageLevelSet();
        }
    }
    public void StageLevelSet() 
    {
        CurrentStage.ReadyStage();
        playerOBJ.transform.position = CurrentStage.PlayerSpawnPoint.position;
    }
}
