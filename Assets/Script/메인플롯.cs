/*
================================================================
  Creature-Tower 게임 기획 분석 메모
  최종 업데이트: 2026-03-12
================================================================

■ 장르 및 기본 구조
  - 2D 탑뷰 싱글플레이 로그라이트 보스 러쉬 슈터
  - 1스테이지씩 보스를 처치하며 타워를 등반하는 구조

  ※ MainGameManager : 기존(멀티) 프로젝트 코드 - 참고/이관용으로 보존
  ※ GameManager     : 현재 메인 게임 플롯 관리자 (실제 사용 중)

----------------------------------------------------------------
■ 게임 플로우 (GameStates - MainGameManager)

  Init → UIPlaying → Start → Playing → End → AugmentListing → (반복)

  - UIPlaying      : 스테이지 시작 전 캐릭터 선택 / 준비 UI
  - Start          : 스테이지 시작, 몬스터 스폰
  - Playing        : 실제 전투 (currentMonsterCount == 0 이 되면 End로)
  - End            : 스테이지 클리어 판정
  - AugmentListing : 증강(강화) 선택 화면 → 다음 스테이지로

----------------------------------------------------------------
■ 스테이지 구조 (GameManager - 타워 등반 방식)

  [전체 흐름]
  1. 게임 시작 → 캐릭터 선택 → GameManager.Init()
  2. MakeStageTree() : stageLevel1~15 리스트 중 등록된 층만 수집
  3. SetStageTree()  : 각 층의 리스트에서 랜덤 1개 프리팹 선택 → 50씩 y값 올려 배치
  4. 플레이어가 방 입장 (StageInCheckPoint 트리거)
     → 보스가 BossSpawnPoint 위치로 이동 후 활성화
     → 아래 문(botDoor) 잠김
  5. 보스 처치 (bossCount 가 0이 되면)
     → GameManager.BossCountMinus() → Stage.OpenTopDoor()
     → 위쪽 문(topDoor) 개방
  6. 플레이어가 포션(RandomHealPoint) 및 보상 아이템(ResultBoxNeedFix) 수령
     → 보상 아이템 접촉 시 ResultManager UI 호출 → 증강 3개 선택
  7. 다음 층 계단(NextStageStairs) 접촉 → GameManager.NextLevel()
     → 다음 층 Stage 활성화, 플레이어 PlayerSpawnPoint로 이동
  8. 모든 층 클리어 → 엔딩 (현재 데모는 thankDemoUI 표시)

  [Stage 프리팹 구성 - 개발자가 MapMakeSetting 씬에서 직접 제작]
    Stage (프리팹)
    ├── 타일맵           - 방 레이아웃 (룰타일로 제작)
    ├── bossOBJ         - 해당 방 전용 보스 (BossBase 상속)
    ├── BossSpawnPoint  - 보스 시작 위치
    ├── PlayerSpawnPoint- 플레이어 스폰 위치
    ├── botDoor         - 아래 문 (입장 후 잠김)
    ├── topDoor         - 위 문 (보스 처치 후 개방)
    ├── StageInCheckPoint - 입장 트리거 (보스 소환 + 문 잠금)
    ├── NextStageStairs - 다음 층 이동 트리거
    ├── RandomHealPoint - 포션 랜덤 스폰 (Potion.cs)
    └── ResultBoxNeedFix- 보상 아이템 스폰 위치 (TODO: Stage.ResultPickPoint)

  [stageLevel 리스트]
    - stageLevel1~15 각각에 해당 층에서 등장 가능한 보스방 프리팹들을 등록
    - 리스트가 비어있으면 해당 층은 스킵됨
    - 실제 등록은 Unity Inspector에서 직접 설정

  ※ GameManager     : 타워 등반 로직 (방 생성/배치/이동) - 현재 메인
  ※ MainGameManager : 기존 멀티 프로젝트에서 가져온 코드, 이관 참고용

----------------------------------------------------------------
■ 플레이어 시스템

  [캐릭터 3종 및 직업 계승]
  - TV          : 스나이퍼 역할 계승 (기존 스나이퍼 캐릭터가 권총으로 바뀌면서 TV가 이 역할 계승)
  - Charlie     : 솔져(따발총) 역할 계승 (기존 Soldier 클래스)
  - KimKilWhan  : 샷건 역할 계승 (기존 Shotgun 클래스)

  [스탯 구조 - Stats 클래스]
    total = (basic + added) * coefficient
    - basic       : 기본값 (PlayerSO ScriptableObject에서 로드)
    - added       : 증강 등으로 추가되는 고정값
    - coefficient : 배율 계수

  [주요 스탯 목록]
    ATK, HP, Speed, AtkSpeed, ReloadCoolTime, SkillCoolTime,
    RollCoolTime, BulletSpread(탄퍼짐), BulletLifeTime(사거리),
    LaunchVolume(발사수), Critical, AmmoMax, defense

  [액션]
    이동, 조준사격(WeaponSystem), 구르기(Roll), 스킬, 장전,
    시즈모드 / 플래시 (증강으로 활성화됨)

  [이동 반전 시스템]
    - isNoramlMove 플래그로 Move / Move2 인풋액션 전환
    - A119 증강(반전) 적용 시 이동 방향 반전

----------------------------------------------------------------
■ 무기 시스템 (WeaponSystem)

  [타입]
    - Shooting  : 일반 연사
    - Charging  : 차징

  [속성 플래그]
    fire(불), water(물), ice, burn, gravity, Penetrate(관통)

  [특수 옵션]
    - sizeBody                    : 몸 크기 비례 총알 크기
    - locator                     : 위치 추적
    - sniping                     : 스나이핑
    - humanAttackintelligentmissile : 추적탄
    - canresurrection             : 부활 가능
    - sniperAtkBuff               : 스나이퍼 공격 버프

  [총알 관리]
    오브젝트 풀링 방식 (poolDictionary)
    - WeaponSystem.pools : Inspector에서 tag / prefab / count 설정
    - poolParent         : 씬의 BulletPool 오브젝트 할당 → 풀링 총알이 자식으로 관리됨
    - 총알 SortingOrder  : 9 (레이어 규칙 준수)
    - 각 캐릭터 전용 총알 프리팹: BulletTV / BulletCharlie / BulletKimKilWhan

    ※ 주의: Charlie / KimKilWhan 프리팹은 루트가 비활성(m_IsActive:0)이므로
      Instantiate 직후 Awake()가 자동 실행되지 않음.
      → StartObjectPOOL() 전에 반드시 player.SetActive(true) 호출 필요

----------------------------------------------------------------
■ 증강(Augment) 시스템 - 핵심 로그라이트 요소

  스테이지 클리어 후 3개의 강화 선택지 제공
  증강은 티어(1~3)와 직업별/공용으로 분류

  [코드 체계]
    A9xx  : 스탯 증강 (티어별 공/체/이속/공속/정밀도/쿨감/크리/장탄)
    A1xx  : 공용 1티어 특수 증강  (100~199)
    A2xx  : 공용 2티어 특수 증강  (200~299)
    A3xx  : 공용 3티어 특수 증강  (300~399)
    A1xxx : TV 전용 증강      (1000~1999, CharacterClass=0)
    A2xxx : Charlie 전용 증강 (2000~2999, CharacterClass=1)
    A3xxx : KimKilWhan 전용 증강 (3000~3999, CharacterClass=2)

    ※ 직업 전용 증강(code≥1000) 은 AugmentManager.AugmentCall()에서
      requiredClass = (code/1000)-1 로 계산 후 현재 CharacterClass와 비교,
      불일치 시 차단 (Debug.LogWarning 후 return)

  [호출 방식]
    AugmentManager.AugmentCall(code)
    → SendMessage("A" + code)
    → 해당 private void A####() 실행
    → 특수 증강은 player.AddComponent<A####>() 방식으로 동작 컴포넌트 부착

  [데이터 소스]
    Resources/CSVReader/ 폴더의 CSV 파일에서 증강 목록 로드
    (stat1, stat2, stat3, Shotgun1~3, Sniper1~3, All1~3 등)

  [증강 획득 흐름]
    1. 캐릭터 선택 시 MakeAugmentListManager.MakeLisk()
       → 캐릭터 전용(TV=Sniper/Charlie=Soldier/KimKilWhan=Shotgun) + 공용(All)
       → 티어별(1/2/3) 증강 리스트 생성 (CSV에서 로드)
    2. 스테이지 내 ResultBoxNeedFix 위치에 보상 아이템 스폰 (Stage.ResultSummon)
    3. 플레이어가 보상 아이템에 닿음
       → ResultManager가 현재 티어에 맞는 증강 3개 랜덤 추출 → UI 표시
    4. 플레이어가 하나 선택
       → AugmentManager.AugmentCall(code) → SendMessage("A"+code) → 즉시 적용

  [증강 목록 UI]
    - ResultManager          : 선택 화면 전체 관리
    - ChoiceSlot             : 개별 선택 슬롯 (이름/설명/티어/직업 아이콘 표시)
    - MySpecialList          : 현재 보유 증강 목록 확인 UI
    - MakeAugmentListManager : 증강 리스트 생성/관리
    - AugmentManager         : 증강 코드 → 실제 효과 적용

----------------------------------------------------------------
■ 보스 시스템

  - 각 방(Stage)마다 보스 존재 (BossBase 상속)
  - bossCount 로 다수 보스 관리 → 전부 처치 시 TopDoor 개방
  - 인트로 무적 시간 지원 (IntroTime)
  - 현재 구현된 보스 : ThreeMonkeyBoss (세 원숭이 보스, MonkeyPart 개별 관리)

----------------------------------------------------------------
■ 레이어 규칙 (A_order layer rule.txt)

  0~1  : 배경 (Background)
  2~4  : 적 및 오브젝트
  5~7  : 큰 적의 뒷다리 (원근감)
  8    : 플레이어 뒤
  9    : 플레이어 아래 오브젝트
  10   : 플레이어
  11   : 플레이어 위 오브젝트
  12   : (여유)
  13~15: 몬스터 2 (플레이어보다 앞에 그려지는 적)

----------------------------------------------------------------
■ 테스트씬 시스템 (MapMakeSetting)

  TestGameManager : 씬 전용 게임매니저
    - 캐릭터 선택 UI → SelectTV/SelectCharlie/SelectKimKilWhan 버튼
    - StartTest(prefab) : 소환 → SetActive(true) → BulletPool 생성 → StartObjectPOOL()

  TestAugmentUI   : 증강 디버그 목록 UI
    - "증강 목록" 버튼(좌상단) → 토글 패널
    - 섹션 분류 : ★ 직업 티어1/2/3, ■ 공용 티어1/2/3
    - 행 클릭 → AugmentManager.AugmentCall(code)

  TestAugmentManager : Galmuri9 SDF 폰트 관리 (한글 지원)
    - TestAugmentUIRoot에 부착, Inspector에서 fontAsset 할당

----------------------------------------------------------------
■ UI / 렌더링 규칙

  ScrollView Viewport : RectMask2D 사용 권장
    - Mask + Image(Color.clear) 조합은 스텐실 실패로 전체 클리핑 버그 발생
    - RectMask2D는 Image 없이 RectTransform 경계로 클리핑 (안정적)

  비활성 상태에서 레이아웃 생성 시 주의
    - ContentSizeFitter / VerticalLayoutGroup은 비활성 GameObject에서 계산 안 됨
    - 패널 활성화 후 Canvas.ForceUpdateCanvases() + LayoutRebuilder.ForceRebuildLayoutImmediate() 필요

  SpriteRenderer.color 주의
    - Color 채널 범위는 0~1 (float)
    - new Vector4(255,255,255,255) 은 255배 포화 → 흰색으로 날아감
    - 올바른 사용: Color.white (=new Color(1,1,1,1)), 투명: new Color(1,1,1,0)

----------------------------------------------------------------
■ 현재 개발 상황 / TO-DO 메모

  [수정 완료]
  - PlayerTVSkill.cs  : CompulsoryRollEnd() 무한재귀 수정
  - Bullet.cs         : CancelInvoke, time=0f, Invoke→직접호출
  - GameManager.cs    : Init() 실행순서, StageLevelSet null 가드
  - WeaponSystem.cs   : StartObjectPOOL null 안전처리, SortingOrder=9
  - PlayerAnimatorController.cs : 구르기 후 총 흰색 버그 수정 (Vector4→Color)
  - AugmentManager.cs : 직업 불일치 증강 차단 로직 추가
  - TestGameManager.cs: 비활성 프리팹 SetActive(true) 처리, BulletPool 부모 생성
  - MapMakeSetting 씬  : Viewport Mask→RectMask2D 교체

  [미구현 / 주석 처리된 기능]
  - Gold(돈) 시스템 → 상점 연동 미구현
  - isEventRoom(이벤트 방) 미구현
  - isShopRoom(상점 방) 미구현
  - A123 (큰힘큰책임 - 팀킬) : 멀티 의존으로 비활성화
  - A128 (프렌드 실드 소환형) : Photon.Instantiate 의존으로 비활성화
  - SpawnMonster() 전체 : Photon 의존으로 주석 처리됨
  - SpawnPlayer() 전체 : Photon 의존으로 주석 처리됨
  - AllReady() 멀티 레디 체크 : 비활성화
  - GameManager.MakeStageTree() : stageLevel2~15가 모두 stageLevel1 추가하는 버그 있음 (수정 필요)
  - Stage.ResultSummon() : ResultPickPoint(보상 아이템) 연결 로직 TODO 상태

  [구조적 이슈]
  - GameManager 와 MainGameManager 두 매니저 병용 중 (MainGameManager는 참고/이관용)
  - 소환형 증강들 (현재 소환 개념 수정 예정, 주석 "소환형" 으로 표시됨)
  - 목숨형 증강 개념 미정 (주석 "목숨형" 으로 표시됨)
  - A117 (777 증강) : atkPercent 스탯 비활성화 상태로 미완성
  - A126 (반사타입) : 주석 처리됨
  - Charlie/KimKilWhan 프리팹 루트 비활성 상태 → 의도인지 확인 후 정리 필요
  - Resources.Load(CSV) → 나중에 Addressables 교체 검토 (현재 보류)

================================================================
*/
