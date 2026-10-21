using UnityEngine;

// candle lights near it stutter 
public class MonsterLightDisturbance : MonoBehaviour
{
    private void OnEnable()
    {
        FlickerLight.AddDisturber(transform);
    }

    private void OnDisable()
    {
        FlickerLight.RemoveDisturber(transform);
    }
}
