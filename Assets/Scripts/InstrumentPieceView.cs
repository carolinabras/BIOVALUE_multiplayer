using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class InstrumentCategoryStyle
{
    public InstrumentType type;
    public string displayName;
    public Color ring;   // outline (strong colour)
    public Color fill;   // front fill (pale)
    public Color back;   // back fill (mid tone)
    public Color badge;  // number badge (dark enough for white text)
    public Sprite icon;

    public InstrumentCategoryStyle() { }

    public InstrumentCategoryStyle(InstrumentType type, string displayName, string ring, string fill, string back, string badge)
    {
        this.type = type;
        this.displayName = displayName;
        this.ring = Hex(ring);
        this.fill = Hex(fill);
        this.back = Hex(back);
        this.badge = Hex(badge);
    }

    private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
}

// Visuals of an instrument piece: front (number + title, colour = category) and back (category icon + name).
// The design lives in "Piece", a fixed 300x346 hexagon that is scaled to fit whatever cell or slot it is in.
// Built into the prefab by the menu "BioValue > Build Instrument Piece".
public class InstrumentPieceView : MonoBehaviour
{
    public const float RefWidth = 300f;
    public const float RefHeight = 346f;

    [Header("Parts (filled in by the builder)")]
    public RectTransform piece;
    public RectTransform flipper;
    public GameObject front;
    public GameObject back;

    public Image frontRing, frontRingSplit, frontFill, frontFillSplit, badge;
    public TMP_Text number, title;
    public Image frontIcon1, frontIcon2;

    public Image backRing, backRingSplit, backFill, backFillSplit, icon1, icon2;
    public TMP_Text categoryName;

    [Header("Categories")]
    public List<InstrumentCategoryStyle> categories = new List<InstrumentCategoryStyle>
    {
        new InstrumentCategoryStyle(InstrumentType.Inspection,  "Enforcement", "#FFC000", "#FAF7D2", "#F2EB96", "#8A6A00"),
        new InstrumentCategoryStyle(InstrumentType.Planning,    "Planning & Project Design", "#228AA2", "#D0EAF0", "#8AC6D2", "#17687A"),
        new InstrumentCategoryStyle(InstrumentType.Incentive,   "Incentive",    "#C00000", "#FCD4D4", "#F67E7E", "#C00000"),
        new InstrumentCategoryStyle(InstrumentType.Agreement,   "Agreement",    "#FFB011", "#F9E5CB", "#F5C187", "#9A5B00"),
        new InstrumentCategoryStyle(InstrumentType.Information, "Information",  "#47D45A", "#D9F2D0", "#8ED973", "#1E7A2C"),
        new InstrumentCategoryStyle(InstrumentType.Regulation,  "Regulation",   "#404040", "#E8E8E8", "#AEAEAE", "#404040"),
    };
    public InstrumentCategoryStyle noCategory =
        new InstrumentCategoryStyle(InstrumentType.None, "", "#8A9099", "#F1F2F4", "#D5D8DC", "#4A4F57");

    [Header("Icon sizes on the back")]
    public float singleIconSize = 112f;
    public float doubleIconSize = 84f;

    [Header("Icon sizes on the front")]
    public float frontSingleIconSize = 46f;
    public float frontDoubleIconSize = 40f;

    [Header("Title size (the same on every instrument)")]
    [Tooltip("Biggest title size, as a fraction of the title box height.")]
    public float titleMaxRatio = 0.26f;
    [Tooltip("Smallest shared title size. A title that doesn't fit even at this size shrinks on its own.")]
    public float titleMinRatio = 0.21f;

    [Header("Badge number and category name size (min, max), as a fraction of their box height")]
    public Vector2 numberSizeRange = new Vector2(0.375f, 0.56f);
    public Vector2 categorySizeRange = new Vector2(0.31f, 0.46f);

    [Header("Placement flip")]
    public float dropScale = 1.25f;
    public float dropTime = 0.45f;
    public float flipTime = 0.35f;   // one full turn (half to the edge, half back)
    public float holdTime = 1.0f;    // how long the back stays up

    private Vector2 lastSize = new Vector2(-1, -1);

    private void Awake()
    {
        ShowFront(true);
        Fit();
    }

    // Only flag it here: this can fire during a canvas rebuild, where text can't be rebuilt.
    private void OnRectTransformDimensionsChange() => lastSize = new Vector2(-1, -1);

    private void OnEnable()
    {
        lastSize = new Vector2(-1, -1);
        lastLossyScale = -1f;
    }

    private void LateUpdate()
    {
        UpdateLinkGlow();

        var size = ((RectTransform)transform).rect.size;
        if (size != lastSize) Fit();
        UpdateTitleSize();
        KeepProportional(number, numberSizeRange, ref numberHeight);
        KeepProportional(categoryName, categorySizeRange, ref categoryHeight);

        // Rebuild the text whenever its on-screen scale has settled at a new value (after a fit,
        // a move from hand to board, the end of the flip...). TextMeshPro otherwise keeps the
        // sharpness it computed for the old scale and the text looks blurry.
        if (piece && !IsAnimating())
        {
            float lossy = piece.lossyScale.y;
            if (!Mathf.Approximately(lossy, lastLossyScale))
            {
                lastLossyScale = lossy;
                RebuildText();
            }
        }
    }

    private float lastLossyScale = -1f;

    private bool IsAnimating() => flipper && LeanTween.isTweening(flipper.gameObject);

    public void RebuildText()
    {
        if (!piece) return;
        foreach (var t in piece.GetComponentsInChildren<TMP_Text>(false))
            t.ForceMeshUpdate(true, true);
    }

    // ── Fit to the box ──────────────────────────────────────────────────────────
    // The prefab is laid out in design units (piece 300x346). At runtime nothing is scaled down:
    // positions and sizes of the shapes and icons are multiplied by the real/design ratio, so the
    // piece is laid out at its true size on screen.
    //
    // Texts are NOT touched here. Their boxes are proportional (anchors only), their font sizes are
    // real screen sizes, and the global TextSupersampler sharpens them exactly like every other
    // text in the game (the same setup the old, crisp instrument titles had).

    private class RectRec { public RectTransform rt; public Vector2 pos, size; public bool inLayout; }

    private readonly List<RectRec> rects = new List<RectRec>();
    private readonly Dictionary<RectTransform, RectRec> rectByTransform = new Dictionary<RectTransform, RectRec>();
    private readonly List<(HorizontalOrVerticalLayoutGroup g, float spacing)> layouts = new List<(HorizontalOrVerticalLayoutGroup, float)>();
    private readonly List<(Shadow sh, Vector2 dist)> shadows = new List<(Shadow, Vector2)>();
    private bool recorded;
    private float scale = 1f;

    // Remembers the design values, before anything is resized.
    private void Record()
    {
        if (recorded || !piece) return;
        recorded = true;

        foreach (var rt in piece.GetComponentsInChildren<RectTransform>(true))
        {
            if (rt == piece || rt.GetComponent<TMP_Text>()) continue;
            var rec = new RectRec
            {
                rt = rt, pos = rt.anchoredPosition, size = rt.sizeDelta,
                inLayout = rt.parent && rt.parent.GetComponent<LayoutGroup>() != null,
            };
            rects.Add(rec);
            rectByTransform[rt] = rec;
        }
        foreach (var g in piece.GetComponentsInChildren<HorizontalOrVerticalLayoutGroup>(true)) layouts.Add((g, g.spacing));
        foreach (var sh in piece.GetComponentsInChildren<Shadow>(true)) shadows.Add((sh, sh.effectDistance));
    }

    // Fills this object's whole box, like the old art did. The hexagon stretches to the box;
    // icons keep their proportions.
    private void Fit()
    {
        if (!piece) return;
        var size = ((RectTransform)transform).rect.size;
        lastSize = size;
        float s = Mathf.Min(size.x / RefWidth, size.y / RefHeight);
        if (s <= 0f || float.IsNaN(s)) return;

        Record();
        scale = s;

        piece.localScale = Vector3.one;
        piece.sizeDelta = size;

        foreach (var g in layouts) if (g.g) g.g.spacing = g.spacing * s;
        foreach (var sh in shadows) if (sh.sh) sh.sh.effectDistance = sh.dist * s;

        foreach (var r in rects)
        {
            if (!r.rt) continue;
            if (!r.inLayout) r.rt.anchoredPosition = r.pos * s;
            r.rt.sizeDelta = r.size * s;
        }
    }

    // ── Shared title size ───────────────────────────────────────────────────────
    // Auto-sizing gave every title its own size. Instead, each name in the database is measured
    // once (as a fraction of the title box height, so it holds for any piece size and for the
    // supersampled text), and every title uses the size that fits the longest of them.
    // Measurements are grouped by box shape, since a hand slot and a board cell can differ.

    private const char ZeroWidthSpace = (char)0x200B;

    private class TitleGroup
    {
        public readonly Dictionary<string, float> best = new Dictionary<string, float>();
        public float shared = -1f;
        public int version;
    }

    private static readonly Dictionary<int, TitleGroup> titleGroups = new Dictionary<int, TitleGroup>();

    private TitleGroup appliedGroup;
    private int appliedVersion = -1;
    private float appliedHeight = -1f;
    private string titleText = "";

    // Lets long words like "Labelling/Certification" wrap after the slash.
    private static string TitleText(string name) => (name ?? "").Trim().Replace("/", "/" + ZeroWidthSpace);

    private void UpdateTitleSize()
    {
        // Runs during the placement flip too: the flip only scales the flipper, never the title box,
        // and a piece dropped from the tray must not keep its (bigger) tray title for 2 seconds.
        if (!title || !title.isActiveAndEnabled) return;
        var box = title.rectTransform.rect;
        if (box.height <= 1f || box.width <= 1f) return;

        int key = Mathf.RoundToInt(box.width / box.height * 20f);
        if (!titleGroups.TryGetValue(key, out var group))
            titleGroups[key] = group = new TitleGroup();

        if (!group.best.ContainsKey(titleText))
        {
            Measure(group, titleText);
            var db = InstrumentDatabaseSession.Instance ? InstrumentDatabaseSession.Instance.SessionDb : null;
            if (db != null && db.instruments != null)
                foreach (var instr in db.instruments)
                    if (instr != null) Measure(group, TitleText(instr.name));

            float min = float.MaxValue;
            foreach (var r in group.best.Values) min = Mathf.Min(min, r);
            group.shared = Mathf.Max(titleMinRatio, min);
            group.version++;
        }

        if (appliedGroup == group && appliedVersion == group.version && Mathf.Approximately(appliedHeight, box.height)) return;
        appliedGroup = group;
        appliedVersion = group.version;
        appliedHeight = box.height;

        float ratio = group.shared;
        if (group.best.TryGetValue(titleText, out var own) && own < ratio) ratio = own; // too long even for the shared size
        title.enableAutoSizing = false;
        title.fontSize = ratio * box.height;
    }

    // Finds the biggest size (fraction of the box height) at which this text fits the title box
    // with no word cut across two lines.
    private void Measure(TitleGroup group, string text)
    {
        if (group.best.ContainsKey(text)) return;
        if (string.IsNullOrEmpty(text)) { group.best[text] = titleMaxRatio; return; }

        float h = title.rectTransform.rect.height;
        string oldText = title.text;
        float oldSize = title.fontSize;
        title.enableAutoSizing = false;
        title.text = text;

        float lo = titleMinRatio * 0.5f, hi = titleMaxRatio;
        float best = lo;
        if (Fits(hi * h, h)) best = hi;
        else
        {
            for (int i = 0; i < 10; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Fits(mid * h, h)) { best = mid; lo = mid; }
                else hi = mid;
            }
        }
        group.best[text] = best;

        title.text = oldText;
        title.fontSize = oldSize;
        title.ForceMeshUpdate(true, true);
    }

    private bool Fits(float size, float boxHeight)
    {
        title.fontSize = size;
        title.ForceMeshUpdate(true, true);
        if (title.preferredHeight > boxHeight) return false;

        var info = title.textInfo;
        for (int l = 0; l < info.lineCount - 1; l++)
        {
            int last = info.lineInfo[l].lastCharacterIndex;
            if (last < 0 || last + 1 >= info.characterCount) continue;
            char c = info.characterInfo[last].character;
            char next = info.characterInfo[last + 1].character;
            bool breakOk = char.IsWhiteSpace(c) || char.IsWhiteSpace(next) || c == '-' || c == '/'
                           || c == ZeroWidthSpace || next == ZeroWidthSpace;
            if (!breakOk) return false; // a word was cut in two
        }
        return true;
    }

    // The badge number and the category name keep auto-sizing (they're short), but their size
    // range follows their box, so they grow and shrink with the piece at any screen resolution.
    private float numberHeight = -1f, categoryHeight = -1f;

    private static void KeepProportional(TMP_Text t, Vector2 range, ref float lastHeight)
    {
        if (!t) return;
        float h = t.rectTransform.rect.height;
        if (h <= 1f || Mathf.Approximately(h, lastHeight)) return;
        lastHeight = h;
        t.enableAutoSizing = true;
        t.fontSizeMin = range.x * h;
        t.fontSizeMax = range.y * h;
    }

    public InstrumentCategoryStyle StyleFor(InstrumentType type)
    {
        if (type == InstrumentType.None) return null;
        foreach (var c in categories)
            if (c != null && c.type == type) return c;
        return null;
    }

    // ── Link glow ───────────────────────────────────────────────────────────────
    // A pulsing pink halo while the local player's selected action cards link to this instrument.

    [Header("Link glow (action cards)")]
    public Color linkGlowColor = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    public float linkGlowPulseSpeed = 3.2f;

    private const float GlowPad = 0.225f; // halo reaches this far past the piece, as a fraction of its size
    private static Sprite glowSprite;
    private int instrumentId = -1;
    private Image linkGlow;
    private float glowAmount; // fades 0..1

    private void UpdateLinkGlow()
    {
        bool want = instrumentId > 0 && InstrumentLinkHighlights.Contains(instrumentId);
        if (!want && glowAmount <= 0f) return;

        glowAmount = Mathf.MoveTowards(glowAmount, want ? 1f : 0f, Time.unscaledDeltaTime * 4f);
        if (!linkGlow) linkGlow = CreateLinkGlow();
        if (!linkGlow) return;

        linkGlow.enabled = glowAmount > 0f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * linkGlowPulseSpeed);
        var c = linkGlowColor;
        c.a = glowAmount * (0.55f + 0.45f * pulse);
        linkGlow.color = c;
        float s = 1f + 0.04f * pulse * glowAmount;
        linkGlow.rectTransform.localScale = new Vector3(s, s, 1f);
    }

    private Image CreateLinkGlow()
    {
        if (!piece) return null;
        var go = new GameObject("LinkGlow", typeof(RectTransform));
        go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(piece, false);
        rt.SetAsFirstSibling(); // behind the hexagon
        // Anchors past the edges, so the halo follows the piece at any size.
        rt.anchorMin = new Vector2(-GlowPad, -GlowPad);
        rt.anchorMax = new Vector2(1f + GlowPad, 1f + GlowPad);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.sprite = GlowSprite();
        img.raycastTarget = false;
        img.enabled = false;
        return img;
    }

    // Soft hexagon: a solid ring just outside the piece fading out to nothing. Made once, in code.
    private static Sprite GlowSprite()
    {
        if (glowSprite) return glowSprite;
        const float a = RefWidth * 0.5f;             // apothem (pointy-top hexagon, flat sides left/right)
        const float b = RefHeight * 0.5f;
        const float scale = 1f + 2f * GlowPad;
        int w = 160, h = Mathf.RoundToInt(160 * RefHeight / RefWidth);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "InstrumentLinkGlow",
        };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float fx = Mathf.Abs(((x + 0.5f) / w * 2f - 1f) * scale * a);
            float fy = Mathf.Abs(((y + 0.5f) / h * 2f - 1f) * scale * b);
            float n = Mathf.Max(fx / a, (fx * 0.5f + fy * 0.8660254f) / a); // 1 = on the piece's edge
            float alpha = n <= 1.05f ? 1f : Mathf.Pow(Mathf.Clamp01(1f - (n - 1.05f) / 0.38f), 2f);
            px[y * w + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        return glowSprite;
    }

    public void Apply(Instrument instr)
    {
        if (instr == null) return;
        instrumentId = instr.id;

        var a = StyleFor(instr.typeOne);
        var b = instr.typeTwo != instr.typeOne ? StyleFor(instr.typeTwo) : null;
        if (a == null) { a = b; b = null; }
        if (a == null) a = noCategory;
        bool two = b != null;

        // Front
        SetColor(frontRing, a.ring);
        SetColor(frontFill, a.fill);
        SetSplit(frontRingSplit, two, two ? b.ring : default);
        SetSplit(frontFillSplit, two, two ? b.fill : default);
        SetColor(badge, a.badge);

        string num = instr.id >= 0 && instr.id <= 100 ? instr.id.ToString() : "";
        if (number) number.text = num;
        if (badge) badge.gameObject.SetActive(num.Length > 0);
        titleText = TitleText(instr.name);
        if (title) title.text = titleText;
        appliedVersion = -1;

        float frontIconSize = two ? frontDoubleIconSize : frontSingleIconSize;
        SetIcon(frontIcon1, a.icon, frontIconSize, true);
        SetIcon(frontIcon2, two ? b.icon : null, frontIconSize, two);

        // Back
        SetColor(backRing, a.ring);
        SetColor(backFill, a.back);
        SetSplit(backRingSplit, two, two ? b.ring : default);
        SetSplit(backFillSplit, two, two ? b.back : default);

        float iconSize = two ? doubleIconSize : singleIconSize;
        SetIcon(icon1, a.icon, iconSize, true);
        SetIcon(icon2, two ? b.icon : null, iconSize, two);

        if (categoryName)
            categoryName.text = two ? $"{a.displayName}\n+ {b.displayName}" : a.displayName;

        ShowFront(true);
    }

    private static void SetColor(Image img, Color c)
    {
        if (img) img.color = c;
    }

    private static void SetSplit(Image img, bool on, Color c)
    {
        if (!img) return;
        img.gameObject.SetActive(on);
        if (on) img.color = c;
    }

    // size is in design units; it's converted to the real size like everything else.
    private void SetIcon(Image img, Sprite sprite, float size, bool on)
    {
        if (!img) return;
        img.gameObject.SetActive(on && sprite != null);
        img.sprite = sprite;
        var design = new Vector2(size, size);
        if (recorded && rectByTransform.TryGetValue(img.rectTransform, out var rec))
        {
            rec.size = design;
            img.rectTransform.sizeDelta = design * scale;
        }
        else
        {
            img.rectTransform.sizeDelta = design;
        }
    }

    private void ShowFront(bool showFront)
    {
        if (front) front.SetActive(showFront);
        if (back) back.SetActive(!showFront);
    }

    // Drop into the cell, flip to show the category, hold, flip back to the front.
    public void PlayPlacementFlip()
    {
        if (!flipper) return;
        LeanTween.cancel(flipper.gameObject);
        ShowFront(true);

        float half = flipTime * 0.5f;
        flipper.localScale = new Vector3(dropScale, dropScale, 1f);

        LeanTween.scale(flipper, Vector3.one, dropTime).setEaseOutBack().setOnComplete(() =>
            Turn(half, () =>
                LeanTween.delayedCall(flipper.gameObject, holdTime, () =>
                    Turn(half, null))));
    }

    // One turn: squeeze to the edge, swap faces, open up again.
    private void Turn(float half, Action done)
    {
        LeanTween.scale(flipper, new Vector3(0f, 1f, 1f), half).setEaseInSine().setOnComplete(() =>
        {
            ShowFront(!front || !front.activeSelf);
            LeanTween.scale(flipper, Vector3.one, half).setEaseOutSine().setOnComplete(() =>
            {
                RebuildText(); // the face that just turned up was built mid-animation
                done?.Invoke();
            });
        });
    }

    private void OnDisable()
    {
        // Never leave a piece stuck mid-flip or on its back.
        if (flipper)
        {
            LeanTween.cancel(flipper.gameObject);
            flipper.localScale = Vector3.one;
        }
        ShowFront(true);
    }
}

// Instruments (by number) the local player's selected action cards are linked to.
// Set by ActionCardsSpawner; every InstrumentPieceView with that number glows while the deck is open.
// Local only: nothing here is sent over the network, so other players never see it.
public static class InstrumentLinkHighlights
{
    private static readonly HashSet<int> ids = new HashSet<int>();

    // Glow only while the action deck is open (set by ActionDeck).
    public static bool Visible { get; set; }

    public static bool Contains(int id) => Visible && ids.Contains(id);

    public static void Set(IEnumerable<int> newIds)
    {
        ids.Clear();
        if (newIds == null) return;
        foreach (int id in newIds) ids.Add(id);
    }

    public static void Clear()
    {
        ids.Clear();
        Visible = false;
    }
}
