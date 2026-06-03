using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FinalPlayerCard : MonoBehaviourPunCallbacks
{
    [SerializeField] private Slider mainObjectiveSlider;
    [SerializeField] private Slider personalObjectiveSlider;
    [SerializeField] private TMP_InputField personalObjectiveChangeInput;
    [SerializeField] private TextMeshProUGUI playerNameText;

    [SerializeField] private TMP_Text mainObjectiveValueText;
    [SerializeField] private TMP_Text personalObjectiveValueText;
    [SerializeField] private GameObject resultsSaveText;
    [SerializeField] private GameObject panelRoot;

    private const string EndGameKey = "endGame";
    private const string ActionsRoundEndedKey = "actionsRoundEnded";

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = transform.Find("Panel")?.gameObject;
    }

    public void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (PhotonNetwork.IsMasterClient) return;

        var me = PhotonNetwork.LocalPlayer;
        playerNameText.text = me.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var nameObj)
            ? nameObj as string
            : $"Player {me.ActorNumber}";

        ConfigureSlider(mainObjectiveSlider,     mainObjectiveValueText);
        ConfigureSlider(personalObjectiveSlider, personalObjectiveValueText);

        var props = PhotonNetwork.CurrentRoom?.CustomProperties;
        if (props == null) return;
        if ((props.TryGetValue(ActionsRoundEndedKey, out var av) && av is bool ab && ab) ||
            (props.TryGetValue(EndGameKey, out var ev) && ev is bool b && b))
            ShowPanel();
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (PhotonNetwork.IsMasterClient) return;
        if (changedProps.ContainsKey(ActionsRoundEndedKey) || changedProps.ContainsKey(EndGameKey))
            ShowPanel();
    }

    private void ConfigureSlider(Slider slider, TMP_Text label)
    {
        if (slider == null) return;
        slider.minValue     = 0;
        slider.maxValue     = 10;
        slider.wholeNumbers = true;
        slider.value        = 0;

        if (label != null)
        {
            label.text = "0";
            slider.onValueChanged.AddListener(v => label.text = Mathf.RoundToInt(v).ToString());
        }
    }

    public void ShowPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Save()
    {
        int mainRating     = mainObjectiveSlider     != null ? Mathf.RoundToInt(mainObjectiveSlider.value)     : 0;
        int personalRating = personalObjectiveSlider != null ? Mathf.RoundToInt(personalObjectiveSlider.value) : 0;

        var props = new Hashtable
        {
            [BiovalueStatics.MainObjectiveRatingKey]     = mainRating,
            [BiovalueStatics.PersonalObjectiveRatingKey] = personalRating,
            [BiovalueStatics.PersonalObjectiveChangeKey] = personalObjectiveChangeInput != null
                ? personalObjectiveChangeInput.text
                : string.Empty,
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (resultsSaveText != null)
            resultsSaveText.SetActive(true);
    }

    public void Closethis()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
