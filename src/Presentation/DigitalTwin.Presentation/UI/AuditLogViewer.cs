using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalTwin.Core.Models;
using DigitalTwin.Core.Enums;

namespace DigitalTwin.Presentation.UI
{
    /// <summary>
    /// Audit log viewer interface component
    /// </summary>
    public class AuditLogViewer : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private ScrollRect logScrollRect;
        [SerializeField] private Transform logContainer;
        [SerializeField] private GameObject logItemPrefab;
        
        [Header("Filter Controls")]
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Dropdown eventTypeFilter;
        [SerializeField] private TMP_Dropdown dateRangeFilter;
        [SerializeField] private TMP_InputField startDateInput;
        [SerializeField] private TMP_InputField endDateInput;
        [SerializeField] private TMP_InputField userFilter;
        [SerializeField] private Toggle showOnlyFailed;
        
        [Header("Controls")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button clearFiltersButton;
        [SerializeField] private Button exportButton;
        [SerializeField] private Button loadMoreButton;
        
        [Header("Pagination")]
        [SerializeField] private TMP_Text currentPageText;
        [SerializeField] private Button previousPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private TMP_Text totalRecordsText;
        
        [Header("Log Details")]
        [SerializeField] private GameObject logDetailsPanel;
        [SerializeField] private TMP_Text logDetailsTitle;
        [SerializeField] private TMP_Text logDetailsTimestamp;
        [SerializeField] private TMP_Text logDetailsEventType;
        [SerializeField] private TMP_Text logDetailsUser;
        [SerializeField] private TMP_Text logDetailsDescription;
        [SerializeField] private TMP_Text logDetailsIpAddress;
        [SerializeField] private TMP_Text logDetailsUserAgent;
        [SerializeField] private TMP_Text logDetailsRequestPath;
        [SerializeField] private Button logDetailsCloseButton;

        private SecurityEventLogger _securityEventLogger;
        private List<SecurityEvent> _allLogs = new List<SecurityEvent>();
        private List<SecurityEvent> _filteredLogs = new List<SecurityEvent>();
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalRecords = 0;

        private void Start()
        {
            InitializeServices();
            SetupUI();
            LoadInitialData();
        }

        private void InitializeServices()
        {
            _securityEventLogger = ServiceLocator.Instance.GetService<SecurityEventLogger>();
        }

        private void SetupUI()
        {
            // Setup filter controls
            searchInput.onValueChanged.AddListener(OnSearchChanged);
            eventTypeFilter.onValueChanged.AddListener(OnEventTypeFilterChanged);
            dateRangeFilter.onValueChanged.AddListener(OnDateRangeFilterChanged);
            startDateInput.onValueChanged.AddListener(OnDateFilterChanged);
            endDateInput.onValueChanged.AddListener(OnDateFilterChanged);
            userFilter.onValueChanged.AddListener(OnUserFilterChanged);
            showOnlyFailed.onValueChanged.AddListener(OnShowOnlyFailedChanged);

            // Setup control buttons
            refreshButton.onClick.AddListener(RefreshLogs);
            clearFiltersButton.onClick.AddListener(ClearFilters);
            exportButton.onClick.AddListener(ExportLogs);
            loadMoreButton.onClick.AddListener(LoadMoreLogs);

            // Setup pagination
            previousPageButton.onClick.AddListener(GoToPreviousPage);
            nextPageButton.onClick.AddListener(GoToNextPage);
            logDetailsCloseButton.onClick.AddListener(CloseLogDetails);

            // Initialize dropdown options
            InitializeEventTypeFilter();
            InitializeDateRangeFilter();

            // Set initial date range to last 7 days
            SetDateRangeToLast7Days();
        }

        private void InitializeEventTypeFilter()
        {
            eventTypeFilter.options.Clear();
            eventTypeFilter.options.Add(new TMP_Dropdown.OptionData("All Events"));
            
            var eventTypes = Enum.GetValues(typeof(SecurityEventType)).Cast<SecurityEventType>();
            foreach (var eventType in eventTypes)
            {
                eventTypeFilter.options.Add(new TMP_Dropdown.OptionData(eventType.ToString()));
            }
            
            eventTypeFilter.value = 0;
            eventTypeFilter.RefreshShownValue();
        }

        private void InitializeDateRangeFilter()
        {
            dateRangeFilter.options.Clear();
            dateRangeFilter.options.Add(new TMP_Dropdown.OptionData("All Time"));
            dateRangeFilter.options.Add(new TMP_Dropdown.OptionData("Last 24 Hours"));
            dateRangeFilter.options.Add(new TMP_Dropdown.OptionData("Last 7 Days"));
            dateRangeFilter.options.Add(new TMP_Dropdown.OptionData("Last 30 Days"));
            dateRangeFilter.options.Add(new TMP_Dropdown.OptionData("Custom Range"));
            
            dateRangeFilter.value = 2; // Last 7 days
            dateRangeFilter.RefreshShownValue();
        }

        private void SetDateRangeToLast7Days()
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-7);
            
            startDateInput.text = startDate.ToString("yyyy-MM-dd");
            endDateInput.text = endDate.ToString("yyyy-MM-dd");
        }

        private async void LoadInitialData()
        {
            await RefreshLogs();
        }

        private async Task RefreshLogs()
        {
            try
            {
                refreshButton.interactable = false;
                
                // Load all logs
                _allLogs = await _securityEventLogger.GetSecurityEventsAsync();
                _totalRecords = _allLogs.Count;
                
                // Apply filters
                ApplyFilters();
                
                // Update UI
                UpdateLogDisplay();
                UpdatePagination();
                
                refreshButton.interactable = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error refreshing audit logs: {ex.Message}");
                ShowErrorMessage("Failed to refresh audit logs");
                refreshButton.interactable = true;
            }
        }

        private void ApplyFilters()
        {
            _filteredLogs = _allLogs.Where(log => 
            {
                // Search filter
                if (!string.IsNullOrEmpty(searchInput.text))
                {
                    var searchTerm = searchInput.text.ToLower();
                    if (!log.Description.ToLower().Contains(searchTerm) &&
                        !log.EventType.ToString().ToLower().Contains(searchTerm) &&
                        (log.UserId == null || !log.UserId.ToLower().Contains(searchTerm)))
                    {
                        return false;
                    }
                }

                // Event type filter
                if (eventTypeFilter.value > 0)
                {
                    var selectedEventType = (SecurityEventType)(eventTypeFilter.value - 1);
                    if (log.EventType != selectedEventType)
                    {
                        return false;
                    }
                }

                // Date range filter
                if (dateRangeFilter.value == 4) // Custom range
                {
                    if (DateTime.TryParse(startDateInput.text, out var startDate) &&
                        DateTime.TryParse(endDateInput.text, out var endDate))
                    {
                        if (log.Timestamp < startDate || log.Timestamp > endDate)
                        {
                            return false;
                        }
                    }
                }
                else if (dateRangeFilter.value > 0)
                {
                    var filterDate = GetFilterDate(dateRangeFilter.value);
                    if (log.Timestamp < filterDate)
                    {
                        return false;
                    }
                }

                // User filter
                if (!string.IsNullOrEmpty(userFilter.text))
                {
                    if (string.IsNullOrEmpty(log.UserId) || 
                        !log.UserId.ToLower().Contains(userFilter.text.ToLower()))
                    {
                        return false;
                    }
                }

                // Show only failed events
                if (showOnlyFailed.isOn)
                {
                    if (!IsFailedEvent(log.EventType))
                    {
                        return false;
                    }
                }

                return true;
            }).ToList();
        }

        private DateTime GetFilterDate(int filterValue)
        {
            return filterValue switch
            {
                1 => DateTime.UtcNow.AddHours(-24), // Last 24 hours
                2 => DateTime.UtcNow.AddDays(-7),   // Last 7 days
                3 => DateTime.UtcNow.AddDays(-30),  // Last 30 days
                _ => DateTime.MinValue
            };
        }

        private bool IsFailedEvent(SecurityEventType eventType)
        {
            return eventType switch
            {
                SecurityEventType.AccessDenied => true,
                SecurityEventType.AccountLocked => true,
                SecurityEventType.BruteForceAttempt => true,
                SecurityEventType.InvalidToken => true,
                SecurityEventType.TokenExpired => true,
                SecurityEventType.SuspiciousActivity => true,
                SecurityEventType.RateLimitExceeded => true,
                _ => false
            };
        }

        private void UpdateLogDisplay()
        {
            // Clear existing items
            foreach (Transform child in logContainer)
            {
                Destroy(child.gameObject);
            }

            // Calculate page range
            var startIndex = (_currentPage - 1) * _pageSize;
            var endIndex = Mathf.Min(startIndex + _pageSize, _filteredLogs.Count);
            var pageLogs = _filteredLogs.Skip(startIndex).Take(_pageSize).ToList();

            // Create log items
            foreach (var log in pageLogs)
            {
                var logItem = Instantiate(logItemPrefab, logContainer);
                var logItemUI = logItem.GetComponent<AuditLogItemUI>();
                
                if (logItemUI != null)
                {
                    logItemUI.Setup(log, OnLogItemSelected);
                }
            }

            // Update load more button
            loadMoreButton.gameObject.SetActive(endIndex < _filteredLogs.Count);
        }

        private void UpdatePagination()
        {
            currentPageText.text = $"Page {_currentPage}";
            totalRecordsText.text = $"Total Records: {_totalRecords} (Filtered: {_filteredLogs.Count})";
            
            previousPageButton.interactable = _currentPage > 1;
            nextPageButton.interactable = _currentPage * _pageSize < _filteredLogs.Count;
        }

        private void OnLogItemSelected(SecurityEvent log)
        {
            ShowLogDetails(log);
        }

        private void ShowLogDetails(SecurityEvent log)
        {
            logDetailsTitle.text = $"Security Event - {log.EventType}";
            logDetailsTimestamp.text = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC");
            logDetailsEventType.text = log.EventType.ToString();
            logDetailsUser.text = log.UserId ?? "N/A";
            logDetailsDescription.text = log.Description;
            logDetailsIpAddress.text = log.IpAddress ?? "N/A";
            logDetailsUserAgent.text = log.UserAgent ?? "N/A";
            logDetailsRequestPath.text = log.RequestPath ?? "N/A";
            
            logDetailsPanel.SetActive(true);
        }

        private void CloseLogDetails()
        {
            logDetailsPanel.SetActive(false);
        }

        private void OnSearchChanged(string value)
        {
            ApplyFilters();
            UpdateLogDisplay();
            UpdatePagination();
        }

        private void OnEventTypeFilterChanged(int value)
        {
            ApplyFilters();
            UpdateLogDisplay();
            UpdatePagination();
        }

        private void OnDateRangeFilterChanged(int value)
        {
            // Enable/disable custom date inputs
            startDateInput.interactable = value == 4;
            endDateInput.interactable = value == 4;
            
            ApplyFilters();
            UpdateLogDisplay();
            UpdatePagination();
        }

        private void OnDateFilterChanged(string value)
        {
            if (dateRangeFilter.value == 4) // Custom range
            {
                ApplyFilters();
                UpdateLogDisplay();
                UpdatePagination();
            }
        }

        private void OnUserFilterChanged(string value)
        {
            ApplyFilters();
            UpdateLogDisplay();
            UpdatePagination();
        }

        private void OnShowOnlyFailedChanged(bool value)
        {
            ApplyFilters();
            UpdateLogDisplay();
            UpdatePagination();
        }

        private void ClearFilters()
        {
            searchInput.text = "";
            eventTypeFilter.value = 0;
            dateRangeFilter.value = 2; // Last 7 days
            userFilter.text = "";
            showOnlyFailed.isOn = false;
            
            SetDateRangeToLast7Days();
            
            ApplyFilters();
            UpdateLogDisplay();
            UpdatePagination();
        }

        private async void ExportLogs()
        {
            try
            {
                var csvData = ConvertLogsToCSV(_filteredLogs);
                
                // In a real implementation, this would save to file or trigger download
                var filename = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                
                Debug.Log($"Exported {_filteredLogs.Count} audit logs to {filename}");
                ShowSuccessMessage($"Exported {_filteredLogs.Count} audit logs");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error exporting audit logs: {ex.Message}");
                ShowErrorMessage("Failed to export audit logs");
            }
        }

        private void LoadMoreLogs()
        {
            _currentPage++;
            UpdateLogDisplay();
            UpdatePagination();
        }

        private void GoToPreviousPage()
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdateLogDisplay();
                UpdatePagination();
            }
        }

        private void GoToNextPage()
        {
            if (_currentPage * _pageSize < _filteredLogs.Count)
            {
                _currentPage++;
                UpdateLogDisplay();
                UpdatePagination();
            }
        }

        private string ConvertLogsToCSV(List<SecurityEvent> logs)
        {
            var csv = "Timestamp,Event Type,User ID,Description,IP Address,User Agent,Request Path\n";
            
            foreach (var log in logs.OrderByDescending(l => l.Timestamp))
            {
                csv += $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                       $"{log.EventType}," +
                       $"\"{log.UserId}\"," +
                       $"\"{log.Description}\"," +
                       $"{log.IpAddress}," +
                       $"\"{log.UserAgent}\"," +
                       $"{log.RequestPath}\n";
            }
            
            return csv;
        }

        private void ShowSuccessMessage(string message)
        {
            // Show success message UI
            Debug.Log($"Success: {message}");
        }

        private void ShowErrorMessage(string message)
        {
            // Show error message UI
            Debug.LogError($"Error: {message}");
        }
    }

    /// <summary>
    /// Individual audit log item UI component
    /// </summary>
    public class AuditLogItemUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text timestampText;
        [SerializeField] private TMP_Text eventTypeText;
        [SerializeField] private TMP_Text userText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text ipAddressText;
        [SerializeField] private Image eventTypeIndicator;
        [SerializeField] private Button detailsButton;

        private SecurityEvent _log;
        private System.Action<SecurityEvent> _onLogSelected;

        public void Setup(SecurityEvent log, System.Action<SecurityEvent> onLogSelected)
        {
            _log = log;
            _onLogSelected = onLogSelected;
            
            UpdateUI();
        }

        private void UpdateUI()
        {
            timestampText.text = _log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            eventTypeText.text = _log.EventType.ToString();
            userText.text = _log.UserId ?? "N/A";
            descriptionText.text = _log.Description;
            ipAddressText.text = _log.IpAddress ?? "N/A";
            
            // Set event type indicator color
            eventTypeIndicator.color = GetEventTypeColor(_log.EventType);
            
            // Setup button click
            detailsButton.onClick.AddListener(OnDetailsClicked);
        }

        private Color GetEventTypeColor(SecurityEventType eventType)
        {
            return eventType switch
            {
                SecurityEventType.UserLoggedIn => Color.green,
                SecurityEventType.UserLoggedOut => Color.blue,
                SecurityEventType.UserRegistered => Color.cyan,
                SecurityEventType.AccessDenied => Color.red,
                SecurityEventType.AccountLocked => Color.red,
                SecurityEventType.BruteForceAttempt => Color.red,
                SecurityEventType.SuspiciousActivity => Color.yellow,
                SecurityEventType.TokenExpired => Color.orange,
                SecurityEventType.InvalidToken => Color.red,
                SecurityEventType.RateLimitExceeded => Color.yellow,
                _ => Color.gray
            };
        }

        private void OnDetailsClicked()
        {
            _onLogSelected?.Invoke(_log);
        }
    }
}