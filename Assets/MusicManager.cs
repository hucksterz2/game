using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [Header("Музыка этой сцены")]
    public AudioClip musicClip;

    [Header("Базовая громкость (0-1)")]
    [Range(0f, 1f)] public float baseVolume = 0.7f;

    private AudioSource source;

    void Start()
    {
        source = GetComponent<AudioSource>();
        source.clip = musicClip;
        source.loop = true;
        source.playOnAwake = true;
        UpdateVolume();
        if (musicClip != null) source.Play();
    }

    void Update()
    {
        UpdateVolume();
    }

    void UpdateVolume()
    {
        float settingsVol = SettingsManager.MusicVolume;
        source.volume = baseVolume * settingsVol;
    }
}