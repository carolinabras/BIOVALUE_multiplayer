using UnityEngine;
using UnityEngine.UI;

public class PlayerButton : MonoBehaviour
{
    
        public int playerCount;
        public Sprite normalSprite;
        public Sprite selectedSprite;
        public Image image;

        public void ToggleButton(bool isSelected)
        {
            
            {
                var image = this.GetComponent<Image>();
                image.sprite = isSelected ? selectedSprite : normalSprite;
            }
        }
}

   
