using System.Collections.Generic;
using UnityEngine;

public class LevelChunk : MonoBehaviour
{
    private Vector2Int index;
    private List<LevelPrimitive> primitives;

    public Vector2Int Index { get => index; }
    public List<LevelPrimitive> Primitives { get => primitives; }
    public Vector3 Position { get => transform.position; }

    public void SetupChunk(Vector2Int index, Vector3 position, List<LevelPrimitive> primitives)
    {
        this.index = index;
        this.primitives = primitives;

        transform.position = position;
    }

    public void CullChunk(ChunkCullType cullType)
    {
        bool cullGameObjects;
        bool cullRenderers;

        switch (cullType)
        {
            case ChunkCullType.None:
                cullGameObjects = false; cullRenderers = false; break;
            case ChunkCullType.Full:
                cullGameObjects = true; cullRenderers = true; break;
            case ChunkCullType.GameObjects:
                cullGameObjects = true; cullRenderers = false; break;
            case ChunkCullType.Renderers:
                cullGameObjects = false; cullRenderers = true; break;
            default:
                cullGameObjects = true; cullRenderers = true; break;
        }

        foreach (var primitive in primitives)
        {
            primitive.SetActiveObjects(!cullGameObjects);
            primitive.SetActiveRenderers(!cullRenderers);
        }
    }
}
