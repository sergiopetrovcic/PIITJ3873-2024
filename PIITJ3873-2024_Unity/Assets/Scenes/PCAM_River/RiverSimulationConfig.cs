// Assets/Scripts/River/RiverSimulationConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "River/Simulation Config")]
public class RiverSimulationConfig : ScriptableObject
{
    [Header("Seed / Determinismo")]
    [Tooltip("Se marcado, sempre usa esta seed e a simulação será reprodutível.")]
    public bool useFixedSeed = true;
    public int seed = 12345;

    [Header("Rejeitos")]
    [Tooltip("Prefabs possíveis de spawnar como rejeito flutuante.")]
    public GameObject[] debrisPrefabs;

    [Tooltip("Posição lateral (X) onde o rejeito pode aparecer, relativo ao spawner.")]
    public Vector2 spawnXRange = new Vector2(-5f, 5f);

    [Tooltip("Posição ao longo do rio (Z) onde o rejeito pode aparecer, relativo ao spawner.")]
    public Vector2 spawnZRange = new Vector2(0f, 0f);

    [Tooltip("Escala mínima e máxima do rejeito.")]
    public Vector2 sizeRange = new Vector2(0.5f, 1.5f);

    [Tooltip("Tempo entre spawns (segundos). O valor real será sorteado entre min e max.")]
    public Vector2 spawnIntervalRange = new Vector2(0.5f, 2.5f);

    [Header("Dinâmica do rio")]
    [Tooltip("Velocidade da correnteza (m/s)")]
    public Vector2 currentSpeedRange = new Vector2(1f, 3f);

    [Tooltip("Amplitude da ondulação da água usada pelos flutuantes.")]
    public Vector2 surfaceWaveAmplitudeRange = new Vector2(0.05f, 0.3f);

    [Tooltip("Frequência da ondulação da água usada pelos flutuantes.")]
    public Vector2 surfaceWaveFrequencyRange = new Vector2(0.5f, 2f);

    [Header("Vento")]
    [Tooltip("Velocidade do vento que empurra os rejeitos.")]
    public Vector2 windSpeedRange = new Vector2(0f, 2f);

    [Tooltip("Direção do vento em graus (0..360).")]
    public Vector2 windAngleRange = new Vector2(0f, 360f);

    [Header("Água")]
    public Color waterColor = new Color(0.1f, 0.25f, 0.3f, 1f);
    [Range(0f, 1f)] public float waterAlpha = 0.8f;

    [Header("Sol / Luz")]
    public Vector2 sunIntensityRange = new Vector2(0.7f, 1.4f);
    public Vector2 sunElevationDegRange = new Vector2(25f, 60f);
    public Vector2 sunAzimuthDegRange = new Vector2(0f, 360f);
}
