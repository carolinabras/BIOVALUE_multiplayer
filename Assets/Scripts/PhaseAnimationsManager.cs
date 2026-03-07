using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PhaseAnimationsManager : MonoBehaviour
{
  [SerializeField] private RectTransform InstrumentsAnimator;
  private bool isOpenInstruments;
  
  
  [SerializeField] private RectTransform ActionAnimator;
  private bool isOpenActions;
  
  
  private Vector2 closedPosition;
  private Vector2 openedPosition;
  private Vector2 closedPosition2;
  private float with;
  
  [SerializeField] private float duration;

  private void Awake()
  {
    with = InstrumentsAnimator.rect.width;
    closedPosition = InstrumentsAnimator.anchoredPosition;
    openedPosition = new Vector2(InstrumentsAnimator.anchoredPosition.x + with, InstrumentsAnimator.anchoredPosition.y);
    closedPosition2 = new Vector2(InstrumentsAnimator.anchoredPosition.x + (with*2), InstrumentsAnimator.anchoredPosition.y);
  }

  //called by objects manager
  public void OpenInstruments()
  {
    isOpenInstruments = true;
    Vector2 target = openedPosition;
    LeanTween.move(InstrumentsAnimator, target, 0.25f).setEaseOutCubic();
  }

  //called by click on button
  public void CloseInstruments()
  {
    isOpenInstruments = false;
    Vector2 target = closedPosition2;
    LeanTween.move(InstrumentsAnimator, target, 0.25f);
  }

  public void OpenActions()
  {
    isOpenActions = true;
    Vector2 target = openedPosition;
    LeanTween.move(ActionAnimator, target, 0.25f);
  }

  public void CloseActions()
  {
    isOpenActions = false;
    Vector2 target = closedPosition2;
    LeanTween.move(ActionAnimator, target, 0.25f);
  }
  
}
