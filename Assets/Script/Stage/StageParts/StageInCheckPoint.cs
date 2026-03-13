using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageInCheckPoint : MonoBehaviour
{
    public Stage stage;
    public SpriteRenderer sprite;

    private void Awake()
    {
        if (stage == null)
            stage = GetComponentInParent<Stage>();

        if (sprite != null)
            sprite.color = new Color(0, 0, 0, 0);

        BindRelaysToChildColliders();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleTriggerEnter(collision);
    }

    public void HandleTriggerEnter(Collider2D collision)
    {
        // 플레이어의 자식 콜라이더가 들어와도 루트에서 PlayerStatControl을 찾는다.
        var playerStatControl = collision.GetComponentInParent<PlayerStatControl>();
        if (playerStatControl != null)
        {
            if (stage != null)
                stage.InCheckClear(playerStatControl.gameObject);
            else
                Debug.LogWarning($"[StageInCheckPoint] Stage reference is null on '{name}'.");
        }
    }

    private void BindRelaysToChildColliders()
    {
        var colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            var col = colliders[i];
            if (col == null) continue;
            if (col.gameObject == gameObject) continue;

            var relay = col.GetComponent<StageInCheckTriggerRelay>();
            if (relay == null)
                relay = col.gameObject.AddComponent<StageInCheckTriggerRelay>();

            relay.target = this;
        }
    }
}
