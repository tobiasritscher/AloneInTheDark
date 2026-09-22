using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays the retro bleeps. Clips live in Assets/Resources/Audio and are looked up by file name,
/// so nothing has to be wired in the scene. A missing clip is silently skipped.
/// </summary>
public class GameAudio : MonoBehaviour
{
    static GameAudio instance;

    AudioSource sfx;
    AudioSource ambient;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    public static GameAudio Get()
    {
        if (instance == null)
        {
            var go = new GameObject("GameAudio");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameAudio>();
        }

        return instance;
    }

    void Awake()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        ambient = gameObject.AddComponent<AudioSource>();
        ambient.playOnAwake = false;
        ambient.loop = true;
        ambient.volume = 0.3f;
        ApplyMute();
    }

    public bool Muted
    {
        get => GameSave.Muted;
        set
        {
            GameSave.Muted = value;
            ApplyMute();
        }
    }

    public void Play(string name, float volume = 1f, float pitch = 1f)
    {
        var clip = Load(name);
        if (clip == null)
        {
            return;
        }

        sfx.pitch = pitch;
        sfx.PlayOneShot(clip, volume);
    }

    public void StartAmbient()
    {
        if (ambient.isPlaying)
        {
            return;
        }

        var clip = Load("ambient");
        if (clip == null)
        {
            return;
        }

        ambient.clip = clip;
        ambient.Play();
    }

    public void StopAmbient()
    {
        ambient.Stop();
    }

    void ApplyMute()
    {
        bool muted = GameSave.Muted;
        sfx.mute = muted;
        ambient.mute = muted;
    }

    AudioClip Load(string name)
    {
        if (clips.TryGetValue(name, out var clip))
        {
            return clip;
        }

        clip = Resources.Load<AudioClip>("Audio/" + name);
        clips[name] = clip;
        return clip;
    }
}
