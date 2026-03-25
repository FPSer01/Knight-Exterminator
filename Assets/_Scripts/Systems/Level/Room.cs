using System;
using UnityEngine;

[Serializable]
public class Room
{
    public Vector2Int Index { get; set; }

    public Vector3 RealPosition { get; set; }

    public bool StartRoom { get; set; }
    public bool BossRoom { get; set; }
    public bool MerchantRoom { get; set; }
    public bool SpecialRoom { get; set; }

    public bool DoorTop { get; set; }
    public bool DoorBottom { get; set; }
    public bool DoorLeft { get; set; }
    public bool DoorRight { get; set; }

    public RoomBehaviour RealRoom { get; set; }

    public Room(Vector2Int index)
    {
        Index = index;
    }
}
