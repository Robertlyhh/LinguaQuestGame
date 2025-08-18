using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PetManager : MonoBehaviour
{
    public PetBubble petBubble;

    [System.Serializable]
    public class PetTrigger
    {
        public Signal triggerSignal;
        public MessageSequence messageSequence;
    }

    public List<PetTrigger> petTriggers = new();

    // In PetManager.cs
    private void Awake()
    {
        if (petBubble == null) { /*...*/ return; }

        foreach (var trigger in petTriggers)
        {
            if (trigger.triggerSignal == null || trigger.messageSequence == null) { /*...*/ continue; }

            SignalListener listener = gameObject.AddComponent<SignalListener>();

            // Temporarily disable the listener component itself
            listener.enabled = false;

            // Configure it while it's disabled
            listener.signal = trigger.triggerSignal;
            listener.response.AddListener(() => petBubble.ShowMessagesToPlayer(trigger.messageSequence.messages));

            // Now, enable it. This will call its OnEnable(), and since 'signal' is now assigned, it will register correctly.
            listener.enabled = true;
        }
    }
}
