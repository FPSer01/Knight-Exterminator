using System;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCollisionRelay : MonoBehaviour
{
    private ParticleSystem ps;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    public event Action<GameObject, List<ParticleCollisionEvent>, int> OnCollision;
    private void DoOnCollision(GameObject other, List<ParticleCollisionEvent> events, int count) => OnCollision?.Invoke(other, events, count);


    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void OnParticleCollision(GameObject other)
    {
        int count = ParticlePhysicsExtensions.GetCollisionEvents(ps, other, collisionEvents);

        DoOnCollision(other, collisionEvents, count);
    }
}
