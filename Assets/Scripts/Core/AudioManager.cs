using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Scene Names")]
    public string titleSceneName = "TitleScene";
    public string gameSceneName = "GameScene";

    [Header("BGM Clips")]
    public AudioClip titleBGM;
    public AudioClip gameBGM;

    [Header("SFX")]
    public AudioClip buttonClickClip;
    public AudioClip clearClip;
    public AudioClip gameOverClip;
    public AudioClip neutralizationClip;

    [Header("Audio Source")]
    [Tooltip("BGM 전용. 비우면 자동 연결.")]
    public AudioSource bgmSource;
    [Tooltip("SFX 전용(버튼·클리어·게임오버). 비우면 자동 생성.")]
    public AudioSource sfxSource;
    [Tooltip("중화 루프 SFX 전용. sfxSource와 분리해야 패널음 볼륨이 줄지 않습니다.")]
    public AudioSource neutralizationSource;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;
    [Range(0f, 1f)]
    public float pauseBgmVolumeRatio = 0.333f;
    [Tooltip("PlayOneShot 배율. 1보다 크게 올릴 수 있습니다.")]
    [Range(0f, 3f)]
    public float sfxVolume = 1f;
    [Tooltip("중화 SFX만 따로 낮출 때 사용 (버튼·클리어 등 sfxVolume과 별도).")]
    [Range(0f, 3f)]
    public float neutralizationSfxVolume = 0.35f;

    private bool bgmPauseDimmed = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSource();
            SetupSfxSource();
            SetupNeutralizationSource();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            Instance.CopyClipsIfEmpty(titleBGM, gameBGM, buttonClickClip, clearClip, gameOverClip, neutralizationClip);
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (Instance != this)
            return;
        PlayBGMForActiveScene();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void SetupAudioSource()
    {
        if (bgmSource == null)
            bgmSource = GetComponent<AudioSource>();
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        ApplyBgmVolume();
    }

    void SetupSfxSource()
    {
        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = 1f;
            return;
        }

        AudioSource[] sources = GetComponents<AudioSource>();
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != bgmSource)
            {
                sfxSource = sources[i];
                break;
            }
        }

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = 1f;
    }

    void SetupNeutralizationSource()
    {
        if (neutralizationSource != null)
        {
            neutralizationSource.playOnAwake = false;
            return;
        }

        neutralizationSource = gameObject.AddComponent<AudioSource>();
        neutralizationSource.playOnAwake = false;
        neutralizationSource.loop = false;
        neutralizationSource.volume = 1f;
    }

    public void PlayButtonSfx()
    {
        PlaySfx(buttonClickClip, sfxVolume);
    }

    public void PlayClearSfx()
    {
        PlaySfx(clearClip, sfxVolume);
    }

    public void PlayGameOverSfx()
    {
        PlaySfx(gameOverClip, sfxVolume);
    }

    public void PlayNeutralizationSfx()
    {
        if (neutralizationClip == null)
            return;

        if (neutralizationSource == null)
            SetupNeutralizationSource();

        if (neutralizationSource.isPlaying && neutralizationSource.clip == neutralizationClip)
            return;

        neutralizationSource.clip = neutralizationClip;
        neutralizationSource.loop = true;
        neutralizationSource.volume = neutralizationSfxVolume;
        neutralizationSource.Play();
    }

    public void StopNeutralizationSfx()
    {
        if (neutralizationSource == null)
            return;
        if (!neutralizationSource.isPlaying)
            return;

        neutralizationSource.Stop();
        neutralizationSource.loop = false;
        neutralizationSource.clip = null;
    }

    void PlaySfx(AudioClip clip, float oneShotVolume)
    {
        if (clip == null)
            return;

        if (sfxSource == null)
            SetupSfxSource();

        sfxSource.volume = 1f;
        sfxSource.PlayOneShot(clip, oneShotVolume);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this)
            return;
        PlayBGMForScene(scene.name);
    }

    void PlayBGMForActiveScene()
    {
        PlayBGMForScene(SceneManager.GetActiveScene().name);
    }

    void PlayBGMForScene(string sceneName)
    {
        if (sceneName == titleSceneName)
            PlayTitleBGM();
        else if (sceneName == gameSceneName)
            PlayGameBGM();
    }

    public void PlayTitleBGM()
    {
        PlayBGM(titleBGM);
    }

    public void PlayGameBGM()
    {
        PlayBGM(gameBGM);
    }

    public void StopBGM()
    {
        if (bgmSource == null)
            SetupAudioSource();
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void SetBgmPauseDim(bool dimmed)
    {
        bgmPauseDimmed = dimmed;
        ApplyBgmVolume();
    }

    void ApplyBgmVolume()
    {
        if (bgmSource == null)
            return;

        float volume = bgmVolume;
        if (bgmPauseDimmed)
            volume *= pauseBgmVolumeRatio;

        bgmSource.volume = volume;
    }

    void CopyClipsIfEmpty(AudioClip title, AudioClip game, AudioClip buttonClick, AudioClip clear, AudioClip gameOver, AudioClip neutralization)
    {
        if (titleBGM == null && title != null)
            titleBGM = title;
        if (gameBGM == null && game != null)
            gameBGM = game;
        if (buttonClickClip == null && buttonClick != null)
            buttonClickClip = buttonClick;
        if (clearClip == null && clear != null)
            clearClip = clear;
        if (gameOverClip == null && gameOver != null)
            gameOverClip = gameOver;
        if (neutralizationClip == null && neutralization != null)
            neutralizationClip = neutralization;
    }

    void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null)
            SetupAudioSource();

        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] BGM 클립이 없습니다. Title BGM / Game BGM 슬롯을 확인하세요.");
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            ApplyBgmVolume();
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = true;
        ApplyBgmVolume();
        bgmSource.Play();

        Debug.Log($"[AudioManager] 재생: {clip.name}");
    }
}
