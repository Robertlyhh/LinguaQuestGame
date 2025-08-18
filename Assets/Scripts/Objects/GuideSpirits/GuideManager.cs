using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class GuideManager : MonoBehaviour
{
    public SpiritBubble guideSpirit;

    [System.Serializable]
    public class GuideTrigger
    {
        public Signal triggerSignal;
        public MessageSequence messageSequence;
    }

    public List<GuideTrigger> guideTriggers = new();

    // In GuideManager.cs
    private void Awake()
    {
        if (guideSpirit == null) { /*...*/ return; }

        foreach (var trigger in guideTriggers)
        {
            if (trigger.triggerSignal == null || trigger.messageSequence == null) { /*...*/ continue; }

            SignalListener listener = gameObject.AddComponent<SignalListener>();

            // Temporarily disable the listener component itself
            listener.enabled = false;

            // Configure it while it's disabled
            listener.signal = trigger.triggerSignal;
            listener.response.AddListener(() => guideSpirit.ShowMessagesToPlayer(trigger.messageSequence.messages));

            // Now, enable it. This will call its OnEnable(), and since 'signal' is now assigned, it will register correctly.
            listener.enabled = true;
        }
    }
}
