using UnityEngine;
using System.Collections.Generic;

public class HouseInteriorIntro : MonoBehaviour
{
    public List<string> houseMessages = new List<string>()
{
    "Dear Explorer,",
    "Welcome to the world of Canada!",
    "Here, you will discover new language skills and embark on a journey filled with adventure and learning.",
    "I am your guiding spirit, and I will be with you every step of the way.",
    "I have been told I am a bit clingy.",
    "Let us begin!",
    "Some obstacles will block your path — strike them down to continue.",
    "Be careful! Enemies will engage you the moment you get too close.",
    "Read the signs on the map, they might contain important information!",
    "Also, if you notice anything unusual, definitely try to interact with it!",
    "Interact with all objects in this room.",
    "Then finish the quiz to explore further. Good luck!"
};

    void Start()
    {
        PetBubble pet = FindFirstObjectByType<PetBubble>();
        if (pet != null)
        {
            pet.ShowMessagesToPlayer(houseMessages);
        }
        else
        {
            Debug.LogWarning("PetBubble not found in scene!");
        }
    }
}