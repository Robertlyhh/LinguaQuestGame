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

    public FloatValue maxHealth;     // e.g. initialValue = 20, drag your max health asset
    public FloatValue currentHealth; // the SAME asset PlayerExploring uses

    void Start()
    {
        EnsureHeartSlots(GetRequiredHeartSlots());
        SetHeart(currentHealth.runtimeValue);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
            UpdateHeart();
    }

    public void UpdateHeart()
    {
        SetHeart(currentHealth.runtimeValue);
        Debug.Log("Heart updated: " + currentHealth.runtimeValue + " / " + maxHealth.runtimeValue);
    }

    public void SetHeart(float healthValue)
    {
        int maxHeartValue = Mathf.Max(1, Mathf.CeilToInt(maxHealth.runtimeValue));
        int requiredHeartSlots = Mathf.CeilToInt(maxHeartValue / 2f);

        EnsureHeartSlots(requiredHeartSlots);

        healthValue = Mathf.Clamp(healthValue, 0f, maxHeartValue);

        for (int i = 0; i < hearts.Length; i++)
        {
            bool isWithinCapacity = i < requiredHeartSlots;
            hearts[i].gameObject.SetActive(isWithinCapacity);
            if (!isWithinCapacity) continue;

            float remainingForSlot = healthValue - (i * 2f);

            if (remainingForSlot >= 2f)
                hearts[i].sprite = fullHeart;
            else if (remainingForSlot >= 1f)
                hearts[i].sprite = halfHeart;
            else
                hearts[i].sprite = emptyHeart;
        }
    }

    private int GetRequiredHeartSlots()
    {
        return Mathf.Max(1, Mathf.CeilToInt(maxHealth.runtimeValue / 2f));
    }

    // EnsureHeartSlots stays exactly the same as before
    private void EnsureHeartSlots(int requiredHeartSlots)
    {
        if (hearts == null || hearts.Length == 0 || hearts[0] == null)
        {
            Debug.LogWarning("[HeartManager] No heart template assigned.");
            return;
        }

        List<Image> heartList = new List<Image>(hearts);
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