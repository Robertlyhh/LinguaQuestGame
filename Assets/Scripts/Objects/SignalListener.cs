using UnityEngine;
using UnityEngine.Events;

public class SignalListener : MonoBehaviour
{
    // Public so it can be set in the Inspector OR by code.
    public Signal signal;

    // Initialized to prevent nulls when added via code.
    public UnityEvent response = new UnityEvent();

    // OnEnable works for the manual setup.
    private void OnEnable()
    {
        if (signal != null)
        {
            signal.RegisterListener(this);
        }
    }

    private void OnDisable()
    {
        if (signal != null)
        {
            signal.UnregisterListener(this);
        }
    }

    public void OnSignalRaised()
    {
        response?.Invoke();
    }
}