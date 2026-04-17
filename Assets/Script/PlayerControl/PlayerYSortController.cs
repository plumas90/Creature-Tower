using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class PlayerYSortController : MonoBehaviour
{
    [SerializeField] private string sortingLayerName = "World_Dynamic";
    [SerializeField] private int ySortBaseOrder = 1500;
    [SerializeField] private int ySortScale = 10;
    [SerializeField] private int ySortOrderOffset = 0;
    [SerializeField] private Transform ySortPivot;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private SpriteRenderer shadowRenderer;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private int shadowOffset = -2;
    [SerializeField] private int weaponBehindOffset = 1;
    [SerializeField] private int weaponFrontOffset = -1;

    private SortingGroup sortingGroup;
    private PlayerAnimatorController animatorController;

    private void Awake()
    {
        sortingGroup = GetComponent<SortingGroup>();
        if (sortingGroup == null)
            sortingGroup = gameObject.AddComponent<SortingGroup>();

        if (!string.IsNullOrEmpty(sortingLayerName))
            sortingGroup.sortingLayerName = sortingLayerName;

        animatorController = GetComponent<PlayerAnimatorController>();
        if (playerRenderer == null && animatorController != null)
            playerRenderer = animatorController.GetPlayerRenderer();
        if (shadowRenderer == null && animatorController != null)
            shadowRenderer = animatorController.GetShadowRenderer();
        if (weaponRenderer == null && animatorController != null)
            weaponRenderer = animatorController.GetWeaponRenderer();
    }

    private void LateUpdate()
    {
        if (animatorController == null)
            animatorController = GetComponent<PlayerAnimatorController>();
        if (playerRenderer == null && animatorController != null)
            playerRenderer = animatorController.GetPlayerRenderer();
        if (shadowRenderer == null && animatorController != null)
            shadowRenderer = animatorController.GetShadowRenderer();
        if (weaponRenderer == null && animatorController != null)
            weaponRenderer = animatorController.GetWeaponRenderer();

        if (sortingGroup == null)
            return;

        Transform pivot = ySortPivot != null
            ? ySortPivot
            : (shadowRenderer != null ? shadowRenderer.transform : transform);
        int rootOrder = ySortBaseOrder - Mathf.RoundToInt(pivot.position.y * ySortScale) + ySortOrderOffset;
        sortingGroup.sortingOrder = rootOrder;

        if (playerRenderer != null)
            playerRenderer.sortingOrder = 0;
        if (shadowRenderer != null)
            shadowRenderer.sortingOrder = shadowOffset;

        if (weaponRenderer == null)
            return;

        bool weaponFront = animatorController != null && animatorController.IsWeaponFront();
        // SortingGroup 내부에서는 상대 오더만 사용해야 한다.
        weaponRenderer.sortingOrder = weaponFront ? weaponFrontOffset : weaponBehindOffset;
    }
}
