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
    public GameObject ResultPickPoint;

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
        //브소설정
        BossBase = bossOBJ.GetComponent<BossBase>();
        bossOBJ.transform.position = BossSpawnPoint.position;


        bossSpawnSprite.color = new Color(0, 0, 0, 0);
        //아래거 보스액티브 꺼두고 지우기
        bossOBJ.SetActive(false);

        //보상설정
        ResultSummon();

        firstIn = true;

        //오브젝트 꺼둠으로 써 대기
        ObjActiveFalse();


    }
    public void ReadyStage() 
    {
        ObjActiveTrue();
        botDoor.gameObject.SetActive(false);
        GameManager.Instance.bossCount = BossBase.bossCount;
        //게임매니저 레디스테이지 아래에 포지션을 옮길것.
    }
    //public void NextGo(GameObject player)  스읍 어쩌다보니 넥스트스테이지스테어에서 게임매니저 호출해서 실행함
    //{
    //    player.transform.position = GameManager.Instance.StageTree[roomNumber + 1].PlayerSpawnPoint.position;
    //}

    public void InCheckClear()
    {
        if (!firstIn) 
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
        //리절트 보상처리
        //ResultPickPoint. todo
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
