using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Tenta acessar a API do Water System da Unity 6.2 via reflexão.
/// Procura tipos conhecidos (HDRP/URP) e método de amostragem de altura.
/// Se não encontrar, sempre retorna false (o controlador cairá no fallback).
/// </summary>
public class UWSReflectionHeightProvider : MonoBehaviour, IWaterHeightProvider
{
    [Tooltip("Arraste aqui o objeto com WaterSurface (do Water System)")]
    public Component waterSurfaceComponent;

    // Reflection caches
    private Type _waterSurfaceType;
    private MethodInfo _getHeightMethod;   // Assumimos assinatura semelhante a: bool GetWaterHeightAtPosition(Vector3 pos, out float height)
    private MethodInfo _getHeightVelMethod;// Variante com velocidade, se existir

    // Para out params via reflexão
    private object[] _argsHeight = new object[2];
    private object[] _argsHeightVel = new object[3];

    void Awake()
    {
        if (waterSurfaceComponent == null) return;

        _waterSurfaceType = waterSurfaceComponent.GetType();

        // Tentativas de nomes de método comuns (podem variar com pipeline)
        string[] candidatesHeight = {
            "GetWaterSurfaceHeightAtPosition",
            "GetWaterHeightAtPosition",
            "SampleHeight"
        };
        string[] candidatesHeightVel = {
            "GetWaterSurfaceHeightAndVelocityAtPosition",
            "GetWaterHeightAndVelocityAtPosition",
            "SampleHeightAndVelocity"
        };

        foreach (var m in candidatesHeight)
        {
            _getHeightMethod = _waterSurfaceType.GetMethod(m, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_getHeightMethod != null) break;
        }

        foreach (var m in candidatesHeightVel)
        {
            _getHeightVelMethod = _waterSurfaceType.GetMethod(m, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_getHeightVelMethod != null) break;
        }
    }

    public bool TryGetWaterHeightAndVelocity(Vector3 worldPos, out float waterY, out Vector3 surfaceVelocity)
    {
        waterY = 0f;
        surfaceVelocity = Vector3.zero;

        if (waterSurfaceComponent == null || _waterSurfaceType == null) return false;

        // Preferimos altura+velocidade, se existir:
        if (_getHeightVelMethod != null)
        {
            _argsHeightVel[0] = worldPos;
            _argsHeightVel[1] = 0f;            // placeholder para out height
            _argsHeightVel[2] = Vector3.zero;  // placeholder para out velocity

            try
            {
                var ok = (bool)_getHeightVelMethod.Invoke(waterSurfaceComponent, _argsHeightVel);
                if (ok)
                {
                    waterY = (float)_argsHeightVel[1];
                    surfaceVelocity = (Vector3)_argsHeightVel[2];
                    return true;
                }
            }
            catch { /* cai para o próximo */ }
        }

        // Só altura:
        if (_getHeightMethod != null)
        {
            _argsHeight[0] = worldPos;
            _argsHeight[1] = 0f;

            try
            {
                var ok = (bool)_getHeightMethod.Invoke(waterSurfaceComponent, _argsHeight);
                if (ok)
                {
                    waterY = (float)_argsHeight[1];
                    surfaceVelocity = Vector3.zero;
                    return true;
                }
            }
            catch { /* falhou */ }
        }

        return false;
    }
}
