using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkTest : MonoBehaviour
{
    int[] voxels;

    public Material material;
    public int chunkWidth = 32;
    public int chunkHeight = 128;
    public int subdivisions = 1;
    public float frequency = 0.005f;
    public int octaves = 6;
    public float lacunarity = 2f;
    public float threshold = 0.5f;
    public int amplitude = 16;
    public int midLevel = 16;
    public ComputeShader voxelShader;
    
    private int width;
    private int height;
    private ComputeBuffer voxelBuffer;
    private Mesh mesh;
    private Vector3[] vertices;
    private List<Vector2> uvs = new List<Vector2>();
    private int[] triangles;
    private List<Color> colors;

    public void CreateChunk() {
        width = chunkWidth * subdivisions;
        height = chunkHeight * subdivisions;

        DestroyImmediate(GetComponent<MeshRenderer>());
        DestroyImmediate(GetComponent<MeshFilter>());
        DestroyImmediate(GetComponent<MeshCollider>());

        float start = Time.realtimeSinceStartup;

        // Add components to GameObject
        gameObject.AddComponent<MeshRenderer>();
        GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshCollider>();
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = material;

        GenerateVoxels();
        float current = Time.realtimeSinceStartup;
        float v = current - start;
        Debug.Log("Generate Voxels(): " + v + "s, " + (1 / v) + " fps");
        GenerateMesh();
        float m = Time.realtimeSinceStartup - current;
        Debug.Log("Generate Mesh(): " + m + "s, " + (1 / m) + " fps");
        ApplyMesh();

        float overall = Time.realtimeSinceStartup - start;
        Debug.Log("Overall: " + overall + "s, " + (1 / overall) + " fps");
    }

    private void GenerateVoxels() {
        voxelShader.SetFloat("frequency", frequency);
        voxelShader.SetInt("octaves", octaves);
        voxelShader.SetFloat("lacunarity", lacunarity);
        voxelShader.SetFloat("threshold", threshold);
        voxelShader.SetInt("midLevel", midLevel);
        voxelShader.SetInt("amplitude", amplitude);
        voxelShader.SetInt("xCoordinate", 0);
        voxelShader.SetInt("yCoordinate", 0);
        voxelShader.SetInt("resolution", subdivisions);
        voxelShader.SetInt("chunkWidth", width);
        voxelShader.SetInt("chunkHeight", height);

        int padded = width + 2;
        int size = padded * padded * height;
        voxels = new int[size];
        voxelBuffer = new ComputeBuffer(size, sizeof(int));

        int kernel = voxelShader.FindKernel("Voxels");
        voxelShader.SetBuffer(kernel, "V", voxelBuffer);
        int threadGroupSizeW = (width + 4) / 4;
        int threadGroupSizeH = height / 4;
        voxelShader.Dispatch(kernel, threadGroupSizeW, threadGroupSizeH, threadGroupSizeW);
        voxelBuffer.GetData(voxels);
        voxelBuffer.Release();
    }

    private void GenerateMesh() {
        uvs.Clear();

        // Create buffers
        voxelBuffer = new ComputeBuffer(voxels.Length, sizeof(int));
        voxelBuffer.SetData(voxels);

        int size = width * height * width * 6;
        ComputeBuffer meshBuffer = new ComputeBuffer(size, sizeof(float) * 12 + sizeof(int), ComputeBufferType.Append);
        meshBuffer.SetCounterValue(0);
        ComputeBuffer argBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        // Set buffers
        int kernel = voxelShader.FindKernel("Mesh");
        voxelShader.SetBuffer(kernel, "V", voxelBuffer);
        voxelShader.SetBuffer(kernel, "M", meshBuffer);

        // Get append buffer size
        int threadGroupSizeW = width / 4;
        int threadGroupSizeH = height / 4;
        voxelShader.Dispatch(kernel, threadGroupSizeW, threadGroupSizeH, threadGroupSizeW);
        ComputeBuffer.CopyCount(meshBuffer, argBuffer, 0);
        int[] args = new int[]{ 0 };
        argBuffer.GetData(args);

        // Get mesh data
        int faceCount = args[0];
        Face[] faces = new Face[faceCount];
        meshBuffer.GetData(faces);
        
        vertices = new Vector3[faceCount * 4];
        colors = new List<Color>();
        for (int i = 0; i < faceCount; ++i) {
            AddTextureToFace(faces[i].f);
            AddColorsToFace();
            for (int j = 0; j < 4; ++j) {
                vertices[i * 4 + j] = faces[i][j];
            }
        }

        // Release buffers
        voxelBuffer.Release();
        meshBuffer.Release();
        argBuffer.Release();
    }

    private void ApplyMesh() {
        int[] triangles = new int[(vertices.Length / 4) * 6];
        int verts = 0;
        for (int i = 0; i < vertices.Length / 4; ++i) {
            triangles[verts] = i * 4;
            triangles[verts + 1] = i * 4 + 1;
            triangles[verts + 2] = i * 4 + 2;
            triangles[verts + 3] = i * 4;
            triangles[verts + 4] = i * 4 + 2;
            triangles[verts + 5] = i * 4 + 3;
            verts += 6;
        }
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    private void AddTextureToFace(int id) {
        float divisions = 16f;

        float idF = (float)(id - 1);
        float offset = 1f / (divisions * 2f);
        float tinyOffset = offset * 0.5f;
        float xOffset = (idF % divisions) / divisions + tinyOffset;
        float yOffset = Mathf.FloorToInt(idF / divisions) / divisions + tinyOffset;
        uvs.Add(new Vector2(xOffset, yOffset));
        uvs.Add(new Vector2(xOffset, yOffset + offset));
        uvs.Add(new Vector2(xOffset + offset, yOffset + offset));
        uvs.Add(new Vector2(xOffset + offset, yOffset));
    }

    private void AddColorsToFace() {
        float rand = UnityEngine.Random.Range(0f, 1f);
        Color color = new Color(rand, rand, rand, 1f);
        for (int i = 0; i < 4; ++i) {
            colors.Add(color);
        }
    }

    // A Face represents four vertix positions (a,b,c,d) and a voxel value (f)
    struct Face {
        #pragma warning disable 649
        // The four vertices
            public Vector3 a;
            public Vector3 b;
            public Vector3 c;
            public Vector3 d;
        // The voxel value
            public int f;
        public Vector3 this [int i] {
            get {
                switch (i) {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    case 2:
                        return c;
                    default:
                        return d;
                }
            }
        }
    }
}
