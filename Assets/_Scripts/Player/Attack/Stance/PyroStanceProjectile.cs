using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PyroStanceProjectile : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [Range(0f, 1f)][SerializeField] private float onHitSFXVolume = 1f;
    [SerializeField] private List<AudioClip> onHitSFX;

    [Header("VFX")]
    [SerializeField] private GameObject hitEffect;

    public event Action<Vector3> OnPathEnded;
    private void DoOnPathEnded(Vector3 endPoint) => OnPathEnded?.Invoke(endPoint);

    private void Start()
    {
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        EndPath();
    }

    private Vector3[] debugPath = null;

    private void Update()
    {
        if (debugPath != null)
        {
            DrawParabola(debugPath);
        }
    }

    private void EndPath()
    {
        rb.DOKill();

        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);

        PlayOnHitSFX();
        DoOnPathEnded(transform.position);

        Destroy(gameObject);
    }

    public void SetPath(Vector3 endPoint, float time, int points)
    {
        Vector3[] path = new Vector3[points];

        float a = GetParabolaCoefficient(transform.position, endPoint);
        float delta = 1f / points;

        for (int i = 0; i < path.Length; i++)
        {
            path[i] = GetPointOnParabola(transform.position, endPoint, a, delta * i);
        }

        debugPath = path;

        rb.isKinematic = true;
        rb.DOPath(path, time, PathType.CatmullRom, gizmoColor: Color.green).SetOptions(closePath: false).SetLookAt(0.01f)
            .OnComplete(() =>
            {
                rb.isKinematic = false;
                EndPath();
            });
    }

    private float GetParabolaCoefficient(Vector3 startPoint, Vector3 endPoint)
    {
        Vector3 point = endPoint - startPoint;

        // Горизонтальное расстояние в плоскости XZ
        float h = new Vector2(point.x, point.z).magnitude;

        if (Mathf.Approximately(h, 0f))
            throw new ArgumentException("Конечная точка совпадает с вершиной по XZ");

        return point.y / (h * h);
    }

    private Vector3 GetPointOnParabola(Vector3 startPoint, Vector3 endPoint, float a, float t)
    {
        Vector3 delta = endPoint - startPoint;

        // Горизонтальное направление в плоскости XZ (нормализованное)
        Vector3 horizontalDir = new Vector3(delta.x, 0f, delta.z).normalized;

        // Полное горизонтальное расстояние до конечной точки
        float totalH = new Vector2(delta.x, delta.z).magnitude;

        // Горизонтальное расстояние в момент t
        float h = totalH * t;

        // Высота по параболе
        float y = a * h * h;

        return startPoint + horizontalDir * h + Vector3.up * y;
    }

    private void DrawParabola(Vector3[] points)
    {
        for (int i = 1; i < points.Length; i++)
        {
            Debug.DrawLine(points[i - 1], points[i], Color.green);
        }
    }

    private void PlayOnHitSFX()
    {
        GameObject sfxObjectPrefab = sfxSource.gameObject;
        GameObject sfxObject = Instantiate(sfxObjectPrefab, transform.position, Quaternion.identity);

        AudioSource source = sfxObject.GetComponent<AudioSource>();

        int index = Random.Range(0, onHitSFX.Count);
        AudioClip clip = onHitSFX[index];

        source.PlayOneShot(clip, onHitSFXVolume);

        Destroy(sfxObject, clip.length);
    }
}
