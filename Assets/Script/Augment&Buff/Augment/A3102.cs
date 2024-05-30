using UnityEngine;

public class A3102 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    private void Awake()
    {

            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            controller.OnSkillEvent += SkillHpUp;
    }
    void SkillHpUp()
    {
        playerStat.HP.added += 2f;
    }
}
