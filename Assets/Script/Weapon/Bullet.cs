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
    //�浹�� ���� ����
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
        CancelInvoke(); // 풀 재사용 시 이전 Invoke("Destroy") 예약 취소
        time = 0f;      // 재사용 시 수명 타이머 초기화
        // BulletLifeTime은 BS()에서 이미 stat 값으로 세팅됨 - 여기서 추가 변경 없음
        _direction = transform.right;
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
            //Debug.Log("�ð��Ǽ� �����");
            Destroy();
        }
        if (locator)
        {
            ATK -= ATK * 3f * Time.deltaTime;
            Debug.Log($"���������� ���� {ATK} �ð� {time}");
        }
        if (sniping)
        {
            ATK += ATK * 2f * Time.deltaTime;
            Debug.Log($"���������� ���� {ATK} �ð� {time}");
        }
    }

    public void Destroy()
    {
        //Destroy(gameObject);
        //Debug.Log("�����");
        time = 0f;
        gameObject.SetActive(false);
    }




    private void OnTriggerEnter2D(Collider2D collision)//TO DEL �� �κ��� ��������1201�� �����Ͽ� �ۼ��Ͽ��� �մϴ�
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall")) //벽이라면
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
            }
            else
            {
                Destroy(); // Invoke 대신 즉시 호출 - 재사용 총알에 Invoke 잔류 방지
            }
            return;
        }
        /*
        //���� ��ų�� �ƴ� ������ �Ѿ��̶�� ���Ͱ� �ƴ϶�� ����
        else if (targets.ContainsValue((int)BulletTarget.Player)
            && !targets.ContainsValue((int)BulletTarget.Enemy)
            && collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Invoke("Destroy", 0.01f);
            return;
        }
        */
        //���� ������ �÷��̾��� �Ѿ��̶�� ���϶�����
        else if (Penetrate)
        {
            return;
        }
        //�÷��̾��� �Ѿ��� ���Ϳ��Ժε����� ����
        else if (targets.ContainsValue((int)BulletTarget.Enemy) && collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Destroy(); // Invoke 대신 즉시 호출
            return;
        }
    }
}