using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteAlways]
public class Floating : MonoBehaviour
{
    [Header("Water Surface (HDRP)")]
    public WaterSurface targetSurface = null;
    [Tooltip("m/s aplicados ao longo da corrente da �gua")]
    public float currentSpeedMetersPerSecond = 0f;
    [Tooltip("Rota��o pr�pria em graus/s no eixo Y")]
    public float selfRotationSpeedDegPerSec = 0f;
    [Tooltip("Se true, considerar deformers na busca")]
    public bool includeDeformers = true;
    [Tooltip("Clique para voltar o objeto ao ponto inicial")]
    public bool clickToResetPosition = false;

    [Header("Submers�o (calado)")]
    [Tooltip("Se Percent, 0..1 da altura do Renderer; se Meters, valor em metros")]
    public float draftValue = 0.3f;
    [Tooltip("Usar normal da �gua se dispon�vel; sen�o Vector3.up")]
    public bool useWaterNormalIfAvailable = true;
    public enum DraftMode { Meters, PercentOfRendererHeight }
    public DraftMode draftMode = DraftMode.PercentOfRendererHeight;

    [Header("Busca/Precis�o")]
    [Tooltip("Erro alvo da busca de altura (menor = mais preciso)")]
    public float searchError = 0.001f;
    [Tooltip("Itera��es m�ximas da busca (maior = mais preciso)")]
    public int searchMaxIterations = 16;

    // Estado
    private Renderer _renderer;
    private Vector3 _horizontalPos;         // posi��o horizontal �desejada� integrada por velocidade
    private bool _hasInit;
    private Vector3 _lastProjectedPos;

    // Estruturas HDRP
    private WaterSearchParameters _searchParameters = new WaterSearchParameters();
    private WaterSearchResult _searchResult = new WaterSearchResult();

    // ===== Ciclo =====

    void OnEnable()
    {
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
        if (!_hasInit)
        {
            _horizontalPos = transform.position;
            _lastProjectedPos = transform.position;
            _hasInit = true;
        }
    }

    void Start()
    {
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
        if (!_hasInit)
        {
            _horizontalPos = transform.position;
            _lastProjectedPos = transform.position;
            _hasInit = true;
        }
    }

    void LateUpdate() // roda depois da simula��o/anim da �gua para minimizar 1-frame de desfasagem
    {
        if (targetSurface == null)
            return;

        // reset manual
        if (clickToResetPosition)
        {
            _horizontalPos = transform.position;
            clickToResetPosition = false;
        }

        // rota��o pr�pria (visual)
        if (selfRotationSpeedDegPerSec != 0f)
        {
            var r = transform.localEulerAngles;
            r.y += selfRotationSpeedDegPerSec * Time.deltaTime;
            transform.localEulerAngles = r;
        }

        // ========= MOVIMENTO HORIZONTAL =========
        // 1) Integra deslocamento pela corrente: v * dt
        //    (Se n�o quiser �surfar�, deixe currentSpeedMetersPerSecond = 0.)
        Vector3 currentDirWS = Vector3.zero;

        // A ProjectPointOnWaterSurface tamb�m devolve dire��o da corrente no _searchResult.currentDirectionWS
        // do frame ANTERIOR. Para n�o depender do resultado anterior, integramos primeiro
        // usando o �ltimo valor conhecido; na primeira itera��o ser� zero e n�o desloca.
        currentDirWS = _searchResult.currentDirectionWS;
        if (currentDirWS.sqrMagnitude > 0.0001f)
            currentDirWS.Normalize();

        _horizontalPos += currentDirWS * (currentSpeedMetersPerSecond * Time.deltaTime);

        // Mant�m Y da posi��o alvo igual ao Y atual para a busca (n�o importa, a �gua corrige depois)
        Vector3 targetHorizontal = new Vector3(_horizontalPos.x, transform.position.y, _horizontalPos.z);

        // ========= PROJE��O NA SUPERF�CIE =========
        _searchParameters.startPositionWS = _lastProjectedPos; // melhora converg�ncia e reduz flicker
        _searchParameters.targetPositionWS = targetHorizontal;
        _searchParameters.error = Mathf.Max(1e-5f, searchError);
        _searchParameters.maxIterations = Mathf.Clamp(searchMaxIterations, 1, 64);
        _searchParameters.includeDeformation = includeDeformers;
        _searchParameters.excludeSimulation = false;

        if (targetSurface.ProjectPointOnWaterSurface(_searchParameters, out _searchResult))
        {
            // Posi��o exata na superf�cie no ponto alvo (instant�nea)
            Vector3 projected = _searchResult.projectedPositionWS;
            _lastProjectedPos = projected;

            // Normal da �gua (se dispon�vel)
            Vector3 waterNormal = Vector3.up;
            if (useWaterNormalIfAvailable)
            {
                // Algumas vers�es exp�em surfaceNormalWS; se n�o, mantenha up
                // if (_searchResult.surfaceNormalWS != Vector3.zero) waterNormal = _searchResult.surfaceNormalWS;
            }

            // Calcular calado em metros
            float heightMeters = (_renderer != null) ? _renderer.bounds.size.y : 1f;
            float draftMeters = (draftMode == DraftMode.Meters)
                ? Mathf.Max(0f, draftValue)
                : Mathf.Max(0f, draftValue) * heightMeters;

            // Posi��o final = ponto da �gua - calado ao longo da normal
            Vector3 finalPos = projected - waterNormal.normalized * draftMeters;

            // Aplicar
            transform.position = finalPos;
        }
        else
        {
            // Prov�vel Script Interactions desativado (Water Surface)
        }

        // ===== Seus efeitos visuais (opcionais) =====
        ApplyVisualsByXBounds();
    }

    // ===== Auxiliares =====

    void ApplyVisualsByXBounds()
    {
        // Ajustes visuais do seu exemplo (n�o afetam o seguimento da onda)
        float boundX = 50;
        float maxBoundX = 68;
        float opacity = 1 - Mathf.Clamp01((Mathf.Abs(transform.position.x) - boundX) / (maxBoundX - boundX));

        var rend = GetComponent<Renderer>();
        if (transform.position.x > 0)
            rend?.sharedMaterial?.SetFloat("_Opacity", opacity);
        else
            transform.localScale = Vector3.one * 0.25f * opacity;

        if (transform.position.x < -maxBoundX)
            _horizontalPos = transform.position; // reset simples
    }
}
