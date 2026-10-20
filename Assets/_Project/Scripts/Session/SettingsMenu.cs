using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Esc menu settings, built in code: adds a "Settings" button under Resume and a panel next to the Esc
// menu with mouse sensitivity, field of view and volume sliders. Changes apply immediately
// (LocalPlayerProfile) and are saved when the panel or the Esc menu closes.
public class SettingsMenu : MonoBehaviour {

    // The Esc menu's panel (vertical list of buttons) and the button / title to copy the look from.
    [SerializeField]
    private RectTransform escPanel;

    [SerializeField]
    private Button templateButton;

    [SerializeField]
    private TMP_Text templateTitle;

    [SerializeField]
    private Vector2 panelSize = new Vector2(560f, 330f);

    [SerializeField]
    private Color fillColor = new Color(1f, 0.65f, 0.3f, 1f);

    private GameObject settingsPanel;
    private Slider sensitivity;
    private Slider fov;
    private Slider volume;
    private TMP_Text sensitivityLabel;
    private TMP_Text fovLabel;
    private TMP_Text volumeLabel;
    private bool dirty;

    private void Awake() {
        AddSettingsButton();
        BuildPanel();
        settingsPanel.SetActive(false);
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

    private void AddSettingsButton() {
        var button = Instantiate(templateButton, escPanel);
        button.name = "SettingsButton";
        button.transform.SetSiblingIndex(templateButton.transform.GetSiblingIndex() + 1);
        button.onClick.AddListener(Toggle);
        var label = button.GetComponentInChildren<TMP_Text>();
        if (label) {
            label.text = "Settings";
        }
        // Make room in the fixed-size panel for one more button.
        var layout = escPanel.GetComponent<VerticalLayoutGroup>();
        var element = button.GetComponent<LayoutElement>();
        var extra = (element ? element.preferredHeight : 40f) + (layout ? layout.spacing : 0f);
        escPanel.sizeDelta += new Vector2(0f, extra);
    }

    private void BuildPanel() {
        settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform));
        var rt = (RectTransform)settingsPanel.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = panelSize;
        // To the right of the Esc menu.
        rt.anchoredPosition = new Vector2(escPanel.sizeDelta.x * 0.5f + 24f + panelSize.x * 0.5f, 0f);
        var background = settingsPanel.AddComponent<Image>();
        if (escPanel.TryGetComponent(out Image escImage)) {
            background.sprite = escImage.sprite;
            background.type = escImage.type;
            background.color = escImage.color;
        }

        var title = MakeText("Settings", new Vector2(0f, panelSize.y * 0.5f - 40f), 34f);
        title.alignment = TextAlignmentOptions.Center;

        sensitivity = MakeRow(0, out sensitivityLabel, LocalPlayerProfile.MinSensitivity, LocalPlayerProfile.MaxSensitivity,
            LocalPlayerProfile.Sensitivity, value => {
                LocalPlayerProfile.Sensitivity = value;
                RefreshLabels();
            });
        fov = MakeRow(1, out fovLabel, LocalPlayerProfile.MinFov, LocalPlayerProfile.MaxFov, LocalPlayerProfile.Fov,
            value => {
                LocalPlayerProfile.Fov = Mathf.Round(value);
                RefreshLabels();
            });
        volume = MakeRow(2, out volumeLabel, 0f, 1f, LocalPlayerProfile.Volume, value => {
            LocalPlayerProfile.Volume = value;
            AudioListener.volume = value;
            RefreshLabels();
        });
        RefreshLabels();
    }

    private Slider MakeRow(int row, out TMP_Text label, float min, float max, float value,
        UnityEngine.Events.UnityAction<float> changed) {
        var y = panelSize.y * 0.5f - 110f - row * 70f;
        label = MakeText("", new Vector2(-panelSize.x * 0.5f + 40f, y), 24f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.rectTransform.pivot = new Vector2(0f, 0.5f);
        label.rectTransform.sizeDelta = new Vector2(260f, 40f);

        var sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
        var srt = (RectTransform)sliderGo.transform;
        srt.SetParent(settingsPanel.transform, false);
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.sizeDelta = new Vector2(220f, 22f);
        srt.anchoredPosition = new Vector2(panelSize.x * 0.5f - 40f - 110f, y);
        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v => {
            dirty = true;
            changed(v);
        });
        foreach (var image in sliderGo.GetComponentsInChildren<Image>(true)) {
            image.color = image.name == "Fill" ? fillColor
                : image.name == "Handle" ? Color.white
                : new Color(0f, 0f, 0f, 0.6f);
        }
        return slider;
    }

    private TMP_Text MakeText(string text, Vector2 position, float size) {
        var label = Instantiate(templateTitle, settingsPanel.transform);
        if (label.TryGetComponent(out LayoutElement element)) {
            element.ignoreLayout = true;
        }
        var lrt = label.rectTransform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(panelSize.x - 40f, 50f);
        lrt.anchoredPosition = position;
        label.fontSize = size;
        label.text = text;
        return label;
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
