using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AmmoSlotUI : MonoBehaviour
{
    public Image iconImage;
    public Image frameHighlight;
    public TextMeshProUGUI bulletNameText;
    public TextMeshProUGUI bulletTypeText;

    public Color normalColor = new(1, 1, 1, 0f);
    public Color selectedColor = Color.yellow;

    public void SetBullet(Sprite icon, string bulletName, BulletType bulletType)
    {
        bool active = icon != null;
        gameObject.SetActive(active);

        if (iconImage != null)
            iconImage.sprite = icon;

        if (bulletTypeText != null)
            bulletTypeText.text = active ? bulletType.ToString() : "";

        if (bulletNameText != null)
            bulletNameText.text = active ? bulletName ?? "" : "";
    }

    public void SetSelected(bool selected)
    {
        if (frameHighlight != null)
        {
            frameHighlight.gameObject.SetActive(selected);
        }
    }
}