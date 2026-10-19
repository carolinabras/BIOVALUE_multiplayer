using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ActionCardBuilder;

// Rebuilds CollabPopUp.prefab as the request card of the "Collaboration round" mockup: the asker's
// avatar and name, the card they want to join, Decline / Accept. Same asset (ColaborationManager keeps
// its reference). Run "BioValue/Build Collaboration Request" (Build Turn Ribbon and GM Panel runs it too).
public static class CollabRequestBuilder
{
    private const string PrefabPath = "Assets/Resources/PhotonPrefabs/CollabPopUp.prefab";

    private static readonly Color Pink = new Color32(0xFE, 0x46, 0xA5, 0xFF);
    private static readonly Color Ink = new Color32(0x24, 0x1B, 0x3A, 0xFF);
    private static readonly Color Muted = new Color32(0x6E, 0x65, 0x80, 0xFF);
    private static readonly Color DeclineBg = new Color32(0xF1, 0xEC, 0xFB, 0xFF);
    private static readonly Color DeclineInk = new Color32(0x46, 0x32, 0x7F, 0xFF);
    private static readonly Color CardBorder = new Color32(0xB5, 0x65, 0x0A, 0xFF);
    private static readonly Color CardFill = new Color32(0xFB, 0xFA, 0xF6, 0xFF);

    [MenuItem("BioValue/Build Collaboration Request")]
    public static void BuildMenu()
    {
        if (!LoadArt())
        {
            EditorUtility.DisplayDialog("Build Collaboration Request", "Missing art or font. Run BioValue/Build Action Card first.", "OK");
            return;
        }
        Build();
        Debug.Log("[CollabRequestBuilder] " + PrefabPath + " rebuilt.");
    }

    // Expects LoadArt() to have run.
    public static void Build()
    {
        const float w = 587, h = 299, pad = 27;
        // Rebuilt inside the existing prefab, so its root keeps its id and the scene's reference holds.
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        foreach (var c in root.GetComponents<Component>())
            if (!(c is Transform) && !(c is CollabPopUpHook)) Object.DestroyImmediate(c);
        var rt = (RectTransform)root.transform;
        rt.localScale = Vector3.one;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-85, 112);
        rt.sizeDelta = new Vector2(w, h);
        var bg = Round(rt, Color.white, 29);
        bg.raycastTarget = true; // clicks don't fall through to the board
        var shadow = root.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.04f, 0.02f, 0.12f, 0.35f);
        shadow.effectDistance = new Vector2(0, -10);
        root.AddComponent<CanvasGroup>();
        var hook = root.GetComponent<CollabPopUpHook>();
        if (!hook) hook = root.AddComponent<CollabPopUpHook>();
        var so = new SerializedObject(hook);

        // Who asks
        var circle = Place(NewRect("Avatar", rt), pad, 30, 61, 61);
        so.FindProperty("avatar").objectReferenceValue = Round(circle, new Color32(0x2F, 0x6F, 0xB0, 0xFF), 30.5f);
        var number = Txt(Stretch(NewRect("Number", circle)), bold, 24, Color.white, TextAlignmentOptions.Center);
        number.text = "2";
        so.FindProperty("avatarNumber").objectReferenceValue = number;

        var title = Txt(Place(NewRect("Title", rt), 102, 20, w - 102 - pad, 62), bold, 24, Ink, TextAlignmentOptions.BottomLeft);
        title.textWrappingMode = TextWrappingModes.Normal;
        title.overflowMode = TextOverflowModes.Ellipsis;
        title.lineSpacing = -12;
        title.text = "Bruno wants to collaborate on your action";
        so.FindProperty("title").objectReferenceValue = title;
        var sub = Txt(Place(NewRect("Sub", rt), 102, 84, w - 102 - pad, 26), regular, 19, Muted, TextAlignmentOptions.TopLeft);
        sub.textWrappingMode = TextWrappingModes.NoWrap;
        sub.overflowMode = TextOverflowModes.Ellipsis;
        sub.text = "Câmara Municipal · spends 1 token";
        so.FindProperty("sub").objectReferenceValue = sub;

        // The card
        var card = Place(NewRect("Card", rt), pad, 126, w - 2 * pad, 70);
        Round(card, CardBorder, 19);
        var fill = Stretch(NewRect("Fill", card));
        fill.offsetMin = new Vector2(2.5f, 2.5f);
        fill.offsetMax = new Vector2(-2.5f, -2.5f);
        Round(fill, CardFill, 16.5f);
        var hexRt = NewRect("Hex", card);
        hexRt.anchorMin = hexRt.anchorMax = new Vector2(0, 0.5f);
        hexRt.pivot = new Vector2(0, 0.5f);
        hexRt.anchoredPosition = new Vector2(14, 0);
        hexRt.sizeDelta = new Vector2(40, 46);
        var hexImg = hexRt.gameObject.AddComponent<Image>();
        hexImg.sprite = hex;
        hexImg.color = CardBorder;
        hexImg.raycastTarget = false;
        Txt(Stretch(NewRect("A", hexRt)), bold, 21, Color.white, TextAlignmentOptions.Center).text = "A";
        var cardTitle = Txt(Place(NewRect("Title", card), 68, 0, w - 2 * pad - 68 - 16, 70), bold, 21, Ink, TextAlignmentOptions.MidlineLeft);
        cardTitle.textWrappingMode = TextWrappingModes.Normal;
        cardTitle.overflowMode = TextOverflowModes.Ellipsis;
        cardTitle.lineSpacing = -14;
        cardTitle.text = "Creating partnerships to enhance biodiversity";
        so.FindProperty("cardTitle").objectReferenceValue = cardTitle;

        // Answer
        float bw = (w - 2 * pad - 13) / 2;
        var decline = LobbyBuilder.PillButton(Place(NewRect("DeclineButton", rt), pad, 214, bw, 61), DeclineBg, "Decline", 23, DeclineInk);
        var accept = LobbyBuilder.PillButton(Place(NewRect("AcceptButton", rt), pad + bw + 13, 214, bw, 61), Pink, "Accept", 23, Color.white);
        so.FindProperty("declineButton").objectReferenceValue = decline;
        so.FindProperty("acceptButton").objectReferenceValue = accept;
        so.FindProperty("collabRequestText").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
