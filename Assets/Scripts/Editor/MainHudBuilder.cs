using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static ActionCardBuilder;

// Main game HUD on MainGame's canvas:
//  - Turn ribbon at the top of the board: who plays now, who is next, vote progress, and for the GM the
//    pink button that moves the game on (End turn / End vote · next turn / End actions round).
//  - Vote result: for a few seconds the ribbon turns green/red ("VOTE PASSED · ... stays") while an
//    APPROVED / REJECTED stamp lands on the piece. Replaces VotingManager's old result text.
//  - GM drawer (GM only): a "GAME MASTER" tab on the left edge slides in the GM panel: game phase,
//    Show all actions, players with remove, results.
//  - Collaboration round (after the action cards round): "Played actions" over the board, 4 players per
//    page, with "Collaborate · 1 token" on the cards; "I'm done" (players) and "End collaboration" (GM,
//    wired like the old End Actions Round button) on the ribbon and in the GM drawer.
//  - Vote bubble: while an instrument is voted on, a pulsing pink outline and a "VOTING" tag on the piece,
//    and a bubble beside it: "Do you agree?" with ✗ / ✓ (voters) or "The others are voting…" (who placed).
//    Replaces VotingManager's old voting panel.
//  - Player end card: when the GM ends the game, players rate the game objective and their own (0-10 number
//    buttons), say if their objective changed, and send; then "Thanks" with who has answered.
//  - Phase intro: when a phase starts, a full-screen title over the phase's drawer gradient with a fan of
//    its pieces opening in the middle ("PHASE 1 OF 3 · Instruments phase").
// Replaces the old GM side panel and its End Turn button (hidden, kept in the scene; their button wiring
// is copied onto the new buttons). Also rebuilds the player capsules first (for their "NEXT" tag).
// Safe to run again.
public static class MainHudBuilder
{
    private const string ScenePath = "Assets/Scenes/MainGame.unity";
    private const string OldPanelName = "-------- GAME MASTER ACTION PANEL -------------";

    private const float RibbonH = 68f;
    private const float GapUnderCapsules = 8f;
    private const float BoardGap = 18f;            // between the ribbon and the board's top row
    private const float BottomGap = 12f;           // between the board and the bottom row of capsules
    private static float RibbonTop = 86f; // measured from the top row of capsules in Build()
    private const float PanelW = 440f;
    private const float Hidden = 30f;     // part of the GM panel kept off screen (square left edge)
    private const float TabW = 52f, TabTuck = 24f;
    private const float DrawerGap = 14f;  // between the GM drawer and the rows of capsules

    private static readonly Color Purple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color PurpleMid = new Color32(0x6A, 0x55, 0xA8, 0xFF);
    private static readonly Color Lilac = new Color32(0xF1, 0xEC, 0xFB, 0xFF);
    private static readonly Color LilacLine = new Color32(0xEE, 0xEA, 0xF5, 0xFF);
    private static readonly Color Pink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color PinkText = new Color32(0xC2, 0x18, 0x7A, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x5E, 0x55, 0x75, 0xFF);
    private static readonly Color Grey = new Color32(0x9A, 0x92, 0xAC, 0xFF);

    [MenuItem("BioValue/Build Turn Ribbon and GM Panel")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Build Turn Ribbon and GM Panel", "Stop Play mode first: the builder edits the scene and prefabs.", "OK");
            return;
        }
        if (!LoadArt())
        {
            EditorUtility.DisplayDialog("Build Turn Ribbon and GM Panel",
                "Missing art or font. Run BioValue/Build Instrument Piece and BioValue/Build Action Card first.", "OK");
            return;
        }

        PlayerCapsuleBuilder.Build(); // capsules with the NEXT tag; opens and saves MainGame

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) return; // the capsule builder reported the problem

        Canvas canvas = null;
        foreach (var hook in Object.FindObjectsByType<PlayerCapsuleHook>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (hook.gameObject.scene == scene) { canvas = hook.GetComponentInParent<Canvas>(true); break; }
        if (!canvas) return;
        var root = (RectTransform)canvas.rootCanvas.transform;

        // The old voting panel (approve / reject buttons), replaced by the vote bubble.
        foreach (var t in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray())
            if (t && t.name == "---------------- VOTING BUTTONS ----------------")
                Object.DestroyImmediate(t.gameObject);

        foreach (var name in new[] { "TurnRibbon", "GameMasterHud", "CollaborationRound", "PhaseIntro", "VoteBubble", "PlayerEndCard", "GmActionsDrawer", "ResultsCard", "GameNotice" })
        {
            Transform t;
            while ((t = root.Find(name)) != null) Object.DestroyImmediate(t.gameObject);
        }

        // Old GM panel: keep its buttons (hidden) as the source of the scene wiring.
        var oldPanel = root.Find(OldPanelName);
        Button OldButton(string path) => oldPanel ? oldPanel.Find(path)?.GetComponent<Button>() : null;
        var oldEndInstruments = OldButton("Panel/Group/End Instruments Round");
        var oldShowActions = OldButton("Panel/Group/Show all actions");
        var oldEndActions = OldButton("Panel/Group/End Actions Round");
        var oldShowResults = OldButton("Panel/Group/ShowAll");
        var oldSaveResults = OldButton("Panel/Group/ShowAll (1)");

        // Above the board, tray and drawers; below the details windows.
        int index = root.childCount;
        foreach (var name in new[] { "InstrumentDetailsPanel", "PlayerDetailsPanel" })
        {
            var t = root.Find(name);
            if (t) index = Mathf.Min(index, t.GetSiblingIndex());
        }

        RibbonTop = TopCapsulesBottom(root) + GapUnderCapsules;
        MoveBoardBelowRibbon(root, RibbonTop + RibbonH + BoardGap);
        var gameState = Object.FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
        if (!gameState) Debug.LogWarning("[MainHudBuilder] No GameState in the scene: \"End actions round\" has no wiring.");
        var ribbon = BuildRibbon(root, BoardCentreX(root), oldEndActions, gameState);
        ribbon.transform.SetSiblingIndex(index);
        var hud = BuildGmHud(root, oldEndInstruments, oldShowActions, oldEndActions, oldShowResults, oldSaveResults, gameState);
        hud.transform.SetSiblingIndex(index + 1);

        // Collaboration round: under the ribbon, the GM drawer and the game log.
        var round = BuildCollaborationRound(root);
        int below = ribbon.transform.GetSiblingIndex();
        var log = root.Find("GameLogDrawer");
        if (log) below = Mathf.Min(below, log.GetSiblingIndex());
        round.transform.SetSiblingIndex(below);
        BuildVoteBubble(root).transform.SetSiblingIndex(ribbon.transform.GetSiblingIndex()); // over the board, under the ribbon

        ActionDrawerBuilder.BuildGmDrawer(root).transform.SetAsLastSibling();
        BuildResultsCard(root, oldSaveResults).transform.SetAsLastSibling();
        BuildPlayerEndCard(root).transform.SetAsLastSibling();
        BuildPhaseIntro(root).transform.SetAsLastSibling(); // over everything
        BuildGameNotice(root).transform.SetAsLastSibling(); // "The game master ended the game", over the intro too

        if (oldPanel)
        {
            var actions = oldPanel.GetComponent<GameMasterActions>();
            if (actions)
            {
                var so = new SerializedObject(actions);
                so.FindProperty("endTurnButton").objectReferenceValue = null; // would switch the old button back on
                so.ApplyModifiedPropertiesWithoutUndo();
                hud.actions = actions;
            }
            foreach (var name in new[] { "Panel", "EndTurnButton" })
            {
                var t = oldPanel.Find(name);
                if (t) t.gameObject.SetActive(false);
            }
        }
        else Debug.LogWarning("[MainHudBuilder] Old GM panel not found: the phase and results buttons have no wiring.");

        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MainHudBuilder] Turn ribbon and GM panel built in MainGame (scene saved).");
    }

    // Measured with the top row of capsules: its vertical centre (from the top) and its left edge.
    private static float topRowCentre = 42f, topRowLeft = 450f;

    // Distance from the top of the screen to the bottom edge of the top row of capsules.
    private static float TopCapsulesBottom(RectTransform root)
    {
        float bottom = 0f;
        float left = float.MaxValue, centre = -1f;
        var corners = new Vector3[4];
        foreach (var hook in Object.FindObjectsByType<PlayerCapsuleHook>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // Skips the old, switched-off capsules still lying around in the scene.
            if (hook.gameObject.scene != root.gameObject.scene || !hook.gameObject.activeInHierarchy) continue;
            ((RectTransform)hook.transform).GetWorldCorners(corners);
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var c in corners)
            {
                float y = root.InverseTransformPoint(c).y;
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
            if ((minY + maxY) / 2f < root.rect.center.y) continue; // bottom row
            bottom = Mathf.Max(bottom, root.rect.yMax - minY);
            centre = root.rect.yMax - (minY + maxY) / 2f;
            foreach (var c in corners) left = Mathf.Min(left, root.InverseTransformPoint(c).x - root.rect.xMin);
        }
        if (centre > 0f) topRowCentre = centre;
        if (left < float.MaxValue) topRowLeft = left;
        return bottom > 0f ? bottom : 68f;
    }

    // Moves the board down so its top row clears the ribbon, but not into the bottom row of capsules.
    // The pieces live under the same parent as the cells, so they move with it.
    private static void MoveBoardBelowRibbon(RectTransform root, float minTopFromTop)
    {
        RectTransform board = null;
        float top = float.MinValue, bottom = float.MaxValue;
        var corners = new Vector3[4];
        foreach (var cell in Object.FindObjectsByType<BoardCell>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cell.gameObject.scene != root.gameObject.scene) continue;
            board = cell.transform.parent as RectTransform;
            ((RectTransform)cell.transform).GetWorldCorners(corners);
            foreach (var c in corners)
            {
                float y = root.InverseTransformPoint(c).y;
                top = Mathf.Max(top, y);
                bottom = Mathf.Min(bottom, y);
            }
        }
        if (!board) return;

        float topFromTop = root.rect.yMax - top;
        float shift = minTopFromTop - topFromTop;
        if (shift <= 0f) return;

        float limit = BottomCapsulesTop(root) - BottomGap - (root.rect.yMax - bottom); // room left under the board
        if (limit < shift)
        {
            Debug.LogWarning($"[MainHudBuilder] The board needs {shift:0} px to clear the ribbon but only has {Mathf.Max(0f, limit):0} px above the bottom capsules.");
            shift = Mathf.Max(0f, limit);
        }
        board.position += root.TransformVector(new Vector3(0f, -shift, 0f));
        EditorUtility.SetDirty(board);
        Debug.Log($"[MainHudBuilder] Moved the board down {shift:0} px to clear the turn ribbon.");
    }

    // Distance from the top of the screen to the top edge of the bottom row of capsules.
    private static float BottomCapsulesTop(RectTransform root)
    {
        float best = root.rect.height;
        var corners = new Vector3[4];
        foreach (var hook in Object.FindObjectsByType<PlayerCapsuleHook>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hook.gameObject.scene != root.gameObject.scene || !hook.gameObject.activeInHierarchy) continue;
            ((RectTransform)hook.transform).GetWorldCorners(corners);
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var c in corners)
            {
                float y = root.InverseTransformPoint(c).y;
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
            if ((minY + maxY) / 2f >= root.rect.center.y) continue; // top row
            best = Mathf.Min(best, root.rect.yMax - maxY);
        }
        return best;
    }

    // Horizontal centre of the board cells, in the canvas' space (the ribbon sits over the board).
    private static float BoardCentreX(RectTransform root)
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (var cell in Object.FindObjectsByType<BoardCell>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cell.gameObject.scene != root.gameObject.scene) continue;
            float x = root.InverseTransformPoint(cell.transform.position).x;
            min = Mathf.Min(min, x);
            max = Mathf.Max(max, x);
        }
        return min <= max ? (min + max) / 2f - root.rect.center.x : -358f;
    }

    // ── Turn ribbon ──────────────────────────────────────────────────────────

    // "End actions round" starts the collaboration round (GameState.SetGamePhaseByIndex(3)).
    private static void WireStartCollaboration(Button button, GameState gameState)
    {
        if (!button || !gameState) return;
        UnityEventTools.AddIntPersistentListener(button.onClick, new UnityAction<int>(gameState.SetGamePhaseByIndex),
            (int)GameState.GamePhase.Collaboration);
    }

    private static TurnRibbon BuildRibbon(RectTransform root, float centreX, Button oldEndActions, GameState gameState)
    {
        var holder = Stretch(NewRect("TurnRibbon", root));
        var ribbon = holder.gameObject.AddComponent<TurnRibbon>();

        var pill = NewRect("Pill", holder);
        pill.anchorMin = pill.anchorMax = pill.pivot = new Vector2(0.5f, 1f);
        pill.anchoredPosition = new Vector2(centreX, -RibbonTop);
        pill.sizeDelta = new Vector2(600, RibbonH);
        var pillImg = Round(pill, Color.white, RibbonH / 2f);
        pillImg.raycastTarget = true;
        var shadow = pill.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.06f, 0.03f, 0.16f, 0.28f);
        shadow.effectDistance = new Vector2(0, -8);
        pill.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var layout = Row(pill, 0, new RectOffset(0, 0, 0, 0));
        layout.childForceExpandHeight = true;
        pill.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Now
        var now = NewRect("Now", pill);
        var nowRow = Row(now, 12, new RectOffset(9, 26, 0, 0));
        nowRow.childAlignment = TextAnchor.MiddleLeft;
        var avatar = NewRect("Avatar", now);
        avatar.sizeDelta = new Vector2(50, 50);
        Fixed(avatar, 50, 50);
        var avatarImg = PlayerCapsuleBuilder.Circle(avatar, PlayerCapsuleHook.PlayerColors[0], out var circle);
        var person = Stretch(NewRect("Person", circle));
        person.offsetMin = new Vector2(8, 0);
        person.offsetMax = new Vector2(-8, -8);
        PlayerCapsuleBuilder.Icon(person, PlayerCapsuleBuilder.GetOrCreatePerson(), Color.white);
        var cards = Stretch(NewRect("Cards", circle));
        foreach (var (angle, x) in new[] { (-12f, -5f), (10f, 5f) })
        {
            var card = NewRect("Card", cards);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(17, 24);
            card.anchoredPosition = new Vector2(x, 0);
            card.localRotation = Quaternion.Euler(0, 0, angle);
            Round(card, Color.white, 3);
        }
        cards.gameObject.SetActive(false);
        var tick = Tick(circle, Color.white, 1f);
        tick.gameObject.SetActive(false);
        var cross = Cross(circle, Color.white, 1f);
        cross.gameObject.SetActive(false);

        var texts = NewRect("Texts", now);
        Column(texts, -2, new RectOffset(0, 0, 0, 0));
        var eyebrow = Label(texts, "Eyebrow", bold, 15, PinkText, "NOW");
        eyebrow.characterSpacing = 6;
        var nowText = Label(texts, "Text", bold, 23, Ink, "Ana is placing");

        var vote = NewRect("Vote", now);
        var voteRow = Row(vote, 5, new RectOffset(6, 0, 0, 0));
        voteRow.childAlignment = TextAnchor.MiddleLeft;
        var dots = NewRect("Dots", vote);
        Row(dots, 5, new RectOffset(0, 0, 0, 0)).childAlignment = TextAnchor.MiddleLeft;
        var dot = NewRect("Dot", dots);
        Fixed(dot, 15, 15);
        Round(dot, Grey, 7.5f);
        var dotRing = dot.gameObject.AddComponent<Outline>(); // keeps the dots readable on the green/red result
        dotRing.effectColor = new Color(1, 1, 1, 0.9f);
        dotRing.effectDistance = new Vector2(1.5f, -1.5f);
        var voteText = Label(vote, "Count", regular, 17, Muted, "0 of 6 voted");
        vote.gameObject.SetActive(false);

        // Next
        var next = NewRect("Next", pill);
        var nextFill = next.gameObject.AddComponent<Image>();
        nextFill.color = new Color32(0xF7, 0xF4, 0xFC, 0xFF);
        nextFill.raycastTarget = false;
        var nextRow = Row(next, 10, new RectOffset(22, 24, 0, 0));
        nextRow.childAlignment = TextAnchor.MiddleLeft;
        var nextEyebrow = Label(next, "Eyebrow", bold, 15, PurpleMid, "NEXT");
        nextEyebrow.characterSpacing = 6;
        var nextAvatar = NewRect("Avatar", next);
        Fixed(nextAvatar, 34, 34);
        var nextAvatarImg = Round(nextAvatar, PlayerCapsuleHook.PlayerColors[1], 17);
        var nextNumber = Txt(Stretch(NewRect("Number", nextAvatar)), bold, 18, Color.white, TextAlignmentOptions.Center);
        nextNumber.textWrappingMode = TextWrappingModes.NoWrap;
        nextNumber.text = "2";
        var nextName = Label(next, "Name", bold, 22, Ink, "Bruno");
        var divider = NewRect("Divider", next);
        divider.anchorMin = new Vector2(0, 0);
        divider.anchorMax = new Vector2(0, 1);
        divider.pivot = new Vector2(0, 0.5f);
        divider.sizeDelta = new Vector2(2, 0);
        divider.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        divider.gameObject.AddComponent<Image>().color = new Color32(0xE6, 0xE0, 0xF2, 0xFF);

        // GM buttons
        var endTurn = PinkButton(pill, "EndTurn", "End turn", out var endTurnLabel);
        UnityEventTools.AddPersistentListener(endTurn.GetComponentInChildren<Button>().onClick, new UnityAction(ribbon.EndTurn));
        var endActions = PinkButton(pill, "EndActions", "End actions round", out _);
        WireStartCollaboration(endActions.GetComponentInChildren<Button>(), gameState);
        endActions.gameObject.SetActive(false);
        var endCollab = PinkButton(pill, "EndCollaboration", "End collaboration", out _);
        CopyOnClick(oldEndActions, endCollab.GetComponentInChildren<Button>());
        endCollab.gameObject.SetActive(false);
        var done = PinkButton(pill, "Done", "I’m done", out var doneLabel);
        var doneButton = done.GetComponentInChildren<Button>();
        done.Find("Button/Arrow").gameObject.SetActive(false);
        UnityEventTools.AddPersistentListener(doneButton.onClick, new UnityAction(ribbon.ToggleDone));
        done.gameObject.SetActive(false);

        // Vote result stamp, moved onto the piece when a vote closes.
        var stampHolder = NewRect("VoteStamp", holder);
        stampHolder.anchorMin = stampHolder.anchorMax = stampHolder.pivot = new Vector2(0.5f, 0.5f);
        stampHolder.sizeDelta = Vector2.zero;
        stampHolder.SetAsFirstSibling(); // under the ribbon
        var stampGroup = stampHolder.gameObject.AddComponent<CanvasGroup>();
        stampGroup.blocksRaycasts = false;
        stampGroup.interactable = false;
        var stamp = NewRect("Stamp", stampHolder);
        stamp.anchorMin = stamp.anchorMax = stamp.pivot = new Vector2(0.5f, 0.5f);
        stamp.sizeDelta = new Vector2(184, 50);
        stamp.localRotation = Quaternion.Euler(0, 0, 8);
        var stampBorder = Round(stamp, new Color32(0x2F, 0xA8, 0x4F, 0xFF), 11);
        var stampFace = Stretch(NewRect("Face", stamp));
        stampFace.offsetMin = new Vector2(4, 4);
        stampFace.offsetMax = new Vector2(-4, -4);
        Round(stampFace, new Color(1, 1, 1, 0.95f), 8);
        var stampLabel = Txt(Stretch(NewRect("Label", stamp)), bold, 27, new Color32(0x2F, 0xA8, 0x4F, 0xFF), TextAlignmentOptions.Center);
        stampLabel.characterSpacing = 6;
        stampLabel.textWrappingMode = TextWrappingModes.NoWrap;
        stampLabel.text = "APPROVED";
        var tally = NewRect("Tally", stampHolder);
        tally.anchorMin = tally.anchorMax = new Vector2(0.5f, 0.5f);
        tally.pivot = new Vector2(0.5f, 1f);
        tally.sizeDelta = new Vector2(150, 32);
        Round(tally, Color.white, 16);
        var tallyShadow = tally.gameObject.AddComponent<Shadow>();
        tallyShadow.effectColor = new Color(0.06f, 0.03f, 0.16f, 0.25f);
        tallyShadow.effectDistance = new Vector2(0, -4);
        var tallyLabel = Txt(Stretch(NewRect("Label", tally)), bold, 17, Ink, TextAlignmentOptions.Center);
        tallyLabel.textWrappingMode = TextWrappingModes.NoWrap;
        tallyLabel.text = "4 yes \u00b7 1 no";
        stampHolder.gameObject.SetActive(false);

        ribbon.pill = pill;
        ribbon.pillImage = pillImg;
        ribbon.resultTick = tick.gameObject;
        ribbon.resultCross = cross.gameObject;
        ribbon.stampHolder = stampHolder;
        ribbon.stamp = stamp;
        ribbon.stampGroup = stampGroup;
        ribbon.stampBorder = stampBorder;
        ribbon.stampLabel = stampLabel;
        ribbon.tally = tally;
        ribbon.tallyLabel = tallyLabel;
        ribbon.nowAvatar = avatarImg;
        ribbon.nowPerson = person.gameObject;
        ribbon.nowCards = cards.gameObject;
        ribbon.nowEyebrow = eyebrow;
        ribbon.nowText = nowText;
        ribbon.voteGroup = vote.gameObject;
        ribbon.voteDots = dots;
        ribbon.voteDotTemplate = dot.gameObject;
        ribbon.voteText = voteText;
        ribbon.nextGroup = next.gameObject;
        ribbon.nextFill = nextFill;
        ribbon.nextAvatar = nextAvatarImg;
        ribbon.nextNumber = nextNumber;
        ribbon.nextName = nextName;
        ribbon.endTurnButton = endTurn.gameObject;
        ribbon.endTurnLabel = endTurnLabel;
        ribbon.endActionsButton = endActions.gameObject;
        ribbon.endCollabButton = endCollab.gameObject;
        ribbon.doneButton = done.gameObject;
        ribbon.doneButtonImage = doneButton.GetComponent<Image>();
        ribbon.doneLabel = doneLabel;
        EditorUtility.SetDirty(ribbon);
        return ribbon;
    }

    // Pink rounded button with a label and a chevron, inset in the ribbon.
    private static RectTransform PinkButton(RectTransform pill, string name, string text, out TextMeshProUGUI label)
    {
        var wrap = NewRect(name, pill);
        Row(wrap, 0, new RectOffset(0, 7, 7, 7)).childForceExpandHeight = true;
        var rt = NewRect("Button", wrap);
        var img = Round(rt, Pink, (RibbonH - 14) / 2f);
        img.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        Row(rt, 8, new RectOffset(26, 20, 0, 0)).childAlignment = TextAnchor.MiddleCenter;
        label = Label(rt, "Label", bold, 22, Color.white, text);
        var arrow = NewRect("Arrow", rt);
        Fixed(arrow, 18, 18);
        var arrowImg = arrow.gameObject.AddComponent<Image>();
        arrowImg.sprite = ActionDrawerBuilder.GetOrCreateChevron();
        arrowImg.color = Color.white;
        arrowImg.raycastTarget = false;
        return wrap;
    }

    // ── GM button + panel ────────────────────────────────────────────────────

    private static GameMasterHud BuildGmHud(RectTransform root, Button oldEndInstruments, Button oldShowActions,
                                            Button oldEndActions, Button oldShowResults, Button oldSaveResults,
                                            GameState gameState)
    {
        var holder = Stretch(NewRect("GameMasterHud", root));
        var hud = holder.gameObject.AddComponent<GameMasterHud>();
        var content = Stretch(NewRect("Content", holder));

        // Dim behind the open drawer; a click on it closes the drawer.
        var dim = Stretch(NewRect("Dim", content));
        var dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0.08f, 0.055f, 0.16f, 0.25f);
        var dimButton = dim.gameObject.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;
        dim.gameObject.SetActive(false);

        // Drawer: full height between the two rows of capsules, against the left edge of the screen.
        // Its left 30 px sit off screen so only the right corners show rounded.
        float drawerTop = RibbonTop - GapUnderCapsules + DrawerGap;
        float drawerBottom = BottomCapsulesTop(root) - DrawerGap;
        float drawerH = Mathf.Max(700f, drawerBottom - drawerTop);
        var drawer = NewRect("Drawer", content);
        drawer.anchorMin = drawer.anchorMax = drawer.pivot = new Vector2(0, 1);
        drawer.sizeDelta = new Vector2(PanelW, drawerH);
        drawer.anchoredPosition = new Vector2(-PanelW, -drawerTop); // starts closed

        // Tab on the drawer's right edge (behind the panel, so its left corners hide under it).
        var tab = NewRect("Tab", drawer);
        tab.anchorMin = tab.anchorMax = new Vector2(1, 0.5f);
        tab.pivot = new Vector2(0, 0.5f);
        tab.anchoredPosition = new Vector2(-TabTuck, 0);
        tab.sizeDelta = new Vector2(TabW + TabTuck, 240);
        var tabImg = Round(tab, Purple, 16);
        tabImg.raycastTarget = true;
        var tabShadow = tab.gameObject.AddComponent<Shadow>();
        tabShadow.effectColor = new Color(0.06f, 0.03f, 0.16f, 0.3f);
        tabShadow.effectDistance = new Vector2(4, -6);
        var tabButton = tab.gameObject.AddComponent<Button>();
        tabButton.targetGraphic = tabImg;
        var tabLabel = Txt(NewRect("Label", tab), bold, 18, Color.white, TextAlignmentOptions.Center);
        tabLabel.rectTransform.anchorMin = tabLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        tabLabel.rectTransform.sizeDelta = new Vector2(190, TabW);
        tabLabel.rectTransform.anchoredPosition = new Vector2(TabTuck / 2f, -12);
        tabLabel.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
        tabLabel.characterSpacing = 8;
        tabLabel.textWrappingMode = TextWrappingModes.NoWrap;
        tabLabel.text = "GAME MASTER";
        var tabArrow = NewRect("Arrow", tab);
        tabArrow.anchorMin = tabArrow.anchorMax = new Vector2(0.5f, 1f);
        tabArrow.sizeDelta = new Vector2(18, 18);
        tabArrow.anchoredPosition = new Vector2(TabTuck / 2f, -24);
        var arrowImg = tabArrow.gameObject.AddComponent<Image>();
        arrowImg.sprite = ActionDrawerBuilder.GetOrCreateChevron(); // points right = "open me"
        arrowImg.color = Color.white;
        arrowImg.raycastTarget = false;

        // Panel
        var panel = Stretch(NewRect("Panel", drawer));
        panel.offsetMin = new Vector2(-Hidden, 0);
        var panelImg = Round(panel, Color.white, 26);
        panelImg.raycastTarget = true;
        var panelShadow = panel.gameObject.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0.04f, 0.02f, 0.1f, 0.35f);
        panelShadow.effectDistance = new Vector2(6, -10);
        panel.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        Column(panel, 0, new RectOffset(0, 0, 0, 0));

        var header = NewRect("Header", panel);
        Fixed(header, -1, 88);
        var headerImg = header.gameObject.AddComponent<Image>();
        headerImg.color = Purple;
        headerImg.raycastTarget = false;
        var eyebrow = Txt(Place(NewRect("Eyebrow", header), Hidden + 24, 16, PanelW - 48, 20), bold, 15, new Color(1, 1, 1, 0.8f), TextAlignmentOptions.TopLeft);
        eyebrow.characterSpacing = 6;
        eyebrow.text = "GAME MASTER";
        var title = Txt(Place(NewRect("Title", header), Hidden + 24, 38, PanelW - 48, 36), bold, 28, Color.white, TextAlignmentOptions.TopLeft);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.text = "Instruments round";

        var body = NewRect("Body", panel);
        Column(body, 10, new RectOffset((int)Hidden + 24, 24, 18, 24));
        body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1; // fills the drawer

        // Phase
        Section(body, "GAME PHASE");
        var stepper = NewRect("Stepper", body);
        Fixed(stepper, -1, 72);
        const int Steps = 4;
        const float colW = (PanelW - 48) / Steps;
        var line = NewRect("Line", stepper);
        line.anchorMin = line.anchorMax = new Vector2(0, 1);
        line.pivot = new Vector2(0, 0.5f);
        line.anchoredPosition = new Vector2(colW / 2f, -19);
        line.sizeDelta = new Vector2(colW * (Steps - 1), 3);
        line.gameObject.AddComponent<Image>().color = LilacLine;
        var stepDots = new Image[Steps];
        var stepMarks = new TMP_Text[Steps];
        var stepTicks = new GameObject[Steps];
        var stepLabels = new TMP_Text[Steps];
        string[] names = { "Instruments", "Action cards", "Collaboration", "Results" };
        for (int i = 0; i < Steps; i++)
        {
            var d = NewRect("Step" + (i + 1), stepper);
            d.anchorMin = d.anchorMax = new Vector2(0, 1);
            d.pivot = new Vector2(0.5f, 1);
            d.anchoredPosition = new Vector2(colW * (i + 0.5f), 0);
            d.sizeDelta = new Vector2(38, 38);
            stepDots[i] = Round(d, i == 0 ? Pink : LilacLine, 19);
            stepMarks[i] = Txt(Stretch(NewRect("Mark", d)), bold, 18, i == 0 ? Color.white : Grey, TextAlignmentOptions.Center);
            stepMarks[i].text = (i + 1).ToString();
            stepTicks[i] = Tick(d, Color.white, 0.8f).gameObject;
            stepTicks[i].SetActive(false);
            stepLabels[i] = Txt(Place(NewRect("Label" + (i + 1), stepper), colW * i, 44, colW, 26), regular, 15, i == 0 ? Ink : Grey, TextAlignmentOptions.Top);
            stepLabels[i].textWrappingMode = TextWrappingModes.NoWrap;
            stepLabels[i].text = names[i];
        }
        var endInstruments = WideButton(body, "EndInstruments", "End instruments round", Purple, Color.white);
        CopyOnClick(oldEndInstruments, endInstruments);
        var endActions = WideButton(body, "EndActions", "End actions round", Purple, Color.white);
        WireStartCollaboration(endActions, gameState);
        endActions.gameObject.SetActive(false);
        var endCollab = WideButton(body, "EndCollaboration", "End collaboration", Purple, Color.white);
        CopyOnClick(oldEndActions, endCollab);
        endCollab.gameObject.SetActive(false);
        // Only during the action cards round; opens the GM's "Everyone's actions" drawer (players never see it).
        var showActions = WideButton(body, "ShowAllActions", "See everyone’s actions", Lilac, Purple);
        var showLabel = showActions.GetComponentInChildren<TextMeshProUGUI>();
        showLabel.alignment = TextAlignmentOptions.MidlineLeft;
        showLabel.rectTransform.offsetMin = new Vector2(24, 0);
        var gmOnly = NewRect("GmOnlyTag", (RectTransform)showActions.transform);
        gmOnly.anchorMin = gmOnly.anchorMax = gmOnly.pivot = new Vector2(1, 0.5f);
        gmOnly.anchoredPosition = new Vector2(-12, 0);
        gmOnly.sizeDelta = new Vector2(104, 28);
        Round(gmOnly, Pink, 14);
        var gmOnlyLabel = Txt(Stretch(NewRect("Label", gmOnly)), bold, 14, Color.white, TextAlignmentOptions.Center);
        gmOnlyLabel.characterSpacing = 4;
        gmOnlyLabel.textWrappingMode = TextWrappingModes.NoWrap;
        gmOnlyLabel.text = "ONLY YOU";
        showActions.gameObject.SetActive(false);

        // Players
        Section(body, "PLAYERS");
        var rows = NewRect("Players", body);
        Column(rows, 0, new RectOffset(0, 0, 0, 0));
        var row = BuildPlayerRow(rows);

        // Results, at the bottom of the drawer
        NewRect("Spacer", body).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
        Section(body, "RESULTS");
        var results = NewRect("Results", body);
        var resultsRow = Row(results, 10, new RectOffset(0, 0, 0, 0));
        resultsRow.childForceExpandWidth = true;
        Fixed(results, -1, 50);
        var showResults = WideButton(results, "ShowResults", "Show to everyone", Lilac, Purple);
        CopyOnClick(oldShowResults, showResults);
        var saveResults = WideButton(results, "SaveResults", "Save results", Lilac, Purple);
        CopyOnClick(oldSaveResults, saveResults);

        // Quit, with a confirmation in its place.
        var quitHolder = NewRect("Quit", body);
        Fixed(quitHolder, -1, 50);
        var quit = WideButton(quitHolder, "QuitButton", "Quit game", Color.white, VoteNo);
        Stretch((RectTransform)quit.transform);
        var quitOutline = quit.gameObject.AddComponent<Outline>();
        quitOutline.effectColor = VoteNo;
        quitOutline.effectDistance = new Vector2(1.5f, -1.5f);
        var confirm = Stretch(NewRect("QuitConfirm", quitHolder));
        var confirmRow = Row(confirm, 8, new RectOffset(4, 0, 0, 0));
        var ask = Label(confirm, "Ask", bold, 18, Ink, "Quit the game?");
        ask.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var cancel = WideButton(confirm, "Cancel", "Cancel", Lilac, Purple);
        Fixed((RectTransform)cancel.transform, 110, 50);
        var quitYes = WideButton(confirm, "ConfirmQuit", "Quit", VoteNo, Color.white);
        Fixed((RectTransform)quitYes.transform, 100, 50);
        confirm.gameObject.SetActive(false);

        hud.content = content.gameObject;
        hud.drawer = drawer;
        hud.panel = panel;
        hud.dim = dim.gameObject;
        hud.tabArrow = tabArrow;
        hud.title = title;
        hud.stepDots = stepDots;
        hud.stepMarks = stepMarks;
        hud.stepTicks = stepTicks;
        hud.stepLabels = stepLabels;
        hud.endInstrumentsButton = endInstruments.gameObject;
        hud.endActionsButton = endActions.gameObject;
        hud.endCollabButton = endCollab.gameObject;
        hud.showActionsButton = showActions.gameObject;
        hud.quitButton = quit.gameObject;
        hud.quitConfirm = confirm.gameObject;
        UnityEventTools.AddPersistentListener(quit.onClick, new UnityAction(hud.AskQuit));
        UnityEventTools.AddPersistentListener(cancel.onClick, new UnityAction(hud.CancelQuit));
        UnityEventTools.AddPersistentListener(quitYes.onClick, new UnityAction(hud.ConfirmQuit));
        hud.rows = rows;
        hud.rowTemplate = row;
        UnityEventTools.AddPersistentListener(tabButton.onClick, new UnityAction(hud.Toggle));
        UnityEventTools.AddPersistentListener(dimButton.onClick, new UnityAction(hud.Close));
        UnityEventTools.AddPersistentListener(showActions.onClick, new UnityAction(hud.OpenAllActions));
        return hud;
    }

    // ── Vote bubble ──────────────────────────────────────────────────────────

    private static readonly Color VoteYes = new Color32(0x2F, 0xA8, 0x4F, 0xFF);
    private static readonly Color VoteNo = new Color32(0xD9, 0x3A, 0x3A, 0xFF);

    private static VoteBubble BuildVoteBubble(RectTransform root)
    {
        var holder = Stretch(NewRect("VoteBubble", root));
        var view = holder.gameObject.AddComponent<VoteBubble>();
        var content = Stretch(NewRect("Content", holder));

        var ring = NewRect("Ring", content);
        ring.anchorMin = ring.anchorMax = ring.pivot = new Vector2(0.5f, 0.5f);
        ring.sizeDelta = new Vector2(120, 138);
        var ringImg = ring.gameObject.AddComponent<Image>();
        ringImg.sprite = GetOrCreateHexRing();
        ringImg.color = Pink;
        ringImg.raycastTarget = false;

        var tag = NewRect("VotingTag", content);
        tag.anchorMin = tag.anchorMax = new Vector2(0.5f, 0.5f);
        tag.pivot = new Vector2(0.5f, 0f);
        tag.sizeDelta = new Vector2(104, 28);
        Round(tag, Pink, 14);
        var tagLabel = Txt(Stretch(NewRect("Label", tag)), bold, 17, Color.white, TextAlignmentOptions.Center);
        tagLabel.characterSpacing = 6;
        tagLabel.textWrappingMode = TextWrappingModes.NoWrap;
        tagLabel.text = "VOTING";

        var bubble = NewRect("Bubble", content);
        bubble.anchorMin = bubble.anchorMax = new Vector2(0.5f, 0.5f);
        bubble.pivot = new Vector2(0f, 0.5f);
        var bubbleImg = Round(bubble, Color.white, 44);
        bubbleImg.raycastTarget = true;
        var bubbleShadow = bubble.gameObject.AddComponent<Shadow>();
        bubbleShadow.effectColor = new Color(0.04f, 0.02f, 0.12f, 0.35f);
        bubbleShadow.effectDistance = new Vector2(0, -10);
        var bubbleRow = Row(bubble, 0, new RectOffset(28, 14, 12, 12));
        bubbleRow.childControlHeight = true;
        var fit = bubble.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var arrow = NewRect("Arrow", bubble);
        arrow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        arrow.anchorMin = arrow.anchorMax = new Vector2(0f, 0.5f);
        arrow.pivot = new Vector2(0.5f, 0.5f);
        arrow.sizeDelta = new Vector2(24, 24);
        arrow.localRotation = Quaternion.Euler(0, 0, 45);
        Round(arrow, Color.white, 3);

        var voter = NewRect("Voter", bubble);
        var voterRow = Row(voter, 16, new RectOffset(0, 0, 0, 0));
        voterRow.childControlHeight = true;
        Label(voter, "Question", bold, 28, Ink, "Do you agree?");
        var no = VoteButton(voter, "No", VoteNo, false);
        var yes = VoteButton(voter, "Yes", VoteYes, true);

        var waiting = NewRect("Waiting", bubble);
        Row(waiting, 0, new RectOffset(0, 14, 10, 10)).childControlHeight = true;
        Label(waiting, "Text", bold, 26, Ink, "The others are voting…");
        waiting.gameObject.SetActive(false);

        content.gameObject.SetActive(false);

        view.content = content.gameObject;
        view.ring = ring;
        view.votingTag = tag;
        view.bubble = bubble;
        view.arrow = arrow;
        view.voterGroup = voter.gameObject;
        view.waitingGroup = waiting.gameObject;
        view.noButton = no;
        view.yesButton = yes;
        EditorUtility.SetDirty(view);
        return view;
    }

    // Round button: coloured ring, white inside and a coloured ✗ / ✓ (filled in when chosen, see VoteBubble).
    private static Button VoteButton(RectTransform parent, string name, Color color, bool tick)
    {
        var rt = NewRect(name, parent);
        Fixed(rt, 72, 72);
        var img = Round(rt, color, 36);
        img.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var inner = Stretch(NewRect("Inner", rt));
        inner.offsetMin = new Vector2(4, 4);
        inner.offsetMax = new Vector2(-4, -4);
        Round(inner, Color.white, 32);
        var glyph = Stretch(NewRect("Glyph", rt));
        if (tick) Tick(glyph, color, 1.15f);
        else Cross(glyph, color, 1.15f);
        return button;
    }

    // Pointy-top hexagon outline (same shape as the pieces), made once into Assets/Images/InstrumentPiece/HexRing.png.
    private static Sprite GetOrCreateHexRing()
    {
        const string path = "Assets/Images/InstrumentPiece/HexRing.png";
        if (System.IO.File.Exists(path)) return SingleSprite(path);

        const int w = 208, h = 240, ss = 4;
        const float thickness = 0.085f; // of the width
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        bool Inside(float x, float y, float scale) // x, y from the centre, in widths
        {
            float hw = 0.5f * scale, hh = 0.5f * (h / (float)w) * scale;
            x = Mathf.Abs(x); y = Mathf.Abs(y);
            return x <= hw && y <= hh - (hh * 0.5f) * (x / hw);
        }
        for (int py = 0; py < h; py++)
        for (int px = 0; px < w; px++)
        {
            int hits = 0;
            for (int sy = 0; sy < ss; sy++)
            for (int sx = 0; sx < ss; sx++)
            {
                float x = (px + (sx + 0.5f) / ss) / w - 0.5f;
                float y = ((py + (sy + 0.5f) / ss) - h / 2f) / w;
                if (Inside(x, y, 1f) && !Inside(x, y, 1f - 2f * thickness)) hits++;
            }
            tex.SetPixel(px, py, new Color(1, 1, 1, hits / (float)(ss * ss)));
        }
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return SingleSprite(path);
    }

    // ── Game notice ──────────────────────────────────────────────────────────

    private static GameNotice BuildGameNotice(RectTransform root)
    {
        var holder = Stretch(NewRect("GameNotice", root));
        var view = holder.gameObject.AddComponent<GameNotice>();
        var content = Stretch(NewRect("Content", holder));
        var group = content.gameObject.AddComponent<CanvasGroup>();
        var dim = content.gameObject.AddComponent<Image>();
        dim.color = new Color(0.08f, 0.05f, 0.16f, 0.7f);
        dim.raycastTarget = true; // nothing else can be clicked while it shows

        var card = NewRect("Card", content);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(720, 300);
        CardLook(card);
        var col = Column(card, 10, new RectOffset(40, 40, 40, 36));
        col.childAlignment = TextAnchor.UpperCenter;
        col.childForceExpandWidth = false;

        var icon = NewRect("Icon", card);
        Fixed(icon, 84, 84);
        Round(icon, EndPurple, 42);
        PlayerCapsuleBuilder.Icon(Stretch(NewRect("Person", icon)), IntroSprite("IntroPersonInCircle", PersonInCircle), Color.white);
        var title = Label(card, "Title", bold, 34, Ink, "The game master ended the game");
        title.alignment = TextAlignmentOptions.Center;
        var body = Label(card, "Body", regular, 22, EndBody, "Going back to the main menu…");
        body.alignment = TextAlignmentOptions.Center;
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        content.gameObject.SetActive(false);
        view.content = content.gameObject;
        view.group = group;
        view.card = card;
        view.title = title;
        view.body = body;
        EditorUtility.SetDirty(view);
        return view;
    }

    // ── Results card ─────────────────────────────────────────────────────────

    private const float ResultsW = 1640f;
    // Columns of a row, from its left edge (the column titles use the same ones).
    private const float ColRank = 18, ColAvatar = 76, ColName = 140, ColGame = 500, ColOwn = 600, ColBar = 690;
    private const float ColBarRight = 300, ColScoreRight = 64;

    private static ResultsCard BuildResultsCard(RectTransform root, Button oldSaveResults)
    {
        var holder = Stretch(NewRect("ResultsCard", root));
        var view = holder.gameObject.AddComponent<ResultsCard>();
        var content = Stretch(NewRect("Content", holder));
        var dim = content.gameObject.AddComponent<Image>();
        dim.color = new Color(0.08f, 0.05f, 0.16f, 0.62f);
        dim.raycastTarget = true;

        var card = NewRect("Card", content);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(ResultsW, 980);
        CardLook(card);

        // Header: titles, the two averages, the formula.
        var header = NewRect("Header", card);
        header.anchorMin = new Vector2(0, 1); header.anchorMax = new Vector2(1, 1);
        header.pivot = new Vector2(0.5f, 1);
        header.sizeDelta = new Vector2(0, 150);
        header.anchoredPosition = Vector2.zero;
        Round(header, EndPurple, 36);
        var square = NewRect("SquareBottom", header);
        square.anchorMin = Vector2.zero; square.anchorMax = new Vector2(1, 0);
        square.pivot = new Vector2(0.5f, 0);
        square.sizeDelta = new Vector2(0, 40);
        var squareImg = square.gameObject.AddComponent<Image>();
        squareImg.color = EndPurple;
        squareImg.raycastTarget = false;
        var headRow = NewRect("Row", header);
        Stretch(headRow);
        Row(headRow, 18, new RectOffset(36, 28, 20, 20)).childForceExpandHeight = true;

        var titles = NewRect("Titles", headRow);
        Column(titles, -4, new RectOffset(0, 0, 8, 0));
        titles.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var eyebrow = Label(titles, "Eyebrow", bold, 17, new Color(1, 1, 1, 0.8f), "RESULTS · ONLY YOU SEE THIS");
        eyebrow.characterSpacing = 10;
        Label(titles, "Title", bold, 40, Color.white, "How the game went");
        var subtitle = Label(titles, "Subtitle", regular, 20, new Color(1, 1, 1, 0.8f), "0 of 0 players answered");

        var gameTile = ResultsTile(headRow, "GameAverage", "AVG · GAME OBJECTIVE", out var gameAvg, out var gameFill);
        var ownTile = ResultsTile(headRow, "OwnAverage", "AVG · PERSONAL OBJECTIVES", out var ownAvg, out var ownFill);
        var formulaTile = NewRect("Formula", headRow);
        Fixed(formulaTile, 440, -1);
        Round(formulaTile, new Color(1, 1, 1, 0.12f), 20);
        Column(formulaTile, 4, new RectOffset(18, 18, 12, 12));
        var formulaEye = Label(formulaTile, "Label", bold, 15, new Color(1, 1, 1, 0.8f), "THIS GAME’S FORMULA");
        formulaEye.characterSpacing = 6;
        var formula = Label(formulaTile, "Text", bold, 19, Color.white, "Score = …");
        formula.textWrappingMode = TextWrappingModes.Normal;

        // Column titles
        var cols = NewRect("Columns", card);
        cols.anchorMin = new Vector2(0, 1); cols.anchorMax = new Vector2(1, 1);
        cols.pivot = new Vector2(0.5f, 1);
        cols.offsetMin = new Vector2(24, 0); cols.offsetMax = new Vector2(-40, 0);
        cols.sizeDelta = new Vector2(cols.sizeDelta.x, 40);
        cols.anchoredPosition = new Vector2(-8, -158);
        ColumnTitle(cols, "#", ColRank, 44, TextAlignmentOptions.Center);
        ColumnTitle(cols, "PLAYER", ColAvatar, 300, TextAlignmentOptions.MidlineLeft);
        ColumnTitle(cols, "GAME OBJ.", ColGame - 20, 100, TextAlignmentOptions.Center);
        ColumnTitle(cols, "OWN OBJ.", ColOwn - 20, 100, TextAlignmentOptions.Center);
        ColumnTitle(cols, "HOW THE SCORE ADDS UP", ColBar, 400, TextAlignmentOptions.MidlineLeft);
        var scoreTitle = ColumnTitle(cols, "SCORE", 0, 140, TextAlignmentOptions.MidlineRight);
        var str = scoreTitle.rectTransform;
        str.anchorMin = str.anchorMax = str.pivot = new Vector2(1, 0.5f);
        str.anchoredPosition = new Vector2(-ColScoreRight, 0);

        // Rows (scroll)
        var scroll = NewRect("Scroll", card);
        Stretch(scroll);
        scroll.offsetMin = new Vector2(24, 104);
        scroll.offsetMax = new Vector2(-24, -200);
        var viewport = Stretch(NewRect("Viewport", scroll));
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0);
        var rows = NewRect("Rows", viewport);
        rows.anchorMin = new Vector2(0, 1); rows.anchorMax = new Vector2(1, 1);
        rows.pivot = new Vector2(0.5f, 1);
        rows.sizeDelta = Vector2.zero;
        var rowsCol = Column(rows, 8, new RectOffset(0, 16, 2, 8));
        rowsCol.childForceExpandWidth = true;
        rows.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var sr = scroll.gameObject.AddComponent<ScrollRect>();
        sr.content = rows;
        sr.viewport = viewport;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40;
        var rowTemplate = ResultsRow(rows);

        // Footer: legend + buttons
        var footer = NewRect("Footer", card);
        footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1, 0);
        footer.pivot = new Vector2(0.5f, 0);
        footer.sizeDelta = new Vector2(0, 96);
        footer.anchoredPosition = Vector2.zero;
        var line = NewRect("Line", footer);
        line.anchorMin = new Vector2(0, 1); line.anchorMax = new Vector2(1, 1);
        line.pivot = new Vector2(0.5f, 1);
        line.sizeDelta = new Vector2(0, 2);
        line.gameObject.AddComponent<Image>().color = EndIdle;
        var footRow = Stretch(NewRect("Row", footer));
        Row(footRow, 14, new RectOffset(36, 28, 18, 18));

        var legend = NewRect("Legend", footRow);
        Row(legend, 18, new RectOffset(0, 0, 0, 0)).childAlignment = TextAnchor.MiddleLeft;
        legend.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var legendItem = NewRect("Item", legend);
        Row(legendItem, 8, new RectOffset(0, 0, 0, 0));
        var legendDot = NewRect("Dot", legendItem);
        Fixed(legendDot, 16, 16);
        Round(legendDot, EndEyebrow, 5);
        Label(legendItem, "Label", regular, 17, EndBody, "Actions played");

        var save = WideButton(footRow, "SaveResults", "Save results", Lilac, Purple);
        Fixed((RectTransform)save.transform, 180, 60);
        CopyOnClick(oldSaveResults, save);
        var share = WideButton(footRow, "ShareResults", "Show results to everyone", Pink, Color.white);
        Fixed((RectTransform)share.transform, 290, 60);
        SoftTransition(share);
        var leave = WideButton(footRow, "QuitGame", "Quit game", Lilac, VoteNo);
        Fixed((RectTransform)leave.transform, 170, 60);
        var endGame = Object.FindFirstObjectByType<EndGameManager>(FindObjectsInactive.Include);
        if (endGame) UnityEventTools.AddPersistentListener(leave.onClick, new UnityAction(endGame.QuitGame));
        else Debug.LogWarning("[MainHudBuilder] No EndGameManager in the scene: \"Quit game\" has no wiring.");

        content.gameObject.SetActive(false);

        view.content = content.gameObject;
        view.eyebrow = eyebrow;
        view.subtitle = subtitle;
        view.gameAverage = gameAvg;
        view.ownAverage = ownAvg;
        view.gameAverageFill = gameFill;
        view.ownAverageFill = ownFill;
        view.formulaText = formula;
        view.scroll = sr;
        view.rows = rows;
        view.rowTemplate = rowTemplate;
        view.legend = legend;
        view.legendTemplate = legendItem.gameObject;
        view.saveButton = save.gameObject;
        view.shareButton = share;
        view.shareLabel = share.GetComponentInChildren<TextMeshProUGUI>();
        view.leaveButton = leave.gameObject;
        EditorUtility.SetDirty(view);
        return view;
    }

    // Average tile: label, big number "/ 10", thin bar.
    private static RectTransform ResultsTile(RectTransform parent, string name, string label, out TextMeshProUGUI big, out RectTransform fill)
    {
        var tile = NewRect(name, parent);
        Fixed(tile, 250, -1);
        Round(tile, new Color(1, 1, 1, 0.12f), 20);
        Column(tile, 2, new RectOffset(18, 18, 12, 14));
        var eye = Label(tile, "Label", bold, 15, new Color(1, 1, 1, 0.8f), label);
        eye.characterSpacing = 6;
        var line = NewRect("Value", tile);
        Row(line, 8, new RectOffset(0, 0, 0, 0)).childAlignment = TextAnchor.LowerLeft;
        big = Label(line, "Big", bold, 44, Color.white, "–");
        Label(line, "Unit", regular, 20, new Color(1, 1, 1, 0.75f), "/ 10");
        var bar = NewRect("Bar", tile);
        Fixed(bar, -1, 8);
        Round(bar, new Color(1, 1, 1, 0.2f), 4);
        fill = NewRect("Fill", bar);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0, 1);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        Round(fill, Color.white, 4);
        return tile;
    }

    private static TextMeshProUGUI ColumnTitle(RectTransform parent, string text, float x, float w, TextAlignmentOptions align)
    {
        var rt = NewRect(text, parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0);
        rt.sizeDelta = new Vector2(w, 30);
        var t = Txt(rt, bold, 16, EndMuted, align);
        t.characterSpacing = 5;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.text = text;
        return t;
    }

    // One player row: Main (click to open) + Details. Names are what ResultsCard looks for.
    private static GameObject ResultsRow(RectTransform parent)
    {
        var row = NewRect("RowTemplate", parent);
        Round(row, Color.white, 20);
        row.gameObject.AddComponent<CanvasGroup>();
        var col = Column(row, 0, new RectOffset(0, 0, 0, 0));
        col.childForceExpandWidth = true;

        var main = NewRect("Main", row);
        Fixed(main, -1, 76);
        var hit = main.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, 0);
        var button = main.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        var rank = RowItem(main, "Rank", ColRank, 44, 44);
        Round(rank, EndIdle, 22);
        Txt(Stretch(NewRect("Label", rank)), bold, 20, EndPurple, TextAlignmentOptions.Center).text = "1";
        var avatar = RowItem(main, "Avatar", ColAvatar, 52, 52);
        Round(avatar, PlayerCapsuleHook.ColorFor(1), 26);
        Txt(Stretch(NewRect("Number", avatar)), bold, 22, Color.white, TextAlignmentOptions.Center).text = "1";
        var name = Txt(RowItem(main, "Name", ColName, 340, 32, 11), bold, 24, Ink, TextAlignmentOptions.MidlineLeft);
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Ellipsis;
        name.text = "Player";
        var company = Txt(RowItem(main, "Company", ColName, 340, 24, -15), regular, 17, EndMuted, TextAlignmentOptions.MidlineLeft);
        company.textWrappingMode = TextWrappingModes.NoWrap;
        company.overflowMode = TextOverflowModes.Ellipsis;
        company.text = "";
        foreach (var (pillName, x) in new[] { ("Game", ColGame), ("Own", ColOwn) })
        {
            var pill = RowItem(main, pillName, x, 60, 42);
            Round(pill, EndIdle, 21);
            Txt(Stretch(NewRect("Label", pill)), bold, 22, Color.white, TextAlignmentOptions.Center).text = "–";
        }

        // Bar area: track with segments, red chip for points taken away, "Still answering…".
        var bar = NewRect("Bar", main);
        bar.anchorMin = new Vector2(0, 0.5f); bar.anchorMax = new Vector2(1, 0.5f);
        bar.pivot = new Vector2(0, 0.5f);
        bar.offsetMin = new Vector2(ColBar, -17); bar.offsetMax = new Vector2(-ColBarRight, 17);
        var track = Stretch(NewRect("Track", bar));
        track.offsetMax = new Vector2(-84, 0);
        Round(track, EndIdle, 17);
        track.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var segments = Stretch(NewRect("Segments", track));
        var seg = NewRect("Segment", segments);
        seg.anchorMin = Vector2.zero; seg.anchorMax = new Vector2(0.3f, 1);
        seg.offsetMin = seg.offsetMax = Vector2.zero;
        seg.gameObject.AddComponent<Image>().color = EndEyebrow;
        seg.GetComponent<Image>().raycastTarget = false;
        var segLabel = Txt(Stretch(NewRect("Label", seg)), bold, 16, Color.white, TextAlignmentOptions.Center);
        segLabel.textWrappingMode = TextWrappingModes.NoWrap;
        var neg = NewRect("Negative", bar);
        neg.anchorMin = neg.anchorMax = neg.pivot = new Vector2(1, 0.5f);
        neg.anchoredPosition = Vector2.zero;
        neg.sizeDelta = new Vector2(74, 34);
        Round(neg, new Color32(0xFD, 0xE3, 0xE3, 0xFF), 17);
        Txt(Stretch(NewRect("Label", neg)), bold, 18, VoteNo, TextAlignmentOptions.Center).text = "−1";
        neg.gameObject.SetActive(false);
        var waiting = Txt(Stretch(NewRect("Waiting", bar)), regular, 19, EndMuted, TextAlignmentOptions.MidlineLeft);
        waiting.fontStyle = FontStyles.Italic;
        waiting.text = "Still answering…";
        waiting.gameObject.SetActive(false);

        var score = NewRect("Score", main);
        score.anchorMin = score.anchorMax = score.pivot = new Vector2(1, 0.5f);
        score.anchoredPosition = new Vector2(-ColScoreRight, 0);
        score.sizeDelta = new Vector2(150, 50);
        var scoreText = Txt(score, bold, 36, Ink, TextAlignmentOptions.MidlineRight);
        scoreText.textWrappingMode = TextWrappingModes.NoWrap;
        scoreText.text = "0.0";
        var chevron = NewRect("Chevron", main);
        chevron.anchorMin = chevron.anchorMax = chevron.pivot = new Vector2(1, 0.5f);
        chevron.pivot = new Vector2(0.5f, 0.5f);
        chevron.anchoredPosition = new Vector2(-28, 0);
        chevron.sizeDelta = new Vector2(22, 22);
        var chevronImg = chevron.gameObject.AddComponent<Image>();
        chevronImg.sprite = ActionDrawerBuilder.GetOrCreateChevron();
        chevronImg.color = EndMuted;
        chevronImg.raycastTarget = false;
        chevron.localRotation = Quaternion.identity;

        // Details
        var details = NewRect("Details", row);
        var detailsRow = Row(details, 28, new RectOffset((int)ColName, 70, 0, 16));
        detailsRow.childAlignment = TextAnchor.UpperLeft;
        var change = NewRect("Change", details);
        Column(change, 2, new RectOffset(0, 0, 0, 0));
        change.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var changeEye = Label(change, "Eyebrow", bold, 15, EndEyebrow, "DID THEIR OBJECTIVE CHANGE?");
        changeEye.characterSpacing = 6;
        var changeText = Label(change, "Text", regular, 19, Ink, "No answer.");
        changeText.textWrappingMode = TextWrappingModes.Normal;
        var math = NewRect("Math", details);
        Row(math, 16, new RectOffset(0, 0, 0, 0)).childAlignment = TextAnchor.UpperLeft;
        Fixed(math, 520, -1);
        var labels = Label(math, "Labels", regular, 18, EndBody, "Actions played · 2 × 2");
        labels.alignment = TextAlignmentOptions.TopLeft;
        labels.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var values = Label(math, "Values", bold, 18, Ink, "+4");
        values.alignment = TextAlignmentOptions.TopRight;
        Fixed(values.rectTransform, 70, -1);
        details.gameObject.SetActive(false);

        row.gameObject.SetActive(false);
        return row.gameObject;
    }

    // Child of a row's Main, from its left edge, vertically centred (dy moves it up).
    private static RectTransform RowItem(RectTransform main, string name, float x, float w, float h, float dy = 0)
    {
        var rt = NewRect(name, main);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, dy);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    // ── Player end card ──────────────────────────────────────────────────────

    private static readonly Color EndPurple = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color EndEyebrow = new Color32(0x6A, 0x55, 0xA8, 0xFF);
    private static readonly Color EndQuoteBg = new Color32(0xF7, 0xF4, 0xFC, 0xFF);
    private static readonly Color EndQuoteText = new Color32(0x3A, 0x31, 0x50, 0xFF);
    private static readonly Color EndMuted = new Color32(0x8A, 0x82, 0xA0, 0xFF);
    private static readonly Color EndBody = new Color32(0x5E, 0x55, 0x75, 0xFF);
    private static readonly Color EndIdle = new Color32(0xF1, 0xEE, 0xF6, 0xFF);
    private static readonly Color EndBorder = new Color32(0xDD, 0xD6, 0xEA, 0xFF);

    private static PlayerEndCard BuildPlayerEndCard(RectTransform root)
    {
        var holder = Stretch(NewRect("PlayerEndCard", root));
        var view = holder.gameObject.AddComponent<PlayerEndCard>();
        var content = Stretch(NewRect("Content", holder));
        var dim = content.gameObject.AddComponent<Image>();
        dim.color = new Color(0.08f, 0.05f, 0.16f, 0.6f);
        dim.raycastTarget = true; // the board stays visible but can't be used

        // ── Form card ──
        var card = NewRect("Form", content);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(1000, 900);
        CardLook(card);
        var cardCol = Column(card, 0, new RectOffset(0, 0, 0, 0));
        cardCol.childForceExpandWidth = true;

        // Header
        var header = NewRect("Header", card);
        Round(header, EndPurple, 36);
        var square = NewRect("SquareBottom", header); // square bottom corners, rounded top ones
        square.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        square.anchorMin = Vector2.zero; square.anchorMax = new Vector2(1, 0);
        square.pivot = new Vector2(0.5f, 0);
        square.sizeDelta = new Vector2(0, 40);
        square.gameObject.AddComponent<Image>().color = EndPurple;
        square.GetComponent<Image>().raycastTarget = false;
        var headRow = Row(header, 18, new RectOffset(28, 28, 20, 20));

        var avatar = NewRect("Avatar", header);
        Fixed(avatar, 64, 64);
        var avatarImg = Round(avatar, PlayerCapsuleHook.ColorFor(1), 32);
        var avatarNumber = Txt(Stretch(NewRect("Number", avatar)), bold, 28, Color.white, TextAlignmentOptions.Center);
        avatarNumber.text = "1";

        var titles = NewRect("Titles", header);
        Column(titles, -4, new RectOffset(0, 0, 0, 0));
        titles.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var over = Label(titles, "Eyebrow", bold, 17, new Color(1, 1, 1, 0.8f), "GAME OVER");
        over.characterSpacing = 10;
        var title = Label(titles, "Title", bold, 34, Color.white, "How did it go?");
        title.textWrappingMode = TextWrappingModes.Normal;

        var chips = NewRect("Recap", header);
        Row(chips, 8, new RectOffset(0, 0, 0, 0));
        var actionsChip = EndChip(chips, "Actions");
        var collabsChip = EndChip(chips, "Collaborations");
        var receivedChip = EndChip(chips, "Received");

        // Body
        var body = NewRect("Body", card);
        Column(body, 22, new RectOffset(40, 40, 22, 28));

        var gameQ = EndQuestion(body, "GameObjective", "GAME OBJECTIVE", "How well did the group reach it?",
            out var gameQuote, out var gameObjective, out var gameScale, out var gameWord);
        var myQ = EndQuestion(body, "MyObjective", "YOUR OBJECTIVE", "How well did you reach yours?",
            out var myQuote, out var myObjective, out var myScale, out var myWord);

        var changeBlock = NewRect("Change", body);
        Column(changeBlock, 8, new RectOffset(0, 0, 0, 0));
        Label(changeBlock, "Ask", bold, 24, Ink, "Did your personal objective change during the game? How?");
        var inputRT = NewRect("Input", changeBlock);
        Fixed(inputRT, -1, 92);
        var input = ActionCardBuilder.Input("Input", inputRT, regular, 20, Color.white,
            "Optional · e.g. I now care more about the river banks than about new housing", 500);
        var outline = inputRT.GetComponent<Outline>();
        outline.effectColor = EndBorder;
        outline.effectDistance = new Vector2(2, -2);
        input.placeholder.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
        input.placeholder.color = new Color32(0xA5, 0x9D, 0xB8, 0xFF);
        input.textComponent.color = Ink;
        Round(inputRT, Color.white, 16).raycastTarget = true;

        var footer = NewRect("Footer", body);
        Row(footer, 16, new RectOffset(0, 0, 0, 0));
        var hint = Label(footer, "Hint", regular, 19, EndMuted, "Rate both objectives to send.");
        hint.textWrappingMode = TextWrappingModes.Normal;
        hint.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var send = NewRect("SendButton", footer);
        Fixed(send, 280, 64);
        var sendImg = Round(send, Pink, 32);
        sendImg.raycastTarget = true;
        var sendButton = send.gameObject.AddComponent<Button>();
        sendButton.targetGraphic = sendImg;
        SoftTransition(sendButton);
        var sendLabel = Txt(Stretch(NewRect("Label", send)), bold, 24, Color.white, TextAlignmentOptions.Center);
        sendLabel.textWrappingMode = TextWrappingModes.NoWrap;
        sendLabel.text = "Send my answers";

        var fit = card.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Thanks card ──
        var thanks = NewRect("Thanks", content);
        thanks.anchorMin = thanks.anchorMax = thanks.pivot = new Vector2(0.5f, 0.5f);
        thanks.sizeDelta = new Vector2(740, 500);
        CardLook(thanks);
        var thanksCol = Column(thanks, 8, new RectOffset(40, 40, 36, 30));
        thanksCol.childAlignment = TextAnchor.UpperCenter;
        thanksCol.childForceExpandWidth = false;

        var check = NewRect("Check", thanks);
        Fixed(check, 96, 96);
        Round(check, VoteYes, 48);
        Tick(check, Color.white, 1.7f);

        var thanksTitle = Label(thanks, "Title", bold, 40, Ink, "Thanks!");
        thanksTitle.alignment = TextAlignmentOptions.Center;
        var message = Label(thanks, "Message", regular, 22, EndBody,
            "Your answers are in. The game master will show the results when everyone has answered.");
        message.alignment = TextAlignmentOptions.Center;
        message.textWrappingMode = TextWrappingModes.Normal;
        Fixed(message.rectTransform, 660, -1);

        var dots = NewRect("Dots", thanks);
        var dotsRow = Row(dots, 10, new RectOffset(0, 0, 14, 4));
        dotsRow.childAlignment = TextAnchor.MiddleCenter;
        var dot = NewRect("Dot", dots);
        Fixed(dot, 36, 36);
        Round(dot, PlayerCapsuleHook.ColorFor(1), 18);
        var dotInner = Stretch(NewRect("Inner", dot));
        dotInner.offsetMin = new Vector2(4, 4);
        dotInner.offsetMax = new Vector2(-4, -4);
        Round(dotInner, Color.white, 14);

        var answered = Label(thanks, "Answered", bold, 20, EndPurple, "0 of 0 players answered");
        answered.alignment = TextAlignmentOptions.Center;

        var change = Label(thanks, "ChangeButton", bold, 20, EndEyebrow, "<u>Change my answers</u>");
        change.alignment = TextAlignmentOptions.Center;
        change.raycastTarget = true;
        var changeButton = change.gameObject.AddComponent<Button>();
        changeButton.targetGraphic = change;
        Fixed(change.rectTransform, -1, 44);

        var thanksFit = thanks.gameObject.AddComponent<ContentSizeFitter>();
        thanksFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        thanks.gameObject.SetActive(false);
        content.gameObject.SetActive(false);

        view.content = content.gameObject;
        view.form = card.gameObject;
        view.thanks = thanks.gameObject;
        view.avatar = avatarImg;
        view.avatarNumber = avatarNumber;
        view.title = title;
        view.actionsChip = actionsChip;
        view.collabsChip = collabsChip;
        view.receivedChip = receivedChip;
        view.gameQuote = gameQuote;
        view.gameObjective = gameObjective;
        view.gameScale = gameScale;
        view.gameWord = gameWord;
        view.myQuote = myQuote;
        view.myObjective = myObjective;
        view.myScale = myScale;
        view.myWord = myWord;
        view.changeInput = input;
        view.hint = hint;
        view.sendButton = sendButton;
        view.sendLabel = sendLabel;
        view.thanksTitle = thanksTitle;
        view.dotsRow = dots;
        view.dotTemplate = dot.gameObject;
        view.answeredText = answered;
        view.changeButton = changeButton;
        EditorUtility.SetDirty(view);
        return view;
    }

    private static void CardLook(RectTransform card)
    {
        Round(card, Color.white, 36).raycastTarget = true;
        var shadow = card.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.04f, 0.02f, 0.12f, 0.45f);
        shadow.effectDistance = new Vector2(0, -14);
    }

    // Colour changes come from PlayerEndCard, so the button only dims a little when pressed.
    private static void SoftTransition(Button button)
    {
        var colors = button.colors;
        colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        colors.disabledColor = Color.white;
        button.colors = colors;
    }

    private static TextMeshProUGUI EndChip(RectTransform parent, string name)
    {
        var chip = NewRect(name, parent);
        Round(chip, new Color(1, 1, 1, 0.14f), 18);
        Row(chip, 0, new RectOffset(14, 14, 4, 4));
        return Label(chip, "Label", medium, 18, Color.white, "<size=125%><b>0</b></size> " + name.ToLower());
    }

    private static RectTransform EndQuestion(RectTransform body, string name, string eyebrow, string ask,
        out GameObject quote, out TextMeshProUGUI objective, out Button[] scale, out TextMeshProUGUI word)
    {
        var block = NewRect(name, body);
        Column(block, 8, new RectOffset(0, 0, 0, 0));

        var eye = Label(block, "Eyebrow", bold, 16, EndEyebrow, eyebrow);
        eye.characterSpacing = 8;

        var box = NewRect("Quote", block);
        Round(box, EndQuoteBg, 16);
        Column(box, 0, new RectOffset(16, 16, 10, 10));
        objective = Label(box, "Text", regular, 20, EndQuoteText, "“…”");
        objective.textWrappingMode = TextWrappingModes.Normal;
        quote = box.gameObject;

        Label(block, "Ask", bold, 24, Ink, ask);

        var row = NewRect("Scale", block);
        var rowLayout = Row(row, 8, new RectOffset(0, 0, 2, 2));
        rowLayout.childForceExpandWidth = true;
        scale = new Button[11];
        for (int n = 0; n <= 10; n++)
        {
            var b = NewRect(n.ToString(), row);
            var size = b.gameObject.AddComponent<LayoutElement>();
            size.preferredHeight = size.minHeight = 54;
            size.flexibleWidth = 1;
            var img = Round(b, EndIdle, 14);
            img.raycastTarget = true;
            var button = b.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            SoftTransition(button);
            var label = Txt(Stretch(NewRect("Label", b)), bold, 24, EndBody, TextAlignmentOptions.Center);
            label.text = n.ToString();
            scale[n] = button;
        }

        var ends = NewRect("Ends", block);
        Fixed(ends, -1, 26);
        var left = Txt(Stretch(NewRect("NotAtAll", ends)), regular, 17, EndMuted, TextAlignmentOptions.MidlineLeft);
        left.text = "Not at all";
        word = Txt(Stretch(NewRect("Word", ends)), bold, 18, EndMuted, TextAlignmentOptions.Center);
        word.text = "Tap a number";
        var right = Txt(Stretch(NewRect("Completely", ends)), regular, 17, EndMuted, TextAlignmentOptions.MidlineRight);
        right.text = "Completely";
        return block;
    }

    // ── Phase intro ──────────────────────────────────────────────────────────

    private static readonly Color ActionsBrown = new Color32(0x8A, 0x4C, 0x07, 0xFF);
    private static readonly Color ActionsPaper = new Color32(0xFF, 0xF3, 0xE3, 0xFF);
    private static readonly Color CollabInk = new Color32(0x1D, 0x4F, 0x86, 0xFF);

    private static PhaseIntro BuildPhaseIntro(RectTransform root)
    {
        var holder = Stretch(NewRect("PhaseIntro", root));
        var intro = holder.gameObject.AddComponent<PhaseIntro>();
        var content = Stretch(NewRect("Content", holder));
        var group = content.gameObject.AddComponent<CanvasGroup>();

        var back = Stretch(NewRect("Background", content));
        var gradient = back.gameObject.AddComponent<RawImage>();
        gradient.texture = InstrumentTrayBuilder.GetOrCreateGradient();
        gradient.raycastTarget = true;
        var skip = back.gameObject.AddComponent<Button>();
        skip.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(skip.onClick, new UnityAction(intro.Skip));

        var instruments = IntroArt(content, "Instruments");
        var hexes = new RectTransform[3];
        var hexColors = new Color[] { Purple, PurpleMid, new Color32(0xED, 0xE6, 0xFA, 0xFF) };
        for (int i = 0; i < 3; i++)
        {
            var piece = IntroPiece(instruments, "Hex " + (i + 1), new Vector2(250, 288));
            var img = piece.gameObject.AddComponent<Image>();
            img.sprite = hex;
            img.preserveAspect = true;
            img.color = hexColors[i];
            img.raycastTarget = false;
            IntroShadow(piece);
            if (i == 2) Txt(Stretch(NewRect("Mark", piece)), bold, 120, Purple, TextAlignmentOptions.Center).text = "i";
            hexes[i] = piece;
        }

        var actions = IntroArt(content, "Actions");
        var cards = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            var piece = IntroPiece(actions, "Card " + (i + 1), new Vector2(220, 308));
            Round(piece, Amber, 20);
            IntroShadow(piece);
            var paper = Stretch(NewRect("Paper", piece));
            paper.offsetMin = new Vector2(8, 8);
            paper.offsetMax = new Vector2(-8, -8);
            Round(paper, ActionsPaper, 14);
            if (i == 2)
            {
                var emblem = NewRect("Emblem", piece);
                emblem.anchorMin = emblem.anchorMax = new Vector2(0.5f, 0.5f);
                emblem.sizeDelta = new Vector2(76, 102);
                emblem.anchoredPosition = new Vector2(0, 22);
                Round(emblem, Amber, 14);
                Txt(Stretch(NewRect("Letter", emblem)), bold, 44, Color.white, TextAlignmentOptions.Center).text = "A";
                var label = Txt(NewRect("Label", piece), bold, 20, Amber, TextAlignmentOptions.Center);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                label.rectTransform.sizeDelta = new Vector2(200, 30);
                label.rectTransform.anchoredPosition = new Vector2(0, -58);
                label.characterSpacing = 10;
                label.text = "ACTIONS";
            }
            cards[i] = piece;
        }

        var collaboration = IntroArt(content, "Collaboration");
        var people = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            // Smooth full-size images (no stretched corner sprite, no mask): soft shadow, white ring,
            // the player's colour, and a person already clipped to the circle.
            var piece = IntroPiece(collaboration, "Player " + (i + 1), new Vector2(230, 230));
            var shadow = Stretch(NewRect("Shadow", piece));
            shadow.offsetMin = new Vector2(-14, -22);
            shadow.offsetMax = new Vector2(14, 6);
            PlayerCapsuleBuilder.Icon(shadow, IntroSprite("IntroSoftShadow", SoftDisc), new Color(0.1f, 0.12f, 0.3f, 0.35f));
            PlayerCapsuleBuilder.Icon(Stretch(NewRect("Ring", piece)), IntroSprite("IntroDisc", Disc), Color.white);
            var fill = Stretch(NewRect("Fill", piece));
            fill.offsetMin = new Vector2(10, 10);
            fill.offsetMax = new Vector2(-10, -10);
            PlayerCapsuleBuilder.Icon(fill, IntroSprite("IntroDisc", Disc), PlayerCapsuleHook.PlayerColors[i + 1]);
            PlayerCapsuleBuilder.Icon(Stretch(NewRect("Person", fill)), IntroSprite("IntroPersonInCircle", PersonInCircle), Color.white);
            people[i] = piece;
        }

        // Texts under the fan
        var texts = NewRect("Texts", content);
        texts.anchorMin = texts.anchorMax = texts.pivot = new Vector2(0.5f, 0.5f);
        texts.anchoredPosition = new Vector2(0, -205);
        texts.sizeDelta = new Vector2(1200, 200);
        var textsGroup = texts.gameObject.AddComponent<CanvasGroup>();
        textsGroup.blocksRaycasts = false;
        var column = Column(texts, 2, new RectOffset(0, 0, 0, 0));
        column.childAlignment = TextAnchor.UpperCenter;
        var eyebrow = Txt(NewRect("Eyebrow", texts), bold, 24, Pink, TextAlignmentOptions.Center);
        eyebrow.characterSpacing = 12;
        eyebrow.textWrappingMode = TextWrappingModes.NoWrap;
        eyebrow.text = "PHASE 1 OF 3";
        var title = Txt(NewRect("Title", texts), bold, 84, Purple, TextAlignmentOptions.Center);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.text = "Instruments phase";
        var subtitle = Txt(NewRect("Subtitle", texts), regular, 30, Muted, TextAlignmentOptions.Center);
        subtitle.text = "Take turns placing instruments on the board";

        content.gameObject.SetActive(false);

        intro.content = content.gameObject;
        intro.group = group;
        intro.background = gradient;
        intro.texts = textsGroup;
        intro.eyebrowText = eyebrow;
        intro.titleText = title;
        intro.subtitleText = subtitle;
        float[] angles = { 18f, -18f, 0f };
        intro.arts = new[]
        {
            new PhaseIntro.Art { root = instruments.gameObject, pieces = hexes, angles = angles,
                background = InstrumentTrayBuilder.GetOrCreateGradient(), titleColor = Purple,
                eyebrow = "PHASE 1 OF 3", title = "Instruments phase", subtitle = "Take turns placing instruments on the board" },
            new PhaseIntro.Art { root = actions.gameObject, pieces = cards, angles = angles,
                background = ActionDrawerBuilder.GetOrCreateGradient(), titleColor = ActionsBrown,
                eyebrow = "PHASE 2 OF 3", title = "Action cards phase", subtitle = "Choose the action cards you want to play" },
            new PhaseIntro.Art { root = collaboration.gameObject, pieces = people, angles = angles,
                background = GetOrCreateCollabGradient(), titleColor = CollabInk,
                eyebrow = "PHASE 3 OF 3", title = "Collaboration phase", subtitle = "Collaborate on other players’ actions" },
        };
        EditorUtility.SetDirty(intro);
        return intro;
    }

    // ── Smooth intro images (512 px, anti-aliased), made once into Assets/Images/Collaboration ──

    private const int IntroTex = 512;

    // Alpha of a pixel, from its centre in -1..1 coordinates (y up).
    private delegate float AlphaAt(float x, float y);

    private static float Disc(float x, float y) => Edge(1f - Mathf.Sqrt(x * x + y * y));

    // Head and shoulders, cut by the circle so the shoulders follow its curve.
    private static float PersonInCircle(float x, float y)
    {
        float circle = Edge(1f - Mathf.Sqrt(x * x + y * y));
        float dHead = Mathf.Sqrt(x * x + (y - 0.2f) * (y - 0.2f));
        float head = Edge(0.33f - dHead);
        float bx = x / 0.72f, by = (y + 0.98f) / 0.78f;
        float body = Edge((1f - Mathf.Sqrt(bx * bx + by * by)) * 0.72f);
        float gap = Edge(dHead - 0.4f); // thin space between head and shoulders
        return circle * Mathf.Max(head, body * gap);
    }

    private static float SoftDisc(float x, float y)
    {
        float d = Mathf.Sqrt(x * x + y * y);
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - d) / 0.28f));
    }

    // Distance (in -1..1 units) to a smooth 0..1 edge about one pixel wide.
    private static float Edge(float d) => Mathf.Clamp01(d * IntroTex * 0.5f + 0.5f);

    private static Sprite IntroSprite(string name, AlphaAt alpha)
    {
        const string folder = "Assets/Images/Collaboration";
        string path = $"{folder}/{name}.png";
        if (System.IO.File.Exists(path)) return SingleSprite(path);
        System.IO.Directory.CreateDirectory(folder);
        var tex = new Texture2D(IntroTex, IntroTex, TextureFormat.RGBA32, false);
        var pixels = new Color32[IntroTex * IntroTex];
        for (int py = 0; py < IntroTex; py++)
        for (int px = 0; px < IntroTex; px++)
        {
            float x = (px + 0.5f) / IntroTex * 2f - 1f;
            float y = (py + 0.5f) / IntroTex * 2f - 1f;
            pixels[py * IntroTex + px] = new Color(1, 1, 1, alpha(x, y));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true; // stays smooth while it scales during the animation
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return SingleSprite(path);
    }

    // The project imports new textures as "Multiple" sprites, which picks the wrong part of the image:
    // make sure it is one sprite covering the whole texture.
    private static Sprite SingleSprite(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Holder for one phase's fan, above the middle of the screen.
    private static RectTransform IntroArt(RectTransform content, string name)
    {
        var art = NewRect(name, content);
        art.anchorMin = art.anchorMax = art.pivot = new Vector2(0.5f, 0.5f);
        art.anchoredPosition = new Vector2(0, 120);
        art.sizeDelta = new Vector2(400, 400);
        art.gameObject.SetActive(false);
        return art;
    }

    // One piece of the fan, turning around its bottom edge like a card in a hand.
    private static RectTransform IntroPiece(RectTransform art, string name, Vector2 size)
    {
        var piece = NewRect(name, art);
        piece.anchorMin = piece.anchorMax = new Vector2(0.5f, 0.5f);
        piece.pivot = new Vector2(0.5f, 0f);
        piece.sizeDelta = size;
        piece.anchoredPosition = new Vector2(0, -size.y / 2f);
        return piece;
    }

    private static void IntroShadow(RectTransform piece)
    {
        var shadow = piece.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.14f, 0.1f, 0.24f, 0.3f);
        shadow.effectDistance = new Vector2(-5, -7);
    }

    // Blue at the top fading to pale blue, made once into Assets/Images/Collaboration/CollabGradient.png.
    private static Texture2D GetOrCreateCollabGradient()
    {
        const string folder = "Assets/Images/Collaboration";
        const string path = folder + "/CollabGradient.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing) return existing;
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Images", "Collaboration");

        Color top = new Color32(0x7F, 0xA8, 0xDC, 0xFF), mid = new Color32(0xB9, 0xD2, 0xF0, 0xFF), bottom = new Color32(0xEC, 0xF3, 0xFC, 0xFF);
        const int w = 4, h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float t = 1f - y / (h - 1f); // 0 at the top
            var c = t < 0.5f
                ? Color.Lerp(top, mid, Mathf.SmoothStep(0f, 1f, t * 2f))
                : Color.Lerp(mid, bottom, Mathf.SmoothStep(0f, 1f, t * 2f - 1f));
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ── Collaboration round ──────────────────────────────────────────────────

    private static readonly Color Cream = new Color32(0xFB, 0xFA, 0xF6, 0xFF);
    private static readonly Color CardLine = new Color32(0xE1, 0xE6, 0xE3, 0xFF);
    private static readonly Color Amber = new Color32(0xB5, 0x65, 0x0A, 0xFF);
    private static readonly Color CollabBlue = new Color32(0x2F, 0x6F, 0xB0, 0xFF);
    private const float RoundGap = 12f;

    // "Played actions" over the board, between the ribbon and the bottom row of capsules.
    private static CollaborationRound BuildCollaborationRound(RectTransform root)
    {
        var holder = Stretch(NewRect("CollaborationRound", root));
        var round = holder.gameObject.AddComponent<CollaborationRound>();
        var content = Stretch(NewRect("Content", holder));

        float top = RibbonTop + RibbonH + RoundGap;
        float bottom = root.rect.height - BottomCapsulesTop(root) + RoundGap;
        var panel = Stretch(NewRect("Panel", content));
        panel.offsetMin = new Vector2(40, bottom);
        panel.offsetMax = new Vector2(-40, -top);
        var panelImg = Round(panel, new Color(0.11f, 0.08f, 0.23f, 0.93f), 28);
        panelImg.raycastTarget = true; // the board underneath can't be used in this round

        // Header: title, hint, pager
        var header = NewRect("Header", panel);
        header.anchorMin = new Vector2(0, 1);
        header.anchorMax = new Vector2(1, 1);
        header.pivot = new Vector2(0.5f, 1);
        header.offsetMin = new Vector2(28, -76);
        header.offsetMax = new Vector2(-28, 0);
        var headerRow = Row(header, 16, new RectOffset(0, 0, 8, 0));
        headerRow.childControlHeight = true;
        var title = Label(header, "Title", bold, 30, Color.white, "Played actions");
        var hint = Label(header, "Hint", regular, 19, new Color(1, 1, 1, 0.8f), "Collaborating costs 1 token");
        hint.overflowMode = TextOverflowModes.Ellipsis;
        var hintSize = hint.gameObject.AddComponent<LayoutElement>();
        hintSize.flexibleWidth = 1;
        hintSize.minWidth = 0;
        var pageText = Label(header, "Page", bold, 18, new Color(1, 1, 1, 0.85f), "Players 1–4 of 7");
        var prev = PageArrow(header, "Prev", 180f, out var prevAlert);
        var dots = NewRect("Dots", header);
        Row(dots, 7, new RectOffset(0, 0, 0, 0)).childControlHeight = true;
        var dot = NewRect("Dot", dots);
        Fixed(dot, 12, 12);
        var dotImg = Round(dot, Color.white, 6);
        dotImg.raycastTarget = true;
        dot.gameObject.AddComponent<Button>().targetGraphic = dotImg;
        var next = PageArrow(header, "Next", 0f, out var nextAlert);

        // Four lanes
        var lanesRT = Stretch(NewRect("Lanes", panel));
        lanesRT.offsetMin = new Vector2(28, 20);
        lanesRT.offsetMax = new Vector2(-28, -84);
        lanesRT.gameObject.AddComponent<RectMask2D>();
        var lanesRow = Row(lanesRT, 20, new RectOffset(0, 0, 0, 0));
        lanesRow.childAlignment = TextAnchor.UpperLeft;
        lanesRow.childControlHeight = true;
        lanesRow.childForceExpandWidth = true;
        lanesRow.childForceExpandHeight = true;
        var lanes = new CollaborationRound.Lane[4];
        for (int i = 0; i < lanes.Length; i++) lanes[i] = BuildLane(lanesRT, i);

        var card = BuildRoundCard(panel);
        var avatar = NewRect("AvatarTemplate", panel);
        Fixed(avatar, 34, 34);
        Round(avatar, PlayerCapsuleHook.PlayerColors[1], 17);
        var ring = avatar.gameObject.AddComponent<Outline>();
        ring.effectColor = Cream;
        ring.effectDistance = new Vector2(2, -2);
        var avatarNumber = Txt(Stretch(NewRect("Number", avatar)), bold, 15, Color.white, TextAlignmentOptions.Center);
        avatarNumber.text = "2";
        avatar.gameObject.SetActive(false);

        content.gameObject.SetActive(false);

        round.content = content.gameObject;
        round.hintText = hint;
        round.pageText = pageText;
        round.prevButton = prev;
        round.nextButton = next;
        round.prevAlert = prevAlert;
        round.nextAlert = nextAlert;
        round.pageDots = dots;
        round.pageDotTemplate = dot.gameObject;
        round.lanes = lanes;
        round.cardTemplate = card;
        round.avatarTemplate = avatar.gameObject;
        EditorUtility.SetDirty(round);
        return round;
    }

    // Round white arrow button for the pager; a pink dot on it when a request waits on that side.
    private static Button PageArrow(RectTransform parent, string name, float angle, out GameObject alert)
    {
        var rt = NewRect(name, parent);
        Fixed(rt, 46, 46);
        var img = Round(rt, Color.white, 23);
        img.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var arrow = NewRect("Arrow", rt);
        arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
        arrow.sizeDelta = new Vector2(20, 20);
        arrow.localRotation = Quaternion.Euler(0, 0, angle);
        var arrowImg = arrow.gameObject.AddComponent<Image>();
        arrowImg.sprite = ActionDrawerBuilder.GetOrCreateChevron();
        arrowImg.color = Purple;
        arrowImg.raycastTarget = false;
        var dotRT = NewRect("Alert", rt);
        dotRT.anchorMin = dotRT.anchorMax = new Vector2(1, 1);
        dotRT.anchoredPosition = new Vector2(-4, -4);
        dotRT.sizeDelta = new Vector2(16, 16);
        Round(dotRT, Pink, 8);
        var dotRing = dotRT.gameObject.AddComponent<Outline>();
        dotRing.effectColor = Color.white;
        dotRing.effectDistance = new Vector2(1.5f, -1.5f);
        dotRT.gameObject.SetActive(false);
        alert = dotRT.gameObject;
        return button;
    }

    private static CollaborationRound.Lane BuildLane(RectTransform lanes, int i)
    {
        var lane = NewRect("Lane" + (i + 1), lanes);
        Column(lane, 12, new RectOffset(0, 0, 0, 0));
        lane.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        var head = NewRect("Head", lane);
        Fixed(head, -1, 52);
        var headRow = Row(head, 12, new RectOffset(0, 0, 0, 0));
        headRow.childControlHeight = true;
        var avatar = NewRect("Avatar", head);
        Fixed(avatar, 46, 46);
        var doneRing = Stretch(NewRect("DoneRing", avatar));
        doneRing.offsetMin = new Vector2(-4, -4);
        doneRing.offsetMax = new Vector2(4, 4);
        Round(doneRing, new Color32(0x7B, 0xD8, 0x8F, 0xFF), 27);
        doneRing.gameObject.SetActive(false);
        var fill = Stretch(NewRect("Fill", avatar));
        var avatarImg = Round(fill, PlayerCapsuleHook.PlayerColors[1], 23);
        var number = Txt(Stretch(NewRect("Number", fill)), bold, 20, Color.white, TextAlignmentOptions.Center);
        number.text = "1";
        var names = NewRect("Names", head);
        Column(names, -4, new RectOffset(0, 0, 0, 0)).childAlignment = TextAnchor.MiddleLeft;
        var namesSize = names.gameObject.AddComponent<LayoutElement>();
        namesSize.flexibleWidth = 1;
        namesSize.minWidth = 0;
        var name = Label(names, "Name", bold, 21, Color.white, "Player");
        name.overflowMode = TextOverflowModes.Ellipsis;
        var company = Label(names, "Company", regular, 15, new Color(1, 1, 1, 0.75f), "Company");
        company.overflowMode = TextOverflowModes.Ellipsis;
        var used = NewRect("Used", head);
        Fixed(used, -1, 26);
        Row(used, 0, new RectOffset(10, 10, 0, 0)).childControlHeight = true;
        Round(used, new Color(1, 1, 1, 0.18f), 13);
        var usedText = Label(used, "Label", bold, 14, Color.white, "0 of 5 tokens");
        used.gameObject.SetActive(false);

        var cards = NewRect("Cards", lane);
        Column(cards, 12, new RectOffset(0, 0, 0, 0));

        return new CollaborationRound.Lane
        {
            root = lane.gameObject, avatar = avatarImg, number = number, doneRing = doneRing.gameObject,
            name = name, company = company, usedChip = used.gameObject, usedText = usedText, cards = cards
        };
    }

    // Compact played card: number hexagon, kind, title, collaborators and the Collaborate button or a tag.
    private static GameObject BuildRoundCard(RectTransform parent)
    {
        var card = NewRect("CardTemplate", parent);
        Round(card, Amber, 16); // border (recoloured: amber, open pink, blue when collaborating)
        Column(card, 0, new RectOffset(3, 3, 3, 3));
        var face = NewRect("Face", card);
        Round(face, Cream, 13);
        Column(face, 8, new RectOffset(16, 16, 12, 12));

        var topRow = NewRect("Top", face);
        Fixed(topRow, -1, 40);
        Row(topRow, 10, new RectOffset(0, 0, 0, 0)).childControlHeight = true;
        var hexRT = NewRect("Hex", topRow);
        Fixed(hexRT, 44, 40);
        var hexImg = PlayerCapsuleBuilder.Icon(hexRT, hex, Amber);
        hexImg.preserveAspect = false;
        var hexNumber = Txt(Stretch(NewRect("Number", hexRT)), bold, 16, Color.white, TextAlignmentOptions.Center);
        hexNumber.textWrappingMode = TextWrappingModes.NoWrap;
        hexNumber.text = "03";
        NewRect("Spacer", topRow).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var kind = Label(topRow, "Kind", bold, 14, Amber, "ACTION");
        kind.characterSpacing = 6;

        var title = Txt(NewRect("Title", face), bold, 20, Ink, TextAlignmentOptions.TopLeft);
        title.text = "Creating partnerships to enhance biodiversity";
        title.overflowMode = TextOverflowModes.Ellipsis;
        title.maxVisibleLines = 2;

        var line = NewRect("Line", face);
        Fixed(line, -1, 1.5f);
        line.gameObject.AddComponent<Image>().color = CardLine;

        var footer = NewRect("Footer", face);
        Fixed(footer, -1, 38);
        var footerRow = Row(footer, 8, new RectOffset(0, 0, 0, 0));
        footerRow.childControlHeight = true;
        var avatars = NewRect("Avatars", footer);
        Row(avatars, -8, new RectOffset(2, 2, 0, 0)).childControlHeight = true;
        var noOne = Label(footer, "NoOne", regular, 15, Muted, "No one yet");
        NewRect("Spacer", footer).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        var collaborate = NewRect("Collaborate", footer);
        Fixed(collaborate, -1, 36);
        var collabImg = Round(collaborate, CollabBlue, 18);
        collabImg.raycastTarget = true;
        collaborate.gameObject.AddComponent<Button>().targetGraphic = collabImg;
        Row(collaborate, 0, new RectOffset(16, 16, 0, 0)).childControlHeight = true;
        Label(collaborate, "Label", bold, 16, Color.white, "Collaborate · 1 token");

        var tag = NewRect("Tag", footer);
        Fixed(tag, -1, 28);
        Round(tag, Lilac, 14);
        Row(tag, 0, new RectOffset(12, 12, 0, 0)).childControlHeight = true;
        Label(tag, "Label", bold, 15, Purple, "Your action");
        tag.gameObject.SetActive(false);

        card.gameObject.SetActive(false);
        return card.gameObject;
    }

    // Player line: colour dot with the number, name, status chip and a remove (×) button on the right.
    private static GameObject BuildPlayerRow(RectTransform rows)
    {
        var row = NewRect("PlayerTemplate", rows);
        Fixed(row, -1, 42);
        var dot = NewRect("Dot", row);
        dot.anchorMin = dot.anchorMax = dot.pivot = new Vector2(0, 0.5f);
        dot.sizeDelta = new Vector2(30, 30);
        Round(dot, PlayerCapsuleHook.PlayerColors[0], 15);
        var number = Txt(Stretch(NewRect("Number", dot)), bold, 15, Color.white, TextAlignmentOptions.Center);
        number.text = "1";
        var nameRT = Stretch(NewRect("Name", row));
        nameRT.offsetMin = new Vector2(42, 0);
        nameRT.offsetMax = new Vector2(-140, 0);
        var name = Txt(nameRT, medium, 19, Ink, TextAlignmentOptions.MidlineLeft);
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Ellipsis;
        name.text = "Player";

        var status = NewRect("Status", row);
        status.anchorMin = status.anchorMax = status.pivot = new Vector2(1, 0.5f);
        status.anchoredPosition = new Vector2(-42, 0);
        status.sizeDelta = new Vector2(88, 26);
        Round(status, LilacLine, 13);
        var statusLabel = Txt(Stretch(NewRect("Label", status)), bold, 14, Purple, TextAlignmentOptions.Center);
        statusLabel.text = "Next";
        status.gameObject.SetActive(false);

        var kick = NewRect("Kick", row);
        kick.anchorMin = kick.anchorMax = kick.pivot = new Vector2(1, 0.5f);
        kick.sizeDelta = new Vector2(34, 30);
        var kickImg = Round(kick, new Color(1, 1, 1, 0), 15);
        kickImg.raycastTarget = true;
        var kickButton = kick.gameObject.AddComponent<Button>();
        kickButton.targetGraphic = kickImg;
        var kickLabel = Txt(Stretch(NewRect("Label", kick)), bold, 22, Grey, TextAlignmentOptions.Center);
        kickLabel.textWrappingMode = TextWrappingModes.NoWrap;
        kickLabel.text = "×";

        var line = NewRect("Line", row);
        line.anchorMin = new Vector2(0, 0);
        line.anchorMax = new Vector2(1, 0);
        line.pivot = new Vector2(0.5f, 0);
        line.sizeDelta = new Vector2(0, 1.5f);
        line.gameObject.AddComponent<Image>().color = LilacLine;
        row.gameObject.SetActive(false);
        return row.gameObject;
    }

    private static Button WideButton(RectTransform parent, string name, string text, Color fill, Color ink)
    {
        var rt = NewRect(name, parent);
        Fixed(rt, -1, 50);
        var img = Round(rt, fill, 25);
        img.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var label = Txt(Stretch(NewRect("Label", rt)), bold, 18, ink, TextAlignmentOptions.Center);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = text;
        return button;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // Tick drawn from two rounded bars, centred in the parent (scale 1 fits a ~50 px circle).
    private static RectTransform Tick(RectTransform parent, Color color, float scale)
    {
        var icon = Stretch(NewRect("Tick", parent));
        Bar(icon, color, new Vector2(-6, -2) * scale, new Vector2(12, 5) * scale, -45f);
        Bar(icon, color, new Vector2(4, 2) * scale, new Vector2(24, 5) * scale, 50f);
        return icon;
    }

    private static RectTransform Cross(RectTransform parent, Color color, float scale)
    {
        var icon = Stretch(NewRect("Cross", parent));
        Bar(icon, color, Vector2.zero, new Vector2(22, 5) * scale, 45f);
        Bar(icon, color, Vector2.zero, new Vector2(22, 5) * scale, -45f);
        return icon;
    }

    private static void Bar(RectTransform parent, Color color, Vector2 pos, Vector2 size, float angle)
    {
        var bar = NewRect("Bar", parent);
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
        bar.anchoredPosition = pos;
        bar.sizeDelta = size;
        bar.localRotation = Quaternion.Euler(0, 0, angle);
        Round(bar, color, size.y / 2f);
    }

    // Same scene wiring as the old button (its On Click list, arguments included).
    private static void CopyOnClick(Button from, Button to)
    {
        if (!from || !to)
        {
            Debug.LogWarning($"[MainHudBuilder] No old button to copy the wiring from for \"{(to ? to.name : "?")}\".");
            return;
        }
        var src = new SerializedObject(from);
        var dst = new SerializedObject(to);
        dst.CopyFromSerializedProperty(src.FindProperty("m_OnClick"));
        dst.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Section(RectTransform body, string text)
    {
        var label = Txt(NewRect("Section", body), bold, 15, PurpleMid, TextAlignmentOptions.BottomLeft);
        label.characterSpacing = 6;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = text;
        Fixed(label.rectTransform, -1, 26);
    }

    private static TextMeshProUGUI Label(RectTransform parent, string name, TMP_FontAsset font, float size, Color color, string text)
    {
        var label = Txt(NewRect(name, parent), font, size, color, TextAlignmentOptions.MidlineLeft);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = text;
        return label;
    }

    private static VerticalLayoutGroup Column(RectTransform rt, float spacing, RectOffset padding)
    {
        var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private static HorizontalLayoutGroup Row(RectTransform rt, float spacing, RectOffset padding)
    {
        var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        return layout;
    }

    // Fixed preferred size for a layout child (-1 leaves that axis to the layout).
    private static void Fixed(RectTransform rt, float w, float h)
    {
        var size = rt.GetComponent<LayoutElement>();
        if (!size) size = rt.gameObject.AddComponent<LayoutElement>();
        if (w >= 0) size.preferredWidth = size.minWidth = w;
        if (h >= 0) size.preferredHeight = size.minHeight = h;
        rt.sizeDelta = new Vector2(w >= 0 ? w : rt.sizeDelta.x, h >= 0 ? h : rt.sizeDelta.y);
    }
}
