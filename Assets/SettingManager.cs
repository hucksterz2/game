using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public static float DialogueSpeed { get; private set; } = 0.03f;
    public static float MusicVolume { get; private set; } = 0.7f;

    public static Resolution[] AvailableResolutions { get; private set; }
    public static int CurrentResolutionIndex { get; private set; }
    public static bool IsFullscreen { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAll();
    }

    public static void LoadAll()
    {
        AvailableResolutions = Screen.resolutions;

        DialogueSpeed = PlayerPrefs.GetFloat("DialogueSpeed", 0.03f);
        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        IsFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

        int savedRes = PlayerPrefs.GetInt("ResolutionIndex", -1);
        if (savedRes >= 0 && savedRes < AvailableResolutions.Length)
            CurrentResolutionIndex = savedRes;
        else
        {
            for (int i = 0; i < AvailableResolutions.Length; i++)
                if (AvailableResolutions[i].width == Screen.width &&
                    AvailableResolutions[i].height == Screen.height)
                { CurrentResolutionIndex = i; break; }
        }

        ApplyResolution();
    }

    public static void SetDialogueSpeed(float val)
    {
        DialogueSpeed = Mathf.Clamp(val, 0.005f, 0.15f);
        PlayerPrefs.SetFloat("DialogueSpeed", DialogueSpeed);
        PlayerPrefs.Save();
    }

    public static void SetMusicVolume(float val)
    {
        MusicVolume = Mathf.Clamp01(val);
        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
        PlayerPrefs.Save();
    }

    public static void SetFullscreen(bool val)
    {
        IsFullscreen = val;
        PlayerPrefs.SetInt("Fullscreen", val ? 1 : 0);
        PlayerPrefs.Save();
        ApplyResolution();
    }

    public static void SetResolution(int index)
    {
        CurrentResolutionIndex = Mathf.Clamp(index, 0, AvailableResolutions.Length - 1);
        PlayerPrefs.SetInt("ResolutionIndex", CurrentResolutionIndex);
        PlayerPrefs.Save();
        ApplyResolution();
    }

    static void ApplyResolution()
    {
        if (AvailableResolutions == null || AvailableResolutions.Length == 0) return;
        Resolution r = AvailableResolutions[CurrentResolutionIndex];
        Screen.SetResolution(r.width, r.height, IsFullscreen);
    }
}