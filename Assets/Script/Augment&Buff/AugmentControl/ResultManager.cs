using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ResultManager : MonoBehaviour//vs�ڵ�
{
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
        int tier = RandomTier();
        switch (tier)
        {
            case 1:
                PickSpecialList(SpecialAugment1);
                break;
            case 2:
                PickSpecialList(SpecialAugment2);
                break;
            case 3:
            case 4:
                PickSpecialList(SpecialAugment3);
                break;
            default:
                PickSpecialList(SpecialAugment1);
                break;
        }
    }
  
    void PickStatList(List<IAugment> origin)// ������ �Ȼ縮���� Ÿ�� = �Ϲݽ���
    {
        playerinput.actions.FindAction("Attack").Disable();

        if (SetActiveCheck) 
        {
            picklist[0].pick();
        }
        int Count = picklist.Length;
        //���⼭ ������������ Ư�� ���������� ����������Ʈ���� �׳� ������
        List<IAugment> list = origin.ToList();

        for (int i = 0; i < Count; ++i)
        {
            int a = Random.Range(0, list.Count);
            picklist[i].stat = list[a];
            picklist[i].gameObject.SetActive(true);
            list.RemoveAt(a);
        }
        IsStat = true;// �̰ɷ� ����Ʈ���� �������� �״������ ������
        SetActiveCheck = true;
    }

    void PickSpecialList(List<SpecialAugment> origin) // ������ ������� Ÿ�� == �÷��̺�ȭ ����
    {
        playerinput.actions.FindAction("Attack").Disable();

        if (SetActiveCheck)
        {
            picklist[0].pick();
        }
        int Count = picklist.Length;
        List<SpecialAugment> list = origin.ToList();
        tempList = origin;
        for (int i = 0; i < Count; ++i)
        {
            int a = Random.Range(0, list.Count);
            picklist[i].stat = list[a];
            picklist[i].gameObject.SetActive(true);
            list.RemoveAt(a);
        }
        SetActiveCheck = true;
        IsStat = false;

    }
    public void close()//��Ͽ��� ����ٸ� ��� ui�� �ݾ���
    {
        playerinput.actions.FindAction("Attack").Enable();

        int Count = picklist.Length;
        for (int i = 0; i < Count; ++i)
        {
            if (picklist[i].Ispick && !IsStat)
            {
                int target= picklist[i].stat.Code;
                int index = tempList.FindIndex(x => x.Code.Equals(target));
                //����Ʈ���� �̸� ã�Ƽ� ����
                MySpecialListSocket newSocket = Instantiate(Socketprefab);//
                newSocket.transform.SetParent(ViewListContent,false);//���������� �����ϸ鼭 �������� �����ɼ��� ���� �¾��� �����������޽��ϴϱ��ذ��
                newSocket.Init(tempList[index].Name, tempList[index].func, tempList[index].Rare, tempList[index].Code);

                tempList.Remove(tempList[index]);
                if (tempList.Count <= 2) 
                {
                    SpecialAugment AllStat = new SpecialAugment("All Stat",999,"���ذ� ���� �ý���", 3);
                    tempList.Add(AllStat);
                }



            }
            picklist[i].gameObject.SetActive(false);
            
        }
        if (!IsStat && !statChance)
        {
            Ready();
        }
        statChance = false;
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

}
