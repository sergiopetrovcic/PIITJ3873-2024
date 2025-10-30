using UnityEngine;

/// <summary>
/// Anexe automaticamente nos objetos spawnados.
/// Conta 'active' no Awake e 'destroyed' no OnDestroy.
/// </summary>
public class DebrisLifecycle : MonoBehaviour
{
    [Tooltip("Identificador do tipo (ex.: nome do prefab).")]
    public string typeId;

    private bool _activated;

    void Awake()
    {
        if (!string.IsNullOrEmpty(typeId) && DebrisRegistry.Instance != null)
        {
            DebrisRegistry.Instance.RegisterActivate(typeId);
            _activated = true;
        }
    }

    void OnDestroy()
    {
        if (_activated && DebrisRegistry.Instance != null)
        {
            DebrisRegistry.Instance.RegisterDestroy(typeId);
            _activated = false;
        }
    }
}
