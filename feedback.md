# Creature-Tower 인수인계 문서 (ThreeMonkeyBoss 중심)

## 0. 문서 목적
이 문서는 채팅 이관용 기술 인수인계 문서다.
다른 에이전트가 바로 이어서 작업할 수 있도록, "무엇을 왜 바꿨는지", "현재 어떤 상태인지", "어디를 확인하면 되는지"를 모듈 단위로 정리한다.

## 1. 전체 변경 요약

### 1-1. 보스/분리체 물리 이동
- `ThreeMonkeyBoss`, `MonkeyPart` 이동을 Cast + MovePosition 기반으로 통일.
- 반사 누락 대응을 위해 `OnCollisionEnter2D`/`OnCollisionStay2D` fallback 반사 추가.
- 거리 0 접촉도 유효 히트로 허용(`hit.distance >= 0f`).

### 1-2. 쫄끼리 접촉 시 멈춤 완화
- 접촉 유지 프레임에서 stop-window를 반복 갱신하지 않도록 완화.
- Cast 루프에서 soft-body 계열(`Boss`, `Creatuer`, `Creature`) 충돌 시
  - 미세 분리 벡터를 적용하고
  - 잔여 이동을 일부 유지하도록 변경.

### 1-3. 분리 소환 안정화
- 분리 소환 위치를 본체 중앙 고정에서 "분리 부위 기준 + 방향 오프셋"으로 변경.
- 분리 직후 벽 겹침이 있으면 overlap resolve로 밀어내기 보정.
- 분리 시 하위층 재배치 연출(1초 정지/이동) 정리.

### 1-4. 플레이어 피격/입원숭이 디버프
- 접촉 피격 게이트: 즉시 1회 + 피격 무적(0.5초) + 공격자별 틱 간격(0.2초).
- 피격 무적 가시화를 위해 깜박임(알파 토글) 추가.
- 입원숭이 디버프(사격불가) 적용 시 이미 누른 공격 입력도 강제 해제하도록 보강.

## 2. 모듈별 상세

### 모듈 A: 보스 공통 베이스
대상 파일
- `Assets/Script/Monster/Boss/BossBase.cs`

핵심 변경
- `TryGetPlayerStatFromCollision(...)`로 플레이어 탐색 경로 확장.
  - `collision.gameObject`
  - `collision.collider.GetComponentInParent`
  - `collision.rigidbody.GetComponentInParent`

의도
- 플레이어 콜라이더가 자식에 있을 때 접촉 데미지/효과 누락 방지.

주의
- `forceKinematicBody2D`가 true면 런타임에 Kinematic 강제.
- 프리팹에서 Dynamic으로 맞춰도 베이스 설정이 덮을 수 있으니 확인 필요.

### 모듈 B: ThreeMonkeyBoss 본체
대상 파일
- `Assets/Script/Monster/Boss/ThreeMonkeyBoss/ThreeMonkeyBoss.cs`

핵심 변경
- Cast + MovePosition 이동 유지.
- fallback 반사에서 Enter/Stay 처리 분리.
  - Enter: stop-window 적용
  - Stay: stop-window 미적용
- soft-body 충돌 시 미세 분리 + 잔여 이동 유지.
- 분리 소환 안정화:
  - `ResolveDetachSpawnPosition(...)`
  - `ResolveDetachSpawnOverlap(...)`
- 본체 사망 시 분리체를 강제 제거하지 않도록 변경(분리체 독립 유지).
- 2차 분리체 효과 타입 지정 버그 수정:
  - `MonkeyEffectType.Ear` -> `MonkeyEffectType.Mouth`

인스펙터 튜닝 포인트
- `detachSpawnOutwardOffset`
- `detachSpawnResolvePadding`
- `detachSpawnResolveIterations`

### 모듈 C: MonkeyPart 분리체
대상 파일
- `Assets/Script/Monster/Boss/ThreeMonkeyBoss/MonkeyPart.cs`

핵심 변경
- Init 시 `EnsureMinionLayer()`로 분리체/자식 레이어를 `Creatuer`로 강제.
- 플레이어 효과 적용 시 부모 경로 탐색 포함(`ResolvePlayerStat`).
- 반사/이동 동작은 본체와 동일 정책으로 정렬.
- 현재 `collisionEffectDuration` 기본값은 `3f`.

주의
- 분리체 프리팹의 레이어가 맞아도 런타임 변형/자식 구조로 흔들릴 수 있어, Init 강제를 유지하는 것이 안전.

### 모듈 D: 플레이어 상태/피격
대상 파일
- `Assets/Script/PlayerStatScript/PlayerStatControl.cs`

핵심 변경
- `defense` 기본값을 `1f`로 보정(0으로 인한 무데미지 방지).
- `TryApplyContactDamage(...)` 추가:
  - 피격 무적 시간 체크
  - 공격자별 next tick 시간 체크
- 피격 무적 시 깜박임 코루틴 추가.

중요 필드
- `contactHitInvincibilityDuration = 0.5f`
- `contactHitTickInterval = 0.2f`
- `hitInvincibleBlinkInterval = 0.08f`
- `hitInvincibleBlinkAlpha = 0.35f`

### 모듈 E: 입원숭이 사격불가 디버프
대상 파일
- `Assets/Script/PlayerControl/PlayerBossStatusEffectReceiver.cs`
- `Assets/Script/PlayerControl/TopDownCharacterController.cs`

핵심 원리
- Mouth 효과 시 `PushExternalFireBlock()`으로 외부 발사 차단 카운트 증가.
- 발사 조건에서 `!IsExternalFireBlocked`를 체크.
- 이미 마우스 홀드 중이던 케이스 대응:
  - `ForceStopAttackInput()` 추가
  - Mouth 디버프 시작 시 1회 강제 호출해 현재 발사 루프 즉시 종료.

## 3. 현재 알려진 이슈/리스크

### 3-1. 코너/다중 콜라이더 경계 반사 품질
- 드물게 법선이 불안정하면 과반사/약반사 체감 가능.

### 3-2. 분리 소환 직후 밀어내기 보정 한계
- 매우 복잡한 지형(좁은 코너 + 다중 겹침)에서는 resolve 반복 횟수 상향이 필요할 수 있음.

### 3-3. 효과 중첩 체감
- Mouth 효과는 스택 구조라 연속 접촉 시 체감 시간이 늘어날 수 있음.

## 4. 빠른 점검 체크리스트

### 체크 A: 입원숭이 사격불가
1. 공격 버튼 홀드 상태에서 Mouth 분리체와 충돌.
2. 즉시 발사 중단되는지 확인.
3. `collisionEffectDuration` 동안 재발사가 막히는지 확인.

### 체크 B: 쫄끼리 충돌 멈춤
1. 분리체 2개를 충돌시키고 장시간 관찰.
2. 완전 정지(stuck) 빈도 감소 여부 확인.

### 체크 C: 벽 끼임 소환
1. 본체를 벽에 밀착시킨 상태에서 분리 트리거.
2. 분리체가 벽 안에 고정되지 않고 이탈 가능한지 확인.

### 체크 D: 본체 사망 후 분리체 독립성
1. 본체만 먼저 처치.
2. 분리체가 동반 사망하지 않는지 확인.

## 5. 다음 에이전트 작업 가이드

### 우선순위 1
- 플레이 재현에서 여전히 간헐 정지가 보이면,
  - soft-body 분리량
  - stop-window
  - max bounce
  를 우선 튜닝.

### 우선순위 2
- Mouth 디버프 체감(너무 김/짧음) 조정:
  - `collisionEffectDuration` 프리팹/스크립트 값 조정.

### 우선순위 3
- 디버그 로그 최소화 정리:
  - `enableMoveDebug` 운영 빌드 비활성.

## 6. 참고 파일 목록
- `Assets/Script/Monster/Boss/BossBase.cs`
- `Assets/Script/Monster/Boss/ThreeMonkeyBoss/ThreeMonkeyBoss.cs`
- `Assets/Script/Monster/Boss/ThreeMonkeyBoss/MonkeyPart.cs`
- `Assets/Script/PlayerStatScript/PlayerStatControl.cs`
- `Assets/Script/PlayerControl/PlayerBossStatusEffectReceiver.cs`
- `Assets/Script/PlayerControl/TopDownCharacterController.cs`
- `Assets/Script/PlayerControl/CoolTimeController.cs`

## 7. 한 줄 결론
현재 상태는 "관통/반사/분리/사격불가" 핵심 버그를 대부분 봉합한 단계이며,
남은 작업은 물리 경계 상황의 감각 튜닝(파라미터 조정) 위주다.
