using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChunkGPU : MonoBehaviour
{
    private World world;

    // Chunk
        public int[] voxels;
        private int width;
        private int chunkWidth;
        private int resolution;
        private Vector3Int coordinates;

    // GPU
        private ComputeShader voxelShader;
        private ComputeBuffer voxelBuffer;

    // Mesh
        private Mesh mesh;
        private Vector3[] vertices;
        private Vector2[] uvs;
        private int[] triangles;
        private Color[] colors;
    
    // Misc
        [HideInInspector] public int pass = 0;
        [HideInInspector] public bool createMesh;

    // public void FixedUpdate() {
    //     if (pass == 1) {
    //         // if (CheckPassesOfSurroundingChunks(1)) {
    //     //         GetStructures();
    //     //     }
    //     // } else if (pass == 2) {
    //     //     if (CheckPassesOfSurroundingChunks(2)) {
    //             if (createMesh && !world.chunksToGenerateMesh.Contains(coordinates)) {
    //                 world.chunksToGenerateMesh.Enqueue(coordinates);
    //             }
    //         // }
    //     }
    // }

    // private void UpdateNeighbors() {
    //     PassUpdate();
    //     Vector2Int up = coordinates + Vector2Int.up;
    //     Vector2Int down = coordinates + Vector2Int.down;
    //     Vector2Int right = coordinates + Vector2Int.right;
    //     Vector2Int left = coordinates + Vector2Int.left;
    //     Vector2Int upRight = coordinates + Vector2Int.up + Vector2Int.right;
    //     Vector2Int upLeft = coordinates + Vector2Int.up + Vector2Int.left;
    //     Vector2Int downRight = coordinates + Vector2Int.down + Vector2Int.right;
    //     Vector2Int downLeft = coordinates + Vector2Int.down + Vector2Int.left;
    //     if (world.chunks.ContainsKey(up))
    //         world.chunks[up].PassUpdate();
    //     if (world.chunks.ContainsKey(down))
    //         world.chunks[down].PassUpdate();
    //     if (world.chunks.ContainsKey(right))
    //         world.chunks[right].PassUpdate();
    //     if (world.chunks.ContainsKey(left))
    //         world.chunks[left].PassUpdate();
    //     if (world.chunks.ContainsKey(upRight))
    //         world.chunks[upRight].PassUpdate();
    //     if (world.chunks.ContainsKey(upLeft))
    //         world.chunks[upLeft].PassUpdate();
    //     if (world.chunks.ContainsKey(downRight))
    //         world.chunks[downRight].PassUpdate();
    //     if (world.chunks.ContainsKey(downLeft))
    //         world.chunks[downLeft].PassUpdate();
    // }

    // public void PassUpdate() {
    //     if (pass == 1) {
    //         if (CheckPassesOfSurroundingChunks(1)) {
    //             GetStructures();
    //         }
    //     } else if (pass == 2) {
    //         if (CheckPassesOfSurroundingChunks(2)) {
    //             if (!world.chunksToGenerateMesh.Contains(coordinates)) {
    //                 // bool hasAir = false;
    //                 // bool hasSolid = false;
    //                 // foreach (int voxel in voxels) {
    //                 //     if (!hasAir && voxel == 0)
    //                 //         hasAir = true;
    //                 //     if (!hasSolid && voxel != 0)
    //                 //         hasSolid = true;
    //                 // }
    //                 // if (hasAir && hasSolid) {
    //                     world.chunksToGenerateMesh.Enqueue(coordinates);
    //                 // }
    //             }
    //         }
    //     }
    // }

    public void CreateChunk(World world, Vector3Int coordinates) {
        // Initialize variables
        this.world = world;
        width = world.GetWidth();
        resolution = world.GetResolution();
        chunkWidth = width * resolution;
        this.coordinates = coordinates;
        voxelShader = world.GetVoxelShader();
        
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
        // Set variables
        voxelShader.SetFloat("frequency", world.GetFrequency());
        voxelShader.SetInt("octaves", world.GetOctaves());
        voxelShader.SetFloat("lacunarity", world.GetLacunarity());
        voxelShader.SetFloat("threshold", world.GetThreshold());
        voxelShader.SetInt("midLevel", world.GetMidLevel());
        voxelShader.SetInt("amplitude", world.GetAmplitude());
        voxelShader.SetInt("xCoordinate", coordinates.x);
        voxelShader.SetInt("yCoordinate", coordinates.y);
        voxelShader.SetInt("zCoordinate", coordinates.z);
        voxelShader.SetInt("resolution", resolution);
        voxelShader.SetInt("chunkWidth", chunkWidth);

        // Create buffer and voxels array
        int padded = chunkWidth + 2;
        int size = padded * padded * padded;
        voxels = new int[size];
        voxelBuffer = new ComputeBuffer(size, sizeof(int));

        // Get data
        int kernel = voxelShader.FindKernel("Voxels");
        voxelShader.SetBuffer(kernel, "V", voxelBuffer);
        int threadGroupSize = (chunkWidth + 4) / 4;
        voxelShader.Dispatch(kernel, threadGroupSize, threadGroupSize, threadGroupSize);
        voxelBuffer.GetData(voxels);
        voxelBuffer.Dispose();
        pass = 1;

        ShouldCreateMesh(voxels);
    }

    // 2nd Pass
    // Gets voxel data for structures
    // private void GetStructures() {
    //     int padded = chunkWidth + 2;
    //     for (int x = 1; x < padded - 1; ++x) {
    //     for (int y = 0; y < padded - 1; ++y) {
    //     for (int z = 1; z < padded - 1; ++z) {
    //         int index = padded * padded * z + padded * y + x;
    //         if (voxels[index] == -1) {
    //             List<Quaternion> treeVoxels = new List<Quaternion>();
    //             float random = UnityEngine.Random.Range(0.6f, 1f);
    //             int height = Mathf.RoundToInt(random * 7 * resolution);
    //             int radius = Mathf.RoundToInt(random * 3 * resolution);
    //             for (int h = -3; h < height; ++h) {
    //                 float r = 1 - (float)h / (float)height;
    //                 r = r * resolution * random * 1.25f;
    //                 int rad = Mathf.RoundToInt(r);
    //                 for (int i = -rad; i <= rad; ++i) {
    //                 for (int j = -rad; j <= rad; ++j) {
    //                     if (Vector2.Distance(Vector2.zero, new Vector2(i, j)) <= rad - 1)
    //                         treeVoxels.Add(new Quaternion(x + i, y + h, z + j, 17));
    //                     else if (Vector2.Distance(Vector2.zero, new Vector2(i, j)) <= rad) {
    //                         int rand = UnityEngine.Random.Range(1, 16);
    //                         if (rand > 1)
    //                             treeVoxels.Add(new Quaternion(x + i, y + h, z + j, 17));
    //                     }
    //                 }
    //                 }
    //             }
    //             for (int i = -radius; i <= radius; ++i) {
    //             for (int j = -radius; j <= radius / 1.5f; ++j) {
    //             for (int k = -radius; k <= radius; k++) {
    //                 if (Mathf.Sqrt(i*i + (j * 1.5f)*(j * 1.5f) + k*k) < radius - 1) {
    //                     treeVoxels.Add(new Quaternion(x + i, y + j + height, z + k, 5));
    //                 } else if (Mathf.Sqrt(i*i + (j * 1.5f)*(j * 1.5f) + k*k) < radius) {
    //                     int rand = UnityEngine.Random.Range(1, 3);
    //                     if (rand > 1)
    //                         treeVoxels.Add(new Quaternion(x + i, y + j + height, z + k, 5));
    //                 }
    //             }
    //             }
    //             }
    //             foreach (Quaternion v in treeVoxels) {
    //                 int xx = (int)v.x;
    //                 int yy = (int)v.y;
    //                 int zz = (int)v.z;
    //                 Vector3Int chunk = coordinates;

    //                 if (yy >= 0 && yy < chunkHeight) {
    //                     if (xx > 1 && xx < chunkWidth && zz > 1 && zz < chunkWidth) {
    //                         int i = padded * chunkHeight * zz + padded * yy + xx;
    //                         voxels[i] = (int)v.w;
    //                     } else { 
    //                         if (xx >= 0 && xx < padded) {
    //                             int zzz = zz;
    //                             Vector2Int cchunk = chunk;
    //                             if (zz <= 1) {
    //                                 zzz += chunkWidth;
    //                                 cchunk = new Vector2Int(chunk.x, chunk.y - 1);
    //                             } else if (zz >= chunkWidth) {
    //                                 zzz -= chunkWidth;
    //                                 cchunk = new Vector2Int(chunk.x, chunk.y + 1);
    //                             }
    //                             int i = padded * chunkHeight * (zzz) + padded * (yy) + (xx);
    //                             world.chunks[cchunk].voxels[i] = (int)v.w;
    //                         }
    //                         if (xx >= 0 && xx < padded && zz >= 0 && zz < padded) {
    //                             int i = padded * chunkHeight * zz + padded * yy + xx;
    //                             voxels[i] = (int)v.w;
    //                         }
    //                         if (xx <= 1) {
    //                             xx += chunkWidth;
    //                             chunk = new Vector2Int(chunk.x - 1, chunk.y);
    //                             if (zz >= 0 && zz < padded) {
    //                                 int i = padded * chunkHeight * (zz) + padded * (yy) + (xx);
    //                                 world.chunks[chunk].voxels[i] = (int)v.w;
    //                             }
    //                             if (zz <= 1) {
    //                                 zz += chunkWidth;
    //                                 chunk = new Vector2Int(chunk.x, chunk.y - 1);
    //                                 int i = padded * chunkHeight * (zz) + padded * (yy) + (xx);
    //                                 world.chunks[chunk].voxels[i] = (int)v.w;
    //                             } else if (zz >= chunkWidth) {
    //                                 zz -= chunkWidth;
    //                                 chunk = new Vector2Int(chunk.x, chunk.y + 1);
    //                                 int i = padded * chunkHeight * (zz) + padded * (yy) + (xx);
    //                                 world.chunks[chunk].voxels[i] = (int)v.w;
    //                             }
    //                         } else if (xx >= chunkWidth) {
    //                             xx -= chunkWidth;
    //                             chunk = new Vector2Int(chunk.x + 1, chunk.y);
    //                             if (zz >= 0 && zz < padded) {
    //                                 int i = padded * chunkHeight * (zz) + padded * (yy) + (xx);
    //                                 world.chunks[chunk].voxels[i] = (int)v.w;
    //                             }
    //                             if (zz <= 1) {
    //                                 zz += chunkWidth;
    //                                 chunk = new Vector2Int(chunk.x, chunk.y - 1);
    //                                 int i = padded * chunkHeight * (zz) + padded * (yy) + (xx);
    //                                 world.chunks[chunk].voxels[i] = (int)v.w;
    //                             } else if (zz >= chunkWidth) {
    //                                 zz -= chunkWidth;
    //                                 chunk = new Vector2Int(chunk.x, chunk.y + 1);
    //                                 int i = padded * chunkHeight * (zz) + padded * (yy) + (xx);
    //                                 world.chunks[chunk].voxels[i] = (int)v.w;
    //                             }
    //                         }
    //                     }
    //                 }
    //             }
    //         }
    //     }
    //     }
    //     }
    //     pass = 2;
    //     // UpdateNeighbors();
    // }

    // Generate mesh
    public void GenerateMesh() {
        GetMesh();
        UpdateMesh();
    }

    // Gets vertices data using compute shader on the GPU
    private void GetMesh() {

        // Create buffers
        voxelBuffer = new ComputeBuffer(voxels.Length, sizeof(int));
        voxelBuffer.SetData(voxels);

        int size = chunkWidth * chunkWidth * chunkWidth * 6;
        ComputeBuffer meshBuffer = new ComputeBuffer(size, sizeof(float) * 12 + sizeof(int), ComputeBufferType.Append);
        meshBuffer.SetCounterValue(0);
        ComputeBuffer argBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        // Set buffers
        int kernel = voxelShader.FindKernel("Mesh");
        voxelShader.SetBuffer(kernel, "V", voxelBuffer);
        voxelShader.SetBuffer(kernel, "M", meshBuffer);

        // Get append buffer size
        int threadGroupSize = chunkWidth / 4;
        voxelShader.Dispatch(kernel, threadGroupSize, threadGroupSize, threadGroupSize);
        ComputeBuffer.CopyCount(meshBuffer, argBuffer, 0);
        int[] args = new int[]{ 0 };
        argBuffer.GetData(args);

        // Get mesh data
        int faceCount = args[0];
        Face[] faces = new Face[faceCount];
        meshBuffer.GetData(faces);
        vertices = new Vector3[faceCount * 4];
        uvs = new Vector2[faceCount * 4];
        colors = new Color[faceCount * 4];
        
        ConvertVertexData(faceCount, faces);

        // Dispose buffers
        voxelBuffer.Dispose();
        meshBuffer.Dispose();
        argBuffer.Dispose();
        pass = 3;
    }

    void ConvertVertexData(int faceCount, Face[] faces) {
        for (int i = 0; i < faceCount; ++i) {
            AddTextureToFace(faces[i].f, i);
            // AddColorsToFace(i);
            for (int j = 0; j < 4; ++j) {
                vertices[i * 4 + j] = faces[i][j];
            }
        }
    }

    // Apply mesh data to GameObject
    private void UpdateMesh() {
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
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        vertices = null;
        triangles = null;
        uvs = null;
        colors = null;
    }

    // Whether or not this chunk will need to render faces
    private void ShouldCreateMesh(int[] output) {
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

    private void AddTextureToFace(int id, int index) {
        float divisions = 16f;
        float idF = (float)(id - 1);
        float offset = 1f / (divisions * 2f);
        float tinyOffset = offset * 0.5f;
        float xOffset = (idF % divisions) / divisions + tinyOffset;
        float yOffset = Mathf.FloorToInt(idF / divisions) / divisions + tinyOffset;
        uvs[index * 4] = new Vector2(xOffset, yOffset);
        uvs[index * 4 + 1] = new Vector2(xOffset, yOffset + offset);
        uvs[index * 4 + 2] = new Vector2(xOffset + offset, yOffset + offset);
        uvs[index * 4 + 3] = new Vector2(xOffset + offset, yOffset);
    }

    private void AddColorsToFace(int index) {
        float rand = UnityEngine.Random.Range(0f, 1f);
        Color color = new Color(rand, rand, rand, 1f);
        for (int i = 0; i < 4; ++i) {
            colors[index * 4 + i] = color;
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

    //Checks the passes of the eight surrounding chunks
    // private bool CheckPassesOfSurroundingChunks(int pass) {
    //     Vector2Int up = coordinates + Vector2Int.up;
    //     Vector2Int down = coordinates + Vector2Int.down;
    //     Vector2Int right = coordinates + Vector2Int.right;
    //     Vector2Int left = coordinates + Vector2Int.left;
    //     Vector2Int upRight = coordinates + Vector2Int.up + Vector2Int.right;
    //     Vector2Int upLeft = coordinates + Vector2Int.up + Vector2Int.left;
    //     Vector2Int downRight = coordinates + Vector2Int.down + Vector2Int.right;
    //     Vector2Int downLeft = coordinates + Vector2Int.down + Vector2Int.left;
    //     if (world.chunks.ContainsKey(up)
    //         && world.chunks.ContainsKey(down)
    //         && world.chunks.ContainsKey(right)
    //         && world.chunks.ContainsKey(left)
    //         && world.chunks.ContainsKey(upRight)
    //         && world.chunks.ContainsKey(upLeft)
    //         && world.chunks.ContainsKey(downRight)
    //         && world.chunks.ContainsKey(downLeft)) {
    //         if (world.chunks[up].pass >= pass
    //             && world.chunks[down].pass >= pass
    //             && world.chunks[left].pass >= pass
    //             && world.chunks[right].pass >= pass
    //             && world.chunks[upRight].pass >= pass
    //             && world.chunks[upLeft].pass >= pass
    //             && world.chunks[downRight].pass >= pass
    //             && world.chunks[downLeft].pass >= pass) {
    //             return true;
    //         }
    //     }
    //     return false;
    // }
}
