using System.Reflection;
using UnityEngine;

public class WaterWindHack : MonoBehaviour
{
    public Component waterSurface; // arraste o WaterSurface
    public float wind = 60f;       // m/s “sem limite”
    public float choppy = 2f;      // mais crista

    void Start()
    {
        var t = waterSurface.GetType();
        t.GetField("m_WindSpeed", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(waterSurface, wind);
        t.GetField("m_Choppiness", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(waterSurface, choppy);
    }
}
