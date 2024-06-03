using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BossBase : MonoBehaviour
{
    public EnemySO MainSO;
    //[HideInInspector] 
    public float atk;             // 공격력
    //[HideInInspector] 
    public float maxHp;              // 체력
    //[HideInInspector] 
    public float curHp;
    //[HideInInspector] 
    public float speed;
    //[HideInInspector] 
    public int bossCount;
    //[HideInInspector] 
    public bool live;
    //[HideInInspector] 
    public GameObject Player;

    public bool wait =true;
    public bool invincibility;

    // Start is called before the first frame update
    public virtual void StatSet() 
    {
        
        atk = MainSO.atk;
        maxHp = MainSO.hp;
        curHp = MainSO.hp;
        speed = MainSO.normalMoveSpeed;
        live = true;
        Player = GameManager.Instance.playerOBJ;
        // 위치 설정 추측상 this.pos this.transform.position = 보스 스폰 포지션
        GameManager.Instance.bossCount = MainSO.bossCount;
        
        //아래 시간 대로 보스 시작 무적 설정 추후 시작 애니메이션이 있다면 그렇겠지 아니면 1초겠지
        // 그렇게 된다면 1초를 에네미 베이스에 만들어서 스타트 모션 세컨드 같은걸로 만들어야겠지
        StartInvincibilityNSecond(1f);
        WaitPls(1f);
        FirstPls(1f);
    }

    public virtual void Damege(float damege) 
    {
        if (invincibility)
        {
            //무적처리 비워둬도 될수도?
        }
        else 
        {
        curHp -= damege;
        if (curHp <= 0) 
            {
                BossDie();
            }
        }
    }
    public void FirstPls(float second) 
    {
        Invoke("First",second);
    }
    public virtual void First() 
    { 

    }

    public virtual void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerStatControl playerStat = collision.gameObject.GetComponent<PlayerStatControl>();
        if (playerStat) 
        {
            playerStat.Damage(atk);
            Debug.Log($"남은체력 {playerStat}");
        }
    }
    public void BossDie() 
    {
        live = false;
        GameManager.Instance.BossCountMinus(bossCount);
        Destroy(this);
    }

    public void StartInvincibilityNSecond(float i)
    {
        invincibility = true;
        Invoke("invincibility", i);
    }

    public void endinvincibility()
    {
        invincibility = false;
    }
    public void WaitPls(float second)
    {
        wait = true;
        Invoke("WaitStop", second);
    }
    public void WaitStop(float second)
    {
        wait = false;
    }
}
