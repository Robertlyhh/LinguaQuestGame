using UnityEngine;

public class EnergyHUDBinder : MonoBehaviour
{
    [SerializeField] private EnergyBar energyBar;

    void Update()
    {
        var gm = SwordWaveManager.Instance;
        if (!gm || !energyBar) return;
        energyBar.Set01((float)gm.energy / gm.maxEnergy);
    }
}
