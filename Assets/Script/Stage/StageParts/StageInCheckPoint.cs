using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageInCheckPoint : MonoBehaviour
{
    public Stage stage;
    public SpriteRenderer sprite;
    private void Awake()
    {
        sprite.color = new Color(0, 0, 0, 0);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out PlayerStatControl playerStatControl)) 
        {
            stage.InCheckClear();
        }
    }
}
