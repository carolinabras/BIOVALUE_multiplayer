using UnityEngine;

public class FullScreenEnforcer : MonoBehaviour
{
    private void Awake()
    {
#if !UNITY_WEBGL // a browser only goes full screen after a click (the page's full-screen button)
        Resolution native = Screen.currentResolution;
        Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
#endif
    }
}
