using TMPro;
using UnityEngine;

public class PlayerResultHook : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text mainRatingText;
    [SerializeField] private TMP_Text personalRatingText;
    [SerializeField] private TMP_Text changeText;

    public void Setup(string playerName, string mainRating, string personalRating, string change)
    {
        playerNameText.text = playerName;
        mainRatingText.text = mainRating;
        personalRatingText.text = personalRating;
        changeText.text = change;
    }
}
