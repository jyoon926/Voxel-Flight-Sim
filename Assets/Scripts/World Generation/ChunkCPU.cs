using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChunkCPU : MonoBehaviour
{
    private World world;

    // Chunk
        public int[,,] voxels;
        private int width;
        private int chunkWidth;
        private int resolution;
        private Vector3Int coordinates;

    // Mesh
        private Mesh mesh;
        private List<Vector3> vertices;
        private List<Vector2> uvs;
        private List<int> triangles;
        private Color[] colors;
    
    // Misc
        [HideInInspector] public int pass = 0;
        [HideInInspector] public bool createMesh;

    public void CreateChunk(World world, Vector3Int coordinates) {
        // Initialize variables
        this.world = world;
        width = world.GetWidth();
        resolution = world.GetResolution();
        chunkWidth = width * resolution;
        this.coordinates = coordinates;
        
        // Add components to GameObject
        gameObject.AddComponent<MeshRenderer>();
        GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshCollider>();
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = world.GetMaterial();

        GetVoxels();

        if (createMesh && !world.chunksToGenerateMesh.Contains(coordinates)) {
            world.chunksToGenerateMesh.Add(coordinates);
            world.chunksToGenerateMeshQueue.Enqueue(coordinates);
        }
    }

    // Gets base voxel data using the compute shader
    private void GetVoxels() {
        int padded = chunkWidth + 2;
        voxels = new int[padded, padded, padded];
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        for (int x = 0; x < padded; ++x) {
        for (int y = 0; y < padded; ++y) {
        for (int z = 0; z < padded; ++z) {
            int posX = ((x - 1) / resolution) + coordinates.x * width;
            int posY = ((y - 1) / resolution) + coordinates.y * width;
            int posZ = ((z - 1) / resolution) + coordinates.z * width;
            float value = noise.GetNoise(posX * 1f, posY * 1f, posZ * 1f) * 20f + 20f;
            if (y > value) {
                voxels[x, y, z] = 0;
            } else {
                voxels[x, y, z] = 1;
            }
        }
        }
        }
        pass = 1;
        ShouldCreateMesh(voxels);
    }

    // Generate mesh
    public void GenerateMesh() {
        GetMesh();
        UpdateMesh();
    }

    // Gets vertices data using compute shader on the GPU
    private void GetMesh() {
        vertices = new List<Vector3>();
        triangles = new List<int>();
        uvs = new List<Vector2>();
        int i = 0;
        for (int x = 1; x < chunkWidth + 1; ++x) {
        for (int y = 1; y < chunkWidth + 1; ++y) {
        for (int z = 1; z < chunkWidth + 1; ++z) {
            if (voxels[x, y, z] != 0) {
                float posX = (float)(x - 1) / (float)resolution;
                float posY = (float)(y - 1) / (float)resolution;
                float posZ = (float)(z - 1) / (float)resolution;
                float fraction = 1f / (float)resolution;
                if (voxels[x, y, z - 1] == 0) {
                    vertices.Add(new Vector3(posX, posY, posZ));
                    vertices.Add(new Vector3(posX, posY + fraction, posZ));
                    vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ));
                    vertices.Add(new Vector3(posX + fraction, posY, posZ));
                    AddTextureToFace(voxels[x, y, z - 1]);
                    AddTriangles(i);
                    i++;
                }
                // Right
                if (voxels[x + 1, y, z] == 0) {
                    vertices.Add(new Vector3(posX + fraction, posY, posZ));
                    vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ));
                    vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ + fraction));
                    vertices.Add(new Vector3(posX + fraction, posY, posZ + fraction));
                    AddTextureToFace(voxels[x + 1, y, z]);
                    AddTriangles(i);
                    i++;
                }
                // Back
                if (voxels[x, y, z + 1] == 0) {
                    vertices.Add(new Vector3(posX + fraction, posY, posZ + fraction));
                    vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ + fraction));
                    vertices.Add(new Vector3(posX, posY + fraction, posZ + fraction));
                    vertices.Add(new Vector3(posX, posY, posZ + fraction));
                    AddTextureToFace(voxels[x, y, z + 1]);
                    AddTriangles(i);
                    i++;
                }
                // Left
                if (voxels[x - 1, y, z] == 0) {
                    vertices.Add(new Vector3(posX, posY, posZ + fraction));
                    vertices.Add(new Vector3(posX, posY + fraction, posZ + fraction));
                    vertices.Add(new Vector3(posX, posY + fraction, posZ));
                    vertices.Add(new Vector3(posX, posY, posZ));
                    AddTextureToFace(voxels[x - 1, y, z]);
                    AddTriangles(i);
                    i++;
                }
                // Up
                if ((voxels[x, y + 1, z] == 0)) {
                    vertices.Add(new Vector3(posX, posY + fraction, posZ));
                    vertices.Add(new Vector3(posX, posY + fraction, posZ + fraction));
                    vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ + fraction));
                    vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ));
                    AddTextureToFace(voxels[x, y + 1, z]);
                    AddTriangles(i);
                    i++;
                }
                // Down
                if ((voxels[x, y - 1, z] == 0)) {
                    vertices.Add(new Vector3(posX, posY, posZ + fraction));
                    vertices.Add(new Vector3(posX, posY, posZ));
                    vertices.Add(new Vector3(posX + fraction, posY, posZ));
                    vertices.Add(new Vector3(posX + fraction, posY, posZ + fraction));
                    AddTextureToFace(voxels[x, y - 1, z]);
                    AddTriangles(i);
                    i++;
                }
            }
        }
        }
        }
        pass = 3;
    }

    private void AddTriangles(int i) {
        triangles.Add(i * 4);
        triangles.Add(i * 4 + 1);
        triangles.Add(i * 4 + 2);
        triangles.Add(i * 4);
        triangles.Add(i * 4 + 2);
        triangles.Add(i * 4 + 3);
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

    // Apply mesh data to GameObject
    private void UpdateMesh() {
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        // mesh.colors = colors;
        mesh.RecalculateNormals();
        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    // Whether or not this chunk will need to render faces
    private void ShouldCreateMesh(int[,,] output) {
        createMesh = false;
        bool hasAir = false;
        bool hasSolid = false;
        foreach (int voxel in output) {
            if (!hasAir && voxel == 0)
                hasAir = true;
            if (!hasSolid && voxel != 0)
                hasSolid = true;
        }
        if (hasAir && hasSolid) {
            createMesh = true;
        }
    }
}
