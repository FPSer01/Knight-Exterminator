using System;
using Unity.Netcode;
using UnityEngine;

public class TestLockOnTarget : MonoBehaviour, ICameraLockable
{
    public Transform GetLockOnPoint()
    {
        return transform;
    }

    public void SetDeathCallback(Action callback) { }

    public void DeleteDeathCallback(Action callback) { }

    public void SetCanGetLockPoint(bool canGetLockPoint)
    {
        
    }
}
