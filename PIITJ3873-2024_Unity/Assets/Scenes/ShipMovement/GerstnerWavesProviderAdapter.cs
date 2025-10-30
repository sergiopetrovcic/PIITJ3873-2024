using UnityEngine;

public class GerstnerWavesProviderAdapter : MonoBehaviour, IWaterHeightProvider
{
    public GerstnerWaves waves;

    public bool TryGetWaterHeightAndVelocity(Vector3 worldPos, out float waterY, out Vector3 surfaceVelocity)
    {
        if (waves == null) { waterY = 0f; surfaceVelocity = Vector3.zero; return false; }
        waves.Sample(worldPos, Time.time, out waterY, out _, out _, out surfaceVelocity);
        return true;
    }
}
