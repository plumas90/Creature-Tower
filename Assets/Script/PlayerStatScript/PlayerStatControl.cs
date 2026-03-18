using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class PlayerStatControl : MonoBehaviour
{
    // ���� ���� �Ŵ����� ���� ������ �Ұ� ���Ƽ� ���� �Ŵ��� ������ ����ص� �ּ� ó�� �ص�


    // ���� ���⼺�� ���� �Ⱦ��� �ִ� �̺�Ʈ ���� ##�� �ٿ���
    public event Action<float> GetDamege; //�ǰݽ� ó�� ���� ������ ó��  = ���� ������
    public event Action<float> HitEvent2; //�ǰ� ������ ��ġ ���� ó�� ��ɹ������� ���� ������ ��ġ ó������  = ���� ������
    public event Action HitEvent; // �ǰ� �߻� ���� ó�� �̺�Ʈ
    public event Action OnDieEvent; // �׾����� ó�� �׷��� �̱۰� ó���� �Ⱦ����� ���� ##
    public event Action OnRegenEvent; // ��Ȱ �̺�Ʈ �̱۰��̶� ���� ���� ##
    public event Action<int> OnRegenCalculateEvent; //�׾����� �����ε� �Ⱦ��� ##
    public event Action OnChangeAmmorEvent; // 
    public event Action OnChangeCurHPEvent; // ���� ü�� ó�� ex ����Ŀ  
    public event Action MoveStartEvent; //  ������ �ִ� �ð� ó��1 ex ������ �ִ� ���� ���ݷ»�� �����̸� ��� 
    public event Action MoveEndEvent; // ������ �ִ� �ð� ó��2
    public event Action EnemyHitEvent; // Ÿ�� nȸ�� �߰� Ÿ�� �� ó�� �Ⱦ��� ���� ##
    public event Action KillCatchEvent; // ų �� ���ݷ� ��� �̺�Ʈ � ��� �ʹ� �������� ����� ��� ���� ���ɼ� ���� x ##
    public event Action<float, int> OnDamageReflectEvent;// ������ �ݻ� ó�� �̺�Ʈ ������ �������� ����� ��մ� Ʈ�� ������ ���� ##


    [SerializeField] private PlayerSO playerStats;
    public int CharacterClass => playerStats != null ? playerStats.CharacterClass : 0; // 캐릭터 클래스 외부 접근용

    PlayerAnimatorController anime;

    [Header("����")]
    public Stats ATK;                 // ���ݷ�
    public Stats HP;                  // ü��
    public Stats Speed;               // �̵� �ӵ�
    [HideInInspector] public Stats AtkSpeed;            // ���� �ӵ�
    [HideInInspector] public Stats ReloadCoolTime;      // ����   �� Ÿ��
    [HideInInspector] public Stats SkillCoolTime;       // ��ų   �� Ÿ��
    [HideInInspector] public Stats RollCoolTime;        // ������ �� Ÿ��
    [HideInInspector] public Stats BulletSpread;        // ź����
    [HideInInspector] public Stats BulletLifeTime;      // �Ѿ� ��Ÿ�
    public Stats LaunchVolume;        // �ѹ��� �߻��� �߻緮
    [HideInInspector] public Stats Critical;            // ũ��Ƽ�� = ũ��Ƽ�� ���尡 ������� ������ ������ ���̵��� �� ū ������ ��
    [HideInInspector] public Stats AmmoMax;             // ��ź��
    [HideInInspector] public float defense; //���� �߿��Ѱ� ������ ���� �̶�°��� �⺻�� 1�� ������ 10 ������ ������ x ���� ���� = ���� ������ ���� ����

    //public Sprite indicatorSprite; ��
    [Header("����")]
    public AudioClip atkClip;
    public AudioClip reloadStartClip;
    public AudioClip reloadFinishClip;

    [Header("��������Ʈ")]
    [HideInInspector] public SpriteLibraryAsset PlayerSprite; // ��������Ʈ
    [HideInInspector] public SpriteLibraryAsset WeaponSprite; // ��������Ʈ
    [HideInInspector] public Sprite BulletSprite; // ��������Ʈ
    [HideInInspector] public SpriteLibrary PlayerSpriteCase; // ��������Ʈ
    [HideInInspector] public SpriteLibrary WeaponSpriteCase; // ��������Ʈ
    public GameObject _PlayerSprite;
    public GameObject _WeaponSprite;
    public PlayerDebuffControl _DebuffControl; //����� �Ŵ��� ��������

    [HideInInspector] public bool isNoramlMove; //���� �¿� ���� üũ
    [HideInInspector] public bool isCanSkill; // ��ų ��Ÿ�� üũ
    [HideInInspector] public bool isCanAtk;
    public bool isDie; //���� üũ
    //public bool isRegen; //
    public int RegenHP; // ��Ȱ�� ü���ε� 

    #region ������
    //�Ʒ� ������͵� ����� ���ӸŴ����� ����� ��Ƽ�� �ƴϴϱ� ų�� ���� ����
    //public int MaxRegenCoin; //��Ȱ ��� �ִ�
    //private int curRegenCoin; // ��Ȱ ��� ����
    //private int kill; // �ξ� ���� ���� ó�� ���� Ŭ����� óġ�� ���� ����
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
    #endregion

    /* @@@@@@ �˾Ƶ־��� �߿��� ���� �����̰� �о����� ���� ��������
     ������ 500�� �ɸ��� ��ų�� ������ 1�� �ɸ��� ��ų�� ������ ���� 500�ʿ� ������ 500���Ŀ� ������ Ǯ���� ������ �صѰ���
    �׷��� �̶� ���� 1�� ¥�� ��ų�� �ɸ� �� ��ų���� 1���Ŀ� ������ Ǯ���� ������� �̰� ��ġ��? 499�� ������ �����°��� 
    ���� , ����� �� �̹� ������ �ɷ������� ���� �����ð� �� ���� ���� ���� ���� �ð��� ���ؼ� ó���� ������� 
     */
    [HideInInspector] public bool CanSpeedBuff; 
    [HideInInspector] public bool CanLowSteam;
    [HideInInspector] public bool CanAtkBuff;
    [HideInInspector] public int MaxSkillStack; //��ɹ������� ��ų ������ �÷����� ����
    [HideInInspector] public int CurSkillStack;
    [HideInInspector] public int MaxRollStack;
    [HideInInspector] public int CurRollStack;
    public int evasionPersent; //ȸ�� Ȯ�� ����
    public float DamegeTemp;// ������ �����


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
                OnChangeCurHPEvent?.Invoke();
            }
        }
    }

    [SerializeField] private float curAmmo;
    //[HideInInspector]
    public float CurAmmo //���� ��ź
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
            OnChangeAmmorEvent?.Invoke();
        }
    }
    [HideInInspector] public bool CanFire;                                //�߻�   ��������
    [HideInInspector] public bool CanReload;                              //����   ��������
    [HideInInspector] public bool CanSkill;                               //��ų   ��������
    [HideInInspector] public bool CanRoll;                                //������ ��������
    private int externalFireBlockCount;
    public bool IsExternalFireBlocked => externalFireBlockCount > 0;
    public bool Invincibility;                          //���� ó�� �ǰݽ� ����
    public bool SkillRollInvincibility;

    public bool useSkill;
    [HideInInspector] public int ActiveSkillCastCount;
    public bool UseRoll;
    //public bool ImGhost;


    [Header("�ǵ�")]
    public bool IsInShield; //�߿�@@ �ǵ尳�� �� ������ũ ���ɼ� ����
    public float InShieldHP; //�ǵ�ó���� �ǵ尳�� ���� �ʿ�
    //int viewID; ����� ó�� ���￹��
    //[HideInInspector] public bool IsChargeAttack; �������� ��� ó��
    //[HideInInspector] public bool CanReflect; �ݻ� ��� ó��

    //public float ReflectCoeff; �ݻ� ó�� ��

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

        PlayerSprite = playerStats.playerSprite;
        WeaponSprite = playerStats.weaponSprite;
        BulletSprite = playerStats.BulletSprite;
        CurHP = HP.total;
        CurAmmo = AmmoMax.total;

        CanFire = true;
        CanReload = true;
        CanSkill = true;
        CanRoll = true;
        externalFireBlockCount = 0;
        ActiveSkillCastCount = 0;
        UseRoll = true;
        Invincibility = false;
        SkillRollInvincibility = false;

        CanSpeedBuff = true;
        CanLowSteam = true;
        CanAtkBuff = true;

        isNoramlMove = true;
        isCanSkill = true;
        isCanAtk = true;

        isDie = false;

        
        evasionPersent = 0;
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

        //IsChargeAttack = false;

        //_DebuffControl = GetComponent<PlayerDebuffControl>();
        //indicatorSprite = playerStats.indicatorSprite;
        atkClip = playerStats.atkClip;
        //reloadStartClip = playerStats.reloadClip[0];
        //reloadFinishClip = playerStats.reloadClip[1];

        //�Ʒ��� ���ֵ� �ɰ� ������ 
        PlayerStatArray = new Stats[11];
        PlayerStatNameArray = new string[11]
        {
            "ü��",
            "���ݷ�",
            "�̵��ӵ�",
            "���ݼӵ�",
            "���� ��Ÿ��",
            "��ų ��Ÿ��",
            "�뽬 ��Ÿ��",
            "ź����",
            "��Ÿ�",
            "ũ��Ƽ��",
            "��ź��",
        };
    }

    public void PushExternalFireBlock()
    {
        externalFireBlockCount++;
    }

    public void PopExternalFireBlock()
    {
        externalFireBlockCount = Mathf.Max(0, externalFireBlockCount - 1);
    }

    private void stageBuffReset() //������ ���� ���������� �Ѿ�� �ö� ���� ó��
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
        //ü�� ��ȭ�� ü�� ����ȭ ó���ε� ��Ƽ �����̶� ��� �ɰ� ������ �ϴ� ����
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
        GameManager.Instance.OnStageStartEvent += startHp;  //�������� ���۽� Ǯ��ó�� ������ ������ ����°� ���� 
        //GameManager.Instance.OnBossStageStartEvent += RefillCoin;
        GameManager.Instance.OnBossStageStartEvent += startHp;
        GameManager.Instance.OnStageStartEvent += PunRpcStageBuffReset;
        GameManager.Instance.OnBossStageStartEvent += PunRpcStageBuffReset;
        //viewID = photonView.ViewID; ���� ��Ƽó���� ����̵� üũ
        */
    }
    public override string ToString() //�׾� �� �� �̰� ��Ʈ�� ex ���丮 ó�� �Ҷ� define? ó�� �ؼ� �ϴ°� �� �� ȿ�����ε� �װ� ó���� ����
    {
        return curHP.ToString() + "/" + HP.total.ToString();
    }

    public void CharacterChange(PlayerSO playerData) //ĳ���� �����°� ó�� ��쿡 ���� �� ������ ����
    {
        playerStats = playerData;
        Awake();
        Debug.Log("[PlayerStatHandler]" + this.ToString());
        Debug.Log("[PlayerStatHandler] " + "CharacterChange Done");
    }
    //public void GiveDamege(float damage) // pun�Լ��� �������� �����̿��µ� �Ⱦ� ���ɼ� ����
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
        //CurHP -= damage;

        DamegeTemp = damage; //������ �� ����
        GetDamege?.Invoke(DamegeTemp);  //���� ������ ���� ������ ȿ���� ���� �������� �޶��� �� ����
        int a = UnityEngine.Random.Range(0, 100); // ȸ�ǰ� 
        if (evasionPersent < a) // ȸ�� ���н� ������ ó��
        {
            //TODO ���⼭ IF�� ���� �ǵ� ó��
            if (IsInShield) 
            {
                if (InShieldHP > 0) 
                {
                    InShieldHP -= DamegeTemp;
                    if (InShieldHP < 0) 
                    {
                        return;
                    }
                }

            }

            DamegeTemp = DamegeTemp * defense;

            HitEvent?.Invoke();
            HitEvent2?.Invoke(DamegeTemp);//�̰� ���� �ʿ��Ѱ��� �ʿ� ���°�찡 �ִµ� �Ѱ��� �Ҽ��� �ִ��� �𸣰��� �ϴ� �̷�����
                                          // ������ ������ �ִ� �ǹ���

            if (CurHP - DamegeTemp <= 0) // ������ �޾����� �����ɷ� ����ɶ� ó��
            {
                CurHP -= DamegeTemp;
                isDie = true;

                //TODO ���� �׾����� �̺�Ʈ �ɸ��� ���� ��� ��ȭó�� �� N���� ���� ���� �߰�
                OnDieEvent?.Invoke();
                

                if (GameManager.Instance.Life > 0) // ��� ��Ȱ ó��
                {
                    // TODO �Ʒ��� �Լ��� ����� �׾����� N���� ��Ȱ ���·� ó�� 
                    GameManager.Instance.Life -= 1;
                    isDie = false;
                    Regen(HP.total);

                //�׾����� ���ӸŴ��� ������ ������ �ȳ����� ���¿� Ư����ġ�� ��Ȱ ó���� �ؾ��Ѵٰ� ������
                // ���ӸŴ��� ��ܿ� �� ���� �� ������ �ʿ� �ϴٰ� �ߴµ� ��Ȱ ��ġ�� �׷��� �ؾ� �Ұ� ����
                //GameManager.Instance?.PlayerDie();
                    return;
                }

            }
            else
            {
                CurHP -= DamegeTemp;
            }
        }
        else 
        {
            //ȸ�ǽ� Ư�� ȿ�� ó�� �Ҹ��� �ð��� ȿ�� �߰� �ؾ��ҵ�
        }

    }

    public void HPadd(float addhp) // �� 
    {
        CurHP += addhp;
    }

    public void Regen(float HP) // ��Ȱ�ε�
    {
        HPadd(HP);
        OnRegenEvent?.Invoke(); //��Ȱ �̺�Ʈ EX ��Ȱ�� ���ݷ� ����
        OnRegenCalculateEvent?.Invoke(RegenHP);// ��Ȱ�� Ǯ ü���� �ƴѴ�� ��������� ������


            //�׾����� ���� �� �������� ���� �ؾ��� �׷��� ��Ȱ �������� ��Ȱ ���� ���ɉ�ٰ� ��Ȱ �ϴ°͵� ������ �̰͵� ������ ����
            PlayerInputController tempInputControl = this.gameObject.GetComponent<PlayerInputController>();
            tempInputControl.ResetSetting();

        isDie = false;
        _DebuffControl.Init(PlayerDebuffControl.buffName.TwoMoon, 5f); //���� ����5�ʰ����� ������ �ٽ� ���ƾ���ߵ�
    }
    public void MoveStartCall() //�����Ӱ����̺�Ʈ
    {
        MoveStartEvent?.Invoke();
    }
    public void MoveEndCall() // ������ �����̺�Ʈ
    {
        MoveEndEvent?.Invoke();
    }

    public void EnemyHitCall() // �� Ÿ�� �̺�Ʈ
    {
        EnemyHitEvent?.Invoke(); 
    }
    public void KillEvent() // ų �̺�Ʈ
    {
        //kill++;
        KillCatchEvent?.Invoke();
    }
    public void RegenHPCalculator(int calHP = 0)
    {
        if (calHP == 0)
        {
            return;
        }
        else
        {
            //Debug.Log($"A206 �߾� ���� : {calHP}");
            HPadd((calHP - HP.total));
        }
    }

    public void CallReflectEvent(float damage, int targetID) // �ݻ� üũ�ε� �� �ݻ翡 ȸ������
    {
        //if (CanReflect)
        //{
        //    OnDamageReflectEvent?.Invoke(damage, targetID);
        //}
    }

    public void StartKnockback(Vector3 direction, float distance)
    {
        StartCoroutine(Knockback(direction, distance));
    }

    //���� ���Ͽ� �˹� �߰��� - ��α�
    public IEnumerator Knockback(Vector3 direction, float distance)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + direction * distance;

        float elapsedTime = 0f;

        while (elapsedTime < 0.1f)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / 0.1f);
            elapsedTime += Time.deltaTime;
            yield return null; // 1������ ���
        }

        // ���� ��ġ�� ����
        transform.position = targetPosition;
    }
    public void SetStatusArray() // ���� �迭ȭ
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
