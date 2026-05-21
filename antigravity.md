# Creature-Tower 프로젝트 지식 문서 (Project Knowledge)
최종 업데이트: 2026-05

이 문서는 프로젝트의 핵심 구조, 설계 철학, 그리고 코드 규칙을 정리한 문서입니다. (클로드의 `claude.md` 역할)

---

## 1. 장르 및 게임 플로우
- **장르**: 2D 탑뷰 싱글플레이 로그라이트 보스 러쉬 슈터
- **플로우**: `Init → UIPlaying → Start → Playing → End → AugmentListing → 다음 층 이동`
- **구조**: 방 입장(`StageInCheckPoint`) -> 보스/적 소환 및 문 잠금 -> 전투(보스 인트로 후 시작) -> 클리어(`aliveBossCount == 0`) -> 문 개방 및 보상 획득 -> 다음 층(`NextStageStairs`).
- **메인 매니저**: `GameManager` (구버전 `MainGameManager`는 이관용으로만 보존)

---

## 2. 몬스터 시스템 (CreatureBase 아키텍처)
최근 리팩토링을 통해 보스와 일반 몬스터의 공통 기능을 단일화했습니다.

### 구조
- **`CreatureBase`** (추상 클래스)
  - `BossBase`와 `EnemyBase`의 공통 부모 클래스입니다.
  - **역할**: 물리 초기화, Y-Sorting, 총알 피격(`Damege`), 플레이어 접촉 데미지, HP 관리 등을 단일화하여 코드 중복을 최소화했습니다.
- **`BossBase`**
  - `CreatureBase`를 상속받으며, 보스 특유의 생명주기(Intro 연출, 무적 판정 등)를 관리합니다.
- **`EnemyBase`**
  - `CreatureBase`를 상속받는 일반 웨이브 몬스터용 베이스입니다.

### 데이터 주입 (EnemySO / MonsterSO)
- **역할 분리 원칙**: "수치와 밸런스는 SO, 전투 규칙 차이는 파생 클래스, 의사결정은 BT(Behaviour Tree)"
- **EnemySO**: 몬스터의 스탯(HP, ATK, Speed), Intro 시간, 카운트, BT용 행동 파라미터(추적 거리 등)를 담고 있습니다. Boss와 Enemy 모두 이 데이터를 주입받아 초기화합니다.

### 물리 및 충돌 처리 규칙
- **Root / Hurtbox 분리**
  - 이동/충돌용 콜라이더(Root)와 플레이어 공격(총알/스킬) 피격용 콜라이더(Hurtbox 자식)를 분리합니다 (`useSeparateHurtbox`).
  - 총알 피격은 Hurtbox에서만 처리되며 중복 피격을 방지합니다(`Bullet.TryMarkBossHit`).
- **물리 이동 안정화**
  - 보스/몬스터의 이동은 Cast + MovePosition 기반으로 처리.
  - 몬스터끼리 겹칠 경우 Soft-body 기반 미세 밀어내기로 멈춤 현상(stuck)을 완화.
  - 분리체 소환 시 벽/오브젝트 겹침이 발생하면 overlap resolve를 통해 밖으로 밀어내도록 보정(`ThreeMonkeyBoss` 적용 완료).

---

## 3. 플레이어 / 무기 / 증강 시스템

### 플레이어 및 렌더링 (Y-Sorting)
- **클래스**: TV(스나이퍼/0), Charlie(솔져/1), KimKilWhan(샷건/2).
- **Y-Sorting 규칙**: Layer 단위 정렬 사용
  - 바닥(`World_Static`), 장판(`World_GroundFX`), 몬스터/플레이어(`World_Dynamic`).
  - `World_Dynamic` 내에서는 y좌표 기준으로 sortingOrder를 동적 갱신(기본 1000). 총알은 기본적으로 `9`, 플레이어는 `10`.

### 증강(Augment) 시스템
- 스테이지 클리어 후 3개의 선택지가 제공되며, 등급(1~3티어)과 직업 전용(코드 >= 1000)으로 나뉩니다.
- `AugmentManager.AugmentCall(code)` 를 통해 `A123` 형태의 컴포넌트가 플레이어에 즉시 부착되어 동작합니다.

### 무기 피격 로직 (입원숭이 디버프 예시)
- 플레이어가 디버프(사격불가 등)를 받을 시, 현재 홀드 중인 공격 입력 상태도 강제로 끊습니다 (`ForceStopAttackInput()`).

---

## 4. UI 및 레이아웃 주의사항
- 스크롤 뷰의 클리핑은 Mask + Image 대신 **RectMask2D**를 사용하여 스텐실 실패 버그를 방지합니다.
- Color 조정 시 Vector4(255,255,255)가 아닌 0~1 단위 범위의 Color 구조체(`Color.white` 등)를 사용해야 그래픽이 튀지 않습니다.

---

## 5. 앞으로의 개발 우선순위 및 가이드
1. **일반 몬스터(EnemyBase) 양산화**: `CreatureBase`가 완성되었으므로, 이를 기반으로 `EnemySO`와 `EnemyBase`를 사용하여 몬스터 패턴 추가 및 확장을 수월하게 진행합니다.
2. **보스 패턴 파라미터화**: BT 노드의 조건들을 SO 기반 파라미터로 지속 분리하여 런타임 하드코딩을 없앱니다.
3. **경제(Gold) 및 상점**: 타워 등반 도중 상점과 재화(Gold) 시스템 연동 마무리.
4. **테스트 씬 활용**: `TestGameManager` 환경에서 개별 몬스터 행동과 증강(Augment) 로직 분리 검증.

---

## 6. 방(Room) 테마 및 보상(Reward) 시스템
일반 스테이지(`NormalStage`)는 생성 시 방 테마(`RoomTheme`)를 지정받고, 클리어 시 또는 진입 즉시 그에 부합하는 보상 기물들을 스폰합니다.

### 방 테마 (`RoomTheme`) 및 특징
- **Mystery (`?` 물음표 방)**
  - 보상 종류가 무작위로 선택되며, 층(Act) 번호에 따라 최대 보상 개수(`ResolveMaxRewards()`)가 증가합니다.
  - 추가 보상이 연쇄적으로 스폰될 확률(20%)이 존재합니다.
- **Shop (상점 방)**
  - 💰 상인이 확정 스폰되어 플레이어가 골드(Coin)로 각종 아이템이나 무기를 구매할 수 있습니다.
- **Transfusion (수혈기 방)**
  - ♥ 수혈 기기가 확정 스폰되어 플레이어의 최대 체력(MaxHP)을 지불하거나 다른 대가를 지불하여 버프/상호작용을 할 수 있습니다.
- **DNA (DNA 방 / 증강 상자 방)**
  - 🧬 **증강 상자 (Augment Box)**가 확정 스폰되며, 개방 시 **100% 확률로 DNA**를 떨어뜨립니다. (플레이어는 이를 통해 증강(Augment)을 획득)
- **Coin (코인 방 / 코인 상자 방)**
  - 💰 **코인 상자 (Coin Box)**가 확정 스폰되며, 개방 시 **100% 확률로 골드(코인 3~7개)**를 떨어뜨립니다.
- **Box (일반 상자 방)**
  - 📦 **골드(CoinBox) 또는 DNA(AugmentBox)** 상자가 50:50 반반 확률로 무작위 배정되어 스폰됩니다.
- **Potion (포션 방)**
  - 🧪 **포션**이 스폰되어 접촉 시 **최대 체력의 50%**를 회복시켜 줍니다.

### 보상 상자의 차이 요약 (코인 상자 vs 증강 상자)
- **일반 상자 (RandomBox)**: 50%는 코인, 50%는 DNA (5% 확률로 레어 DNA 획득 가능).
- **코인 상자 (CoinBox)**: 100% 확정 코인 드롭.
- **증강 상자 (AugmentBox)**: 100% 확정 DNA 드롭.

