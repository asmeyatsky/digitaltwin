
# Architectural Principles and Code Generation Standards

## Overview

This skill enforces strict architectural principles and code generation standards to ensure all code follows enterprise-grade patterns. When this skill is active, Claude must generate code that adheres to these principles without exception.

**Unity-Specific Note**: When working with Unity projects, these principles are adapted to work within Unity’s engine constraints and patterns. See Unity-Specific sections throughout this document.

## 🏗️ Four Core Architectural Principles

### 1. Separation of Concerns (SoC)

- **Principle**: Each module/component should have a single, well-defined responsibility
- **Implementation**:
  - Separate data access, business logic, and presentation layers
  - Use dependency injection to manage dependencies
  - Create focused, single-purpose classes and functions
  - Avoid mixing concerns (e.g., UI logic in data access layer)

**Unity Application**:

- Separate MonoBehaviour lifecycle from game logic
- Keep Unity-specific code (Transform manipulation, Coroutines) isolated from business logic
- Use composition over inheritance for game components
- Segregate scene-specific code from reusable systems

### 2. Domain-Driven Design (DDD)

- **Principle**: Software design should reflect the business domain
- **Implementation**:
  - Create rich domain models that encapsulate business rules
  - Use ubiquitous language from the business domain
  - Implement aggregates, entities, and value objects
  - Define clear bounded contexts
  - Use domain events for cross-boundary communication

**Unity Application**:

- Game domain models should be POCOs (Plain Old C# Objects), not MonoBehaviours
- Use ScriptableObjects for immutable domain data and configurations
- Separate game state from Unity scene state
- Use events/messaging for cross-system communication (avoiding direct GameObject references)

### 3. Clean/Hexagonal Architecture

- **Principle**: Business logic should be independent of frameworks and infrastructure
- **Implementation**:
 
  ```
  Domain Layer (Core)
  ├── Entities
  ├── Value Objects
  ├── Domain Services
  └── Repository Interfaces
 
  Application Layer
  ├── Use Cases
  ├── DTOs
  └── Application Services
 
  Infrastructure Layer
  ├── Database Implementation
  ├── External Service Adapters
  └── Framework-specific Code
 
  Presentation Layer
  ├── Controllers/Handlers
  ├── View Models
  └── UI Components
  ```

**Unity Architecture**:

```
Core (Domain Layer)
├── Game Logic (POCOs)
├── Game State Models
├── Business Rules
└── Service Interfaces

Application Layer
├── Game Systems (non-MonoBehaviour)
├── Command Pattern Implementations
└── State Machines

Unity Infrastructure Layer
├── MonoBehaviour Adapters
├── ScriptableObject Configurations
├── PlayerPrefs/Persistence Adapters
└── Unity Event System Wrappers

Presentation Layer
├── View Controllers (MonoBehaviours)
├── UI Components
└── Visual Effect Handlers
```

### 4. High Cohesion, Low Coupling

- **Principle**: Related functionality should be grouped together, dependencies minimized
- **Implementation**:
  - Group related functionality in modules
  - Use interfaces to define contracts
  - Minimize dependencies between modules
  - Favor composition over inheritance

**Unity Application**:

- Use assembly definitions to enforce module boundaries
- Avoid cross-scene GameObject references (use services/events instead)
- Component composition over deep inheritance hierarchies
- Minimize Update() dependencies between systems

## 🛡️ Five Non-Negotiable Rules

### Rule 1: Zero Business Logic in Infrastructure Components

```csharp
// ❌ WRONG - Business logic in MonoBehaviour
public class PlayerController : MonoBehaviour
{
    void Update()
    {
        // Business logic should NOT be here!
        if (Input.GetKeyDown(KeyCode.Space))
        {
            int damage = CalculateDamage();
            if (player.health - damage <= 0)
            {
                player.isDead = true;
                // Complex death logic mixed with input handling
            }
        }
    }
}

// ✅ CORRECT - Business logic in domain model, MonoBehaviour as adapter
public class Player
{
    public int Health { get; private set; }
    public bool IsDead => Health <= 0;
   
    public DamageResult TakeDamage(int amount)
    {
        if (IsDead) return DamageResult.AlreadyDead;
       
        Health = Math.Max(0, Health - amount);
        return IsDead ? DamageResult.Killed : DamageResult.Damaged;
    }
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerView _view;
    private Player _player;
    private IInputService _inputService;
   
    void Update()
    {
        if (_inputService.GetAttackInput())
        {
            var result = _player.TakeDamage(CalculateDamage());
            _view.UpdateHealth(_player.Health);
           
            if (result == DamageResult.Killed)
            {
                _view.PlayDeathAnimation();
            }
        }
    }
}
```

### Rule 2: Interface-First Development (Ports and Adapters)

```csharp
// Define ports (interfaces) first - NEVER depend on Unity types in core logic
public interface IInputService
{
    bool GetAttackInput();
    Vector2 GetMovementInput();
}

public interface ISaveService
{
    void SaveGame(GameState state);
    GameState LoadGame();
}

public interface IAudioService
{
    void PlaySound(string soundId);
    void PlayMusic(string musicId);
}

// Then implement Unity-specific adapters
public class UnityInputAdapter : MonoBehaviour, IInputService
{
    public bool GetAttackInput() => Input.GetKeyDown(KeyCode.Space);
    public Vector2 GetMovementInput() => new Vector2(
        Input.GetAxis("Horizontal"),
        Input.GetAxis("Vertical")
    );
}

public class PlayerPrefsSaveAdapter : ISaveService
{
    public void SaveGame(GameState state)
    {
        var json = JsonUtility.ToJson(state);
        PlayerPrefs.SetString("GameState", json);
        PlayerPrefs.Save();
    }
   
    public GameState LoadGame()
    {
        var json = PlayerPrefs.GetString("GameState", "{}");
        return JsonUtility.FromJson<GameState>(json);
    }
}

public class UnityAudioAdapter : MonoBehaviour, IAudioService
{
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;
   
    public void PlaySound(string soundId)
    {
        var clip = Resources.Load<AudioClip>($"Audio/SFX/{soundId}");
        _sfxSource.PlayOneShot(clip);
    }
   
    public void PlayMusic(string musicId)
    {
        var clip = Resources.Load<AudioClip>($"Audio/Music/{musicId}");
        _musicSource.clip = clip;
        _musicSource.Play();
    }
}
```

### Rule 3: Immutable Domain Models

```csharp
using System;

// Use readonly structs for value objects
public readonly struct Health
{
    public int Current { get; }
    public int Maximum { get; }
    public bool IsDepleted => Current <= 0;
   
    public Health(int current, int maximum)
    {
        if (maximum <= 0) throw new ArgumentException("Maximum must be positive");
        Current = Math.Clamp(current, 0, maximum);
        Maximum = maximum;
    }
   
    public Health TakeDamage(int amount) =>
        new Health(Current - amount, Maximum);
   
    public Health Heal(int amount) =>
        new Health(Current + amount, Maximum);
}

// Immutable entities with copy methods
public class GameState
{
    public int Level { get; }
    public int Score { get; }
    public Health PlayerHealth { get; }
    public DateTime LastSaved { get; }
   
    public GameState(int level, int score, Health playerHealth, DateTime lastSaved)
    {
        Level = level;
        Score = score;
        PlayerHealth = playerHealth;
        LastSaved = lastSaved;
    }
   
    public GameState WithScore(int newScore) =>
        new GameState(Level, newScore, PlayerHealth, DateTime.UtcNow);
   
    public GameState WithPlayerHealth(Health newHealth) =>
        new GameState(Level, Score, newHealth, DateTime.UtcNow);
   
    public GameState NextLevel() =>
        new GameState(Level + 1, Score, PlayerHealth, DateTime.UtcNow);
}

// ScriptableObjects for immutable configuration
[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Enemy Configuration")]
public class EnemyConfiguration : ScriptableObject
{
    [SerializeField] private int _health;
    [SerializeField] private float _speed;
    [SerializeField] private int _damage;
   
    public int Health => _health;
    public float Speed => _speed;
    public int Damage => _damage;
}
```

### Rule 4: Mandatory Testing Coverage

```csharp
// Unity Test Framework - Domain logic tests (EditMode)
using NUnit.Framework;

public class HealthTests
{
    [Test]
    public void TakeDamage_ReducesCurrentHealth()
    {
        var health = new Health(100, 100);
        var damaged = health.TakeDamage(30);
       
        Assert.AreEqual(70, damaged.Current);
        Assert.AreEqual(100, damaged.Maximum);
    }
   
    [Test]
    public void TakeDamage_DoesNotMutateOriginal()
    {
        var health = new Health(100, 100);
        var damaged = health.TakeDamage(30);
       
        Assert.AreEqual(100, health.Current); // Original unchanged
    }
   
    [Test]
    public void TakeDamage_ClampsToZero()
    {
        var health = new Health(50, 100);
        var damaged = health.TakeDamage(100);
       
        Assert.AreEqual(0, damaged.Current);
        Assert.IsTrue(damaged.IsDepleted);
    }
}

// Game system tests with mocked dependencies
public class PlayerSystemTests
{
    private class MockInputService : IInputService
    {
        public bool AttackPressed { get; set; }
        public Vector2 Movement { get; set; }
       
        public bool GetAttackInput() => AttackPressed;
        public Vector2 GetMovementInput() => Movement;
    }
   
    [Test]
    public void PlayerSystem_ProcessesAttackInput()
    {
        var mockInput = new MockInputService { AttackPressed = true };
        var player = new Player(new Health(100, 100));
        var system = new PlayerSystem(player, mockInput);
       
        system.Update();
       
        // Verify attack was processed
    }
}

// Integration tests (PlayMode) for Unity-specific interactions
[UnityTest]
public IEnumerator PlayerController_AnimatesOnDeath()
{
    var go = new GameObject();
    var controller = go.AddComponent<PlayerController>();
   
    // Setup
    controller.Initialize(new Player(new Health(10, 100)));
   
    // Act
    controller.TakeFatalDamage();
   
    // Wait for animation
    yield return new WaitForSeconds(1f);
   
    // Assert
    Assert.IsTrue(controller.IsPlayingDeathAnimation);
}
```

### Rule 5: Documentation of Architectural Intent

```csharp
/// <summary>
/// Player System Module
///
/// Architectural Intent:
/// - Manages player state and behavior following clean architecture principles
/// - Player domain model is independent of Unity MonoBehaviour lifecycle
/// - All Unity-specific code isolated in PlayerController adapter
/// - Input handling abstracted behind IInputService port
/// - Health changes trigger events for UI/audio systems to react
///
/// Key Design Decisions:
/// 1. Player is a POCO to enable testing without Unity Test Runner
/// 2. Health is an immutable struct to prevent accidental state corruption
/// 3. PlayerController is a thin adapter that translates Unity events to domain commands
/// 4. Combat system is separated into its own bounded context
/// 5. State changes are validated in domain model, never in MonoBehaviours
///
/// Unity-Specific Considerations:
/// - PlayerController handles MonoBehaviour lifecycle (Awake, Update, OnDestroy)
/// - Player reference is serialized as JSON for save/load via PlayerPrefs
/// - Transform manipulation delegated to separate MovementController
/// - Animator state machine triggered by domain events, not directly from logic
/// </summary>
public class Player
{
    // Domain model implementation
}
```

## 📋 Implementation Checklist

When generating code, Claude must verify:

### Layer Separation

- [ ] Domain layer has NO dependencies on UnityEngine namespace
- [ ] Application layer depends only on domain layer
- [ ] MonoBehaviours are thin adapters, not business logic containers
- [ ] ScriptableObjects used for configuration, not runtime state

### Interface Design

- [ ] All Unity dependencies have interface definitions
- [ ] Interfaces are defined without UnityEngine types in signatures
- [ ] Concrete Unity implementations are in infrastructure layer
- [ ] Dependency injection or service locator wires implementations

### Domain Modeling

- [ ] Game state models are POCOs or immutable structs
- [ ] Business rules are encapsulated in domain objects
- [ ] Value objects used for game concepts (Health, Damage, Position)
- [ ] No MonoBehaviour inheritance in domain layer

### Unity-Specific Patterns

- [ ] Update() methods are thin, delegating to game systems
- [ ] Coroutines isolated from business logic
- [ ] Scene references managed through services/events, not direct GameObject links
- [ ] Assembly definitions enforce architectural boundaries

### Testing Requirements

- [ ] EditMode tests for all domain logic (no Unity dependencies)
- [ ] PlayMode tests for MonoBehaviour integration
- [ ] Mock implementations for all service interfaces
- [ ] Test coverage meets minimum 80% threshold for domain/application layers

### Documentation Standards

- [ ] Each module has architectural intent documented
- [ ] Unity-specific constraints and patterns explained
- [ ] Assembly definition purposes documented
- [ ] Integration points between Unity and domain clearly marked

## 🎯 Unity-Specific Code Generation Guidelines

### When Creating New Game Systems

1. **Start with Domain POCOs**
  
   ```csharp
   // Domain layer - no Unity dependencies
   public class Enemy
   {
       public Health Health { get; private set; }
       public int Damage { get; }
       public bool IsAlive => !Health.IsDepleted;
      
       public Enemy(Health health, int damage)
       {
           Health = health;
           Damage = damage;
       }
      
       public AttackResult Attack(Player target)
       {
           // Pure business logic
           return target.TakeDamage(Damage);
       }
   }
   ```
1. **Define Service Interfaces**
  
   ```csharp
   public interface IEnemySpawner
   {
       Enemy SpawnEnemy(EnemyType type, Vector3 position);
       void DespawnEnemy(Enemy enemy);
   }
  
   public interface IPathfinding
   {
       Vector3[] CalculatePath(Vector3 from, Vector3 to);
   }
   ```
1. **Implement Game Systems (non-MonoBehaviour)**
  
   ```csharp
   public class CombatSystem
   {
       private readonly IAudioService _audio;
       private readonly IParticleService _particles;
      
       public CombatSystem(IAudioService audio, IParticleService particles)
       {
           _audio = audio;
           _particles = particles;
       }
      
       public void ProcessAttack(Enemy attacker, Player defender)
       {
           var result = attacker.Attack(defender);
          
           _audio.PlaySound(result.SoundEffect);
           _particles.PlayEffect(result.ParticleEffect, defender.Position);
       }
   }
   ```
1. **Create MonoBehaviour Adapters**
  
   ```csharp
   public class EnemyController : MonoBehaviour
   {
       private Enemy _enemy;
       private CombatSystem _combatSystem;
       private IPathfinding _pathfinding;
      
       [SerializeField] private Animator _animator;
       [SerializeField] private EnemyConfiguration _config;
      
       public void Initialize(Enemy enemy, CombatSystem combatSystem, IPathfinding pathfinding)
       {
           _enemy = enemy;
           _combatSystem = combatSystem;
           _pathfinding = pathfinding;
       }
      
       void Update()
       {
           if (!_enemy.IsAlive)
           {
               HandleDeath();
               return;
           }
          
           UpdateMovement();
           CheckAttackRange();
       }
      
       private void UpdateMovement()
       {
           // Unity-specific movement code
           var path = _pathfinding.CalculatePath(transform.position, targetPosition);
           // Move along path
       }
   }
   ```
1. **Use ScriptableObjects for Configuration**
  
   ```csharp
   [CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Configuration")]
   public class GameConfiguration : ScriptableObject
   {
       [Header("Player Settings")]
       [SerializeField] private int _playerMaxHealth = 100;
       [SerializeField] private float _playerSpeed = 5f;
      
       [Header("Enemy Settings")]
       [SerializeField] private EnemyConfiguration[] _enemyTypes;
      
       public int PlayerMaxHealth => _playerMaxHealth;
       public float PlayerSpeed => _playerSpeed;
       public IReadOnlyList<EnemyConfiguration> EnemyTypes => _enemyTypes;
   }
   ```

### Unity Project Structure

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/                          # Domain Layer (Assembly: Game.Core)
│   │   │   ├── Entities/
│   │   │   │   ├── Player.cs
│   │   │   │   ├── Enemy.cs
│   │   │   │   └── Projectile.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── Health.cs
│   │   │   │   ├── Damage.cs
│   │   │   │   └── Position.cs
│   │   │   ├── Services/
│   │   │   │   └── CombatSystem.cs
│   │   │   └── Interfaces/
│   │   │       ├── IInputService.cs
│   │   │       ├── ISaveService.cs
│   │   │       └── IAudioService.cs
│   │   │
│   │   ├── Application/                   # Application Layer (Assembly: Game.Application)
│   │   │   ├── Commands/
│   │   │   │   ├── AttackCommand.cs
│   │   │   │   └── MoveCommand.cs
│   │   │   ├── StateMachines/
│   │   │   │   └── GameStateMachine.cs
│   │   │   └── Events/
│   │   │       └── GameEvents.cs
│   │   │
│   │   ├── Infrastructure/                # Unity Infrastructure (Assembly: Game.Infrastructure)
│   │   │   ├── UnityAdapters/
│   │   │   │   ├── UnityInputAdapter.cs
│   │   │   │   ├── UnityAudioAdapter.cs
│   │   │   │   └── PlayerPrefsSaveAdapter.cs
│   │   │   ├── Services/
│   │   │   │   ├── ServiceLocator.cs
│   │   │   │   └── DependencyInjection.cs
│   │   │   └── Persistence/
│   │   │       └── SaveLoadManager.cs
│   │   │
│   │   ├── Presentation/                  # Presentation Layer (Assembly: Game.Presentation)
│   │   │   ├── Controllers/
│   │   │   │   ├── PlayerController.cs
│   │   │   │   ├── EnemyController.cs
│   │   │   │   └── UIController.cs
│   │   │   ├── Views/
│   │   │   │   ├── HealthBar.cs
│   │   │   │   └── ScoreDisplay.cs
│   │   │   └── Installers/
│   │   │       └── GameSceneInstaller.cs
│   │   │
│   │   └── Tests/
│   │       ├── EditMode/                  # Domain/Application tests
│   │       │   ├── Core/
│   │       │   │   ├── HealthTests.cs
│   │       │   │   └── PlayerTests.cs
│   │       │   └── Application/
│   │       │       └── CombatSystemTests.cs
│   │       └── PlayMode/                  # Integration tests
│   │           └── PlayerControllerTests.cs
│   │
│   ├── ScriptableObjects/
│   │   ├── Configurations/
│   │   │   ├── GameConfiguration.asset
│   │   │   └── EnemyConfigurations/
│   │   └── Data/
│   │       └── LevelData/
│   │
│   ├── Prefabs/
│   │   ├── Player/
│   │   ├── Enemies/
│   │   └── UI/
│   │
│   └── Scenes/
│       ├── Bootstrap.unity              # DI setup, scene loading
│       ├── MainMenu.unity
│       └── GameLevel.unity
```

## ⚠️ Unity-Specific Anti-Patterns to Avoid

1. **God MonoBehaviours**: Single MonoBehaviour with hundreds of lines containing all game logic
  
   ```csharp
   // ❌ WRONG
   public class GameManager : MonoBehaviour
   {
       // 1000+ lines of mixed concerns
       void Update() { /* everything happens here */ }
   }
   ```
1. **FindObjectOfType Coupling**: Direct runtime GameObject dependencies
  
   ```csharp
   // ❌ WRONG
   void Start()
   {
       player = GameObject.Find("Player").GetComponent<Player>();
       uiManager = FindObjectOfType<UIManager>();
   }
  
   // ✅ CORRECT - Inject dependencies
   public void Initialize(Player player, IUIService uiService) { }
   ```
1. **Business Logic in Update()**: Complex game rules in MonoBehaviour lifecycle
  
   ```csharp
   // ❌ WRONG
   void Update()
   {
       if (player.health <= 0 && !isDead)
       {
           // Complex death logic here
       }
   }
  
   // ✅ CORRECT - Thin Update, delegate to systems
   void Update()
   {
       _gameSystem.Update(Time.deltaTime);
   }
   ```
1. **Singleton Abuse**: Static state instead of proper DI
  
   ```csharp
   // ❌ WRONG
   public class GameManager : MonoBehaviour
   {
       public static GameManager Instance;
       void Awake() { Instance = this; }
   }
  
   // ✅ CORRECT - Service locator or DI container
   public class ServiceLocator
   {
       private Dictionary<Type, object> _services;
       public T Get<T>() => (T)_services[typeof(T)];
   }
   ```
1. **Coroutine Business Logic**: Game rules inside Coroutines
  
   ```csharp
   // ❌ WRONG
   IEnumerator AttackSequence()
   {
       // Business logic mixed with timing
       player.health -= 10;
       yield return new WaitForSeconds(1f);
       if (player.health <= 0) player.Die();
   }
  
   // ✅ CORRECT - Coroutine for timing only
   IEnumerator AttackSequence()
   {
       var result = _combatSystem.ProcessAttack(_enemy, _player);
       _view.PlayAnimation(result.Animation);
       yield return new WaitForSeconds(result.Duration);
       _view.UpdateHealth(_player.Health);
   }
   ```
1. **Scene-Coupled State**: Game state tied to active scene
  
   ```csharp
   // ❌ WRONG - State lost on scene change
   public class LevelManager : MonoBehaviour
   {
       public int playerScore; // Lost on scene load!
   }
  
   // ✅ CORRECT - Persistent state service
   public class GameStateService : IGameState
   {
       private GameState _state;
       public int Score => _state.Score;
      
       public void SaveState()
       {
           _saveService.Save(_state);
       }
   }
   ```

## 🚀 Unity Advanced Patterns

### Event-Driven Communication (ScriptableObject Events)

```csharp
// Event channel
[CreateAssetMenu(fileName = "GameEvent", menuName = "Events/Game Event")]
public class GameEvent : ScriptableObject
{
    private readonly List<GameEventListener> _listeners = new List<GameEventListener>();
   
    public void Raise()
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            _listeners[i].OnEventRaised();
        }
    }
   
    public void RegisterListener(GameEventListener listener) => _listeners.Add(listener);
    public void UnregisterListener(GameEventListener listener) => _listeners.Remove(listener);
}

// Generic typed event
[CreateAssetMenu(fileName = "IntEvent", menuName = "Events/Int Event")]
public class IntEvent : ScriptableObject
{
    private readonly List<IIntEventListener> _listeners = new List<IIntEventListener>();
   
    public void Raise(int value)
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            _listeners[i].OnEventRaised(value);
        }
    }
   
    public void RegisterListener(IIntEventListener listener) => _listeners.Add(listener);
    public void UnregisterListener(IIntEventListener listener) => _listeners.Remove(listener);
}

// Usage
public class HealthDisplay : MonoBehaviour, IIntEventListener
{
    [SerializeField] private IntEvent _healthChangedEvent;
   
    void OnEnable() => _healthChangedEvent.RegisterListener(this);
    void OnDisable() => _healthChangedEvent.UnregisterListener(this);
   
    public void OnEventRaised(int newHealth)
    {
        UpdateDisplay(newHealth);
    }
}
```

### Command Pattern for Input

```csharp
// Command interface
public interface ICommand
{
    void Execute();
    void Undo();
}

// Concrete command
public class MoveCommand : ICommand
{
    private readonly Player _player;
    private readonly Vector3 _direction;
    private Vector3 _previousPosition;
   
    public MoveCommand(Player player, Vector3 direction)
    {
        _player = player;
        _direction = direction;
    }
   
    public void Execute()
    {
        _previousPosition = _player.Position;
        _player.Move(_direction);
    }
   
    public void Undo()
    {
        _player.SetPosition(_previousPosition);
    }
}

// Command processor
public class CommandProcessor
{
    private readonly Stack<ICommand> _commandHistory = new Stack<ICommand>();
   
    public void ExecuteCommand(ICommand command)
    {
        command.Execute();
        _commandHistory.Push(command);
    }
   
    public void UndoLastCommand()
    {
        if (_commandHistory.Count > 0)
        {
            var command = _commandHistory.Pop();
            command.Undo();
        }
    }
}
```

### Object Pool Pattern

```csharp
// Pool interface
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}

// Generic pool
public class ObjectPool<T> where T : Component, IPoolable
{
    private readonly T _prefab;
    private readonly Queue<T> _pool = new Queue<T>();
    private readonly Transform _parent;
   
    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent;
       
        for (int i = 0; i < initialSize; i++)
        {
            var obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
   
    public T Spawn(Vector3 position, Quaternion rotation)
    {
        T obj;
        if (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
        }
        else
        {
            obj = Object.Instantiate(_prefab, _parent);
        }
       
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.gameObject.SetActive(true);
        obj.OnSpawn();
        return obj;
    }
   
    public void Despawn(T obj)
    {
        obj.OnDespawn();
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
    }
}

// Usage
public class Projectile : MonoBehaviour, IPoolable
{
    public void OnSpawn()
    {
        // Reset projectile state
    }
   
    public void OnDespawn()
    {
        // Clean up before returning to pool
    }
}
```

### State Machine Pattern

```csharp
// State interface
public interface IState
{
    void OnEnter();
    void OnUpdate();
    void OnExit();
}

// Concrete states
public class IdleState : IState
{
    private readonly Enemy _enemy;
   
    public IdleState(Enemy enemy) => _enemy = enemy;
   
    public void OnEnter() { /* Setup idle */ }
    public void OnUpdate() { /* Check for player proximity */ }
    public void OnExit() { /* Cleanup */ }
}

public class ChaseState : IState
{
    private readonly Enemy _enemy;
    private readonly IPathfinding _pathfinding;
   
    public ChaseState(Enemy enemy, IPathfinding pathfinding)
    {
        _enemy = enemy;
        _pathfinding = pathfinding;
    }
   
    public void OnEnter() { /* Start chase */ }
    public void OnUpdate() { /* Follow player */ }
    public void OnExit() { /* Stop movement */ }
}

// State machine
public class StateMachine
{
    private IState _currentState;
   
    public void ChangeState(IState newState)
    {
        _currentState?.OnExit();
        _currentState = newState;
        _currentState?.OnEnter();
    }
   
    public void Update()
    {
        _currentState?.OnUpdate();
    }
}
```

## 🎯 Unity Assembly Definitions Strategy

```
Assets/_Project/Scripts/
├── Core/
│   └── Game.Core.asmdef          # References: None (pure C#)
├── Application/
│   └── Game.Application.asmdef   # References: Game.Core
├── Infrastructure/
│   └── Game.Infrastructure.asmdef # References: Game.Core, Game.Application, UnityEngine
└── Presentation/
    └── Game.Presentation.asmdef  # References: All layers, Unity UI, etc.
```

**Benefits**:

- Enforces architectural boundaries at compile time
- Faster iteration (only affected assemblies recompile)
- Clear dependency graph
- Prevents circular dependencies

## 🧪 Unity Testing Strategy

### EditMode Tests (Fast, No Unity Runtime)

```csharp
// Test domain logic without Unity
[TestFixture]
public class CombatSystemTests
{
    private CombatSystem _system;
    private MockAudioService _mockAudio;
   
    [SetUp]
    public void Setup()
    {
        _mockAudio = new MockAudioService();
        _system = new CombatSystem(_mockAudio);
    }
   
    [Test]
    public void ProcessAttack_WithValidTarget_ReducesHealth()
    {
        var attacker = new Enemy(new Health(100, 100), damage: 25);
        var defender = new Player(new Health(100, 100));
       
        _system.ProcessAttack(attacker, defender);
       
        Assert.AreEqual(75, defender.Health.Current);
    }
}
```

### PlayMode Tests (Integration with Unity)

```csharp
[UnityTest]
public IEnumerator PlayerController_RespondsToInput()
{
    // Arrange
    var scene = SceneManager.CreateScene("TestScene");
    SceneManager.SetActiveScene(scene);
   
    var go = new GameObject();
    var controller = go.AddComponent<PlayerController>();
    var mockInput = new MockInputService();
    controller.Initialize(new Player(new Health(100, 100)), mockInput);
   
    // Act
    mockInput.AttackPressed = true;
    yield return null; // Wait one frame
   
    // Assert
    Assert.IsTrue(controller.HasProcessedAttack);
   
    // Cleanup
    SceneManager.UnloadSceneAsync(scene);
}
```

## 📚 Unity-Specific Required Knowledge

When using this skill in Unity projects, Claude should understand:

- Unity’s MonoBehaviour lifecycle (Awake, Start, Update, FixedUpdate, OnDestroy)
- ScriptableObject patterns for data-driven design
- Unity’s serialization system and limitations
- Assembly Definition files for architectural enforcement
- Unity Test Framework (EditMode vs PlayMode tests)
- Object pooling for performance
- Event-driven architecture using ScriptableObjects or C# events
- Coroutines for timing, not business logic

## 🎖️ Unity Certification Criteria

Unity code generated with this skill must:

1. Keep domain logic independent of UnityEngine namespace
1. Use MonoBehaviours only as thin adapters/controllers
1. Leverage ScriptableObjects for configuration and event channels
1. Have assembly definitions enforcing layer boundaries
1. Include both EditMode and PlayMode tests where appropriate
1. Follow Unity naming conventions and folder structure
1. Document Unity-specific constraints and patterns

-----

**Note**: This skill enforces architectural discipline specifically adapted for Unity game development. All patterns should be applied pragmatically based on game complexity and team size. For small prototypes, lighter architectural approaches are acceptable; for production games, strict adherence is recommended.