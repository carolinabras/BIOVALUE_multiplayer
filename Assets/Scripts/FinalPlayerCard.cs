using ExitGames.Client.Photon;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FinalPlayerCard : MonoBehaviour
{
    [SerializeField] private Slider mainObjectiveSlider;
    [SerializeField] private Slider personalObjectiveSlider;
    [SerializeField] private TMP_InputField personalObjectiveChangeInput;
    [SerializeField] private TextMeshProUGUI playerNameText;

    [SerializeField] private TMP_Text mainObjectiveValueText;
    [SerializeField] private TMP_Text personalObjectiveValueText;

    public void Awake()
    {
        var me = PhotonNetwork.LocalPlayer;
        playerNameText.text = me.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var nameObj)
            ? nameObj as string
            : $"Player {me.ActorNumber}";

        ConfigureSlider(mainObjectiveSlider,     mainObjectiveValueText);
        ConfigureSlider(personalObjectiveSlider, personalObjectiveValueText);
    }

    private void ConfigureSlider(Slider slider, TMP_Text label)
    {
        if (slider == null) return;
        slider.minValue    = 0;
        slider.maxValue    = 10;
        slider.wholeNumbers = true;
        slider.value        = 0;

        if (label != null)
        {
            label.text = "0";
            slider.onValueChanged.AddListener(v => label.text = Mathf.RoundToInt(v).ToString());
        }
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
    }

    public void Closethis()
    {
        gameObject.SetActive(false);
    }
}
