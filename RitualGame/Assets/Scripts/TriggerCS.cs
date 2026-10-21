using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TriggerCS : MonoBehaviour
{

    public int listCount = 5;
    public TMP_Text listText;
    [SerializeField] private GameOverManager gameOver;

        private void Awake()
    {
        listCount = 5;
        listText.text = listCount.ToString();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Item"))
        {
            Destroy(other.gameObject);
            listCount = Mathf.Max(0, listCount - 1);
            listText.text = listCount.ToString();

            if (gameOver != null)
            {
                if (listCount == 0) gameOver.TriggerWin();
                else gameOver.OfferingPlaced();
            }
        }
    }
}
