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
    public void RandomSetting()
    {
        int random = Random.Range(0, 101);

        if (random <= 5)
        {
            spriteRenderer.sprite = OnehundredPersentHeal;
            _persent = 1;
        }
        else if(random <= 10) 
        {
            spriteRenderer.sprite = FiveZeroPersentHeal;
            _persent = 0.5f;
        }
        else if(random <= 30) 
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
        if (collision.TryGetComponent(out PlayerStatControl playerStatControl)) 
        {
            float getHp = playerStatControl.HP.total;
            getHp = getHp * _persent;
            playerStatControl.HPadd(getHp);
            this.gameObject.SetActive(false);
        }
        
    }

}
