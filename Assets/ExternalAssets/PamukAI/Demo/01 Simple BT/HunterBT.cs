using UnityEngine;
using static PamukAI.PAI;

namespace PAI_Demo
{
    public class HunterBT : MonoBehaviour
    {
        public Transform target;
        public Transform camp;
        public float speed = 1;
        public float viewDistance = 7;

        // This example demonstrates a simple Behavior Tree for a hunter AI.
        // The hunter will either pursue the target if it is nearby, or return to camp if the target is not nearby.

        void Update()
        {
            SelectorTick(Chase, GoCamp);
        }

        bool Chase()
        {
            if (!IsNear(transform, target, viewDistance))
                return false;

            MoveTo(transform, target, speed);
            return true;
        }

        bool GoCamp()
        {
            MoveTo(transform, camp, speed);
            return true;
        }
    }
}