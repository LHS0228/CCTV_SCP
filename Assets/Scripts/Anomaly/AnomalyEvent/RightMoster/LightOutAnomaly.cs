using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Anomalies/Event/RightRoom/Anomaly_LightOut")]
public class LightOutAnomaly : BasicEventAnomaly
{
    
    private GameObject CrossingGats;
    private float[] lights_Intensity;

    public void SetCrossingGats(GameObject crossingGats)
    {
        CrossingGats = crossingGats;
    }

    public override void Clear()
    {
        Clean();
    }

    public override EventType Execute()
    {
        CrossingGats.GetComponent<CrossingGats>().mainLight.color = Color.red;

        CrossingGats.GetComponent<Animator>().SetBool("isShotDown", true);
        lights_Intensity = new float[GameManager.Instance.lights.Length];

        for (int i = 0; i < GameManager.Instance.lights.Length; i++)
        {
            lights_Intensity[i] = GameManager.Instance.lights[i].intensity;
            GameManager.Instance.lights[i].intensity = 0;
        }
        return eventType;
    }

    public override void Fail()
    {
        Clean();
    }

    private void Clean()
    {
        CrossingGats.GetComponent<CrossingGats>().mainLight.color = Color.white;

        for (int i = 0; i < GameManager.Instance.lights.Length; i++)
        {
            GameManager.Instance.lights[i].intensity = lights_Intensity[i];
        }
    }
}
