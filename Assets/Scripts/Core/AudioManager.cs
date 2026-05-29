using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM Clips")]
    public AudioClip titleBGM;
    public AudioClip gameBGM;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float bgmVolume = 0.7f;
    public float fadeDuration = 1.0f;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.volume = bgmVolume;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
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
        StopAllCoroutines();
        StartCoroutine(FadeOut(fadeDuration));
    }

    private void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        StopAllCoroutines();
        StartCoroutine(CrossFade(clip));
    }

    private IEnumerator CrossFade(AudioClip newClip)
    {
        if (audioSource.isPlaying)
            yield return StartCoroutine(FadeOut(fadeDuration * 0.5f));

        audioSource.clip = newClip;
        audioSource.Play();
        yield return StartCoroutine(FadeIn(fadeDuration * 0.5f));
    }

    private IEnumerator FadeIn(float duration)
    {
        audioSource.volume = 0f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, bgmVolume, time / duration);
            yield return null;
        }
        audioSource.volume = bgmVolume;
    }

    private IEnumerator FadeOut(float duration)
    {
        float startVolume = audioSource.volume;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            yield return null;
        }
        audioSource.volume = 0f;
        audioSource.Stop();
    }
}
