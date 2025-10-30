using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RiverDebrisSpawner : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Manager único da simulação que contém a seed e os valores globais.")]
    public RiverSimulationManager manager;

    [Tooltip("Ponto base do rio neste spawner. O FORWARD define a direção; o RIGHT define o lado.")]
    public Transform spawnLine;

    [Tooltip("Se você tem um objeto de água (water system / plano), arraste aqui. Repasado ao floater.")]
    public GameObject waterObject;

    [Header("Execução")]
    public bool autoStart = true;
    public bool showDebugGizmos = true;
    [Tooltip("Se true, o rejeito instanciado herda a rotação do spawnLine.")]
    public bool inheritRotation = true;

    [Header("Limites de ATIVOS (opcional)")]
    [Tooltip("Máximo de objetos ATIVOS deste spawner. -1 desativa.")]
    public int maxActivePerSpawner = -1;

    [Tooltip("Usa DebrisRegistry para impor limite GLOBAL de ATIVOS (soma de todos os spawners). -1 desativa.")]
    public int maxActiveGlobal = -1;

    [Header("Limites TOTAIS (definitivos)")]
    [Tooltip("Máximo TOTAL de spawns gerados por ESTE spawner (contagem definitiva). -1 desativa.")]
    public int maxTotalSpawnedPerSpawner = 100;

    [Tooltip("Máximo TOTAL GLOBAL de spawns (todos os spawners somados). Requer DebrisRegistry. -1 desativa.")]
    public int maxTotalSpawnedGlobal = -1;

    // estado interno
    private RiverSimulationConfig config;
    private bool isRunning = false;
    private readonly List<GameObject> _alive = new List<GameObject>();
    private int _totalSpawnedPerSpawner = 0;

    void Start()
    {
        if (manager == null)
            manager = RiverSimulationManager.Instance;

        if (manager == null || manager.config == null)
        {
            Debug.LogError($"[{name}] Spawner sem manager ou sem config.");
            enabled = false;
            return;
        }

        config = manager.config;
        if (spawnLine == null) spawnLine = this.transform;

        if (autoStart)
            StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        isRunning = true;

        while (isRunning)
        {
            // Checagem de limite TOTAL (antes de gastar PRNG/tempo)
            if (ReachedTotalLimits())
            {
                // Para definitivamente este spawner
                isRunning = false;
                yield break;
            }

            // Espera determinística
            float wait = manager.RandRange(config.spawnIntervalRange);
            yield return new WaitForSeconds(wait);

            // Checa novamente (pode ter mudado no intervalo)
            if (ReachedTotalLimits())
            {
                isRunning = false;
                yield break;
            }

            // Checagem de limite de ATIVOS (opcional)
            if (!CanSpawnConsideringActiveLimits())
                continue; // pula este ciclo, tenta depois

            SpawnOneInternal();
        }
    }

    bool ReachedTotalLimits()
    {
        // Limite total por spawner
        if (maxTotalSpawnedPerSpawner >= 0 && _totalSpawnedPerSpawner >= maxTotalSpawnedPerSpawner)
            return true;

        // Limite total global via DebrisRegistry
        if (maxTotalSpawnedGlobal >= 0 && DebrisRegistry.Instance != null)
        {
            var snap = DebrisRegistry.Instance.GetSnapshot();
            int totalSpawnedGlobal = snap.Values.Sum(v => v.spawned);
            if (totalSpawnedGlobal >= maxTotalSpawnedGlobal)
                return true;
        }

        return false;
    }

    bool CanSpawnConsideringActiveLimits()
    {
        // Limite de ativos por spawner
        if (maxActivePerSpawner >= 0)
        {
            PruneDead();
            if (_alive.Count >= maxActivePerSpawner)
                return false;
        }

        // Limite de ativos global
        if (maxActiveGlobal >= 0 && DebrisRegistry.Instance != null)
        {
            var snap = DebrisRegistry.Instance.GetSnapshot();
            int totalActive = snap.Values.Sum(v => v.active);
            if (totalActive >= maxActiveGlobal)
                return false;
        }

        return true;
    }

    void SpawnOneInternal()
    {
        if (config.debrisPrefabs == null || config.debrisPrefabs.Length == 0)
            return;

        // 1) escolhe prefab determinístico
        int idx = manager.RandInt(0, config.debrisPrefabs.Length);
        GameObject prefab = config.debrisPrefabs[idx];

        // 2) base e eixos
        Vector3 basePos = spawnLine.position;
        Vector3 right = spawnLine.right;
        Vector3 forward = spawnLine.forward;

        // 3) offsets determinísticos
        float offsetX = manager.RandRange(config.spawnXRange);
        float offsetZ = manager.RandRange(config.spawnZRange);

        // 4) posição final
        Vector3 spawnPos = basePos + right * offsetX + forward * offsetZ;
        spawnPos.y = basePos.y;

        // 5) rotação
        Quaternion rot = inheritRotation ? spawnLine.rotation : Quaternion.identity;

        // 6) instancia
        GameObject go = Instantiate(prefab, spawnPos, rot);

        // 7) escala determinística
        float scale = manager.RandRange(config.sizeRange);
        go.transform.localScale = Vector3.one * scale;

        // 8) floater e parâmetros
        var floater = go.AddComponent<RiverDebrisFloater>();
        floater.currentSpeed = manager.globalCurrentSpeed;
        floater.windDir = manager.globalWindDir;
        floater.windSpeed = manager.globalWindSpeed;
        floater.riverForward = forward;
        if (waterObject != null)
            floater.unityWaterObject = waterObject;

        // 9) contagens locais e globais
        _alive.Add(go);
        _totalSpawnedPerSpawner++;

        if (DebrisRegistry.Instance != null)
        {
            string typeId = prefab != null ? prefab.name : "Unknown";
            DebrisRegistry.Instance.RegisterSpawn(typeId);

            var life = go.AddComponent<DebrisLifecycle>();
            life.typeId = typeId;
        }
    }

    void PruneDead()
    {
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null)
                _alive.RemoveAt(i);
        }
    }

    void OnDrawGizmosSelected()
    {
        Transform refT = spawnLine ? spawnLine : transform;
        Vector3 basePos = refT.position;
        Vector3 right = refT.right;
        Vector3 fwd = refT.forward;

        float xMin = -5f, xMax = 5f, zMin = 0f, zMax = 0f;
        if (manager != null && manager.config != null)
        {
            xMin = manager.config.spawnXRange.x;
            xMax = manager.config.spawnXRange.y;
            zMin = manager.config.spawnZRange.x;
            zMax = manager.config.spawnZRange.y;
        }

        Vector3 p1 = basePos + right * xMin + fwd * zMin;
        Vector3 p2 = basePos + right * xMax + fwd * zMin;
        Vector3 p3 = basePos + right * xMax + fwd * zMax;
        Vector3 p4 = basePos + right * xMin + fwd * zMax;

        Gizmos.color = Color.cyan; Gizmos.DrawLine(p1, p2); Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4); Gizmos.DrawLine(p4, p1);

        Gizmos.color = Color.blue; Gizmos.DrawRay(basePos, fwd * 2f);
        Gizmos.color = Color.red; Gizmos.DrawRay(basePos, right * 1.5f);
    }
}
