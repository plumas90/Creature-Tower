using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 충돌로 인한 단기 상태이상(암전/음소거/사격불가)을 처리한다.
/// </summary>
public class PlayerBossStatusEffectReceiver : MonoBehaviour
{
    [Header("Eye Blind Overlay")]
    [SerializeField] private string blindOverlaySortingLayer = "UI";
    [SerializeField] private int blindOverlaySortingOrder = -100;
    [SerializeField] private Color blindOverlayColor = Color.black;

    private PlayerStatControl playerStat;
    private TopDownCharacterController controller;

    private float blindUntil;
    private int muteStack;
    private int noFireStack;
    private Canvas blindCanvas;
    private Image blindImage;
    private bool blindOverlayVisible;

    private void Awake()
    {
        playerStat = GetComponent<PlayerStatControl>();
        controller = GetComponent<TopDownCharacterController>();
    }

    public void ApplyEyeBlind(float seconds)
    {
        if (seconds <= 0f) return;
        blindUntil = Mathf.Max(blindUntil, Time.time + seconds);
        EnsureBlindOverlay();
        SetBlindOverlayVisible(true);
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

    private void Update()
    {
        SetBlindOverlayVisible(Time.time < blindUntil);
    }

    private void OnDisable()
    {
        SetBlindOverlayVisible(false);

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

    private void EnsureBlindOverlay()
    {
        if (blindCanvas != null && blindImage != null)
            return;

        Transform existing = transform.Find("BossBlindOverlayCanvas");
        GameObject overlayGo;
        if (existing != null)
        {
            overlayGo = existing.gameObject;
        }
        else
        {
            overlayGo = new GameObject("BossBlindOverlayCanvas", typeof(RectTransform));
            overlayGo.transform.SetParent(transform, false);
        }

        blindCanvas = overlayGo.GetComponent<Canvas>();
        if (blindCanvas == null)
            blindCanvas = overlayGo.AddComponent<Canvas>();

        blindCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        blindCanvas.overrideSorting = true;
        blindCanvas.sortingLayerName = blindOverlaySortingLayer;
        blindCanvas.sortingOrder = blindOverlaySortingOrder;

        CanvasScaler scaler = overlayGo.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = overlayGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = overlayGo.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            raycaster = overlayGo.AddComponent<GraphicRaycaster>();

        Transform imageTransform = overlayGo.transform.Find("BlindFill");
        GameObject imageGo;
        if (imageTransform != null)
        {
            imageGo = imageTransform.gameObject;
        }
        else
        {
            imageGo = new GameObject("BlindFill", typeof(RectTransform), typeof(Image));
            imageGo.transform.SetParent(overlayGo.transform, false);
        }

        blindImage = imageGo.GetComponent<Image>();
        if (blindImage == null)
            blindImage = imageGo.AddComponent<Image>();
        blindImage.color = blindOverlayColor;
        blindImage.raycastTarget = false;

        RectTransform rt = imageGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void SetBlindOverlayVisible(bool visible)
    {
        if (visible)
            EnsureBlindOverlay();

        if (blindCanvas == null)
            return;

        if (blindOverlayVisible == visible)
            return;

        blindOverlayVisible = visible;
        blindCanvas.enabled = visible;
    }
}
