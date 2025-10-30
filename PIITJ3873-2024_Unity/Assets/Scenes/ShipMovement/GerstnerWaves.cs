using UnityEngine;

[System.Serializable]
public struct GerstnerWave
{
    public float amplitude;   // A (m)
    public float wavelength;  // L (m)
    public float speed;       // c (m/s) (deixe 0 para usar deep-water: w = sqrt(g*k))
    public Vector2 direction; // eixo XZ normalizado
    [Range(0f, 2f)] public float steepness; // 0..~1.3 usual
}

public class GerstnerWaves : MonoBehaviour, IWaterHeightProvider
{
    public float baseWaterLevel = 0f;
    public GerstnerWave[] waves;

    const float g = 9.81f;
    const float TWO_PI = Mathf.PI * 2f;

    // Deslocamento completo (x,z,y) + normal e velocidade superficial
    public void Sample(Vector3 worldPos, float time, out float height, out Vector3 displacement, out Vector3 normal, out Vector3 surfaceVel)
    {
        height = baseWaterLevel;
        displacement = Vector3.zero;
        Vector3 dHdx = Vector3.zero; // derivadas p/ normal
        Vector3 dHdz = Vector3.zero;
        surfaceVel = Vector3.zero;

        for (int i = 0; i < waves.Length; i++)
        {
            var wv = waves[i];
            if (wv.wavelength <= 0f) continue;

            float k = TWO_PI / wv.wavelength;           // número de onda
            float w = (wv.speed > 0f) ? (wv.speed * k) : Mathf.Sqrt(g * k); // frequência
            Vector2 D = wv.direction.sqrMagnitude > 0.001f ? wv.direction.normalized : Vector2.right;

            float phase = k * (D.x * worldPos.x + D.y * worldPos.z) - w * time;

            float cosP = Mathf.Cos(phase);
            float sinP = Mathf.Sin(phase);

            // Fórmulas de Gerstner (com steepness)
            float Qi = Mathf.Clamp01(wv.steepness) * wv.amplitude * k; // “q” efetivo
            float Ax = D.x * (Qi * wv.amplitude * cosP);
            float Az = D.y * (Qi * wv.amplitude * cosP);
            float Ay = wv.amplitude * sinP;

            displacement += new Vector3(Ax, Ay, Az);
            height += Ay;

            // Derivadas para normal aproximada (parciais de y w.r.t x e z)
            float dYdx = wv.amplitude * k * D.x * cosP * 1f;
            float dYdz = wv.amplitude * k * D.y * cosP * 1f;
            dHdx += new Vector3(1f, dYdx, 0f);
            dHdz += new Vector3(0f, dYdz, 1f);

            // Velocidade superficial (aprox.) — derivada temporal do deslocamento
            if (wv.speed > 0f || true)
            {
                float dphase_dt = -w;
                float dcos_dt = -sinP * dphase_dt;
                float dsin_dt = cosP * dphase_dt;

                float dAx_dt = D.x * (Qi * wv.amplitude * dcos_dt);
                float dAz_dt = D.y * (Qi * wv.amplitude * dcos_dt);
                float dAy_dt = wv.amplitude * dsin_dt;

                surfaceVel += new Vector3(dAx_dt, dAy_dt, dAz_dt);
            }
        }

        // Normal por cruzamento das derivadas
        normal = Vector3.Normalize(Vector3.Cross(dHdz, dHdx));
    }

    // Compatível com seu controlador de flutuabilidade
    public bool TryGetWaterHeightAndVelocity(Vector3 worldPos, out float waterY, out Vector3 surfaceVelocity)
    {
        Sample(worldPos, Time.time, out waterY, out _, out _, out surfaceVelocity);
        return true;
    }
}
