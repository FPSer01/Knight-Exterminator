using UnityEngine;
using static PamukAI.PAI;

namespace PAI_Demo
{
    public class HunterUAI : MonoBehaviour
    {
        public Transform target;
        public Transform camp;
        public Transform home;
        public float speed = 1;
        public float viewDistance = 7;

        float tired = 0;

        // This example demonstrates a Behavior Tree using Utility AI concepts.
        // Sleeping at home when tired, hunting when not tired, and returning to camp if the target is not near.

        void Update()
        {
            Vote(Sleep, tired);
            Vote(Chase, IsNear(transform, target, viewDistance) ? 2 : 0);
            Vote(ReturnToCamp, 1f);

            MaxUtilityTick(Chase, ReturnToCamp, Sleep );
        }

        bool Chase()
        {
            LogOnce("Chase the target!");

            tired += Time.deltaTime * 0.3f; // Increase tiredness while hunting
            return MoveTo(transform, target, speed);
        }

        bool ReturnToCamp()
        {
            LogOnce("Return to camp...");
            return MoveTo(transform, camp, speed);
        }

        bool Sleep()
        {
            LogOnce("I am tired, go sleep...");
            
            if (!IsNear(transform, home))
                return MoveTo(transform, home, speed);

            LogOnce("Sleep at home...");

            // Simulate sleeping for 2 seconds
            if (Wait(2))
                return true;

            tired = 0; // Reset tiredness after sleeping
            return false;
        }
    }
}