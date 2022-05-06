using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    private World world;

    // Chunk
        [HideInInspector] public int[] voxels;
        private int width;
        private int chunkWidth;
        private int resolution;
        private Vector3Int coordinates;

    // Mesh
        private Mesh mesh;
        private List<Vector3> vertices;
        private List<Vector2> uvs;
        private List<Color> colors;
        private List<int> triangles;
    
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
        vertices = new List<Vector3>();
        triangles = new List<int>();
        uvs = new List<Vector2>();
        colors = new List<Color>();

        int padded = chunkWidth + 2;
        int size = padded * padded * padded;
        voxels = new int[size];

        GetVoxels();
        world.chunksToPassUpdate.Add(coordinates);
    }

    // Gets base voxel data using the compute shader
    private void GetVoxels() {
        world.GetVoxelShader().SetInt("xCoordinate", coordinates.x);
        world.GetVoxelShader().SetInt("yCoordinate", coordinates.y);
        world.GetVoxelShader().SetInt("zCoordinate", coordinates.z);
        
        // Create buffer and voxels array
        ComputeBuffer voxelBuffer = new ComputeBuffer(voxels.Length, sizeof(int));

        // Get data
        int kernel = world.GetVoxelShader().FindKernel("Voxels");
        world.GetVoxelShader().SetBuffer(kernel, "V", voxelBuffer);
        int threadGroupSize = (chunkWidth + 4) / 4;
        world.GetVoxelShader().Dispatch(kernel, threadGroupSize, threadGroupSize, threadGroupSize);
        voxelBuffer.GetData(voxels);
        voxelBuffer.Release();
        pass = 1;
    }

    // Generate mesh
    public void GenerateMesh() {
        GetMesh();
        UpdateMesh();
    }

    // Gets vertices data using compute shader on the CPU
    private void GetMesh() {
        int i = 0;
        for (int x = 1; x < chunkWidth + 1; ++x) {
        for (int y = 1; y < chunkWidth + 1; ++y) {
        for (int z = 1; z < chunkWidth + 1; ++z) {
            int id = voxels[GetIndex(x, y, z)];
            if (id != 0) {
                float posX = (float)(x - 1) / (float)resolution;
                float posY = (float)(y - 1) / (float)resolution;
                float posZ = (float)(z - 1) / (float)resolution;
                float fraction = 1f / (float)resolution;
                float rand = Mathf.Clamp(UnityEngine.Random.Range(0f, 1f), 0f, 1f);
                Color color = new Color(rand, rand, rand, 1f);
                // Front
                if (voxels[GetIndex(x, y, z - 1)] == 0) {
                    vertices.Add(new Vector3(posX, posY, posZ));
                    vertices.Add(new Vector3(posX, posY + fraction, posZ));
                    vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ));
                    vertices.Add(new Vector3(posX + fraction, posY, posZ));
                    AddTextureToFace(id);
                    RandomizeColor(color);
                    AddTriangles(i);
                    i++;
                }
                // Right
                if (voxels[GetIndex(x + 1, y, z)] == 0) {
                    vertices.Add(new Vector3(posX + fraction, posY, posZ));
                    vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ));
                    vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ + fraction));
                    vertices.Add(new Vector3(posX + fraction, posY, posZ + fraction));
                    AddTextureToFace(id);
                    RandomizeColor(color);
                    AddTriangles(i);
                    i++;
                }
                // Back
                if (voxels[GetIndex(x, y, z + 1)] == 0) {
                    vertices.Add(new Vector3(posX + fraction, posY, posZ + fraction));
                    vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ + fraction));
                    vertices.Add(new Vector3(posX, posY + fraction, posZ + fraction));
                    vertices.Add(new Vector3(posX, posY, posZ + fraction));
                    AddTextureToFace(id);
                    RandomizeColor(color);
                    AddTriangles(i);
                    i++;
                }
                // Left
                if (voxels[GetIndex(x - 1, y, z)] == 0) {
                    vertices.Add(new Vector3(posX, posY, posZ + fraction));
                    vertices.Add(new Vector3(posX, posY + fraction, posZ + fraction));
                    vertices.Add(new Vector3(posX, posY + fraction, posZ));
                    vertices.Add(new Vector3(posX, posY, posZ));
                    AddTextureToFace(id);
                    RandomizeColor(color);
                    AddTriangles(i);
                    i++;
                }
                // Up
                if ((voxels[GetIndex(x, y + 1, z)] == 0)) {
                    vertices.Add(new Vector3(posX, posY + fraction, posZ));
                    vertices.Add(new Vector3(posX, posY + fraction, posZ + fraction));
                    vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ + fraction));
                    vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ));
                    AddTextureToFace(id);
                    RandomizeColor(color);
                    AddTriangles(i);
                    i++;
                }
                // Down
                if ((voxels[GetIndex(x, y - 1, z)] == 0)) {
                    vertices.Add(new Vector3(posX, posY, posZ + fraction));
                    vertices.Add(new Vector3(posX, posY, posZ));
                    vertices.Add(new Vector3(posX + fraction, posY, posZ));
                    vertices.Add(new Vector3(posX + fraction, posY, posZ + fraction));
                    AddTextureToFace(id);
                    RandomizeColor(color);
                    AddTriangles(i);
                    i++;
                }
            } else if (coordinates.y == 0 && y == 1) {
                float posX = (float)(x - 1) / (float)resolution;
                float posY = (float)(y - 1) / (float)resolution;
                float posZ = (float)(z - 1) / (float)resolution;
                float fraction = 1f / (float)resolution;
                // Up
                vertices.Add(new Vector3(posX, posY + fraction, posZ));
                vertices.Add(new Vector3(posX, posY + fraction, posZ + fraction));
                vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ + fraction));
                vertices.Add(new Vector3(posX + fraction, posY + fraction, posZ));
                AddTextureToFace(17);
                RandomizeColor(new Color(1f, 1f, 1f, 1f));
                AddTriangles(i);
                i++;
            }
        }
        }
        }
        pass = 3;
    }

    private int GetIndex(int x, int y, int z) {
        int padded = chunkWidth + 2;
        return padded * padded * z + padded * y + x;
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

    private void RandomizeColor(Color color) {
        for (int i = 0; i < 4; ++i) {
            colors.Add(color);
        }
    }

    // Apply mesh data to GameObject
    private void UpdateMesh() {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        GetComponent<MeshCollider>().sharedMesh = mesh;

        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        colors.Clear();
    }

    // Whether or not this chunk will need to render faces
    public void ShouldCreateMesh() {
        createMesh = false;
        bool hasAir = false;
        bool hasSolid = false;
        for (int i = 0; i < voxels.Length; ++i) {
            int voxel = voxels[i];
            if (!hasAir && voxel == 0)
                hasAir = true;
            if (!hasSolid && voxel != 0)
                hasSolid = true;
        }
        if (hasAir && hasSolid) {
            createMesh = true;
        }
    }

    // 2nd Pass
    // Gets voxel data for structures
    public void GetStructures() {
        int padded = chunkWidth + 2;
        for (int x = 1; x < padded - 1; ++x) {
        for (int y = 1; y < padded - 1; ++y) {
        for (int z = 1; z < padded - 1; ++z) {
            int index = padded * padded * z + padded * y + x;
            // Trees
            if (voxels[index] == -1) {
                List<Quaternion> treeVoxels = new List<Quaternion>();
                float random = UnityEngine.Random.Range(0.5f, 1f);
                // Trunk
                int height = Mathf.RoundToInt(random * 20 * resolution);
                int radius = Mathf.RoundToInt(random * 6 * resolution);
                for (int h = -2; h < height; ++h) {
                    float r = 1.5f - (float)h / (float)height;
                    r = r * resolution * 1.25f;
                    int rad = Mathf.RoundToInt(r);
                    for (int i = -rad; i <= rad; ++i) {
                    for (int j = -rad; j <= rad; ++j) {
                        if (Vector2.Distance(Vector2.zero, new Vector2(i, j)) <= rad - 1)
                            treeVoxels.Add(new Quaternion(x + i, y + h, z + j, 6));
                        else if (Vector2.Distance(Vector2.zero, new Vector2(i, j)) <= rad) {
                            int rand = UnityEngine.Random.Range(1, 16);
                            if (rand > 1)
                                treeVoxels.Add(new Quaternion(x + i, y + h, z + j, 6));
                        }
                    }
                    }
                }
                // Leaves
                for (int i = -radius; i <= radius; ++i) {
                for (int j = -radius; j <= radius / 1.5f; ++j) {
                for (int k = -radius; k <= radius; k++) {
                    if (Mathf.Sqrt(i*i + (j * 1.5f)*(j * 1.5f) + k*k) < radius - 1) {
                        treeVoxels.Add(new Quaternion(x + i, y + j + height, z + k, 5));
                    } else if (Mathf.Sqrt(i*i + (j * 1.5f)*(j * 1.5f) + k*k) < radius) {
                        int rand = UnityEngine.Random.Range(1, 3);
                        if (rand > 1)
                            treeVoxels.Add(new Quaternion(x + i, y + j + height, z + k, 5));
                    }
                }
                }
                }
                // Fill voxel arrays
                for (int vi = 0; vi < treeVoxels.Count; ++vi) {
                    Quaternion v = treeVoxels[vi];
                    int xx = (int)v.x;
                    int yy = (int)v.y;
                    int zz = (int)v.z;
                    for (int a = -1; a <= 1; ++a) {
                    for (int b = -1; b <= 1; ++b) {
                    for (int c = -1; c <= 1; ++c) {
                        int xxx = xx - (chunkWidth * a);
                        int yyy = yy - (chunkWidth * b);
                        int zzz = zz - (chunkWidth * c);
                        if (xxx >= 0 && xxx < padded && yyy >= 0 && yyy < padded && zzz >= 0 && zzz < padded) {
                            int i = padded * padded * zzz + padded * yyy + xxx;
                            Vector3Int chunk = coordinates + new Vector3Int(a, b, c);
                            // Debug.Log("Index: " + i + "; Chunk: " + chunk);
                            if (world.chunks.ContainsKey(chunk)) {
                                world.chunks[chunk].voxels[i] = (int)v.w;
                            }
                        }
                    }
                    }
                    }
                }
            }
        }
        }
        }
        pass = 2;
    }
    
    //Checks the passes of the eight surrounding chunks
    public bool CheckPassesOfSurroundingChunks(int pass) {
        for (int x = -1; x < 2; ++x) {
        for (int y = -1; y < 2; ++y) {
        for (int z = -1; z < 2; ++z) {
        if ((x != 0 || y != 0 || z != 0) &&
            (y + coordinates.y > -1 && y + coordinates.y < 6)) {
            Vector3Int neighbor = coordinates + new Vector3Int(x, y, z);
            if (!world.chunks.ContainsKey(neighbor) || world.chunks[neighbor].pass < pass) {
                return false;
            }
        }
        }
        }
        }
        return true;
    }

    public void Build(Vector3Int position, int radius, int material) {
        int padded = chunkWidth + 2;
        List<Vector3Int> chunksToUpdate = new List<Vector3Int>();
        for (int i = -radius; i <= radius; ++i) {
        for (int j = -radius; j <= radius; ++j) {
        for (int k = -radius; k <= radius; ++k) {
            int x = position.x + i;
            int y = position.y + j;
            int z = position.z + k;
            if (Vector3Int.Distance(Vector3Int.zero, new Vector3Int(i, j, k)) <= radius * UnityEngine.Random.Range(0.9f, 1f)) {
                int blockID = 0;
                for (int a = -1; a <= 1; ++a) {
                for (int b = -1; b <= 1; ++b) {
                for (int c = -1; c <= 1; ++c) {
                    int xxx = x - (chunkWidth * a);
                    int yyy = y - (chunkWidth * b);
                    int zzz = z - (chunkWidth * c);
                    if (xxx >= 0 && xxx < padded && yyy >= 0 && yyy < padded && zzz >= 0 && zzz < padded) {
                        int index = padded * padded * zzz + padded * yyy + xxx;
                        Vector3Int chunk = coordinates + new Vector3Int(a, b, c);
                        if (world.chunks.ContainsKey(chunk)) {
                            blockID = world.chunks[chunk].voxels[index];
                            world.chunks[chunk].voxels[index] = material;
                            if (!chunksToUpdate.Contains(chunk))
                                chunksToUpdate.Add(chunk);
                        }
                    }
                }
                }
                }
                // if (blockID != 0) {
                //     GameObject.Instantiate(world.blocks[blockID], new Vector3(x, y, z) + coordinates * width, Quaternion.identity);
                // }
            }
        }
        }
        }
        foreach (Vector3Int chunk in chunksToUpdate) {
            world.chunks[chunk].GenerateMesh();
        }
    }
}
