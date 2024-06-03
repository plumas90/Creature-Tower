using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.XR.OpenVR;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem.XR;

public class ThreeMonkeyBoss : BossBase
{
    // Start is called before the first frame update
    public BaseMonkey secondMouseMonkeySO;
    public BaseMonkey LastEarMonkeySO;


    public GameObject tower3EarOBJ;
    public GameObject tower2MouseOBJ;
    public GameObject tower1EyeOBJ;

    public GameObject prefab1TowerEye;
    public GameObject prefab2TowerMouse;

    private GameObject _tower1Eye;
    private GameObject _tower2Mouse;

    private BoxCollider2D _boxCollider2D;


    private Vector3 zero = new Vector3(0,0);
    private Vector3 midlle = new Vector3(0,1.5f);

    int makeMonkeyCount;

    object targetPlayer;
    Transform targetPlayerTransform;
    Vector2 direction = Vector2.zero;
    public override void StatSet() 
    {
        base.StatSet();
        _boxCollider2D = this.GetComponent<BoxCollider2D>();
        makeMonkeyCount = 1;
        //atk= 
        targetPlayerTransform = Player.transform;
    }


    // Update is called once per frame
    public void Update()
    {
        if (wait)
        {

        }
        else 
        {
            transform.Translate(direction * speed * Time.deltaTime);
        }
    }
    
    //TODO   레이어 바꾸기 컨 해야됨;
    public override void First()
    {
        GetDirection();
    }
    public void GetDirection() 
    {
        Vector2 me = transform.position;
        Vector2 u = targetPlayerTransform.position;
        direction = (u - me).normalized;
    }
    public override void OnCollisionEnter2D(Collision2D collision)
    {
            base.OnCollisionEnter2D(collision);
        //벽 혹은 플레이어 혹은 원숭이 일때 반사각 처리 
        if ((collision.gameObject.layer == LayerMask.NameToLayer("Wall")
            || collision.gameObject.layer == LayerMask.NameToLayer("Player")
            || collision.gameObject.layer == LayerMask.NameToLayer("Creatuer")))
        {
            Vector3 normal = collision.contacts[0].normal; // 법선벡터
            direction = Vector3.Reflect(direction, normal).normalized; // 반사
        }

    }
    public override void Damege(float damege)
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
                if (makeMonkeyCount == 1)
                {
                    tower1fire();
                    StatReSetting(secondMouseMonkeySO);
                }
                else if (makeMonkeyCount == 2)
                {
                    tower2fire();
                    StatReSetting(LastEarMonkeySO);
                }
                else if(makeMonkeyCount == 3)//내가 그게 된 상태
                {
                    KillBro();
                    BossDie();
                }

            }
        }
    }
    public void KillBro()
    {
        _tower1Eye.SetActive(false);
        _tower1Eye.GetComponent<MonkeyPart>().BossDie();
        _tower2Mouse.SetActive(false);
        _tower2Mouse.GetComponent<MonkeyPart>().BossDie();
    }
    public void StatReSetting(EnemySO enemyso) 
    {
        atk = enemyso.atk;
        maxHp = enemyso.hp;
        curHp = enemyso.hp;
        speed = enemyso.normalMoveSpeed;
    }


    public void tower1fire() 
    {
        //프리팹소환
        _boxCollider2D.enabled = false; // 충돌 비활성화 앞에 애가 지나가는걸 기다림

        _tower1Eye =Instantiate(prefab1TowerEye);
        _tower1Eye.transform.position = this.transform.position;
        _tower1Eye.SetActive(true);
        _tower1Eye.GetComponent<MonkeyPart>().Init(direction);


        Invoke("OnCol", 1f);

        ++makeMonkeyCount;
        tower1EyeOBJ.SetActive(false);
        //타워 2 3 포지션 내려주기
        StartCoroutine(Run(1, tower2MouseOBJ, zero));
        StartCoroutine(Run(1, tower3EarOBJ, midlle));
        //1초무적
        StartInvincibilityNSecond(1f);
        WaitPls(1f);
    }
    public void tower2fire()
    {
        //프리팹소환
        _boxCollider2D.enabled = false; // 충돌 비활성화 앞에 애가 지나가는걸 기다림

        _tower2Mouse = Instantiate(prefab1TowerEye);
        _tower2Mouse.transform.position = this.transform.position;
        _tower2Mouse.SetActive(true);
        _tower2Mouse.GetComponent<MonkeyPart>().Init(direction);


        Invoke("OnCol", 1f);

        ++makeMonkeyCount;
        tower2MouseOBJ.SetActive(false);
        //타워 3 포지션 내려주기
        StartCoroutine(Run(1,tower3EarOBJ,zero));

        StartInvincibilityNSecond(1f);
        WaitPls(1f);
    }
    public void OnCol() 
    {
        _boxCollider2D.enabled = true;
    }

    IEnumerator Run(float duration , GameObject target , Vector3 endposition)
    {
        var runTime = 0.0f;
        Transform moveTarget = target.transform;
        while (runTime < duration)
        {
            runTime += Time.deltaTime;

            moveTarget.position = Vector3.Lerp(moveTarget.position, endposition, runTime / duration);

            yield return null;
        }
    }

}
