using System.Collections;
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
    [Tooltip("스테이지별 탐사 BGM. 0=1-1, 1=1-2, 2=1-3 (stage_data.json bgmIndex와 맞출 것)")]
    public AudioClip[] stageBgmClips;

    [Header("SFX")]
    public AudioClip buttonClickClip;
    public AudioClip clearClip;
    public AudioClip gameOverClip;
    public AudioClip neutralizationClip;
    [Tooltip("가스 밸브 잠금 (squeakyValveSFX)")]
    public AudioClip squeakyValveClip;
    [Tooltip("스플래시 로고 (splashSFX)")]
    public AudioClip splashClip;

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
    [Tooltip("타이틀·게임 BGM 페이드 인/아웃 시간(초)")]
    public float bgmFadeDuration = 0.8f;
    [Range(0f, 1f)]
    public float pauseBgmVolumeRatio = 0.333f;
    [Tooltip("PlayOneShot 배율. 1보다 크게 올릴 수 있습니다.")]
    [Range(0f, 3f)]
    public float sfxVolume = 1f;
    [Tooltip("A~C 중화 루프 SFX 볼륨 (버튼·클리어 sfxVolume과 별도).")]
    [Range(0f, 3f)]
    public float neutralizationSfxVolume = 0.2f;
    [Tooltip("가스 밸브(squeakyValve) 루프 SFX 볼륨 — 중화음과 따로 조절.")]
    [Range(0f, 3f)]
    public float valveSfxVolume = 0.85f;

    private bool bgmPauseDimmed = false;
    private Coroutine bgmFadeRoutine;

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
            Instance.CopyClipsIfEmpty(titleBGM, gameBGM, buttonClickClip, clearClip, gameOverClip, neutralizationClip, squeakyValveClip, splashClip, stageBgmClips);
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

    public void PlaySplashSfx()
    {
        if (splashClip == null)
        {
            Debug.LogWarning("[AudioManager] splashClip이 비어 있습니다. Inspector에 splashSFX를 연결하세요.");
            return;
        }

        if (sfxSource == null)
            SetupSfxSource();

        sfxSource.Stop();
        sfxSource.clip = splashClip;
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
        sfxSource.Play();
    }

    public void StopSplashSfx()
    {
        if (sfxSource == null || splashClip == null)
            return;
        if (!sfxSource.isPlaying || sfxSource.clip != splashClip)
            return;

        sfxSource.Stop();
        sfxSource.clip = null;
    }

    public void PlayNeutralizationSfx()
    {
        PlayLoopingContactSfx(neutralizationClip, neutralizationSfxVolume);
    }

    public void StopNeutralizationSfx()
    {
        StopLoopingContactSfx();
    }

    public void PlayValveSfx()
    {
        if (squeakyValveClip == null)
        {
            Debug.LogWarning("[AudioManager] squeakyValveClip이 비어 있습니다. Inspector에 squeakyValveSFX를 연결하세요.");
            return;
        }

        PlayLoopingContactSfx(squeakyValveClip, valveSfxVolume);
    }

    public void StopValveSfx()
    {
        StopLoopingContactSfx();
    }

    void PlayLoopingContactSfx(AudioClip clip, float loopVolume)
    {
        if (clip == null)
            return;

        if (neutralizationSource == null)
            SetupNeutralizationSource();

        if (neutralizationSource.isPlaying && neutralizationSource.clip == clip)
            return;

        neutralizationSource.Stop();
        neutralizationSource.clip = clip;
        neutralizationSource.loop = true;
        neutralizationSource.spatialBlend = 0f;
        neutralizationSource.volume = loopVolume;
        neutralizationSource.Play();
    }

    void StopLoopingContactSfx()
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
            return;
        else
            StopBGM();
    }

    public void PlayTitleBGM()
    {
        PlayBGM(titleBGM);
    }

    public void PlayGameBGM()
    {
        PlayStageBgm(0);
    }

    public void PlayStageBgm(int index)
    {
        AudioClip clip = null;
        if (stageBgmClips != null && index >= 0 && index < stageBgmClips.Length)
            clip = stageBgmClips[index];
        if (clip == null)
            clip = gameBGM;
        PlayBGM(clip);
    }

    public void StopBGM()
    {
        if (bgmSource == null)
            SetupAudioSource();
        CancelBgmFade();
        bgmFadeRoutine = StartCoroutine(FadeOutAndStopBgm());
    }

    public void SetBgmPauseDim(bool dimmed)
    {
        bgmPauseDimmed = dimmed;
        ApplyBgmVolume();
    }

    float GetTargetBgmVolume()
    {
        float volume = bgmVolume;
        if (bgmPauseDimmed)
            volume *= pauseBgmVolumeRatio;
        return volume;
    }

    void ApplyBgmVolume()
    {
        if (bgmSource == null)
            return;

        bgmSource.volume = GetTargetBgmVolume();
    }

    void CancelBgmFade()
    {
        if (bgmFadeRoutine == null)
            return;

        StopCoroutine(bgmFadeRoutine);
        bgmFadeRoutine = null;
    }

    void CopyClipsIfEmpty(AudioClip title, AudioClip game, AudioClip buttonClick, AudioClip clear, AudioClip gameOver, AudioClip neutralization, AudioClip valve, AudioClip splash, AudioClip[] stageBgms)
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
        if (squeakyValveClip == null && valve != null)
            squeakyValveClip = valve;
        if (splashClip == null && splash != null)
            splashClip = splash;
        CopyStageBgmClipsIfNeeded(stageBgms);
    }

    void CopyStageBgmClipsIfNeeded(AudioClip[] from)
    {
        if (from == null || from.Length == 0)
            return;

        if (stageBgmClips == null || stageBgmClips.Length == 0)
        {
            stageBgmClips = from;
            return;
        }

        if (from.Length > stageBgmClips.Length)
        {
            stageBgmClips = from;
            return;
        }

        for (int i = 0; i < stageBgmClips.Length; i++)
        {
            if (stageBgmClips[i] != null)
                continue;
            if (i < from.Length && from[i] != null)
                stageBgmClips[i] = from[i];
        }
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

        CancelBgmFade();
        bgmFadeRoutine = StartCoroutine(FadeInBgm(clip));
    }

    private IEnumerator FadeOutAndStopBgm()
    {
        yield return FadeOutBgmVolume();
        bgmFadeRoutine = null;
    }

    private IEnumerator FadeOutBgmVolume()
    {
        if (bgmSource == null || !bgmSource.isPlaying)
        {
            if (bgmSource != null)
                bgmSource.Stop();
            yield break;
        }

        float startVolume = bgmSource.volume;
        float duration = Mathf.Max(0f, bgmFadeDuration);
        if (duration <= 0f)
        {
            bgmSource.Stop();
            yield break;
        }

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        bgmSource.Stop();
    }

    private IEnumerator FadeInBgm(AudioClip clip)
    {
        if (bgmSource.isPlaying)
            yield return FadeOutBgmVolume();

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = 0f;
        bgmSource.Play();

        float targetVolume = GetTargetBgmVolume();
        float duration = Mathf.Max(0f, bgmFadeDuration);
        if (duration <= 0f)
        {
            bgmSource.volume = targetVolume;
            bgmFadeRoutine = null;
            Debug.Log($"[AudioManager] 재생: {clip.name}");
            yield break;
        }

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        bgmSource.volume = targetVolume;
        bgmFadeRoutine = null;
        Debug.Log($"[AudioManager] 재생: {clip.name}");
    }
}
