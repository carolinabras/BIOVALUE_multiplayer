using UnityEngine;

public class FullScreenEnforcer : MonoBehaviour
{
    private void Awake()
    {
        Resolution native = Screen.currentResolution;
        Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
    }
}
