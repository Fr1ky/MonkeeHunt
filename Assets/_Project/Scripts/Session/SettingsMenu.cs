using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Esc menu settings (EscMenuCanvas prefab): the "Settings" button under Resume toggles a panel next to
// the Esc menu with mouse sensitivity, field of view and volume sliders. Changes apply immediately
// (LocalPlayerProfile) and are saved when the panel or the Esc menu closes.
public class SettingsMenu : MonoBehaviour {

    [SerializeField]
    private Button settingsButton;

    [SerializeField]
    private GameObject settingsPanel;

    [SerializeField]
    private Slider sensitivity;

    [SerializeField]
    private Slider fov;

    [SerializeField]
    private Slider volume;

    [SerializeField]
    private TMP_Text sensitivityLabel;

    [SerializeField]
    private TMP_Text fovLabel;

    [SerializeField]
    private TMP_Text volumeLabel;

    private bool dirty;

    private void Awake() {
        settingsButton.onClick.AddListener(Toggle);
        Setup(sensitivity, LocalPlayerProfile.MinSensitivity, LocalPlayerProfile.MaxSensitivity, LocalPlayerProfile.Sensitivity,
            value => LocalPlayerProfile.Sensitivity = value);
        Setup(fov, LocalPlayerProfile.MinFov, LocalPlayerProfile.MaxFov, LocalPlayerProfile.Fov,
            value => LocalPlayerProfile.Fov = Mathf.Round(value));
        Setup(volume, 0f, 1f, LocalPlayerProfile.Volume, value => {
            LocalPlayerProfile.Volume = value;
            AudioListener.volume = value;
        });
        RefreshLabels();
        settingsPanel.SetActive(false);
    }

    private void OnDestroy() {
        settingsButton.onClick.RemoveListener(Toggle);
    }

    private void Update() {
        // The Esc menu closed (Resume, Esc, Leave): close settings too.
        if (settingsPanel.activeSelf && !EscMenu.IsOpen) {
            Close();
        }
    }

    private void OnDisable() {
        SaveIfDirty();
    }

    private void Setup(Slider slider, float min, float max, float value, System.Action<float> changed) {
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v => {
            dirty = true;
            changed(v);
            RefreshLabels();
        });
    }

    private void RefreshLabels() {
        sensitivityLabel.text = $"Mouse sensitivity  {LocalPlayerProfile.Sensitivity:0.00}x";
        fovLabel.text = $"Field of view  {LocalPlayerProfile.Fov:0}°";
        volumeLabel.text = $"Volume  {LocalPlayerProfile.Volume * 100f:0}%";
    }

    private void Toggle() {
        if (settingsPanel.activeSelf) {
            Close();
        } else {
            sensitivity.SetValueWithoutNotify(LocalPlayerProfile.Sensitivity);
            fov.SetValueWithoutNotify(LocalPlayerProfile.Fov);
            volume.SetValueWithoutNotify(LocalPlayerProfile.Volume);
            RefreshLabels();
            settingsPanel.SetActive(true);
        }
    }

    private void Close() {
        settingsPanel.SetActive(false);
        SaveIfDirty();
    }

    private void SaveIfDirty() {
        if (dirty) {
            dirty = false;
            LocalPlayerProfile.Save();
        }
    }
}
