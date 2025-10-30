using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class RiverDebrisSpawner : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Manager �nico da simula��o que cont�m a seed e os valores globais.")]
    public RiverSimulationManager manager;

    [Tooltip("Ponto base do rio neste spawner. O FORWARD dele define a dire��o do rio. O RIGHT dele define o lado.")]
    public Transform spawnLine;

    [Tooltip("Se voc� j� tem um objeto de �gua (water system, plano de �gua, etc.), arraste aqui. O spawner vai passar isso para cada floater.")]
    public GameObject waterObject;
    public WaterSurface waterSurface;

    [Header("Op��es")]
    public bool autoStart = true;
    public bool showDebugGizmos = true;

    [Tooltip("Se true, o rejeito instanciado herda a rota��o do spawnLine.")]
    public bool inheritRotation = true;

    private RiverSimulationConfig config;
    private bool isRunning = false;

    void Start()
    {
        // pega manager global se n�o foi arrastado
        if (manager == null)
            manager = RiverSimulationManager.Instance;

        if (manager == null || manager.config == null)
        {
            Debug.LogError($"[{name}] Spawner sem manager ou sem config.");
            enabled = false;
            return;
        }

        config = manager.config;

        // se n�o veio nada, usa o pr�prio transform
        if (spawnLine == null)
            spawnLine = this.transform;

        if (autoStart)
            StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        isRunning = true;
        while (isRunning)
        {
            // tempo determin�stico
            float wait = manager.RandRange(config.spawnIntervalRange);
            yield return new WaitForSeconds(wait);
            SpawnOne();
        }
    }

    public void SpawnOne()
    {
        if (config.debrisPrefabs == null || config.debrisPrefabs.Length == 0)
            return;

        // 1) escolhe o prefab de forma determin�stica
        int idx = manager.RandInt(0, config.debrisPrefabs.Length);
        GameObject prefab = config.debrisPrefabs[idx];

        // 2) base e eixos
        Vector3 basePos = spawnLine.position;
        Vector3 right = spawnLine.right;     // lado do rio
        Vector3 forward = spawnLine.forward; // dire��o do rio

        // 3) offsets determin�sticos
        float offsetX = manager.RandRange(config.spawnXRange);
        float offsetZ = manager.RandRange(config.spawnZRange);

        // 4) posi��o final
        Vector3 spawnPos = basePos + right * offsetX + forward * offsetZ;

        // 5) fixa Y na altura do spawnLine (pra n�o pegar inclina��o)
        spawnPos.y = basePos.y;

        // 6) rota��o
        Quaternion rot = inheritRotation ? spawnLine.rotation : Quaternion.identity;

        // 7) instancia
        GameObject go = Instantiate(prefab, spawnPos, rot);
        Floating f = go.GetComponent<Floating>();
        f.targetSurface = waterSurface;

        // 8) escala determin�stica
        float scale = manager.RandRange(config.sizeRange);
        go.transform.localScale = Vector3.one * scale;

        // --- CONTADORES / REGISTRO ---
        string typeId = prefab != null ? prefab.name : "Unknown";

        // 1) marca que spawnou este tipo
        if (DebrisRegistry.Instance != null)
            DebrisRegistry.Instance.RegisterSpawn(typeId);

        // 2) adiciona o ciclo de vida p/ contar ativos e destru�dos
        var life = go.AddComponent<DebrisLifecycle>();
        life.typeId = typeId;

        // 9) adiciona floater e passa par�metros globais
        //var floater = go.AddComponent<RiverDebrisFloater>();
        //floater.currentSpeed = manager.globalCurrentSpeed;
        //floater.windDir = manager.globalWindDir;
        //floater.windSpeed = manager.globalWindSpeed;
        //floater.riverForward = forward;

        // 10) se o spawner souber qual � o water system, passa para o floater
        //if (waterObject != null)
        //    floater.unityWaterObject = waterObject;
    }

    void OnDrawGizmosSelected()
    {
        Transform refT = spawnLine ? spawnLine : transform;
        Vector3 basePos = refT.position;
        Vector3 right = refT.right;
        Vector3 forward = refT.forward;

        // valores default se manager n�o existe ainda
        float xMin = -5f;
        float xMax = 5f;
        float zMin = 0f;
        float zMax = 0f;

        if (manager != null && manager.config != null)
        {
            xMin = manager.config.spawnXRange.x;
            xMax = manager.config.spawnXRange.y;
            zMin = manager.config.spawnZRange.x;
            zMax = manager.config.spawnZRange.y;
        }

        Vector3 p1 = basePos + right * xMin + forward * zMin;
        Vector3 p2 = basePos + right * xMax + forward * zMin;
        Vector3 p3 = basePos + right * xMax + forward * zMax;
        Vector3 p4 = basePos + right * xMin + forward * zMax;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);

        // seta da dire��o do rio
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(basePos, forward * 2f);

        // seta pro lado
        Gizmos.color = Color.red;
        Gizmos.DrawRay(basePos, right * 1.5f);
    }
}
