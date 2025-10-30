using UnityEngine;
using System.Collections.Generic;

public enum WaterProviderMode { Auto, UWSOnly, GerstnerOnly }

[RequireComponent(typeof(Rigidbody))]
public class BuoyantShip : MonoBehaviour
{
    [Header("Provider Mode")]
    public WaterProviderMode providerMode = WaterProviderMode.Auto;

    [Header("CoM")]
    public Vector3 centerOfMassOffset = new Vector3(0f, +2.0f, 0f); // suba depois se quiser mais rolagem

    [Header("Água (providers)")]
    [Tooltip("Arraste aqui QUALQUER MonoBehaviour que implemente IWaterHeightProvider (ex.: componente WaterSurface ou GerstnerWaves).")]
    public GameObject uwsProviderObject;
    public GameObject gerstnerProviderObject;
    private IWaterHeightProvider _uws, _gerstner;

    [Header("Casco / Amostragem")]
    public Bounds localHullBounds = new Bounds(new Vector3(0, -0.5f, 0), new Vector3(6, 1, 20));
    public int samplesX = 4;
    public int samplesZ = 8;
    public float maxSubmergence = 1.2f;

    [Header("Empuxo / Fluido")]
    public float waterDensity = 1025f;
    public float displacementVolume = 30f;
    public float buoyancyCoefficient = 1.0f;

    [Header("Arrasto / Damping")]
    public float verticalDamping = 500f;
    public float horizontalDrag = 350f;
    public float angularDrag = 700f; // use o campo do Rigidbody para efeito real
    public bool useSurfaceCurrent = true;

    [Header("Auto-calibração")]
    public bool computeDisplacementFromMass = true;
    public float reserveBuoyancy = 1.05f;
    public bool snapToWaterlineOnStart = true;
    public float waterlineYOffset = -0.05f;

    [Header("Anti-catapulta / Estabilidade")]
    public float leverArmClamp = 1.5f;      // m
    public float maxForcePerSample = 20000f; // N
    public float maxTorquePerSample = 50000f; // N·m
    public float maxAngularVelocity = 1.8f;   // rad/s

    private Rigidbody _rb;
    private readonly List<Vector3> _localSamplePoints = new List<Vector3>();
    private float _perSampleVolume;
    private float _fluidWeightPerSample;
    private const float g = 9.81f;
    private bool _warnedNoWater = false;

    void OnValidate()
    {
        samplesX = Mathf.Max(1, samplesX);
        samplesZ = Mathf.Max(1, samplesZ);
        maxSubmergence = Mathf.Max(0.05f, maxSubmergence);
        displacementVolume = Mathf.Max(0.001f, displacementVolume);
        waterDensity = Mathf.Max(1f, waterDensity);
        reserveBuoyancy = Mathf.Max(1.0f, reserveBuoyancy);
        leverArmClamp = Mathf.Max(0.2f, leverArmClamp);
        maxForcePerSample = Mathf.Max(100f, maxForcePerSample);
        maxTorquePerSample = Mathf.Max(1000f, maxTorquePerSample);
        maxAngularVelocity = Mathf.Clamp(maxAngularVelocity, 0.5f, 20f);
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.centerOfMass += centerOfMassOffset;

        // Casts dos providers (podem ser null)
        _uws = uwsProviderObject ? uwsProviderObject.GetComponent<IWaterHeightProvider>() : null;
        _gerstner = gerstnerProviderObject ? gerstnerProviderObject.GetComponent<IWaterHeightProvider>() : null;

        if (providerMode == WaterProviderMode.GerstnerOnly && _gerstner == null)
            Debug.LogError("GerstnerOnly ativo mas 'gerstnerProviderBehaviour' não aponta para um IWaterHeightProvider (ex.: GerstnerWaves).");

        if (computeDisplacementFromMass)
            displacementVolume = (_rb.mass / waterDensity) * reserveBuoyancy;

        // Solver estável
        _rb.solverIterations = Mathf.Max(_rb.solverIterations, 20);
        _rb.solverVelocityIterations = Mathf.Max(_rb.solverVelocityIterations, 10);
        _rb.maxAngularVelocity = maxAngularVelocity;

        // Corrige bug: o campo certo é angularDrag (não existe angularDamping)
        if (_rb.angularDamping < 0.05f) _rb.angularDamping = 0.05f;


        BuildSamplePoints();
        RecomputePerSampleConstants();

        if (snapToWaterlineOnStart) SnapToWaterline();
    }

    void BuildSamplePoints()
    {
        _localSamplePoints.Clear();

        for (int iz = 0; iz < samplesZ; iz++)
        {
            float fz = (samplesZ == 1) ? 0.5f : (float)iz / (samplesZ - 1);
            float z = Mathf.Lerp(localHullBounds.min.z, localHullBounds.max.z, fz);

            for (int ix = 0; ix < samplesX; ix++)
            {
                float fx = (samplesX == 1) ? 0.5f : (float)ix / (samplesX - 1);
                float x = Mathf.Lerp(localHullBounds.min.x, localHullBounds.max.x, fx);
                float y = localHullBounds.center.y;
                _localSamplePoints.Add(new Vector3(x, y, z));
            }
        }
    }

    void RecomputePerSampleConstants()
    {
        int n = Mathf.Max(1, _localSamplePoints.Count);
        _perSampleVolume = displacementVolume / n;
        _fluidWeightPerSample = waterDensity * g * _perSampleVolume;
    }

    public void RebuildSamplesAndRecompute()
    {
        BuildSamplePoints();
        RecomputePerSampleConstants();
    }

    void FixedUpdate()
    {
        // Damping rotacional global (leve)
        _rb.AddTorque(-_rb.angularVelocity * angularDrag, ForceMode.Force);

        int gotWaterCount = 0;

        foreach (var local in _localSamplePoints)
        {
            Vector3 world = transform.TransformPoint(local);

            float waterY = 0f;
            Vector3 surfVel = Vector3.zero;
            bool gotWater = false;

            // Seleção de provider
            switch (providerMode)
            {
                case WaterProviderMode.UWSOnly:
                    if (_uws != null) gotWater = _uws.TryGetWaterHeightAndVelocity(world, out waterY, out surfVel);
                    break;

                case WaterProviderMode.GerstnerOnly:
                    if (_gerstner != null) gotWater = _gerstner.TryGetWaterHeightAndVelocity(world, out waterY, out surfVel);
                    break;

                default: // Auto
                    if (_uws != null) gotWater = _uws.TryGetWaterHeightAndVelocity(world, out waterY, out surfVel);
                    if (!gotWater && _gerstner != null) gotWater = _gerstner.TryGetWaterHeightAndVelocity(world, out waterY, out surfVel);
                    break;
            }

            if (!gotWater) continue;

            gotWaterCount++;

            float subm = waterY - world.y;
            if (subm <= 0f) continue;

            float k = Mathf.Clamp01(subm / maxSubmergence);
            float submFrac = k * k * (3f - 2f * k); // smoothstep

            // Empuxo + damping + drag
            Vector3 pointVel = _rb.GetPointVelocity(world);
            float relVy = pointVel.y - (useSurfaceCurrent ? surfVel.y : 0f);

            Vector3 buoyancy = Vector3.up * (_fluidWeightPerSample * buoyancyCoefficient * submFrac);
            Vector3 damping = Vector3.up * (-relVy * verticalDamping);

            Vector3 relVxz = new Vector3(pointVel.x, 0f, pointVel.z)
                           - (useSurfaceCurrent ? new Vector3(surfVel.x, 0f, surfVel.z) : Vector3.zero);
            Vector3 drag = -relVxz * horizontalDrag;

            Vector3 force = buoyancy + damping + drag;

            // ---------- Anti-catapulta ----------
            if (force.sqrMagnitude > maxForcePerSample * maxForcePerSample)
                force = force.normalized * maxForcePerSample;

            Vector3 com = _rb.worldCenterOfMass;
            Vector3 r = world - com;
            if (r.sqrMagnitude > leverArmClamp * leverArmClamp)
            {
                Vector3 rClamped = r.normalized * leverArmClamp;
                world = com + rClamped;
                r = rClamped;
            }

            Vector3 tentativeTorque = Vector3.Cross(r, force);
            float tauMag = tentativeTorque.magnitude;
            if (tauMag > maxTorquePerSample && tauMag > 1e-5f)
            {
                float s = maxTorquePerSample / tauMag;
                force *= s;
            }

            _rb.AddForceAtPosition(force, world, ForceMode.Force);
        }

        if (gotWaterCount == 0 && !_warnedNoWater)
        {
            _warnedNoWater = true;
            Debug.LogWarning("[BuoyantShip] Nenhum provider retornou altura da água. Confira os campos 'uwsProviderBehaviour' e 'gerstnerProviderBehaviour'.");
        }

        // Teto de rotação
        if (_rb.angularVelocity.sqrMagnitude > maxAngularVelocity * maxAngularVelocity)
            _rb.angularVelocity = _rb.angularVelocity.normalized * maxAngularVelocity;
    }

    void SnapToWaterline()
    {
        float sum = 0f;
        int count = 0;

        foreach (var local in _localSamplePoints)
        {
            Vector3 world = transform.TransformPoint(local);
            float waterY = 0f; Vector3 surfVel;

            bool got = false;
            switch (providerMode)
            {
                case WaterProviderMode.UWSOnly:
                    if (_uws != null) got = _uws.TryGetWaterHeightAndVelocity(world, out waterY, out surfVel);
                    break;
                case WaterProviderMode.GerstnerOnly:
                    if (_gerstner != null) got = _gerstner.TryGetWaterHeightAndVelocity(world, out waterY, out surfVel);
                    break;
                default:
                    if (_uws != null) got = _uws.TryGetWaterHeightAndVelocity(world, out waterY, out surfVel);
                    if (!got && _gerstner != null) got = _gerstner.TryGetWaterHeightAndVelocity(world, out waterY, out surfVel);
                    break;
            }

            if (got) { sum += waterY; count++; }
        }

        if (count > 0)
        {
            float avgWater = sum / count;
            var p = transform.position;
            p.y = avgWater + waterlineYOffset;
            transform.position = p;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        var m = transform.localToWorldMatrix;
        var c = m.MultiplyPoint3x4(localHullBounds.center);
        var sz = localHullBounds.size;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(c, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, sz);
        Gizmos.matrix = old;

        if (_localSamplePoints != null)
        {
            foreach (var p in _localSamplePoints)
            {
                Vector3 w = transform.TransformPoint(p);
                Gizmos.DrawSphere(w, 0.06f);
            }
        }
    }
#endif
}
