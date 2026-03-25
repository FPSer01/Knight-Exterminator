using UnityEngine;

namespace PamukAI
{
    public partial class PAI
    {
        #region Demo helpers
        public static bool IsNear(Transform transform, Transform target, float distance = 0.1f) => (transform.position - target.position).sqrMagnitude <= distance * distance;
        public static bool MoveTo(Transform transform, Transform target, float speed = 1)
        {
            transform.LookAt(target);
            transform.position = Vector3.MoveTowards(transform.position, target.position, Time.deltaTime * speed);
            return true;
        }
        #endregion
    }
}