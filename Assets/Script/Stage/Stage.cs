using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Stage : MonoBehaviour
{
    public int roomNumber;
    [Header("Boss")]
    public GameObject bossOBJ;
    [SerializeField]public BossBase BossBase;

    [Header("Door")]
    public Door botDoor;
    public Door topDoor;

    [Header("Choice Result")]
    public RandomHealPoint randomHealPoint;
    public ResultDNA resultDNA;

    [Header("Spawn Point")]
    public GameObject spawnPointStairs;
    public Transform PlayerSpawnPoint;
    public NextStageStairs NextStageStairs;
    public Transform BossSpawnPoint;
    public SpriteRenderer bossSpawnSprite;


    private bool firstIn;


    // Start is called before the first frame update
    private void Awake()
    {
        //��Ҽ���
        BossBase = bossOBJ.GetComponent<BossBase>();
        bossOBJ.transform.position = BossSpawnPoint.position;


        bossSpawnSprite.color = new Color(0, 0, 0, 0);
        //�Ʒ��� ������Ƽ�� ���ΰ� �����
        bossOBJ.SetActive(false);

        //������
        ResultSummon();

        firstIn = true;

        //������Ʈ �������� �� ���
        ObjActiveFalse();


    }
    public void ReadyStage() 
    {
        ObjActiveTrue();
        botDoor.gameObject.SetActive(false);
        GameManager.Instance.bossCount = BossBase.bossCount;
        //���ӸŴ��� ���������� �Ʒ��� �������� �ű��.
    }
    //public void NextGo(GameObject player)  ���� ��¼�ٺ��� �ؽ�Ʈ�����������׾�� ���ӸŴ��� ȣ���ؼ� ������
    //{
    //    player.transform.position = GameManager.Instance.StageTree[roomNumber + 1].PlayerSpawnPoint.position;
    //}

    public void InCheckClear()
    {
        if (firstIn) 
        {
            SummonBoss();
            CloseBotDoor();
            firstIn = false;
        }
    }

    public void SummonBoss() 
    {
        BossBase.StatSet();
        bossOBJ.SetActive(true);
    }

    public void ResultSummon() 
    {
        randomHealPoint.MakePotion();
        if (resultDNA != null)
            resultDNA.Init();
    }
    public void ObjActiveTrue() 
    {
        this.gameObject.SetActive(true);
    }
    public void ObjActiveFalse() 
    {
        this.gameObject.SetActive(false);
    }

    public void OpenBotDoor() 
    {
        botDoor.UnLock();
    }
    public void CloseBotDoor() 
    {
        botDoor.Lock();
    }
    public void OpenTopDoor() 
    {
        topDoor.UnLock();
    }
    public void CloseTopDoor() 
    {
        topDoor.Lock();
    }

}
