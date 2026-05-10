using TMPro;
using UnityEngine;

public class PlayerResultHook : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text mainRatingText;
    [SerializeField] private TMP_Text personalRatingText;
    [SerializeField] private TMP_Text changeText;
    [SerializeField] private TMP_Text scoreText;

    public void Setup(string playerName, string mainRating, string personalRating, string change, string score)
    {
        if (playerNameText)     playerNameText.text     = playerName;
        if (mainRatingText)     mainRatingText.text     = mainRating;
        if (personalRatingText) personalRatingText.text = personalRating;
        if (changeText)         changeText.text         = change;
        if (scoreText)          scoreText.text          = score;
    }
}
