using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class ClothStabilityMonitor : NetworkBehaviour
{
    [Header("Monitoring")]
    [SerializeField] private float checkInterval = 0.1f;       // как часто проверять (сек)
    [SerializeField] private float maxVertexSpeed = 50f;       // макс. скорость вершины (м/с)
    [SerializeField] private float maxDistanceFromOrigin = 10f;// макс. удаление от объекта
    [SerializeField] private int unstableFramesThreshold = 3;  // сколько плохих кадров до рестарта

    [Header("Reset")]
    [SerializeField] private float resetCooldown = 1f;         // пауза между ресетами
    [SerializeField] private bool disablePhysicsOnReset = true;

    [Header("Settings")]
    [SerializeField] private bool CheckOnOwner = false;

    private Cloth _cloth;
    private Vector3[] _prevPositions;
    private int _unstableFrameCount;
    private float _lastResetTime = -999f;
    private bool _isResetting;

    // Сохранённые параметры для полного ресета
    private ClothSkinningCoefficient[] _savedCoefficients;
    private Vector3 _initialLocalPos;
    private Quaternion _initialLocalRot;

    private void Awake()
    {
        _cloth = GetComponent<Cloth>();
        _initialLocalPos = transform.localPosition;
        _initialLocalRot = transform.localRotation;

        // Сохраняем skinning coefficients для восстановления
        _savedCoefficients = (ClothSkinningCoefficient[])_cloth.coefficients.Clone();
    }

    private void OnEnable()
    {
        if (CheckOnOwner && !IsOwner || !CheckOnOwner && IsOwner)
            return;

        StartCoroutine(MonitorRoutine());
    }

    private void OnDisable()
    {
        if (CheckOnOwner && !IsOwner || !CheckOnOwner && IsOwner)
            return;

        StopAllCoroutines();
    }

    private IEnumerator MonitorRoutine()
    {
        // Ждём первый кадр, чтобы симуляция инициализировалась
        yield return new WaitForSeconds(0.5f);

        _prevPositions = GetCurrentParticlePositions();

        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            if (_isResetting) continue;

            if (IsSimulationUnstable())
            {
                _unstableFrameCount++;
                Debug.LogWarning($"[ClothMonitor] Unstable frame #{_unstableFrameCount}");

                if (_unstableFrameCount >= unstableFramesThreshold)
                {
                    yield return StartCoroutine(ResetSimulation());
                }
            }
            else
            {
                _unstableFrameCount = 0;
                _prevPositions = GetCurrentParticlePositions();
            }
        }
    }

    private bool IsSimulationUnstable()
    {
        Vector3[] currentPositions = GetCurrentParticlePositions();

        if (currentPositions == null || currentPositions.Length == 0)
            return false;

        Vector3 origin = transform.position;

        for (int i = 0; i < currentPositions.Length; i++)
        {
            Vector3 pos = currentPositions[i];

            // 1. Проверка на NaN / Infinity
            if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z) ||
                float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || float.IsInfinity(pos.z))
            {
                Debug.LogWarning($"[ClothMonitor] NaN/Inf detected at vertex {i}");
                return true;
            }

            // 2. Слишком далеко от объекта
            if (Vector3.Distance(pos, origin) > maxDistanceFromOrigin)
            {
                Debug.LogWarning($"[ClothMonitor] Vertex {i} too far: {Vector3.Distance(pos, origin):F2}m");
                return true;
            }

            // 3. Резкий скачок скорости
            if (_prevPositions != null && i < _prevPositions.Length)
            {
                float speed = Vector3.Distance(pos, _prevPositions[i]) / checkInterval;
                if (speed > maxVertexSpeed)
                {
                    Debug.LogWarning($"[ClothMonitor] Vertex {i} speed too high: {speed:F2} m/s");
                    return true;
                }
            }
        }

        return false;
    }

    private IEnumerator ResetSimulation()
    {
        // Cooldown между ресетами
        if (Time.time - _lastResetTime < resetCooldown)
        {
            _unstableFrameCount = 0;
            yield break;
        }

        _isResetting = true;
        _lastResetTime = Time.time;
        Debug.Log("[ClothMonitor] Resetting cloth simulation...");

        // Способ 1: Toggle enabled (самый простой)
        if (disablePhysicsOnReset)
        {
            _cloth.enabled = false;
            yield return new WaitForSeconds(0.05f);

            // Возвращаем трансформ на место если уплыл
            transform.localPosition = _initialLocalPos;
            transform.localRotation = _initialLocalRot;

            // Восстанавливаем coefficients (могут испортиться при NaN)
            _cloth.coefficients = _savedCoefficients;

            yield return new WaitForSeconds(0.05f);
            _cloth.enabled = true;
        }
        else
        {
            // Способ 2: ClearTransformMotion — сбрасывает накопленный импульс
            _cloth.ClearTransformMotion();
        }

        yield return new WaitForSeconds(0.1f);

        _unstableFrameCount = 0;
        _prevPositions = GetCurrentParticlePositions();
        _isResetting = false;

        Debug.Log("[ClothMonitor] Cloth simulation reset complete.");
    }

    private Vector3[] GetCurrentParticlePositions()
    {
        // cloth.vertices — позиции в local space
        return _cloth.vertices;
    }

#if UNITY_EDITOR
    // Отображение статуса в инспекторе
    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        GUI.Label(new Rect(10, 10, 300, 20),
            $"Cloth unstable frames: {_unstableFrameCount}/{unstableFramesThreshold}");
    }
#endif
}
