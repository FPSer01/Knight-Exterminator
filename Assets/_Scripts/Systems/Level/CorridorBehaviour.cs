using System.Collections.Generic;
using UnityEngine;
using static CorridorBehaviour;

public class CorridorBehaviour : LevelPrimitive
{
    [Header("Size")]
    public bool UseCustomSize;
    [SerializeField] private Vector3 customSize;
    [SerializeField] private LengthAxis lengthAxis = LengthAxis.X;

    public Vector3 GetSize()
    {
        if (UseCustomSize)
            return customSize;

        return Vector3.zero;
    }

    public float GetLength()
    {
        switch (lengthAxis)
        {
            case LengthAxis.X:
                return customSize.x;
            case LengthAxis.Y:
                return customSize.y;
            case LengthAxis.Z:
                return customSize.z;
        }

        return Mathf.Max(customSize.x, customSize.y, customSize.z);
    }

    private void OnDrawGizmosSelected()
    {
        if (UseCustomSize)
        {
            Gizmos.color = Color.white;

            Gizmos.DrawWireCube(transform.position + Vector3.Scale(customSize / 2, Vector3.up), customSize);
        }
    }

    public enum LengthAxis
    {
        X, Y, Z
    }
}
