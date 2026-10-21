using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlickerLight : MonoBehaviour
{
    [SerializeField] private float flickerAmount = 0.25f;
    [SerializeField] private float flickerSpeed = 6f;
    [SerializeField] private float positionJitter = 0.03f;   // flames sway

    [Header("Dropouts")]
    [SerializeField] private float dropoutsPerMinute = 0f;   // the light cuts out 
    [SerializeField] private Vector2 dropoutDuration = new Vector2(0.15f, 1.2f);

    [Header("Monster Disturbance")]
    [SerializeField] private bool reactToMonster = true;     // lights stutter and die when monster is close
    [SerializeField] private float disturbRadius = 9f;

    // anything that disturbs lights (the monster) registers itself here
    private static readonly List<Transform> disturbers = new List<Transform>();
    public static void AddDisturber(Transform t) { if (!disturbers.Contains(t)) disturbers.Add(t); }
    public static void RemoveDisturber(Transform t) { disturbers.Remove(t); }

    private Light lightSource;
    private float baseIntensity;
    private Vector3 basePosition;
    private float seed;
    private float dropoutUntil;

    private void Awake()
    {
        lightSource = GetComponent<Light>();
        baseIntensity = lightSource.intensity;
        basePosition = transform.localPosition;
        seed = Random.Range(0f, 100f);   
    }

    private void Update()
    {
        float t = Time.time * flickerSpeed + seed;

        // two layers
        float slow = Mathf.PerlinNoise(t * 0.2f, seed) - 0.5f;
        float fast = Mathf.PerlinNoise(t, seed + 10f) - 0.5f;
        float intensity = baseIntensity * (1f + (slow + fast) * 2f * flickerAmount);

        // occasional dropout
        if (dropoutsPerMinute > 0f && Time.time > dropoutUntil && Random.value < dropoutsPerMinute / 60f * Time.deltaTime)
        {
            dropoutUntil = Time.time + Random.Range(dropoutDuration.x, dropoutDuration.y);
        }
        if (Time.time < dropoutUntil) intensity *= 0.04f;

        // closer the monster the more light stutters
        if (reactToMonster)
        {
            float closeness = MonsterCloseness();
            if (closeness > 0f)
            {
                float strobe = Mathf.PerlinNoise(Time.time * 22f, seed + 50f);
                intensity *= (1f - 0.65f * closeness) * (strobe < closeness * 0.6f ? 0.05f : 1f);
            }
        }

        lightSource.intensity = intensity;

        if (positionJitter > 0f)
        {
            Vector3 sway = new Vector3(Mathf.PerlinNoise(t, 1f) - 0.5f, Mathf.PerlinNoise(t, 2f) - 0.5f, Mathf.PerlinNoise(t, 3f) - 0.5f);
            transform.localPosition = basePosition + sway * 2f * positionJitter;
        }
    }

    private float MonsterCloseness()
    {
        float best = 0f;
        for (int i = disturbers.Count - 1; i >= 0; i--)
        {
            if (disturbers[i] == null) { disturbers.RemoveAt(i); continue; }
            float d = Vector3.Distance(disturbers[i].position, transform.position);
            if (d < disturbRadius) best = Mathf.Max(best, 1f - d / disturbRadius);
        }
        return best;
    }
}
