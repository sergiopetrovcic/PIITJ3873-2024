using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// RiverCarver
/// - Como usar: criar uma imagem com preton onde é rio e branco onde é terra. Colocar a imagem dentro do Unity. 
///   Configurar a imagem como sRGB desativado (Linear) e Read/Write Enabled ativado. Certificar de ativar Run On Start.
///   Talvez seja necessário colocar o terreno acima do plano y=0.
/// - Usa uma máscara (grayscale) para esculpir o terreno:
///   Preto (0)  = canal (escava máximo)
///   Branco (1) = margem (eleva máximo)
/// - Mantém o leito do rio ~em y=0 (não eleva o terreno todo).
/// - Parâmetros:
///   depthMeters:      a profundidade de escavação do canal (em metros)
///   bankRaiseMeters:  quanto elevar apenas as margens (em metros)
///   bankPower:        curva para controlar a largura/forma das margens
///   bankSoftness:     suavização nas transições
///   intensity:        ganho geral da máscara
/// </summary>
[ExecuteAlways]
public class RiverCarver : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Terrain a ser modificado. Se vazio, será buscado no mesmo GameObject.")]
    public Terrain terrain;

    [Tooltip("Máscara em escala de cinza: Preto=canal (escava), Branco=margem (eleva).")]
    public Texture2D riverMask; // Import: Read/Write Enabled (ON). Ideal sRGB OFF.

    [Header("Canal (escavação)")]
    [Tooltip("Profundidade máxima do canal (em metros). Preto=escava máximo; Branco=0.")]
    public float depthMeters = 6f;

    [Header("Margens (elevação)")]
    [Tooltip("Altura máxima para elevar apenas as margens (em metros). Branco=eleva máximo; Preto=0.")]
    public float bankRaiseMeters = 4f;

    [Tooltip("Curva que controla a 'largura' das margens (potência sobre o valor da máscara). " +
             "Valores maiores tendem a margens mais estreitas e altas.")]
    [Range(0.5f, 3f)]
    public float bankPower = 1.2f;

    [Tooltip("Suaviza bordas/platôs nas transições canal↔margem.")]
    [Range(0f, 1f)]
    public float bankSoftness = 0.25f;

    [Header("Máscara")]
    [Tooltip("Ganho geral sobre a máscara (0 = desliga; 1 = original).")]
    [Range(0f, 2f)]
    public float intensity = 1f;

    [Tooltip("Se a textura estiver com sRGB ligado, marque para ler em linear.")]
    public bool linearizeMask = false;

    [Header("Aplicação")]
    [Tooltip("Aplicar automaticamente ao dar Play. Em edição, prefira o botão 'Carve Now'.")]
    public bool runOnStart = true;

    [Tooltip("Usa SetHeightsDelayLOD (pode ser mais rápido para áreas grandes).")]
    public bool useSetHeightsDelayLOD = false;

    // -----------------------------
    // Utilidades
    // -----------------------------

    private bool Validate(out TerrainData td, out string error)
    {
        error = null;
        td = null;

        if (!terrain)
        {
            error = "Defina o 'terrain'.";
            return false;
        }

        td = terrain.terrainData;
        if (!td)
        {
            error = "TerrainData inválido.";
            return false;
        }

        if (!riverMask)
        {
            error = "Defina a textura 'riverMask'.";
            return false;
        }

        if (!riverMask.isReadable)
        {
            error = "A textura 'riverMask' não está Read/Write Enabled.";
            return false;
        }

        if (td.heightmapResolution <= 0 || td.size.y <= 0f)
        {
            error = "heightmapResolution ou size.y inválidos no Terrain.";
            return false;
        }

        return true;
    }

    private static float ReadMaskGray(Texture2D tex, float u, float v, bool linearize)
    {
        // GetPixelBilinear: independe do tamanho da textura
        Color c = tex.GetPixelBilinear(u, v);
        float g = c.grayscale; // média em gama

        if (linearize)
        {
            // Converte para linear; opção simples usando o cinza
            g = Mathf.GammaToLinearSpace(g);
        }

        return Mathf.Clamp01(g);
    }

    // -----------------------------
    // Ação principal
    // -----------------------------

    [ContextMenu("Carve Now")]
    public void CarveNow()
    {
        if (!terrain)
        {
            // tenta auto-atribuir no mesmo GameObject
            terrain = GetComponent<Terrain>();
        }

        if (!Validate(out TerrainData td, out string error))
        {
            Debug.LogWarning("RiverCarver: " + error);
            return;
        }

        int hmW = td.heightmapResolution;
        int hmH = td.heightmapResolution;

        float[,] heights;
        try
        {
            heights = td.GetHeights(0, 0, hmW, hmH);
        }
        catch (System.Exception e)
        {
            Debug.LogError("RiverCarver: erro ao ler heights: " + e.Message);
            return;
        }

        // Converte metros para [0..1]
        float depth01 = depthMeters / td.size.y;
        float bankRaise01 = bankRaiseMeters / td.size.y;

        // Se intensidade for 0, não faz nada visível.
        if (intensity <= 0f || (depth01 <= 0f && bankRaise01 <= 0f))
        {
            Debug.LogWarning("RiverCarver: parâmetros resultam em nenhuma alteração (verifique depth/bankRaise/intensity).");
        }

        // Loop principal: canal (escava) e margens (elevam)
        for (int y = 0; y < hmH; y++)
        {
            float v = (float)y / (hmH - 1); // v cresce para baixo
            for (int x = 0; x < hmW; x++)
            {
                float u = (float)x / (hmW - 1);

                // g: 0=preto (canal), 1=branco (margem)
                float g = ReadMaskGray(riverMask, u, v, linearizeMask);

                // ----- Canal: escavar (preto escava máximo) -----
                float tChannel = Mathf.Clamp01(1f - g); // 0..1
                if (bankSoftness > 0f)
                {
                    tChannel = Mathf.SmoothStep(0f, 1f, tChannel);
                    tChannel = Mathf.Lerp(tChannel, tChannel * tChannel, bankSoftness);
                }
                tChannel *= intensity;

                float deltaDown = tChannel * depth01;

                // Aplica "descer" (clamp)
                float h = heights[y, x] - deltaDown;
                if (h < 0f) h = 0f; // leito nunca abaixo de 0

                // ----- Margens: elevar (branco eleva máximo) -----
                float tBank = Mathf.Pow(Mathf.Clamp01(g), bankPower); // 0 no canal, 1 na margem
                if (bankSoftness > 0f)
                    tBank = Mathf.SmoothStep(0f, 1f, tBank);

                float targetBank = tBank * bankRaise01; // altura-alvo da margem
                if (h < targetBank)
                    h = targetBank; // eleva só a margem

                heights[y, x] = Mathf.Clamp01(h);
            }
        }

        // Aplica no Terrain
        try
        {
            if (useSetHeightsDelayLOD)
                td.SetHeightsDelayLOD(0, 0, heights);
            else
                td.SetHeights(0, 0, heights);
        }
        catch (System.Exception e)
        {
            Debug.LogError("RiverCarver: erro ao gravar heights: " + e.Message);
            return;
        }

        // Em algumas versões ajuda sincronizar:
        // td.SyncHeightmap();

        Debug.Log("RiverCarver: canal (~0) e margens elevadas aplicados com sucesso.");
    }

    // Evita aplicar automaticamente em modo de edição (Editor)
    private void Start()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        if (runOnStart) CarveNow();
    }

    // Conveniência: tenta auto-atribuir Terrain ao adicionar o componente
    private void Reset()
    {
        if (!terrain) terrain = GetComponent<Terrain>();
    }
}
