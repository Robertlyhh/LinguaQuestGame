using System;
using UnityEngine;

public enum HokkienInteractionMode
{
    World,
    Dialogue,
    Quiz
}

public static class HokkienInteractionState
{
    private static UnityEngine.Object currentOwner;

    public static HokkienInteractionMode CurrentMode { get; private set; } = HokkienInteractionMode.World;
    public static event Action ModeChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        currentOwner = null;
        CurrentMode = HokkienInteractionMode.World;
        ModeChanged = null;
    }

    public static bool TryEnter(HokkienInteractionMode mode, UnityEngine.Object owner)
    {
        if (owner == null || mode == HokkienInteractionMode.World)
        {
            return false;
        }

        // A destroyed owner should not leave gameplay locked.
        if (currentOwner == null && CurrentMode != HokkienInteractionMode.World)
        {
            CurrentMode = HokkienInteractionMode.World;
        }

        if (CurrentMode != HokkienInteractionMode.World)
        {
            return currentOwner == owner && CurrentMode == mode;
        }

        currentOwner = owner;
        CurrentMode = mode;
        ModeChanged?.Invoke();
        return true;
    }

    public static void Exit(UnityEngine.Object owner)
    {
        if (owner == null || currentOwner != owner)
        {
            return;
        }

        currentOwner = null;
        CurrentMode = HokkienInteractionMode.World;
        ModeChanged?.Invoke();
    }

    public static void RefreshWorldInteractions()
    {
        if (CurrentMode == HokkienInteractionMode.World)
        {
            ModeChanged?.Invoke();
        }
    }
}
