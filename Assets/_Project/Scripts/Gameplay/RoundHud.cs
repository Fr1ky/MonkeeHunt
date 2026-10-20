using TMPro;
using UnityEngine;
using UnityEngine.UI;

// In-round HUD (local): timer, players alive per team, own health bar and team label,
// plus the centre overlay for the countdown (3/2/1/GO) and the results.
public class RoundHud : MonoBehaviour {

    [SerializeField]
    private TMP_Text timerText;

    [SerializeField]
    private TMP_Text aliveText;

    [SerializeField]
    private GameObject ownStatusRoot;

    [SerializeField]
    private TMP_Text teamText;

    [SerializeField]
    private Image healthFill;

    [SerializeField]
    private TMP_Text healthText;

    [SerializeField]
    private TMP_Text centerText;

    [SerializeField]
    private float goDisplaySeconds = 1f;

    [SerializeField]
    private Color huntersColor = new Color(1f, 0.65f, 0.3f);

    [SerializeField]
    private Color monkeysColor = new Color(0.8f, 0.55f, 0.3f);

    private RoundController subscribed;
    private float goTimer;

    private void OnDisable() {
        Unsubscribe();
    }

    private void Update() {
        var round = RoundController.Instance;
        if (round != subscribed) {
            Unsubscribe();
            subscribed = round;
            if (subscribed) {
                subscribed.OnStateChanged += OnStateChanged;
            }
        }
        if (!round) {
            timerText.text = "";
            aliveText.text = "";
            centerText.text = "";
            ownStatusRoot.SetActive(false);
            return;
        }

        goTimer -= Time.deltaTime;
        UpdateTimer(round);
        aliveText.text = $"Hunters {round.HuntersAlive}/{round.HuntersTotal}  ·  Monkeys {round.MonkeysAlive}/{round.MonkeysTotal}";
        UpdateCenter(round);
        UpdateOwnStatus();
    }

    private void UpdateTimer(RoundController round) {
        if (round.State != RoundState.Playing) {
            timerText.text = "";
            return;
        }
        var seconds = Mathf.CeilToInt(round.TimeRemaining);
        timerText.text = $"{seconds / 60}:{seconds % 60:00}";
    }

    private void UpdateCenter(RoundController round) {
        switch (round.State) {
            case RoundState.Countdown:
                centerText.text = Mathf.Max(1, Mathf.CeilToInt(round.TimeRemaining)).ToString();
                break;
            case RoundState.Playing:
                centerText.text = goTimer > 0f ? "GO!" : "";
                break;
            case RoundState.Results:
                centerText.text = round.Result switch {
                    RoundResult.HuntersWin => "Hunters win",
                    RoundResult.MonkeysWin => "Monkeys win",
                    RoundResult.Draw => "Draw",
                    _ => ""
                };
                break;
        }
    }

    private void UpdateOwnStatus() {
        var local = PlayerNetworkController.Local;
        var health = local ? local.GetComponent<PlayerHealth>() : null;
        var info = local ? local.GetComponent<PlayerInfo>() : null;
        if (!health || !info || local.gameObject.scene != gameObject.scene) {
            ownStatusRoot.SetActive(false);
            return;
        }
        ownStatusRoot.SetActive(true);
        teamText.text = info.Team == Team.Monkeys ? "MONKEY" : "HUNTER";
        teamText.color = info.Team == Team.Monkeys ? monkeysColor : huntersColor;
        healthFill.fillAmount = health.MaxHealth > 0 ? (float)health.CurrentHealth / health.MaxHealth : 0f;
        healthText.text = $"{health.CurrentHealth} / {health.MaxHealth}";
    }

    private void OnStateChanged(RoundState state) {
        if (state == RoundState.Playing) {
            goTimer = goDisplaySeconds;
        }
    }

    private void Unsubscribe() {
        if (subscribed) {
            subscribed.OnStateChanged -= OnStateChanged;
        }
        subscribed = null;
    }
}
