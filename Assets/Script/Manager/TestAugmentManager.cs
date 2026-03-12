using TMPro;
using UnityEngine;

/// <summary>
/// TestAugmentUIRoot에 부착.
/// Inspector에서 fontAsset을 할당하면 하위 모든 TextMeshProUGUI 및
/// TestAugmentUI가 동적으로 생성하는 행·헤더 텍스트에 자동 적용됩니다.
/// </summary>
public class TestAugmentManager : MonoBehaviour
{
    public static TestAugmentManager Instance;

    [Header("폰트 설정")]
    public TMP_FontAsset fontAsset;   // ← Inspector에서 폰트 할당

    private void Awake()
    {
        Instance = this;
        // 이미 씬에 배치된 하위 TMP(토글 버튼 텍스트 등)에도 폰트 적용
        ApplyFontToChildren();
    }

    /// <summary>현재 하위의 모든 TextMeshProUGUI에 fontAsset 적용</summary>
    public void ApplyFontToChildren()
    {
        if (fontAsset == null) return;
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.font = fontAsset;
    }
}
