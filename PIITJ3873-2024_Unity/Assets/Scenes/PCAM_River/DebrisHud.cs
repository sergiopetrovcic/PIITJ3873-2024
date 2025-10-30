using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// HUD simples via IMGUI mostrando contadores por tipo.
/// Não precisa de TextMeshPro nem UI Canvas.
/// </summary>
public class DebrisHud : MonoBehaviour
{
    [Header("Estilo HUD")]
    public int fontSize = 14;
    public float panelWidth = 380f;
    public float rowHeight = 22f;
    public float margin = 10f;
    public bool alignTopLeft = true; // se false, topo-direita

    [Header("Atualização")]
    [Tooltip("Atualiza a leitura a cada X segundos para evitar custo por frame.")]
    public float refreshInterval = 0.25f;

    private Dictionary<string, DebrisRegistry.Counts> _snapshot = new Dictionary<string, DebrisRegistry.Counts>();
    private float _nextRefreshTime;

    void Update()
    {
        if (Time.unscaledTime >= _nextRefreshTime)
        {
            if (DebrisRegistry.Instance != null)
                _snapshot = DebrisRegistry.Instance.GetSnapshot();
            else
                _snapshot.Clear();

            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);
        }
    }

    void OnGUI()
    {
        if (_snapshot == null || _snapshot.Count == 0)
            return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            alignment = TextAnchor.MiddleLeft
        };

        // Ordena por nome de tipo
        var entries = _snapshot.OrderBy(kv => kv.Key).ToList();

        float x = alignTopLeft ? margin : (Screen.width - panelWidth - margin);
        float y = margin;

        // Cabeçalho
        Rect r = new Rect(x, y, panelWidth, rowHeight);
        DrawRow(r, "Tipo", "Criados", "Ativos", "Destr.", style, header: true);
        y += rowHeight;

        // Linhas
        foreach (var kv in entries)
        {
            r = new Rect(x, y, panelWidth, rowHeight);
            var c = kv.Value;
            DrawRow(r, kv.Key, c.spawned.ToString(), c.active.ToString(), c.destroyed.ToString(), style, header: false);
            y += rowHeight;
        }

        // Total
        int tSpawn = entries.Sum(e => e.Value.spawned);
        int tAct = entries.Sum(e => e.Value.active);
        int tDest = entries.Sum(e => e.Value.destroyed);

        y += 4f;
        r = new Rect(x, y, panelWidth, rowHeight);
        DrawRow(r, "TOTAL", tSpawn.ToString(), tAct.ToString(), tDest.ToString(), style, header: true);
    }

    void DrawRow(Rect area, string col1, string col2, string col3, string col4, GUIStyle style, bool header)
    {
        // Fundo
        var bg = header ? new Color(0, 0, 0, 0.55f) : new Color(0, 0, 0, 0.35f);
        EditorGUI_DrawRect(area, bg);

        float w1 = area.width * 0.46f; // Tipo
        float w2 = area.width * 0.18f; // Spawned
        float w3 = area.width * 0.18f; // Active
        float w4 = area.width * 0.18f; // Destroyed

        var a1 = new Rect(area.x + 6f, area.y, w1 - 12f, area.height);
        var a2 = new Rect(area.x + w1 + 4f, area.y, w2 - 8f, area.height);
        var a3 = new Rect(area.x + w1 + w2 + 8f, area.y, w3 - 8f, area.height);
        var a4 = new Rect(area.x + w1 + w2 + w3 + 12f, area.y, w4 - 16f, area.height);

        var st = new GUIStyle(style);
        if (header) st.fontStyle = FontStyle.Bold;

        GUI.Label(a1, col1, st);
        GUI.Label(a2, col2, st);
        GUI.Label(a3, col3, st);
        GUI.Label(a4, col4, st);
    }

    // Desenha um retângulo (compatível com runtime) sem precisar de UnityEditor
    void EditorGUI_DrawRect(Rect position, Color color)
    {
        Color prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(position, Texture2D.whiteTexture);
        GUI.color = prev;
    }
}
