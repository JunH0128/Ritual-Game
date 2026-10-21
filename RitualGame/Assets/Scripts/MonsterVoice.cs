using UnityEngine;
using BreadcrumbAi;

// The monster's 3D voice: low growls while it roams, nastier ones while it hunts you,
// a roar when it awakens, a pained roar each time an offering hurts it, and a death roar on the altar.
public class MonsterVoice : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip awakeRoar;
    [SerializeField] private AudioClip[] idleGrowls;       // roaming
    [SerializeField] private AudioClip[] chaseGrowls;      // hunting you
    [SerializeField] private AudioClip[] hurtRoars;        // offering was placed
    [SerializeField] private AudioClip[] deathRoars;       // all played together on the altar

    [Header("Timing")]
    [SerializeField] private Vector2 idleInterval = new Vector2(7f, 15f);
    [SerializeField] private Vector2 chaseInterval = new Vector2(3.5f, 7f);

    [Header("Sound")]
    [SerializeField] private AudioSource voice;            // growl
    [SerializeField] private AudioSource roar;             // big roars
    [SerializeField] private float idleVolume = 0.7f;
    [SerializeField] private float chaseVolume = 1f;
    [SerializeField] private float roarVolume = 1.2f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.82f, 0.98f);

    private Ai ai;
    private float nextSound;
    private bool silenced;
    private AudioClip lastClip;

    private void Start()
    {
        ai = GetComponent<Ai>();
        Play(roar, awakeRoar, roarVolume);
        nextSound = Time.time + Random.Range(idleInterval.x, idleInterval.y);
    }

    private void Update()
    {
        if (silenced || Time.time < nextSound) return;

        bool chasing = ai != null && (ai.moveState == Ai.MOVEMENT_STATE.IsFollowingPlayer || ai.moveState == Ai.MOVEMENT_STATE.IsFollowingBreadcrumb);
        if (chasing) Play(voice, PickNew(chaseGrowls), chaseVolume);
        else Play(voice, PickNew(idleGrowls), idleVolume);

        Vector2 interval = chasing ? chaseInterval : idleInterval;
        nextSound = Time.time + Random.Range(interval.x, interval.y);
    }

    // an offering hurts it
    public void Hurt()
    {
        if (silenced) return;
        voice.Stop();
        Play(roar, PickNew(hurtRoars), roarVolume);
        nextSound = Time.time + 4f;
    }

    // dying on the altar
    public void Die()
    {
        silenced = true;
        voice.Stop();
        roar.Stop();
        roar.pitch = Random.Range(pitchRange.x, pitchRange.y);
        foreach (AudioClip clip in deathRoars)
        {
            if (clip != null) roar.PlayOneShot(clip, roarVolume);
        }
    }

    // it caught the player
    public void Silence()
    {
        silenced = true;
        voice.Stop();
        roar.Stop();
    }

    private void Play(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null) return;
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(clip, volume);
    }

    private AudioClip PickNew(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clips.Length > 1 && clip == lastClip) clip = clips[(System.Array.IndexOf(clips, clip) + 1) % clips.Length];
        lastClip = clip;
        return clip;
    }
}
