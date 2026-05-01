using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class HeartManager : MonoBehaviour
{
    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite emptyHeart;
    public Sprite halfHeart;
    public FloatValue heartContainers;

    // Start is called before the first frame update
    void Start()
    {
        EnsureHeartSlots(GetRequiredHeartSlots());
        SetHeart((int)heartContainers.runtimeValue);
    }

    public void SetHeart(int heartCount)
    {
        int maxHeartValue = GetMaxHeartValue();
        int requiredHeartSlots = Mathf.CeilToInt(maxHeartValue / 2f);

        EnsureHeartSlots(requiredHeartSlots);
        heartCount = Mathf.Clamp(heartCount, 0, maxHeartValue);

        for (int i = 0; i < hearts.Length; i++)
        {
            bool isWithinCapacity = i < requiredHeartSlots;
            hearts[i].gameObject.SetActive(isWithinCapacity);

            if (!isWithinCapacity)
            {
                continue;
            }

            int remainingValueForSlot = heartCount - (i * 2);
            if (remainingValueForSlot >= 2)
            {
                hearts[i].sprite = fullHeart;
            }
            else if (remainingValueForSlot == 1)
            {
                hearts[i].sprite = halfHeart;
            }
            else
            {
                hearts[i].sprite = emptyHeart;
            }
        }
    }

    public void UpdateHeart()
    {
        SetHeart((int)heartContainers.runtimeValue);
        Debug.Log("Heart count updated to: " + heartContainers.runtimeValue);
    }

    private int GetRequiredHeartSlots()
    {
        return Mathf.Max(1, Mathf.CeilToInt(GetMaxHeartValue() / 2f));
    }

    private int GetMaxHeartValue()
    {
        if (heartContainers == null)
        {
            return hearts != null ? hearts.Length * 2 : 0;
        }

        float configuredMax = heartContainers.maxValue > 0
            ? heartContainers.maxValue
            : Mathf.Max(heartContainers.initialValue, heartContainers.runtimeValue);

        return Mathf.Max(1, Mathf.CeilToInt(configuredMax));
    }

    private void EnsureHeartSlots(int requiredHeartSlots)
    {
        if (hearts == null || hearts.Length == 0 || hearts[0] == null)
        {
            Debug.LogWarning("[HeartManager] No heart template is assigned.");
            return;
        }

        List<Image> heartList = new List<Image>();
        foreach (var heart in hearts)
        {
            if (heart != null)
            {
                heartList.Add(heart);
            }
        }

        Image template = heartList[0];
        while (heartList.Count < requiredHeartSlots)
        {
            Image clone = Instantiate(template, template.transform.parent);
            clone.name = template.name + "_" + heartList.Count;
            clone.sprite = emptyHeart;
            clone.gameObject.SetActive(true);
            clone.transform.SetSiblingIndex(heartList.Count);
            heartList.Add(clone);
        }

        hearts = heartList.ToArray();
    }
}
