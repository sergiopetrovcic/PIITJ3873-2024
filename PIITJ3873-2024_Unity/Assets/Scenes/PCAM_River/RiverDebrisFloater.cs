using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(Transform))]
public class RiverDebrisFloater : MonoBehaviour
{
    [Header("Movimento")]
    public float currentSpeed = 2f;      // velocidade da correnteza
    public float windSpeed = 0f;         // empurr�o extra
    public Vector3 windDir = Vector3.zero;

    [Header("Dire��o do rio (vem do spawner)")]
    public Vector3 riverForward = Vector3.forward;

    [Header("Vida �til")]
    public float maxTravelDistance = 200f;

    [Header("�gua (arraste o objeto de �gua / plano de �gua aqui)")]
    public GameObject unityWaterObject;  // pode ser um plane, um water surface, etc.
    public WaterSurface water;   // arraste aqui o WaterSurface real

    [Header("Ondula��o")]
    [Tooltip("Altura m�dia da �gua quando n�o for poss�vel ler do objeto de �gua.")]
    public float baseWaterHeight = 0f;

    [Tooltip("Quanto o objeto sobe/desce.")]
    public float waveAmplitude = 0.35f;

    [Tooltip("Frequ�ncia da onda (velocidade do sobe/desce).")]
    public float waveFrequency = 1.5f;

    [Tooltip("Quanto a posi��o X/Z influencia a fase da onda.")]
    public float waveSpatialScale = 0.2f;

    [Tooltip("Dire��o da propaga��o da onda no plano XZ.")]
    public Vector2 waveDirectionXZ = new Vector2(1f, 0.5f);

    private Vector3 startPos;
    private Rigidbody rb;

    // cache do Y do objeto de �gua, se existir
    private bool hasWaterObject = false;
    private float waterBaseY = 0f;
    private Transform waterTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        startPos = transform.position;

        // normalizar dire��es
        riverForward = riverForward.sqrMagnitude < 0.0001f ? Vector3.forward : riverForward.normalized;
        if (windDir.sqrMagnitude > 0.0001f)
            windDir = windDir.normalized;

        // pegar refer�ncia do objeto de �gua (se foi arrastado)
        if (unityWaterObject != null)
        {
            hasWaterObject = true;
            waterTransform = unityWaterObject.transform;
            waterBaseY = waterTransform.position.y; // << base da �gua
        }
        else
        {
            hasWaterObject = false;
            waterBaseY = baseWaterHeight;           // << usa o valor configurado
        }
    }

    void FixedUpdate()
    {
        // posi��o atual
        Vector3 pos = transform.position;

        // 1) mover com o rio
        pos += riverForward * currentSpeed * Time.fixedDeltaTime;

        // 2) vento
        if (windSpeed > 0.01f)
            pos += windDir * windSpeed * Time.fixedDeltaTime;

        // 3) calcular altura da �gua + onda
        float targetY = ComputeWaterY(pos, Time.time);
        pos.y = targetY;

        // 4) aplicar
        if (rb != null && !rb.isKinematic)
        {
            rb.MovePosition(pos);
        }
        else
        {
            transform.position = pos;
        }

        // 5) destruir quando cruzar X = 0
        if (pos.x <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // 6) destruir se foi longe demais
        float traveled = Vector3.Distance(startPos, pos);
        if (traveled > maxTravelDistance)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Calcula a altura da �gua para a posi��o atual.
    /// Se tiver waterObject: usa o Y dele como base.
    /// Sempre soma uma ondinha 2D para dar vida.
    /// </summary>
    float ComputeWaterY(Vector3 worldPos, float time)
    {
        // base vinda do objeto de �gua ou do fallback
        float baseY = hasWaterObject ? waterBaseY : baseWaterHeight;

        // dire��o da onda
        Vector2 dir = waveDirectionXZ;
        if (dir.sqrMagnitude < 0.001f)
            dir = Vector2.right;
        dir.Normalize();

        // posi��o no plano
        Vector2 posXZ = new Vector2(worldPos.x, worldPos.z);

        // fase espacial: onda se desloca no espa�o
        float along = Vector2.Dot(posXZ, dir) * waveSpatialScale;

        // fase temporal
        float timePhase = time * waveFrequency;

        // onda final
        float wave = Mathf.Sin(timePhase - along) * waveAmplitude;

        return baseY + wave;
    }
}
