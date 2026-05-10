using System;
using UnityEngine;

public class closeThisPanel : MonoBehaviour
{
  public GameObject parent;

  public void Awake()
  {
  
    
    
  }

  public void CloseThisPanel()
  {
    {
      if (parent == null || !parent.activeSelf) return;
      LeanTween.cancel(parent);
      parent.GetComponent<RectTransform>().localScale = Vector3.one;
      LeanTween.scale(parent.GetComponent<RectTransform>(), Vector3.zero, 0.25f)
        .setEaseInBack()
        .setOnComplete(() =>
        {
            parent.SetActive(false);
            parent.GetComponent<RectTransform>().localScale = Vector3.one;
        });
    }
  }
}
