using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class Floating : MonoBehaviour
{
    public WaterSurface targetSurface = null;
    public float currentSpeedMultiplier = 0;
    public float selfRotationSpeed = 0;
    //public Vector3 initialPosition;
    public float initialScale = 0.25f;
    public bool includeDeformers = true;
    public bool clickToResetPosition = false;

    // === SUBMERSÃO ===
    public enum DraftMode { Meters, PercentOfRendererHeight }
    [Header("Submersão (calado)")]
    public DraftMode draftMode = DraftMode.PercentOfRendererHeight;
    [Tooltip("Se Mode=Percent, use 0..1. Se Mode=Meters, valor em metros.")]
    public float draftValue = 0.3f; // 0.3 => 30% da altura, ou 0.3 m se Meters
    [Tooltip("Se o WaterSearchResult não fornecer normal, usa Vector3.up")]
    public bool useWaterNormalIfAvailable = true;

    // Cache
    Renderer _renderer;

    // Internal search params
    WaterSearchParameters searchParameters = new WaterSearchParameters();
    WaterSearchResult searchResult = new WaterSearchResult();

    void Start()
    {
        _renderer = GetComponentInChildren<Renderer>();
        Reset();
    }

    void Update()
    {
        float boundX = 50;
        float maxBoundX = 68;
        float opacity = 1 - Mathf.Clamp01((Mathf.Abs(this.transform.position.x) - boundX) / (maxBoundX - boundX));

        // Ajustes visuais do seu exemplo
        if (this.transform.position.x > 0)
            this.GetComponent<Renderer>()?.sharedMaterial?.SetFloat("_Opacity", opacity);
        else
            this.transform.localScale = Vector3.one * initialScale * opacity;

        if (this.transform.position.x < -maxBoundX || clickToResetPosition)
        {
            Reset();
        }

        if (selfRotationSpeed != 0)
        {
            Vector3 r = this.transform.localEulerAngles;
            r.y += selfRotationSpeed;
            this.transform.localEulerAngles = r;
        }

        if (targetSurface != null)
        {
            // Build the search parameters
            searchParameters.startPositionWS = searchResult.candidateLocationWS;
            searchParameters.targetPositionWS = this.transform.position;
            searchParameters.error = 0.01f;
            searchParameters.maxIterations = 8;
            searchParameters.includeDeformation = includeDeformers;
            searchParameters.excludeSimulation = false;

            // Do the search
            if (targetSurface.ProjectPointOnWaterSurface(searchParameters, out searchResult))
            {
                // 1) posição básica na superfície + corrente
                Vector3 basePos = searchResult.projectedPositionWS + searchResult.currentDirectionWS * currentSpeedMultiplier;

                // 2) calcular calado (offset) e normal
                Vector3 waterNormal = Vector3.up;
                // Algumas versões do HDRP expõem normal/gradient; se não existir, fica em up
                // Ex.: waterNormal = searchResult.surfaceNormalWS; // se estiver disponível
                if (useWaterNormalIfAvailable)
                {
                    // Tente inferir normal do próprio WaterSurface, se você tiver um método utilitário.
                    // Caso não, mantemos Vector3.up.
                }

                float heightMeters = 1f;
                if (_renderer != null)
                    heightMeters = _renderer.bounds.size.y;

                float draftMeters = (draftMode == DraftMode.Meters)
                    ? Mathf.Max(0f, draftValue)
                    : Mathf.Max(0f, draftValue) * heightMeters; // Percentual da altura

                // 3) aplicar calado, deslocando ao longo da normal da água
                Vector3 finalPos = basePos - waterNormal.normalized * draftMeters;

                this.transform.position = finalPos;
            }
            else
            {
                //Can't Find Height, Script Interaction is probably disabled. 
            }
        }
    }

    void Reset()
    {
        //this.transform.position = initialPosition;
        this.transform.localScale = Vector3.one * initialScale;
        clickToResetPosition = false;

        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
    }
}
