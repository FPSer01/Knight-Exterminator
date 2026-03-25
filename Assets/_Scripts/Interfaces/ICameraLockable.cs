using System;
using UnityEngine;

public interface ICameraLockable
{
    Transform GetLockOnPoint();
    void SetDeathCallback(Action callback);
    void DeleteDeathCallback(Action callback);
    void SetCanGetLockPoint(bool canGetLockPoint);
}
