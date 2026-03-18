using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    public enum WeaponType
    {
        Shooting,
        Charging,
    }

    private PlayerStatControl playerStatHandler;
    private TopDownCharacterController _controller;
    //private PhotonView pv;
    public Transform muzzleOfAGun;
    [SerializeField]private GameObject bullet;
    public Dictionary<string, int> targets;
    public bool isDamage;
    public bool sizeUp;
    public bool sizeBody;
    public bool locator;
    public bool sniping;
    public bool canAngle;

    public bool fire;
    public bool water;
    public bool ice;
    public bool burn;
    public bool gravity;
    public bool Penetrate;

    public bool pivotSet;
    public bool humanAttackintelligentmissile;
    public bool canresurrection;
    public bool sniperAtkBuff;

    // �߰�
    public int bulletNum;
    private CoolTimeController _cool;
    public string nameTag;

    public WeaponType weaponType;

    //public event Action OnFinalDamageEvent;
    //public float finalAttackCoeff;
    [System.Serializable]
    public struct Pool
    {
        public string tag;
        public GameObject prefab;
        public int count;
    }
    public GameObject poolParent;
    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    public void StartObjectPOOL()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        foreach (var pool in pools)
        {
            // pool.prefab이 미설정이면 Awake에서 Resources.Load한 bullet 필드로 대체
            GameObject prefabToUse = pool.prefab != null ? pool.prefab : bullet;
            if (prefabToUse == null)
            {
                Debug.LogError($"[WeaponSystem] 풀 '{pool.tag}'의 prefab이 없고 bullet 필드도 null입니다. 총알을 생성할 수 없습니다.");
                continue;
            }

            Queue<GameObject> objectsPool = new Queue<GameObject>();
            for (int i = 0; i < pool.count; i++)
            {
                GameObject obj = Instantiate(prefabToUse);
                if (poolParent != null)
                {
                    obj.transform.SetParent(poolParent.transform);
                }
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (playerStatHandler.BulletSprite != null)
                        sr.sprite = playerStatHandler.BulletSprite;
                    sr.sortingOrder = 9; // 레이어 규칙: 플레이어 아래 오브젝트
                }
                obj.SetActive(false);
                objectsPool.Enqueue(obj);
            }
            poolDictionary.Add(pool.tag, objectsPool);
        }
        nameTag = pools[0].tag;
    }



    public GameObject SpawnFromPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
            return null;

        GameObject obj = poolDictionary[tag].Dequeue();
        poolDictionary[tag].Enqueue(obj);
        return obj;
    }

    private void Awake()
    {
        isDamage = true;
        bullet = Resources.Load<GameObject>("Prefabs/Player/Bullet");
        if (bullet == null)
        {
            // Import mismatch fallback: some revisions keep Bullet under PlayerHUD.
            bullet = Resources.Load<GameObject>("Prefabs/PlayerHUD/Bullet");
        }
        //pv = GetComponent<PhotonView>(); ���� ����
        _controller = GetComponent<TopDownCharacterController>();
        //_viewID = pv.ViewID; ���� ����
        playerStatHandler = GetComponent<PlayerStatControl>();
        //target = BulletTarget.Enemy; �Ʒ�2��°�� �ҷ�Ÿ��.���׹̷� ��ü
        targets = new Dictionary<string, int>();
        targets["Enemy"] = (int)BulletTarget.Enemy;
        // �߰�
        sizeUp = false; //�Ѿ�ũ�� ��� üũ
        sizeBody = false; // �� ũ�� ��� üũ
        locator = false; // ���� ���� üũ
        sniping = false; // ���� ���� üũ
        fire = false; // �� ����� üũ
        water = false; // �� ����� üũ
        //ice = false; // ���� ����� üũ = ���ý��� ���� ���� �ʿ�
        burn = false;  // ȭ�� üũ 
        gravity = false; // �߷� ��� üũ
        Penetrate = false; //  ���� üũ
        pivotSet = false; // �ǹ����� 
        //canresurrection = false; // ��Ȱ üũ ��Ƽ x
        //sniperAtkBuff = false; // ���� üũ ��Ƽ x
        canAngle = false; // �� ƨ�� üũ 
        weaponType = WeaponType.Shooting;
        // finalAttackCoeff = 1; ��¡ üũ
        humanAttackintelligentmissile = false; // ���� üũ

        _cool = GetComponent<CoolTimeController>();
        _controller.OnAttackEvent += Shooting; // ���� �ӵ� ���� üũ
    }
    private void Start()
    {
        // StartObjectPOOL()은 GameManager.Init()에서 캐릭터 선택 후 명시적으로 호출됨
    }


    public void Shooting()
    {
        for (int i = 0; i < _controller.playerStatHandler.LaunchVolume.total; i++)
        {
            Quaternion rot = muzzleOfAGun.transform.rotation;
            rot.eulerAngles += new Vector3(0, 0, Random.Range(-1 * _controller.playerStatHandler.BulletSpread.total, _controller.playerStatHandler.BulletSpread.total));// �߿���
            float _ATK = _controller.playerStatHandler.ATK.total;
            float _BLT = _controller.playerStatHandler.BulletLifeTime.total;
            var _targets = targets;
            bool _isDamage = isDamage;
            BS(rot, _ATK, _BLT, _targets, _isDamage);
            //pv.RPC("BS", RpcTarget.All, rot, _ATK, _BLT, _targets, _isDamage, _viewID);
        }
        _controller.playerStatHandler.CurAmmo--;
    }

    /*public void Charging() ��¡ ����α�
    {
        int bullets = _cool.bulletNum;
        if (bullets <= 1)
            return;

        OnFinalDamageEvent?.Invoke();

        for (int i = 0; i < bullets; i++)
        {
            Quaternion rot = muzzleOfAGun.transform.rotation;
            rot.eulerAngles += new Vector3(0, 0, Random.Range(-1 * _controller.playerStatHandler.BulletSpread.total, _controller.playerStatHandler.BulletSpread.total));// �߿���
            float _ATK = _controller.playerStatHandler.ATK.total * finalAttackCoeff;
            float _BLT = _controller.playerStatHandler.BulletLifeTime.total;
            var _targets = targets;
            bool _isDamage = isDamage;

            pv.RPC("BS", RpcTarget.All, rot, _ATK, _BLT, _targets, _isDamage, _viewID);
        }
        finalAttackCoeff = 1;
        _cool.bulletNum = 0;
    } */ 
    public void burstCall(Quaternion rot) //������ ��ų ���
    {
        float _ATK = _controller.playerStatHandler.ATK.total;
        float _BLT = _controller.playerStatHandler.BulletLifeTime.total;
        var _targets = targets;
        bool _isDamage = isDamage;
        pivotSet = true;
        BS(rot, _ATK, _BLT, _targets, _isDamage);
        //pv.RPC("BS", RpcTarget.All, rot, _ATK, _BLT, _targets, _isDamage, _viewID);
        pivotSet = false;
    }
    public void BS(Quaternion rot, float Atk, float bulletLifeTime, Dictionary<string, int> _targets, bool _isDamage)//BulletSpawn
    {
        float critical = playerStatHandler.Critical.total;
        int criticalchance = Random.Range(1, 101);
        if (critical >= criticalchance)// ũ��Ƽ�ý� ������ 1.5�� ó��
        {
            Atk = Atk * 1.5f;
        }
        //foreach (var target in _targets)
        //{
        //    Debug.Log(target);
        //}
        //Debug.Log(_isDamage);
        float size = 1f; // ������ ó���� �⺻ ��

        if (sizeBody) // �ٵ� ������ ��� �Ѿ�ũ�� ��� ó��
        {
            size = transform.localScale.x;
        }
        if (sizeUp) // �Ѿ�ũ��� ó��
        {
            size *= 1.3f;
        }


        Vector3 bulletPositon = muzzleOfAGun.transform.position;// �Ѿ� ���� ��ġ

        Vector3 eulerRotation = rot.eulerAngles; // euler ������ ����
        if (pivotSet)
        {
            bulletPositon = this.gameObject.transform.localPosition;
        }
        GameObject _object = SpawnFromPool(nameTag);
        _object.transform.position = bulletPositon;
        //GameObject _object = Instantiate(bullet, bulletPositon, Quaternion.identity); // �Ѿ� ����
        _object.transform.rotation = Quaternion.Euler(eulerRotation);
        Bullet _bullet = _object.GetComponent<Bullet>(); //�ش� �Ѿ˿� �� Ư�� �ο� ���� ��ü ��������

        _object.transform.localScale = new Vector2(size, size); //ũ�� ó��
        //���� ������/ �ֽ� ������ ó�� ��� - �⺻ ������ * ���� +- ü���� ������  == �⺻������*����(�Ѿ˻����� ó��) �Ѿ� ü�� ������(�Ѿ� �ǽð� ó��)
        _bullet.locator = locator;
        if (locator)
        {
            Atk += Atk * 1f;
        }
        _bullet.sniping = sniping;
        if (sniping)
        {
            Atk -= Atk * 0.3f;
        }
        _bullet.ATK = Atk;
        _bullet.BulletLifeTime = bulletLifeTime;
        _bullet.targets = _targets;
        _bullet.IsDamage = _isDamage;
        //_object.GetComponent<SpriteRenderer>().sprite = _controller.playerStatHandler.BulletSprite;
        _bullet.fire = fire;
        _bullet.water = water;
        _bullet.ice = ice;
        _bullet.burn = burn;
        _bullet.gravity = gravity;
        _bullet.Penetrate = Penetrate;
        _bullet.canresurrection = canresurrection;
        _bullet.sniperAtkBuff = sniperAtkBuff;
        _bullet.canAngle = canAngle;
        _object.GetComponent<Bullet>().Init();
        if (humanAttackintelligentmissile)
        {
            _bullet.MissileFire(1);
        }
        _object.SetActive(true);
        //Debug.Log($"�����ð� {bulletLifeTime}");
    }

        /*[PunRPC] ���� �Ѿ� ���� ������
        public void BS(Quaternion rot, float Atk, float bulletLifeTime, Dictionary<string, int> _targets, bool _isDamage, int _viewID)//BulletSpawn
        {
            float critical = playerStatHandler.Critical.total;
            int criticalchance = Random.Range(1, 101);
            if (critical >= criticalchance)
            {
                Atk = Atk * 1.5f;
            }
            foreach (var target in _targets)
            {
                //Debug.Log(target);
            }
            //Debug.Log(_isDamage);
            float size = 1f;

            if (sizeBody)
            {
                size = transform.localScale.x;
            }
            if (sizeUp)
            {
                size *= 1.3f;
            }


            Vector3 bulletPositon = muzzleOfAGun.transform.position;

            Vector3 eulerRotation = rot.eulerAngles;
            if (pivotSet)
            {
                bulletPositon = this.gameObject.transform.localPosition;
            }
            GameObject _object = Instantiate(bullet, bulletPositon, Quaternion.identity);
            _object.transform.rotation = Quaternion.Euler(eulerRotation);
            Bullet _bullet = _object.GetComponent<Bullet>();

            _object.transform.localScale = new Vector2(size, size);
            if (locator)
            {
                _bullet.locator = true;
                Atk += Atk * 1f;
            }
            if (sniping)
            {
                _bullet.sniping = true;
                Atk -= Atk * 0.3f;
            }
            _bullet.ATK = Atk;
            _bullet.BulletLifeTime = bulletLifeTime;
            _bullet.targets = _targets;
            _bullet.IsDamage = _isDamage;
            _bullet.BulletOwner = _viewID;
            _object.GetComponent<SpriteRenderer>().sprite = _controller.playerStatHandler.BulletSprite;
            _bullet.fire = fire;
            _bullet.water = water;
            _bullet.ice = ice;
            _bullet.burn = burn;
            _bullet.gravity = gravity;
            _bullet.Penetrate = Penetrate;
            _bullet.canresurrection = canresurrection;
            _bullet.sniperAtkBuff = sniperAtkBuff;
            _bullet.canAngle = canAngle;
            _object.GetComponent<Bullet>().Init();
            if (humanAttackintelligentmissile)
            {
                _bullet.MissileFire(1);
            }
        }*/
    }
