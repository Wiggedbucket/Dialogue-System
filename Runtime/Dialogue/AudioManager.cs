using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public bool handleMusicQueue = false;
    private AudioSource musicSource;
    public List<AudioClip> musicQueue = new();

    private int currentTrackIndex = -1;
    public bool loop = true;
    public bool shuffle = false;

    public List<AudioSource> activeSources = new();
    private List<Coroutine> soundCoroutines = new();

    private void Start()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
    }

    public void ResetController()
    {
        loop = true;
        shuffle = false;

        StopAllSounds();
        musicSource.Stop();
    }

    private void Update()
    {
        if (handleMusicQueue)
            HandleMusicQueue();
    }

    #region Music Controls
    private void HandleMusicQueue()
    {
        // If music has stopped playing, play the next track
        if (musicSource.isPlaying || musicQueue.Count == 0)
            return;

        // If loop is enabled and we've reached the end of the queue, reset index
        if (loop)
            if (currentTrackIndex >= musicQueue.Count)
                currentTrackIndex = 0;

        PlayNextTrack();
    }

    public void SetMusicQueue(List<AudioClip> newQueue, bool _loop, bool _shuffle)
    {
        // Stop current music and set new queue
        musicSource.Stop();
        musicQueue.Clear();
        musicQueue.AddRange(newQueue);

        // Set loop and shuffle settings
        loop = _loop;
        shuffle = _shuffle;

        // Shuffle queue if needed
        if (shuffle)
            Shuffle(musicQueue);

        // Reset track index and start playing
        currentTrackIndex = -1;
        PlayNextTrack();
    }

    private void PlayNextTrack()
    {
        currentTrackIndex++;

        if (currentTrackIndex >= musicQueue.Count)
        {
            // End of queue
            Debug.Log("Music queue finished.");
            currentTrackIndex = -1;
            return;
        }

        AudioClip nextTrack = musicQueue[currentTrackIndex];
        if (nextTrack == null || nextTrack == null)
        {
            Debug.LogWarning("Missing AudioResource or AudioClip at index " + currentTrackIndex);
            PlayNextTrack(); // skip invalid entries
            return;
        }

        musicSource.clip = nextTrack;
        musicSource.Play();
        Debug.Log("Now playing: " + nextTrack.name);
    }

    public void MusicState(MusicCommand command)
    {
        switch (command)
        {
            case MusicCommand.Play:
                musicSource.Play();
                break;
            case MusicCommand.Pause:
                musicSource.Pause();
                break;
            case MusicCommand.Resume:
                musicSource.UnPause();
                break;
            case MusicCommand.Stop:
                musicSource.Stop();
                break;
        }
    }
    #endregion

    #region Sound Effects Controls
    public void PlayAllSounds(List<AudioClip> audioList)
    {
        foreach (AudioClip clip in audioList)
        {
            if (clip == null)
                continue;

            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.clip = clip;
            src.Play();
            activeSources.Add(src);

            soundCoroutines.Add(StartCoroutine(DestroyAfterPlaying(src)));
        }
    }

    private IEnumerator DestroyAfterPlaying(AudioSource src)
    {
        yield return new WaitForSeconds(src.clip.length);
        activeSources.Remove(src);
        Destroy(src);
    }

    public void StopAllSounds()
    {
        foreach (AudioSource src in activeSources)
        {
            if (src != null)
                Destroy(src);
        }
        foreach (Coroutine coroutine in soundCoroutines)
        {
            StopCoroutine(coroutine);
        }
        activeSources.Clear();
    }
    #endregion

    public void Shuffle<T>(IList<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = Random.Range(i, count);
            (ts[r], ts[i]) = (ts[i], ts[r]);
        }
    }
}

public enum MusicCommand
{
    Play,
    Pause,
    Resume,
    Stop,
}