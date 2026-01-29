using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SwitchButton : MonoBehaviour
{
   [SerializeField] private GameObject objectToSwitch;
   [SerializeField] private Sprite buttonImageA;
   [SerializeField] private Sprite buttonImageB;
   
   // string to appear on screen
   [SerializeField] private string textToShare = "";
   
   // text on UI
   [SerializeField] private TMP_Text displayText;
   
   // input field text
   [SerializeField] private TMP_InputField textInput;

   public bool isEmpty = true;

   public void Update()
   {
      switchSprite();
      if (!isEmpty)
      {
         displayText.text = "";
      }
   }

   public void switchSprite()
   {
      //if string is empty, set objectToSwitch image to buttonImageB sprite
      if (textInput.text == "")
      {
         
         objectToSwitch.GetComponent<Image>().sprite = buttonImageB;
         
      }
      else
      {
         isEmpty = false;
         objectToSwitch.GetComponent<Image>().sprite = buttonImageA;
      }
      
   }
   
   public void SetDisplayText()
   {
      if (isEmpty)
      {
         displayText.text = textToShare;
      }
   }

   
}
