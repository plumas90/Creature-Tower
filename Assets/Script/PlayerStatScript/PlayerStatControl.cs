using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class PlayerStatControl : MonoBehaviour
{
    // 현재 게임 매니저가 새로 만들어야 할거 같아서 게임 매니저 관련은 사용해도 주석 처리 해둠


    // 추후 방향성에 따라 안쓸수 있는 이벤트 끝에 ##을 붙여둠
    public event Action<float> GetDamege; //피격시 처음 들어온 데미지 처리  = 들어온 데미지
    public event Action<float> HitEvent2; //피격 데미지 수치 관련 처리 재능발현으로 실제 데미지 수치 처리를함  = 최종 데미지
    public event Action HitEvent; // 피격 발생 여부 처리 이벤트
    public event Action OnDieEvent; // 죽었을때 처리 그런데 싱글겜 처리라서 안쓸수도 있음 ##
    public event Action OnRegenEvent; // 부활 이벤트 싱글겜이라 이하 생략 ##
    public event Action<int> OnRegenCalculateEvent; //죽었을때 셋팅인데 안쓸듯 ##
    public event Action OnChangeAmmorEvent; // 방어력 처리 안쓸 가능성 많음 ##
    public event Action OnChangeCurHPEvent; // 현재 체력 처리 ex 버서커  
    public event Action MoveStartEvent; //  가만히 있는 시간 처리1 ex 가만히 있는 동안 공격력상승 움직이면 취소 
    public event Action MoveEndEvent; // 가만히 있는 시간 처리2
    public event Action EnemyHitEvent; // 타격 n회시 추가 타격 등 처리 안쓸수 있음 ##
    public event Action KillCatchEvent; // 킬 시 공격력 상승 이벤트 등에 사용 초반 보스러쉬 진행시 사용 추후 가능성 있음 x ##
    public event Action<float, int> OnDamageReflectEvent;// 데미지 반사 처리 이벤트 구현이 생각보다 힘들고 재밌는 트리 같지가 않음 ##


    [SerializeField] private PlayerSO playerStats;

    //PlayerAnimatorController anime;


    public Stats ATK;                 // 공격력
    public Stats HP;                  // 체력
    public Stats Speed;               // 이동 속도
    public Stats AtkSpeed;            // 공격 속도
    public Stats ReloadCoolTime;      // 장전   쿨 타임
    public Stats SkillCoolTime;       // 스킬   쿨 타임
    public Stats RollCoolTime;        // 구르기 쿨 타임
    public Stats BulletSpread;        // 탄퍼짐
    public Stats BulletLifeTime;      // 총알 사거리
    public Stats LaunchVolume;        // 한번의 발사의 발사량
    public Stats Critical;            // 크리티컬 = 크리티컬 빌드가 재밌을거 같지만 데미지 난이도에 꾀 큰 영향을 줌
    public Stats AmmoMax;             // 장탄수
    public float defense; //방어력 중요한건 데미지 배율 이라는거임 기본값 1로 데미지 10 받을시 데미지 x 방어력 배율 = 실제 데미지 같은 느낌

    //public Sprite indicatorSprite; 모름
    public AudioClip atkClip;
    public AudioClip reloadStartClip;
    public AudioClip reloadFinishClip;

    [HideInInspector] public SpriteLibraryAsset PlayerSprite; // 스프라이트
    [HideInInspector] public SpriteLibraryAsset WeaponSprite; // 스프라이트
    [HideInInspector] public Sprite BulletSprite; // 스프라이트
    [HideInInspector] public SpriteLibrary PlayerSpriteCase; // 스프라이트
    [HideInInspector] public SpriteLibrary WeaponSpriteCase; // 스프라이트


    public GameObject _PlayerSprite;
    public GameObject _WeaponSprite;
    //public PlayerDebuffControl _DebuffControl; //디버프 매니저 만들어야함

    public bool isNoramlMove; //조작 좌우 반전 체크
    public bool isCanSkill; // 스킬 쿨타임 체크
    public bool isCanAtk;
    //public bool isDie; 싱글겜이라 필요없음
    //public bool isRegen; //
    public int RegenHP; // 체젠 증강이 있는데 없애는것도 고려 ex 보스방 전에 밥먹고와서 풀피전
    //public int MaxRegenCoin; //부활 목숨 최대
    //private int curRegenCoin; // 부활 목숨 현재
    //private int kill; // 로아 가족 사진 처럼 게임 클리어시 처치한 몹수 고려

    /* @@@@@@ 알아둬야할 중요한 개념 현준이가 읽었으면 이제 지워도됨
     스턴이 500초 걸리는 스킬과 스턴이 1초 걸리는 스킬이 있을때 스턴 500초에 맞으면 500초후에 스턴이 풀리게 설정을 해둘거임
    그런데 이때 스턴 1초 짜리 스킬을 걸면 이 스킬또한 1초후에 스턴이 풀리게 만들거임 이게 겹치면? 499초 스턴이 씹히는거임 
    버프 , 디버프 때 이미 버프가 걸려있을때 남은 버프시간 과 새로 넣을 버프 지속 시간을 비교해서 처리를 해줘야함 
     */
    [HideInInspector] public bool CanSpeedBuff; 
    [HideInInspector] public bool CanLowSteam;
    [HideInInspector] public bool CanAtkBuff;
    /*
    public int CurRegenCoin
    {
        get { return curRegenCoin; }
        set
        {
            if (curRegenCoin != value)
            {
                curRegenCoin = value;
            }
            if (value == 0)
            {
                OnRegenCalculateEvent += RegenHPCalculator;
            }
        }
    } 
    */
    public int MaxSkillStack; //재능발현으로 스킬 스택을 늘렸을때 사용용
    public int CurSkillStack;
    public int MaxRollStack;
    public int CurRollStack;
    //public int evasionPersent; 회피 확률 증강
    //public float DamegeTemp; 데미지 저장용


    private float curHP;
    [HideInInspector]
    public float CurHP
    {
        get
        {
            return curHP;
        }
        set
        {
            if (curHP != value)
            {
                if (value > HP.total)
                {
                    curHP = HP.total;
                }
                else if (value < 0)
                {
                    curHP = 0;
                }
                else
                {
                    curHP = value;
                }
                //OnChangeCurHPEvent?.Invoke();
            }
        }
    }

    [SerializeField] private float curAmmo;
    //[HideInInspector]
    public float CurAmmo //현재 잔탄
    {
        get
        {
            return curAmmo;
        }
        set
        {
            if (value > AmmoMax.total)
            curAmmo = AmmoMax.total;
            curAmmo = value;
            //OnChangeAmmorEvent?.Invoke();
        }
    }
    [HideInInspector] public bool CanFire;                                //발사   가능한지
    [HideInInspector] public bool CanReload;                              //장전   가능한지
    [HideInInspector] public bool CanSkill;                               //스킬   가능한지
    [HideInInspector] public bool CanRoll;                                //구르기 가능한지
    public bool Invincibility;                          //무적 처리 피격시 구별

    public bool useSkill;
    public bool UseRoll;
    //public bool ImGhost;
    //public bool IsInShield; 중요@@ 실드개념 다 리메이크 가능성 높음
    //public float InShieldHP; 실드처리값 실드개념 정리 필요
    //int viewID; 포톤뷰 처리 지울예정
    //[HideInInspector] public bool IsChargeAttack; 차지어택 재능 처리
    //[HideInInspector] public bool CanReflect; 반사 재능 처리

    //public float ReflectCoeff; 반사 처리 값

    [HideInInspector] public string[] PlayerStatNameArray;
    [HideInInspector] public Stats[] PlayerStatArray;

    private void Awake()
    {
        //anime = GetComponent<PlayerAnimatorController>();
        ATK = new Stats(playerStats.atk);
        HP = new Stats(playerStats.hp);
        Speed = new Stats(playerStats.unitSpeed);
        AtkSpeed = new Stats(playerStats.atkSpeed);
        ReloadCoolTime = new Stats(playerStats.reloadCoolTime);
        SkillCoolTime = new Stats(playerStats.skillCoolTime);
        RollCoolTime = new Stats(playerStats.rollCoolTime);
        BulletSpread = new Stats(playerStats.bulletSpread);
        BulletLifeTime = new Stats(playerStats.bulletLifeTime);
        LaunchVolume = new Stats(playerStats.launchVolume);
        Critical = new Stats(playerStats.critical);
        AmmoMax = new Stats(playerStats.ammoMax);
        //MaxRegenCoin = 0;
        //CurRegenCoin = MaxRegenCoin;

        PlayerSprite = playerStats.playerSprite;
        WeaponSprite = playerStats.weaponSprite;
        BulletSprite = playerStats.BulletSprite;
        CurHP = HP.total;
        CurAmmo = AmmoMax.total;

        CanFire = true;
        CanReload = true;
        CanSkill = true;
        CanRoll = true;
        UseRoll = true;
        Invincibility = false;

        CanSpeedBuff = true;
        CanLowSteam = true;
        CanAtkBuff = true;

        isNoramlMove = true;
        isCanSkill = true;
        isCanAtk = true;
        //evasionPersent = 0;
        //isRegen = false;
        //ImGhost = false;

        //kill = 0;
        MaxSkillStack = 1;
        CurSkillStack = MaxSkillStack;
        MaxRollStack = 1;
        CurRollStack = MaxRollStack;

        PlayerSpriteCase = _PlayerSprite.GetComponent<SpriteLibrary>();
        WeaponSpriteCase = _WeaponSprite.GetComponent<SpriteLibrary>();

        PlayerSpriteCase.spriteLibraryAsset = PlayerSprite;
        WeaponSpriteCase.spriteLibraryAsset = WeaponSprite;
        //defense = 1;

        //IsChargeAttack = false;

        //_DebuffControl = GetComponent<PlayerDebuffControl>();
        //indicatorSprite = playerStats.indicatorSprite;
        atkClip = playerStats.atkClip;
        reloadStartClip = playerStats.reloadClip[0];
        reloadFinishClip = playerStats.reloadClip[1];

        PlayerStatArray = new Stats[11];
        PlayerStatNameArray = new string[11]
        {
            "체력",
            "공격력",
            "이동속도",
            "공격속도",
            "장전 쿨타임",
            "스킬 쿨타임",
            "대쉬 쿨타임",
            "탄퍼짐",
            "사거리",
            "크리티컬",
            "장탄수",
        };
    }

    private void stageBuffReset() //버프중 다음 스테이지로 넘어갈때 올라간 스탯 처리
    {
        if (!CanSpeedBuff)
        {
            Speed.added -= 3f;
            CanSpeedBuff = true;
        }
        if (!CanLowSteam)
        {
            CanSpeedBuff = true;
            AtkSpeed.added -= 0.5f;
            Speed.added -= 0.5f;
        }
        if (!CanAtkBuff)
        {
            ATK.coefficient -= 0.1f;
            CanAtkBuff = true;
        }
    }

    private void Start()
    {
        //체력 변화시 체력 동기화 처리인데 멀티 기준이라서 없어도 될거 같은데 일단 나둠
        //if (MainGameManager.Instance != null)
        //{
        //    MainGameManager.Instance.OnGameStartedEvent += RefillCoin;
        //    viewID = photonView.ViewID;
        //    OnChangeCurHPEvent += SendSyncHP;
        //}

        /*
        if (TestGameManager.Instance != null)
        {
           // viewID = photonView.ViewID;
            OnChangeCurHPEvent += SendSyncHP;
        }
        if (GameManager.Instance != null)
        {
            //viewID = photonView.ViewID;
            OnChangeCurHPEvent += SendSyncHP;
            StageStartSet();
        }
        */

    }
    public void StageStartSet()
    {
        /*
        //GameManager.Instance.OnStageStartEvent += RefillCoin;
        GameManager.Instance.OnStageStartEvent += startHp;  //스테이지 시작시 풀피처리 였던거 같은데 지우는거 고려 
        //GameManager.Instance.OnBossStageStartEvent += RefillCoin;
        GameManager.Instance.OnBossStageStartEvent += startHp;
        GameManager.Instance.OnStageStartEvent += PunRpcStageBuffReset;
        GameManager.Instance.OnBossStageStartEvent += PunRpcStageBuffReset;
        //viewID = photonView.ViewID; 포톤 멀티처리용 뷰아이디 체크
        */
    }
    public override string ToString() //그아 그 그 이게 스트링 ex 스토리 처리 할때 define? 처리 해서 하는게 좀 더 효율적인데 그거 처리용 같음
    {
        return curHP.ToString() + "/" + HP.total.ToString();
    }

    public void CharacterChange(PlayerSO playerData) //캐릭터 고르는거 처리 경우에 따라 안 쓸수도 있음
    {
        playerStats = playerData;
        Awake();
        Debug.Log("[PlayerStatHandler]" + this.ToString());
        Debug.Log("[PlayerStatHandler] " + "CharacterChange Done");
    }
    //public void GiveDamege(float damage) // pun함수용 직접공격 개념이였는데 안쓸 가능성 높음
    //{
    //    Damage(damage);
    //}

    /*
    public void DirectDamage(float damage, int targetID)
    {
        if (photonView.gameObject.layer == 12)
            return;

        if (photonView.gameObject.GetComponent<PlayerStatHandler>().Invincibility)
            return;

        if (IsInShield)
        {
            damage -= InShieldHP;
        }
        if (CanReflect)
        {
            CallReflectEvent(damage, targetID);
            damage *= (1 - ReflectCoeff);
        }
        Damage(damage);
    }
    */

    public void Damage(float damage)
    {
        CurHP -= damage;
        //GameManager.Instance?.PlayerDie(); 

        /*if (!isDie) // 죽어있음 = 멀티 개념 인데 한번 읽어봐서 아 재능을 섞으면 이런 느낌이구나 한번 파악
        {
            DamegeTemp = damage; 데미지 값 저장
            GetDamege?.Invoke(DamegeTemp);  //받은 데미지 저장 
            int a = UnityEngine.Random.Range(0, 100); // 회피값 
            if (evasionPersent <= a) // 회피 실패시 데미지 처리
            {
                DamegeTemp = DamegeTemp * defense;

                HitEvent?.Invoke();
                HitEvent2?.Invoke(DamegeTemp);//이게 값이 필요한경우와 필요 없는경우가 있는데 한개로 할수가 있는지 모르겠음 일단 이렇게함
                                              // 아직도 가지고 있는 의문임

                if (CurHP - DamegeTemp <= 0) // 데미지 받았을때 죽을걸로 예상될때 처리
                {
                    CurHP -= DamegeTemp;
                    isDie = true;
                    OnDieEvent?.Invoke();

                    if (CurRegenCoin > 0) // 목숨 부활 처리
                    {
                        CurRegenCoin -= 1;
                        isDie = false;
                        Debug.Log($"부활 : {CurRegenCoin}");
                        Regen(HP.total);
                        return;
                    }

                    TestGameManager.Instance?.DiedAfter();
                    GameManager.Instance?.PlayerDie();
                    photonView.RPC("LayerSet", RpcTarget.All);
                }
                else
                {
                    CurHP -= DamegeTemp;
                }
            }
        }
        */

    }
    public void LayerSet() //12레이어가 기존에선 죽은 상태였음 스테이지 재시작시 부활 처리하면서 레이어도 바꿔줌 
    {
        this.gameObject.layer = 12;
    }

    public void HPadd(float addhp) // 힐 
    {
        CurHP += addhp;
    }

    /*public void Regen(float HP) // 체젠 사용할지 모르겠음
    {
        HPadd(HP);
        OnRegenEvent?.Invoke();
        OnRegenCalculateEvent?.Invoke(RegenHP);
        if (gameObject.GetPhotonView().IsMine)
        {
            PlayerInputController tempInputControl = this.gameObject.GetComponent<PlayerInputController>();
            tempInputControl.ResetSetting();
        }
        isDie = false;
        isRegen = true;
        _DebuffControl.Init(PlayerDebuffControl.buffName.TwoMoon, 5f);
        photonView.RPC("SendRegenBool", RpcTarget.All, viewID);
    }*/

    /*
    public void SendRegenBool(int viewID) //체젠 펀 통신 처리 삭제 예정
    {
        PhotonView pv = PhotonView.Find(viewID);
        pv.GetComponent<PlayerStatHandler>().isRegen = true;

        Invoke("InvokeSetRegenBool", 5f);
    }

    private void InvokeSetRegenBool()//불값 통신 처리 삭제 예정
    {
        SetRegenBool(viewID);
    }
    */

    /*
    public void SetRegenBool(int viewID) 삭제예정
    {
        PhotonView pv = PhotonView.Find(viewID);
        if (pv.IsMine)
        {
            isRegen = false;
        }
        else
        {
            pv.GetComponent<PlayerStatHandler>().isRegen = false;
        }
        // 부활 파티클이 꺼져야 하는 시점
    }
    */

    //public void RefillCoin() 목숨 +1 같은거 있으면 쓸거 같긴함
    //{
    //    CurRegenCoin = MaxRegenCoin;
    //}
    public void PunRpcStartHp() // 시작시 체력값 설정 함수 근데 멀티용으로 짜서 다시 짜야함
    {
        /*if (gameObject.GetPhotonView().IsMine)
        {
            PlayerInputController tempInputControl = this.gameObject.GetComponent<PlayerInputController>();
            tempInputControl.ResetSetting();
            tempInputControl.InputOn();
        }
        CurHP = HP.total;
        this.gameObject.layer = 8;
        if (ImGhost)
        {
           this.gameObject.layer = 13;
        }
        if (isDie == true)
        {
            isDie = false;
            anime._animation.SetTrigger("IsRegen");
        }*/
    }


    /*public void SetSyncHP(int viewID, float _CurHP) // 현재 체력 통신 삭제 예정
    {
        PhotonView _PV;
        _PV = PhotonView.Find(viewID);
        PlayerStatHandler _PvPlayer = _PV.gameObject.GetComponent<PlayerStatHandler>();
        _PvPlayer.CurHP = _CurHP;

        if (_PvPlayer.CurHP <= 0)
        {
            isDie = true;
            OnDieEvent?.Invoke();
            this.gameObject.layer = 12;
        }
    }*/

    public void MoveStartCall() //움직임감지이벤트
    {
        //MoveStartEvent?.Invoke();
    }
    public void MoveEndCall() // 움직임 감지이벤트
    {
        //MoveEndEvent?.Invoke();
    }

    public void EnemyHitCall() // 적 타격 이벤트
    {
        //EnemyHitEvent?.Invoke(); 
    }
    public void KillEvent() // 킬 이벤트
    {
        //kill++;
        //KillCatchEvent?.Invoke();
    }
    public void RegenHPCalculator(int calHP = 0)
    {
        if (calHP == 0)
        {
            return;
        }
        else
        {
            //Debug.Log($"A206 발악 실행 : {calHP}");
            HPadd((calHP - HP.total));
        }
    }

    public void CallReflectEvent(float damage, int targetID) // 반사 체크인데 난 반사에 회의적임
    {
        //if (CanReflect)
        //{
        //    OnDamageReflectEvent?.Invoke(damage, targetID);
        //}
    }

    /*public void thankyouLife(int pvid) //팀 살려주기 멀티용
    {
        PhotonView photonView = PhotonView.Find(pvid);
        WeaponSystem weapon = photonView.gameObject.GetComponent<WeaponSystem>();
        weapon.canresurrection = false;
    }*/

    public void StartKnockback(Vector3 direction, float distance)
    {
        StartCoroutine(Knockback(direction, distance));
    }

    //보스 패턴용 넉백 추가함 - 우민규
    public IEnumerator Knockback(Vector3 direction, float distance)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + direction * distance;

        float elapsedTime = 0f;

        while (elapsedTime < 0.1f)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / 0.1f);
            elapsedTime += Time.deltaTime;
            yield return null; // 1프레임 대기
        }

        // 최종 위치에 고정
        transform.position = targetPosition;
    }
    public void ImLive() // 부활 생존 알림 싱글 삭제 가능성 높음
    {
        //Regen(HP.total);
        //this.gameObject.layer = 8;
    }

    public void SetStatusArray() // 스탯 배열화
    {
        PlayerStatArray = new Stats[11]
        {
            HP,
            ATK,
            Speed,
            AtkSpeed,
            ReloadCoolTime,
            SkillCoolTime,
            RollCoolTime,
            BulletSpread,
            BulletLifeTime,
            Critical,
            AmmoMax
        };

    }
}
