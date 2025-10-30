// Assets/Scripts/River/WaterApplier.cs
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WaterApplier : MonoBehaviour
{
    public RiverSimulationManager manager;

    void Start()
    {
        if (manager == null)
            manager = RiverSimulationManager.Instance;

        if (manager == null || manager.config == null)
            return;

        var rend = GetComponent<Renderer>();
        var mat = rend.material; // instancia
        Color c = manager.config.waterColor;
        c.a = manager.config.waterAlpha;

        // tente primeiro URP/Lit
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", c);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", c);
    }
}
