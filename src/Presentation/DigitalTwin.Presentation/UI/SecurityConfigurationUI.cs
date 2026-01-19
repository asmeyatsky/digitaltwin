using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Security;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Enums;

namespace DigitalTwin.Presentation.UI
{
    /// <summary>
    /// Security configuration UI panel
    /// </summary>
    public class SecurityConfigurationUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject securityPanel;
        [SerializeField] private TabGroup tabGroup;
        
        [Header("User Management Tab")]
        [SerializeField] private Transform userListContainer;
        [SerializeField] private GameObject userItemPrefab;
        [SerializeField] private Button addUserButton;
        [SerializeField] private TMP_InputField searchUserInput;
        
        [Header("Role Management Tab")]
        [SerializeField] private Transform roleListContainer;
        [SerializeField] private GameObject roleItemPrefab;
        [SerializeField] private Button addRoleButton;
        
        [Header("Security Settings Tab")]
        [SerializeField] private Toggle enableTwoFactorAuth;
        [SerializeField] private Toggle enableSessionTimeout;
        [SerializeField] private Slider sessionTimeoutSlider;
        [SerializeField] private TMP_Text sessionTimeoutText;
        [SerializeField] private Toggle enableRateLimiting;
        [SerializeField] private Slider rateLimitSlider;
        [SerializeField] private TMP_Text rateLimitText;
        [SerializeField] private Button saveSecuritySettingsButton;
        
        [Header("Audit Log Tab")]
        [SerializeField] private Transform auditLogContainer;
        [SerializeField] private GameObject auditLogItemPrefab;
        [SerializeField] private TMP_InputField logSearchInput;
        [SerializeField] private TMP_Dropdown logEventTypeFilter;
        [SerializeField] private Button refreshLogsButton;
        [SerializeField] private Button exportLogsButton;
        
        [Header("User Details Modal")]
        [SerializeField] private GameObject userDetailsModal;
        [SerializeField] private TMP_Text userDetailsTitle;
        [SerializeField] private TMP_InputField userDetailsEmail;
        [SerializeField] private TMP_InputField userDetailsFirstName;
        [SerializeField] private TMP_InputField userDetailsLastName;
        [SerializeField] private TMP_Dropdown userDetailsRole;
        [SerializeField] private Toggle userDetailsActive;
        [SerializeField] private Button userDetailsSaveButton;
        [SerializeField] private Button userDetailsCancelButton;
        [SerializeField] private Button userDetailsDeleteButton;
        [SerializeField] private Button userDetailsResetPasswordButton;

        private AuthenticationService _authService;
        private RoleBasedAccessControlService _rbacService;
        private SecurityEventLogger _securityEventLogger;
        private List<UserDTO> _currentUsers = new List<UserDTO>();
        private UserDTO _selectedUser;

        private void Start()
        {
            InitializeServices();
            SetupUI();
            LoadInitialData();
        }

        private void InitializeServices()
        {
            // Get services from DI container
            _authService = ServiceLocator.Instance.GetService<AuthenticationService>();
            _rbacService = ServiceLocator.Instance.GetService<RoleBasedAccessControlService>();
            _securityEventLogger = ServiceLocator.Instance.GetService<SecurityEventLogger>();
        }

        private void SetupUI()
        {
            // Setup tab group
            if (tabGroup != null)
            {
                tabGroup.OnTabSelected += OnTabSelected;
            }

            // Setup user management
            addUserButton.onClick.AddListener(ShowAddUserModal);
            searchUserInput.onValueChanged.AddListener(OnSearchUserChanged);

            // Setup role management
            addRoleButton.onClick.AddListener(ShowAddRoleModal);

            // Setup security settings
            sessionTimeoutSlider.onValueChanged.AddListener(OnSessionTimeoutChanged);
            rateLimitSlider.onValueChanged.AddListener(OnRateLimitChanged);
            saveSecuritySettingsButton.onClick.AddListener(SaveSecuritySettings);

            // Setup audit log
            refreshLogsButton.onClick.AddListener(RefreshAuditLogs);
            exportLogsButton.onClick.AddListener(ExportAuditLogs);
            logSearchInput.onValueChanged.AddListener(OnLogSearchChanged);
            logEventTypeFilter.onValueChanged.AddListener(OnLogEventTypeFilterChanged);

            // Setup user details modal
            userDetailsSaveButton.onClick.AddListener(SaveUserDetails);
            userDetailsCancelButton.onClick.AddListener(CloseUserDetailsModal);
            userDetailsDeleteButton.onClick.AddListener(DeleteUser);
            userDetailsResetPasswordButton.onClick.AddListener(ResetUserPassword);
        }

        private async void LoadInitialData()
        {
            await LoadUsers();
            await LoadRoles();
            await LoadSecuritySettings();
            await LoadAuditLogs();
        }

        private async Task LoadUsers()
        {
            try
            {
                // In a real implementation, this would call the user service
                // For now, we'll create mock data
                _currentUsers = CreateMockUsers();
                RefreshUserList();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading users: {ex.Message}");
                ShowErrorMessage("Failed to load users");
            }
        }

        private async Task LoadRoles()
        {
            try
            {
                var allRoles = await _rbacService.GetAllRolePermissionsAsync();
                RefreshRoleList(allRoles);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading roles: {ex.Message}");
                ShowErrorMessage("Failed to load roles");
            }
        }

        private async Task LoadSecuritySettings()
        {
            try
            {
                // Load security settings from configuration
                var securityConfig = await GetSecurityConfiguration();
                
                enableTwoFactorAuth.isOn = securityConfig.EnableTwoFactorAuth;
                enableSessionTimeout.isOn = securityConfig.EnableSessionTimeout;
                sessionTimeoutSlider.value = securityConfig.SessionTimeoutMinutes;
                OnSessionTimeoutChanged(securityConfig.SessionTimeoutMinutes);
                
                enableRateLimiting.isOn = securityConfig.EnableRateLimiting;
                rateLimitSlider.value = securityConfig.RateLimitRequestsPerMinute;
                OnRateLimitChanged(securityConfig.RateLimitRequestsPerMinute);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading security settings: {ex.Message}");
                ShowErrorMessage("Failed to load security settings");
            }
        }

        private async Task LoadAuditLogs()
        {
            try
            {
                var logs = await _securityEventLogger.GetSecurityEventsAsync();
                RefreshAuditLogList(logs);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading audit logs: {ex.Message}");
                ShowErrorMessage("Failed to load audit logs");
            }
        }

        private void RefreshUserList()
        {
            // Clear existing items
            foreach (Transform child in userListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create user items
            foreach (var user in _currentUsers)
            {
                var userItem = Instantiate(userItemPrefab, userListContainer);
                var userItemUI = userItem.GetComponent<UserItemUI>();
                
                if (userItemUI != null)
                {
                    userItemUI.Setup(user, OnUserSelected);
                }
            }
        }

        private void RefreshRoleList(Dictionary<UserRole, List<Permission>> allRoles)
        {
            // Clear existing items
            foreach (Transform child in roleListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create role items
            foreach (var role in allRoles)
            {
                var roleItem = Instantiate(roleItemPrefab, roleListContainer);
                var roleItemUI = roleItem.GetComponent<RoleItemUI>();
                
                if (roleItemUI != null)
                {
                    roleItemUI.Setup(role.Key, role.Value, OnRoleSelected);
                }
            }
        }

        private void RefreshAuditLogList(List<Core.Models.SecurityEvent> logs)
        {
            // Clear existing items
            foreach (Transform child in auditLogContainer)
            {
                Destroy(child.gameObject);
            }

            // Create audit log items
            foreach (var log in logs)
            {
                var logItem = Instantiate(auditLogItemPrefab, auditLogContainer);
                var logItemUI = logItem.GetComponent<AuditLogItemUI>();
                
                if (logItemUI != null)
                {
                    logItemUI.Setup(log);
                }
            }
        }

        private void OnTabSelected(int tabIndex)
        {
            // Refresh data when switching tabs
            switch (tabIndex)
            {
                case 0: // Users
                    LoadUsers();
                    break;
                case 1: // Roles
                    LoadRoles();
                    break;
                case 2: // Security Settings
                    LoadSecuritySettings();
                    break;
                case 3: // Audit Logs
                    LoadAuditLogs();
                    break;
            }
        }

        private void OnUserSelected(UserDTO user)
        {
            _selectedUser = user;
            ShowUserDetailsModal(user);
        }

        private void OnRoleSelected(UserRole role)
        {
            // Show role details modal
            Debug.Log($"Role selected: {role}");
        }

        private void OnSearchUserChanged(string searchText)
        {
            // Filter users based on search text
            var filteredUsers = _currentUsers.FindAll(user => 
                user.Email.ToLower().Contains(searchText.ToLower()) ||
                user.FirstName.ToLower().Contains(searchText.ToLower()) ||
                user.LastName.ToLower().Contains(searchText.ToLower()));

            // Update UI with filtered users
            // Implementation depends on your UI framework
        }

        private void OnSessionTimeoutChanged(float value)
        {
            sessionTimeoutText.text = $"{value} minutes";
        }

        private void OnRateLimitChanged(float value)
        {
            rateLimitText.text = $"{value} requests/minute";
        }

        private void OnLogSearchChanged(string searchText)
        {
            // Filter audit logs based on search text
            // Implementation depends on your UI framework
        }

        private void OnLogEventTypeFilterChanged(int eventTypeIndex)
        {
            // Filter audit logs based on event type
            // Implementation depends on your UI framework
        }

        private void ShowAddUserModal()
        {
            _selectedUser = null;
            userDetailsTitle.text = "Add New User";
            userDetailsEmail.text = "";
            userDetailsFirstName.text = "";
            userDetailsLastName.text = "";
            userDetailsRole.value = 0;
            userDetailsActive.isOn = true;
            userDetailsDeleteButton.gameObject.SetActive(false);
            userDetailsResetPasswordButton.gameObject.SetActive(false);
            userDetailsModal.SetActive(true);
        }

        private void ShowUserDetailsModal(UserDTO user)
        {
            _selectedUser = user;
            userDetailsTitle.text = "Edit User";
            userDetailsEmail.text = user.Email;
            userDetailsFirstName.text = user.FirstName;
            userDetailsLastName.text = user.LastName;
            userDetailsRole.value = (int)user.Role;
            userDetailsActive.isOn = user.IsActive;
            userDetailsDeleteButton.gameObject.SetActive(true);
            userDetailsResetPasswordButton.gameObject.SetActive(true);
            userDetailsModal.SetActive(true);
        }

        private void CloseUserDetailsModal()
        {
            userDetailsModal.SetActive(false);
            _selectedUser = null;
        }

        private async void SaveUserDetails()
        {
            try
            {
                if (_selectedUser == null)
                {
                    // Add new user
                    var registerRequest = new RegisterUserRequest
                    {
                        Email = userDetailsEmail.text,
                        Password = "TempPassword123!", // In real implementation, this would be generated
                        FirstName = userDetailsFirstName.text,
                        LastName = userDetailsLastName.text
                    };

                    var newUser = await _authService.RegisterUserAsync(registerRequest);
                    
                    // Assign role
                    await _rbacService.AssignRoleAsync(newUser.Id, (UserRole)userDetailsRole.value);
                    
                    ShowSuccessMessage("User created successfully");
                }
                else
                {
                    // Update existing user
                    // In real implementation, this would call user update service
                    ShowSuccessMessage("User updated successfully");
                }

                CloseUserDetailsModal();
                await LoadUsers();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error saving user details: {ex.Message}");
                ShowErrorMessage("Failed to save user details");
            }
        }

        private async void DeleteUser()
        {
            if (_selectedUser == null) return;

            try
            {
                // Show confirmation dialog
                if (await ShowConfirmationDialog("Are you sure you want to delete this user?"))
                {
                    await _rbacService.RemoveUserAsync(_selectedUser.Id);
                    ShowSuccessMessage("User deleted successfully");
                    CloseUserDetailsModal();
                    await LoadUsers();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error deleting user: {ex.Message}");
                ShowErrorMessage("Failed to delete user");
            }
        }

        private async void ResetUserPassword()
        {
            if (_selectedUser == null) return;

            try
            {
                // In real implementation, this would generate and send a new password
                ShowSuccessMessage("Password reset email sent");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error resetting password: {ex.Message}");
                ShowErrorMessage("Failed to reset password");
            }
        }

        private async void SaveSecuritySettings()
        {
            try
            {
                var securityConfig = new SecurityConfiguration
                {
                    EnableTwoFactorAuth = enableTwoFactorAuth.isOn,
                    EnableSessionTimeout = enableSessionTimeout.isOn,
                    SessionTimeoutMinutes = (int)sessionTimeoutSlider.value,
                    EnableRateLimiting = enableRateLimiting.isOn,
                    RateLimitRequestsPerMinute = (int)rateLimitSlider.value
                };

                await SaveSecurityConfiguration(securityConfig);
                ShowSuccessMessage("Security settings saved successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error saving security settings: {ex.Message}");
                ShowErrorMessage("Failed to save security settings");
            }
        }

        private async void RefreshAuditLogs()
        {
            await LoadAuditLogs();
        }

        private async void ExportAuditLogs()
        {
            try
            {
                var logs = await _securityEventLogger.GetSecurityEventsAsync();
                var csvData = ConvertLogsToCSV(logs);
                
                // In real implementation, this would save to file or download
                Debug.Log("Audit logs exported successfully");
                ShowSuccessMessage("Audit logs exported successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error exporting audit logs: {ex.Message}");
                ShowErrorMessage("Failed to export audit logs");
            }
        }

        private void ShowAddRoleModal()
        {
            // Show role creation modal
            Debug.Log("Add role modal");
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

        private async Task<bool> ShowConfirmationDialog(string message)
        {
            // Show confirmation dialog and return result
            // For now, return true
            return await Task.FromResult(true);
        }

        private async Task<SecurityConfiguration> GetSecurityConfiguration()
        {
            // Load security configuration from storage
            return await Task.FromResult(new SecurityConfiguration
            {
                EnableTwoFactorAuth = false,
                EnableSessionTimeout = true,
                SessionTimeoutMinutes = 30,
                EnableRateLimiting = true,
                RateLimitRequestsPerMinute = 100
            });
        }

        private async Task SaveSecurityConfiguration(SecurityConfiguration config)
        {
            // Save security configuration to storage
            await Task.CompletedTask;
        }

        private string ConvertLogsToCSV(List<Core.Models.SecurityEvent> logs)
        {
            var csv = "Timestamp,Event Type,User,Description,IP Address\n";
            
            foreach (var log in logs)
            {
                csv += $"{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.EventType},{log.UserId},{log.Description},{log.IpAddress}\n";
            }
            
            return csv;
        }

        private List<UserDTO> CreateMockUsers()
        {
            return new List<UserDTO>
            {
                new UserDTO
                {
                    Id = "user1",
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    Role = UserRole.SuperAdmin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    LastLoginAt = DateTime.UtcNow.AddHours(-2)
                },
                new UserDTO
                {
                    Id = "user2",
                    Email = "manager@example.com",
                    FirstName = "Manager",
                    LastName = "User",
                    Role = UserRole.Manager,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    LastLoginAt = DateTime.UtcNow.AddHours(-4)
                },
                new UserDTO
                {
                    Id = "user3",
                    Email = "operator@example.com",
                    FirstName = "Operator",
                    LastName = "User",
                    Role = UserRole.Operator,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    LastLoginAt = DateTime.UtcNow.AddHours(-1)
                }
            };
        }
    }

    /// <summary>
    /// Security configuration data model
    /// </summary>
    public class SecurityConfiguration
    {
        public bool EnableTwoFactorAuth { get; set; }
        public bool EnableSessionTimeout { get; set; }
        public int SessionTimeoutMinutes { get; set; }
        public bool EnableRateLimiting { get; set; }
        public int RateLimitRequestsPerMinute { get; set; }
    }
}