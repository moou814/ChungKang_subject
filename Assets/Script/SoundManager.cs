using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    AudioSource audioSource;
    [SerializeField] AudioClip[] SoundEffects;
    public void soundEffect(int soundNum)
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.clip = SoundEffects[soundNum];
        audioSource.Play();
    }
}
