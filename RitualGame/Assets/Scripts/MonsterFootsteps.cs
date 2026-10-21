using UnityEngine;

// Heavy, wet, limping footsteps for the monster. Each step layers a weighty step with a wet squelch,
// on a dragging uneven rhythm. The AudioSource is routed through the "MonsterFootsteps" mixer group
// (pitch shift, low boost, distortion, chorus, echo, lowpass, cathedral reverb).
public class MonsterFootsteps : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip[] stoneSteps;      // ground floor weight
    [SerializeField] private AudioClip[] woodSteps;       // stairs / upper floor weight
    [SerializeField] private AudioClip[] runSteps;        // weight while chasing
    [SerializeField] private AudioClip[] wetLayer;        // squelch on step
    [SerializeField] private float woodAboveHeight = 0.3f;

    [Header("Gait")]
    [SerializeField] private float longStride = 1.9f;     // limping
    [SerializeField] private float shortStride = 1.1f;    // dragged one
    [SerializeField] private float strideJitter = 0.15f;
    [SerializeField] private float runSpeed = 3.5f;       // chasing
    [SerializeField] private float minMoveSpeed = 0.3f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float stepVolume = 1f;
    [SerializeField] private float wetVolume = 0.45f;
    [SerializeField] private float accentVolume = 1.25f;   
    [SerializeField] private Vector2 pitchRange = new Vector2(0.85f, 0.95f);

    private Vector3 lastPosition;
    private float smoothSpeed;
    private float distanceSinceStep;
    private bool longNext = true;
    private float nextStride;
    private AudioClip lastWeight;

    private void Start()
    {
        lastPosition = transform.position;
        nextStride = Stride();
    }

    private void Update()
    {
        // the AI moves with MovePosition
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        delta.y = 0f;
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        // physics steps smooth it
        smoothSpeed = Mathf.Lerp(smoothSpeed, speed, 1f - Mathf.Exp(-Time.deltaTime * 4f));

        RaycastHit ground;
        if (smoothSpeed < minMoveSpeed || !CheckGround(out ground)) return;

        distanceSinceStep += delta.magnitude;
        if (distanceSinceStep < nextStride) return;

        distanceSinceStep = 0f;
        bool running = smoothSpeed > runSpeed;
        bool accent = longNext;   //long stride 
        longNext = !longNext;
        nextStride = Stride() * (running ? 1.15f : 1f);

        AudioClip[] weightSet = running ? runSteps : (ground.point.y > woodAboveHeight ? woodSteps : stoneSteps);
        PlayStep(weightSet, wetLayer, accent);
    }

    private float Stride()
    {
        float stride = longNext ? longStride : shortStride;
        return stride * (1f + Random.Range(-strideJitter, strideJitter));
    }

    private bool CheckGround(out RaycastHit hit)
    {
        Collider body = GetComponent<Collider>();
        float reach = body != null ? body.bounds.extents.y + 0.3f : 1.6f;
        // ignore the monsters layer 
        int mask = ~(1 << gameObject.layer);
        return Physics.Raycast(transform.position, Vector3.down, out hit, reach, mask, QueryTriggerInteraction.Ignore);
    }

    private void PlayStep(AudioClip[] weight, AudioClip[] wet, bool accent)
    {
        if (audioSource == null) return;

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);

        AudioClip clip = Pick(weight);
        if (clip == lastWeight && weight != null && weight.Length > 1) clip = Pick(weight);
        lastWeight = clip;
        if (clip != null) audioSource.PlayOneShot(clip, stepVolume * (accent ? accentVolume : 0.8f));

        AudioClip squelch = Pick(wet);
        if (squelch != null) audioSource.PlayOneShot(squelch, wetVolume);
    }

    private static AudioClip Pick(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}
