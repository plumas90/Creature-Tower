using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeyPart : BossBase
{
    [Header("Monkey Identity")]
    public MonkeyEffectType effectType = MonkeyEffectType.Eye;
    public float collisionEffectDuration = 1f;

    Vector2 direction = Vector2.zero;

    public void Init(Vector2 vecter)
    {
        bossCount = 1;
        atk = MainSO.atk;
        maxHp = MainSO.hp;
        curHp = MainSO.hp;
        speed = MainSO.speed;
        live = true;

        if (GameManager.Instance != null)
            Player = GameManager.Instance.playerOBJ;

        direction = vecter.sqrMagnitude > 0.0001f ? vecter.normalized : Vector2.right;
        invincibility = false;
        wait = false;
    }

    public override void BossDie()
    {
        base.BossDie();
        gameObject.SetActive(false);
    }

    public override void Damege(float damege)
    {
        base.Damege(damege);
    }
    public override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);

        if (collision.gameObject.TryGetComponent(out PlayerStatControl playerStat))
        {
            var receiver = playerStat.GetComponent<PlayerBossStatusEffectReceiver>();
            if (receiver == null)
                receiver = playerStat.gameObject.AddComponent<PlayerBossStatusEffectReceiver>();

            receiver.ApplyEffect(effectType, collisionEffectDuration);
        }

        if (IsReflectTargetLayer(collision.gameObject.layer))
            {
            Debug.Log($"�ε��� �̸� {collision.gameObject.layer}");
            Debug.Log($"�ε����� �� ���� {direction}");
            Vector3 normal = collision.contacts[0].normal; // ��������
            direction = Vector3.Reflect(direction, normal).normalized; // �ݻ�
            Debug.Log($"�ε��� �� ���� {direction}");
        }
    }

    public void Update()
    {
        if (!live || wait)
        {

        }
        else
        {
            transform.Translate(direction * speed * Time.deltaTime);
        }
    }

    private bool IsReflectTargetLayer(int layer)
    {
        int wall = LayerMask.NameToLayer("Wall");
        int player = LayerMask.NameToLayer("Player");
        int creatureTypo = LayerMask.NameToLayer("Creatuer");
        int creature = LayerMask.NameToLayer("Creature");
        int enemy = LayerMask.NameToLayer("Enemy");
        int boss = LayerMask.NameToLayer("Boss");

        return layer == wall
            || layer == player
            || layer == enemy
            || layer == boss
            || (creatureTypo >= 0 && layer == creatureTypo)
            || (creature >= 0 && layer == creature);
    }
}
