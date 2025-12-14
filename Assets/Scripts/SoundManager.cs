using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Data")]
    private SoundData _soundData;
    public SoundData Data => _soundData;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _globalSfxSource;

    // [추가됨] 현재 재생 중인, 제어 가능한(Stoppable/3D) SFX 소스들을 관리하는 리스트
    private List<AudioSource> _activeSfxSources = new List<AudioSource>();


    [Header("Volume Settings (0.0 ~ 1.0)")]
    public SoundVolumeData volumeData;
    [Range(0f, 1f)] public static float MasterVolume = 1.0f;
    [Range(0f, 1f)] public static float BgmVolume = 1.0f;
    [Range(0f, 1f)] public static float SfxVolume = 1.0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        MasterVolume = volumeData.masterVolume;
        BgmVolume = volumeData.bgmVolume;
        SfxVolume = volumeData.sfxVolume;

        _soundData = Resources.Load<SoundData>("Sound/SoundData");

        if (_bgmSource == null)
        {
            GameObject bgmObj = new GameObject("Channel_BGM");
            bgmObj.transform.SetParent(this.transform);
            _bgmSource = bgmObj.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;
        }

        if (_globalSfxSource == null)
        {
            GameObject globalObj = new GameObject("Channel_GlobalSFX");
            globalObj.transform.SetParent(this.transform);
            _globalSfxSource = globalObj.AddComponent<AudioSource>();
            _globalSfxSource.spatialBlend = 0f;
        }
    }

    // ====================================================
    // 볼륨 조절
    // ====================================================
    public void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);
        volumeData.masterVolume = MasterVolume;
        UpdateBgmVolume();
        UpdateAllSfxVolume(); // [추가됨] 마스터 볼륨 변경 시 SFX도 갱신
    }

    public void SetBgmVolume(float volume)
    {
        BgmVolume = Mathf.Clamp01(volume);
        volumeData.bgmVolume = BgmVolume;
        UpdateBgmVolume();
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        volumeData.sfxVolume = SfxVolume;
        UpdateAllSfxVolume(); // [추가됨] SFX 볼륨 변경 시 실시간 갱신
    }

    private void UpdateBgmVolume()
    {
        if (_bgmSource != null)
        {
            _bgmSource.volume = MasterVolume * BgmVolume;
        }
    }

    // [추가됨] 현재 떠있는 모든 SFX 오디오 소스의 볼륨을 실시간으로 갱신
    private void UpdateAllSfxVolume()
    {
        // 1. 글로벌 소스 갱신
        if (_globalSfxSource != null)
        {
            _globalSfxSource.volume = MasterVolume * SfxVolume;
        }

        // 2. 개별 생성된 소스들 갱신 (리스트 역순 순회 - 삭제 안전성 위해)
        for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
        {
            if (_activeSfxSources[i] == null)
            {
                // 이미 파괴된 오브젝트는 리스트에서 제거
                _activeSfxSources.RemoveAt(i);
            }
            else
            {
                // 살아있다면 볼륨 재설정
                _activeSfxSources[i].volume = MasterVolume * SfxVolume;
            }
        }
    }

    // ====================================================
    // BGM 관리
    // ====================================================
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        _bgmSource.clip = clip;
        _bgmSource.volume = MasterVolume * BgmVolume;
        _bgmSource.Play();
    }

    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    // ====================================================
    // 1. 일반 2D 효과음
    // ====================================================
    public void PlayGlobalSFX(AudioClip clip)
    {
        if (clip == null) return;

        // PlayOneShot을 쓰더라도 Source 자체의 volume을 업데이트해주면
        // 이후 출력에 영향을 줄 수 있으므로 volume 갱신
        _globalSfxSource.volume = MasterVolume * SfxVolume;

        // OneShot은 Scale을 1.0으로 주어 Source의 볼륨을 그대로 따르게 함
        _globalSfxSource.PlayOneShot(clip, 1.0f);
    }

    public void StopAllGlobalSFX()
    {
        _globalSfxSource.Stop();
    }

    // ====================================================
    // 2. 제어 가능한 2D 효과음
    // ====================================================
    public AudioSource PlayStoppable2DSFX(AudioClip clip, bool loop = false)
    {
        if (clip == null) return null;

        GameObject audioObj = new GameObject("Temp_2D_Stoppable");
        audioObj.transform.SetParent(this.transform);

        AudioSource audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = MasterVolume * SfxVolume;
        audioSource.spatialBlend = 0f;
        audioSource.loop = loop;

        audioSource.Play();

        // [추가됨] 관리 리스트에 등록
        _activeSfxSources.Add(audioSource);

        if (!loop)
        {
            Destroy(audioObj, clip.length);
            // Destroy 되어도 리스트에는 null로 남으므로 UpdateAllSfxVolume에서 청소됨
        }

        return audioSource;
    }

    // ====================================================
    // 3. 3D 효과음
    // ====================================================
    public AudioSource Play3DSFX(AudioClip clip, Vector3 position, float maxDistance = 20.0f, bool loop = false)
    {
        if (clip == null) return null;

        GameObject audioObj = new GameObject("Temp_3DSFX");
        audioObj.transform.position = position;

        AudioSource audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = MasterVolume * SfxVolume;
        audioSource.spatialBlend = 1.0f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 1.0f;
        audioSource.maxDistance = maxDistance;
        audioSource.loop = loop;

        audioSource.Play();

        // [추가됨] 관리 리스트에 등록
        _activeSfxSources.Add(audioSource);

        if (!loop)
        {
            Destroy(audioObj, clip.length);
        }

        return audioSource;
    }

    // ====================================================
    // 공통: 특정 소리 멈추기
    // ====================================================
    public void StopSFX(AudioSource source)
    {
        if (source != null)
        {
            // [추가됨] 리스트에서도 명시적으로 제거 (성능 최적화)
            if (_activeSfxSources.Contains(source))
            {
                _activeSfxSources.Remove(source);
            }

            source.Stop();
            Destroy(source.gameObject);
        }
    }
}