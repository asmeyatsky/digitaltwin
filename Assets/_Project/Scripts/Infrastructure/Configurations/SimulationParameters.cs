using System;
using UnityEngine;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Infrastructure.Configurations
{
    /// <summary>
    /// Simulation Parameters ScriptableObject
    /// 
    /// Architectural Intent:
    /// - Provides configurable parameters for building simulations
    /// - Enables data-driven simulation setup and customization
    /// - Supports different simulation scenarios and presets
    /// - Maintains separation between configuration and logic
    /// 
    /// Key Design Decisions:
    /// 1. ScriptableObject for reusable parameter sets
    /// 2. Preset system for common simulation types
    /// 3. Validation methods for parameter constraints
    /// 4. Runtime parameter adjustment support
    /// </summary>
    [CreateAssetMenu(fileName = "SimulationParameters", menuName = "Digital Twin/Simulation Parameters")]
    public class SimulationParametersScriptableObject : ScriptableObject
    {
        [Header("Basic Parameters")]
        [SerializeField] private SimulationPreset _preset = SimulationPreset.Custom;
        [SerializeField] private float _timeStepMinutes = 15f;
        [SerializeField] private bool _enableRealTimeData = true;
        [SerializeField] private bool _useHistoricalData = true;
        [SerializeField] private int _randomSeed = 0;
        
        [Header("Energy Simulation")]
        [SerializeField] private bool _enableEnergySimulation = true;
        [SerializeField] private float _energyVariationFactor = 0.2f;
        [SerializeField] private bool _enableSeasonalEffects = true;
        [SerializeField] private bool _enableTimeOfDayEffects = true;
        [SerializeField] private bool _enableOccupancyEffects = true;
        
        [Header("Environmental Simulation")]
        [SerializeField] private bool _enableEnvironmentalSimulation = true;
        [SerializeField] private float _targetTemperature = 22f;
        [SerializeField] private float _targetHumidity = 45f;
        [SerializeField] private float _targetLightLevel = 500f;
        [SerializeField] private bool _enableHVACSimulation = true;
        [SerializeField] private bool _enableLightingSimulation = true;
        
        [Header("Equipment Simulation")]
        [SerializeField] private bool _enableEquipmentSimulation = true;
        [SerializeField] private bool _enableFailureSimulation = true;
        [SerializeField] private float _failureProbability = 0.01f;
        [SerializeField] private bool _enableMaintenanceSimulation = true;
        [SerializeField] private float _degradationRate = 0.001f;
        
        [Header("Occupancy Simulation")]
        [SerializeField] private bool _enableOccupancySimulation = true;
        [SerializeField] private int _maxOccupancy = 100;
        [SerializeField] private float _baseOccupancyRate = 0.7f;
        [SerializeField] private bool _enableTimeBasedPatterns = true;
        [SerializeField] private bool _enableSeasonalVariation = true;
        
        [Header("Optimization")]
        [SerializeField] private bool _enableOptimization = false;
        [SerializeField] private OptimizationObjective _objective = OptimizationObjective.EnergyEfficiency;
        [SerializeField] private int _maxIterations = 1000;
        [SerializeField] private float _tolerance = 0.001f;
        
        [Header("Advanced Settings")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxConcurrentSimulations = 5;
        [SerializeField] private float _simulationTimeoutMinutes = 30f;
        [SerializeField] private bool _enableParallelProcessing = true;

        // Properties
        public SimulationPreset Preset => _preset;
        public float TimeStepMinutes => _timeStepMinutes;
        public bool EnableRealTimeData => _enableRealTimeData;
        public bool UseHistoricalData => _useHistoricalData;
        public int RandomSeed => _randomSeed;
        
        public bool EnableEnergySimulation => _enableEnergySimulation;
        public float EnergyVariationFactor => _energyVariationFactor;
        public bool EnableSeasonalEffects => _enableSeasonalEffects;
        public bool EnableTimeOfDayEffects => _enableTimeOfDayEffects;
        public bool EnableOccupancyEffects => _enableOccupancyEffects;
        
        public bool EnableEnvironmentalSimulation => _enableEnvironmentalSimulation;
        public float TargetTemperature => _targetTemperature;
        public float TargetHumidity => _targetHumidity;
        public float TargetLightLevel => _targetLightLevel;
        public bool EnableHVACSimulation => _enableHVACSimulation;
        public bool EnableLightingSimulation => _enableLightingSimulation;
        
        public bool EnableEquipmentSimulation => _enableEquipmentSimulation;
        public bool EnableFailureSimulation => _enableFailureSimulation;
        public float FailureProbability => _failureProbability;
        public bool EnableMaintenanceSimulation => _enableMaintenanceSimulation;
        public float DegradationRate => _degradationRate;
        
        public bool EnableOccupancySimulation => _enableOccupancySimulation;
        public int MaxOccupancy => _maxOccupancy;
        public float BaseOccupancyRate => _baseOccupancyRate;
        public bool EnableTimeBasedPatterns => _enableTimeBasedPatterns;
        public bool EnableSeasonalVariation => _enableSeasonalVariation;
        
        public bool EnableOptimization => _enableOptimization;
        public OptimizationObjective Objective => _objective;
        public int MaxIterations => _maxIterations;
        public float Tolerance => _tolerance;
        
        public bool EnableLogging => _enableLogging;
        public int MaxConcurrentSimulations => _maxConcurrentSimulations;
        public float SimulationTimeoutMinutes => _simulationTimeoutMinutes;
        public bool EnableParallelProcessing => _enableParallelProcessing;

        // Unity Methods
        private void OnValidate()
        {
            ValidateParameters();
        }

        private void OnEnable()
        {
            ApplyPresetSettings();
        }

        // Public Methods
        public SimulationParameters CreateSimulationParameters()
        {
            var timeStep = TimeSpan.FromMinutes(_timeStepMinutes);
            
            return new SimulationParameters
            {
                TimeStep = timeStep,
                EnableRealTimeData = _enableRealTimeData,
                UseHistoricalData = _useHistoricalData,
                RandomSeed = _randomSeed,
                EnableLogging = _enableLogging
            };
        }

        public EnvironmentalParameters CreateEnvironmentalParameters()
        {
            var timeStep = TimeSpan.FromMinutes(_timeStepMinutes);
            
            return new EnvironmentalParameters
            {
                TimeStep = timeStep,
                TargetTemperature = DigitalTwin.Core.ValueObjects.Temperature.FromCelsius(_targetTemperature),
                TargetHumidity = _targetHumidity,
                TargetLightLevel = _targetLightLevel,
                EnableHVACSimulation = _enableHVACSimulation,
                EnableLightingSimulation = _enableLightingSimulation,
                EnableOccupancyEffects = _enableOccupancyEffects
            };
        }

        public EquipmentParameters CreateEquipmentParameters()
        {
            var timeStep = TimeSpan.FromMinutes(_timeStepMinutes);
            
            return new EquipmentParameters
            {
                TimeStep = timeStep,
                EnableFailureSimulation = _enableFailureSimulation,
                FailureProbability = (decimal)_failureProbability,
                EnableMaintenanceSimulation = _enableMaintenanceSimulation,
                EnablePerformanceDegradation = true,
                DegradationRate = (decimal)_degradationRate
            };
        }

        public OccupancyParameters CreateOccupancyParameters()
        {
            var timeStep = TimeSpan.FromMinutes(_timeStepMinutes);
            
            return new OccupancyParameters
            {
                TimeStep = timeStep,
                MaxOccupancy = _maxOccupancy,
                BaseOccupancyRate = (decimal)_baseOccupancyRate,
                EnableTimeBasedPatterns = _enableTimeBasedPatterns,
                EnableSeasonalVariation = _enableSeasonalVariation,
                RandomSeed = _randomSeed
            };
        }

        public OptimizationParameters CreateOptimizationParameters()
        {
            return new OptimizationParameters
            {
                Objective = _objective,
                MaxIterations = _maxIterations,
                Tolerance = (decimal)_tolerance
            };
        }

        // Private Methods
        private void ValidateParameters()
        {
            // Validate basic parameters
            if (_timeStepMinutes <= 0 || _timeStepMinutes > 1440) // Max 24 hours
            {
                Debug.LogWarning($"Invalid time step: {_timeStepMinutes} minutes. Using default value.");
                _timeStepMinutes = 15f;
            }

            if (_maxOccupancy <= 0)
            {
                Debug.LogWarning($"Invalid max occupancy: {_maxOccupancy}. Using default value.");
                _maxOccupancy = 100;
            }

            if (_baseOccupancyRate < 0 || _baseOccupancyRate > 1)
            {
                Debug.LogWarning($"Invalid base occupancy rate: {_baseOccupancyRate}. Using default value.");
                _baseOccupancyRate = 0.7f;
            }

            // Validate probability values
            if (_failureProbability < 0 || _failureProbability > 1)
            {
                Debug.LogWarning($"Invalid failure probability: {_failureProbability}. Using default value.");
                _failureProbability = 0.01f;
            }

            // Validate target values
            if (_targetTemperature < -50 || _targetTemperature > 100)
            {
                Debug.LogWarning($"Invalid target temperature: {_targetTemperature}°C. Using default value.");
                _targetTemperature = 22f;
            }

            if (_targetHumidity < 0 || _targetHumidity > 100)
            {
                Debug.LogWarning($"Invalid target humidity: {_targetHumidity}%. Using default value.");
                _targetHumidity = 45f;
            }

            if (_targetLightLevel < 0)
            {
                Debug.LogWarning($"Invalid target light level: {_targetLightLevel} lux. Using default value.");
                _targetLightLevel = 500f;
            }

            // Validate advanced settings
            if (_maxConcurrentSimulations <= 0)
            {
                Debug.LogWarning($"Invalid max concurrent simulations: {_maxConcurrentSimulations}. Using default value.");
                _maxConcurrentSimulations = 5;
            }

            if (_simulationTimeoutMinutes <= 0)
            {
                Debug.LogWarning($"Invalid simulation timeout: {_simulationTimeoutMinutes} minutes. Using default value.");
                _simulationTimeoutMinutes = 30f;
            }

            if (_maxIterations <= 0)
            {
                Debug.LogWarning($"Invalid max iterations: {_maxIterations}. Using default value.");
                _maxIterations = 1000;
            }

            if (_tolerance <= 0)
            {
                Debug.LogWarning($"Invalid tolerance: {_tolerance}. Using default value.");
                _tolerance = 0.001f;
            }
        }

        private void ApplyPresetSettings()
        {
            switch (_preset)
            {
                case SimulationPreset.EnergyFocus:
                    ApplyEnergyFocusPreset();
                    break;
                case SimulationPreset.ComfortFocus:
                    ApplyComfortFocusPreset();
                    break;
                case SimulationPreset.CostOptimization:
                    ApplyCostOptimizationPreset();
                    break;
                case SimulationPreset.RealTime:
                    ApplyRealTimePreset();
                    break;
                case SimulationPreset.Analysis:
                    ApplyAnalysisPreset();
                    break;
                case SimulationPreset.Custom:
                default:
                    // Keep custom settings
                    break;
            }
        }

        private void ApplyEnergyFocusPreset()
        {
            _enableEnergySimulation = true;
            _enableEnvironmentalSimulation = true;
            _enableEquipmentSimulation = true;
            _enableOccupancySimulation = true;
            _timeStepMinutes = 60f; // 1 hour steps for energy analysis
            _enableSeasonalEffects = true;
            _enableTimeOfDayEffects = true;
            _enableOptimization = true;
            _objective = OptimizationObjective.EnergyEfficiency;
        }

        private void ApplyComfortFocusPreset()
        {
            _enableEnvironmentalSimulation = true;
            _enableHVACSimulation = true;
            _enableLightingSimulation = true;
            _targetTemperature = 22f;
            _targetHumidity = 45f;
            _targetLightLevel = 500f;
            _timeStepMinutes = 15f; // Fine-grained for comfort analysis
            _enableOccupancyEffects = true;
        }

        private void ApplyCostOptimizationPreset()
        {
            _enableEnergySimulation = true;
            _enableEquipmentSimulation = true;
            _enableOptimization = true;
            _objective = OptimizationObjective.CostMinimization;
            _enableMaintenanceSimulation = true;
            _timeStepMinutes = 60f;
            _maxIterations = 2000;
            _tolerance = 0.0001f;
        }

        private void ApplyRealTimePreset()
        {
            _enableRealTimeData = true;
            _timeStepMinutes = 5f; // Fast updates for real-time
            _enableEnergySimulation = true;
            _enableEnvironmentalSimulation = true;
            _enableEquipmentSimulation = true;
            _enableLogging = false; // Reduce overhead for real-time
            _dataUpdateInterval = 0.1f;
            _maxSimultaneousDataUpdates = 50; // Limit for performance
        }

        private void ApplyAnalysisPreset()
        {
            _useHistoricalData = true;
            _enableEnergySimulation = true;
            _enableEnvironmentalSimulation = true;
            _enableEquipmentSimulation = true;
            _enableOccupancySimulation = true;
            _timeStepMinutes = 30f; // Balance detail and performance
            _enableSeasonalEffects = true;
            _enableTimeBasedPatterns = true;
            _enableLogging = true;
            _enableParallelProcessing = true;
        }
    }

    /// <summary>
    /// Simulation Presets
    /// </summary>
    public enum SimulationPreset
    {
        Custom,
        EnergyFocus,
        ComfortFocus,
        CostOptimization,
        RealTime,
        Analysis
    }
}