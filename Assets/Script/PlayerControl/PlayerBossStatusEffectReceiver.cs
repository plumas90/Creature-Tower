using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 충돌로 인한 단기 상태이상(암전/음소거/사격불가)을 처리한다.
/// </summary>
public class PlayerBossStatusEffectReceiver : MonoBehaviour
{
    private PlayerStatControl playerStat;
    private TopDownCharacterController controller;

    private float blindUntil;
    private int muteStack;
    private int noFireStack;

    private Texture2D blackTex;

    private void Awake()
    {
        playerStat = GetComponent<PlayerStatControl>();
        controller = GetComponent<TopDownCharacterController>();
    }

    public void ApplyEyeBlind(float seconds)
    {
        if (seconds <= 0f) return;
        blindUntil = Mathf.Max(blindUntil, Time.time + seconds);

        if (blackTex == null)
        {
            blackTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            blackTex.SetPixel(0, 0, Color.black);
            blackTex.Apply();
        }
    }

    public void ApplyEarMute(float seconds)
    {
        if (seconds <= 0f) return;
        StartCoroutine(EarMuteRoutine(seconds));
    }

    public void ApplyMouthNoFire(float seconds)
    {
        if (seconds <= 0f) return;
        StartCoroutine(MouthNoFireRoutine(seconds));
    }

    public void ApplyEffect(MonkeyEffectType effectType, float seconds)
    {
        switch (effectType)
        {
            case MonkeyEffectType.Eye:
                ApplyEyeBlind(seconds);
                break;
            case MonkeyEffectType.Ear:
                ApplyEarMute(seconds);
                break;
            case MonkeyEffectType.Mouth:
                ApplyMouthNoFire(seconds);
                break;
        }
    }

    private IEnumerator EarMuteRoutine(float seconds)
    {
        muteStack++;
        AudioManager.SetBGMMuted(true);

        yield return new WaitForSeconds(seconds);

        muteStack = Mathf.Max(0, muteStack - 1);
        if (muteStack == 0)
            AudioManager.SetBGMMuted(false);
    }

    private IEnumerator MouthNoFireRoutine(float seconds)
    {
        bool firstBlock = noFireStack == 0;
        noFireStack++;

        if (firstBlock && controller != null)
            controller.ForceStopAttackInput();

        if (playerStat != null)
            playerStat.PushExternalFireBlock();

        yield return new WaitForSeconds(seconds);

        noFireStack = Mathf.Max(0, noFireStack - 1);
        if (playerStat != null)
            playerStat.PopExternalFireBlock();
    }

    private void OnGUI()
    {
        if (Time.time >= blindUntil) return;
        if (blackTex == null) return;

        Color old = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTex);
        GUI.color = old;
    }

    private void OnDisable()
    {
        if (muteStack > 0)
            AudioManager.SetBGMMuted(false);

        if (playerStat != null)
        {
            while (noFireStack > 0)
            {
                playerStat.PopExternalFireBlock();
                noFireStack--;
            }
        }

        muteStack = 0;
        blindUntil = 0f;
    }
}
