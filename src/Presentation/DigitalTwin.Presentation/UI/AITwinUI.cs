using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.DTOs;

namespace DigitalTwin.Presentation.UI
{
    /// <summary>
    /// AI Twin user interface for conversational interaction
    /// </summary>
    public class AITwinUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject aiTwinPanel;
        [SerializeField] private GameObject conversationPanel;
        [SerializeField] private TMP_Text aiTwinName;
        [SerializeField] private TMP_Text aiTwinStatus;
        [SerializeField] private TMP_Text conversationPrompt;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button voiceButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button minimizeButton;
        
        [Header("Conversation Display")]
        [SerializeField] private ScrollRect conversationScrollRect;
        [SerializeField] private Transform conversationContent;
        [SerializeField] private GameObject messagePrefab;
        
        [Header("AI Twin Visualization")]
        [SerializeField] private GameObject avatarDisplay;
        [SerializeField] private RawImage avatarImage;
        [SerializeField] private GameObject emotionalIndicator;
        [SerializeField] private Animator avatarAnimator;
        [SerializeField] private Image emotionalExpressionImage;
        [SerializeField] private Sprite[] emotionalSprites;
        
        [Header("Voice Controls")]
        [SerializeField] private Button muteButton;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Dropdown voiceDropdown;
        
        [Header("Learning Progress")]
        [SerializeField] private Slider activationLevelSlider;
        [SerializeField] private TMP_Text activationLevelText;
        [SerializeField] private Slider learningProgressSlider;
        [SerializeField] private TMP_Text learningProgressText;
        [SerializeField] private GameObject[] milestoneIndicators;
        
        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
       SerializeField] private Toggle learningModeToggle;
        [SerializeField] private Slider personalityFriendlinessSlider;
        [SerializeField] private Slider personalityProfessionalismSlider;
        [SerializeField] private Slider personalityCuriositySlider;
        [SerializeField] private Slider personalityHumorSlider;
        [SerializeField] private Button resetLearningButton;
        [SerializeField] private Button backupButton;
        [SerializeField] private Button restoreButton;

        private IAITwinService _aiTwinService;
        private IVoiceSynthesisService _voiceService;
        private IAITwinImageService _imageService;
        private IEmotionalRecognitionService _emotionalService;
        
        private AITwinProfile _currentTwin;
        private List<AITwinConversation> _conversations;
        private int _currentConversationPage = 1;
        private bool _isVoiceEnabled = true;
        private bool _isRecording = false;
        private AITwinVoiceConfiguration _currentVoiceConfig;

        private void Start()
        {
            InitializeServices();
            SetupUI();
            LoadUserTwinProfiles();
        }

        private void InitializeServices()
        {
            _aiTwinService = ServiceLocator.Instance.GetService<IAITwinService>();
            _voiceService = ServiceLocator.Instance.GetService<IVoiceSynthesisService>();
            _imageService = ServiceLocator.Instance.GetService<IAITwinImageService>();
            _emotionalService = ServiceLocator.Instance.GetService<IEmotionalRecognitionService>();
        }

        private void SetupUI()
        {
            // Setup conversation input
            conversationPrompt.onEndEdit.AddListener(OnPromptSubmitted);
            sendButton.onClick.AddListener(OnSendButtonClick);
            
            // Setup voice controls
            voiceButton.onClick.AddListener(ToggleVoiceRecording);
            muteButton.onClick.AddListener(ToggleMute);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            voiceDropdown.onValueChanged.AddListener(OnVoiceChanged);
            
            // Settings panel
            settingsButton.onClick.AddListener(ToggleSettingsPanel);
            minimizeButton.onClick.AddListener(MinimizePanel);
            resetLearningButton.onClick.AddListener(ResetLearning);
            backupButton.onClick.AddListener(BackupTwinData);
            restoreButton.onClick.AddListener(RestoreTwinData);
            
            // Personality sliders
            personalityFriendlinessSlider.onValueChanged.AddListener(UpdatePersonalityTrait);
            personalityProfessionalismSlider.onValueChanged.AddListener(UpdatePersonalityTrait);
            personalityCuriositySlider.onValueChanged.AddListener(UpdatePersonalityTrait);
            personalityHumorSlider.onValueChanged.AddListener(UpdatePersonalityTrait);
            
            // Learning mode toggle
            learningModeToggle.onValueChanged.AddListener(OnLearningModeChanged);
        }

        private async void LoadUserTwinProfiles()
        {
            try
            {
                // Mock user ID - in real implementation, get from auth service
                var userId = "user123";
                var twinProfiles = await _aiTwinService.GetUserTwinProfilesAsync(userId);
                
                if (twinProfiles.Any())
                {
                    // Auto-select the most recent twin
                    _currentTwin = twinProfiles.OrderByDescending(t => t.LastInteraction).First();
                    await LoadAITwin(_currentTwin.Id);
                }
                else
                {
                    ShowCreateTwinPanel();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading AI twin profiles: {ex.Message}");
                ShowErrorMessage("Failed to load AI twin profiles");
            }
        }

        private async void LoadAITwin(Guid twinId)
        {
            try
            {
                _currentTwin = await _aiTwinService.GetByIdAsync(twinId);
                if (_currentTwin == null)
                {
                    throw new ArgumentException($"AI twin with ID {twinId} not found");
                }

                await UpdateUIWithTwinData();
                await LoadConversationHistory();
                await UpdateLearningProgress();
                await UpdateAvatar();
                
                ShowAITwinPanel();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading AI twin: {ex.Message}");
                ShowErrorMessage("Failed to load AI twin");
            }
        }

        private async Task UpdateUIWithTwinData()
        {
            if (_currentTwin == null) return;

            // Update basic info
            aiTwinName.text = _currentTwin.Name;
            aiTwinStatus.text = $"Active • Learning {_currentTwin.LearningMode}";
            activationLevelSlider.value = _currentTwin.ActivationLevel;
            activationLevelText.text = $"Activation: {(_currentTwin.ActivationLevel * 100):F0}%";

            // Update personality sliders
            personalityFriendlinessSlider.value = _currentTwin.PersonalityTraits.Friendliness;
            personalityProfessionalismSlider.value = _currentTwin.PersonalityTraits.Professionalism;
            personalityCuriositySlider.value = _currentTwin.PersonalityTraits.Curiosity;
            personalityHumorSlider.value = _currentTwin.PersonalityTraits.Humor;

            // Update learning mode
            learningModeToggle.isOn = _currentTwin.LearningMode == AITwinLearningMode.Adaptive;

            await LoadAvailableVoices();
        }

        private async Task LoadConversationHistory()
        {
            if (_currentTwin == null) return;

            try
            {
                _conversations = await _aiTwinService.GetConversationsAsync(_currentTwin.Id);
                DisplayConversations();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading conversation history: {ex.Message}");
            }
        }

        private void DisplayConversations()
        {
            if (_conversations == null) return;

            // Clear existing conversation items
            foreach (Transform child in conversationContent)
            {
                Destroy(child.gameObject);
            }

            // Create conversation items
            foreach (var conversation in _conversations.Take(50)) // Limit to 50 recent conversations
            {
                CreateConversationItem(conversation);
            }
        }

        private void CreateConversationItem(AITwinConversation conversation)
        {
            var item = Instantiate(messagePrefab, conversationContent);
            var itemUI = item.GetComponent<ConversationItemUI>();
            
            if (itemUI != null)
            {
                itemUI.Setup(conversation);
            }
        }

        private async Task UpdateLearningProgress()
        {
            if (_currentTwin == null) return;

            try
            {
                var progress = await _aiTwinService.GetLearningProgressAsync(_currentTwin.Id);
                
                // Update progress sliders
                learningProgressSlider.value = progress.ActivationLevel;
                learningProgressText.text = $"Learning: {(progress.ActivationLevel * 100):F0}%";
                
                // Update milestone indicators
                UpdateMilestoneIndicators(progress.DevelopmentalMilestones);
                
                // Update activation level
                activationLevelSlider.value = progress.ActivationLevel;
                activationLevelText.text = $"Activation: {(progress.ActivationLevel * 100):F0}%";
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating learning progress: {ex.Message}");
            }
        }

        private void UpdateMilestoneIndicators(List<string> milestones)
        {
            for (int i = 0; i < milestoneIndicators.Length; i++)
            {
                var isAchieved = i < milestones.Count;
                milestoneIndicators[i].SetActive(isAchieved);
            }
        }

        private async Task UpdateAvatar()
        {
            if (_currentTwin == null) return;

            try
            {
                // Generate avatar image
                var avatarData = await _imageService.GenerateAvatarAsync(_currentTwin);
                
                if (avatarData != null)
                {
                    var texture = new Texture2D(1, 1);
                    texture.LoadImage(avatarData);
                    avatarImage.texture = texture;
                }

                // Update emotional expression
                await UpdateEmotionalExpression(_currentTwin.EmotionalState);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating avatar: {ex.Message}");
            }
        }

        private async Task UpdateEmotionalExpression(EmotionalState emotionalState)
        {
            try
            {
                // Get emotional sprite
                var sprite = GetEmotionalSprite(emotionalState);
                if (sprite != null)
                {
                    emotionalExpressionImage.sprite = sprite;
                }

                // Trigger animation
                if (avatarAnimator != null)
                {
                    var triggerName = GetEmotionalAnimationTrigger(emotionalState);
                    if (!string.IsNullOrEmpty(triggerName))
                    {
                        avatarAnimator.SetTrigger(triggerName);
                    }
                }

                // Show/hide emotional indicator
                emotionalIndicator.SetActive(emotionalState != EmotionalState.Neutral);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating emotional expression: {ex.Message}");
            }
        }

        private Sprite GetEmotionalSprite(EmotionalState state)
        {
            return state switch
            {
                EmotionalState.Happy => emotionalSprites[0],
                EmotionalState.Excited => emotionalSprites[1],
                EmotionalState.Neutral => emotionalSprites[2],
                EmotionalState.Concerned => emotionalSprites[3],
                EmotionalState.Frustrated => emotionalSprites[4],
                EmotionalState.Curious => emotionalSprites[5],
                _ => emotionalSprites[2] // Default to neutral
            };
        }

        private string GetEmotionalAnimationTrigger(EmotionalState state)
        {
            return state switch
            {
                EmotionalState.Happy => "Smile",
                EmotionalState.Excited => "Excited",
                EmotionalState.Neutral => "Neutral",
                EmotionalState.Concerned => "Concerned",
                EmotionalState.Frustrated => "Frustrated",
                EmotionalState.Curious => "Curious",
                _ => "Neutral"
            };
        }

        private async Task LoadAvailableVoices()
        {
            try
            {
                var voices = await _voiceService.GetAvailableVoicesAsync();
                
                voiceDropdown.options.Clear();
                foreach (var voice in voices)
                {
                    voiceDropdown.options.Add(new TMP_Dropdown.OptionData(voice.Name));
                }
                
                voiceDropdown.value = 0; // Select first voice by default
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading voices: {ex.Message}");
            }
        }

        private void OnPromptSubmitted(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt) || _currentTwin == null)
                return;

            ProcessUserMessage(prompt);
        }

        private void OnSendButtonClick()
        {
            OnPromptSubmitted(conversationPrompt.text);
            conversationPrompt.text = "";
            conversationPrompt.ActivateInputField();
        }

        private async void ProcessUserMessage(string message)
        {
            try
            {
                // Show typing indicator
                ShowTypingIndicator();

                // Create user message
                var userMessage = new AITwinMessage
                {
                    Type = "user",
                    Content = message,
                    Timestamp = DateTime.UtcNow,
                    Context = new Dictionary<string, object>()
                };

                // Process message through AI twin service
                var aiResponse = await _aiTwinService.ProcessMessageAsync(userMessage, _currentTwin.Id);

                // Display both messages
                DisplayUserMessage(userMessage);
                DisplayTwinResponse(aiResponse);

                // Play voice if enabled
                if (_isVoiceEnabled)
                {
                    await SpeakResponse(aiResponse.Content);
                }

                HideTypingIndicator();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing message: {ex.Message}");
                ShowErrorMessage("Failed to process message");
                HideTypingIndicator();
            }
        }

        private void DisplayUserMessage(AITwinMessage message)
        {
            var messageObj = Instantiate(messagePrefab, conversationContent);
            var messageUI = messageObj.GetComponent<ConversationItemUI>();
            
            if (messageUI != null)
            {
                messageUI.Setup(message, true);
            }
        }

        private void DisplayTwinResponse(AITwinResponse response)
        {
            var messageObj = Instantiate(messagePrefab, conversationContent);
            var messageUI = messageObj.GetComponent<ConversationItemUI>();
            
            if (messageUI != null)
            {
                messageUI.Setup(response, false);
            }

            // Scroll to bottom
            Canvas.ForceUpdateRectTransforms();
            LayoutRebuilder.ForceRebuildLayoutImmediate(conversationContent);
            
            await Task.Delay(100); // Small delay to ensure scroll works
            Canvas.ForceUpdateRectTransforms();
            LayoutRebuilder.ForceRebuildLayoutImmediate(conversationContent);
            
            // Scroll to bottom
            conversationScrollRect.verticalNormalizedPosition = 0;
        }

        private async Task SpeakResponse(string text)
        {
            try
            {
                var audioData = await _voiceService.SynthesizeSpeechAsync(text, _currentVoiceConfig);
                await PlayAudio(audioData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error synthesizing speech: {ex.Message}");
            }
        }

        private async Task PlayAudio(byte[] audioData)
        {
            // In a real implementation, this would use Unity's AudioSystem
            // For now, we'll just log the audio size
            Debug.Log($"Playing audio of size: {audioData.Length} bytes");
            await Task.CompletedTask;
        }

        private void ShowTypingIndicator()
        {
            aiTwinStatus.text = "Typing...";
            emotionalIndicator.SetActive(true);
        }

        private void HideTypingIndicator()
        {
            if (_currentTwin != null)
            {
                aiTwinStatus.text = $"Active • Learning {_currentTwin.LearningMode}";
            }
            emotionalIndicator.SetActive(false);
        }

        private void ToggleVoiceRecording()
        {
            _isRecording = !_isRecording;
            
            if (_isRecording)
            {
                voiceButton.GetComponentInChildren<Image>().color = Color.red;
                StartVoiceRecording();
            }
            else
            {
                voiceButton.GetComponentInChildren<Image>().color = Color.white;
                StopVoiceRecording();
            }
        }

        private void StartVoiceRecording()
        {
            // In a real implementation, this would start recording from microphone
            Debug.Log("Starting voice recording...");
        }

        private void StopVoiceRecording()
        {
            // In a real implementation, this would stop recording and process the audio
            Debug.Log("Stopping voice recording...");
        }

        private void ToggleMute()
        {
            _isVoiceEnabled = !_isVoiceEnabled;
            muteButton.GetComponentInChildren<Image>().color = _isVoiceEnabled ? Color.white : Color.gray;
            
            if (!_isVoiceEnabled)
            {
                StopAllAudioPlayback();
            }
        }

        private void OnVolumeChanged(float value)
        {
            // Update audio volume
            AudioListener.volume = value;
        }

        private void OnVoiceChanged(int index)
        {
            // In a real implementation, this would change the voice
            Debug.Log($"Selected voice index: {index}");
        }

        private void ToggleSettingsPanel()
        {
            settingsPanel.SetActive(!settingsPanel.active);
            minimizeButton.gameObject.SetActive(!settingsPanel.active);
        }

        private void MinimizePanel()
        {
            conversationPanel.SetActive(false);
            minimizeButton.gameObject.SetActive(true);
        }

        private async void UpdatePersonalityTrait(float value)
        {
            if (_currentTwin == null) return;

            // Update the appropriate trait based on which slider was changed
            var sliderName = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.name;
            
            if (sliderName.Contains("Friendliness"))
                _currentTwin.PersonalityTraits.Friendliness = value;
            else if (sliderName.Contains("Professionalism"))
                _currentTwin.PersonalityTraits.Professionalism = value;
            else if (sliderName.Contains("Curiosity"))
                _currentTwin.PersonalityTraits.Curiosity = value;
            else if (sliderName.Contains("Humor"))
                _currentTwin.PersonalityTraits.Humor = value;

            await _aiTwinService.UpdatePersonalityTraitsAsync(_currentTwin.Id, _currentTwin.PersonalityTraits);
        }

        private async void OnLearningModeChanged(bool isOn)
        {
            var mode = isOn ? AITwinLearningMode.Adaptive : AITwinLearningMode.Fixed;
            
            if (_currentTwin != null)
            {
                await _aiTwinService.SetLearningModeAsync(_currentTwin.Id, mode);
            }
        }

        private async Task ResetLearning(Guid twinId)
        {
            try
            {
                await _aiTwinService.ResetLearningAsync(twinId);
                ShowSuccessMessage("AI twin learning has been reset");
                
                // Reload twin data
                await LoadAITwin(twinId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error resetting learning: {ex.Message}");
                ShowErrorMessage("Failed to reset learning");
            }
        }

        private async Task BackupTwinData()
        {
            if (_currentTwin == null) return;

            try
            {
                var backupData = await _aiTwinService.BackupTwinDataAsync(_currentTwin.Id);
                await SaveBackupToFile(backupData);
                ShowSuccessMessage("AI twin data backed up successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error backing up twin data: {ex.Message}");
                ShowErrorMessage("Failed to backup twin data");
            }
        }

        private async Task SaveBackupToFile(string backupData)
        {
            var filename = $"ai_twin_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var filePath = System.IO.Path.Combine(Application.persistentDataPath, "Backups", filename);
            
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
            await System.IO.File.WriteAllTextAsync(filePath, backupData);
            
            Debug.Log($"Backup saved to: {filePath}");
        }

        private void ShowCreateTwinPanel()
        {
            // Show a panel to create a new AI twin
            Debug.Log("Show create AI twin panel");
        }

        private void ShowAITwinPanel()
        {
            aiTwinPanel.SetActive(true);
            conversationPanel.SetActive(true);
            settingsPanel.SetActive(false);
            minimizeButton.gameObject.SetActive(false);
        }

        private void ShowSuccessMessage(string message)
        {
            Debug.Log($"Success: {message}");
            // In a real implementation, this would show a UI notification
        }

        private void ShowErrorMessage(string message)
        {
            Debug.LogError($"Error: {message}");
            // In a real implementation, this would show a UI error message
        }

        private void StopAllAudioPlayback()
        {
            // In a real implementation, this would stop all audio
            Debug.Log("Stopping all audio playback");
        }
    }

    /// <summary>
    /// UI component for displaying conversation messages
    /// </summary>
    public class ConversationItemUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text senderText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text timestampText;
        [SerializeField] private Button copyButton;
        [SerializeField] private Button reportButton;

        public void Setup(object message, bool isUser)
        {
            if (message is AITwinMessage userMessage)
            {
                Setup(userMessage, isUser);
            }
            else if (message is AITwinResponse aiResponse)
            {
                Setup(aiResponse, isUser);
            }
        }

        public void Setup(AITwinMessage message, bool isUser)
        {
            senderText.text = "You";
            messageText.text = message.Content;
            timestampText.text = message.Timestamp.ToString("HH:mm");
            
            if (isUser)
            {
                backgroundImage.color = new Color(0.8f, 0.8f, 1f, 0.2f);
                senderText.color = Color.white;
                avatarImage.gameObject.SetActive(false);
            }
        }

        public void Setup(AITwinResponse response, bool isUser)
        {
            var profileName = "AI Twin"; // Would get from profile
            
            senderText.text = profileName;
            messageText.text = response.Content;
            timestampText.text = DateTime.UtcNow.ToString("HH:mm");
            
            if (!isUser)
            {
                backgroundImage.color = new Color(0.2f, 0.4f, 0.8f, 0.3f);
                senderText.color = Color.white;
                avatarImage.gameObject.SetActive(true);
                
                // Load AI twin avatar
                // avatarImage.sprite = GetTwinAvatarSprite();
            }
            
            // Add confidence indicator
            if (response.Confidence < 0.7)
            {
                copyButton.gameObject.SetActive(true);
                copyButton.GetComponentInChildren<TMP_Text>().text = "Low Confidence";
            }
            else
            {
                copyButton.gameObject.SetActive(false);
            }

            // Add emotional tone indicator
            if (response.EmotionalTone != EmotionalTone.Neutral)
            {
                var color = GetEmotionalColor(response.EmotionalTone);
                timestampText.color = color;
            }
        }

        private void Start()
        {
            copyButton.onClick.AddListener(CopyToClipboard);
            reportButton.onClick.AddListener(ReportMessage);
        }

        private async void CopyToClipboard()
        {
            var text = messageText.text;
            GUIUtility.systemCopyBuffer = text;
            
            // Show confirmation
            var originalText = copyButton.GetComponentInChildren<TMP_Text>().text;
            copyButton.GetComponentInChildren<TMP_Text>().text = "Copied!";
            
            await Task.Delay(1000);
            copyButton.GetComponentInChildren<TMP_Text>().text = originalText;
        }

        private void ReportMessage()
        {
            // In a real implementation, this would report inappropriate content
            Debug.Log("Reporting message: " + messageText.text);
        }

        private Sprite GetTwinAvatarSprite()
        {
            // In a real implementation, this would return the AI twin's avatar sprite
            return null;
        }

        private Color GetEmotionalColor(EmotionalTone tone)
        {
            return tone switch
            {
                EmotionalTone.Happy => Color.yellow,
                EmotionalTone.Excited => Color.magenta,
                EmotionalTone.Concerned => Color.orange,
                EmotionalTone.Frustrated => Color.red,
                EmotionalTone.Curious => Color.cyan,
                _ => Color.white
            };
        }
    }
}