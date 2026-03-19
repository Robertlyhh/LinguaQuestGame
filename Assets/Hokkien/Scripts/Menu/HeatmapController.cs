using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeatmapController : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject cellPrefab;
    public GameObject monthLabelPrefab; 

    [Header("Containers")]
    public Transform gridContainer;
    public Transform monthLabelContainer;

    [Header("Colors")]
    public Color level0 = new Color(0.92f, 0.93f, 0.94f); 
    public Color level1 = new Color(0.61f, 0.88f, 0.57f); 
    public Color level2 = new Color(0.26f, 0.77f, 0.33f); 
    public Color level3 = new Color(0.19f, 0.63f, 0.23f); 
    public Color level4 = new Color(0.13f, 0.43f, 0.16f); 

    private Dictionary<string, int> mockData;

    void Start()
    {
        FetchMockData();
        GenerateHeatmap();
    }

    private void FetchMockData()
    {
        mockData = new Dictionary<string, int>
        {
            { "2026-03-18", 5 },
            { "2026-03-17", 12 },
            { "2026-03-16", 8 },
            { "2026-02-20", 3 }
        };
    }

    private void GenerateHeatmap()
    {
        int totalWeeks = 20; // 140 days
        
        // 1. Get Today's date but strip the time (00:00:00)
        DateTime today = DateTime.Today; 

        // 2. Find the start of the current week (Sunday)
        int daysToLastSunday = (int)today.DayOfWeek; 
        DateTime currentWeekSunday = today.AddDays(-daysToLastSunday);

        // 3. Go back exactly 19 weeks from this Sunday to get our 20-week start
        DateTime startDate = currentWeekSunday.AddDays(-(totalWeeks - 1) * 7);

        // Loop through weeks (columns)
        for (int w = 0; w < totalWeeks; w++)
        {
            DateTime weekStart = startDate.AddDays(w * 7);

            // --- Spawn Month Label for this Column ---
            bool isNewMonthColumn = false;
            string monthText = "";

            if (w == 0)
            {
                // Always label the very first column so it isn't blank
                isNewMonthColumn = true;
                monthText = weekStart.ToString("MMM");
            }
            else
            {
                // Check if the 1st day of ANY month falls inside this specific week
                for (int d = 0; d < 7; d++)
                {
                    DateTime checkDate = weekStart.AddDays(d);
                    if (checkDate.Day == 1)
                    {
                        isNewMonthColumn = true;
                        monthText = checkDate.ToString("MMM");
                        break; // Found the 1st, no need to check the rest of the week
                    }
                }
            }

            if (isNewMonthColumn)
            {
                SpawnLabel(monthText);
            }
            else
            {
                SpawnLabel(""); // Spacer
            }

            // --- Spawn 7 Cells for this Column ---
            for (int d = 0; d < 7; d++)
            {
                DateTime currentDate = weekStart.AddDays(d);
                string dateKey = currentDate.ToString("yyyy-MM-dd");

                GameObject cell = Instantiate(cellPrefab, gridContainer);
                int activity = 0;
                mockData.TryGetValue(dateKey, out activity);
                cell.GetComponent<Image>().color = GetColor(activity);
            }
        }
    }
    
    private void SpawnLabel(string text)
    {
        GameObject label = Instantiate(monthLabelPrefab, monthLabelContainer);
        label.GetComponent<TextMeshProUGUI>().text = text;
        
        // Exact math: Cell Width (50) + Spacing (5) = 55
        var layout = label.GetComponent<LayoutElement>();
        if (layout != null) 
        {
            layout.preferredWidth = 55f; 
        }
    }

    private Color GetColor(int count)
    {
        if (count == 0) return level0;
        if (count < 3) return level1;
        if (count < 6) return level2;
        if (count < 10) return level3;
        return level4;
    }
}