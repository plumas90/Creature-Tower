using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    //TO DO 현재 방 스테이지 정보 현재 처음에 층수 X 맵을 다해두고 좌표이동으로 다음 층 가는걸 생각중임 그래서 현재 층 정보에 대한게 필요함 그게 게임데이타 
    // 방클리어 여부를 그거에 연동하는식으로 체크 하는게 좋을거 같다고 생각됨
    public enum GameStates
    {
        Init,
        UIPlaying,
        Start,
        Playing,
        End,
        AugmentListing,
    }

    [Header("PlayerData")]
    public GameObject playerOBJ;
    public PlayerDataSetting characterSetting; // 플레이어의 정보
    public bool isDie; // 플레이어 죽음 여부
    public int Gold;  // 골드
    public int Life; //목숨

    [Header("GameData")]
    //public StageData stageData;//스테이지 데이터 필요 없을거 같은데 혹시 모름
    public int TowerLevelCount; // 층수
    public int currentMonsterCount; // 현재 남은 몬스터수
    public int bossCount;
    public bool clearRoom;// 방클리어여부
    public int EndingTowerStage; // 마지막층

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


    [Header("UI")] //의미 불가
    public GameObject StageInfoUI; //스테이지 UI

    [HideInInspector]
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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        bossCount = 0;
    }
    public void Init(GameObject player) 
    {
        playerOBJ = player;
        Life = 0;
        clearRoom = false;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
            RoolOpen();
        }
    }
    public void RoolOpen()
    {
        clearRoom = true;
    }
    public void RoolClose()
    {
        clearRoom = false;
    }
}
