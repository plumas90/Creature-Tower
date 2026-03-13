using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BulletTarget
{
    Player,
    Enemy,
    All// �̺κ� ��ų������ �Ѵ��ľ��ؼ� �� �ݿ����Ѿߵɵ� �ٸ��е����� ���ϱ�
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
        time = 0f;
        BulletLifeTime = UnityEngine.Random.Range(BulletLifeTime * 0.15f, BulletLifeTime * 0.2f);
        //Invoke("Destroy", BulletLifeTime);
        _direction = transform.right;
        //to del �Ʒ�
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
        if (time>= BulletLifeTime) 
        {
            Destroy();
        }
        if (locator) 
        {
            ATK -= ATK*3f * Time.deltaTime;
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
        gameObject.SetActive(false);
    }

    private float GetAngle(Vector2 vec1, Vector2 vec2) 
    {
        float angle = (Mathf.Atan2(vec2.y, vec2.x) - Mathf.Atan2(vec1.y, vec1.x)) * Mathf.Rad2Deg;
        return angle;
    }
    public float CalculateAngle(Vector3 from, Vector3 to)
    {
        return Quaternion.FromToRotation(Vector3.up, to - from).eulerAngles.z;
    }
    public static float GetAngle2(Vector3 from, Vector3 to)
    {
        Vector3 v = to - from;
        return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }




    private void OnTriggerEnter2D(Collider2D collision)//TO DEL �� �κ��� ��������1201�� �����Ͽ� �ۼ��Ͽ��� �մϴ�
    {
        if (collision == null)
            return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall")) //���ຮ�̶��
        {

            if (canAngle)
            {

                hit = Physics2D.Raycast(this.transform.position, _direction, BulletSpeed * BulletLifeTime, layerMask);
                Debug.DrawRay(this.transform.position, _direction, UnityEngine.Color.red, 3f);

                if (!hit)
                {
                    Destroy();
                    return;
                }

                Vector3 reflectVector = Vector3.Reflect(_direction, hit.normal).normalized;
                float angle = Mathf.Atan2(reflectVector.y, reflectVector.x) * Mathf.Rad2Deg;


                Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, angle));
                this.transform.rotation = rotation;
                _direction = reflectVector;
                //Debug.DrawRay(this.transform.position, reflectVector, UnityEngine.Color.red, 3f);
            }
            else 
            {
                Destroy();
            }
            return;
        }
        //���� ��ų�� �ƴ� ������ �Ѿ��̶�� ���Ͱ� �ƴ϶�� ����
        else if (targets.ContainsValue((int)BulletTarget.Player)
            && !targets.ContainsValue((int)BulletTarget.Enemy)
            && collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Destroy();
            return;
        }
        //���� ������ �÷��̾��� �Ѿ��̶�� ���϶�����
        else if (Penetrate)
        {
            return;
        }
        //�÷��̾��� �Ѿ��� ���Ϳ��Ժε����� ����
        else if (targets.ContainsValue((int)BulletTarget.Enemy) && collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Destroy();
            return;
        }
    }
}
