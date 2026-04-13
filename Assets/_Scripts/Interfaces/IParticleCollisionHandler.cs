using System;
using System.Collections.Generic;
using UnityEngine;

public interface IParticleCollisionHandler
{
    void HandleParticleHit(GameObject other, List<ParticleCollisionEvent> events, int count);
}
