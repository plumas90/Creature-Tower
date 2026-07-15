using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using static UnityEngine.UI.CanvasScaler;

public class AugmentManager : MonoBehaviour //실질적으로 증강을 불러오는곳 AugmentManager.Instance.Invoke(code,0); 을통해 해당 증강불러옴
{   // 주의 이부분은 포톤 멀티 동기화를 위해 포톤 중심으로 작성되었기 때문에 수정시 주석처리를 통해 원본을 남겨두기를 바람
    public static AugmentManager Instance;//싱글톤
    //아래 4개는 플레이어 시스템 각 부위 해당 부위에 새 기는을 붙이는 식으로 재능은 작동
    public PlayerStatControl playerstatHandler;//정확히는 이름을 타겟 플레이어 스탯 핸들러가 맞는 표현 같기도함 // 생각할수록 맞음
    public GameObject player;//처음 세팅값에 필요함 == 이젠 무조건 플레이어가 이거 하나기 때문에 진짜 플레이어임
    public PlayerInput playerInput;//이것도 사실 타켓플레이어 인풋 잘안쓰기에 함수가 따로 만들지 않음
    public WeaponSystem playerWeapon;//

    [Header("스탯 1당 상승치 데이터 SO")]
    [SerializeField] private StatIncrementSO statIncrementSO;

    //아래 힘/체/스피드/공속/탄퍼짐/쿨타임/크리/총알을 so로 만들어서 대체할것
    private float atk = 5f;
    private float hp = 5f;
    private float speed = 0.05f;
    private float atkspeed = 0.05f;
    private float bulletSpread = -1f;
    private float cooltime = -0.25f;
    private float critical = 5f;
    private float AmmoMax = 2f;


    // 현재 "소환형"  타입은 소환 개념을 수정해야 되는데 우선 전체적인 수정이 우선임 소환형으로 주석을 달아놧으니 컨트롤f로 언젠가 수정할것
    // 목숨을 어떻게 할것인가? 목숨형 주석 추가

    //public GameObject targetPlayer;//실제 적용되는 타켓 플레이어 99% 경우 이걸 사용함 진짜 진짜 중요함
    //public PhotonView PlayerPv;//현재플레이어의 포톤뷰값== 증강매니저의 포톤뷰가 아님 (중요)
    //public int PlayerPvNumber;//현재플레이어의 포톤뷰 넘버

    private void Awake()//싱글톤
    {
        //Debug.Log("AugmentManager - Awake");
        if (null == Instance)
        {
            Instance = this;

            //DontDestroyOnLoad(this.gameObject);
            InitializeStatIncrements();
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void InitializeStatIncrements()
    {
        if (statIncrementSO != null)
        {
            atk = statIncrementSO.atk;
            hp = statIncrementSO.hp;
            speed = statIncrementSO.speed;
            atkspeed = statIncrementSO.atkSpeed;
            bulletSpread = statIncrementSO.bulletSpread;
            cooltime = statIncrementSO.skillCoolTime;
            critical = statIncrementSO.critical;
            AmmoMax = statIncrementSO.ammoMax;
        }
    }
    public void startset(GameObject PlayerObj)//스타트세팅 메인게임매니저 게임 처음 시작부분에 호출되면 값셋팅 해줌
    {
        player = PlayerObj;//플레이어 받아옴 
        playerstatHandler= player.GetComponent<PlayerStatControl>();
        playerInput = player.GetComponent<PlayerInput>();
        playerWeapon = player.GetComponent<WeaponSystem>();
        //PlayerPvNumber = player.GetPhotonView().ViewID;//
        //PlayerPv = PhotonView.Find(PlayerPvNumber);//플레이어pv 확보
    }
    public void AugmentCall(int code)//slot에서 pick으로 호출해서 punppc로 모든컴퓨터에 뿌려줌
    {
        // 직업 전용 증강(코드 1000 이상)은 캐릭터 타입이 일치해야만 적용
        if (code >= 1000 && playerstatHandler != null)
        {
            // 코드 첫 자리로 필요 직업 판별: 1xxx=TV, 2xxx=Charlie, 3xxx=KimKilWhan
            int requiredClass = (code / 1000) - 1;
            int currentClass  = playerstatHandler.CharacterClass;
            if (requiredClass != currentClass)
            {
                string[] classNames = { "TV", "Charlie", "KimKilWhan" };
                string req = (requiredClass >= 0 && requiredClass < classNames.Length) ? classNames[requiredClass] : requiredClass.ToString();
                string cur = (currentClass  >= 0 && currentClass  < classNames.Length) ? classNames[currentClass]  : currentClass.ToString();
                Debug.LogWarning($"[AugmentManager] 직업 불일치로 증강 차단 | 증강코드={code} (필요:{req}) / 현재 캐릭터:{cur}");
                return;
            }
        }

        string callName = "A" + code.ToString();
        var method = GetType().GetMethod(
            callName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
        );
        if (method == null)
        {
            Debug.LogError($"[AugmentManager] 증강 핸들러를 찾지 못함 | code={code}, method={callName}");
            return;
        }
        //photonView.RPC(callName, RpcTarget.All, PlayerPvNumber);
        // 속도를 원한다면 if(itemCode=="Item001") AItem001(); 이런식으로 모든 애들을 처리하는게 더 빠른데 코드상 너무 비효율적 센드메시지로 보내는게
        // 낫다고 판단됨 그리고 이방법을 한다면 딕셔너리로 액션을 만들어서 하는 방법도 가능 센드메시지를 채용 @@ 단 센드메시지로 성능 떨어짐이생각되면 다 이프 스위치문으로 만들것@@
        //Dictionary<string, Action> methods = new Dictionary<string, Action>();
        SendMessage(callName, SendMessageOptions.DontRequireReceiver);

    }
    // void ChangePlayerAndPlayerStatHandler(int PlayerNumber)
    //{
    //    playerstatHandler = targetPlayer.GetComponent<PlayerStatControl>();
    //}//플레이어스탯핸들러, 타겟플레이어 모두 변하는경우
    // private void ChangePlayerStatHandler(int PlayerNumber)// 플레이어스탯핸들러만변하는경우   //싱글겜으로 변하면서 스탯핸들러 돌리기 필요x
    //{
    //    PhotonView photonView = PhotonView.Find(PlayerNumber);
    //    playerstatHandler = photonView.gameObject.GetComponent<PlayerStatHandler>();
    //}
    //private void ChangeOnlyPlayer(int PlayerNumber) //타겟 플레이어만 변하는경우//싱글겜으로 변하면서 스탯핸들러 돌리기 필요x
    //{
    //    PhotonView photonView = PhotonView.Find(PlayerNumber);
    //    targetPlayer = photonView.gameObject;
    //}
    //private void FindMaster(int num)//싱글겜으로 변하면서 스탯핸들러 돌리기 필요x
    //{
    //    PhotonView a = PhotonView.Find(num);
    //    a.transform.SetParent(targetPlayer.transform);
    //    a.transform.localPosition = Vector3.zero;
    //}

    #region stat
    private void A901()//스탯 공 티어 1
    {
        //ChangePlayerStatHandler(PlayerNumber);
        playerstatHandler.ATK.added += atk;
    }
    private void A902()//스탯 체 티어 1
    {
        //ChangePlayerAndPlayerStatHandler(PlayerNumber);
        playerstatHandler.HP.added += hp;
        playerstatHandler.HPadd(hp);
    }
    private void A903()//스탯 이속 티어 1
    {
        playerstatHandler.Speed.added += speed;
    }

    private void A904()//스탯 공속 티어 1
    {
        playerstatHandler.AtkSpeed.added += atkspeed;
    }

    private void A905()//스탯 정밀도 티어 1 탄퍼짐이 이상해서 정밀도로 바꿨는데 괜찮겠지? 어차피바꿔도됨
    {
        playerstatHandler.BulletSpread.added += bulletSpread;
    }

    private void A906()//스탯 스킬쿨타임 티어1
    {
        playerstatHandler.SkillCoolTime.added += cooltime;
    }

    private void A907()//스탯 치명타 티어1
    {
        playerstatHandler.Critical.added += critical;
    }
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@스탯티어2

    private void A911()//스탯 공 티어 2
    {
        playerstatHandler.ATK.added += atk * 2;
    }

    private void A912()//스탯 체 티어 2
    {
        float hpUp = hp * 2;
        playerstatHandler.HP.added += hpUp;
        playerstatHandler.HPadd(hpUp);
    }

    private void A913()//스탯 이속 티어 2
    {
        playerstatHandler.Speed.added += speed * 2;
    }

    private void A914()//스탯 공속 티어 2
    {
        playerstatHandler.AtkSpeed.added += atkspeed * 2;
    }

    private void A915()//스탯 정밀도 티어 1 탄퍼짐이 이상해서 정밀도로 바꿨는데 괜찮겠지? 어차피바꿔도됨
    {
        playerstatHandler.BulletSpread.added += bulletSpread * 2;
    }

    private void A916()//스탯 스킬쿨타임 티어2
    {
        playerstatHandler.SkillCoolTime.added += cooltime * 2;
    }

    private void A917()//스탯 치명타 티어2
    {
        playerstatHandler.Critical.added += critical * 2;
    }

    private void A918()//스탯 장탄수 티어2
    {
        playerstatHandler.AmmoMax.added += AmmoMax;
    }
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@스탯3티어

    private void A921()//스탯 공 티어 3
    {
        playerstatHandler.ATK.added += atk * 3;
    }

    private void A922()//스탯 체 티어 3
    {
        float hpUp = hp * 3;
        playerstatHandler.HP.added += hpUp;
        playerstatHandler.HPadd(hpUp);
    }

    private void A923()//스탯 이속 티어 3
    {
        playerstatHandler.Speed.added += speed * 3;
    }

    private void A924()//스탯 공속 티어 3
    {
        playerstatHandler.AtkSpeed.added += atkspeed * 3;
    }

    private void A925()//스탯 정밀도 티어 3
    {
        playerstatHandler.BulletSpread.added += bulletSpread * 3;
    }

    private void A926()//스탯 스킬쿨타임 티어3
    {
        playerstatHandler.SkillCoolTime.added += cooltime * 3;
    }

    private void A927()//스탯 치명타 티어3
    {

        playerstatHandler.Critical.added += critical * 3;
    }

    private void A928()//스탯 장탄수 티어3
    {
        playerstatHandler.AmmoMax.added += AmmoMax + 1;
    }

    private void A999()//공 체 이속 공속 정밀도  스킬 쿨타임 치명타 장탄
    {

        playerstatHandler.ATK.added += atk;
        playerstatHandler.HP.added += hp;
        playerstatHandler.Speed.added += speed;
        playerstatHandler.AtkSpeed.added += atkspeed;
        playerstatHandler.BulletSpread.added += bulletSpread;
        playerstatHandler.SkillCoolTime.added += cooltime;
        playerstatHandler.Critical.added += critical;
        playerstatHandler.AmmoMax.added +=AmmoMax;
    }
    #endregion
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@공용1티어
    #region ALL1
    private void A101()//아이언스킨 @@ 방어력 건드는건 찾기 쉽게 방어력 이라고 주석을 달아둘것
    {
        playerstatHandler.defense *= 0.9f;
    }
    private void A102()//인파이터 사거리 계수 -0.3 공 계수 +0.3
    {
        playerstatHandler.BulletLifeTime.coefficient *= 0.7f;
        playerstatHandler.ATK.coefficient *= 1.3f;
    }
    private void A103()//난사//탄퍼짐이 높을수록 장전시간 감소 //스테이지 시작시 탄퍼짐*0.2f 쿨감
    {
        //ChangeOnlyPlayer(PlayerNumber);
        player.AddComponent<A0103>();
    }
    private void A104()//약자멸시 현재 스테이지가 낮을수록 공격력 증가
    {
        player.AddComponent<A0104>();
    }
    private void A105()// 유리대포 //현재 최대 체력을 1로 만들고 그 값 의 절반 만큼 공업// 먹은후 앞으로 체력을 먹으면 체력이 늘어남 계속 체력1로할지고민
    {
        float up = ((int)playerstatHandler.HP.total - 1);
        playerstatHandler.HP.added -= up;
        playerstatHandler.CurHP = 1;
        playerstatHandler.ATK.added += up * 0.5f;
    }
    private void A106()//처치시 영구적 공증
    {
        player.AddComponent<A1106>();
    }
    private void A107()//알맞은 타이밍 //가만히 있는 시간에 비례하여 공업
    {
        player.AddComponent<A0107>();
    }
    private void A108()//타격시 일시적 이속 증가 A0108이 ..타격시 인줄 알고 스크립트만들고보니아니라서 손안댐
    {
        player.AddComponent<A0108>();
    }
    private void A109()// 소형화
    {
        float x = (player.transform.localScale.x * 0.75f);
        float y = (player.transform.localScale.y * 0.75f);
        player.transform.localScale = new Vector2(x, y);
        playerstatHandler.HP.coefficient *= 0.8f;
        playerstatHandler.Speed.coefficient *= 1.2f;
    }
    private void A110()//대형화 
    {
        float x = (player.transform.localScale.x * 1.25f);
        float y = (player.transform.localScale.y * 1.25f);
        player.transform.localScale = new Vector2(x, y);
        playerstatHandler.HP.coefficient *= 1.5f;
        playerstatHandler.Speed.coefficient *= 0.8f;
    }
    private void A111()//침착한 일격
    {
        player.AddComponent<A0111>();
    }
    private void A112()//빠른장전
    {
        playerstatHandler.ReloadCoolTime.coefficient *= 0.7f;
    }
    private void A113()// 머니=파워 현재 너는 돈개념이 없음 반드시 알아둬야함 @@@@@@@@
    {
        player.AddComponent<A0113>();
    }
    private void A114()//불
    {
        playerWeapon.fire = true;
    }
    private void A115()//물
    {
        playerWeapon.water = true;
    }
    private void A116()//사이즈샷 몸크기 비례 총알크기업
    {
        playerWeapon.sizeBody = true;
    }
    private void A117()//777 공격 확률 조정 추후 공격 성공 확률 비슷한 개념으로 도입될가능성이 있음
    {
        //현재 공격 확률 스탯은 막혀있는 상태 수정 필요
        PlayerInputController inputControl = player.GetComponent<PlayerInputController>();
        //inputControl.atkPercent -= 10;
        //playerstatHandler.ATK.coefficient *= 1.3f;
    }
    private void A118()        //고장내기 mk3 1,2,3 공용 증강 이기에 좀 남다른 코드임  현재 10 /30 /60 총합 100확률을 가지고 있습죠
    {
        if (player.GetComponent<BreakDownMk>()) //만약 BreakDownMk를 가지고 있다면
        {
            BreakDownMk Mk3 = player.GetComponent<BreakDownMk>();
            Mk3.PercentUp(10);
        }
        else
        {
            player.AddComponent<BreakDownMk>();
            BreakDownMk Mk3 = player.GetComponent<BreakDownMk>();
            Mk3.PercentUp(10);
        }
    }
    private void A119()// 반전 공격방향 , 이동방향이 반대가되고 공체 대폭 증가 == 현재 이동방향 반대만 구현 A119 A2105는 동일 함수 합치는거 고려
    {
        if (playerstatHandler.isNoramlMove)
        {
            playerInput.actions.FindAction("Move2").Enable();
            playerInput.actions.FindAction("Move").Disable();
            playerstatHandler.isNoramlMove = false;
        }
        else
        {
            playerInput.actions.FindAction("Move2").Disable();
            playerInput.actions.FindAction("Move").Enable();
            playerstatHandler.isNoramlMove = true;
        }
        playerstatHandler.HP.coefficient *= 1.5f;
        playerstatHandler.ATK.coefficient *= 1.5f;
    }
    private void A120()//워터 파크 개장 122의 물버전
    {
        player.AddComponent<A0120>();
    }
    private void A121() // 구르기 스택 +1 
    {
        playerstatHandler.MaxRollStack += 1;
        playerstatHandler.CurRollStack += 1;
    }
    private void A122()//화다닥 120의 불버전//122없음
    {
        player.AddComponent<A0122>();
    }
    private void A123()//큰힘큰책임 //총알 피아구분 x // 멀티 기준 팀킬 증강 삭제 해야 할수도 있음
    {
        //playerstatHandler.ATK.coefficient *= 1.3f;
        //WeaponSystem a = player.GetComponent<WeaponSystem>();
        //player.GetPhotonView().Owner.CustomProperties.TryGetValue("Char_Class", out object classNum);
        //if ((int)classNum != 2)
        //{
        //    a.targets["Player"] = (int)BulletTarget.Player;
        //}
        //else
        //{
        //    playerstatHandler.ATK.coefficient *= 1.1f;
        //    // 일단 무덤 : 업적 느낌으로 UI로 띄워주면 좋을 것 같은데,,,? : 도현님이 비명을 지르시겠지?
        //}
    }

    private void A124()//눈먼총잡이 : 시야가 대폭 감소 하며 공격 속도, 재장전 속도가 증가합니다.<<애매모호한듯?
    {
        //PhotonView photonView = PhotonView.Find(PlayerPvNumber);
        player.AddComponent<A0124>();//A0124에서 화면어둡게 하는 프리팹 만들고 스테이지시작에 ON 끝에 OFF
        playerstatHandler.AtkSpeed.added += 1;
        playerstatHandler.ReloadCoolTime.added += 2;
    }
    private void A125()//참기 a0125 클래스 삭제 후 수치 조정으로 변경 회피율 추가 a0125 스크립트 남겨두긴 했는데 삭제고려@@@@
    {
        //playerstatHandler.evasionPersent = 20;
    }
    private void A126()
    {
        //targetPlayer.AddComponent<A0126>();
    }
    private void A127()//재생력 코루틴 돌리기
    {
        player.AddComponent<A0127>();
    }
    private void A128()//프렌드 실드 현재 속도 140 @@@@@@@@@@@@@ 소환형
    {
        //GameObject prefab = PhotonNetwork.Instantiate("AugmentList/A0128", targetPlayer.transform.localPosition, Quaternion.identity);
        //int num = prefab.GetPhotonView().ViewID;
        //photonView.RPC("FindMaster", RpcTarget.All, num);
    }
    #endregion
    #region All2
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ 공용2티어
    private void A201()//탱탱볼 부활
    {
        playerstatHandler.BulletLifeTime.coefficient *= 2f;
        playerWeapon.canAngle = true;
    }
    private void A202()//베스트프렌드불렛 총알이거대해집니다 //현재 1.3배
    {
        playerWeapon.sizeUp = true;
    }
    private void A203()//버서커 최대체력 / 현재 체력비례 뎀증
    {
        player.AddComponent<A0203>();
    }
    private void A204()//로케이터 거리비례약해짐
    {
        playerWeapon.locator = true;
    }
    private void A205()//퍼스트 블러드 장전후 첫총알 데미지 증가
    {
        player.AddComponent<A0205>();
    }
    private void A206()
    {
        player.AddComponent<A0206>();
    }
    private void A207()//하이리스크 하이리턴
    {
        playerstatHandler.defense *= 2f;
        playerstatHandler.ATK.coefficient *= 2f;
    }
    private void A208()//회피의달인
    {
        player.AddComponent<A0208>();
    }
    private void A209()//재정비 구르기시 재장전 수행
    {
        player.AddComponent<A0209>();
    }
    private void A210() // 목숨형
    {
        //playerstatHandler.MaxRegenCoin += 1;
        //playerstatHandler.CurRegenCoin += 1;
        //playerstatHandler.RegenHP += 1;
    }
    private void A211()//피해복구 일정확률로 일정 체력 회복 목숨형 목숨형은 아닌데 스탯 관련임
    {
        player.AddComponent<A0211>();
    }
    private void A212()//강자멸시 현재 스테이지가 높을수록 공업
    {
        player.AddComponent<A0212>();
    }

    private void A213()//생존자 플레이어 혼자 남았을때 능력치업
    {
        player.AddComponent<A0213>();
    }
    private void A214()// 평타 극대화 << 이름 맘에안듬 스킬포기 데미지 대폭 증가 << 대폭 급인가? 그급은 아닌거 같은데
    {
        playerInput.actions.FindAction("Skill").Disable();
        playerstatHandler.isCanSkill = false;
        playerstatHandler.ATK.coefficient *= 1.3f;
    }
    private void A215()//화염
    {
        playerWeapon.burn = true;
    }
    private void A216()//아이스
    {
        // playerWeapon.ice = true;
    }
    private void A217()//용기의 깃발 범위내 이속 공속증가
    {
        player.AddComponent<A0217>();
    }
    private void A218()//과질량 장치
    {
        // playerWeapon.gravity = true;
    }
    private void A219() //고장내기mk2 1,2,3 공용 증강 이기에 좀 남다른 코드임 30
    {
        if (player.GetComponent<BreakDownMk>()) //만약 BreakDownMk를 가지고 있다면
        {
            BreakDownMk Mk3 = player.GetComponent<BreakDownMk>();
            Mk3.PercentUp(30);
        }
        else
        {
            player.AddComponent<BreakDownMk>();
            BreakDownMk Mk3 = player.GetComponent<BreakDownMk>();
            Mk3.PercentUp(30);
        }
    }
    private void A220()// 피흡은 좀 사기 같음 
    {
        player.AddComponent<A0220>();
        A0220 drainComponent = player.GetComponent<A0220>();
        drainComponent.PercentUp(5);
    }
    private void A221() // 이게 누구더라
    {
        player.AddComponent<A0221>();
    }
    private void A222()//재정비 구르기후 회복
    {
        player.AddComponent<A0222>();
    }
    private void A223() // 스킬 스택 +
    {
        playerstatHandler.MaxSkillStack += 1;        
        playerstatHandler.CurSkillStack += 1;
    }
    #endregion
    #region All3
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@공용 3티어
    private void A301()//고장내기 mk3 1,2,3 공용 증강 이기에 좀 남다른 코드임 
    {
        if (player.GetComponent<BreakDownMk>()) //만약 BreakDownMk를 가지고 있다면
        {
            BreakDownMk Mk3 = player.GetComponent<BreakDownMk>();
            Mk3.PercentUp(60);
        }
        else
        {
            player.AddComponent<BreakDownMk>();
            BreakDownMk Mk3 = player.GetComponent<BreakDownMk>();
            Mk3.PercentUp(60);
        }
    }
    private void A302()//인피니티불렛 탄창 9999 획득시점의 총알 값 계산하여 9999로 맞춰줌 많든 적든 같음
    {
        playerstatHandler.AmmoMax.added += 99 - playerstatHandler.AmmoMax.total;
    }
    private void A303()//분신 사실상 포기한 기술
    {
        /*
        Debug.Log("미완성");
        ChangeOnlyPlayer(PlayerNumber);
        if (targetPlayer.GetPhotonView().IsMine)
        {
            GameObject prefab = PhotonNetwork.Instantiate("AugmentList/A0303", targetPlayer.transform.localPosition, Quaternion.identity);
            prefab.GetComponent<A0303>().Initialize(prefab.transform);
            int num = prefab.GetPhotonView().ViewID;
            photonView.RPC("FindMaster", RpcTarget.All, num);
        }
        */
    }
    private void A304() // 목숨 3티어 급이 아닌데 뭐지
    {
        //playerstatHandler.MaxRegenCoin += 1;
        //playerstatHandler.CurRegenCoin += 1;
        //playerstatHandler.HP.coefficient *= 0.5f;
    }
    private void A305()//멀티샷 샷2배
    {
        playerstatHandler.LaunchVolume.coefficient *= 2;
    }

    #endregion
    #region Sniper1
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@스나이퍼 1티어
    private void A1101() //대기만성 적 타격시 공증
    {
        player.AddComponent<A1101>();
    }
    private void A1102()//경량화 << 이름뭔가 이상함 장탄수가 5 증가 하며 데미지 감소, 공격 속도 증가, 이동속도 증가를 얻습니다.
    {
        playerstatHandler.AmmoMax.added += 5;
        playerstatHandler.ATK.coefficient *= 0.9f;
        playerstatHandler.AtkSpeed.coefficient *= 1.1f;
        playerstatHandler.Speed.coefficient *= 1.1f;
    }
    private void A1103()
    {
        player.AddComponent<A1103>();
    }
    private void A1104()//플래시 
    {
        player.AddComponent<A1104>();
        //playerInput.Flash = true;
        playerInput.actions.FindAction("Flash").Enable();
        playerInput.actions.FindAction("Roll").Disable();
        playerInput.actions.FindAction("SiegeMode").Disable();
    }
    private void A1105()//오토 쉬프트
    {
        player.AddComponent<A1105>();
        playerInput.actions.FindAction("Skill").Disable();
        playerstatHandler.isCanSkill = false;
        playerstatHandler.ATK.coefficient *= 1.5f;
    }
    private void A1106() // 소환형
    {
        /*
        GameObject prefab = PhotonNetwork.Instantiate("AugmentList/A1106", targetPlayer.transform.localPosition, Quaternion.identity);
        int num = prefab.GetPhotonView().ViewID;
        photonView.RPC("FindMaster", RpcTarget.All, num);
        prefab.GetComponent<A1106>().Init();
        */
    }
    private void A1107() //영역전개 최초의 대상에게 영구적으로 올려주는 타입 아직 세세한 오류가 있을것으로 예상된 소환형
    {
        //ChangeOnlyPlayer(PlayerNumber);
        //if (targetPlayer.GetPhotonView().IsMine)
        //{
            //GameObject Prefabs = PhotonNetwork.Instantiate("AugmentList/A1107", targetPlayer.transform.localPosition, Quaternion.identity);
            //int num = Prefabs.GetPhotonView().ViewID;
            //photonView.RPC("FindMaster", RpcTarget.All, num);
            //Prefabs.GetComponent<A1107>().Init();
        //}
    }
    #endregion
    #region Sniper2
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@스나이퍼 2티어
    private void A1201()//관통탄
    {
        // playerWeapon.Penetrate = true;
        playerstatHandler.AmmoMax.added -= playerstatHandler.AmmoMax.total - 1;
        playerstatHandler.ReloadCoolTime.coefficient *= 0.5f;
        playerstatHandler.ATK.added += 5f;
    }
    private void A1202()//최장거리 저격 로케이터의 반대버전
    {
        playerWeapon.sniping = true;
    }
    private void A1203()
    {
        Debug.Log("미완성");
    }
    private void A1204()
    {
        Debug.Log("미완성");
    }
    private void A1205()//신중한 사격 스킬체크
    {
        player.AddComponent<A1205>();
    }
    private void A1206()
    {
        player.AddComponent<A1206>();
    }
    private void A1207()//>>이동다끔 콜리전끔 포지션업데이트로 다른플레이어값 받아서 // 사실상 멀티용이라 버려야할거같음
                                        //돌림 시작하자마자 플레이어 목숨 -1 그냥 죽은취급
    {
        /*
        ChangePlayerAndPlayerStatHandler(PlayerNumber);
        targetPlayer.AddComponent<A1207>();
        playerstatHandler.ATK.coefficient *= 1.5f;
        if (targetPlayer.GetPhotonView().IsMine) 
        {
            PlayerInputController inputController = targetPlayer.GetComponent<PlayerInputController>();
            inputController.cantMove = true;
            inputController.cantSpacebar = true;
            playerInput = targetPlayer.GetComponent<PlayerInput>();
            playerInput.actions.FindAction("Move2").Disable();
            playerInput.actions.FindAction("Move").Disable();
            playerInput.actions.FindAction("SiegeMode").Disable();
            playerInput.actions.FindAction("Flash").Disable();
            playerstatHandler.ImGhost = true;
        }
        */
    }
    #endregion
    #region Sniper3
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@스나이퍼 3티어
    private void A1301()//고급지원가 
    {
        // playerWeapon.sniperAtkBuff = true;
    }
    private void A1302()//리바이브샷 스테이지당 한번 아군 부활시키기
    {
        // playerWeapon.canresurrection = true;
        player.AddComponent<A1302>();
    }
    private void A1303()// 내용 이해 못함 나중에 제목보고 다시 체크 할것
    {
        if (player.GetComponent<PlayerKimKilWhan>() != null)
        {
            playerstatHandler.AmmoMax.added += 29;
            playerstatHandler.CurAmmo += 29;
        }
    }
    private void A1304()// 기회비용 힐모드 변경 x 딜모드 딜량증가 애도 사실상 멀티용이라 제거 각인데
    {
        /*
        if (targetPlayer.GetPhotonView().IsMine) 
        {
            WeaponSystem weaponSystemA = targetPlayer.GetComponent<WeaponSystem>();
            playerInput = targetPlayer.GetComponent<PlayerInput>();
            playerInput.actions.FindAction("Skill").Disable();
            playerstatHandler.isCanSkill = false;
            weaponSystemA.isDamage = true;
            playerstatHandler.ATK.coefficient *= 1.5f;
        }
        */
    }
    #endregion
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@솔져 1티어
    private void A2101() //노련함 = 스킬사용후 공속 증가 테스트 ㄴ
    {
        player.AddComponent<A2101>();
    }
    private void A2102() ///와다다다ㅏ다다 
    {
        playerstatHandler.AtkSpeed.coefficient *= 1.5f;
        playerstatHandler.ATK.coefficient *= 0.5f;
    }
    private void A2103()//긴장감 주변 적 비례 스탯 ++//이거 생각보다 엄청까다롭네 소환형
    {
        /*
        GameObject prefab = PhotonNetwork.Instantiate("AugmentList/A2103", targetPlayer.transform.localPosition, Quaternion.identity);
        int num = prefab.GetPhotonView().ViewID;
        photonView.RPC("FindMaster", RpcTarget.All, num);
        prefab.GetComponent<A2103>().Init();
        */
    }
    private void A2104()//무기교체 :  핸드건 >> 등가 교환 최대 장탄수가 감소하지만 기본 스킬 스팀팩 효과를 증가시키는 핸드건으로변경
    {
        playerstatHandler.AmmoMax.added -= 5;
        PlayerCharlieSkill skill = player.GetComponent<PlayerCharlieSkill>();
        if (skill != null) 
        {
            skill.applicationAtkSpeed += 1f;
            skill.applicationspeed += 0.5f;
        }

    }
    private void A2105()// 반전 공격방향 , 이동방향이 반대가되고 공체 대폭 증가 == 현재 이동방향 반대만 구현 A119 A2105는 동일 함수 합치는거 고려
    {
        if (playerstatHandler.isNoramlMove)
        {
            playerInput.actions.FindAction("Move2").Enable();
            playerInput.actions.FindAction("Move").Disable();
            playerstatHandler.isNoramlMove = false;
        }
        else
        {
            playerInput.actions.FindAction("Move2").Disable();
            playerInput.actions.FindAction("Move").Enable();
            playerstatHandler.isNoramlMove = true;
        }
        playerstatHandler.HP.coefficient *= 1.5f;
        playerstatHandler.ATK.coefficient *= 1.5f;
    }
    private void A2201()// 빈틈 만들기 //기본 공격 시 구르기 쿨타임이 감소합니다.
    {
        player.AddComponent<A2201>();
    }
    private void A2202()//티타임 구른후 스킬 재사용 대기시간 감소
    {
        player.AddComponent<A2202>();
    }
    private void A2203()//구른자리에힐생성 힐한다는거 자체가 어캐 될지 모르겠음 
    {
        player.AddComponent<A2203_1>();
    }
    private void A2204()//열광전염 소환형 멀티용
    {
        /*
            GameObject prefab = PhotonNetwork.Instantiate("AugmentList/A2204", targetPlayer.transform.localPosition, Quaternion.identity);
            int num = prefab.GetPhotonView().ViewID;
            photonView.RPC("FindMaster", RpcTarget.All, num);
            prefab.GetComponent<A2204>().Init();
        */
    }
    private void A2205()//무기교체 어썰트라이플 >> 묵직한 탄창 장탄수 30+ 이동속도- 구르기 쿨업
    {
        playerstatHandler.AmmoMax.added += 30;
        playerstatHandler.Speed.added -= 0.5f;
        playerstatHandler.RollCoolTime.added += 2;
    }
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@솔져 3티어
    private void A2301()// 집중 총알이 1발이 되지만 감소한 총알수 비례 공 ++
    {
        float changePower = playerstatHandler.AmmoMax.total - 1;
        playerstatHandler.AmmoMax.added -= playerstatHandler.AmmoMax.total - 1;
        playerstatHandler.ATK.added += changePower * 3f;
        playerstatHandler.ReloadCoolTime.added += 1f;
    }
    private void A2302()// 유도탄
    {
        player.GetComponent<WeaponSystem>().humanAttackintelligentmissile = true;
        playerstatHandler.ATK.coefficient *= 0.9f;
    }
    private void A2303()//아크로바틱 샷 
    {
        player.AddComponent<A2303>();
    }
    private void A2304()//스팀팩 막히고 일부 상시 적용
    {
        playerInput.actions.FindAction("Skill").Disable();
        playerstatHandler.isCanSkill = false;
        PlayerCharlieSkill skill = player.GetComponent<PlayerCharlieSkill>();
        if (skill != null)
        {
            skill.applicationAtkSpeed *= 0.5f;
            skill.applicationspeed *= 0.5f;
        }
    }
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@샷건 1티어
    private void A3101()//쉬는시간
    {
        player.AddComponent<A3101>();
    }
    private void A3102()//굳은살 스킬 사용후 체력증가
    {
        player.AddComponent<A3102>();
    }
    private void A3103()//시즈모드 구르기를 시즈모드로 변경 
    {
            player.AddComponent<A3103>();
            player.GetComponent<PlayerInputController>().siegeMode = true;
            playerInput.actions.FindAction("Roll").Disable();
            playerInput.actions.FindAction("SiegeMode").Enable();
    }
    private void A3104()
    {
        player.AddComponent<A3104>();
    }
    private void A3105()//공격태세 스킬 사용시 다음 공격을 강화 시키는 스킬로 대체 #스킬대체 #다음공경
    {
        player.AddComponent<A3105>();
    }
    private void A3106()
    {
        player.AddComponent<A3106>();
    }
    private void A3107() // 파이어 토네이도 테스트안함 소환형
    {
        /*
            GameObject prefab = PhotonNetwork.Instantiate("AugmentList/A3107", targetPlayer.transform.localPosition, Quaternion.identity);
            prefab.GetComponent<A3107>().Init(targetPlayer);
            int num = prefab.GetPhotonView().ViewID;
            photonView.RPC("FindMaster", RpcTarget.All, num);
        */
    }
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@샷건 2티어
    private void A3201() //굴러서 장전 << 구르기 2초증가 장탄수 +3으로 재장전
    {       
        playerstatHandler.RollCoolTime.added += 1f;
        player.AddComponent<A3201>();
    }
    private void A3202()//저는 저는 펌프 액션 샷건이 싫어요최대 장탄수가 5 증가 하며 연사속도 를 얻고
    {
        playerstatHandler.AmmoMax.added += 5;
        playerstatHandler.AtkSpeed.coefficient *= 1.1f;
    }
    private void A3203()//사이즈업 몸2배체력3배
    {
        float x = (player.transform.localScale.x * 2f);//절반
        float y = (player.transform.localScale.y * 2f);//절반
        player.transform.localScale = new Vector2(x, y);
        playerstatHandler.HP.coefficient *= 3;
    }
    private void A3204()
    {
        player.AddComponent<A3204>();
    }
    private void A3205()//기쁨은 나누면 두배 팀도 실드  //2204의 샷건버전 소환형 멀티
    {
        /*
            GameObject prefab = PhotonNetwork.Instantiate("AugmentList/A3205", targetPlayer.transform.localPosition, Quaternion.identity);
            int num = prefab.GetPhotonView().ViewID;
            photonView.RPC("FindMaster", RpcTarget.All, num);
            prefab.GetComponent<A3205>().Init();
        */
    }
    private void A3206()//공병 스킬
    {
        player.AddComponent<A3206>();
        playerstatHandler.SkillCoolTime.added += 3f;
    }
    private void A3207()//보호 모드 실드 크기 업인데 싱글이면 실드 크기 커져서 뭐함? 지켜줄 팀이 없는데
    {
        PlayerKimKilWhan player2 = player.GetComponent<PlayerKimKilWhan>();
        if (player2 != null)
        {
            player2.shieldScale += 0.5f;
        }
        playerstatHandler.HP.coefficient *= 0.8f;
        player.AddComponent<A3207>();
    }
    //@@@@@@@@@@@@@@@@@@@@@@@@@@@샷건 3티어
    private void A3301()//
    {
        player.AddComponent<A3301>();
    }
    private void A3302()//쉴드 범위 증가, 쉴드량 증가, //  평타 약화,  쉴드 안에 아군 버프//
    {
        PlayerKimKilWhan player2 = player.GetComponent<PlayerKimKilWhan>();
        if (player2 != null)
        {
            player2.shieldScale *= 2f;
            player2.shieldHP += 20f;
        }
        playerstatHandler.ATK.coefficient *= 0.8f;
    }
    private void A3303()//닥치고 돌격 스킬x 모능업
    {
            playerstatHandler.AmmoMax.added += 5f;
            playerstatHandler.AtkSpeed.added += 1f;
            playerstatHandler.RollCoolTime.added -= 2f;
            playerInput.actions.FindAction("Skill").Disable();
            playerstatHandler.isCanSkill = false;
    }
}
