using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared direction minigame (escape, rescue, pickup race, cage unlock, head-ride shake-off, walkie-talkie
// call-in). One per round HUD, local only. A system calls Begin(title, length, onSuccess); the
// player enters the shown directions in order; a wrong one flashes red and starts a NEW random
// sequence. Finishing calls onSuccess. Cancel() stops it (e.g. moved out of reach, got hit).
// Input is the arrow keys, or mouse flicks for the head-ride shake-off (WASD stays free there): look the
// shown way, then back to center before the next arrow counts.
// Code entry (BeginCode): several fixed codes are shown as rows; typed arrows light up on every row that
// still matches and the others dim. No row matching = flash and start over; a full match calls back
// with that row.
public class DirectionMinigame : MonoBehaviour {

    public const int MaxLength = 8;
    public const int MaxCodeRows = 4;

    public enum InputMode {
        Arrows,
        MouseFlick
    }

    [SerializeField]
    private GameObject root;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private Color pendingColor = new Color(0f, 0f, 0f, 0.55f);

    [SerializeField]
    private Color currentColor = new Color(1f, 0.8f, 0.3f, 0.95f);

    [SerializeField]
    private Color doneColor = new Color(0.35f, 0.8f, 0.35f, 0.9f);

    [SerializeField]
    private Color wrongColor = new Color(0.9f, 0.25f, 0.2f, 0.95f);

    [SerializeField]
    private float wrongFlashSeconds = 0.25f;

    [Header("Mouse flicks")]
    // How far (mouse pixels) the look has to move from center, at any speed, to count as a direction.
    [SerializeField, Min(10f)]
    private float flickDistance = 150f;

    // After each direction the look has to come back within this distance of center before the next one
    // counts ("look left, back to center, next").
    [SerializeField, Min(1f)]
    private float flickCenterGrace = 50f;

    // The tracked offset never goes further than this, so overshooting doesn't need a long way back.
    [SerializeField, Min(10f)]
    private float flickMaxOffset = 300f;

    [SerializeField]
    private string backToCenterTitle = "Back to center...";

    [Header("Code entry")]
    [SerializeField]
    private float codeCellSize = 44f;

    [SerializeField]
    private float codeLabelWidth = 150f;

    // Code panel centre, relative to the screen centre.
    [SerializeField]
    private Vector2 codePanelOffset = new Vector2(0f, -190f);

    [SerializeField]
    private Color dimColor = new Color(0f, 0f, 0f, 0.2f);

    [Header("HUD parts")]
    // The arrow row: MaxLength cells, each with an arrow image.
    [SerializeField]
    private Image[] cells;

    [SerializeField]
    private RawImage[] arrows;

    // Code panel: title, one label per row and MaxCodeRows x MaxLength cells, row by row
    // (index = row * MaxLength + key).
    [SerializeField]
    private RectTransform codePanel;

    [SerializeField]
    private TMP_Text codeTitle;

    [SerializeField]
    private TMP_Text[] codeLabels;

    [SerializeField]
    private Image[] codeCells;

    [SerializeField]
    private RawImage[] codeArrows;

    private readonly int[] sequence = new int[MaxLength];
    private InputMode mode;

    // Mouse flicks: look offset from center, and whether it has come back to center since the last one.
    private Vector2 flickOffset;
    private bool flickCentered = true;
    private string baseTitle;

    // Code entry.
    private bool codeMode;
    private int codeRows;
    private readonly int[][] codes = new int[MaxCodeRows][];
    private readonly bool[] rowAlive = new bool[MaxCodeRows];
    private Action<int> onCodeMatch;
    private int length;
    private int progress;
    private bool active;
    private float wrongUntil;
    private Action onSuccess;
    private Action onCancel;

    public static DirectionMinigame Instance { get; private set; }

    public bool IsActive => active;
    public int Progress => progress;
    public int Length => length;

    // Direction of key i in the current sequence: 0 Up, 1 Right, 2 Down, 3 Left.
    public int SequenceAt(int index) => sequence[index];

    private void Awake() {
        Instance = this;
        root.SetActive(false);
        codePanel.gameObject.SetActive(false);
    }

    private void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
    }

    public InputMode Mode => mode;

    // A code entry (walkie-talkie call) is running.
    public bool IsCodeEntry => active && codeMode;

    // Code row r, key i (tests): 0 Up, 1 Right, 2 Down, 3 Left.
    public int CodeAt(int row, int index) => codes[row][index];

    // Starts (or restarts) a minigame. onCancel fires if it is cancelled before finishing.
    public void Begin(string title, int keys, Action success, Action cancelled = null,
        InputMode inputMode = InputMode.Arrows) {
        HideCodePanel();
        length = Mathf.Clamp(keys, 1, MaxLength);
        baseTitle = title;
        titleText.text = title;
        onSuccess = success;
        onCancel = cancelled;
        mode = inputMode;
        flickOffset = Vector2.zero;
        flickCentered = true;
        active = true;
        NewSequence();
        root.SetActive(true);
    }

    // Starts a code entry with up to MaxCodeRows fixed codes (arrow keys). matched gets the row index.
    public void BeginCode(string title, IReadOnlyList<int[]> rowCodes, IReadOnlyList<string> rowLabels,
        IReadOnlyList<Color> rowColors, Action<int> matched, Action cancelled = null) {
        root.SetActive(false);
        codeRows = Mathf.Min(rowCodes.Count, MaxCodeRows);
        for (int r = 0; r < codeRows; r++) {
            codes[r] = rowCodes[r];
            codeLabels[r].text = rowLabels[r];
            codeLabels[r].color = rowColors[r];
        }
        codeTitle.text = title;
        onCodeMatch = matched;
        onSuccess = null;
        onCancel = cancelled;
        mode = InputMode.Arrows;
        codeMode = true;
        active = true;
        ResetCodeRows();
        LayoutCodePanel();
        codePanel.gameObject.SetActive(true);
        RedrawCode();
    }

    public void Cancel() {
        if (!active) {
            return;
        }
        active = false;
        root.SetActive(false);
        HideCodePanel();
        var cancelled = onCancel;
        onSuccess = null;
        onCancel = null;
        onCodeMatch = null;
        cancelled?.Invoke();
    }

    // One arrow press (also used by tests).
    public void Submit(int direction) {
        if (!active || Time.time < wrongUntil) {
            return;
        }
        if (codeMode) {
            SubmitCode(direction);
            return;
        }
        if (direction == sequence[progress]) {
            progress++;
            if (progress >= length) {
                active = false;
                root.SetActive(false);
                var success = onSuccess;
                onSuccess = null;
                onCancel = null;
                success?.Invoke();
            }
            return;
        }
        wrongUntil = Time.time + wrongFlashSeconds;
        NewSequence();
    }

    private void SubmitCode(int direction) {
        var anyMatch = false;
        for (int r = 0; r < codeRows; r++) {
            if (!rowAlive[r]) {
                continue;
            }
            if (progress < codes[r].Length && codes[r][progress] == direction) {
                anyMatch = true;
            } else {
                rowAlive[r] = false;
            }
        }
        if (!anyMatch) {
            wrongUntil = Time.time + wrongFlashSeconds;
            ResetCodeRows();
            return;
        }
        progress++;
        for (int r = 0; r < codeRows; r++) {
            if (rowAlive[r] && progress >= codes[r].Length) {
                active = false;
                HideCodePanel();
                var matched = onCodeMatch;
                onCodeMatch = null;
                onCancel = null;
                matched?.Invoke(r);
                return;
            }
        }
    }

    private void ResetCodeRows() {
        progress = 0;
        for (int r = 0; r < MaxCodeRows; r++) {
            rowAlive[r] = r < codeRows;
        }
    }

    private void HideCodePanel() {
        codeMode = false;
        if (codePanel) {
            codePanel.gameObject.SetActive(false);
        }
    }

    private void Update() {
        if (!active) {
            return;
        }
        var local = PlayerNetworkController.Local;
        if (!local || local.gameObject.scene != gameObject.scene) {
            Cancel();
            return;
        }
        if (!EscMenu.IsOpen && local.TryGetComponent(out PlayerInputReader input)) {
            var direction = mode == InputMode.MouseFlick ? ReadFlick(input.Look) : input.DirectionPressed;
            if (direction >= 0) {
                Submit(direction);
            }
        }
        if (codeMode) {
            RedrawCode();
        } else {
            Redraw();
        }
    }

    // Tracks how far the look has moved from center. Once it is flickDistance out, returns its main
    // direction (0 Up, 1 Right, 2 Down, 3 Left); after that nothing counts until the look is back within
    // flickCenterGrace of center. -1 while no direction.
    private int ReadFlick(Vector2 delta) {
        flickOffset = Vector2.ClampMagnitude(flickOffset + delta, flickMaxOffset);
        if (!flickCentered) {
            if (flickOffset.magnitude <= flickCenterGrace) {
                flickCentered = true;
            }
            return -1;
        }
        if (flickOffset.magnitude < flickDistance) {
            return -1;
        }
        flickCentered = false;
        if (Mathf.Abs(flickOffset.x) > Mathf.Abs(flickOffset.y)) {
            return flickOffset.x > 0f ? 1 : 3;
        }
        return flickOffset.y > 0f ? 0 : 2;
    }

    // Mouse flicks: the next arrow waits until the look is back at center.
    private bool WaitingForCenter => mode == InputMode.MouseFlick && !flickCentered;

    private void NewSequence() {
        progress = 0;
        for (int i = 0; i < length; i++) {
            sequence[i] = UnityEngine.Random.Range(0, 4);
        }
        Redraw();
    }

    private void Redraw() {
        var flashing = Time.time < wrongUntil;
        // The current arrow stays dim until the look is back at center (mouse flicks).
        var waiting = WaitingForCenter;
        titleText.text = waiting ? backToCenterTitle : baseTitle;
        for (int i = 0; i < MaxLength; i++) {
            var show = i < length;
            cells[i].gameObject.SetActive(show);
            if (!show) {
                continue;
            }
            cells[i].color = flashing ? wrongColor
                : i < progress ? doneColor
                : i == progress && !waiting ? currentColor
                : pendingColor;
            arrows[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f * sequence[i]);
            cells[i].rectTransform.localScale = !flashing && !waiting && i == progress ? Vector3.one * 1.15f : Vector3.one;
        }
    }

    // Rows still matching the typed arrows light up as usual; the others dim.
    private void RedrawCode() {
        var flashing = Time.time < wrongUntil;
        for (int r = 0; r < MaxCodeRows; r++) {
            var showRow = r < codeRows;
            codeLabels[r].gameObject.SetActive(showRow);
            for (int i = 0; i < MaxLength; i++) {
                var show = showRow && i < codes[r].Length;
                codeCells[r * MaxLength + i].gameObject.SetActive(show);
                if (!show) {
                    continue;
                }
                codeCells[r * MaxLength + i].color = flashing ? wrongColor
                    : !rowAlive[r] ? dimColor
                    : i < progress ? doneColor
                    : i == progress ? currentColor
                    : pendingColor;
                codeArrows[r * MaxLength + i].rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f * codes[r][i]);
                codeArrows[r * MaxLength + i].color = rowAlive[r] || flashing ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
            if (showRow) {
                var c = codeLabels[r].color;
                c.a = rowAlive[r] || flashing ? 1f : 0.35f;
                codeLabels[r].color = c;
            }
        }
    }

    // Sizes the panel to the rows in use and places the labels and cells (row by row from the top).
    private void LayoutCodePanel() {
        const float pad = 14f;
        const float gap = 6f;
        const float titleHeight = 46f;
        var rowHeight = codeCellSize + gap;
        var width = pad * 2f + codeLabelWidth + MaxLength * (codeCellSize + gap) - gap;
        codePanel.sizeDelta = new Vector2(width, titleHeight + codeRows * rowHeight + pad - gap);
        codePanel.anchoredPosition = codePanelOffset;
        for (int r = 0; r < MaxCodeRows; r++) {
            var y = -titleHeight - r * rowHeight;
            codeLabels[r].rectTransform.anchoredPosition = new Vector2(pad, y);
            for (int i = 0; i < MaxLength; i++) {
                codeCells[r * MaxLength + i].rectTransform.anchoredPosition =
                    new Vector2(pad + codeLabelWidth + i * (codeCellSize + gap), y);
            }
        }
    }
}
