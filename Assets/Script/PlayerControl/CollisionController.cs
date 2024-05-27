using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class CollisionController : MonoBehaviour // 모노비하비어펀에서 그냥으로 변경 버그 가능성 있음
    // 이부분 처리가 여러가지에 영향을 많이 받음 추후에 대대적인 수정을 하는쪽으로 가고 수정을 거의 안함 그래도 읽어보셈
{
    private PlayerStatControl playerStat;
    //private PhotonView PV;
    private int LastHealedViewID;

    // ADDED
    private float healedTotal;
    public float HealedTotal        // 걸려있는 컴포넌트에 따라 이벤트 시작 
    {
        get { return healedTotal; }
        set
        {
            if (value != healedTotal)
            {
                healedTotal = value;
            }
        }
    }

    public event Action<float, int> OnHealedEvent; // 인트값을 포톤뷰아이디로 받음 수정필요

    public bool CanPayBack; // 피격시 일정 데미지 만큼 회복 재능 페이백
    public bool CanSupport; // 버프 가능 여부 체크 싱글로 되면서 없어질듯함

    public CapsuleCollider2D footCollider; // 실제 땅 밟는 부분체크 
    //public Rigidbody2D rigidbody; // 왜 있는지 모르겠음 없애도 될거 같은데 보류
    private void Awake()
    {
        playerStat = GetComponent<PlayerStatControl>();
        //PV = GetComponent<PhotonView>();

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        /*if (collision.gameObject.GetComponent<Bullet>()) //부활 체크 
        {
            if (playerStat.isDie && collision.gameObject.GetComponent<Bullet>().canresurrection && this.gameObject.layer == 12)
            // 부활 시키는 버프타입 멀티라서 싱글 삭제 가능성 높음 죽은상태,총알이 부활이라면 , 레이어에서 한번더 죽음체크(정상처리됬으면 불필요)- 포톤 모션 체크때매필요했었음
            {
                Bullet _bullet = collision.gameObject.GetComponent<Bullet>();
                PhotonView photonView = PhotonView.Find(_bullet.BulletOwner);
                WeaponSystem stat = photonView.gameObject.GetComponent<WeaponSystem>();
                if (stat.canresurrection)
                {
                    playerStat.photonView.RPC("ImLive", RpcTarget.All);
                    int PvNum = _bullet.BulletOwner;

                    playerStat.photonView.RPC("thankyouLife", RpcTarget.All, PvNum);

                    if (MainGameManager.Instance != null)
                    {
                        MainGameManager.Instance.photonView.RPC("RemovePartyDeathCount", RpcTarget.All);
                    }

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.PV.RPC("RemovePartyDeathCount", RpcTarget.All);
                    }
                }
                Destroy(collision.gameObject);
            }
            
        }*/

        /*if (collision.gameObject.layer == LayerMask.NameToLayer("AttackArea")) // 공격 지역형 처리 
        {
            //넉백

            float Boss_Dragon_atk = collision.gameObject.GetComponentInParent<BossAI_Dragon>().bossSO.atk;
            //playerStat.GiveDamege(Boss_Dragon_atk);
        }*/

        if (collision.gameObject.layer == LayerMask.NameToLayer("Bullet")
            && !playerStat.Invincibility
            //&& !playerStat.isDie
            //&& !playerStat.isRegen
            && collision.gameObject.GetComponent<Bullet>().targets.ContainsValue((int)BulletTarget.Player))
            //기존 불렛 타입이 팀킬 기능을 고려해서 컨테인 밸류로 피격 대상을 판정했는데 고치는거 고려 - 적 총알도 같은거 쓰니까 기존거대로 해도 될거 같은데
        {
            //if (PV.IsMine) 포톤뷰 처리 삭제
            //{

                Bullet _bullet = collision.gameObject.GetComponent<Bullet>();

                float damage = _bullet.ATK;
                int targetID = _bullet.BulletOwner;

                if (_bullet.IsDamage) // 데미지 타입 이라면 - 멀티 x되면서 버프타입이 없어진다고 봐야함
                {
                    // 반사 처리
                    //if (playerStat.CanReflect)
                    //{
                    //    playerStat.CallReflectEvent(damage, targetID);
                    //    damage *= (1 - playerStat.ReflectCoeff);
                    //}
                    playerStat.Damage(damage);
                }
                /* 버프타입
                else
                {
                    playerStat._DebuffControl.Init(PlayerDebuffControl.buffName.Heal, 1f);
                    Debug.Log("체력 회복 ");
                    // ADD : 힐량 누적
                    damage = (damage + playerStat.CurHP > playerStat.HP.total) ? playerStat.HP.total - playerStat.CurHP : damage;
                    photonView.RPC("AddHealAmount", RpcTarget.All, damage, _bullet.BulletOwner);
                    PhotonView.Find(_bullet.BulletOwner).RPC("InvokeHealedEvent", RpcTarget.All, damage, _bullet.BulletOwner);

                    playerStat.HPadd(damage);
                    if (_bullet.sniperAtkBuff)
                    {
                        Debuff.Instance.GiveAtkBuff(gameObject);
                    }

                }
                */
            //}
            Destroy(collision.gameObject);
        }
    }


    /*[PunRPC] // 멀티 대상 힐
    private void AddHealAmount(float healedAmount, int viewID)
    {
        HealedTotal += healedAmount;
        LastHealedViewID = viewID;
    }

    //[PunRPC] // 힐 발생시 이벤트 처리 = 멀티
    private void InvokeHealedEvent(float healedAmount, int viewID)
    {
        OnHealedEvent?.Invoke(healedAmount, viewID);
    }
    */

}
