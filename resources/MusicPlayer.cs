using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using TMPro; // Import TextMesh Pro namespace

public class MusicPlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public TextMeshPro titleText; // Change to TextMeshPro for 3D Text
    public TextMeshPro lengthText; // Change to TextMeshPro for 3D Text
    public GameObject nextButton; // Cube for next track
    public GameObject previousButton; // Cube for previous track

    private List<string> mp3Files;
    private int currentTrackIndex;

    void Start()
    {
        string directoryPath = @"C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag\BepInEx\plugins\MusicBoard";
        mp3Files = Directory.GetFiles(directoryPath, "*.mp3").ToList();

        currentTrackIndex = -1; // Start with no track selected

        if (mp3Files.Count > 0)
        {
            PlayNextTrack(); // Start playing the first track
        }
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            UpdateTrackLength();
        }
    }

    private void PlayTrack(int index)
    {
        if (index < 0 || index >= mp3Files.Count)
            return;

        if (File.Exists(mp3Files[index]))
        {
            currentTrackIndex = index;
            AudioClip clip = LoadAudioClip(mp3Files[index]);
            audioSource.clip = clip;
            audioSource.Play();
            titleText.text = Path.GetFileName(mp3Files[index]);
            UpdateTrackLength();
        }
    }

    public void PlayNextTrack()
    {
        PlayTrack((currentTrackIndex + 1) % mp3Files.Count);
    }

    public void PlayPreviousTrack()
    {
        PlayTrack((currentTrackIndex - 1 + mp3Files.Count) % mp3Files.Count);
    }

    private void UpdateTrackLength()
    {
        float remainingTime = audioSource.clip.length - audioSource.time;
        lengthText.text = "Length Left: " + Mathf.CeilToInt(remainingTime) + "s";
    }

    private AudioClip LoadAudioClip(string filePath)
    {
        var www = new WWW("file:///" + filePath);
        while (!www.isDone) { }
        return www.GetAudioClip();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == nextButton)
        {
            PlayNextTrack();
        }
        else if (other.gameObject == previousButton)
        {
            PlayPreviousTrack();
        }
    }
}
