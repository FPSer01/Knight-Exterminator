using UnityEngine;

public class TutorialMapRoom : MapRoom
{
    [SerializeField] private Transform teleportPoint;

    public override Vector3 GetTeleportPoint()
    {
        return teleportPoint.position;
    }
}
