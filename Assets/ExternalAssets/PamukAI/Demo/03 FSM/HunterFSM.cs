using UnityEngine;
using static PamukAI.PAI;

namespace PAI_Demo
{
    public class HunterFSM : MonoBehaviour
    {
        public Transform target;
        public Transform camp;
        public AudioClip alert;
        public float speed = 1;
        public float viewDistance = 7;

        // current state of FSM
        Method fsm;

        void Start()
        {
            // This example demonstrates a simple FSM using a Behavior Tree,
            // where the AI can switch between Camp, Patrol, and Chase states based on conditions.
            // Also you can press Space (runtime) to force the AI switch back to Camp state.

            SwitchState(Camp, ref fsm);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                SwitchState(Camp, ref fsm);

            Tick(fsm);

            name = $"State: {fsm.Method.Name}";
        }

        bool Patrol()
        {
            LogOnce("Enter Patrol State");

            if (IsNear(transform, target, viewDistance))
                SwitchState(Chase, ref fsm);

            return true;
        }

        bool Chase()
        {
            LogOnce("Enter Chase State");

            if (ONCE)
                AudioSource.PlayClipAtPoint(alert, transform.position);

            if (!IsNear(transform, target, viewDistance))
                SwitchState(Patrol, ref fsm);

            return MoveTo(transform, target, speed);
        }

        bool Camp()
        {
            LogOnce("Enter Camp State");

            MoveTo(transform, camp, speed);

            if (!IsNear(transform, camp))
                return true;

            return SwitchState(Patrol, ref fsm);
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 300, 300, 120), "Press Space to force \r\nthe AI switch to Camp state.", GUI.skin.box);
        }
    }
}