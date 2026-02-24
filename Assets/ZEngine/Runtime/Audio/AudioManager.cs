using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZEngine.Audio
{
    /// <summary>
    /// Manages background music and sound effects.
    /// </summary>
    public class AudioManager : Core.MonoSingleton<AudioManager>
    {
        private AudioSource _musicSource;
        private readonly List<AudioSource> _soundPool = new List<AudioSource>();

        [SerializeField] private int _soundPoolSize = 10;
        [SerializeField] private float _musicVolume = 1f;
        [SerializeField] private float _soundVolume = 1f;

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                if (_musicSource != null)
                    _musicSource.volume = _musicVolume;
            }
        }

        public float SoundVolume
        {
            get => _soundVolume;
            set => _soundVolume = Mathf.Clamp01(value);
        }

        protected override void OnInit()
        {
            SetupMusicSource();
            SetupSoundPool();
            Debug.Log("[AudioManager] Initialized.");
        }

        private void SetupMusicSource()
        {
            var go = new GameObject("MusicSource");
            go.transform.SetParent(transform);
            _musicSource = go.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.volume = _musicVolume;
        }

        private void SetupSoundPool()
        {
            for (int i = 0; i < _soundPoolSize; i++)
            {
                var go = new GameObject($"SoundSource_{i}");
                go.transform.SetParent(transform);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _soundPool.Add(source);
            }
        }

        #region Music

        /// <summary>
        /// Play background music. Fades in if fadeInDuration > 0.
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true, float fadeInDuration = 0f)
        {
            if (clip == null) return;
            _musicSource.clip = clip;
            _musicSource.loop = loop;

            if (fadeInDuration > 0f)
            {
                _musicSource.volume = 0f;
                _musicSource.Play();
                StartCoroutine(FadeMusic(_musicVolume, fadeInDuration));
            }
            else
            {
                _musicSource.volume = _musicVolume;
                _musicSource.Play();
            }
            Event.EventManager.Instance.Dispatch(Event.EventIds.AudioPlayMusic);
        }

        /// <summary>
        /// Play background music from Resources folder.
        /// </summary>
        public void PlayMusic(string path, bool loop = true, float fadeInDuration = 0f)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] AudioClip not found at path: {path}");
                return;
            }
            PlayMusic(clip, loop, fadeInDuration);
        }

        /// <summary>
        /// Stop background music. Fades out if fadeOutDuration > 0.
        /// </summary>
        public void StopMusic(float fadeOutDuration = 0f)
        {
            if (fadeOutDuration > 0f)
            {
                StartCoroutine(FadeOutAndStop(fadeOutDuration));
            }
            else
            {
                _musicSource.Stop();
            }
            Event.EventManager.Instance.Dispatch(Event.EventIds.AudioStopMusic);
        }

        /// <summary>
        /// Pause background music.
        /// </summary>
        public void PauseMusic() => _musicSource.Pause();

        /// <summary>
        /// Resume background music.
        /// </summary>
        public void ResumeMusic() => _musicSource.UnPause();

        private IEnumerator FadeMusic(float targetVolume, float duration)
        {
            float elapsed = 0f;
            float startVolume = _musicSource.volume;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }
            _musicSource.volume = targetVolume;
        }

        private IEnumerator FadeOutAndStop(float duration)
        {
            yield return FadeMusic(0f, duration);
            _musicSource.Stop();
        }

        #endregion

        #region Sound Effects

        /// <summary>
        /// Play a one-shot sound effect.
        /// </summary>
        public void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            var source = GetAvailableSoundSource();
            if (source == null)
            {
                Debug.LogWarning("[AudioManager] No available sound sources in pool.");
                return;
            }
            source.volume = _soundVolume;
            source.PlayOneShot(clip);
            Event.EventManager.Instance.Dispatch(Event.EventIds.AudioPlaySound);
        }

        /// <summary>
        /// Play a one-shot sound effect from Resources folder.
        /// </summary>
        public void PlaySound(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] AudioClip not found at path: {path}");
                return;
            }
            PlaySound(clip);
        }

        /// <summary>
        /// Play a sound effect at a world position.
        /// </summary>
        public void PlaySoundAtPosition(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, _soundVolume);
        }

        private AudioSource GetAvailableSoundSource()
        {
            foreach (var source in _soundPool)
            {
                if (!source.isPlaying) return source;
            }
            // Expand pool if all sources are busy
            var go = new GameObject($"SoundSource_{_soundPool.Count}");
            go.transform.SetParent(transform);
            var newSource = go.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            _soundPool.Add(newSource);
            return newSource;
        }

        #endregion
    }
}
