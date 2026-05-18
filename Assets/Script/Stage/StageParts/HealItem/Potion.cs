using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D;
using UnityEngine;

public class Potion : MonoBehaviour
{
    private float _persent =0;


    public SpriteRenderer spriteRenderer;

    public Sprite TenPerentHeal;
    public Sprite TwoFivePersentHeal;
    public Sprite FiveZeroPersentHeal;
    public Sprite OnehundredPersentHeal;

    public void Init() 
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        RandomSetting();
    }
    public void InitFixed(float percent)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        _persent = percent;
        if (percent >= 1f) spriteRenderer.sprite = OnehundredPersentHeal;
        else if (percent >= 0.5f) spriteRenderer.sprite = FiveZeroPersentHeal;
        else if (percent >= 0.25f) spriteRenderer.sprite = TwoFivePersentHeal;
        else spriteRenderer.sprite = TenPerentHeal;
    }
    public void RandomSetting()
    {
        int random = Random.Range(0, 100);

        if (random < 1)
        {
            spriteRenderer.sprite = OnehundredPersentHeal;
            _persent = 1;
        }
        else if(random < 6) 
        {
            spriteRenderer.sprite = FiveZeroPersentHeal;
            _persent = 0.5f;
        }
        else if(random < 30) 
        {
            spriteRenderer.sprite = TwoFivePersentHeal;
            _persent = 0.25f;
        }
        else
        {
            spriteRenderer.sprite = TenPerentHeal;
            _persent = 0.1f;
        }

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var playerStatControl = collision.GetComponentInParent<PlayerStatControl>();
        if (playerStatControl != null)
        {
            float getHp = playerStatControl.HP.total;
            getHp = getHp * _persent;
            playerStatControl.HPadd(getHp);
            this.gameObject.SetActive(false);
        }
        
    }

}
