// Assets/Scripts/River/RiverSimulationManager.cs
using System;
using UnityEngine;

public class RiverSimulationManager : MonoBehaviour
{
    public static RiverSimulationManager Instance { get; private set; }

    [Header("Configura��o da simula��o")]
    public RiverSimulationConfig config;

    private System.Random rnd;

    // Valores globais j� sorteados (pra todos usarem iguais)
    [HideInInspector] public float globalCurrentSpeed;
    [HideInInspector] public float globalWaveAmp;
    [HideInInspector] public float globalWaveFreq;
    [HideInInspector] public Vector3 globalWindDir;
    [HideInInspector] public float globalWindSpeed;
    [HideInInspector] public float globalSunIntensity;
    [HideInInspector] public float globalSunElev;
    [HideInInspector] public float globalSunAzim;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("H� mais de um RiverSimulationManager na cena. Deixe s� um.");
            Destroy(this);
            return;
        }

        Instance = this;

        if (config == null)
        {
            Debug.LogError("RiverSimulationManager sem config.");
            return;
        }

        // cria o random determin�stico
        int seedToUse;
        if (config.useFixedSeed)
            seedToUse = config.seed;
        else
            seedToUse = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        rnd = new System.Random(seedToUse);

        // Sorteia e fixa valores globais
        globalCurrentSpeed = RandRange(config.currentSpeedRange);
        globalWaveAmp = RandRange(config.surfaceWaveAmplitudeRange);
        globalWaveFreq = RandRange(config.surfaceWaveFrequencyRange);

        float windSpd = RandRange(config.windSpeedRange);
        float windAng = RandRange(config.windAngleRange);
        globalWindDir = Quaternion.Euler(0f, windAng, 0f) * Vector3.forward;
        globalWindSpeed = windSpd;

        // sol
        globalSunIntensity = RandRange(config.sunIntensityRange);
        globalSunElev = RandRange(config.sunElevationDegRange);
        globalSunAzim = RandRange(config.sunAzimuthDegRange);
    }

    /// <summary>
    /// Retorna um float determin�stico dentro do range usando o PRNG central.
    /// </summary>
    public float RandRange(Vector2 range)
    {
        if (rnd == null)
            rnd = new System.Random(12345);
        double t = rnd.NextDouble(); // 0..1
        return (float)(range.x + t * (range.y - range.x));
    }

    /// <summary>
    /// Retorna um int determin�stico no intervalo [min, max).
    /// </summary>
    public int RandInt(int minInclusive, int maxExclusive)
    {
        if (rnd == null)
            rnd = new System.Random(12345);
        return rnd.Next(minInclusive, maxExclusive);
    }

    /// <summary>
    /// Acesso direto ao PRNG se precisar.
    /// </summary>
    public System.Random GetRandom()
    {
        return rnd;
    }
}
