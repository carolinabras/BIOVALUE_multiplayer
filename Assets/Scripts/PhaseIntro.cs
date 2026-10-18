using System;
using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main game: a full-screen intro when a phase starts. The background is the phase's drawer gradient
// (instruments purple, action cards orange, collaboration blue); in the middle a big fan of the phase's
// pieces (instrument hexagons with the "i", action card backs with the "A", player avatars) opens like
// the stacks on the screen edge, under it "PHASE 1 OF 3 · Instruments phase" and a short line.
// Plays for a few seconds, a tap skips it. Built by "BioValue/Build Turn Ribbon and GM Panel".
public class PhaseIntro : MonoBehaviour
{
    [Serializable]
    public class Art
    {
        public GameObject root;
        public RectTransform[] pieces;  // back to front
        public float[] angles;
        public Texture background;
        public Color titleColor = Color.black;
        public string eyebrow;
        public string title;
        public string subtitle;
    }

    public GameObject content;
    public CanvasGroup group;
    public RawImage background;
    public CanvasGroup texts;
    public TMP_Text eyebrowText;
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public Art[] arts;                  // instruments, action cards, collaboration

    public float fadeIn = 0.3f;
    public float fanTime = 0.7f;
    public float hold = 2.3f;
    public float fadeOut = 0.4f;
    public float spread = 1.25f;        // the fan opens a little more while it waits, like a hovered stack

    private const string EndGameKey = "endGame";

    private int lastShown = -1;
    private bool subscribed;
    private bool skip;
    private Coroutine playing;

    private void Start()
    {
        if (content) content.SetActive(false);
        TrySubscribe();
    }

    public static PhaseIntro Instance { get; private set; }

    private void Awake() => Instance = this;

    private void Update() => TrySubscribe();

    private void TrySubscribe()
    {
        if (subscribed || GameState.Instance == null) return;
        subscribed = true;
        GameState.Instance.onGamePhaseChanged.AddListener(OnPhase);
        OnPhase(GameState.Instance.GetCurrentGamePhase()); // the phase may already be set
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (subscribed && GameState.Instance != null) GameState.Instance.onGamePhaseChanged.RemoveListener(OnPhase);
    }

    private void OnPhase(GameState.GamePhase phase)
    {
        int index = phase == GameState.GamePhase.InstrumentSelection ? 0
                  : phase == GameState.GamePhase.ActionCardPlay ? 1
                  : phase == GameState.GamePhase.Collaboration ? 2 : -1;
        if (index < 0 || index == lastShown || arts == null || index >= arts.Length) return;
        lastShown = index;
        bool ended = PhotonNetwork.InRoom &&
                     PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(EndGameKey, out var e) && e is bool b && b;
        if (ended || !isActiveAndEnabled) return;
        if (playing != null) StopCoroutine(playing);
        playing = StartCoroutine(Play(arts[index]));
    }

    // Tap anywhere on the intro.
    public void Skip() => skip = true;

    // Plays a phase's intro again, even if it was already shown (debug: "Spawn 20 instruments").
    public void Replay(GameState.GamePhase phase)
    {
        lastShown = -1;
        OnPhase(phase);
    }

    private IEnumerator Play(Art art)
    {
        skip = false;
        content.SetActive(true);
        foreach (var a in arts) if (a.root) a.root.SetActive(a == art);
        if (background) background.texture = art.background;
        if (eyebrowText) eyebrowText.text = art.eyebrow;
        if (titleText) { titleText.text = art.title; titleText.color = art.titleColor; }
        if (subtitleText) subtitleText.text = art.subtitle;
        group.blocksRaycasts = true;
        group.alpha = 0f;
        if (texts) texts.alpha = 0f;
        var textsRT = texts ? (RectTransform)texts.transform : null;
        Vector2 textsHome = textsRT ? textsRT.anchoredPosition : Vector2.zero;
        SetFan(art, 0f, 0.6f, 1f);

        float t = 0f;
        float total = fadeIn + fanTime + hold;
        while (t < total && !skip)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / fadeIn);

            // The fan opens one piece after the other, overshooting a little.
            float open = Mathf.Clamp01((t - fadeIn * 0.5f) / fanTime);
            // then keeps spreading slowly while it waits
            float wait = Mathf.Clamp01((t - fadeIn - fanTime) / hold);
            SetFan(art, open, 0.6f, Mathf.Lerp(1f, spread, Mathf.SmoothStep(0f, 1f, wait)));

            float shown = Mathf.Clamp01((t - fadeIn - 0.15f) / 0.4f);
            if (texts) texts.alpha = shown;
            if (textsRT) textsRT.anchoredPosition = textsHome + new Vector2(0f, -24f * (1f - EaseOut(shown)));
            yield return null;
        }

        SetFan(art, 1f, 1f, spread);
        if (texts) texts.alpha = 1f;
        if (textsRT) textsRT.anchoredPosition = textsHome;

        float from = group.alpha;
        for (float o = 0f; o < 1f; o += Time.unscaledDeltaTime / fadeOut)
        {
            group.alpha = Mathf.Lerp(from, 0f, o);
            if (art.root) art.root.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.12f, EaseOut(o));
            yield return null;
        }
        if (art.root) art.root.transform.localScale = Vector3.one;
        group.alpha = 0f;
        group.blocksRaycasts = false;
        content.SetActive(false);
        playing = null;
    }

    // open 0..1 rotates the pieces out from the middle (each a little after the previous);
    // spreadNow scales the final angles.
    private static void SetFan(Art art, float open, float startScale, float spreadNow)
    {
        if (art.pieces == null) return;
        int n = art.pieces.Length;
        for (int i = 0; i < n; i++)
        {
            var piece = art.pieces[i];
            if (!piece) continue;
            float delay = n > 1 ? i / (float)(n - 1) * 0.25f : 0f;
            float p = Mathf.Clamp01((open - delay) / (1f - 0.25f));
            float e = EaseOutBack(p);
            float angle = (art.angles != null && i < art.angles.Length ? art.angles[i] : 0f) * spreadNow;
            piece.localRotation = Quaternion.Euler(0f, 0f, angle * e);
            piece.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, e);
        }
    }

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        t = Mathf.Clamp01(t);
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
