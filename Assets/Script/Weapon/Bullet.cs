using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BulletTarget
{
    Player,
    Enemy
}

public class Bullet : MonoBehaviour
{
    //충돌할 면의 벡터
    Vector2 collisionVector;


    public float ATK;
    public float BulletLifeTime;
    public float BulletSpeed = 15;
    public bool IsDamage = false;
    public bool fire;
    public bool water;
    public bool ice;
    public bool burn;
    public bool gravity;
    public bool Penetrate;
    public bool canresurrection;
    public bool sniperAtkBuff;

    private HumanAttackintelligentmissile missile;

    public Dictionary<string, int> targets;



    //targets.Contains(BulletTarget.Enemy)
    public Vector2 _direction;
    float time = 0f;
    public int layerMask;

    public bool locator;
    public bool sniping;

    public int BulletOwner;

    RaycastHit2D hit;
    public bool canAngle;
    Vector3 income;
    Vector3 normal;
    private void Awake()
    {
        targets = new Dictionary<string, int>();
    }
    public void Init()
    {
        BulletLifeTime = UnityEngine.Random.Range(BulletLifeTime * 0.15f, BulletLifeTime * 0.2f);
        //Debug.Log($"인잇 남은 시간 {BulletLifeTime}");
        //Invoke("Destroy", BulletLifeTime);
        _direction = transform.right;
        //to del 아래
        layerMask = 1 << LayerMask.NameToLayer("Wall");
    }
    public void MissileFire(int i)
    {
        missile = GetComponentInChildren<HumanAttackintelligentmissile>();
        missile.init(i);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.right * BulletSpeed * Time.deltaTime);
        time += Time.deltaTime;
        if (time >= BulletLifeTime)
        {
            //Debug.Log("시간되서 사라짐");
            Destroy();
        }
        if (locator)
        {
            ATK -= ATK * 3f * Time.deltaTime;
            Debug.Log($"약해지는중 현재 {ATK} 시간 {time}");
        }
        if (sniping)
        {
            ATK += ATK * 2f * Time.deltaTime;
            Debug.Log($"강해지는중 현재 {ATK} 시간 {time}");
        }
    }

    public void Destroy()
    {
        //Destroy(gameObject);
        //Debug.Log("사라짐");
        time = 0f;
        gameObject.SetActive(false);
    }




    private void OnTriggerEnter2D(Collider2D collision)//TO DEL 이 부분은 스나이퍼1201을 고려하여 작성하여야 합니다
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall")) //만약벽이라면
        {

            if (canAngle)
            {

                hit = Physics2D.Raycast(this.transform.position, _direction, BulletSpeed * BulletLifeTime, layerMask);
                Debug.DrawRay(this.transform.position, _direction, UnityEngine.Color.red, 3f);

                Vector3 reflectVector = Vector3.Reflect(_direction, hit.normal).normalized;
                float angle = Mathf.Atan2(reflectVector.y, reflectVector.x) * Mathf.Rad2Deg;


                Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, angle));
                this.transform.rotation = rotation;
                _direction = reflectVector;
                //Debug.DrawRay(this.transform.position, reflectVector, UnityEngine.Color.red, 3f);
            }
            else
            {
                Invoke("Destroy", 0.01f);
            }
            return;
        }
        /*
        //만약 팀킬이 아닌 몬스터의 총알이라면 몬스터가 아니라면 삭제
        else if (targets.ContainsValue((int)BulletTarget.Player)
            && !targets.ContainsValue((int)BulletTarget.Enemy)
            && collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Invoke("Destroy", 0.01f);
            return;
        }
        */
        //만약 관통인 플레이어의 총알이라면 벽일때삭제
        else if (Penetrate)
        {
            return;
        }
        //플레이어의 총알이 몬스터에게부딪혀서 삭제
        else if (targets.ContainsValue((int)BulletTarget.Enemy) && collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Invoke("Destroy", 0.01f);
            return;
        }
    }
}