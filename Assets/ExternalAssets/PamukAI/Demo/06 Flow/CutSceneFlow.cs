using UnityEngine;
using static PamukAI.PAI;

namespace PAI_Demo
{
    public class CutSceneFlow : MonoBehaviour
    {
        public Transform target1;
        public Transform target2;
        public Transform target3;
        bool showGui;

        // This example demonstrates a cutscene flow.
        // The cutscene will transition between three targets, changing the camera's field of view and looking at each target in sequence.

        private void Update()
        {
            Tick(Flow);
        }

        bool Flow()
        {
            // Check space key to start the cutscene flow
            if (ONCE && !Input.GetKeyDown(KeyCode.Space))
            {
                showGui = true;
                return false;
            }

            LogOnce("Cutscene started!");

            if (ONCE) { Camera.main.fieldOfView = 30; showGui = false; }
            if (ONCE) Camera.main.transform.LookAt(target1); // Look at the first target
            if (Wait(2)) return true; // Wait for 2 seconds
            if (ONCE) Camera.main.transform.LookAt(target2); // Look at the second target
            if (Wait(2)) return true; // Wait for 2 seconds
            if (ONCE) Camera.main.transform.LookAt(target3); // Look at the third target
            if (Wait(2)) return true; // Wait for 2 seconds
            if (ONCE) Camera.main.fieldOfView = 60; // Reset the camera field of view

            LogOnce("Cutscene ended!");

            return false; // End the cutscene flow
        }

        private void OnGUI()
        {
            if (showGui)
                GUI.Label(new Rect(10, 300, 300, 120), "Press Space to start the cutscene flow.", GUI.skin.box);
        }
    }
}