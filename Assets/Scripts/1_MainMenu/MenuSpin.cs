using UnityEngine;

// Turns a spinner (the "Connecting…" state of Start game).
public class MenuSpin : MonoBehaviour
{
    public float degreesPerSecond = 450f;

    private void Update()
    {
        transform.Rotate(0f, 0f, -degreesPerSecond * Time.unscaledDeltaTime);
    }
}
