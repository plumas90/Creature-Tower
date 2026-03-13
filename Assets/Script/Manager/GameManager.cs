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
    //TO DO ���� �� �������� ���� ���� ó���� ���� X ���� ���صΰ� ��ǥ�̵����� ���� �� ���°� �������� �׷��� ���� �� ������ ���Ѱ� �ʿ��� �װ� ���ӵ���Ÿ 
    // ��Ŭ���� ���θ� �װſ� �����ϴ½����� üũ �ϴ°� ������ ���ٰ� ������


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
    //public PlayerDataSetting characterSetting; // �÷��̾��� ����
    public bool isDie; // �÷��̾� ���� ����
    //public int Gold;  // ���
    public int Life; //���

    [Header("GameData")]
    //public StageData stageData;//�������� ������ �ʿ� ������ ������ Ȥ�� ��
    private int TowerLevelCount; // ����
    //public int currentMonsterCount; // ���� ���� ���ͼ� ����ī���� ���� �׳�
    public int bossCount;
    //public bool clearRoom;// ��Ŭ����� �濡�� ������
    private int EndingTowerStage = 0; // ��������


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

    [Header("UI")] //�ǹ� �Ұ�
    public GameObject StageInfoUI; //�������� UI  // �� �ö󰡴� ���
    public GameObject thankDemoUI; // ���� �÷��� ���� UI ũ���� ����

    #region ���
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
    /* �̺�Ʈ ���
    public event Action OnGameStartedEvent; // ���� ���۽� �̺�Ʈ (������)
    public event Action OnGameEndedEvent; // ���� ������ �̺�Ʈ (������)
    public event Action OnGameClearedEvent; // ���� Ŭ����� �̺�Ʈ
    public event Action OnPlayerDieEvent; // �÷��̾� ����� �̺�Ʈ
    public event Action OnGameOverEvent; // ���� ������ �̺�Ʈ


    //public event Action OnOverCheckEvent; // �ٸ� �÷��̾� ����� ���� �����ڸ� ���� �̺�Ʈ
    public event Action OnUIPlayingStateChanged; // UI�� �÷��̽� �̺�Ʈ
    public event Action OnStartStateChanged; // ��ŸƮ ������Ʈ�� �̺�Ʈ
    public event Action OnPlayingStateChanged; // �÷��� ������Ʈ�� �̺�Ʈ
    public event Action OnEndStateChanged; // ���� �� �̺�Ʈ
    public event Action OnAugmentListingStateChanged; // ���� ������Ʈ�� �̺�Ʈ
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
        //�̰ź��� �� ���� ����� �ִٰ� �����Ѵ� �ٵ� �𸣰ھ� �ù� �ٵ� �� ���� �̰� �ϼ��ص־��� �ù�
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
            List<GameObject> curStageList = stageList[i];
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
            if (false)//���� �̿ϼ�
            {

            }
            else //
            {
                //ui test play ���� ��ť
                thankDemoUI.SetActive(true);
            }
            //to do end
        }
        else if (false)  // ������ ���� ����
        {
            //���� ���� �� �ڷ�ƾ ������ ��ĳ�� �Ʒ��� �ϸ� �ɵ�
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
        if (CurrentStage == null) return;
        CurrentStage.ReadyStage();
        playerOBJ.transform.position = CurrentStage.PlayerSpawnPoint.position;
        OnStageStartEvent?.Invoke();
    }
}
