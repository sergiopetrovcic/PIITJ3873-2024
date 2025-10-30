using UnityEngine;

/// <summary>
/// Gerador simples de ondas Gerstner para fallback.
/// Ajuste amplitudes/direções/frequência para aproximar o visual do Water System.
/// </summary>
public class GerstnerFallbackProvider : MonoBehaviour, IWaterHeightProvider
{
    [Header("Gerstner Waves (até 4 componentes)")]
    public float baseWaterLevel = 0f;

    public float amplitude1 = 0.6f;
    public float wavelength1 = 20f;
    public float speed1 = 5f;
    public Vector2 direction1 = new Vector2(1, 0);

    public float amplitude2 = 0.3f;
    public float wavelength2 = 12f;
    public float speed2 = 3f;
    public Vector2 direction2 = new Vector2(0.5f, 0.86f);

    public float amplitude3 = 0.2f;
    public float wavelength3 = 8f;
    public float speed3 = 2f;
    public Vector2 direction3 = new Vector2(-0.7f, 0.7f);

    public float amplitude4 = 0.1f;
    public float wavelength4 = 5f;
    public float speed4 = 1.2f;
    public Vector2 direction4 = new Vector2(0.2f, -0.98f);

    private float TwoPi = Mathf.PI * 2f;

    public bool TryGetWaterHeightAndVelocity(Vector3 worldPos, out float waterY, out Vector3 surfaceVelocity)
    {
        float t = Time.time;

        waterY = baseWaterLevel;
        surfaceVelocity = Vector3.zero; // opcional: compute derivadas p/ vel.

        Accumulate(ref waterY, worldPos, amplitude1, wavelength1, speed1, direction1.normalized, t);
        Accumulate(ref waterY, worldPos, amplitude2, wavelength2, speed2, direction2.normalized, t);
        Accumulate(ref waterY, worldPos, amplitude3, wavelength3, speed3, direction3.normalized, t);
        Accumulate(ref waterY, worldPos, amplitude4, wavelength4, speed4, direction4.normalized, t);

        return true;
    }

    private void Accumulate(ref float y, Vector3 pos, float A, float L, float S, Vector2 D, float t)
    {
        if (A == 0f || L == 0f) return;
        float k = TwoPi / L;
        float w = Mathf.Sqrt(9.81f * k); // disp. deep-water (aprox); pode usar S como override
        float phase = k * (D.x * pos.x + D.y * pos.z) - (S > 0 ? S : w) * t;
        y += A * Mathf.Sin(phase);
    }
}
