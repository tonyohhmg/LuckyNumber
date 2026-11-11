using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource companionAudioSource; // <-- Dedicated audio source for companion whispers

    [Header("Audio Clips")]
    [SerializeField] private AudioClip ambientMusicClip;
    [SerializeField] private AudioClip absorbItemClip;
    [SerializeField] private AudioClip overloadClip;
    [SerializeField] private AudioClip portalTriggerClip;
    [SerializeField] private AudioClip portalSpawnClip;
    [SerializeField] private AudioClip orbShatterClip;
    [SerializeField] private AudioClip companionWhisperClip;
    [SerializeField] private AudioClip itemRelocateClip; // <-- NEW: Audio clip for the item shifting/disappearing effect
    
    [Header("UI Audio Clips")]
    [SerializeField] private AudioClip buttonHoverClip;

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

    public void PlayButtonHoverSound()
    {
        if (sfxAudioSource != null && buttonHoverClip != null)
        {
            sfxAudioSource.PlayOneShot(buttonHoverClip);
        }
    }

    public void PlayOrbShatterSound()
    {
        if (sfxAudioSource != null && orbShatterClip != null)
        {
            sfxAudioSource.PlayOneShot(orbShatterClip);
        }
    }

    // --- NEW METHOD ---
    public void PlayItemRelocateSound()
    {
        if (sfxAudioSource != null && itemRelocateClip != null)
        {
            sfxAudioSource.PlayOneShot(itemRelocateClip);
        }
    }

    // --- COMPANION AUDIO METHODS (Independent Audio Source & Volume Control) ---

    public void PlayCompanionWhisperLoop()
    {
        if (companionAudioSource != null && companionWhisperClip != null)
        {
            companionAudioSource.clip = companionWhisperClip;
            companionAudioSource.loop = true;
            if (!companionAudioSource.isPlaying)
            {
                companionAudioSource.Play();
            }
        }
    }

    public void StopCompanionWhisperLoop()
    {
        if (companionAudioSource != null && companionAudioSource.isPlaying)
        {
            companionAudioSource.Stop();
        }
    }

    public void SetCompanionVolume(float volume)
    {
        if (companionAudioSource != null)
        {
            companionAudioSource.volume = Mathf.Clamp01(volume);
        }
    }
}