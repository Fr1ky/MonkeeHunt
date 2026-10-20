using UnityEngine;

// This machine's player choices: name, server address and port, hunter loadout and settings.
// Saved on this PC (PlayerPrefs) and loaded when the game starts.
public static class LocalPlayerProfile {

    public const int MaxNameLength = 20;

    private const string NameKey = "mh.name";
    private const string AddressKey = "mh.address";
    private const string PortKey = "mh.port";
    private const string LoadoutKey = "mh.loadout";
    private const string SensitivityKey = "mh.sensitivity";
    private const string FovKey = "mh.fov";
    private const string VolumeKey = "mh.volume";

    public static string Name = "";

    // Empty / 0 = use the transport's defaults.
    public static string Address = "";
    public static int Port;

    // Hunter loadout from the hub racks (LoadoutCode-packed).
    public static int Loadout = LoadoutCode.Default;

    // Settings (Esc menu): mouse sensitivity multiplier, field of view in degrees, master volume 0-1.
    public const float MinSensitivity = 0.25f;
    public const float MaxSensitivity = 3f;
    public const float MinFov = 60f;
    public const float MaxFov = 100f;
    public static float Sensitivity = 1f;
    public static float Fov = 75f;
    public static float Volume = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Load() {
        Name = PlayerPrefs.GetString(NameKey, "");
        Address = PlayerPrefs.GetString(AddressKey, "");
        Port = PlayerPrefs.GetInt(PortKey, 0);
        var loadout = PlayerPrefs.GetInt(LoadoutKey, LoadoutCode.Default);
        Loadout = LoadoutCode.IsValid(loadout) ? loadout : LoadoutCode.Default;
        Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, 1f), MinSensitivity, MaxSensitivity);
        Fov = Mathf.Clamp(PlayerPrefs.GetFloat(FovKey, 75f), MinFov, MaxFov);
        Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        AudioListener.volume = Volume;
    }

    public static void Save() {
        PlayerPrefs.SetString(NameKey, Name ?? "");
        PlayerPrefs.SetString(AddressKey, Address ?? "");
        PlayerPrefs.SetInt(PortKey, Port);
        PlayerPrefs.SetInt(LoadoutKey, Loadout);
        PlayerPrefs.SetFloat(SensitivityKey, Sensitivity);
        PlayerPrefs.SetFloat(FovKey, Fov);
        PlayerPrefs.SetFloat(VolumeKey, Volume);
        PlayerPrefs.Save();
    }
}
