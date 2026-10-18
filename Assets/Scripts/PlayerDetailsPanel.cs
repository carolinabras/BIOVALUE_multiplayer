using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main game: the window opened by clicking a player's capsule. Header in the player's colour with
// their profile icon and number, name and organisation; then collaboration tokens, instruments placed and action cards
// played, the lists of those instruments and cards, and a button that opens their played cards.
// Built by the editor menu "BioValue/Build Player Capsules".
public class PlayerDetailsPanel : MonoBehaviour
{
    public static PlayerDetailsPanel Instance { get; private set; }

    public GameObject root;            // overlay + window, hidden when closed
    public CanvasGroup rootGroup;
    public RectTransform window;

    [Header("Header")]
    public Image headerFill;
    public TMP_Text badgeNumber;
    public Image avatarIcon;           // the person on the white circle, in the player's colour
    public TMP_Text eyebrow;
    public TMP_Text nameText;
    public TMP_Text companyText;

    [Header("Body")]
    public GameObject turnTag;
    public TMP_Text tokensValue;
    public TMP_Text instrumentsValue;
    public TMP_Text cardsValue;
    public RectTransform instrumentsContainer;
    public GameObject instrumentTemplate;
    public GameObject noInstruments;
    public RectTransform cardsContainer;
    public GameObject cardTemplate;
    public GameObject noCards;
    public GameObject showCardsButton;

    public bool IsOpen => root && root.activeSelf;

    private PlayerCapsuleHook current;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private string listsShown;
    private float nextRefresh;

    private void Awake()
    {
        Instance = this;
        if (root) root.SetActive(false);
        if (instrumentTemplate) instrumentTemplate.SetActive(false);
        if (cardTemplate) cardTemplate.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (!current || !current.isActiveAndEnabled) { Close(); return; }

        // Turn, tokens, placements and cards can change while it is open.
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.5f;
            Fill();
        }
    }

    // ── Open / close ─────────────────────────────────────────────────────────

    public void Open(PlayerCapsuleHook capsule)
    {
        if (!capsule || !root) return;
        bool wasOpen = IsOpen;
        if (current != capsule) listsShown = null;
        current = capsule;
        root.SetActive(true);
        transform.SetAsLastSibling();
        Fill();

        if (!wasOpen && window)
        {
            LeanTween.cancel(window.gameObject);
            window.localScale = Vector3.one * 0.94f;
            LeanTween.scale(window.gameObject, Vector3.one, 0.22f).setEaseOutCubic().setIgnoreTimeScale(true);
            if (rootGroup)
            {
                rootGroup.alpha = 0f;
                LeanTween.alphaCanvas(rootGroup, 1f, 0.18f).setIgnoreTimeScale(true);
            }
        }
    }

    public void Close()
    {
        current = null;
        listsShown = null;
        if (root) root.SetActive(false);
    }

    // Opens the played-cards panel of this player (for everyone when the GM does it).
    public void ShowPlayedCards()
    {
        var spawner = current ? current.CardSpawner : null;
        Close();
        if (spawner) spawner.OnClickSpawnCards();
    }

    // ── Content ──────────────────────────────────────────────────────────────

    private void Fill()
    {
        int actor = current._id;
        int number = current.PlayerNumber;
        Color color = current.PlayerColor;
        var player = FindPlayer(actor);

        if (headerFill) headerFill.color = color;
        if (badgeNumber) badgeNumber.text = number.ToString();
        if (avatarIcon) avatarIcon.color = color;
        if (eyebrow) eyebrow.text = $"PLAYER {number}";
        if (nameText) nameText.text = current.PlayerName;
        if (companyText) companyText.text = Prop(player, BiovalueStatics.PlayerCompanyKey);

        bool isTurn = GameState.Instance != null && GameState.Instance.GetCurrentPlayerTurnIndex() == actor;
        if (turnTag) turnTag.SetActive(isTurn);

        var instruments = PlacedInstruments(number);
        var cards = PlayedCards(actor);
        if (tokensValue)
            tokensValue.text = player != null && player.CustomProperties.TryGetValue(BiovalueStatics.CollabTokensKey, out var t) && t != null
                ? t.ToString() : "–";
        if (instrumentsValue) instrumentsValue.text = instruments.Count.ToString();
        if (cardsValue) cardsValue.text = cards.Count.ToString();
        if (showCardsButton) showCardsButton.SetActive(cards.Count > 0 && current.CardSpawner);

        // Rebuild the chips only when the lists change, so the window doesn't flicker on refresh.
        string lists = string.Join(",", instruments.Select(h => h.instrument.id)) + "|" + string.Join(",", cards);
        if (lists == listsShown) return;
        listsShown = lists;

        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
        foreach (var hook in instruments) AddInstrument(hook);
        foreach (var id in cards) AddCard(id);
        if (noInstruments) noInstruments.SetActive(instruments.Count == 0);
        if (noCards) noCards.SetActive(cards.Count == 0);
        if (window) LayoutRebuilder.ForceRebuildLayoutImmediate(window);
    }

    private void AddInstrument(InstrumentHook hook)
    {
        if (!instrumentTemplate || !instrumentsContainer) return;
        var chip = Instantiate(instrumentTemplate, instrumentsContainer);
        chip.SetActive(true);
        var view = hook.GetComponent<InstrumentPieceView>();
        var style = view ? view.StyleFor(hook.instrument.typeOne) : null;
        var hexT = chip.transform.Find("Hex");
        if (hexT && style != null) hexT.GetComponent<Image>().color = style.ring;
        var num = chip.transform.Find("Hex/Number");
        if (num) num.GetComponent<TMP_Text>().text = hook.instrument.id.ToString();
        var label = chip.transform.Find("Label");
        if (label) label.GetComponent<TMP_Text>().text = hook.instrument.name;
        spawned.Add(chip);
    }

    private void AddCard(int cardId)
    {
        if (!cardTemplate || !cardsContainer) return;
        var chip = Instantiate(cardTemplate, cardsContainer);
        chip.SetActive(true);
        var db = ActionCardsDatabaseSession.Instance ? ActionCardsDatabaseSession.Instance.SessionDb : null;
        var card = db ? db.GetActionCardById(cardId) : null;
        var label = chip.GetComponentInChildren<TMP_Text>(true);
        if (label)
            label.text = $"<b>{InstrumentDetailsPanel.CardNumber(cardId)}</b>  {(card != null ? card.cardName : "")}";
        spawned.Add(chip);
    }

    // Instruments on the board placed by this player (ownerId is the player's number), by id.
    private static List<InstrumentHook> PlacedInstruments(int number)
    {
        var seen = new HashSet<int>();
        return FindObjectsByType<InstrumentHook>(FindObjectsSortMode.None)
            .Where(h => h.instrument != null && h.instrument.isPlaced && h.instrument.ownerId == number && seen.Add(h.instrument.id))
            .OrderBy(h => h.instrument.id)
            .ToList();
    }

    // Same key as PlayedCardsSpawner: actor number - 1.
    private static List<int> PlayedCards(int actor)
    {
        if (GameState.Instance != null && GameState.Instance.playerActionCards.TryGetValue(actor - 1, out var cards) && cards != null)
            return cards;
        return new List<int>();
    }

    private static Player FindPlayer(int actor) =>
        PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == actor);

    private static string Prop(Player player, string key) =>
        player != null && player.CustomProperties.TryGetValue(key, out var v) && v != null ? v.ToString() : "";
}
