using UnityEngine;

public interface IWaterHeightProvider
{
    /// <summary>
    /// Retorna a altura da superfície da água (Y) e opcionalmente a velocidade superficial.
    /// Se não souber a velocidade, retorne Vector3.zero.
    /// </summary>
    bool TryGetWaterHeightAndVelocity(Vector3 worldPos, out float waterY, out Vector3 surfaceVelocity);
}
