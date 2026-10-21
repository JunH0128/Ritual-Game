using System.Collections;
using UnityEngine;
using BreadcrumbAi;

public class MonsterCatch : MonoBehaviour
{
    [SerializeField] private float catchDistance = 1.3f;
    [SerializeField] private float maxHeightDifference = 2f;

    [Header("Ritual Reactions")]
    [SerializeField] private float writheAngle = 10f;
    [SerializeField] private float writheOffset = 0.06f;

    [Header("Body")]
    [SerializeField] private Transform head;

    // Where the camera looks when the monster catches you / dies on the altar
    public Vector3 HeadPosition
    {
        get
        {
            if (head != null) return head.position;
            Bounds bounds = BodyBounds(transform);
            return new Vector3(bounds.center.x, bounds.max.y - 0.3f, bounds.center.z);
        }
    }

    private Transform player;
    private GameOverManager gameOver;
    private bool hasCaught = false;
    private Coroutine stunRoutine;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;

        gameOver = FindObjectOfType<GameOverManager>();
    }

    private void Update()
    {
        if (hasCaught || player == null) return;

        
        Vector3 offset = player.position - transform.position;
        float heightDifference = Mathf.Abs(offset.y);
        offset.y = 0f;

        if (heightDifference < maxHeightDifference && offset.magnitude < catchDistance)
        {
            Catch();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasCaught && collision.collider.CompareTag("Player"))
        {
            Catch();
        }
    }

    private void Catch()
    {
        hasCaught = true;
        StopMoving();

        MonsterVoice voice = GetComponent<MonsterVoice>();
        if (voice != null) voice.Silence();

        if (gameOver != null)
        {
            gameOver.TriggerLose(transform);
        }
    }

    // rhe monster freezes for a moment
    public void Hurt(float stunDuration)
    {
        if (hasCaught) return;

        MonsterVoice voice = GetComponent<MonsterVoice>();
        if (voice != null) voice.Hurt();

        if (stunRoutine != null) StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(Stun(stunDuration));
    }

    private IEnumerator Stun(float duration)
    {
        SetAiEnabled(false);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic) rb.velocity = Vector3.zero;

        Quaternion startRotation = transform.rotation;
        float t = 0f;
        while (t < duration && !hasCaught)
        {
            t += Time.deltaTime;
            // shake
            Writhe(startRotation, 1f - t / duration);
            yield return null;
        }

        transform.rotation = startRotation;
        if (!hasCaught) SetAiEnabled(true);
        stunRoutine = null;
    }

    // put the monster on the altar facing the player
    public void PlaceOnAltar(Vector3 altarTop, Vector3 lookAt)
    {
        hasCaught = true;
        if (stunRoutine != null) StopCoroutine(stunRoutine);
        StopMoving();

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        float feetOffset = transform.position.y - BodyBounds(transform).min.y;
        transform.position = altarTop + Vector3.up * feetOffset;

        Vector3 facing = lookAt - transform.position;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(facing);

        //death roar 
        MonsterVoice voice = GetComponent<MonsterVoice>();
        if (voice != null) voice.Die();
    }

    // sink through ground 
    public void Banish(float writheDuration, float sinkDuration)
    {
        hasCaught = true;
        StopMoving();
        StartCoroutine(WritheAndSink(writheDuration, sinkDuration));
    }

    private IEnumerator WritheAndSink(float writheDuration, float sinkDuration)
    {
        Quaternion startRotation = transform.rotation;
        Vector3 startPosition = transform.position;

        float t = 0f;
        while (t < writheDuration)
        {
            t += Time.unscaledDeltaTime;
            Writhe(startRotation, 1f);
            transform.position = startPosition + Random.insideUnitSphere * writheOffset;
            yield return null;
        }
        transform.rotation = startRotation;

        // sink until the top of the body 
        float depth = BodyBounds(transform).max.y + 0.2f;

        t = 0f;
        while (t < sinkDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / sinkDuration);
            Writhe(startRotation, 1f - k);
            transform.position = startPosition + Vector3.down * depth * k;
            yield return null;
        }

        Destroy(gameObject);
    }

    // Bounds of every visible part of the monster 
    public static Bounds BodyBounds(Transform monster)
    {
        Renderer[] parts = monster.GetComponentsInChildren<Renderer>();
        if (parts.Length == 0) return new Bounds(monster.position, Vector3.one);

        Bounds bounds = parts[0].bounds;
        for (int i = 1; i < parts.Length; i++)
        {
            bounds.Encapsulate(parts[i].bounds);
        }
        return bounds;
    }

    private void Writhe(Quaternion baseRotation, float strength)
    {
        Vector3 jitter = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * writheAngle * strength;
        transform.rotation = baseRotation * Quaternion.Euler(jitter);
    }

    private void StopMoving()
    {
        SetAiEnabled(false);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    private void SetAiEnabled(bool on)
    {
        // only the Breadcrumb AI voice, footsteps and disturbance keep running
        Ai ai = GetComponent<Ai>();
        if (ai != null) ai.enabled = on;
    }
}
