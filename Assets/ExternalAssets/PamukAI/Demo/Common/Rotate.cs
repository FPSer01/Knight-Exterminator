using UnityEngine;

namespace PAI_Demo
{
    public class Rotate : MonoBehaviour
    {
        public Vector3 Center;
        public float Speed = 2f;

        void Update()
        {
            transform.RotateAround(Center, Vector3.up, Speed * Time.deltaTime);
        }
    }
}