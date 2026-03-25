using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelChunksController : MonoBehaviour
{
    [Header("Chunks")]
    [SerializeField] private List<LevelChunk> chunks = new();
    private LevelChunk currentChunk = null;

    [Header("Settings")]
    [SerializeField] private float renderChunksDistance;
    [SerializeField] private bool enableCulling;

    public float RenderChunksDistance { set => renderChunksDistance = value; get => renderChunksDistance; }
    public bool EnableCulling { set => enableCulling = value; get => enableCulling; }

    private Camera currentCamera;
    private Transform chunksRoot;

    private float distanceBetweenChunks;

    private void Start()
    {
        StartCoroutine(TryGetClientCamera());
    }

    private IEnumerator TryGetClientCamera()
    {
        Camera cam = null;

        while (cam == null)
        {
            cam = Camera.main;

            yield return null;
        }

        currentCamera = cam;
    }

    public void CreateChunk(Vector2Int roomIndex, List<LevelPrimitive> primitives, Vector3 position)
    {
        if (chunks.Count == 0)
        {
            GameObject root = new("Chunks Root");
            chunksRoot = root.transform;
        }

        GameObject chunkObject = new($"Chunk [{roomIndex.x}, {roomIndex.y}]");
        chunkObject.transform.SetParent(chunksRoot);

        LevelChunk chunk = chunkObject.AddComponent<LevelChunk>();

        chunk.SetupChunk(roomIndex, position, primitives);
        chunks.Add(chunk);

        UpdateDistanceBetweenChunks();
    }

    private void Update()
    {
        if (!enableCulling || currentCamera == null)
            return;

        LevelChunk nearest = FindNearestChunk();

        if (nearest != currentChunk)
        {
            currentChunk = nearest;
            RefreshCulling();
            Debug.Log($"[Chunks] Entered [{currentChunk.Index.x}, {currentChunk.Index.y}]");
        }
    }

    private LevelChunk FindNearestChunk()
    {
        float minDist = float.MaxValue;
        LevelChunk nearest = currentChunk;

        foreach (var chunk in chunks)
        {
            float dist = Vector3.Distance(currentCamera.transform.position, chunk.Position);

            if (dist < minDist)
            {
                minDist = dist;
                nearest = chunk;
            }
        }

        return nearest;
    }

    private void RefreshCulling()
    {
        foreach (var chunk in chunks)
        {
            int dist = GetManhattanDistance(currentChunk.Index, chunk.Index);

            chunk.CullChunk(dist <= renderChunksDistance
                ? ChunkCullType.None
                : ChunkCullType.Full);
        }
    }

    private int GetManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    /// <summary>
    /// [ДЛЯ ДЕБАГА] Дистанция между чанками
    /// </summary>
    private void UpdateDistanceBetweenChunks()
    {
        if (chunks.Count <= 1)
        {
            distanceBetweenChunks = 0;
            return;
        }

        distanceBetweenChunks = Vector3.Distance(chunks[0].Position, chunks[1].Position);
    }

    private void OnDrawGizmosSelected()
    {
        if (chunks.Count <= 1)
            return;

        Vector3 chunkSize = new(
            distanceBetweenChunks,
            distanceBetweenChunks,
            distanceBetweenChunks
            );

        foreach (var chunk in chunks)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(chunk.Position + new Vector3(0, distanceBetweenChunks, 0), chunk.Position - new Vector3(0, distanceBetweenChunks, 0));

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(chunk.Position, chunkSize);
        }
    }
}
