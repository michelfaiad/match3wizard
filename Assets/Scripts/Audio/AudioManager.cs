using UnityEngine;

namespace Match3Wizard
{
    /// <summary>
    /// Simple AudioManager: one BGM track, element SFX banks, UI sounds.
    /// Replace AudioSource calls with FMOD if richer audio is needed.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("BGM")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioClip   bgmMain;

        [Header("Match SFX (index = ElementType)")]
        [SerializeField] private AudioClip[] matchSFX = new AudioClip[5];

        [Header("Spell SFX (index = ElementType)")]
        [SerializeField] private AudioClip[] spellSFX = new AudioClip[5];

        [Header("UI SFX")]
        [SerializeField] private AudioClip uiButton;
        [SerializeField] private AudioClip uiGalleryReveal;
        [SerializeField] private AudioClip uiResultJingle;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.6f;

        private AudioSource _sfxSource;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop   = true;
            bgmSource.volume = bgmVolume;
        }

        private void Start() => PlayBGM();

        // ── Public API ────────────────────────────────────────────────────────

        public void PlayMatchSFX(ElementType e)  => PlayClip(matchSFX[(int)e]);
        public void PlaySpellSFX(ElementType e)  => PlayClip(spellSFX[(int)e]);
        public void PlayButtonSFX()               => PlayClip(uiButton);
        public void PlayGalleryRevealSFX()        => PlayClip(uiGalleryReveal);
        public void PlayResultJingle()            => PlayClip(uiResultJingle);

        public void SetSFXVolume(float v)  { sfxVolume = v; }
        public void SetBGMVolume(float v)  { bgmVolume = v; if (bgmSource) bgmSource.volume = v; }

        public void PauseBGM()  { bgmSource?.Pause(); }
        public void ResumeBGM() { bgmSource?.UnPause(); }

        // ── Private ───────────────────────────────────────────────────────────

        private void PlayBGM()
        {
            if (bgmSource == null || bgmMain == null) return;
            bgmSource.clip   = bgmMain;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
}
