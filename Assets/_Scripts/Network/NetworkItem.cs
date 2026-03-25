using System;
using Unity.Netcode;
using UnityEngine;

public struct NetworkItem : INetworkSerializable, IEquatable<NetworkItem>
{
    public int ItemDatabaseIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ItemDatabaseIndex);
    }

    public bool Equals(NetworkItem other) => ItemDatabaseIndex == other.ItemDatabaseIndex;
}
