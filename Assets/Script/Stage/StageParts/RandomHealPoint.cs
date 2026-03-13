using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomHealPoint : MonoBehaviour
{
    public Potion potion;

    private void Awake()
    {
        ResolvePotionIfMissing();
    }

    private void ResolvePotionIfMissing()
    {
        if (potion != null) return;

        // 같은 오브젝트 또는 자식(비활성 포함)에서 자동 탐색
        potion = GetComponent<Potion>();
        if (potion == null)
            potion = GetComponentInChildren<Potion>(true);
    }


    public void MakePotion() 
    {
        ResolvePotionIfMissing();

        if (potion == null)
        {
            Debug.LogWarning($"[RandomHealPoint] Potion reference is null on '{name}'.");
            return;
        }

        potion.Init();
    }
}
