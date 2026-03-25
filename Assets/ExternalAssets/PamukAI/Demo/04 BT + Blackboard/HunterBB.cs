using System.Linq;
using UnityEngine;
using static PamukAI.PAI;

namespace PAI_Demo
{
    public class HunterBB : MonoBehaviour
    {
        public Transform camp;
        public float speed = 1;
        public float viewDistance = 7;

        Transform Target;

        // This example demonstrates a Behavior Tree using a Blackboard pattern,
        // where the AI will chase the nearest target if found, or return to camp if no target is nearby.
        // The target is stored in the Blackboard (Target variable) and updated each tick.

        void Update()
        {
            SelectorTick(Chase, GoCamp);
        }

        bool Chase()
        {
            FindNearestTarget();
            if (Target == null || !IsNear(transform, Target, viewDistance))
                return false;

            return MoveTo(transform, Target, speed);
        }

        bool GoCamp() => MoveTo(transform, camp, speed);

        private void FindNearestTarget()
        {
            var targets = gameObject.scene.GetRootGameObjects().Where(go => go.name.StartsWith("Target"));

            GameObject closest = null;
            float minSqrDist = viewDistance * viewDistance;

            foreach (var t in targets)
            {
                var sqrDist = (t.transform.position - transform.position).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closest = t;
                }
            }

            // Set the closest target in the Blackboard (or null)
            Target = closest?.transform;
        }
    }
}