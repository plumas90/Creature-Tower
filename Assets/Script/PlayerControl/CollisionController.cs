using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class CollisionController : MonoBehaviour // �����Ϻ���ݿ��� �׳����� ���� ���� ���ɼ� ����
    // �̺κ� ó���� ���������� ������ ���� ���� ���Ŀ� ������� ������ �ϴ������� ���� ������ ���� ���� �׷��� �о��
{
    private PlayerStatControl playerStat;
    //private PhotonView PV;
    private int LastHealedViewID;

    // ADDED
    private float healedTotal;
    public float HealedTotal        // �ɷ��ִ� ������Ʈ�� ���� �̺�Ʈ ���� 
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

    //public event Action<float, int> OnHealedEvent; // ��Ʈ���� �������̵�� ���� �����ʿ� - �̱۵Ǹ� �� �̺�Ʈ�� �����Ѱ�? ���� ����

    public bool CanPayBack; // �ǰݽ� ���� ������ ��ŭ ȸ�� ��� ���̹�
    public bool CanSupport; // ���� ���� ���� üũ �̱۷� �Ǹ鼭 ����������

    public CapsuleCollider2D footCollider; // ���� �� ��� �κ�üũ 
    //public Rigidbody2D rigidbody; // �� �ִ��� �𸣰��� ���ֵ� �ɰ� ������ ����
    private void Awake()
    {
        playerStat = GetComponent<PlayerStatControl>();
        //PV = GetComponent<PhotonView>();

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        /*if (collision.gameObject.GetComponent<Bullet>()) //��Ȱ üũ 
        {
            if (playerStat.isDie && collision.gameObject.GetComponent<Bullet>().canresurrection && this.gameObject.layer == 12)
            // ��Ȱ ��Ű�� ����Ÿ�� ��Ƽ�� �̱� ���� ���ɼ� ���� ��������,�Ѿ��� ��Ȱ�̶�� , ���̾�� �ѹ��� ����üũ(����ó�������� ���ʿ�)- ���� ��� üũ�����ʿ��߾���
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

        /*if (collision.gameObject.layer == LayerMask.NameToLayer("AttackArea")) // ���� ������ ó�� 
        {
            //�˹�

            float Boss_Dragon_atk = collision.gameObject.GetComponentInParent<BossAI_Dragon>().bossSO.atk;
            //playerStat.GiveDamege(Boss_Dragon_atk);
        }*/

        if (collision.gameObject.layer == LayerMask.NameToLayer("Bullet")
            && !playerStat.Invincibility
            && !playerStat.NormalRollInvincibility
            && !playerStat.SkillRollInvincibility
            //&& !playerStat.isDie
            //&& !playerStat.isRegen
            && collision.gameObject.GetComponent<Bullet>().targets.ContainsValue((int)BulletTarget.Player))
            //���� �ҷ� Ÿ���� ��ų ����� �����ؼ� ������ ����� �ǰ� ����� �����ߴµ� ��ġ�°� ���� - �� �Ѿ˵� ������ ���ϱ� �����Ŵ�� �ص� �ɰ� ������
        {
            //if (PV.IsMine) ����� ó�� ����
            //{

                Bullet _bullet = collision.gameObject.GetComponent<Bullet>();

                float damage = _bullet.ATK;
                int targetID = _bullet.BulletOwner;

                if (_bullet.IsDamage) // ������ Ÿ�� �̶�� - ��Ƽ x�Ǹ鼭 ����Ÿ���� �������ٰ� ������
                {
                    // �ݻ� ó��
                    //if (playerStat.CanReflect)
                    //{
                    //    playerStat.CallReflectEvent(damage, targetID);
                    //    damage *= (1 - playerStat.ReflectCoeff);
                    //}
                    playerStat.Damage(damage);
                }
                /* ����Ÿ��
                else
                {
                    playerStat._DebuffControl.Init(PlayerDebuffControl.buffName.Heal, 1f);
                    Debug.Log("ü�� ȸ�� ");
                    // ADD : ���� ����
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
            Destroy(collision.gameObject);// ���� �Ѿ� punrpc�� ���⼭ �����ε� �̰ŵ� ���� �ؾ��Ұ� ������?
        }
    }


    /*[PunRPC] // ��Ƽ ��� ��
    private void AddHealAmount(float healedAmount, int viewID)
    {
        HealedTotal += healedAmount;
        LastHealedViewID = viewID;
    }

    //[PunRPC] // �� �߻��� �̺�Ʈ ó�� = ��Ƽ
    private void InvokeHealedEvent(float healedAmount, int viewID)
    {
        OnHealedEvent?.Invoke(healedAmount, viewID);
    }
    */

}
