// Assets/Scripts/River/SunApplier.cs
using UnityEngine;

[RequireComponent(typeof(Light))]
public class SunApplier : MonoBehaviour
{
    public RiverSimulationManager manager;
    private Light sun;

    void Awake()
    {
        sun = GetComponent<Light>();
    }

    void Start()
    {
        if (manager == null)
            manager = RiverSimulationManager.Instance;

        if (manager == null)
            return;

        // aplica os valores que o manager já sorteou
        sun.intensity = manager.globalSunIntensity;
        sun.transform.rotation = Quaternion.Euler(manager.globalSunElev, manager.globalSunAzim, 0f);
    }
}
