using UnityEngine;

/// Gera um grid e deforma via GerstnerWaves.
/// Use um Material URP/Lit. O objeto pode seguir o player (opcional).
[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
public class GerstnerOceanMesh : MonoBehaviour
{
    [Header("Malha")]
    public int vertsX = 200;           // resolução em X
    public int vertsZ = 200;           // resolução em Z
    public float sizeX = 500f;         // metros
    public float sizeZ = 500f;         // metros
    public bool followTarget = true;   // ocean patch segue o alvo
    public Transform target;           // navio/câmera

    [Header("Ondas")]
    public GerstnerWaves waves;        // arraste o componente GerstnerWaves

    [Header("Sombreamento")]
    public bool recalcNormalsCPU = true; // usa normal das ondas (melhor) ou RecalculateNormals

    Mesh _mesh;
    Vector3[] _base;   // posições base (em plano)
    Vector3[] _verts;
    Vector3[] _normals;

    void Start()
    {
        var mf = GetComponent<MeshFilter>();
        _mesh = new Mesh { name = "GerstnerOceanMesh" };
        mf.sharedMesh = _mesh;

        BuildGrid();
    }

    void LateUpdate()
    {
        if (followTarget && target != null)
        {
            // mantém o patch sob o alvo (ancora por célula)
            float cellX = sizeX / (vertsX - 1);
            float cellZ = sizeZ / (vertsZ - 1);
            Vector3 p = transform.position;
            p.x = Mathf.Round(target.position.x / cellX) * cellX;
            p.z = Mathf.Round(target.position.z / cellZ) * cellZ;
            transform.position = p;
        }

        if (waves == null || _verts == null) return;

        float t = Time.time;
        int i = 0;
        for (int z = 0; z < vertsZ; z++)
        {
            for (int x = 0; x < vertsX; x++, i++)
            {
                // world pos do vértice "flat"
                Vector3 localFlat = _base[i];
                Vector3 worldPos = transform.TransformPoint(localFlat);

                waves.Sample(worldPos, t, out float h, out Vector3 disp, out Vector3 nrm, out _);

                // Aplicar deslocamento em espaço MUNDO, depois voltar p/ local
                Vector3 worldDeformed = new Vector3(worldPos.x + disp.x,
                                                    h, // altura final
                                                    worldPos.z + disp.z);

                _verts[i] = transform.InverseTransformPoint(worldDeformed);
                if (recalcNormalsCPU) _normals[i] = transform.InverseTransformDirection(nrm);
            }
        }

        _mesh.SetVertices(_verts);
        if (recalcNormalsCPU)
            _mesh.SetNormals(_normals);
        else
            _mesh.RecalculateNormals();
        _mesh.RecalculateBounds(); // opcional
    }

    void BuildGrid()
    {
        int vCount = vertsX * vertsZ;
        _base = new Vector3[vCount];
        _verts = new Vector3[vCount];
        _normals = new Vector3[vCount];

        Vector2[] uvs = new Vector2[vCount];
        int[] tris = new int[(vertsX - 1) * (vertsZ - 1) * 6];

        float dx = sizeX / (vertsX - 1);
        float dz = sizeZ / (vertsZ - 1);
        Vector3 origin = new Vector3(-sizeX * 0.5f, 0f, -sizeZ * 0.5f);

        int i = 0;
        for (int z = 0; z < vertsZ; z++)
        {
            for (int x = 0; x < vertsX; x++, i++)
            {
                var pos = origin + new Vector3(x * dx, 0f, z * dz);
                _base[i] = pos;
                _verts[i] = pos;
                _normals[i] = Vector3.up;
                uvs[i] = new Vector2((float)x / (vertsX - 1), (float)z / (vertsZ - 1));
            }
        }

        int ti = 0;
        for (int z = 0; z < vertsZ - 1; z++)
        {
            for (int x = 0; x < vertsX - 1; x++)
            {
                int a = z * vertsX + x;
                int b = a + 1;
                int c = a + vertsX;
                int d = c + 1;

                tris[ti++] = a; tris[ti++] = c; tris[ti++] = b;
                tris[ti++] = b; tris[ti++] = c; tris[ti++] = d;
            }
        }

        _mesh.indexFormat = (vCount > 65000) ? UnityEngine.Rendering.IndexFormat.UInt32
                                             : UnityEngine.Rendering.IndexFormat.UInt16;
        _mesh.SetVertices(_verts);
        _mesh.SetTriangles(tris, 0);
        _mesh.SetNormals(_normals);
        _mesh.SetUVs(0, uvs);
        _mesh.RecalculateBounds();
    }
}
