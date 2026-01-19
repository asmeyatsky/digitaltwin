using UnityEngine;

namespace DigitalTwin.Presentation.UI
{
    /// <summary>
    /// Digital Twin UI Configuration
    /// 
    /// Architectural Intent:
    /// - Centralizes all UI settings and configurations
    /// - Provides easy customization of visual appearance
    /// - Enables different themes and display modes
    /// - Supports localization and accessibility settings
    /// </summary>
    [CreateAssetMenu(fileName = "DigitalTwinUIConfig", menuName = "Digital Twin/UI Configuration")]
    public class DigitalTwinUIConfig : ScriptableObject
    {
        [Header("Color Scheme")]
        [SerializeField] public ColorScheme primaryColorScheme = ColorScheme.Blue;
        [SerializeField] public Color customPrimaryColor = new Color(0.2f, 0.6f, 1.0f, 1.0f);
        [SerializeField] public Color customSecondaryColor = new Color(0.8f, 0.8f, 0.9f, 1.0f);
        [SerializeField] public Color customAccentColor = new Color(0.2f, 0.8f, 0.4f, 1.0f);

        [Header("Status Colors")]
        [SerializeField] public Color successColor = Color.green;
        [SerializeField] public Color warningColor = Color.yellow;
        [SerializeField] public Color errorColor = Color.red;
        [SerializeField] public Color infoColor = Color.blue;
        [SerializeField] public Color offlineColor = Color.gray;

        [Header("Animation Settings")]
        [SerializeField] public bool enableAnimations = true;
        [SerializeField] public float animationDuration = 0.3f;
        [SerializeField] public float fadeInDuration = 0.2f;
        [SerializeField] public float slideInDuration = 0.4f;

        [Header("Chart Settings")]
        [SerializeField] public int maxDataPoints = 100;
        [SerializeField] public bool showGridLines = true;
        [SerializeField] public bool showDataLabels = true;
        [SerializeField] public float chartUpdateInterval = 1.0f;

        [Header("Dashboard Layout")]
        [SerializeField] public DashboardLayout defaultLayout = DashboardLayout.Split;
        [SerializeField] public bool enableCustomLayouts = true;
        [SerializeField] public bool showPanelHeaders = true;
        [SerializeField] public bool enablePanelResizing = true;

        [Header("Data Display")]
        [SerializeField] public bool enableRealTimeUpdates = true;
        [SerializeField] public float dataRefreshRate = 5.0f;
        [SerializeField] public bool showDataQuality = true;
        [SerializeField] public bool showTrendIndicators = true;

        [Header("Alert Settings")]
        [SerializeField] public bool enableAudioAlerts = false;
        [SerializeField] public bool enableVisualNotifications = true;
        [SerializeField] public float notificationDuration = 5.0f;
        [SerializeField] public bool enableAutoDismissal = true;

        [Header("Accessibility")]
        [SerializeField] public int defaultFontSize = 14;
        [SerializeField] public bool enableHighContrast = false;
        [SerializeField] public bool enableLargeText = false;
        [SerializeField] public bool enableScreenReader = false;

        [Header("Performance")]
        [SerializeField] public bool enableOptimizedRendering = true;
        [SerializeField] public int maxVisibleSensors = 50;
        [SerializeField] public bool enableDataCaching = true;
        [SerializeField] public int cacheRefreshInterval = 60;

        public Color GetPrimaryColor()
        {
            return primaryColorScheme switch
            {
                ColorScheme.Blue => new Color(0.2f, 0.6f, 1.0f, 1.0f),
                ColorScheme.Green => new Color(0.2f, 0.8f, 0.4f, 1.0f),
                ColorScheme.Orange => new Color(1.0f, 0.6f, 0.2f, 1.0f),
                ColorScheme.Purple => new Color(0.6f, 0.2f, 0.8f, 1.0f),
                ColorScheme.Custom => customPrimaryColor,
                _ => new Color(0.2f, 0.6f, 1.0f, 1.0f)
            };
        }

        public Color GetSecondaryColor()
        {
            return primaryColorScheme switch
            {
                ColorScheme.Blue => new Color(0.8f, 0.8f, 0.9f, 1.0f),
                ColorScheme.Green => new Color(0.9f, 0.9f, 0.8f, 1.0f),
                ColorScheme.Orange => new Color(0.9f, 0.8f, 0.8f, 1.0f),
                ColorScheme.Purple => new Color(0.9f, 0.8f, 0.9f, 1.0f),
                ColorScheme.Custom => customSecondaryColor,
                _ => new Color(0.8f, 0.8f, 0.9f, 1.0f)
            };
        }

        public Color GetAccentColor()
        {
            return primaryColorScheme switch
            {
                ColorScheme.Blue => new Color(0.2f, 0.8f, 0.4f, 1.0f),
                ColorScheme.Green => new Color(0.4f, 0.8f, 0.2f, 1.0f),
                ColorScheme.Orange => new Color(0.8f, 0.4f, 0.2f, 1.0f),
                ColorScheme.Purple => new Color(0.8f, 0.2f, 0.6f, 1.0f),
                ColorScheme.Custom => customAccentColor,
                _ => new Color(0.2f, 0.8f, 0.4f, 1.0f)
            };
        }

        public int GetFontSize()
        {
            return enableLargeText ? defaultFontSize + 2 : defaultFontSize;
        }

        public Color GetBackgroundColor()
        {
            return enableHighContrast ? Color.black : new Color(0.1f, 0.1f, 0.15f, 1.0f);
        }

        public Color GetTextColor()
        {
            return enableHighContrast ? Color.white : new Color(0.9f, 0.9f, 0.9f, 1.0f);
        }
    }

    /// <summary>
    /// UI Color Schemes
    /// </summary>
    public enum ColorScheme
    {
        Blue,
        Green,
        Orange,
        Purple,
        Custom
    }

    /// <summary>
    /// Dashboard Layout Options
    /// </summary>
    public enum DashboardLayout
    {
        Split,      // Left panels, right visualization
        Tabbed,     // Tabbed interface
        Overlay,     // Floating panels
        Grid,       // Grid layout
        Fullscreen,  // Full-screen panels
        Compact     // Minimalist layout
    }

    /// <summary>
    /// UI Theme Manager
    /// </summary>
    public class UIThemeManager : MonoBehaviour
    {
        [SerializeField] private DigitalTwinUIConfig _config;
        [SerializeField] private Material[] _themeMaterials;

        private void Start()
        {
            if (_config != null)
            {
                ApplyTheme();
            }
        }

        public void ApplyTheme()
        {
            ApplyColorScheme();
            ApplyFontSize();
            ApplyAccessibility();
            ApplyPerformanceSettings();
        }

        private void ApplyColorScheme()
        {
            var primaryColor = _config.GetPrimaryColor();
            var secondaryColor = _config.GetSecondaryColor();
            var accentColor = _config.GetAccentColor();

            // Update UI elements with new colors
            var uiElements = FindObjectsOfType<UnityEngine.UI.Image>();
            foreach (var element in uiElements)
            {
                // Apply colors based on element type or naming convention
                if (element.gameObject.name.Contains("Primary"))
                    element.color = primaryColor;
                else if (element.gameObject.name.Contains("Secondary"))
                    element.color = secondaryColor;
                else if (element.gameObject.name.Contains("Accent"))
                    element.color = accentColor;
            }
        }

        private void ApplyFontSize()
        {
            var fontSize = _config.GetFontSize();
            var textElements = FindObjectsOfType<TextMeshProUGUI>();
            
            foreach (var text in textElements)
            {
                // Skip elements that should maintain their own size
                if (!text.gameObject.name.Contains("Header") && !text.gameObject.name.Contains("Title"))
                {
                    text.fontSize = fontSize;
                }
            }
        }

        private void ApplyAccessibility()
        {
            // Apply high contrast mode
            if (_config.enableHighContrast)
            {
                var panels = FindObjectsOfType<UnityEngine.UI.Image>();
                foreach (var panel in panels)
                {
                    if (panel.gameObject.name.Contains("Panel") || panel.gameObject.name.Contains("Background"))
                    {
                        panel.color = _config.GetBackgroundColor();
                    }
                }
            }
        }

        private void ApplyPerformanceSettings()
        {
            if (_config.enableOptimizedRendering)
            {
                // Reduce update rates for non-critical elements
                QualitySettings.SetQualityLevel(QualityLevel.Fast);
            }
        }

        public void SetColorScheme(ColorScheme scheme)
        {
            // Update the config and apply new theme
            // This would typically require a runtime config system
            ApplyTheme();
        }
    }
}