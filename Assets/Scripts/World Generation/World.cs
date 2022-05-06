using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour {
    [Header("Parameters")]
        [SerializeField] private int width;
        [SerializeField] private int resolution;
        [SerializeField] private int viewDistance;
        [SerializeField] private int chunkUpdatesPerFrame;
        [SerializeField] private int meshDrawsPerFrame;
        [SerializeField] private Transform viewer;

    [Header("Noise Parameters")]
        [SerializeField] private ComputeShader voxelShader;
        [SerializeField] private float frequency;
        [SerializeField] private int octaves;
        [SerializeField] private float lacunarity;
        [SerializeField] private float threshold;
        [SerializeField] private int amplitude;
        [SerializeField] private int midLevel;

    [Header("Miscellaneous")]
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private Material material;
        [SerializeField] private GameObject plane;
        [SerializeField] private GameObject explosion;
        // public GameObject[] blocks;
        // [SerializeField] private RigidbodyCharacter player;

    // Hidden
        public Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
        private Dictionary<Vector3Int, Chunk> visibleChunks = new Dictionary<Vector3Int, Chunk>();
        private HashSet<Vector3Int> chunksToCreate = new HashSet<Vector3Int>();
        private Queue<Vector3Int> chunksToCreateQueue = new Queue<Vector3Int>();
        [HideInInspector] public HashSet<Vector3Int> chunksToGenerateMesh = new HashSet<Vector3Int>();
        [HideInInspector] public Queue<Vector3Int> chunksToGenerateMeshQueue = new Queue<Vector3Int>();
        [HideInInspector] public List<Vector3Int> chunksToPassUpdate = new List<Vector3Int>();
        private Vector3Int viewerCoordinates;
        private int viewDistanceInChunks;
        private int started;
        private bool calculatedVoxelsInPreviousUpdate;
        private bool runMesh;
        [HideInInspector] public FastNoiseLite noise;

    private void Start() {
        started = -1;
        calculatedVoxelsInPreviousUpdate = true;
        viewDistanceInChunks = (int)(viewDistance / width);
        noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.Value);

        voxelShader.SetFloat("frequency", frequency);
        voxelShader.SetInt("octaves", octaves);
        voxelShader.SetFloat("lacunarity", lacunarity);
        voxelShader.SetFloat("threshold", threshold);
        voxelShader.SetInt("midLevel", midLevel);
        voxelShader.SetInt("amplitude", amplitude);
        voxelShader.SetInt("resolution", resolution);
        voxelShader.SetInt("chunkWidth", width * resolution);

        UpdateChunks();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update() {
        Vector3Int coordinates = WorldToChunk(viewer.position);

        if (coordinates != viewerCoordinates) {
            viewerCoordinates = coordinates;
            UpdateVisibleChunks();
            UpdateChunks();
        }

        runMesh = false;
        if (chunksToGenerateMeshQueue.Count > 0 && calculatedVoxelsInPreviousUpdate) {
            GenerateMeshes();
            calculatedVoxelsInPreviousUpdate = false;
        }
        if (chunksToCreate.Count > 0 && !runMesh) {
            CreateChunks();
            calculatedVoxelsInPreviousUpdate = true;
            if (started < 1) {
                started = 0;
            }
        } else {
            calculatedVoxelsInPreviousUpdate = true;
        }

        if (started == 0 && chunksToCreate.Count == 0) {
            // All chunks are created
            started = 1;
            plane.SetActive(true);
            // player.Init();
        }

        int count = chunksToPassUpdate.Count;
        for (int i = 0; i < count; ++i) {
            Vector3Int coords = chunksToPassUpdate[i];
            Chunk chunk = chunks[coords];
            if (chunk.pass > 2) {
                break;
            } else if (chunk.pass == 1) {
                if (chunk.CheckPassesOfSurroundingChunks(1)) {
                    chunk.GetStructures();
                }
            } else if (chunk.pass == 2) {
                if (chunk.CheckPassesOfSurroundingChunks(2)) {
                    chunk.ShouldCreateMesh();
                    if (chunk.createMesh && !chunksToGenerateMesh.Contains(coords)) {
                        chunksToGenerateMesh.Add(coords);
                        chunksToGenerateMeshQueue.Enqueue(coords);
                    }
                    chunksToPassUpdate.Remove(coords);
                    i--;
                    count--;
                }
            }
        }
    }
    
    //Update already visible chunks based on their distance from the viewer
    private void UpdateVisibleChunks() {
        List<Vector3Int> hideChunks = new List<Vector3Int>();
        foreach (KeyValuePair<Vector3Int, Chunk> pair in visibleChunks) {
            Vector3Int chunk = pair.Key;
            if (!ChunkIsInView(chunk)) {
                visibleChunks[chunk].gameObject.SetActive(false);
                hideChunks.Add(chunk);
            }
        }
        for (int i = 0; i < hideChunks.Count; ++i) {
            Vector3Int chunk = hideChunks[i];
            visibleChunks.Remove(chunk);
        }
    }

    //Update new chunks inside view distance that need to be either created or made visible
    private void UpdateChunks() {
        for (int x = viewerCoordinates.x - viewDistanceInChunks; x < viewerCoordinates.x + viewDistanceInChunks; ++x) {
        for (int z = viewerCoordinates.z - viewDistanceInChunks; z < viewerCoordinates.z + viewDistanceInChunks; ++z) {
        Vector3Int chunk = new Vector3Int(x, 0, z);
        if (ChunkIsInView(chunk)) {
            for (int y = 0; y < 6; ++y) {
                UpdateChunk(new Vector3Int(x, y, z));
            }
        }
        }
        }
    }

    void UpdateChunk(Vector3Int chunk) {
        if (ChunkIsInView(chunk) && !visibleChunks.ContainsKey(chunk) && !chunksToCreate.Contains(chunk)) {
            if (chunks.ContainsKey(chunk)) {
                Chunk chunkObject = chunks[chunk];
                chunkObject.gameObject.SetActive(true);
                visibleChunks.Add(chunk, chunkObject);
            } else {
                chunksToCreate.Add(chunk);
                chunksToCreateQueue.Enqueue(chunk);
            }
        }
    }

    //Create chunks that need to be created
    void CreateChunks() {
        for (int i = 0; i < chunkUpdatesPerFrame; ++i) {
            if (chunksToCreate.Count > 0) {
                Vector3Int chunk = chunksToCreateQueue.Dequeue();
                chunksToCreate.Remove(chunk);
                if (ChunkIsInView(chunk) && !chunks.ContainsKey(chunk)) {
                    CreateChunk(chunk);
                } else {
                    i--;
                }
            }
        }
    }

    void GenerateMeshes() {
        for (int i = 0; i < meshDrawsPerFrame; ++i) {
            if (chunksToGenerateMeshQueue.Count > 0) {
                Vector3Int chunk = chunksToGenerateMeshQueue.Dequeue();
                if (ChunkIsInView(chunk)) {
                    chunks[chunk].GenerateMesh();
                    chunksToGenerateMesh.Remove(chunk);
                    runMesh = true;
                } else {
                    chunksToGenerateMeshQueue.Enqueue(chunk);
                }
            }
        }
    }

    private void CreateChunk(Vector3Int coordinates) {
        GameObject chunk = GameObject.Instantiate(chunkPrefab, ChunkToWorld(coordinates), Quaternion.identity);
        Chunk chunkClass = chunk.GetComponent<Chunk>();
        chunk.transform.parent = this.transform;
        chunkClass.CreateChunk(this, coordinates);
        chunks.Add(coordinates, chunkClass);
        visibleChunks.Add(coordinates, chunkClass);
    }

    public void Build(Vector3 position, int material) {
        Vector3Int chunk = WorldToChunk(position);
        Vector3 tmp = position - chunk * width;
        Vector3Int voxelPosition = new Vector3Int(Mathf.FloorToInt(tmp.x), Mathf.FloorToInt(tmp.y), Mathf.FloorToInt(tmp.z));
        int radius = Mathf.RoundToInt(UnityEngine.Random.Range(0.8f, 1.2f) * 12f);
        if (chunks.ContainsKey(chunk)) {
            chunks[chunk].Build(voxelPosition, radius, material);
        }
        StartCoroutine(Explosion(position));
    }

    IEnumerator Explosion(Vector3 position) {
        yield return 0;
        GameObject.Instantiate(explosion, position, Quaternion.identity);
    }

    //Check if the input chunk coordinate is within view distance
    public bool ChunkIsInView(Vector3Int chunk) {
        return ChunkDistance(chunk) <= viewDistance;
    }

    //Return the chunk coordinates of a world position
    public Vector3Int WorldToChunk(Vector3 position) {
        return new Vector3Int(Mathf.FloorToInt(position.x / width), Mathf.FloorToInt(position.y / width), Mathf.FloorToInt(position.z / width));
    }

    //Return the world position of a given chunk coordinate
    public Vector3 ChunkToWorld(Vector3Int chunk) {
        return new Vector3(chunk.x * width, chunk.y * width, chunk.z * width);
    }

    //Return the world distance from the viewer to the given chunk coordinate
    public float ChunkDistance(Vector3Int chunk) {
        return Vector3.Distance(ChunkToWorld(viewerCoordinates), ChunkToWorld(chunk));
    }

    public int GetWidth() {
        return width;
    }

    public int GetResolution() {
        return resolution;
    }

    public int GetViewDistance() {
        return viewDistance;
    }

    public ComputeShader GetVoxelShader() {
        return voxelShader;
    }

    public float GetFrequency() {
        return frequency;
    }

    public int GetOctaves() {
        return octaves;
    }

    public float GetLacunarity() {
        return lacunarity;
    }
    
    public float GetThreshold() {
        return threshold;
    }

    public int GetAmplitude() {
        return amplitude;
    }
    
    public int GetMidLevel() {
        return midLevel;
    }

    public Material GetMaterial() {
        return material;
    }
}
