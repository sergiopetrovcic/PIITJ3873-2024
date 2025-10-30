using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registro global de contadores de rejeitos por tipo.
/// </summary>
public class DebrisRegistry : MonoBehaviour
{
    public static DebrisRegistry Instance { get; private set; }

    public class Counts
    {
        public int spawned;
        public int active;
        public int destroyed;
    }

    // chave = typeId (ex.: nome do prefab), valor = contadores
    private readonly Dictionary<string, Counts> _data = new Dictionary<string, Counts>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Mais de um DebrisRegistry na cena. Destruindo duplicado.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Opcional: evitar destruir ao trocar de cena
        // DontDestroyOnLoad(gameObject);
    }

    public void RegisterSpawn(string typeId)
    {
        var c = Get(typeId);
        c.spawned++;
        // não incrementa active aqui — o active conta no Awake do DebrisLifecycle
    }

    public void RegisterActivate(string typeId)
    {
        var c = Get(typeId);
        c.active++;
    }

    public void RegisterDestroy(string typeId)
    {
        var c = Get(typeId);
        c.active = Mathf.Max(0, c.active - 1);
        c.destroyed++;
    }

    private Counts Get(string typeId)
    {
        if (!_data.TryGetValue(typeId, out var c))
        {
            c = new Counts();
            _data[typeId] = c;
        }
        return c;
    }

    /// <summary>
    /// Snapshot seguro p/ leitura no HUD (cópia).
    /// </summary>
    public Dictionary<string, Counts> GetSnapshot()
    {
        var copy = new Dictionary<string, Counts>(_data.Count);
        foreach (var kv in _data)
        {
            copy[kv.Key] = new Counts
            {
                spawned = kv.Value.spawned,
                active = kv.Value.active,
                destroyed = kv.Value.destroyed
            };
        }
        return copy;
    }

    /// <summary>
    /// Zera tudo (opcional, útil para testes).
    /// </summary>
    public void ResetAll()
    {
        _data.Clear();
    }
}
