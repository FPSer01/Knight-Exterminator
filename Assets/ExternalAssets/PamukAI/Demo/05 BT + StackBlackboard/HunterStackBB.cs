using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PamukAI.PAI;

namespace PAI_Demo
{
    public class HunterStackBB : MonoBehaviour
    {
        public float speed = 2;

        Stack<Transform> targets = new Stack<Transform>();
        Transform Target = null;

        // This example demonstrates a Behavior Tree using a Stack-based Blackboard to manage targets.
        // The hunter will visit targets in a stack, moving towards each target until it is near enough,
        // then it will pop the target from the stack and move to the next one.

        void Start()
        {
            // Find all targets in the scene and add them to the stack
            foreach (var t in gameObject.scene.GetRootGameObjects().Where(go => go.name.StartsWith("Target")))
                targets.Push(t.transform);
        }

        void Update()
        {
            Tick(Chase);
        }

        bool Chase()
        {
            // Execute once when sequence starts
            if (ONCE)
            {
                // if no targets are available, return False to stop the sequence
                if (!targets.TryPop(out Target))
                    return false;
            }

            if (IsNear(transform, Target))
                return false;// Target reached, return False to stop current sequence and restart it

            // Move towards the current target
            return MoveTo(transform, Target, speed);
        }
    }
}