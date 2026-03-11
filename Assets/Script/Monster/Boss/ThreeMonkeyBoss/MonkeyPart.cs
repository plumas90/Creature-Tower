using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeyPart : BossBase
{
    Vector2 direction = Vector2.zero;

    public void Init(Vector2 vecter)
    {
        atk = MainSO.atk;
        maxHp = MainSO.hp;
        curHp = MainSO.hp;
        speed = MainSO.speed;
        live = true;
        Player = GameManager.Instance.playerOBJ;
        direction = -vecter;
        invincibility = true;
    }

    public override void Damege(float damege)
    {
        base.Damege(damege);
    }
    public override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);

        if ((collision.gameObject.layer == LayerMask.NameToLayer("Wall")
            || collision.gameObject.layer == LayerMask.NameToLayer("Player")
            || collision.gameObject.layer == LayerMask.NameToLayer("Creatuer")))
            {
            Debug.Log($"ºÎµúÈù ÀÌ¸§ {collision.gameObject.layer}");
            Debug.Log($"ºÎµúÈ÷±â Àü ¹æÇâ {direction}");
            Vector3 normal = collision.contacts[0].normal; // ¹ý¼±º¤ÅÍ
            direction = Vector3.Reflect(direction, normal).normalized; // ¹Ý»ç
            Debug.Log($"ºÎµúÈù ÈÄ ¹æÇâ {direction}");
        }
    }

    public void Update()
    {
        if (wait)
        {

        }
        else
        {
            transform.Translate(direction * speed * Time.deltaTime);
        }
    }
}
