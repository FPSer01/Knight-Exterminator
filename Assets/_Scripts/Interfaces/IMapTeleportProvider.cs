using UnityEngine;

public interface IMapTeleportProvider
{
    public Vector3 GetTeleportPoint();

    public void Highlight(bool highlight);
}
