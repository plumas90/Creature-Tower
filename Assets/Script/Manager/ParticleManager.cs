using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public enum ParticleType
{
    NONE,
    NORMAL,
    SPECIAL
}

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance;

    [SerializeField] private List<GameObject> prefabs;
    [SerializeField] private Dictionary<string, GameObject> prefabDict;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        prefabDict = new Dictionary<string, GameObject>();

        // ����Ʈ�� �÷��� ����Ʈ Dict�� �߰��Ͽ� ĳ��
        foreach(var prefab in prefabs)
            prefabDict.Add(prefab.name, prefab);
    }

    /// <summary>
    /// <para>��ƼŬ ������Ʈ�� �����մϴ�.</para>
    /// <para>ĳ�� ��δ� Resources/Particle/... �Դϴ�.</para>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="parents"></param>
    static public void PlayEffectLocal(string name, Vector3 pos, Transform parents)
    {
        var tempDict = Instance.prefabDict;

        if (!CheckContainKey(name))
            return;

        GameObject prefab = Instantiate(tempDict[name], parents);
        prefab.transform.position = pos;

        prefab.GetComponent<ParticleSystem>().Play();
    }

    /// <summary>
    /// <para>��ƼŬ ������Ʈ�� �����մϴ�.</para>
    /// <para>ĳ�� ��δ� Resources/Particle/... �Դϴ�.</para>
    /// <para>��� �÷��̾�� ���̴� ��ƼŬ�Դϴ�. </para>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="pos"></param>
    /// <param name="pViewID">�θ� �� ������Ʈ�� ViewID</param>
    public void PlayEffect(string name, Vector3 pos, GameObject parent = null)
    {
        if (!CheckContainKey(name))
            return;

        var tempDict = Instance.prefabDict;
        Transform parentTransform = parent != null ? parent.transform : null;
        GameObject prefab = Instantiate(tempDict[name], pos, Quaternion.identity, parentTransform);
        prefab.GetComponent<ParticleSystem>().Play();
    }

    public void PlayEffect(string name, Vector3 pos)
    {
        if (!CheckContainKey(name))
            return;

        var tempDict = Instance.prefabDict;
        GameObject prefab = Instantiate(tempDict[name], pos, Quaternion.identity);
        prefab.GetComponent<ParticleSystem>().Play();
    }

    static private bool CheckContainKey(string name)
    {
        var tempDict = Instance.prefabDict;
        if (!tempDict.ContainsKey(name))
        {
            Debug.Log(name + " is not Contained audioClips.");
            bool result = TryCachingClip(name);

            if (!result)
                return false;
        }
        return true;
    }

    static private bool TryCachingClip(string name)
    {
        var clipDict = Instance.prefabDict;

        if (!clipDict.ContainsKey(name))
        {
            var clip = Resources.Load<GameObject>("Prefabs/Particle/" + name);

            if (clip == null)
            {
                Debug.LogError("Can't find " + name);
                return false;
            }

            clipDict.Add(clip.name, clip);
        }
        return true;
    }
}
