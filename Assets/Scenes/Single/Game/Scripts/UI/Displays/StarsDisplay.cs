using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class StarsDisplay : MonoBehaviour
{
    [Header("Спрайты")]
    public Sprite filledStar;
    public Sprite emptyStar;

    [Header("Настройки")]
    public int maxStars = 3;
    public Vector2 starSize = new(32, 32);
    public float spacing = 8f;

    private Image[] stars;

    public void SetStars(int count)
    {
        if (stars == null)
        {
            CreateStars();
        }

        count = Mathf.Clamp(count, 0, maxStars);

        for (int i = 0; i < maxStars; i++)
        {
            if (i < count)
                stars[i].sprite = filledStar;
            else
                stars[i].sprite = emptyStar;

            stars[i].color = Color.white;
        }
    }

    private void CreateStars()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        stars = new Image[maxStars];
        RectTransform parent = transform as RectTransform;

        for (int i = 0; i < maxStars; i++)
        {
            GameObject go = new("Star_" + i);
            go.transform.SetParent(transform, false);

            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.sprite = emptyStar; 

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = starSize;

            float totalWidth = maxStars * starSize.x + (maxStars - 1) * spacing;
            rt.anchoredPosition = new Vector2(
                -totalWidth * 0.5f + i * (starSize.x + spacing) + starSize.x * 0.5f,
                0
            );

            stars[i] = img;
        }
    }
}