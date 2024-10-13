using System;
using System.IO;
using BepInEx;
using MusicBoard;
using TMPro;
using UnityEngine;
using Utilla;
using UnityEngine.Audio;
using UnityEngine.Networking;

namespace MusicBoard
{
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private bool inRoom;
        private string[] audioFiles; // Array to store detected audio file paths
        private int currentTrackIndex = 0; // To keep track of the current audio file
        private AudioSource audioSource; // To play audio
        private TextMeshPro titleText; // Reference to the top title text component
        private TextMeshPro lengthText; // Reference to the bottom length text component

        void Start()
        {
            Utilla.Events.GameInitialized += OnGameInitialized;
        }

        void OnEnable()
        {
            HarmonyPatches.ApplyHarmonyPatches();
        }

        void OnDisable()
        {
            HarmonyPatches.RemoveHarmonyPatches();
        }

        void OnGameInitialized(object sender, EventArgs e)
        {
            // Load the asset bundle
            string bundlePath = @"C:\Users\uwusu\source\repos\MusicBoard\resources\musicboard";
            AssetBundle musicBoardBundle = AssetBundle.LoadFromFile(bundlePath);

            if (musicBoardBundle != null)
            {
                // Load the music board asset from the bundle
                GameObject musicBoard = musicBoardBundle.LoadAsset<GameObject>("musicboard");
                if (musicBoard != null)
                {
                    // Set position, rotation, and scale
                    Vector3 position = new Vector3(-69.2099f, 12.3715f, -81.9676f);
                    Quaternion rotation = Quaternion.Euler(0f, 196.1689f, 0f);
                    Vector3 scale = new Vector3(0.0781f, 0.63f, 1.1758f);

                    // Instantiate the music board at the specified position and rotation
                    GameObject instantiatedMusicBoard = Instantiate(musicBoard, position, rotation);
                    instantiatedMusicBoard.transform.localScale = scale;

                    // Get the TextMeshPro components for title (top) and length (bottom)
                    titleText = instantiatedMusicBoard.transform.Find("Title").GetComponent<TextMeshPro>();
                    if (titleText == null) Logger.LogError("Failed to find 'Title' text component.");

                    lengthText = instantiatedMusicBoard.transform.Find("length").GetComponent<TextMeshPro>();
                    if (lengthText == null) Logger.LogError("Failed to find 'length' text component.");

                    // Set up the audio source
                    audioSource = instantiatedMusicBoard.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;

                    // Detect audio files
                    DetectAudioFiles();

                    // Play the first track if available
                    if (audioFiles.Length > 0)
                    {
                        PlayTrack(currentTrackIndex);
                    }
                    else
                    {
                        Logger.LogError("No audio files found in the specified directory.");
                    }

                    // Set up buttons
                    GameObject nextButton = instantiatedMusicBoard.transform.Find("Next").gameObject;
                    GameObject backButton = instantiatedMusicBoard.transform.Find("Back").gameObject;
                    GameObject playButton = instantiatedMusicBoard.transform.Find("Play").gameObject;
                    GameObject pauseButton = instantiatedMusicBoard.transform.Find("Pause").gameObject;

                    if (nextButton != null && backButton != null && playButton != null && pauseButton != null)
                    {
                        nextButton.AddComponent<ButtonTrigger>().Initialize(this, "Next");
                        backButton.AddComponent<ButtonTrigger>().Initialize(this, "Back");
                        playButton.AddComponent<ButtonTrigger>().Initialize(this, "Play");
                        pauseButton.AddComponent<ButtonTrigger>().Initialize(this, "Pause");
                    }

                    Logger.LogInfo("MusicBoard instantiated successfully.");
                }
                else
                {
                    Logger.LogError("Failed to load musicboard from the asset bundle.");
                }
            }
            else
            {
                Logger.LogError("Failed to load the asset bundle from the specified path.");
            }

            // Load the MusicPlayer script (assumes it's a MonoBehaviour)
            GameObject musicPlayerObject = new GameObject("MusicPlayer");
            musicPlayerObject.AddComponent<MusicPlayer>();
        }

        private void DetectAudioFiles()
        {
            // Path to the folder where .mp3 files are located
            string audioFolderPath = @"C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag\BepInEx\plugins\MusicBoard";
            audioFiles = Directory.GetFiles(audioFolderPath, "*.mp3");

            if (audioFiles.Length == 0)
            {
                Logger.LogError("No .mp3 files found in the specified directory.");
            }
        }

        private void PlayTrack(int index)
        {
            if (index >= 0 && index < audioFiles.Length)
            {
                string filePath = audioFiles[index];
                AudioClip audioClip = LoadAudioClip(filePath);
                if (audioClip != null)
                {
                    audioSource.clip = audioClip;
                    audioSource.Play();

                    // Update text components
                    UpdateTextComponents();
                }
                else
                {
                    Logger.LogError("Failed to load audio clip from: " + filePath);
                }
            }
        }

        private AudioClip LoadAudioClip(string path)
        {
            // Load the audio clip from the file
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + path, AudioType.MPEG))
            {
                var asyncOperation = www.SendWebRequest();
                while (!asyncOperation.isDone) { } // Wait until the audio is loaded

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Logger.LogError("Error loading audio file: " + www.error);
                    return null;
                }
                else
                {
                    return DownloadHandlerAudioClip.GetContent(www);
                }
            }
        }

        private void UpdateTextComponents()
        {
            if (titleText != null)
                titleText.text = GetCurrentTrackTitle(); // Update the title (top) text

            // Initialize the length text with the total length of the track
            if (lengthText != null)
                lengthText.text = GetCurrentTrackLength();
        }

        private string GetCurrentTrackTitle()
        {
            return Path.GetFileNameWithoutExtension(audioFiles[currentTrackIndex]); // File name without extension
        }

        private string GetCurrentTrackLength()
        {
            if (audioSource.clip != null)
            {
                TimeSpan totalTime = TimeSpan.FromSeconds(audioSource.clip.length);
                return string.Format("{0:D2}:{1:D2}", totalTime.Minutes, totalTime.Seconds); // Total track length
            }
            return "00:00";
        }

        private string GetRemainingTrackTime()
        {
            if (audioSource.clip != null)
            {
                TimeSpan timeLeft = TimeSpan.FromSeconds(audioSource.clip.length - audioSource.time);
                return string.Format("{0:D2}:{1:D2}", timeLeft.Minutes, timeLeft.Seconds); // Remaining time (minutes:seconds)
            }
            return "00:00";
        }

        void Update()
        {
            // Update the remaining track time each frame while the track is playing
            if (audioSource.isPlaying && lengthText != null)
            {
                lengthText.text = GetRemainingTrackTime(); // Countdown in real-time
            }
        }

        // Method to play the next track
        public void NextTrack()
        {
            if (audioFiles.Length > 1) // Ensure there are multiple tracks to switch between
            {
                currentTrackIndex = (currentTrackIndex + 1) % audioFiles.Length; // Move to the next track, wrap to the first
                PlayTrack(currentTrackIndex);
            }
        }

        // Method to play the previous track
        public void PreviousTrack()
        {
            if (audioFiles.Length > 1) // Ensure there are multiple tracks to switch between
            {
                currentTrackIndex--;
                if (currentTrackIndex < 0) currentTrackIndex = audioFiles.Length - 1; // Wrap to the last track
                PlayTrack(currentTrackIndex);
            }
        }

        // Method to pause the current track
        public void PauseTrack()
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
                lengthText.text = GetRemainingTrackTime(); // Update length text to show remaining time
            }
        }

        // Method to resume the current track
        public void PlayTrack()
        {
            if (!audioSource.isPlaying)
            {
                audioSource.UnPause(); // Resume the audio
            }
        }

        [ModdedGamemodeJoin]
        public void OnJoin(string gamemode)
        {
            inRoom = true;
        }

        [ModdedGamemodeLeave]
        public void OnLeave(string gamemode)
        {
            inRoom = false;
        }
    }

    // A new component to handle button trigger detection
    public class ButtonTrigger : MonoBehaviour
    {
        private Plugin plugin;
        private string buttonType;

        public void Initialize(Plugin pluginInstance, string type)
        {
            plugin = pluginInstance;
            buttonType = type;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (buttonType == "Next")
            {
                plugin.NextTrack();
            }
            else if (buttonType == "Back")
            {
                plugin.PreviousTrack();
            }
            else if (buttonType == "Play")
            {
                plugin.PlayTrack();
            }
            else if (buttonType == "Pause")
            {
                plugin.PauseTrack();
            }
        }
    }
}
