using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The Loading scene, between the main menu and the lobby. Same background, hexes and logo as the menu,
// so the scene change can't be seen: the logo glides up from its menu place and the loader fades in.
// Steps follow PhotonConnection: 0 server, 1 lobby, 2 ready. If the connection fails: an error with
// "Try again" / "Back to menu". Built by "BioValue/Build Loading Screen".
public class LoadingScreen : MonoBehaviour
{
    [System.Serializable]
    public class StepView
    {
        public Image line;      // to the step before (none on the first)
        public Image pulse;     // ring growing out of the current step
        public Image dot;
        public GameObject inner; // white centre: current step
        public GameObject tick;  // done
        public GameObject todo;  // outline: not reached yet
        public TMP_Text label;
    }

    public static LoadingScreen Instance { get; private set; }

    private const int MainMenuScene = 0, LobbyScene = 2;
    private const float MinStepTime = 0.7f;  // each step stays long enough to be read
    private const float ReadyHold = 1.2f;    // "Ready!" before the lobby opens
    private const float Timeout = 25f;       // no lobby by then: show the error
    private const float CellCycle = 1.2f;

    private static readonly Color Pink = new Color32(0xFE, 0x49, 0xA4, 0xFF);
    private static readonly Color Faded = new Color(1f, 1f, 1f, 0.5f);
    private static readonly Color LineOff = new Color(1f, 1f, 1f, 0.2f);
    private static readonly Color CellOff = new Color(1f, 1f, 1f, 0.18f);

    private static readonly string[] Status = { "Connecting to the server…", "Joining the lobby…", "Ready!" };
    private static readonly string[] Sub = { "This takes a few seconds.", "Almost there.", "Opening the lobby." };

    public PhotonConnection connection;

    [Header("Logo: from its main menu place to its loading place")]
    public RectTransform logo;
    public CanvasGroup logoGroup;
    public Vector2 logoFromPosition, logoFromSize, logoToPosition, logoToSize;

    [Header("Loader")]
    public CanvasGroup content;
    public Image[] cells;
    public Color[] cellColors;
    public TMP_Text status;
    public TMP_Text sub;
    public GameObject steps;
    public StepView[] stepViews;
    public GameObject errorButtons;
    public GameObject cancelButton;

    private int _target, _shown;
    private float _sinceShown, _elapsed;
    private bool _error, _leaving;

    private void Awake()
    {
        Instance = this;
        if (logo)
        {
            logo.anchoredPosition = logoFromPosition;
            logo.sizeDelta = logoFromSize;
        }
        if (content) content.alpha = 0f;
        Refresh();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        StartCoroutine(Intro());
    }

    // The logo glides up, the loader fades in from a little lower.
    private IEnumerator Intro()
    {
        var contentRect = content ? (RectTransform)content.transform : null;
        Vector2 contentTo = contentRect ? contentRect.anchoredPosition : Vector2.zero;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.7f)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f); // ease out
            if (logo)
            {
                logo.anchoredPosition = Vector2.Lerp(logoFromPosition, logoToPosition, e);
                logo.sizeDelta = Vector2.Lerp(logoFromSize, logoToSize, e);
            }
            float f = Mathf.Clamp01((t - 0.2f) / 0.65f); // starts a little after the logo
            if (content)
            {
                content.alpha = f;
                contentRect.anchoredPosition = contentTo + new Vector2(0f, -16f * (1f - f));
            }
            yield return null;
        }
        if (logo)
        {
            logo.anchoredPosition = logoToPosition;
            logo.sizeDelta = logoToSize;
        }
        if (content)
        {
            content.alpha = 1f;
            contentRect.anchoredPosition = contentTo;
        }
    }

    // Called by PhotonConnection as the connection moves on (0 server, 1 lobby, 2 ready).
    public void SetStep(int step)
    {
        if (_error || _leaving) return;
        _target = Mathf.Max(_target, Mathf.Clamp(step, 0, 2));
    }

    public void ShowError()
    {
        if (_leaving || _error) return;
        _error = true;
        Refresh();
    }

    // "Try again"
    public void Retry()
    {
        if (_leaving) return;
        _error = false;
        _target = _shown = 0;
        _sinceShown = _elapsed = 0f;
        Refresh();
        if (connection) connection.Retry();
    }

    // "Cancel" / "Back to menu"
    public void BackToMenu()
    {
        if (_leaving) return;
        _leaving = true;
        StartCoroutine(LeaveToMenu());
    }

    private IEnumerator LeaveToMenu()
    {
        if (PhotonNetwork.NetworkClientState != ClientState.Disconnected) PhotonNetwork.Disconnect();
        for (float wait = 5f; PhotonNetwork.NetworkClientState != ClientState.Disconnected && wait > 0f; wait -= Time.unscaledDeltaTime)
            yield return null;
        SceneManager.LoadScene(MainMenuScene);
    }

    private IEnumerator OpenLobby()
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
        {
            if (content) content.alpha = 1f - t;
            if (logoGroup) logoGroup.alpha = 1f - t;
            yield return null;
        }
        SceneManager.LoadScene(LobbyScene);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (!_error && !_leaving)
        {
            _sinceShown += dt;
            _elapsed += dt;
            if (_shown < _target && _sinceShown >= MinStepTime)
            {
                _shown++;
                _sinceShown = 0f;
                Refresh();
            }
            if (_shown == 2 && _sinceShown >= ReadyHold)
            {
                _leaving = true;
                StartCoroutine(OpenLobby());
            }
            else if (_shown < 2 && _elapsed > Timeout)
            {
                Debug.LogWarning("[LoadingScreen] No lobby after " + Timeout + " s.");
                ShowError();
            }
        }
        Animate();
    }

    // Board cells taking turns (one "pop" each), and a ring growing out of the current step.
    private void Animate()
    {
        float time = Time.unscaledTime;
        for (int i = 0; i < cells.Length; i++)
        {
            if (!cells[i]) continue;
            if (_error)
            {
                cells[i].transform.localScale = Vector3.one;
                cells[i].color = CellOff;
                continue;
            }
            float phase = Mathf.Repeat((time - i * 0.2f) / CellCycle, 1f);
            float v = Mathf.SmoothStep(0f, 1f, phase < 0.45f ? phase / 0.45f : 1f - (phase - 0.45f) / 0.55f);
            cells[i].transform.localScale = Vector3.one * Mathf.Lerp(0.78f, 1f, v);
            var c = i < cellColors.Length ? cellColors[i] : Color.white;
            c.a = Mathf.Lerp(0.45f, 1f, v);
            cells[i].color = c;
        }

        float p = Mathf.Repeat(time / CellCycle, 1f);
        for (int i = 0; i < stepViews.Length; i++)
        {
            var pulse = stepViews[i].pulse;
            if (!pulse) continue;
            bool now = i == _shown && _shown < 2;
            pulse.enabled = now;
            if (!now) continue;
            pulse.transform.localScale = Vector3.one * (1f + 0.8f * p);
            pulse.color = new Color(Pink.r, Pink.g, Pink.b, 0.6f * (1f - p));
        }
    }

    private void Refresh()
    {
        if (status) status.text = _error ? "Couldn't connect to the server" : Status[_shown];
        if (sub) sub.text = _error ? "Check your internet connection and try again." : Sub[_shown];
        if (steps) steps.SetActive(!_error);
        if (errorButtons) errorButtons.SetActive(_error);
        if (cancelButton) cancelButton.SetActive(!_error && _shown < 2);

        for (int i = 0; i < stepViews.Length; i++)
        {
            var v = stepViews[i];
            bool done = i < _shown || (i == 2 && _shown == 2);
            bool now = i == _shown && !done;
            if (v.dot) v.dot.enabled = done || now;
            if (v.dot) v.dot.color = Pink;
            if (v.inner) v.inner.SetActive(now);
            if (v.tick) v.tick.SetActive(done);
            if (v.todo) v.todo.SetActive(!done && !now);
            if (v.label) v.label.color = done || now ? Color.white : Faded;
            if (v.line) v.line.color = i <= _shown ? Pink : LineOff;
        }
    }
}
