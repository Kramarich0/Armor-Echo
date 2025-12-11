using UnityEngine;

[RequireComponent(typeof(Light))]
public class HorrorFlickerLamp : MonoBehaviour
{
    public Light lamp;
    public float minIntensity = 0f;       
    public float maxIntensity = 5f;        
    public float minDelay = 0.02f;       
    public float maxDelay = 0.2f;          
    public float occasionalOffChance = 0.1f; 

    private float nextFlicker = 0f;

    void Update()
    {
        if (Time.time >= nextFlicker)
        {
            if (Random.value < occasionalOffChance)
            {
                lamp.intensity = 0f; 
            }
            else
            {
                lamp.intensity = Random.Range(minIntensity, maxIntensity); 
            }

            nextFlicker = Time.time + Random.Range(minDelay, maxDelay);
        }
    }
}
