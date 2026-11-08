using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip ambientMusicClip;
    [SerializeField] private AudioClip absorbItemClip;
    [SerializeField] private AudioClip overloadClip;
    [SerializeField] private AudioClip portalTriggerClip;
    [SerializeField] private AudioClip portalSpawnClip;
    
    [Header("UI Audio Clips")] // <-- NEW
    [SerializeField] private AudioClip buttonHoverClip; // <-- NEW

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

    private void Start()
    {
        PlayAmbientMusic();
    }

    public void PlayAmbientMusic()
    {
        if (ambientAudioSource != null && ambientMusicClip != null)
        {
            ambientAudioSource.clip = ambientMusicClip;
            ambientAudioSource.loop = true;
            if (!ambientAudioSource.isPlaying)
            {
                ambientAudioSource.Play();
            }
        }
    }

    public void PlayAbsorbSound()
    {
        if (sfxAudioSource != null && absorbItemClip != null)
        {
            sfxAudioSource.PlayOneShot(absorbItemClip);
        }
    }

    public void PlayOverloadSound()
    {
        if (sfxAudioSource != null && overloadClip != null)
        {
            sfxAudioSource.PlayOneShot(overloadClip);
        }
    }

    public void PlayPortalTriggerSound()
    {
        if (sfxAudioSource != null && portalTriggerClip != null)
        {
            sfxAudioSource.PlayOneShot(portalTriggerClip);
        }
    }

    public void PlayPortalSpawnSound()
    {
        if (sfxAudioSource != null && portalSpawnClip != null)
        {
            sfxAudioSource.PlayOneShot(portalSpawnClip);
        }
    }

    // --- NEW METHOD ---
    public void PlayButtonHoverSound()
    {
        if (sfxAudioSource != null && buttonHoverClip != null)
        {
            sfxAudioSource.PlayOneShot(buttonHoverClip);
        }
    }
}