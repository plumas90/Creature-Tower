using UnityEngine;
 
 /// <summary>
 /// 가장 기본적인 근접 공격형 일반 몬스터.
 /// - 매 프레임 플레이어를 향해 물리 이동한다.
 /// - 플레이어와의 충돌 데미지는 CreatureBase → PlayerStatControl.TryApplyContactDamage()로 위임.
 /// - 애니메이터와 스프라이트 반전은 선택적으로 동작하며 없어도 정상 작동한다.
 /// </summary>
 [RequireComponent(typeof(Rigidbody2D))]
 public class NormalMeleeEnemy : EnemyBase
 {
     private Animator _animator;
     private SpriteRenderer _sr;
     private static readonly int AnimIsWalk = Animator.StringToHash("IsWalking");
 
     [Header("Sprite Animation (Fallback if no Animator)")]
     [SerializeField] private Sprite[] walkSprites;
     [SerializeField] private Sprite idleSprite;
     [SerializeField] private float walkFrameRate = 0.2f;
 
     private int _currentWalkFrame = 0;
     private float _walkAnimTimer = 0f;
     private bool _isWalkingState = false;
 
     protected override void Start()
     {
         base.Start();
         _animator = GetComponentInChildren<Animator>();
         _sr       = GetComponentInChildren<SpriteRenderer>();
 
         if (idleSprite == null && _sr != null)
         {
             idleSprite = _sr.sprite;
         }
     }
 
     // ─── AI ───────────────────────────────────────────────────
     protected override void OnTick()
     {
         if (Player == null)
         {
             ResolvePlayer();
             _rb2d.linearVelocity = Vector2.zero;
             SetWalk(false);
             return;
         }
 
         Vector2 toPlayer = (Player.transform.position - transform.position).normalized;
 
         // Context Steering: 주변 몬스터/벽을 피해 자연스럽게 접근
         Vector2 moveDir = ComputeContextSteering(toPlayer);
 
         _rb2d.linearVelocity = moveDir * speed;
 
         // 좌우 반전은 플레이어 방향 기준 유지 (이동 방향이 꺾여도 플레이어를 바라봄)
         if (_sr != null && Mathf.Abs(toPlayer.x) > 0.01f)
             _sr.flipX = toPlayer.x < 0f;
 
         SetWalk(true);
     }
 
     // ─── 사망 처리 ────────────────────────────────────────────
     protected override void Die()
     {
         _rb2d.linearVelocity = Vector2.zero;
         SetWalk(false);
         base.Die();
     }
 
     // ─── 유틸 ─────────────────────────────────────────────────
     private void SetWalk(bool value)
     {
         _isWalkingState = value;
         if (_animator != null)
         {
             _animator.SetBool(AnimIsWalk, value);
         }
 
         if (_animator == null && _sr != null)
         {
             if (!value && idleSprite != null)
             {
                 _sr.sprite = idleSprite;
             }
         }
     }
 
     protected override void Update()
     {
         base.Update();
 
         // 걷기 애니메이션 처리 (애니메이터가 없고 walkSprites가 지정되어 있을 때)
         if (_animator == null && _isWalkingState && walkSprites != null && walkSprites.Length > 0 && _sr != null)
         {
             _walkAnimTimer += Time.deltaTime;
             if (_walkAnimTimer >= walkFrameRate)
             {
                 _walkAnimTimer = 0f;
                 _currentWalkFrame = (_currentWalkFrame + 1) % walkSprites.Length;
                 _sr.sprite = walkSprites[_currentWalkFrame];
             }
         }
     }
 }
