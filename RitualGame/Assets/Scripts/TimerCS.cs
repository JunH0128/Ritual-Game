using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimerCS : MonoBehaviour
{
    public float timeRemaining = 5f;
    public TMP_Text timerText;

    public GameObject Enemy;
    public Transform spawnPoint;

    private bool enemySpawned = false;

    private void Awake()
    {
        UpdateTimerText();
    }

    private void Update()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            timeRemaining = Mathf.Max(0, timeRemaining);

            UpdateTimerText();
        }
        else if (!enemySpawned)
        {
            Instantiate(Enemy, spawnPoint.position, spawnPoint.rotation);
            enemySpawned = true;
        }
    }

    private void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
