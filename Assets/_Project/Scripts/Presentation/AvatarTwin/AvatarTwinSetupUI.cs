using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DigitalTwin.Presentation.AvatarTwin
{
    /// <summary>
    /// UI for setting up the Avatar Twin - captures photo and voice sample
    /// </summary>
    public class AvatarTwinSetupUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AvatarTwinController avatarController;

        [Header("Setup Panels")]
        [SerializeField] private GameObject setupPanel;
        [SerializeField] private GameObject photoCapturepanel;
        [SerializeField] private GameObject voiceRecordPanel;
        [SerializeField] private GameObject processingPanel;
        [SerializeField] private GameObject chatPanel;

        [Header("Photo Capture")]
        [SerializeField] private RawImage webcamPreview;
        [SerializeField] private Button capturePhotoButton;
        [SerializeField] private Button retakePhotoButton;
        [SerializeField] private RawImage capturedPhotoPreview;
        [SerializeField] private TMP_Text photoInstructions;

        [Header("Voice Recording")]
        [SerializeField] private Button startRecordingButton;
        [SerializeField] private Button stopRecordingButton;
        [SerializeField] private Slider recordingProgress;
        [SerializeField] private TMP_Text voiceInstructions;
        [SerializeField] private TMP_Text recordingTimeText;
        [SerializeField] private float voiceSampleDuration = 30f;

        [Header("Processing")]
        [SerializeField] private TMP_Text processingStatusText;
        [SerializeField] private Slider processingProgress;

        [Header("Chat Interface")]
        [SerializeField] private TMP_InputField messageInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button voiceInputButton;
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private Transform chatContent;
        [SerializeField] private GameObject userMessagePrefab;
        [SerializeField] private GameObject avatarMessagePrefab;

        private WebCamTexture _webcamTexture;
        private byte[] _capturedPhoto;
        private byte[] _voiceSample;
        private bool _isRecording;
        private float _recordingStartTime;
        private string _userId;

        private void Start()
        {
            _userId = SystemInfo.deviceUniqueIdentifier;

            SetupEventListeners();
            ShowPhotoCapture();
        }

        private void SetupEventListeners()
        {
            capturePhotoButton.onClick.AddListener(CapturePhoto);
            retakePhotoButton.onClick.AddListener(RetakePhoto);
            startRecordingButton.onClick.AddListener(StartVoiceRecording);
            stopRecordingButton.onClick.AddListener(StopVoiceRecording);
            sendButton.onClick.AddListener(SendMessage);
            voiceInputButton.onClick.AddListener(ToggleVoiceInput);

            messageInput.onSubmit.AddListener(_ => SendMessage());

            // Avatar controller events
            if (avatarController != null)
            {
                avatarController.OnAvatarLoaded += OnAvatarLoaded;
                avatarController.OnVoiceCloned += OnVoiceCloned;
                avatarController.OnSpeechStarted += OnSpeechStarted;
                avatarController.OnSpeechEnded += OnSpeechEnded;
                avatarController.OnError += OnError;
            }
        }

        private void Update()
        {
            if (_isRecording)
            {
                UpdateRecordingUI();
            }
        }

        #region Photo Capture

        private void ShowPhotoCapture()
        {
            setupPanel.SetActive(true);
            photoCapturepanel.SetActive(true);
            voiceRecordPanel.SetActive(false);
            processingPanel.SetActive(false);
            chatPanel.SetActive(false);

            capturedPhotoPreview.gameObject.SetActive(false);
            retakePhotoButton.gameObject.SetActive(false);

            photoInstructions.text = "Position your face in the center of the frame.\nMake sure you have good lighting.";

            StartWebcam();
        }

        private void StartWebcam()
        {
            if (WebCamTexture.devices.Length == 0)
            {
                photoInstructions.text = "No camera detected!";
                return;
            }

            _webcamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 640, 480, 30);
            webcamPreview.texture = _webcamTexture;
            _webcamTexture.Play();
        }

        private async void CapturePhoto()
        {
            if (_webcamTexture == null || !_webcamTexture.isPlaying)
                return;

            try
            {
                _capturedPhoto = await avatarController.CapturePhotoAsync(_webcamTexture);

                // Show preview
                var previewTexture = new Texture2D(2, 2);
                previewTexture.LoadImage(_capturedPhoto);
                capturedPhotoPreview.texture = previewTexture;
                capturedPhotoPreview.gameObject.SetActive(true);

                webcamPreview.gameObject.SetActive(false);
                capturePhotoButton.gameObject.SetActive(false);
                retakePhotoButton.gameObject.SetActive(true);

                photoInstructions.text = "Photo captured! Click 'Continue' to record your voice, or 'Retake' for a new photo.";

                // Add continue button or auto-advance
                StartCoroutine(AdvanceToVoiceRecordingAfterDelay(2f));
            }
            catch (Exception ex)
            {
                photoInstructions.text = $"Error capturing photo: {ex.Message}";
            }
        }

        private void RetakePhoto()
        {
            _capturedPhoto = null;
            capturedPhotoPreview.gameObject.SetActive(false);
            webcamPreview.gameObject.SetActive(true);
            capturePhotoButton.gameObject.SetActive(true);
            retakePhotoButton.gameObject.SetActive(false);

            photoInstructions.text = "Position your face in the center of the frame.";
        }

        private IEnumerator AdvanceToVoiceRecordingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowVoiceRecording();
        }

        #endregion

        #region Voice Recording

        private void ShowVoiceRecording()
        {
            // Stop webcam to save resources
            if (_webcamTexture != null)
            {
                _webcamTexture.Stop();
            }

            photoCapturepanel.SetActive(false);
            voiceRecordPanel.SetActive(true);

            stopRecordingButton.gameObject.SetActive(false);
            recordingProgress.value = 0;

            voiceInstructions.text = $"Record at least {voiceSampleDuration} seconds of your voice.\n" +
                                     "Speak naturally - read a passage or talk about your day.";
        }

        private void StartVoiceRecording()
        {
            if (Microphone.devices.Length == 0)
            {
                voiceInstructions.text = "No microphone detected!";
                return;
            }

            _isRecording = true;
            _recordingStartTime = Time.time;

            startRecordingButton.gameObject.SetActive(false);
            stopRecordingButton.gameObject.SetActive(true);

            voiceInstructions.text = "Recording... Speak clearly and naturally.";

            StartCoroutine(avatarController.RecordVoiceSample(voiceSampleDuration, OnVoiceRecordingComplete));
        }

        private void StopVoiceRecording()
        {
            _isRecording = false;
            Microphone.End(null);
        }

        private void UpdateRecordingUI()
        {
            float elapsed = Time.time - _recordingStartTime;
            float progress = Mathf.Clamp01(elapsed / voiceSampleDuration);

            recordingProgress.value = progress;
            recordingTimeText.text = $"{elapsed:F1}s / {voiceSampleDuration}s";

            if (elapsed >= voiceSampleDuration)
            {
                _isRecording = false;
            }
        }

        private void OnVoiceRecordingComplete(byte[] voiceData)
        {
            _voiceSample = voiceData;
            _isRecording = false;

            if (voiceData != null && voiceData.Length > 0)
            {
                voiceInstructions.text = "Voice sample recorded! Processing your Avatar Twin...";
                StartCoroutine(ProcessAvatarTwinSetup());
            }
            else
            {
                voiceInstructions.text = "Recording failed. Please try again.";
                startRecordingButton.gameObject.SetActive(true);
                stopRecordingButton.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Avatar Processing

        private IEnumerator ProcessAvatarTwinSetup()
        {
            voiceRecordPanel.SetActive(false);
            processingPanel.SetActive(true);

            processingStatusText.text = "Generating your 3D avatar...";
            processingProgress.value = 0.1f;

            // Initialize the avatar twin
            var initTask = avatarController.InitializeAvatarTwinAsync(_userId, _capturedPhoto, _voiceSample);

            while (!initTask.IsCompleted)
            {
                yield return null;
            }

            if (initTask.IsFaulted)
            {
                processingStatusText.text = $"Error: {initTask.Exception?.InnerException?.Message}";
                yield break;
            }

            processingProgress.value = 1f;
            processingStatusText.text = "Avatar Twin ready!";

            yield return new WaitForSeconds(1f);

            ShowChatInterface();
        }

        private void OnAvatarLoaded(string avatarId)
        {
            processingStatusText.text = "Avatar generated! Cloning your voice...";
            processingProgress.value = 0.5f;
        }

        private void OnVoiceCloned(string voiceId)
        {
            processingStatusText.text = "Voice cloned! Finalizing...";
            processingProgress.value = 0.9f;
        }

        #endregion

        #region Chat Interface

        private void ShowChatInterface()
        {
            setupPanel.SetActive(false);
            processingPanel.SetActive(false);
            chatPanel.SetActive(true);

            AddSystemMessage("Your Avatar Twin is ready! Start chatting.");
        }

        private async void SendMessage()
        {
            var message = messageInput.text.Trim();
            if (string.IsNullOrEmpty(message)) return;
            if (avatarController.IsSpeaking) return;

            messageInput.text = "";
            AddUserMessage(message);

            try
            {
                sendButton.interactable = false;
                var response = await avatarController.SendMessageAsync(message);
                AddAvatarMessage(response);
            }
            catch (Exception ex)
            {
                AddSystemMessage($"Error: {ex.Message}");
            }
            finally
            {
                sendButton.interactable = true;
            }
        }

        private void ToggleVoiceInput()
        {
            // TODO: Implement voice-to-text input
            Debug.Log("Voice input not yet implemented");
        }

        private void AddUserMessage(string message)
        {
            var messageObj = Instantiate(userMessagePrefab, chatContent);
            var textComponent = messageObj.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
                textComponent.text = message;

            ScrollToBottom();
        }

        private void AddAvatarMessage(string message)
        {
            var messageObj = Instantiate(avatarMessagePrefab, chatContent);
            var textComponent = messageObj.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
                textComponent.text = message;

            ScrollToBottom();
        }

        private void AddSystemMessage(string message)
        {
            // Use avatar message prefab with different styling
            var messageObj = Instantiate(avatarMessagePrefab, chatContent);
            var textComponent = messageObj.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = $"<i>{message}</i>";
                textComponent.color = Color.gray;
            }

            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }

        private void OnSpeechStarted(string text)
        {
            sendButton.interactable = false;
            messageInput.interactable = false;
        }

        private void OnSpeechEnded()
        {
            sendButton.interactable = true;
            messageInput.interactable = true;
            messageInput.ActivateInputField();
        }

        private void OnError(string error)
        {
            AddSystemMessage($"Error: {error}");
        }

        #endregion

        private void OnDestroy()
        {
            if (_webcamTexture != null)
            {
                _webcamTexture.Stop();
                Destroy(_webcamTexture);
            }

            if (avatarController != null)
            {
                avatarController.OnAvatarLoaded -= OnAvatarLoaded;
                avatarController.OnVoiceCloned -= OnVoiceCloned;
                avatarController.OnSpeechStarted -= OnSpeechStarted;
                avatarController.OnSpeechEnded -= OnSpeechEnded;
                avatarController.OnError -= OnError;
            }
        }
    }
}
