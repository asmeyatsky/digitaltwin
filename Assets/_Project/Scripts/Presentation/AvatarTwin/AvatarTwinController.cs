using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DigitalTwin.Presentation.AvatarTwin
{
    /// <summary>
    /// Main controller for the Avatar Twin system.
    /// Handles avatar loading, voice playback, lip sync, and LLM interaction.
    /// </summary>
    public class AvatarTwinController : MonoBehaviour
    {
        [Header("Service Configuration")]
        [SerializeField] private string avatarServiceUrl = "http://localhost:8002";
        [SerializeField] private string voiceServiceUrl = "http://localhost:8003";
        [SerializeField] private string aiTwinServiceUrl = "http://localhost:8080/api/aitwin";

        [Header("Avatar Components")]
        [SerializeField] private GameObject avatarContainer;
        [SerializeField] private SkinnedMeshRenderer faceMeshRenderer;
        [SerializeField] private AudioSource audioSource;

        [Header("Lip Sync Settings")]
        [SerializeField] private float lipSyncSmoothness = 10f;
        [SerializeField] private float lipSyncMultiplier = 1.5f;

        [Header("Blend Shape Indices")]
        [SerializeField] private int jawOpenIndex = 0;
        [SerializeField] private int mouthSmileIndex = 1;
        [SerializeField] private int mouthPuckerIndex = 2;

        // State
        private string _userId;
        private string _avatarId;
        private string _voiceId;
        private bool _isInitialized;
        private bool _isSpeaking;
        private Queue<VisemeData> _visemeQueue = new Queue<VisemeData>();
        private VisemeData _currentViseme;
        private float _visemeStartTime;

        // Events
        public event Action<string> OnAvatarLoaded;
        public event Action<string> OnVoiceCloned;
        public event Action<string> OnSpeechStarted;
        public event Action OnSpeechEnded;
        public event Action<string> OnError;

        // Blend shape targets
        private float _targetJawOpen;
        private float _targetMouthSmile;
        private float _targetMouthPucker;

        private void Update()
        {
            if (_isSpeaking)
            {
                UpdateLipSync();
            }

            // Smooth blend shape transitions
            SmoothBlendShapes();
        }

        #region Initialization

        /// <summary>
        /// Initialize the Avatar Twin with user photo and voice sample
        /// </summary>
        public async Task InitializeAvatarTwinAsync(string userId, byte[] userPhoto, byte[] voiceSample)
        {
            _userId = userId;

            try
            {
                // Step 1: Generate 3D avatar from photo
                Debug.Log("Generating avatar from photo...");
                _avatarId = await GenerateAvatarAsync(userPhoto);
                OnAvatarLoaded?.Invoke(_avatarId);

                // Step 2: Load the generated avatar
                Debug.Log("Loading avatar model...");
                await LoadAvatarModelAsync(_avatarId);

                // Step 3: Clone user's voice
                if (voiceSample != null && voiceSample.Length > 0)
                {
                    Debug.Log("Cloning user voice...");
                    _voiceId = await CloneVoiceAsync(voiceSample);
                    OnVoiceCloned?.Invoke(_voiceId);
                }

                _isInitialized = true;
                Debug.Log("Avatar Twin initialized successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize Avatar Twin: {ex.Message}");
                OnError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Initialize with existing avatar and voice IDs
        /// </summary>
        public async Task InitializeWithExistingAsync(string userId, string avatarId, string voiceId)
        {
            _userId = userId;
            _avatarId = avatarId;
            _voiceId = voiceId;

            await LoadAvatarModelAsync(avatarId);
            _isInitialized = true;
        }

        #endregion

        #region Avatar Generation

        private async Task<string> GenerateAvatarAsync(byte[] photoData)
        {
            var form = new WWWForm();
            form.AddBinaryData("file", photoData, "photo.jpg", "image/jpeg");
            form.AddField("user_id", _userId);
            form.AddField("avatar_style", "realistic");

            using var request = UnityWebRequest.Post($"{avatarServiceUrl}/avatar/generate", form);
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"Avatar generation failed: {request.error}");

            var response = JsonUtility.FromJson<AvatarGenerationResponse>(request.downloadHandler.text);
            return response.avatar_id;
        }

        private async Task LoadAvatarModelAsync(string avatarId)
        {
            // Download GLB file
            using var request = UnityWebRequest.Get($"{avatarServiceUrl}/avatar/{avatarId}/download");
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"Failed to download avatar: {request.error}");

            // Load GLB into scene (requires GLTFUtility or similar)
            var glbData = request.downloadHandler.data;
            LoadGLBModel(glbData);
        }

        private void LoadGLBModel(byte[] glbData)
        {
            // Note: This requires a GLB/GLTF importer like GLTFUtility
            // For now, we'll use a placeholder approach

            #if GLTF_UTILITY
            var model = GLTFUtility.Importer.LoadFromBytes(glbData);
            if (model != null)
            {
                model.transform.SetParent(avatarContainer.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                // Find the face mesh renderer
                faceMeshRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            }
            #else
            Debug.LogWarning("GLTFUtility not available. Using placeholder avatar.");
            // Use existing avatar in scene as placeholder
            #endif
        }

        #endregion

        #region Voice Cloning

        private async Task<string> CloneVoiceAsync(byte[] voiceSample)
        {
            var form = new WWWForm();
            form.AddBinaryData("files", voiceSample, "voice_sample.mp3", "audio/mpeg");
            form.AddField("user_id", _userId);
            form.AddField("voice_name", $"AvatarTwin_{_userId}");

            using var request = UnityWebRequest.Post($"{voiceServiceUrl}/voice/clone", form);
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"Voice cloning failed: {request.error}");

            var response = JsonUtility.FromJson<VoiceCloneResponse>(request.downloadHandler.text);
            return response.voice_id;
        }

        #endregion

        #region Conversation

        /// <summary>
        /// Send a message to the AI Twin and speak the response
        /// </summary>
        public async Task<string> SendMessageAsync(string message)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Avatar Twin not initialized");

            try
            {
                // Get AI response
                var aiResponse = await GetAIResponseAsync(message);

                // Speak the response with lip sync
                await SpeakWithLipSyncAsync(aiResponse);

                return aiResponse;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Message handling failed: {ex.Message}");
                OnError?.Invoke(ex.Message);
                throw;
            }
        }

        private async Task<string> GetAIResponseAsync(string message)
        {
            var requestBody = JsonUtility.ToJson(new AIMessageRequest
            {
                content = message,
                type = "user"
            });

            using var request = new UnityWebRequest($"{aiTwinServiceUrl}/message", "POST");
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(requestBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"AI request failed: {request.error}");

            var response = JsonUtility.FromJson<AIResponse>(request.downloadHandler.text);
            return response.content;
        }

        #endregion

        #region Text-to-Speech with Lip Sync

        /// <summary>
        /// Speak text with lip sync animation
        /// </summary>
        public async Task SpeakWithLipSyncAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            OnSpeechStarted?.Invoke(text);
            _isSpeaking = true;

            try
            {
                // Get TTS with viseme data
                var ttsResponse = await GetTTSWithVisemesAsync(text);

                // Load viseme queue
                _visemeQueue.Clear();
                foreach (var viseme in ttsResponse.visemes)
                {
                    _visemeQueue.Enqueue(viseme);
                }

                // Download and play audio
                var audioClip = await DownloadAudioAsync(ttsResponse.audio_url);
                if (audioClip != null)
                {
                    _visemeStartTime = Time.time;
                    audioSource.clip = audioClip;
                    audioSource.Play();

                    // Wait for audio to finish
                    while (audioSource.isPlaying)
                        await Task.Yield();
                }
            }
            finally
            {
                _isSpeaking = false;
                ResetBlendShapes();
                OnSpeechEnded?.Invoke();
            }
        }

        private async Task<TTSWithVisemesResponse> GetTTSWithVisemesAsync(string text)
        {
            var requestBody = JsonUtility.ToJson(new TTSRequest
            {
                text = text,
                user_id = _userId,
                voice_id = _voiceId
            });

            using var request = new UnityWebRequest($"{voiceServiceUrl}/tts/with-visemes", "POST");
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(requestBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"TTS request failed: {request.error}");

            return JsonUtility.FromJson<TTSWithVisemesResponse>(request.downloadHandler.text);
        }

        private async Task<AudioClip> DownloadAudioAsync(string audioUrl)
        {
            var fullUrl = audioUrl.StartsWith("http") ? audioUrl : $"{voiceServiceUrl}{audioUrl}";

            using var request = UnityWebRequestMultimedia.GetAudioClip(fullUrl, AudioType.MPEG);
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download audio: {request.error}");
                return null;
            }

            return DownloadHandlerAudioClip.GetContent(request);
        }

        #endregion

        #region Lip Sync Animation

        private void UpdateLipSync()
        {
            if (_visemeQueue.Count == 0 && _currentViseme == null)
            {
                _targetJawOpen = 0;
                _targetMouthSmile = 0;
                _targetMouthPucker = 0;
                return;
            }

            float currentTimeMs = (Time.time - _visemeStartTime) * 1000f;

            // Check if we need to advance to next viseme
            while (_visemeQueue.Count > 0)
            {
                var nextViseme = _visemeQueue.Peek();
                if (currentTimeMs >= nextViseme.start_time_ms)
                {
                    _currentViseme = _visemeQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }

            // Apply current viseme
            if (_currentViseme != null && currentTimeMs <= _currentViseme.end_time_ms)
            {
                _targetJawOpen = _currentViseme.blend_shapes.jawOpen * lipSyncMultiplier;
                _targetMouthSmile = _currentViseme.blend_shapes.mouthSmile * lipSyncMultiplier;
                _targetMouthPucker = _currentViseme.blend_shapes.mouthPucker * lipSyncMultiplier;
            }
            else if (_currentViseme != null && currentTimeMs > _currentViseme.end_time_ms)
            {
                _currentViseme = null;
                _targetJawOpen = 0;
                _targetMouthSmile = 0;
                _targetMouthPucker = 0;
            }
        }

        private void SmoothBlendShapes()
        {
            if (faceMeshRenderer == null) return;

            float smoothFactor = Time.deltaTime * lipSyncSmoothness;

            // Get current values
            float currentJaw = faceMeshRenderer.GetBlendShapeWeight(jawOpenIndex);
            float currentSmile = faceMeshRenderer.GetBlendShapeWeight(mouthSmileIndex);
            float currentPucker = faceMeshRenderer.GetBlendShapeWeight(mouthPuckerIndex);

            // Lerp towards targets
            faceMeshRenderer.SetBlendShapeWeight(jawOpenIndex,
                Mathf.Lerp(currentJaw, _targetJawOpen * 100f, smoothFactor));
            faceMeshRenderer.SetBlendShapeWeight(mouthSmileIndex,
                Mathf.Lerp(currentSmile, _targetMouthSmile * 100f, smoothFactor));
            faceMeshRenderer.SetBlendShapeWeight(mouthPuckerIndex,
                Mathf.Lerp(currentPucker, _targetMouthPucker * 100f, smoothFactor));
        }

        private void ResetBlendShapes()
        {
            _targetJawOpen = 0;
            _targetMouthSmile = 0;
            _targetMouthPucker = 0;
            _currentViseme = null;
            _visemeQueue.Clear();
        }

        #endregion

        #region Public Utilities

        /// <summary>
        /// Capture photo from webcam for avatar generation
        /// </summary>
        public async Task<byte[]> CapturePhotoAsync(WebCamTexture webcamTexture)
        {
            if (webcamTexture == null || !webcamTexture.isPlaying)
                throw new InvalidOperationException("Webcam not available");

            // Create texture from webcam
            var texture = new Texture2D(webcamTexture.width, webcamTexture.height);
            texture.SetPixels(webcamTexture.GetPixels());
            texture.Apply();

            // Encode to JPG
            var photoData = texture.EncodeToJPG(90);
            Destroy(texture);

            return photoData;
        }

        /// <summary>
        /// Record voice sample for cloning
        /// </summary>
        public IEnumerator RecordVoiceSample(float duration, Action<byte[]> onComplete)
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No microphone detected");
                onComplete?.Invoke(null);
                yield break;
            }

            var micDevice = Microphone.devices[0];
            var clip = Microphone.Start(micDevice, false, Mathf.CeilToInt(duration), 44100);

            yield return new WaitForSeconds(duration);

            Microphone.End(micDevice);

            // Convert to WAV bytes
            var wavData = ConvertToWav(clip);
            onComplete?.Invoke(wavData);
        }

        private byte[] ConvertToWav(AudioClip clip)
        {
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            var intData = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * 32767f);
            }

            var byteData = new byte[intData.Length * 2];
            Buffer.BlockCopy(intData, 0, byteData, 0, byteData.Length);

            // Create WAV header
            var wav = new List<byte>();

            // RIFF header
            wav.AddRange(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            wav.AddRange(BitConverter.GetBytes(36 + byteData.Length));
            wav.AddRange(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            wav.AddRange(System.Text.Encoding.ASCII.GetBytes("fmt "));
            wav.AddRange(BitConverter.GetBytes(16)); // chunk size
            wav.AddRange(BitConverter.GetBytes((short)1)); // audio format (PCM)
            wav.AddRange(BitConverter.GetBytes((short)clip.channels));
            wav.AddRange(BitConverter.GetBytes(clip.frequency));
            wav.AddRange(BitConverter.GetBytes(clip.frequency * clip.channels * 2)); // byte rate
            wav.AddRange(BitConverter.GetBytes((short)(clip.channels * 2))); // block align
            wav.AddRange(BitConverter.GetBytes((short)16)); // bits per sample

            // data chunk
            wav.AddRange(System.Text.Encoding.ASCII.GetBytes("data"));
            wav.AddRange(BitConverter.GetBytes(byteData.Length));
            wav.AddRange(byteData);

            return wav.ToArray();
        }

        public bool IsInitialized => _isInitialized;
        public bool IsSpeaking => _isSpeaking;
        public string AvatarId => _avatarId;
        public string VoiceId => _voiceId;

        #endregion
    }

    #region Data Models

    [Serializable]
    public class AvatarGenerationResponse
    {
        public string avatar_id;
        public string user_id;
        public string status;
        public string avatar_url;
        public string thumbnail_url;
    }

    [Serializable]
    public class VoiceCloneResponse
    {
        public string job_id;
        public string user_id;
        public string status;
        public string voice_id;
    }

    [Serializable]
    public class AIMessageRequest
    {
        public string content;
        public string type;
    }

    [Serializable]
    public class AIResponse
    {
        public string content;
        public string emotional_tone;
        public float confidence;
    }

    [Serializable]
    public class TTSRequest
    {
        public string text;
        public string user_id;
        public string voice_id;
    }

    [Serializable]
    public class TTSWithVisemesResponse
    {
        public string audio_url;
        public VisemeData[] visemes;
        public int duration_ms;
        public string text;
    }

    [Serializable]
    public class VisemeData
    {
        public string character;
        public int start_time_ms;
        public int end_time_ms;
        public string viseme_type;
        public BlendShapeData blend_shapes;
    }

    [Serializable]
    public class BlendShapeData
    {
        public float jawOpen;
        public float mouthSmile;
        public float mouthPucker;
    }

    #endregion
}
