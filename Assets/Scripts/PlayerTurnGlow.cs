using UnityEngine;
using UnityEngine.UI;

// The pink outline around the capsule of the player whose turn it is: breathes in and out while shown.
// Added by the editor menu "BioValue/Build Player Capsules".
[RequireComponent(typeof(Image))]
public class PlayerTurnGlow : MonoBehaviour
{
    public float period = 1.4f;
    [Range(0f, 1f)] public float minAlpha = 0.55f;

    private Image image;
    private float baseAlpha;

    private void Awake()
    {
        image = GetComponent<Image>();
        baseAlpha = image.color.a;
    }

    private void Update()
    {
        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / period);
        var c = image.color;
        c.a = baseAlpha * Mathf.Lerp(minAlpha, 1f, t);
        image.color = c;
    }
}
