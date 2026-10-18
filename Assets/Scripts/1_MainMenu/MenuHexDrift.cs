using UnityEngine;

// A floating hexagon on the main menu: slowly bobs up and down (the only animation on the menu).
public class MenuHexDrift : MonoBehaviour
{
    public float height = 19f;   // how far up it goes, in canvas units
    public float period = 7f;    // seconds for one up-and-down
    public float delay;          // so the hexes don't move together

    private RectTransform _rt;
    private Vector2 _rest;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _rest = _rt.anchoredPosition;
    }

    private void Update()
    {
        float t = (Time.unscaledTime + delay) / period;
        float up = (1f - Mathf.Cos(t * 2f * Mathf.PI)) * 0.5f; // eases in and out
        _rt.anchoredPosition = _rest + new Vector2(0f, up * height);
    }
}
