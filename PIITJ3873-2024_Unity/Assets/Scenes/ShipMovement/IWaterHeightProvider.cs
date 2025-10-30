using UnityEngine;

public interface IWaterHeightProvider
{
    /// <summary>
    /// Retorna a altura da superf�cie da �gua (Y) e opcionalmente a velocidade superficial.
    /// Se n�o souber a velocidade, retorne Vector3.zero.
    /// </summary>
    bool TryGetWaterHeightAndVelocity(Vector3 worldPos, out float waterY, out Vector3 surfaceVelocity);
}
