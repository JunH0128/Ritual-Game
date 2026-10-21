using UnityEngine;

[System.Serializable]
public class FootstepSet
{
    public AudioClip[] walk;
    public AudioClip[] run;
    public AudioClip[] land;
}

public class PlayerFootsteps : MonoBehaviour
{
    [Header("Surfaces")]
    [SerializeField] private FootstepSet stoneFloor;   // ground floor
    [SerializeField] private FootstepSet woodFloor;    // stairs
    [SerializeField] private float woodAboveHeight = 0.3f;

    [Header("Timing")]
    [SerializeField] private float walkStepDistance = 2f;
    [SerializeField] private float runStepDistance = 2.3f;
    [SerializeField] private float runSpeedThreshold = 5f;
    [SerializeField] private float minMoveSpeed = 0.5f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float walkVolume = 0.5f;
    [SerializeField] private float runVolume = 0.8f;
    [SerializeField] private float landVolume = 0.9f;
    [SerializeField] private float minLandSpeed = 3f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private float distanceSinceStep;
    private bool wasGrounded = true;
    private float fallSpeed;
    private AudioClip lastClip;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        distanceSinceStep = walkStepDistance * 0.6f;
    }

    private void FixedUpdate()
    {
        RaycastHit ground;
        bool grounded = CheckGround(out ground);

        Vector3 velocity = rb.velocity;
        float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

        if (!grounded)
        {
            fallSpeed = Mathf.Max(fallSpeed, -velocity.y);
        }
        else if (!wasGrounded)
        {
            // Landing
            if (fallSpeed > minLandSpeed) Play(SurfaceFor(ground).land, landVolume);
            fallSpeed = 0f;
            distanceSinceStep = 0f;
        }
        wasGrounded = grounded;

        if (!grounded) return;

        if (speed < minMoveSpeed)
        {
           
            distanceSinceStep = walkStepDistance * 0.6f;
            return;
        }

        bool running = speed > runSpeedThreshold;
        distanceSinceStep += speed * Time.fixedDeltaTime;

        if (distanceSinceStep >= (running ? runStepDistance : walkStepDistance))
        {
            distanceSinceStep = 0f;
            FootstepSet surface = SurfaceFor(ground);
            if (running) Play(surface.run, runVolume);
            else Play(surface.walk, walkVolume);
        }
    }

    private bool CheckGround(out RaycastHit hit)
    {
        Vector3 origin = transform.position;
        float reach = 0.15f;
        if (capsule != null)
        {
            origin = transform.TransformPoint(capsule.center);
            reach += capsule.height * 0.5f * transform.lossyScale.y;
        }

        // ignore layers
        int mask = ~(1 << gameObject.layer);
        return Physics.Raycast(origin, Vector3.down, out hit, reach, mask, QueryTriggerInteraction.Ignore);
    }

    private FootstepSet SurfaceFor(RaycastHit ground)
    {
        // The ground floor is stone everything else is wood
        return ground.point.y > woodAboveHeight ? woodFloor : stoneFloor;
    }

    private void Play(AudioClip[] clips, float volume)
    {
        if (audioSource == null || clips == null || clips.Length == 0) return;

        // pick a random clip
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clips.Length > 1 && clip == lastClip)
        {
            clip = clips[(System.Array.IndexOf(clips, clip) + 1) % clips.Length];
        }
        lastClip = clip;

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, volume);
    }
}
