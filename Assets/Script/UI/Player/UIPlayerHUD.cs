using UnityEngine;

public class UIPlayerHUD : UIMainGame
{
    [SerializeField] private PlayerUiManager playerUiManager;

    public override void Initialize()
    {
        base.Initialize();
        CacheManager();
    }

    private void Awake()
    {
        CacheManager();
    }

    private void CacheManager()
    {
        if (playerUiManager != null)
            return;

        playerUiManager = GetComponent<PlayerUiManager>();
        if (playerUiManager == null)
            playerUiManager = GetComponentInChildren<PlayerUiManager>(true);
    }

    public void SetupData()
    {
        CacheManager();
        if (playerUiManager == null)
        {
            Debug.LogWarning("[UIPlayerHUD] PlayerUiManager component was not found.");
            return;
        }

        playerUiManager.SetupData();
    }

    public void Update()
    {
        // PlayerUiManager handles runtime HUD updates.
    }

    public override void Foo()
    {
        Debug.Log("Foo! " + GetType().Name);
    }
}
