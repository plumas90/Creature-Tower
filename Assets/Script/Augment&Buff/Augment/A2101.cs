using UnityEngine;

public class A2101 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl playerStat;
    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            playerStat = GetComponent<PlayerStatControl>();
            controller.OnSkillEvent += AtkSpeedUp;
    }
    // Update is called once per frame
    void AtkSpeedUp()
    {
        playerStat.AtkSpeed.added += 0.02f;
    }
}
