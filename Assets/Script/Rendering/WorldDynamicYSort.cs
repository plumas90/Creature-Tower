using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 2.5D 월드 동적 오브젝트용 Y 기반 정렬.
/// SortingGroup의 sortingOrder를 y값으로 갱신해 앞/뒤를 결정한다.
/// </summary>
[DisallowMultipleComponent]
public class WorldDynamicYSort : MonoBehaviour
{
    [SerializeField] private bool useYBasedSorting = true;
    [SerializeField] private string sortingLayerName = "World_Dynamic";
    [SerializeField] private int ySortBaseOrder = 1500;
    [SerializeField] private int ySortScale = 10;
    [SerializeField] private int ySortOrderOffset = 0;
    [SerializeField] private Transform ySortPivot;

    private SortingGroup sortingGroup;

    private void Awake()
    {
        sortingGroup = GetComponent<SortingGroup>();
        if (sortingGroup == null)
            sortingGroup = gameObject.AddComponent<SortingGroup>();

        if (!string.IsNullOrEmpty(sortingLayerName))
            sortingGroup.sortingLayerName = sortingLayerName;
    }

    private void LateUpdate()
    {
        if (!useYBasedSorting || sortingGroup == null)
            return;

        Transform pivot = ySortPivot != null ? ySortPivot : transform;
        int order = ySortBaseOrder - Mathf.RoundToInt(pivot.position.y * ySortScale) + ySortOrderOffset;
        sortingGroup.sortingOrder = order;
    }
}
