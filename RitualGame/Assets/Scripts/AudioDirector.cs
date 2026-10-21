using System.Collections;
using UnityEngine;
using BreadcrumbAi;

// Background music that escalates with the ritual, a drone that swells as the monster gets close,
// and stingers for the big moments. Lives on the GameManager.
public class AudioDirector : MonoBehaviour
{
    [Header("Music (escalates as offerings are placed)")]
    [SerializeField] private AudioClip[] musicTracks;              
    [SerializeField] private int[] offeringsToSwitch = { 0, 2, 4 };  
    [SerializeField] private float musicVolume = 0.45f;
    [SerializeField] private float crossfadeTime = 4f;

    [Header("Monster")]
    [SerializeField] private AudioClip monsterAwakeSting;        
    [SerializeField] private float monsterAwakeVolume = 1f;
    [SerializeField] private AudioClip dangerDrone;              
    [SerializeField] private float droneMaxVolume = 0.6f;
    [SerializeField] private float droneRange = 22f;
    [SerializeField] private AudioClip[] spottedStings;          
    [SerializeField] private float spottedVolume = 0.85f;
    [SerializeField] private float spottedCooldown = 20f;
    [SerializeField] private float unseenTimeBeforeSting = 6f;   

    [Header("Chase")]
    [SerializeField] private AudioClip chaseMusic;
    [SerializeField] private float chaseVolume = 0.75f;
    [SerializeField] private float chaseFadeIn = 0.5f;
    [SerializeField] private float chaseFadeOut = 3f;
    [SerializeField] private float chaseLinger = 5f;               // keeps going this long after it loses you
    [SerializeField] private float musicDuckDuringChase = 0.15f;   // exploration music level under the chase

    [Header("Ritual")]
    [SerializeField] private TriggerCS ritualTrigger;
    [SerializeField] private int totalOfferings = 5;
    [SerializeField] private AudioClip[] offeringStings;         
    [SerializeField] private float offeringVolume = 0.8f;
    [SerializeField] private AudioClip ritualCompleteSting;
    [SerializeField] private float ritualCompleteVolume = 1f;

    [Header("Ambient Scares")]
    [SerializeField] private AudioClip[] ambientStings;
    [SerializeField] private Vector2 ambientInterval = new Vector2(35f, 80f);
    [SerializeField] private float ambientVolume = 0.6f;
    [SerializeField] private Vector2 ambientDistance = new Vector2(5f, 10f);

    private AudioSource musicA, musicB, drone, stingSource, chase;
    private float baseA, baseB;            // music volumes before the chase duck is applied
    private float musicDuck = 1f;
    private float droneLevel;
    private bool chasing;
    private float lastChasing = -999f;
    private bool usingA = true;
    private int currentTrack = -1;
    private int lastPlaced;
    private bool gameOver;

    private Transform player;
    private Ai monster;
    private bool monsterAnnounced;
    private float lastSeenPlayer = -999f;
    private float lastSpottedSting = -999f;
    private float nextAmbient;
    private float nextMonsterSearch;

    private void Awake()
    {
        musicA = MakeSource(true);
        musicB = MakeSource(true);
        drone = MakeSource(true);
        stingSource = MakeSource(false);
        chase = MakeSource(true);
        nextAmbient = Time.time + Random.Range(ambientInterval.x, ambientInterval.y);
    }

    private AudioSource MakeSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = 0f;
        return source;
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;

        lastPlaced = PlacedOfferings();
        UpdateMusicForProgress();

        if (dangerDrone != null)
        {
            drone.clip = dangerDrone;
            drone.Play();
        }
    }

    private void Update()
    {
        if (gameOver) return;

        
        int placed = PlacedOfferings();
        if (placed > lastPlaced)
        {
            
            if (placed < totalOfferings) PlaySting(Pick(offeringStings, placed - 1), offeringVolume);
            lastPlaced = placed;
            UpdateMusicForProgress();
        }

       
        if (monster == null && Time.time > nextMonsterSearch)
        {
            nextMonsterSearch = Time.time + 0.25f;
            MonsterCatch found = FindObjectOfType<MonsterCatch>();
            if (found != null)
            {
                monster = found.GetComponent<Ai>();
                if (!monsterAnnounced)
                {
                    monsterAnnounced = true;
                    PlaySting(monsterAwakeSting, monsterAwakeVolume);
                }
            }
        }

        float droneTarget = 0f;
        if (monster != null && player != null)
        {
            float distance = Vector3.Distance(monster.transform.position, player.position);
            float closeness = Mathf.Clamp01(1f - distance / droneRange);
            droneTarget = closeness * closeness * droneMaxVolume;

            bool seesPlayer = monster.visionState == Ai.VISION_STATE.CanSeePlayer;
            if (seesPlayer)
            {
                if (Time.time - lastSeenPlayer > unseenTimeBeforeSting && Time.time - lastSpottedSting > spottedCooldown)
                {
                    PlaySting(Pick(spottedStings, Random.Range(0, spottedStings.Length)), spottedVolume);
                    lastSpottedSting = Time.time;
                }
                lastSeenPlayer = Time.time;
                droneTarget = Mathf.Max(droneTarget, droneMaxVolume * 0.5f);   // being hunted
            }

            // chasing = it can see you, or it's on your breadcrumb trail
            if (monster.moveState == Ai.MOVEMENT_STATE.IsFollowingPlayer || monster.moveState == Ai.MOVEMENT_STATE.IsFollowingBreadcrumb)
            {
                lastChasing = Time.time;
            }
        }
        UpdateChase();
        droneLevel = Mathf.MoveTowards(droneLevel, droneTarget, Time.deltaTime * 0.5f);

        //  ambient scares
        if (Time.time > nextAmbient)
        {
            nextAmbient = Time.time + Random.Range(ambientInterval.x, ambientInterval.y);
            PlayBehindPlayer(Pick(ambientStings, Random.Range(0, ambientStings.Length)));
        }
    }

    private void UpdateChase()
    {
        bool shouldChase = chaseMusic != null && Time.time - lastChasing < chaseLinger;

        if (shouldChase && !chasing)
        {
            chasing = true;
            if (!chase.isPlaying)
            {
                // a fresh chase starts the track from the top
                chase.clip = chaseMusic;
                chase.volume = 0f;
                chase.Play();
            }
        }
        else if (!shouldChase && chasing)
        {
            chasing = false;
        }

        float chaseTarget = chasing ? chaseVolume : 0f;
        float chaseSpeed = chaseVolume / (chasing ? chaseFadeIn : chaseFadeOut);
        chase.volume = Mathf.MoveTowards(chase.volume, chaseTarget, chaseSpeed * Time.deltaTime);
        if (!chasing && chase.volume <= 0f && chase.isPlaying) chase.Stop();

        float duckTarget = chasing ? musicDuckDuringChase : 1f;
        musicDuck = Mathf.MoveTowards(musicDuck, duckTarget, Time.deltaTime / (chasing ? chaseFadeIn : chaseFadeOut));
    }

    // music + drone levels are applied here every frame so the chase duck sits on top of the crossfades
    private void LateUpdate()
    {
        musicA.volume = baseA * musicDuck;
        musicB.volume = baseB * musicDuck;
        drone.volume = droneLevel * Mathf.Lerp(0.3f, 1f, musicDuck);
    }

    private int PlacedOfferings()
    {
        if (ritualTrigger == null) return 0;
        return Mathf.Clamp(totalOfferings - ritualTrigger.listCount, 0, totalOfferings);
    }

    private void UpdateMusicForProgress()
    {
        int track = 0;
        for (int i = 0; i < offeringsToSwitch.Length && i < musicTracks.Length; i++)
        {
            if (lastPlaced >= offeringsToSwitch[i]) track = i;
        }
        if (track == currentTrack || musicTracks.Length == 0) return;

        currentTrack = track;
        AudioSource from = usingA ? musicA : musicB;
        AudioSource to = usingA ? musicB : musicA;
        usingA = !usingA;

        to.clip = musicTracks[track];
        if (to == musicA) baseA = 0f; else baseB = 0f;
        to.Play();
        StartCoroutine(FadeMusic(to == musicA, musicVolume, crossfadeTime));
        StartCoroutine(FadeMusic(from == musicA, 0f, crossfadeTime));
    }

    // called by GameOverManager
    public void OnGameOver(float fadeTime)
    {
        if (gameOver) return;
        gameOver = true;
        StopAllCoroutines();
        StartCoroutine(FadeMusic(true, 0f, fadeTime));
        StartCoroutine(FadeMusic(false, 0f, fadeTime));
        StartCoroutine(FadeDrone(fadeTime));
        StartCoroutine(Fade(chase, 0f, fadeTime, true));
    }

    // Instant dead silence 
    public void CutToSilence()
    {
        gameOver = true;
        StopAllCoroutines();
        baseA = baseB = droneLevel = 0f;
        foreach (AudioSource source in new[] { musicA, musicB, drone, stingSource, chase })
        {
            source.Stop();
            source.volume = 0f;
        }
        foreach (AudioSource source in FindObjectsOfType<AudioSource>())
        {
            if (source.gameObject.name == "AmbientScare") source.Stop();
        }
    }

    public void PlayRitualComplete()
    {
        PlaySting(ritualCompleteSting, ritualCompleteVolume);
    }

    // helper
    private void PlaySting(AudioClip clip, float volume)
    {
        if (clip != null) stingSource.PlayOneShot(clip, volume);
    }

    private void PlayBehindPlayer(AudioClip clip)
    {
        if (clip == null || player == null) return;

        
        Vector3 back = -player.forward;
        Vector3 side = player.right * Random.Range(-1f, 1f);
        Vector3 position = player.position + (back + side).normalized * Random.Range(ambientDistance.x, ambientDistance.y);

        GameObject temp = new GameObject("AmbientScare");
        temp.transform.position = position;
        AudioSource source = temp.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = ambientVolume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = 30f;
        source.Play();
        Destroy(temp, clip.length + 0.5f);
    }

    private static AudioClip Pick(AudioClip[] clips, int index)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Mathf.Clamp(index, 0, clips.Length - 1)];
    }

    // fades a music track's base volume (the chase duck is applied on top in LateUpdate)
    private IEnumerator FadeMusic(bool trackA, float target, float time)
    {
        float start = trackA ? baseA : baseB;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(start, target, t / time);
            if (trackA) baseA = v; else baseB = v;
            yield return null;
        }
        if (trackA) baseA = target; else baseB = target;
        if (target <= 0f) (trackA ? musicA : musicB).Stop();
    }

    private IEnumerator FadeDrone(float time)
    {
        float start = droneLevel;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            droneLevel = Mathf.Lerp(start, 0f, t / time);
            yield return null;
        }
        droneLevel = 0f;
        drone.Stop();
    }

    private IEnumerator Fade(AudioSource source, float target, float time, bool stopAtEnd = false)
    {
        float start = source.volume;
        float t = 0f;
        while (t < time)
        {
            // unscaled music
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        source.volume = target;
        if (stopAtEnd && target <= 0f) source.Stop();
    }
}
