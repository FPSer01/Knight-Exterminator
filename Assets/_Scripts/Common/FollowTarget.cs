using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [SerializeField] private Transform _object;
    [SerializeField] private Transform target;
    [Space]
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool smoothFollow;
    [SerializeField] private float smoothValue;
    [Space]
    [SerializeField] private bool followRotation;
    [SerializeField] private Vector3 followAxisRotation;
    [SerializeField] private Vector3 offsetRotation;

    private void OnValidate()
    {
        Follow();
    }

    private void Update()
    {
        Follow();
    }

    private void Follow()
    {
        if (smoothFollow)
            _object.position = Vector3.Lerp(_object.position, target.position + offset, smoothValue * Time.deltaTime);
        else
            _object.position = target.position + offset;

        if (followRotation)
        {
            Vector3 currentEuler = _object.eulerAngles;
            Vector3 targetEuler = target.eulerAngles;

            // Заменяем только выбранные оси
            Vector3 finalEuler = new Vector3(
                Mathf.LerpAngle(currentEuler.x, targetEuler.x + offsetRotation.x, followAxisRotation.x),
                Mathf.LerpAngle(currentEuler.y, targetEuler.y + offsetRotation.y, followAxisRotation.y),
                Mathf.LerpAngle(currentEuler.z, targetEuler.z + offsetRotation.z, followAxisRotation.z)
            );

            _object.rotation = Quaternion.Euler(finalEuler);
        }

    }
}
