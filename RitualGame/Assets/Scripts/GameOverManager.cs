using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject[] hideOnGameOver;

    [Header("Ritual")]
    [SerializeField] private TriggerCS ritualTrigger;
    [SerializeField] private int totalOfferings = 5;
    [SerializeField] private TimerCS monsterTimer;

    [Header("Lose Screen")]
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private CanvasGroup losePanel;
    [SerializeField] private TMP_Text progressText;

    [Header("Win Screen")]
    [SerializeField] private CanvasGroup winFadeGroup;
    [SerializeField] private CanvasGroup winPanel;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Color ritualGlowColor = new Color(1f, 0.8f, 0.3f);

    [Header("Timing")]
    [SerializeField] private float lookAtDuration = 0.3f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float panelFadeDuration = 0.4f;
    [SerializeField] private float winLookDuration = 0.5f;
    [SerializeField] private float ritualDuration = 1.5f;
    [SerializeField] private float winFadeDuration = 1.5f;
    [SerializeField] private float shakeStrength = 0.06f;
    [SerializeField] private float appearLookDuration = 0.35f;
    [SerializeField] private float sinkDuration = 1.5f;
    [SerializeField] private float hurtStunDuration = 1.5f;
    [SerializeField] private float caughtShakeStrength = 0.18f;
    [SerializeField] private float caughtShakeDuration = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip caughtSting;               
    [SerializeField] private AudioClip[] caughtImpactLayers;      
    [SerializeField] private float caughtImpactVolume = 1.3f;
    [SerializeField] private float caughtTailVolume = 0.7f;
    [FormerlySerializedAs("winSting")]
    [SerializeField] private AudioClip monsterDeathSound;
    [SerializeField] private AudioClip monsterHurtSound;
    [SerializeField] private AudioDirector audioDirector;

    private bool isGameOver = false;

    private void Awake()
    {
        Time.timeScale = 1f;
        SetGroup(fadeGroup, 0f, false);
        SetGroup(losePanel, 0f, false);
        SetGroup(winFadeGroup, 0f, false);
        SetGroup(winPanel, 0f, false);
    }

    public void TriggerLose(Transform monster)
    {
        if (isGameOver) return;
        isGameOver = true;

        // dead silence first, so the hit lands out of nothing
        if (audioDirector != null) audioDirector.CutToSilence();
        FreezePlayer();
        StartCoroutine(LoseSequence(monster));
    }

    public void TriggerWin()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (audioDirector != null) audioDirector.OnGameOver(2.5f);
        FreezePlayer();

        // No monster spawns when the ritual is done
        if (monsterTimer != null) monsterTimer.enabled = false;

        StartCoroutine(WinSequence());
    }

    private void FreezePlayer()
    {
        // Disables movement + mouse look
        foreach (MonoBehaviour behaviour in playerController.GetComponents<MonoBehaviour>())
        {
            behaviour.enabled = false;
        }

        Rigidbody rb = playerController.GetComponent<Rigidbody>();
        if (!rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        foreach (GameObject hud in hideOnGameOver)
        {
            if (hud != null) hud.SetActive(false);
        }
    }

    private IEnumerator LoseSequence(Transform monster)
    {
        // layered jumpscare hit + the long screech underneath
        if (audioSource != null)
        {
            if (caughtImpactLayers != null)
            {
                foreach (AudioClip layer in caughtImpactLayers)
                {
                    if (layer != null) audioSource.PlayOneShot(layer, caughtImpactVolume);
                }
            }
            if (caughtSting != null) audioSource.PlayOneShot(caughtSting, caughtTailVolume);
        }
        StartCoroutine(CameraJolt(caughtShakeStrength, caughtShakeDuration));

        // Face the monster's head
        yield return TurnToFace(monster.GetComponent<MonsterCatch>().HeadPosition, lookAtDuration);
        yield return FadeGroup(fadeGroup, fadeDuration, false);

        int placed = 0;
        if (ritualTrigger != null)
        {
            placed = Mathf.Clamp(totalOfferings - ritualTrigger.listCount, 0, totalOfferings);
        }
        progressText.text = string.Format("Ritual: {0}/{1} offerings", placed, totalOfferings);

        yield return ShowPanel(losePanel);
    }

    // Each offering placed hurts the monster
    public void OfferingPlaced()
    {
        if (isGameOver) return;

        MonsterCatch[] monsters = FindObjectsOfType<MonsterCatch>();
        if (monsters.Length == 0) return;

        PlaySound(monsterHurtSound);
        foreach (MonsterCatch monster in monsters)
        {
            monster.Hurt(hurtStunDuration);
        }
    }

    private IEnumerator WinSequence()
    {
        float runTime = Time.timeSinceLevelLoad;

        // Face the ritual table
        Renderer altar = ritualTrigger.GetComponent<Renderer>();
        yield return TurnToFace(altar.bounds.center, winLookDuration);

        // Drag the monster onto the altar
        MonsterCatch monster = null;
        foreach (MonsterCatch m in FindObjectsOfType<MonsterCatch>())
        {
            if (monster == null) monster = m;
            else Destroy(m.gameObject);
        }
        if (monster == null && monsterTimer != null && monsterTimer.Enemy != null)
        {
            monster = Instantiate(monsterTimer.Enemy).GetComponent<MonsterCatch>();
        }

        Material altarMaterial = altar.material;
        Color startColor = altarMaterial.color;
        Transform cam = playerCamera.transform;
        Vector3 camStart = cam.localPosition;

        if (monster != null)
        {
            Vector3 altarTop = new Vector3(altar.bounds.center.x, altar.bounds.min.y, altar.bounds.center.z);
            monster.PlaceOnAltar(altarTop, cam.position);
            PlaySound(monsterDeathSound);

            yield return TurnToFace(monster.HeadPosition, appearLookDuration);

            monster.Banish(ritualDuration, sinkDuration);
        }

       
        float ritualTime = ritualDuration + (monster != null ? sinkDuration : 0f);
        float t = 0f;
        while (t < ritualTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / ritualTime);

            // two bright pulses settling on gold
            float pulse = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f));
            altarMaterial.color = Color.Lerp(startColor, ritualGlowColor, k) * (1f + pulse * 0.6f);

            cam.localPosition = camStart + Random.insideUnitSphere * shakeStrength * (1f - k);
            yield return null;
        }
        altarMaterial.color = ritualGlowColor;
        cam.localPosition = camStart;

        if (audioDirector != null) audioDirector.PlayRitualComplete();
        yield return FadeGroup(winFadeGroup, winFadeDuration, false);

        int minutes = Mathf.FloorToInt(runTime / 60f);
        int seconds = Mathf.FloorToInt(runTime % 60f);
        timeText.text = string.Format("Time: {0}:{1:00}", minutes, seconds);

        yield return ShowPanel(winPanel);
    }

    // violent shake that decays fast, moves the camera (not its rotation, so it doesn't fight TurnToFace)
    private IEnumerator CameraJolt(float strength, float duration)
    {
        Transform cam = playerCamera.transform;
        Vector3 start = cam.localPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - t / duration;
            cam.localPosition = start + Random.insideUnitSphere * strength * k * k;
            yield return null;
        }
        cam.localPosition = start;
    }

    private IEnumerator TurnToFace(Vector3 target, float duration)
    {
        // Turn the player's body and camera together
        Transform body = playerController.transform;
        Transform cam = playerCamera.transform;
        Vector3 toTarget = target - cam.position;

        float startYaw = body.eulerAngles.y;
        float startPitch = cam.localEulerAngles.x;
        float endYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        float endPitch = -Mathf.Asin(Mathf.Clamp(toTarget.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            body.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(startYaw, endYaw, k), 0f);
            cam.localRotation = Quaternion.Euler(Mathf.LerpAngle(startPitch, endPitch, k), 0f, 0f);
            yield return null;
        }
    }

    private IEnumerator ShowPanel(CanvasGroup panel)
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        yield return FadeGroup(panel, panelFadeDuration, true);
    }

    private IEnumerator FadeGroup(CanvasGroup group, float duration, bool interactive)
    {
        group.blocksRaycasts = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }

        SetGroup(group, 1f, interactive);
    }

    private void SetGroup(CanvasGroup group, float alpha, bool interactive)
    {
        if (group == null) return;

        group.alpha = alpha;
        group.interactable = interactive;
        group.blocksRaycasts = alpha > 0f;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Quit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
