using System.Collections.Generic;
using UnityEngine;

public class MapRoom : MonoBehaviour, IMapTeleportProvider
{
    [SerializeField] private SpriteRenderer highlightObject;

    private Vector2Int index;
    private Room room;
    private List<GameObject> corridors;
    private bool discovered = false;

    public Vector2Int Index { get => index; set => index = value; }
    public Room Room { get => room; set => room = value; }
    public List<GameObject> Corridors { get => corridors; set => corridors = value; }
    public bool IsDiscovered { get => discovered; set => discovered = value; }

    public Vector3 Position { get => transform.position; set => transform.position = value; }

    private void Start()
    {
        highlightObject.gameObject.SetActive(false);
    }

    public virtual Vector3 GetTeleportPoint()
    {
        return room.RealRoom.TeleportPoint.position;
    }

    public void Highlight(bool highlight)
    {
        highlightObject.gameObject.SetActive(highlight);
    }
}
