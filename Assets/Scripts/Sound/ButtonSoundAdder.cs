using UnityEngine;
using UnityEngine.UI;

public class ButtonSoundAdder : MonoBehaviour
{
    public AudioClip btnClickSound = null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Button btn = gameObject.GetComponent<Button>();
        SoundManager soundManager = SoundManager.Instance;
        if (btn != null && soundManager != null)
        {
            if (btnClickSound == null)
                btnClickSound = soundManager.Data.ingameCctvChange;
            btn.onClick.AddListener(()=> SoundManager.Instance.PlayGlobalSFX(btnClickSound));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
