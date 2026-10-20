using System;
using System.Collections.Generic;
using PurrNet;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Host-only match settings panel: enemy type, round time, Start.
// Start writes the settings into MatchSession and loads the round scene for everyone.
public class HostConsoleUI : MonoBehaviour {

    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private TMP_Dropdown enemyDropdown;

    [SerializeField]
    private TMP_Text recommendedText;

    [SerializeField]
    private TMP_InputField roundMinutesField;

    [SerializeField]
    private Button startButton;

    [SerializeField]
    private Button closeButton;

    [SerializeField]
    private TMP_Text hintText;

    [SerializeField]
    private float hintDuration = 2f;

    [SerializeField]
    private string roundSceneName = "Blockout";

    private readonly List<EnemyType> enemyTypes = new();
    private PlayerNetworkController user;
    private float hintTimer;

    private void Awake() {
        enemyDropdown.ClearOptions();
        var labels = new List<string>();
        foreach (EnemyType type in Enum.GetValues(typeof(EnemyType))) {
            enemyTypes.Add(type);
            labels.Add(EnemyTypeInfo.DisplayName(type));
        }
        enemyDropdown.AddOptions(labels);
        roundMinutesField.contentType = TMP_InputField.ContentType.IntegerNumber;
        panel.SetActive(false);
        hintText.gameObject.SetActive(false);
    }

    private void OnEnable() {
        enemyDropdown.onValueChanged.AddListener(OnEnemyChanged);
        startButton.onClick.AddListener(StartRound);
        closeButton.onClick.AddListener(Close);
    }

    private void OnDisable() {
        enemyDropdown.onValueChanged.RemoveListener(OnEnemyChanged);
        startButton.onClick.RemoveListener(StartRound);
        closeButton.onClick.RemoveListener(Close);
        if (panel.activeSelf) {
            ModalUI.NotifyClosed();
        }
    }

    public void Open(PlayerNetworkController player) {
        user = player;
        enemyDropdown.SetValueWithoutNotify(Mathf.Max(0, enemyTypes.IndexOf(MatchSession.EnemyType)));
        roundMinutesField.text = Mathf.RoundToInt(MatchSession.RoundTimeSeconds / 60f).ToString();
        OnEnemyChanged(enemyDropdown.value);
        if (!panel.activeSelf) {
            ModalUI.NotifyOpened();
        }
        panel.SetActive(true);
        user.SetControlLocked(true);
    }

    public void Close() {
        if (panel.activeSelf) {
            ModalUI.NotifyClosed();
        }
        panel.SetActive(false);
        if (user) {
            user.SetControlLocked(false);
            user = null;
        }
    }

    public void ShowHint(string message) {
        hintText.text = message;
        hintText.gameObject.SetActive(true);
        hintTimer = hintDuration;
    }

    private void Update() {
        if (panel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
            Close();
        }
        if (hintTimer > 0f) {
            hintTimer -= Time.deltaTime;
            if (hintTimer <= 0f) {
                hintText.gameObject.SetActive(false);
            }
        }
    }

    private void OnEnemyChanged(int index) {
        EnemyTypeInfo.RecommendedSplit(enemyTypes[index], out var hunters, out var animals);
        recommendedText.text = $"Recommended: {hunters} hunters / {animals} {EnemyTypeInfo.DisplayName(enemyTypes[index]).ToLower()}s";
    }

    private void StartRound() {
        var networkManager = NetworkManager.main;
        if (!networkManager || !networkManager.isServer) {
            return;
        }
        if (!int.TryParse(roundMinutesField.text, out var minutes) || minutes < 1) {
            ShowHint("Round time must be at least 1 minute.");
            return;
        }
        MatchSession.EnemyType = enemyTypes[enemyDropdown.value];
        MatchSession.RoundTimeSeconds = minutes * 60f;
        Close();
        networkManager.sceneModule.LoadSceneAsync(roundSceneName);
    }
}
