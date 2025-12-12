using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelCard : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public Image thumbnail;
    public TextMeshProUGUI levelNameText;
    public LocalizeStringEvent levelNameLSE;
    public TextMeshProUGUI scoreText;
    public LocalizeStringEvent scoreLSE;
    public Image checkmark;
    public Image[] stars;
    public Sprite filledStar;
    public Sprite emptyStar;

    public void InitCache()
    {
        if (thumbnail == null)
        {
            var imgs = GetComponentsInChildren<Image>(true);
            foreach (var i in imgs)
            {
                if (i.name.ToLower().Contains("thumbnail")) { thumbnail = i; break; }
            }
        }

        if (levelNameText == null)
            levelNameText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (levelNameLSE == null && levelNameText != null)
            levelNameLSE = levelNameText.GetComponent<LocalizeStringEvent>();

        if (scoreText == null)
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t.name.ToLower().Contains("score")) { scoreText = t; break; }
            }
        }

        if (scoreLSE == null && scoreText != null)
            scoreLSE = scoreText.GetComponent<LocalizeStringEvent>();

        if (checkmark == null)
        {
            var imgs = GetComponentsInChildren<Image>(true);
            foreach (var i in imgs)
            {
                if (i.name.ToLower().Contains("check")) { checkmark = i; break; }
            }
        }

        if (stars == null || stars.Length == 0)
        {
            var imgs = GetComponentsInChildren<Image>(true);
            var found = new System.Collections.Generic.List<Image>();
            foreach (var i in imgs)
            {
                if (i.name.ToLower().Contains("star")) found.Add(i);
            }
            stars = found.ToArray();
        }

        if (filledStar == null || emptyStar == null)
        {
            var imgs = GetComponentsInChildren<Image>(true);
            if (imgs.Length >= 2)
            {
                if (filledStar == null) filledStar = imgs[0].sprite;
                if (emptyStar == null) emptyStar = imgs[1].sprite;
            }
        }
    }

    public void UpdateUI(Sprite thumb, string levelKey, bool unlocked, bool completed, int score, int starsCount)
    {
        if (thumbnail != null)
            thumbnail.sprite = unlocked ? thumb : thumbnail.sprite;

        if (levelNameLSE != null)
            LocalizationHelper.SetLocalizedText(levelNameLSE, unlocked && !string.IsNullOrEmpty(levelKey) ? levelKey : "level_card_locked");
        else if (levelNameText != null)
            levelNameText.text = unlocked && !string.IsNullOrEmpty(levelKey) ? levelKey : "Locked";

        if (checkmark != null)
            checkmark.gameObject.SetActive(completed);

        if (stars != null && stars.Length > 0)
        {
            bool anyFilled = starsCount > 0;

            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;

                if (filledStar != null && emptyStar != null)
                    stars[i].sprite = i < starsCount ? filledStar : emptyStar;

                stars[i].gameObject.SetActive(anyFilled && i < 3);
            }
        }

        if (scoreText != null)
        {
            if (completed)
                LocalizationHelper.SetLocalizedText(scoreLSE, "level_card_score", Mathf.Max(0, score));
            else
            {
                if (scoreLSE != null) scoreLSE.enabled = false;
                scoreText.text = "";
            }
        }
    }
}