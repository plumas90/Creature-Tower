using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public enum ClipType
{
    NONE,
    BGM,
    SE
}

// ADDED
public enum BGMList
{
    Ace_Of_Bananas,
    Dragao_Inkomodo,
    Duty_Cycle_GB,
    Strike_Witches_Get_Bitches,
}


public class AudioManager : MonoBehaviour
{
    private AudioSource BGMPlayer;
    private GameObject[] SEPlayer;

    [Header("Data")]
    [SerializeField] private AudioMixer mixer;

    [Header("Setup")]
    [SerializeField] private int SEPlayerSize = 8;
    [SerializeField] private List<AudioClip> SEClips;
    [SerializeField] private List<AudioClip> BGMClips;

    [SerializeField] private Dictionary<string, AudioClip> clipDict;

    [SerializeField] private float minLength;
    [SerializeField] private float maxLength;

    // ADDED
    [Header("AudioLibrary")]
    public AudioLibrary AudioLibrary;

    public AudioMixer Mixer { get { return mixer; } }
    private static AudioManager Instance = null;

    void Awake()
    {
        if (null == Instance)
        {
            //이 클래스 인스턴스가 탄생했을 때 전역변수 instance에 게임매니저 인스턴스가 담겨있지 않다면, 자신을 넣어준다.
            Instance = this;

            //씬 전환이 되더라도 파괴되지 않게 한다.
            //gameObject만으로도 이 스크립트가 컴포넌트로서 붙어있는 Hierarchy상의 게임오브젝트라는 뜻이지만, 
            //나는 헷갈림 방지를 위해 this를 붙여주기도 한다.
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            //만약 씬 이동이 되었는데 그 씬에도 Hierarchy에 GameMgr이 존재할 수도 있다.
            //그럴 경우엔 이전 씬에서 사용하던 인스턴스를 계속 사용해주는 경우가 많은 것 같다.
            //그래서 이미 전역변수인 instance에 인스턴스가 존재한다면 자신(새로운 씬의 GameMgr)을 삭제해준다.
            Destroy(this.gameObject);
        }
    }

    public void Initialize()
    {
        InitializeData();
        InitializeObject();
        
        // ADDED
        AudioLibrary = this.gameObject.GetComponent<AudioLibrary>();

        //gameObject.AddComponent<PhotonView>();
        //photonView.ViewID = 10;
    }

    public void InitializeData()//딕셔너리 데이터 세팅
    {
        clipDict = new Dictionary<string, AudioClip>();

        // BGM
        foreach (var clip in BGMClips)
            clipDict.Add(clip.name, clip);

        // SE
        foreach(var clip in SEClips)
            clipDict.Add(clip.name, clip);
    }

    // AudioManager�� �ڽ����� AudioSource ������Ʈ ������Ʈ �߰�, Mixer ����
    public void InitializeObject()
    {
        GameObject bgmPlayer = new GameObject("BGMPlayer");
        GameObject sePlayer = new GameObject("SEPlayer");

        bgmPlayer.transform.parent = Instance.transform;
        sePlayer.transform.parent = Instance.transform;

        //Add Component to Objects
        BGMPlayer = bgmPlayer.AddComponent<AudioSource>();
        SEPlayer = new GameObject[SEPlayerSize];

        BGMPlayer.outputAudioMixerGroup = mixer.FindMatchingGroups("Master/BGM")[0];

        for(int i=0; i<SEPlayerSize; ++i)
        {
            SEPlayer[i] = new GameObject("se_obj");
            SEPlayer[i].transform.SetParent(sePlayer.transform);

            var source = SEPlayer[i].AddComponent<AudioSource>();
            source.outputAudioMixerGroup = mixer.FindMatchingGroups("Master/SE")[0];
        }
    }

    /// <summary>
    /// <para>BGM ����� Ŭ���� �����մϴ�. Ŭ���� �������� ������ ĳ���մϴ�.</para>
    /// <para>ĳ�� ��δ� Resources/Audio/BGM/... �Դϴ�.</para>
    /// </summary>
    /// <param name="clipName"></param>
    /// <param name="loop"></param>
    static public void PlayBGM(BGMList clipName, float volume = 1f, bool loop = true)
    {
        var clipDict = Instance.clipDict;
        var player = Instance.BGMPlayer;
        var encodedName = EncodeBGMEnum(clipName);
        if (!CheckContainKey(encodedName, ClipType.BGM))
            return;

        player.clip = clipDict[encodedName];
        player.volume = volume;
        player.loop = loop;
        player.Play();
    }

    //static public void PlayBGM(string clipName, float volume=1f, bool loop=true)
    //{
    //    var clipDict = Instance.clipDict;
    //    var player = Instance.BGMPlayer;

    //    if (!CheckContainKey(clipName, ClipType.BGM))
    //        return;

    //    player.clip = clipDict[clipName];
    //    player.volume = volume;
    //    player.loop = loop;
    //    player.Play();
    //}

    /// <summary>
    /// <para>SE ����� Ŭ���� �����մϴ�. Ŭ���� �������� ������ ĳ���մϴ�.</para>
    /// <para>ĳ�� ��δ� Resources/Audio/SE/... �Դϴ�.</para>
    /// </summary>
    /// <param name="clipName"></param>
    static public void PlaySE(string clipName, float volume=1f)
    {
        var clipDict = Instance.clipDict;

        if (!CheckContainKey(clipName, ClipType.SE))
            return;

        foreach (var player in Instance.SEPlayer)
        {
            var source = player.GetComponent<AudioSource>();
            if (!source.isPlaying)
            {
                source.clip = clipDict[clipName];
                source.volume = volume;
                source.loop = false;
                source.gameObject.transform.position = Vector3.zero;
                source.Play();
                return;
            }
        }
    }
    static public void PlaySE(string clipName, Vector3 pos)
    {
        var clipDict = Instance.clipDict;

        if (!CheckContainKey(clipName, ClipType.SE))
            return;

        foreach (var player in Instance.SEPlayer)
        {
            var source = player.GetComponent<AudioSource>();
            if (!source.isPlaying)
            {
                source.clip = clipDict[clipName];
                source.loop = false;

                //플레이어 위치가 기존 코드는 로비시 로비매니저의 플레이어 위치 게임플레이중일땐 게임매니저한테서 플레이어 위치를 받아옴 멀티
                //기능이 없기에 로비매니저는 없음 게임매니저 작성후 위치를 받아올것
                Vector3 vec;
                //if (SceneManager.GetActiveScene().name != "LobbyScene")
                //   vec  = GameManager.Instance.clientPlayer.transform.position - pos;
                //else
                //   vec = LobbyManager.Instance.instantiatedPlayer.transform.position - pos;
                //float volume = Mathf.InverseLerp(Instance.maxLength, Instance.minLength, vec.magnitude);
                //source.volume = volume;

                source.Play();
                return;
            }
        }
    }


    static public void PlayClip(AudioClip clip, float volume = 1f)
    {
        foreach (var player in Instance.SEPlayer)
        {
            var temp = player.GetComponent<AudioSource>();
            if (!temp.isPlaying)
            {
                temp.clip = clip;
                temp.volume = volume;
                temp.loop = false;
                temp.Play();
                return;
            }
        }
    }

    static private bool CheckContainKey(string clipName, ClipType clipType)
    {
        var clipDict = Instance.clipDict;
        if(!clipDict.ContainsKey(clipName))
        {
            Debug.Log(clipName + " is not Contained audioClips.");
            bool result = TryCachingClip(clipName, clipType);

            if (!result)
                return false;
        }
        return true;
    }

    static private bool TryCachingClip(string clipName, ClipType clipType)
    {
        var clipDict = Instance.clipDict;

        if (!clipDict.ContainsKey(clipName))
        {
            var clip = Resources.Load<AudioClip>("Audio/"+clipType.ToString()+"/"+clipName);

            if (clip == null)
            {
                Debug.LogError("Can't find " + clipName);
                return false;
            }

            clipDict.Add(clip.name, clip);
        }
        return true;
    }

    // ADDED
    static private string EncodeBGMEnum(BGMList bgmEnum)
    {
        string bgmName = Enum.GetName(typeof(BGMList), bgmEnum);
        string encodedName = bgmName.Replace('_', ' ');
        return encodedName;
    }
}

