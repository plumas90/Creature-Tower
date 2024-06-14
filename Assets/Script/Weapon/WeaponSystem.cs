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
    private GameObject bullet;
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

    // 추가
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
            Queue<GameObject> objectsPool = new Queue<GameObject>();
            for (int i = 0; i < pool.count; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                if (poolParent != null) 
                {
                    obj.transform.SetParent(poolParent.transform);

                }
                obj.GetComponent<SpriteRenderer>().sprite = playerStatHandler.BulletSprite;
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
        //pv = GetComponent<PhotonView>(); 포톤 삭제
        _controller = GetComponent<TopDownCharacterController>();
        //_viewID = pv.ViewID; 포톤 삭제
        playerStatHandler = GetComponent<PlayerStatControl>();
        //target = BulletTarget.Enemy; 아래2번째줄 불렛타겟.에네미로 대체
        targets = new Dictionary<string, int>();
        targets["Enemy"] = (int)BulletTarget.Enemy;
        // 추가
        sizeUp = false; //총알크기 재능 체크
        sizeBody = false; // 몸 크기 재능 체크
        locator = false; // 근접 강함 체크
        sniping = false; // 멀음 강함 체크
        fire = false; // 불 디버프 체크
        water = false; // 물 디버프 체크
        //ice = false; // 얼음 디버프 체크 = 몹시스템 관련 변경 필요
        burn = false;  // 화상 체크 
        gravity = false; // 중력 당김 체크
        Penetrate = false; //  관통 체크
        pivotSet = false; // 피벗세팅 
        //canresurrection = false; // 부활 체크 멀티 x
        //sniperAtkBuff = false; // 공벞 체크 멀티 x
        canAngle = false; // 벽 튕김 체크 
        weaponType = WeaponType.Shooting;
        // finalAttackCoeff = 1; 차징 체크
        humanAttackintelligentmissile = false; // 유도 체크

        _cool = GetComponent<CoolTimeController>();
        _controller.OnAttackEvent += Shooting; // 공격 속도 관련 체크
    }
    private void Start()
    {
        StartObjectPOOL();
    }


    public void Shooting()
    {
        for (int i = 0; i < _controller.playerStatHandler.LaunchVolume.total; i++)
        {
            Quaternion rot = muzzleOfAGun.transform.rotation;
            rot.eulerAngles += new Vector3(0, 0, Random.Range(-1 * _controller.playerStatHandler.BulletSpread.total, _controller.playerStatHandler.BulletSpread.total));// 중요함
            float _ATK = _controller.playerStatHandler.ATK.total;
            float _BLT = _controller.playerStatHandler.BulletLifeTime.total;
            var _targets = targets;
            bool _isDamage = isDamage;
            BS(rot, _ATK, _BLT, _targets, _isDamage);
            //pv.RPC("BS", RpcTarget.All, rot, _ATK, _BLT, _targets, _isDamage, _viewID);
        }
        _controller.playerStatHandler.CurAmmo--;
    }

    /*public void Charging() 차징 묻어두기
    {
        int bullets = _cool.bulletNum;
        if (bullets <= 1)
            return;

        OnFinalDamageEvent?.Invoke();

        for (int i = 0; i < bullets; i++)
        {
            Quaternion rot = muzzleOfAGun.transform.rotation;
            rot.eulerAngles += new Vector3(0, 0, Random.Range(-1 * _controller.playerStatHandler.BulletSpread.total, _controller.playerStatHandler.BulletSpread.total));// 중요함
            float _ATK = _controller.playerStatHandler.ATK.total * finalAttackCoeff;
            float _BLT = _controller.playerStatHandler.BulletLifeTime.total;
            var _targets = targets;
            bool _isDamage = isDamage;

            pv.RPC("BS", RpcTarget.All, rot, _ATK, _BLT, _targets, _isDamage, _viewID);
        }
        finalAttackCoeff = 1;
        _cool.bulletNum = 0;
    } */ 
    public void burstCall(Quaternion rot) //라이플 스킬 재능
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
        if (critical >= criticalchance)// 크리티컬시 데미지 1.5배 처리
        {
            Atk = Atk * 1.5f;
        }
        //Debug.Log("타겟");
        //foreach (var target in _targets)
        //{
        //    Debug.Log(target);
        //}
        //Debug.Log("데미지를 주는가?");
        //Debug.Log(_isDamage);
        float size = 1f; // 사이즈 처리전 기본 값

        if (sizeBody) // 바디 사이즈 비례 총알크기 재능 처리
        {
            size = transform.localScale.x;
        }
        if (sizeUp) // 총알크기업 처리
        {
            size *= 1.3f;
        }


        Vector3 bulletPositon = muzzleOfAGun.transform.position;// 총알 생성 위치

        Vector3 eulerRotation = rot.eulerAngles; // euler 값으로 변경
        if (pivotSet)
        {
            bulletPositon = this.gameObject.transform.localPosition;
        }
        GameObject _object = SpawnFromPool(nameTag);
        _object.transform.position = bulletPositon;
        //GameObject _object = Instantiate(bullet, bulletPositon, Quaternion.identity); // 총알 생성
        _object.transform.rotation = Quaternion.Euler(eulerRotation);
        Bullet _bullet = _object.GetComponent<Bullet>(); //해당 총알에 내 특성 부여 위한 객체 가져오기

        _object.transform.localScale = new Vector2(size, size); //크기 처리
        //현재 가까울시/ 멀시 데미지 처리 방식 - 기본 데미지 * 배율 +- 체공시 증감값  == 기본데미지*배율(총알생성시 처리) 총알 체공 증감값(총알 실시간 처리)
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
        //Debug.Log($"남은시간 {bulletLifeTime}");
    }

        /*[PunRPC] 원본 총알 생성 보관용
        public void BS(Quaternion rot, float Atk, float bulletLifeTime, Dictionary<string, int> _targets, bool _isDamage, int _viewID)//BulletSpawn
        {
            float critical = playerStatHandler.Critical.total;
            int criticalchance = Random.Range(1, 101);
            if (critical >= criticalchance)
            {
                Atk = Atk * 1.5f;
            }
            //Debug.Log("타겟");
            foreach (var target in _targets)
            {
                //Debug.Log(target);
            }
            //Debug.Log("데미지를 주는가?");
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
