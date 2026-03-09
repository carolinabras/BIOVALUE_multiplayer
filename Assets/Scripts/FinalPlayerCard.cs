using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class FinalPlayerCard : MonoBehaviour
{
    [SerializeField] private TMP_InputField mainObjectiveRatingInput;
    [SerializeField] private TMP_InputField personalObjectiveRatingInput;
    [SerializeField] private TMP_InputField personalObjectiveChangeInput;
    [SerializeField] private TextMeshProUGUI playerNameText;

    public void Awake()
    {
        var me = PhotonNetwork.LocalPlayer;
        if (me.CustomProperties.TryGetValue(BiovalueStatics.PlayerNameKey, out var nameObj))
            playerNameText.text = nameObj as string;
        else
            playerNameText.text = $"Player {me.ActorNumber}";
    }

    public void Save()
    {
        Hashtable props = new Hashtable();
        props[BiovalueStatics.MainObjectiveRatingKey] = mainObjectiveRatingInput.text;
        props[BiovalueStatics.PersonalObjectiveRatingKey] = personalObjectiveRatingInput.text;
        props[BiovalueStatics.PersonalObjectiveChangeKey] = personalObjectiveChangeInput.text;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void Closethis()
    {
        this.gameObject.SetActive(false);
    }
}
