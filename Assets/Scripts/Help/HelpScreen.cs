using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// "How to play" (mockup "How to play"): one page per moment of the game, in the order it is played,
// each written for players or for the game master (the switch at the top).
// Opened by the main menu's "How to play" and by the "?" in the other scenes (HelpButton), at the
// page for where you are. Built by "BioValue/Build Help" into Resources/HelpScreen.prefab.
public class HelpScreen : MonoBehaviour
{
    public class Page
    {
        public string title, lead, tip;
        public string[] steps;
    }

    public class Chapter
    {
        public string title, sub, eyebrow;
        public Color color, fill;
        public Page player, gm;
    }

    private const string EndGameKey = "endGame";
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x6B, 0x65, 0x84, 0xFF);
    private static readonly Color Pink = new Color32(0xFE, 0x49, 0xA4, 0xFF);
    private static readonly Color DoneDot = new Color32(0xC9, 0xBE, 0xEA, 0xFF);
    private static readonly Color TodoDot = new Color32(0xE4, 0xDF, 0xF0, 0xFF);
    private static readonly Color Faint = new Color32(0xC9, 0xC3, 0xD8, 0xFF);

    private static Color Hex(string html) => ColorUtility.TryParseHtmlString(html, out var c) ? c : Color.white;

    public static readonly Chapter[] Chapters =
    {
        new Chapter
        {
            title = "The goal", sub = "What BioValue is about", eyebrow = "WELCOME",
            color = Hex("#FE49A4"), fill = Hex("#FFF0F7"),
            player = new Page
            {
                title = "Bring biodiversity into the city, together",
                lead = "You play people from different organisations who share one game objective, written by the game master. Over three rounds you choose policy instruments, act on them, and help each other.",
                steps = new[]
                {
                    "The game objective lands in the middle of the board. Click it any time to read it in full and open the materials.",
                    "Each round has an intro that tells you what happens next.",
                    "At the end, everyone says how close the group got to the objective and to their own."
                },
                tip = ""
            },
            gm = new Page
            {
                title = "You run the game",
                lead = "As game master you set the game up in the lobby, start each round and close it when everyone is ready. You don't place instruments or play cards like the players.",
                steps = new[]
                {
                    "In the lobby: number of players, the game objective and a link to materials.",
                    "Choose the instruments, the action card limit, the tokens and the score formula.",
                    "Start the game when everyone has joined."
                },
                tip = "The checklist at the bottom of the lobby says what is still missing before you can start."
            }
        },
        new Chapter
        {
            title = "Your role", sub = "Name, organisation, objective", eyebrow = "BEFORE THE GAME",
            color = Hex("#257AFE"), fill = Hex("#EEF4FF"),
            player = new Page
            {
                title = "Say who you are",
                lead = "Before the board appears, write who you play and what you want to achieve. The others see it during the game.",
                steps = new[]
                {
                    "Write your name and your organisation.",
                    "Write your own objective, in a sentence.",
                    "Press I'm ready. You can still edit until the game master starts."
                },
                tip = "A concrete objective (where, what, by when) makes the end of the game easier to rate."
            },
            gm = new Page
            {
                title = "Watch everyone get ready",
                lead = "You see a card per player that fills in as they type, and who is ready.",
                steps = new[]
                {
                    "Wait for the cards to turn ready.",
                    "Start the game when all are ready, or Start anyway if someone is stuck."
                },
                tip = "Players who left the room are not counted."
            }
        },
        new Chapter
        {
            title = "Instruments round", sub = "Place and vote", eyebrow = "ROUND 1",
            color = Hex("#228AA2"), fill = Hex("#E8F5F8"),
            player = new Page
            {
                title = "Place one instrument per turn",
                lead = "Instruments are policy tools, in six categories. Players take turns; the ribbon at the top says whose turn it is and who is next.",
                steps = new[]
                {
                    "On your turn, pick an instrument from your tray and drop it on a free cell (they glow violet).",
                    "Everyone else votes: do you agree with it?",
                    "Long-press or click a piece to read what it does."
                },
                tip = "Place instruments near ones they work well with: action cards link to them later."
            },
            gm = new Page
            {
                title = "Keep the turns moving",
                lead = "You see who is placing and the votes as they come in.",
                steps = new[]
                {
                    "End the vote and move to the next turn from the ribbon.",
                    "You can place an instrument yourself as a special action.",
                    "End the instruments round from your panel when the board is ready."
                },
                tip = "Your panel opens from the tab on the left edge."
            }
        },
        new Chapter
        {
            title = "Action cards round", sub = "Act on the instruments", eyebrow = "ROUND 2",
            color = Hex("#C97A12"), fill = Hex("#FFF4E3"),
            player = new Page
            {
                title = "Choose what you will do",
                lead = "Everyone plays at the same time. Open the deck, choose your action cards and say which instruments on the board each one uses.",
                steps = new[]
                {
                    "Select up to the card limit the game master set.",
                    "Link each card to instruments on the board.",
                    "Write your own open action if no card fits, then play your cards."
                },
                tip = "The fan of cards at the bottom shows how many you can still play."
            },
            gm = new Page
            {
                title = "Wait for everyone's cards",
                lead = "The ribbon counts who has played, and your panel marks each player Choosing or Played.",
                steps = new[]
                {
                    "When everyone has played, End actions round glows.",
                    "See everyone's actions from your panel.",
                    "End the round to start the collaboration round."
                },
                tip = "You can end the round early if someone is stuck."
            }
        },
        new Chapter
        {
            title = "Collaboration round", sub = "Join each other's actions", eyebrow = "ROUND 3",
            color = Hex("#2F6FB0"), fill = Hex("#EAF2FB"),
            player = new Page
            {
                title = "Collaborate with tokens",
                lead = "Everyone's played actions are shown side by side. Joining one costs a token, and its player decides.",
                steps = new[]
                {
                    "Press Collaborate on an action you want to join: 1 token.",
                    "Its player accepts or declines; a declined request gives your token back.",
                    "Press I'm done when you have no tokens left or nothing left to join (the button glows)."
                },
                tip = "Requests on your own actions pop up in the corner: answer them so others can plan."
            },
            gm = new Page
            {
                title = "See the collaborations happen",
                lead = "Each player's column shows how many tokens they used and who joined their actions.",
                steps = new[]
                {
                    "Watch players turn Done.",
                    "End the collaboration round to go to the results."
                },
                tip = "Players who are done get a green ring on their avatar."
            }
        },
        new Chapter
        {
            title = "Results", sub = "Rate and compare", eyebrow = "END OF THE GAME",
            color = Hex("#8C6BE8"), fill = Hex("#F3EEFF"),
            player = new Page
            {
                title = "How close did we get?",
                lead = "Rate how close the group got to the game objective, and how close you got to your own. Then everyone's scores appear.",
                steps = new[]
                {
                    "Give your two ratings.",
                    "Choose whether to show your objective on the results.",
                    "Compare scores: actions and collaborations count too."
                },
                tip = "The game master chose the score formula in the lobby; the results show it."
            },
            gm = new Page
            {
                title = "Close the game",
                lead = "You see the results once everyone has rated, and can save them.",
                steps = new[]
                {
                    "Wait for all ratings.",
                    "Show the results to everyone.",
                    "Save the results to keep the game's data."
                },
                tip = "On the web, saving downloads a file."
            }
        },
        new Chapter
        {
            title = "Game master tools", sub = "Your panel, at a glance", eyebrow = "GAME MASTER",
            color = Hex("#46327F"), fill = Hex("#F1ECFB"),
            player = new Page
            {
                title = "What the game master does",
                lead = "One person runs the game and does not play cards. They open and close the rounds and can help when someone is stuck.",
                steps = new[]
                {
                    "They start and end each round.",
                    "They can see everyone's actions.",
                    "They can remove a player who left."
                },
                tip = "If something seems stuck, ask the game master to look at their panel."
            },
            gm = new Page
            {
                title = "Your panel",
                lead = "The tab on the left edge opens your panel: the round you are in, the players and what each one is doing.",
                steps = new[]
                {
                    "The steps at the top show the round.",
                    "Each player shows a status: Placing, Next, Choosing, Played or Done.",
                    "Remove a player with ×, twice to confirm."
                },
                tip = "Esc closes this page and the panel."
            }
        }
    };

    [Header("Chapters (each: root image, Hex, Hex/Number, Title, Sub)")]
    public Button[] chapterButtons;

    [Header("Top")]
    public Button[] roleButtons;       // Player, Game master (root image, Label)
    public Button closeButton;

    [Header("Page")]
    public Image artFill;
    public GameObject[] arts;          // one per chapter
    public RectTransform textColumn;
    public TMP_Text eyebrow;
    public TMP_Text title;
    public TMP_Text lead;
    public GameObject[] steps;         // each: Badge, Badge/Number, Text
    public GameObject tip;
    public TMP_Text tipText;

    [Header("Footer")]
    public Button backButton;
    public Button nextButton;
    public TMP_Text nextLabel;
    public Image[] dots;

    private static HelpScreen _open;
    private int _at;
    private bool _gm;

    public static bool IsOpen => _open;

    // Opens at `chapter`, or at the page for where you are (-1).
    public static void Open(int chapter = -1)
    {
        if (_open)
        {
            _open.Show(chapter < 0 ? _open._at : chapter);
            return;
        }
        var prefab = Resources.Load<GameObject>("HelpScreen");
        if (!prefab)
        {
            Debug.LogError("[HelpScreen] Resources/HelpScreen.prefab is missing: run BioValue/Build Help.");
            return;
        }

        // Its own canvas, over everything in the scene.
        var holder = new GameObject("HelpCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = holder.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = holder.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var screen = Instantiate(prefab, holder.transform, false).GetComponent<HelpScreen>();
        _open = screen;
        screen._gm = PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient;
        screen.Show(chapter < 0 ? ChapterForScene() : chapter);
    }

    // Lobby: the goal; Role: your role; the game: the round you are in.
    private static int ChapterForScene()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "Role") return 1;
        if (scene != "MainGame") return 0;
        bool ended = PhotonNetwork.InRoom &&
                     PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(EndGameKey, out var e) && e is bool b && b;
        if (ended) return 5;
        var phase = GameState.Instance != null ? GameState.Instance.GetCurrentGamePhase() : GameState.GamePhase.None;
        return phase == GameState.GamePhase.Collaboration ? 4 : phase == GameState.GamePhase.ActionCardPlay ? 3 : 2;
    }

    private void Awake()
    {
        for (int i = 0; i < chapterButtons.Length; i++) { int c = i; chapterButtons[i].onClick.AddListener(() => Show(c)); }
        for (int i = 0; i < roleButtons.Length; i++) { bool gm = i == 1; roleButtons[i].onClick.AddListener(() => { _gm = gm; Refresh(); }); }
        closeButton.onClick.AddListener(Close);
        backButton.onClick.AddListener(() => { if (_at > 0) Show(_at - 1); });
        nextButton.onClick.AddListener(() => Show(_at + 1 < Chapters.Length ? _at + 1 : 0));
    }

    private void OnDestroy()
    {
        if (_open == this) _open = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    public void Close() => Destroy(transform.root.gameObject); // the HelpCanvas made in Open

    private void Show(int chapter)
    {
        _at = Mathf.Clamp(chapter, 0, Chapters.Length - 1);
        Refresh();
    }

    private void Refresh()
    {
        var ch = Chapters[_at];
        var page = _gm ? ch.gm : ch.player;

        for (int i = 0; i < chapterButtons.Length && i < Chapters.Length; i++)
        {
            bool on = i == _at, done = i < _at;
            var t = chapterButtons[i].transform;
            SetColor(t, "", on ? Color.white : Color.clear);
            SetColor(t, "Hex", on ? Chapters[i].color : done ? new Color(1, 1, 1, .85f) : new Color(1, 1, 1, .18f));
            SetColor(t, "Hex/Number", on ? Color.white : done ? Chapters[i].color : Color.white);
            SetColor(t, "Title", on ? Ink : Color.white);
            SetColor(t, "Sub", on ? Muted : new Color(1, 1, 1, .7f));
        }
        for (int i = 0; i < roleButtons.Length; i++)
        {
            bool on = (i == 1) == _gm;
            SetColor(roleButtons[i].transform, "", on ? Color.white : Color.clear);
            SetColor(roleButtons[i].transform, "Label", on ? Ink : Color.white);
        }

        artFill.color = ch.fill;
        for (int i = 0; i < arts.Length; i++) if (arts[i]) arts[i].SetActive(i == _at);

        eyebrow.text = ch.eyebrow + (_gm ? " · GAME MASTER" : "");
        eyebrow.color = ch.color;
        title.text = page.title;
        lead.text = page.lead;
        for (int i = 0; i < steps.Length; i++)
        {
            bool on = i < page.steps.Length;
            steps[i].SetActive(on);
            if (!on) continue;
            SetColor(steps[i].transform, "Badge", ch.color);
            SetText(steps[i].transform, "Badge/Number", (i + 1).ToString());
            SetText(steps[i].transform, "Text", page.steps[i]);
        }
        bool hasTip = !string.IsNullOrEmpty(page.tip);
        tip.SetActive(hasTip);
        if (hasTip) tipText.text = page.tip;

        backButton.interactable = _at > 0;
        SetColor(backButton.transform, "Label", _at > 0 ? Ink : Faint);
        nextLabel.text = _at == Chapters.Length - 1 ? "Start again" : "Next: " + Chapters[_at + 1].title;
        for (int i = 0; i < dots.Length; i++)
        {
            dots[i].color = i == _at ? Pink : i < _at ? DoneDot : TodoDot;
            var size = dots[i].GetComponent<LayoutElement>();
            if (size) size.preferredWidth = size.minWidth = i == _at ? 37 : 13;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(textColumn);
    }

    private static void SetColor(Transform root, string path, Color color)
    {
        var t = string.IsNullOrEmpty(path) ? root : root.Find(path);
        var g = t ? t.GetComponent<Graphic>() : null;
        if (g) g.color = color;
    }

    private static void SetText(Transform root, string path, string text)
    {
        var t = root.Find(path);
        var tmp = t ? t.GetComponent<TMP_Text>() : null;
        if (tmp) tmp.text = text;
    }
}
