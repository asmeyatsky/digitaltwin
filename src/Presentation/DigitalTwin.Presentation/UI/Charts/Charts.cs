using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using DigitalTwin.Core.DTOs;

namespace DigitalTwin.Presentation.UI.Charts
{
    /// <summary>
    /// Base class for all chart components
    /// </summary>
    public abstract class BaseChart : MonoBehaviour
    {
        [Header("Chart Components")]
        [SerializeField] protected TMP_Text titleText;
        [SerializeField] protected RectTransform chartContainer;
        [SerializeField] protected GameObject legendContainer;
        [SerializeField] protected Button exportButton;
        [SerializeField] protected Button fullscreenButton;

        protected string _chartTitle;
        protected List<MetricTrend> _data;
        protected List<ChartDataPoint> _chartData;

        protected virtual void Start()
        {
            exportButton.onClick.AddListener(ExportChart);
            fullscreenButton.onClick.AddListener(ToggleFullscreen);
        }

        public virtual void SetTitle(string title)
        {
            _chartTitle = title;
            titleText.text = title;
        }

        public abstract void SetData(object data);

        protected virtual void ExportChart()
        {
            Debug.Log($"Exporting chart: {_chartTitle}");
        }

        protected virtual void ToggleFullscreen()
        {
            Debug.Log($"Toggling fullscreen for chart: {_chartTitle}");
        }

        protected virtual Color GetColorForIndex(int index)
        {
            var colors = new Color[]
            {
                Color.red, Color.blue, Color.green, Color.yellow, 
                Color.cyan, Color.magenta, Color.white, Color.gray
            };
            return colors[index % colors.Length];
        }

        protected virtual string FormatValue(double value, string unit = "")
        {
            if (value >= 1000000)
                return $"{value / 1000000:F1}M{unit}";
            if (value >= 1000)
                return $"{value / 1000:F1}K{unit}";
            return $"{value:F1}{unit}";
        }
    }

    /// <summary>
    /// Line chart for time series data
    /// </summary>
    public class LineChart : BaseChart
    {
        [Header("Line Chart Settings")]
        [SerializeField] private GameObject linePointPrefab;
        [SerializeField] private RectTransform lineContainer;
        [SerializeField] private bool showDataPoints = true;
        [SerializeField] private bool showGrid = true;
        [SerializeField] private AnimationCurve interpolationCurve = AnimationCurve.Linear;

        private List<GameObject> _linePoints = new List<GameObject>();
        private List<LineRenderer> _lines = new List<LineRenderer>();

        public override void SetData(object data)
        {
            if (data is List<MetricTrend> trends)
            {
                _data = trends;
                RenderChart();
            }
        }

        private void RenderChart()
        {
            ClearChart();

            if (_data == null || _data.Count == 0) return;

            // Create data points
            var points = new List<Vector3>();
            for (int i = 0; i < _data.Count; i++)
            {
                var x = (float)i / (_data.Count - 1) * chartContainer.rect.width;
                var y = NormalizeValue(_data[i].Value) * chartContainer.rect.height;
                var point = new Vector3(x, y, 0);
                points.Add(point);

                if (showDataPoints)
                {
                    CreateDataPoint(point, _data[i]);
                }
            }

            // Create line
            if (points.Count > 1)
            {
                CreateLine(points);
            }

            // Update legend
            UpdateLegend();
        }

        private void CreateDataPoint(Vector3 position, MetricTrend trend)
        {
            var point = Instantiate(linePointPrefab, lineContainer);
            point.transform.localPosition = position;
            
            var pointUI = point.GetComponent<ChartDataPointUI>();
            if (pointUI != null)
            {
                pointUI.Setup(trend.Timestamp, trend.Value, trend.Unit);
            }

            _linePoints.Add(point);
        }

        private void CreateLine(List<Vector3> points)
        {
            var lineObject = new GameObject("Line");
            lineObject.transform.SetParent(lineContainer);
            
            var lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.color = GetColorForIndex(0);
            lineRenderer.widthMultiplier = 2f;
            lineRenderer.positionCount = points.Count;
            
            for (int i = 0; i < points.Count; i++)
            {
                lineRenderer.SetPosition(i, points[i]);
            }

            _lines.Add(lineRenderer);
        }

        private float NormalizeValue(double value)
        {
            if (_data.Count == 0) return 0;

            var minValue = _data.Min(t => t.Value);
            var maxValue = _data.Max(t => t.Value);
            var range = maxValue - minValue;
            
            if (range == 0) return 0.5f;
            
            return (float)((value - minValue) / range);
        }

        private void ClearChart()
        {
            foreach (var point in _linePoints)
            {
                Destroy(point);
            }
            _linePoints.Clear();

            foreach (var line in _lines)
            {
                Destroy(line.gameObject);
            }
            _lines.Clear();
        }

        private void UpdateLegend()
        {
            // Update legend based on data
            // Implementation depends on specific legend UI structure
        }
    }

    /// <summary>
    /// Bar chart for categorical data
    /// </summary>
    public class BarChart : BaseChart
    {
        [Header("Bar Chart Settings")]
        [SerializeField] private GameObject barPrefab;
        [SerializeField] private RectTransform barsContainer;
        [SerializeField] private bool showValues = true;
        [SerializeField] private bool horizontal = false;
        [SerializeField] private float barSpacing = 0.1f;

        private List<GameObject> _bars = new List<GameObject>();

        public override void SetData(object data)
        {
            if (data is List<MetricTrend> trends)
            {
                _data = trends;
                _chartData = ConvertToChartDataPoints(trends);
                RenderChart();
            }
            else if (data is List<ChartDataPoint> chartData)
            {
                _chartData = chartData;
                RenderChart();
            }
        }

        private void RenderChart()
        {
            ClearChart();

            if (_chartData == null || _chartData.Count == 0) return;

            var barWidth = (chartContainer.rect.width / _chartData.Count) * (1 - barSpacing);
            var maxHeight = chartContainer.rect.height;

            for (int i = 0; i < _chartData.Count; i++)
            {
                var bar = CreateBar(i, barWidth, maxHeight);
                _bars.Add(bar);
            }

            UpdateLegend();
        }

        private GameObject CreateBar(int index, float width, float maxHeight)
        {
            var bar = Instantiate(barPrefab, barsContainer);
            
            var barRect = bar.GetComponent<RectTransform>();
            var barValue = _chartData[index].Value;
            var maxValue = _chartData.Max(d => d.Value);
            var normalizedHeight = (float)(barValue / maxValue) * maxHeight;
            
            if (horizontal)
            {
                barRect.sizeDelta = new Vector2(normalizedHeight, width);
                barRect.anchoredPosition = new Vector2(normalizedHeight / 2, -index * (width + barSpacing * width) - width / 2);
            }
            else
            {
                barRect.sizeDelta = new Vector2(width, normalizedHeight);
                barRect.anchoredPosition = new Vector2(index * (width + barSpacing * width) + width / 2, normalizedHeight / 2);
            }

            // Set bar color
            var barImage = bar.GetComponent<Image>();
            if (barImage != null)
            {
                barImage.color = _chartData[index].Color;
            }

            // Add value label
            if (showValues)
            {
                AddValueLabel(bar, barValue, _chartData[index].Label);
            }

            return bar;
        }

        private void AddValueLabel(GameObject bar, double value, string label)
        {
            var labelObject = new GameObject("ValueLabel");
            labelObject.transform.SetParent(bar.transform);
            
            var labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(100, 20);
            labelRect.anchoredPosition = new Vector2(0, 25);
            
            var labelComponent = labelObject.AddComponent<TMPro.TextMeshProUGUI>();
            labelComponent.text = $"{FormatValue(value)}\n{label}";
            labelComponent.fontSize = 10;
            labelComponent.alignment = TMPro.TextAlignmentOptions.Center;
        }

        private List<ChartDataPoint> ConvertToChartDataPoints(List<MetricTrend> trends)
        {
            return trends.Select(t => new ChartDataPoint
            {
                Label = t.Timestamp.ToString("MM/dd"),
                Value = t.Value,
                Color = GetColorForIndex(0)
            }).ToList();
        }

        private void ClearChart()
        {
            foreach (var bar in _bars)
            {
                Destroy(bar);
            }
            _bars.Clear();
        }

        private void UpdateLegend()
        {
            // Update legend based on data
        }
    }

    /// <summary>
    /// Pie chart for proportional data
    /// </summary>
    public class PieChart : BaseChart
    {
        [Header("Pie Chart Settings")]
        [SerializeField] private GameObject slicePrefab;
        [SerializeField] private RectTransform slicesContainer;
        [SerializeField] private bool showPercentages = true;
        [SerializeField] private float sliceSpacing = 2f;

        private List<GameObject> _slices = new List<GameObject>();

        public override void SetData(object data)
        {
            if (data is List<ChartDataPoint> chartData)
            {
                _chartData = chartData;
                RenderChart();
            }
        }

        private void RenderChart()
        {
            ClearChart();

            if (_chartData == null || _chartData.Count == 0) return;

            var total = _chartData.Sum(d => d.Value);
            var currentAngle = 0f;

            for (int i = 0; i < _chartData.Count; i++)
            {
                var slice = CreateSlice(i, currentAngle, total);
                _slices.Add(slice);
                
                var sliceAngle = (float)(_chartData[i].Value / total) * 360f;
                currentAngle += sliceAngle;
            }

            UpdateLegend();
        }

        private GameObject CreateSlice(int index, float startAngle, double total)
        {
            var slice = Instantiate(slicePrefab, slicesContainer);
            
            var sliceImage = slice.GetComponent<Image>();
            if (sliceImage != null)
            {
                sliceImage.color = _chartData[index].Color;
            }

            // Calculate slice size
            var sliceAngle = (float)(_chartData[index].Value / total) * 360f - sliceSpacing;
            var percentage = (float)(_chartData[index].Value / total);
            
            // Create slice shape (simplified - in production, use proper pie slice mesh)
            var sliceRect = slice.GetComponent<RectTransform>();
            var radius = Mathf.Min(chartContainer.rect.width, chartContainer.rect.height) / 2;
            sliceRect.sizeDelta = new Vector2(radius * 2, radius * 2);
            
            // Apply rotation
            slice.transform.localRotation = Quaternion.Euler(0, 0, startAngle);

            // Add label
            if (showPercentages)
            {
                AddSliceLabel(slice, percentage, _chartData[index].Label, startAngle + sliceAngle / 2);
            }

            return slice;
        }

        private void AddSliceLabel(GameObject slice, float percentage, string label, float angle)
        {
            var labelObject = new GameObject("SliceLabel");
            labelObject.transform.SetParent(slice.transform);
            
            var labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(80, 20);
            
            // Position label at middle of slice
            var radius = 50f; // Distance from center
            var angleRad = angle * Mathf.Deg2Rad;
            labelRect.anchoredPosition = new Vector2(
                Mathf.Cos(angleRad) * radius,
                Mathf.Sin(angleRad) * radius
            );
            
            var labelComponent = labelObject.AddComponent<TMPro.TextMeshProUGUI>();
            labelComponent.text = $"{percentage:P1}\n{label}";
            labelComponent.fontSize = 9;
            labelComponent.alignment = TMPro.TextAlignmentOptions.Center;
        }

        private void ClearChart()
        {
            foreach (var slice in _slices)
            {
                Destroy(slice);
            }
            _slices.Clear();
        }

        private void UpdateLegend()
        {
            // Create legend items
            for (int i = 0; i < _chartData.Count; i++)
            {
                CreateLegendItem(_chartData[i], i);
            }
        }

        private void CreateLegendItem(ChartDataPoint dataPoint, int index)
        {
            var legendItem = new GameObject($"LegendItem_{index}");
            legendItem.transform.SetParent(legendContainer.transform);
            
            var itemRect = legendItem.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(150, 20);
            itemRect.anchoredPosition = new Vector2(0, -index * 25);
            
            // Color indicator
            var colorIndicator = new GameObject("ColorIndicator");
            colorIndicator.transform.SetParent(legendItem.transform);
            var colorRect = colorIndicator.AddComponent<RectTransform>();
            colorRect.sizeDelta = new Vector2(15, 15);
            colorRect.anchoredPosition = new Vector2(-60, 0);
            
            var colorImage = colorIndicator.AddComponent<Image>();
            colorImage.color = dataPoint.Color;
            
            // Label
            var label = new GameObject("Label");
            label.transform.SetParent(legendItem.transform);
            var labelRect = label.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(120, 20);
            labelRect.anchoredPosition = new Vector2(10, 0);
            
            var labelComponent = label.AddComponent<TMPro.TextMeshProUGUI>();
            labelComponent.text = $"{dataPoint.Label} ({FormatValue(dataPoint.Value)})";
            labelComponent.fontSize = 10;
            labelComponent.alignment = TMPro.TextAlignmentOptions.Left;
        }
    }

    /// <summary>
    /// Gauge chart for displaying single values with ranges
    /// </summary>
    public class GaugeChart : BaseChart
    {
        [Header("Gauge Chart Settings")]
        [SerializeField] private RectTransform needle;
        [SerializeField] private Image gaugeFill;
        [SerializeField] private List<Color> rangeColors = new List<Color>();
        [SerializeField] private List<float> rangeValues = new List<float>();
        [SerializeField] private float minValue = 0;
        [SerializeField] private float maxValue = 100;
        [SerializeField] private TMP_Text valueText;

        private double _currentValue;

        public override void SetData(object data)
        {
            if (data is double value)
            {
                SetValue(value);
            }
            else if (data is float floatValue)
            {
                SetValue(floatValue);
            }
        }

        public void SetValue(double value)
        {
            _currentValue = value;
            UpdateGauge();
        }

        private void UpdateGauge()
        {
            // Update value text
            if (valueText != null)
            {
                valueText.text = FormatValue(_currentValue);
            }

            // Update needle rotation
            if (needle != null)
            {
                var normalizedValue = (float)((_currentValue - minValue) / (maxValue - minValue));
                var angle = Mathf.Lerp(-90, 90, normalizedValue);
                needle.rotation = Quaternion.Euler(0, 0, angle);
            }

            // Update gauge fill color based on range
            if (gaugeFill != null)
            {
                var color = GetColorForValue(_currentValue);
                gaugeFill.color = color;
            }
        }

        private Color GetColorForValue(double value)
        {
            for (int i = 0; i < rangeValues.Count; i++)
            {
                if (value <= rangeValues[i])
                {
                    return rangeColors[Mathf.Min(i, rangeColors.Count - 1)];
                }
            }
            return rangeColors[rangeColors.Count - 1];
        }
    }

    /// <summary>
    /// Area chart for filled line charts
    /// </summary>
    public class AreaChart : BaseChart
    {
        [Header("Area Chart Settings")]
        [SerializeField] private RectTransform areaContainer;
        [SerializeField] private float areaOpacity = 0.3f;

        private LineRenderer _areaLine;
        private MeshRenderer _areaFill;

        public override void SetData(object data)
        {
            if (data is List<MetricTrend> trends)
            {
                _data = trends;
                RenderChart();
            }
        }

        private void RenderChart()
        {
            ClearChart();

            if (_data == null || _data.Count == 0) return;

            // Create area points
            var points = new List<Vector3>();
            for (int i = 0; i < _data.Count; i++)
            {
                var x = (float)i / (_data.Count - 1) * chartContainer.rect.width;
                var y = NormalizeValue(_data[i].Value) * chartContainer.rect.height;
                points.Add(new Vector3(x, y, 0));
            }

            // Add bottom corners to close the area
            points.Add(new Vector3(points[points.Count - 1].x, 0, 0));
            points.Add(new Vector3(0, 0, 0));

            CreateArea(points);
            CreateLine(points.GetRange(0, _data.Count));
        }

        private void CreateArea(List<Vector3> points)
        {
            var areaObject = new GameObject("AreaFill");
            areaObject.transform.SetParent(areaContainer);
            
            // Create mesh for area fill
            var meshFilter = areaObject.AddComponent<MeshFilter>();
            var meshRenderer = areaObject.AddComponent<MeshRenderer>();
            
            var mesh = new Mesh();
            mesh.vertices = points.ToArray();
            
            // Create triangles for the mesh
            var triangles = new List<int>();
            for (int i = 0; i < points.Count - 2; i++)
            {
                triangles.Add(0);
                triangles.Add(i + 1);
                triangles.Add(i + 2);
            }
            mesh.triangles = triangles.ToArray();
            
            meshFilter.mesh = mesh;
            _areaFill = meshRenderer;
            
            // Set material with transparency
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(GetColorForIndex(0).r, GetColorForIndex(0).g, GetColorForIndex(0).b, areaOpacity);
            meshRenderer.material = material;
        }

        private void CreateLine(List<Vector3> points)
        {
            var lineObject = new GameObject("AreaLine");
            lineObject.transform.SetParent(areaContainer);
            
            _areaLine = lineObject.AddComponent<LineRenderer>();
            _areaLine.material = new Material(Shader.Find("Sprites/Default"));
            _areaLine.color = GetColorForIndex(0);
            _areaLine.widthMultiplier = 2f;
            _areaLine.positionCount = points.Count;
            
            for (int i = 0; i < points.Count; i++)
            {
                _areaLine.SetPosition(i, points[i]);
            }
        }

        private float NormalizeValue(double value)
        {
            if (_data.Count == 0) return 0;

            var minValue = _data.Min(t => t.Value);
            var maxValue = _data.Max(t => t.Value);
            var range = maxValue - minValue;
            
            if (range == 0) return 0.5f;
            
            return (float)((value - minValue) / range);
        }

        private void ClearChart()
        {
            if (_areaLine != null)
            {
                Destroy(_areaLine.gameObject);
            }
            
            if (_areaFill != null)
            {
                Destroy(_areaFill.gameObject);
            }
        }
    }

    /// <summary>
    /// UI component for chart data points
    /// </summary>
    public class ChartDataPointUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text value;
        [SerializeField] private GameObject tooltip;

        public void Setup(DateTime timestamp, double dataValue, string unit)
        {
            label.text = timestamp.ToString("MM/dd HH:mm");
            value.text = $"{dataValue:F1}{unit}";
        }

        public void ShowTooltip()
        {
            if (tooltip != null)
            {
                tooltip.SetActive(true);
            }
        }

        public void HideTooltip()
        {
            if (tooltip != null)
            {
                tooltip.SetActive(false);
            }
        }
    }

    /// <summary>
    /// UI component for alert items
    /// </summary>
    public class AlertItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Image severityIndicator;
        [SerializeField] private Button acknowledgeButton;

        private Alert _alert;

        public void Setup(Alert alert)
        {
            _alert = alert;
            
            titleText.text = alert.Title;
            descriptionText.text = alert.Description;
            timeText.text = alert.Timestamp.ToString("HH:mm");
            
            // Set severity color
            severityIndicator.color = GetSeverityColor(alert.Severity);
            
            // Setup button
            acknowledgeButton.onClick.AddListener(AcknowledgeAlert);
        }

        private Color GetSeverityColor(string severity)
        {
            return severity.ToLower() switch
            {
                "high" => Color.red,
                "medium" => Color.yellow,
                "low" => Color.green,
                _ => Color.gray
            };
        }

        private void AcknowledgeAlert()
        {
            // Acknowledge alert logic
            Debug.Log($"Acknowledged alert: {_alert.Id}");
        }
    }

    /// <summary>
    /// UI component for insight items
    /// </summary>
    public class InsightItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text insightText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private Image typeIcon;

        public void Setup(string content, string type)
        {
            insightText.text = content;
            typeText.text = type;
            
            // Set icon based on type
            typeIcon.color = GetTypeColor(type);
        }

        private Color GetTypeColor(string type)
        {
            return type.ToLower() switch
            {
                "recommendation" => Color.blue,
                "warning" => Color.yellow,
                "error" => Color.red,
                _ => Color.gray
            };
        }
    }
}