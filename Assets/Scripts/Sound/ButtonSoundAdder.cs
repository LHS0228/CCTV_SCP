using UnityEngine;
using UnityEngine.UI;

public class ButtonSoundAdder : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Button btn = gameObject.GetComponent<Button>();
        SoundManager soundManager = SoundManager.Instance;
        if (btn != null && soundManager != null)
        {
            AudioClip audioClip = soundManager.Data.systemUiStartButtonClick;
            btn.onClick.AddListener(()=> SoundManager.Instance.PlayGlobalSFX(audioClip));
        }
        Destroy(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
