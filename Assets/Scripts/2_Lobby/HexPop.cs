using UnityEngine;
using UnityEngine.UI;

// A board cell that "pops" (grows and brightens) in turn with its neighbours: the waiting loader.
public class HexPop : MonoBehaviour
{
    public float delay;
    public float cycle = 1.2f;

    private Image _image;
    private Color _color;

    private void Awake()
    {
        _image = GetComponent<Image>();
        if (_image) _color = _image.color;
    }

    private void Update()
    {
        float phase = Mathf.Repeat((Time.unscaledTime - delay) / cycle, 1f);
        float v = Mathf.SmoothStep(0f, 1f, phase < 0.45f ? phase / 0.45f : 1f - (phase - 0.45f) / 0.55f);
        transform.localScale = Vector3.one * Mathf.Lerp(0.78f, 1f, v);
        if (_image) _image.color = new Color(_color.r, _color.g, _color.b, Mathf.Lerp(0.45f, 1f, v));
    }
}
