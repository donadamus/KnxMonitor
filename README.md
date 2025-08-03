# KnxMonitor

A comprehensive .NET system for managing, monitoring, and testing KNX home automation devices with advanced state management and hierarchical restoration capabilities.

## 🏠 Overview

KnxMonitor provides a robust framework for interacting with KNX building automation devices. It features a sophisticated state management system with hierarchical restoration, device locking mechanisms, and comprehensive testing coverage.

## 🎯 Features

- **Multi-device Support**: Light switches, dimmers, and shutters
- **Hierarchical State Management**: Smart state saving and restoration across device inheritance hierarchy
- **Lock Management**: Device locking with automatic unlock during restoration
- **Real-time Monitoring**: KNX message feedback processing
- **Comprehensive Testing**: Unit and integration tests with high coverage
- **Type Safety**: Strongly-typed device states and addresses

## 🔌 Supported Devices

### 💡 Light
Basic lighting control with the following capabilities:
- **Switch Control**: On/Off operations
- **Lock Management**: Device locking/unlocking
- **State Persistence**: Save and restore switch and lock states
- **Feedback Processing**: Real-time state updates from KNX bus

**Key Methods:**
```csharp
await light.TurnOnAsync();
await light.TurnOffAsync();
await light.ToggleAsync();
await light.SetLockAsync(Lock.On);
light.SaveCurrentState();
await light.RestoreSavedStateAsync();
```

### 🔆 Dimmer
Advanced lighting control extending Light functionality:
- **Brightness Control**: 0-100% brightness adjustment
- **Fade Operations**: Smooth transitions between brightness levels
- **Switch Control**: Inherited from Light
- **Lock Management**: Inherited from Light
- **State Persistence**: Save and restore brightness, switch, and lock states

**Key Methods:**
```csharp
await dimmer.SetBrightnessAsync(75f);
await dimmer.FadeToAsync(50f, TimeSpan.FromSeconds(2));
await dimmer.WaitForBrightnessAsync(75f);
```

### 🪟 Shutter
Window covering control with positioning:
- **Position Control**: 0-100% position (0% = fully open, 100% = fully closed)
- **Movement Control**: Up/Down/Stop operations
- **Lock Management**: Movement prevention when locked
- **Movement Monitoring**: Real-time movement state feedback
- **State Persistence**: Save and restore position, lock, and movement states

**Key Methods:**
```csharp
await shutter.SetPositionAsync(75f);
await shutter.OpenAsync();
await shutter.CloseAsync();
await shutter.MoveAsync(ShutterDirection.Up, TimeSpan.FromSeconds(5));
await shutter.StopAsync();
```

## 🔄 Hierarchical State Restoration

The system implements a sophisticated state restoration mechanism:

### Restoration Chain
```
Dimmer.RestoreSavedStateAsync()
├── Restore Brightness (device-specific)
└── base.RestoreSavedStateAsync() (Light)
    ├── Restore Switch (device-specific)
    └── base.RestoreSavedStateAsync() (LockableKnxDeviceBase)
        └── Restore Lock (inherited behavior)
```

### Smart Lock Handling
When restoring state on a locked device:
1. **Check Current State**: Verify if restoration is needed
2. **Temporary Unlock**: If device is locked, unlock it temporarily
3. **Restore State**: Apply the saved state (brightness, position, etc.)
4. **Restore Lock**: Apply the saved lock state through base class

Example from Dimmer:
```csharp
protected override async Task PerformStateRestoration()
{
    if (CurrentState.Brightness != SavedState!.Brightness)
    {
        if (CurrentState.Lock == Lock.On)
        {
            Console.WriteLine($"Dimmer {Id} is locked, temporarily unlocking...");
            await SetLockAsync(Lock.Off);
        }
        await SetBrightnessAsync(SavedState.Brightness);
    }
}
```

## 🧪 Testing

The project includes comprehensive testing with high coverage across all device types and scenarios.

### 📋 Unit Tests

Located in `KnxTest/Unit/Models/`:

#### LightUnitTests.cs
- **Basic Operations**: Turn on/off, toggle, lock/unlock
- **State Management**: Save and restore states
- **Lock Scenarios**: Restoration with active locks
- **Exception Handling**: Invalid operations and error cases
- **Wait Operations**: Asynchronous state waiting

#### DimmerUnitTests.cs  
- **Brightness Control**: Set brightness, fade operations
- **State Management**: Save and restore brightness, switch, lock states
- **Lock Scenarios**: Brightness restoration with active locks
- **Hierarchical Restoration**: Verify base class restoration calls
- **Wait Operations**: Brightness waiting with tolerance
- **Exception Handling**: Invalid brightness values, missing saved states

#### ShutterUnitTests.cs
- **Position Control**: Set position, movement operations
- **State Management**: Save and restore position, lock, movement states  
- **Lock Scenarios**: Position restoration with active locks
- **Movement Control**: Up/down/stop operations
- **Exception Handling**: Invalid positions, missing saved states

### 🔗 Integration Tests

Located in `KnxTest/Integration/`:

#### Device-Specific Integration Tests
- **LightIntegrationTests.cs**: End-to-end light control scenarios
- **DimmerIntegrationTests.cs**: Brightness and fade operation testing
- **ShutterIntegrationTests.cs**: Position and movement testing

#### Test Features
- **KnxServiceFixture**: Shared KNX service instances for realistic testing
- **Real KNX Communication**: Tests actual KNX message sending/receiving
- **State Persistence**: Verify state saving/restoration in realistic scenarios
- **Lock Prevention**: Test that locked devices reject commands appropriately

### 🎯 Test Coverage Highlights

| Device Type | Save State | Restore State | Lock Handling | Exception Cases | Integration |
|-------------|------------|---------------|---------------|-----------------|-------------|
| Light 💡    | ✅         | ✅            | ✅            | ✅              | ✅          |
| Dimmer 🔆   | ✅         | ✅            | ✅            | ✅              | ✅          |
| Shutter 🪟  | ✅         | ✅            | ✅            | ✅              | ✅          |

## 🏗️ Architecture

### Class Hierarchy
```
KnxDeviceBase (abstract)
└── LockableKnxDeviceBase (abstract)
    ├── Light
    │   └── Dimmer
    └── Shutter
```

### Key Design Patterns
- **Template Method**: Hierarchical state restoration
- **Strategy Pattern**: Device-specific message processing
- **Observer Pattern**: KNX message feedback handling
- **Chain of Responsibility**: Lock state management

### Type System
- **Records for State**: Immutable state objects with `with` expressions
- **Type-Safe Addresses**: Strongly-typed KNX address records
- **Enum-Based States**: Switch, Lock, and Movement state enums

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 or higher
- KNX system for integration testing (optional)

### Basic Usage

```csharp
// Create a dimmer
var knxService = new KnxService();
var dimmer = new Dimmer("DIM1", "Living Room Dimmer", "1", knxService);

// Initialize and use
await dimmer.InitializeAsync();
dimmer.SaveCurrentState();

// Control the dimmer  
await dimmer.SetBrightnessAsync(75f);
await dimmer.FadeToAsync(25f, TimeSpan.FromSeconds(3));

// Restore previous state
await dimmer.RestoreSavedStateAsync();
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📁 Project Structure

```
KnxMonitor/
├── KnxModel/                    # Core device models
│   ├── Models/                  # Device implementations
│   │   ├── KnxDeviceBase.cs
│   │   ├── LockableKnxDeviceBase.cs
│   │   ├── Light.cs
│   │   ├── Dimmer.cs
│   │   └── Shutter.cs
│   ├── Types/                   # Type definitions
│   │   ├── LightTypes.cs
│   │   ├── DimmerTypes.cs
│   │   └── ShutterTypes.cs
│   └── Interfaces/              # Device interfaces
├── KnxService/                  # KNX communication
├── KnxTest/                     # Test projects
│   ├── Unit/Models/             # Unit tests
│   └── Integration/             # Integration tests
└── KnxMonitor/                  # Main application
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests for your changes
4. Ensure all tests pass (`dotnet test`)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Contribution Guidelines
- Maintain high test coverage (>90%)
- Follow established patterns for state management
- Include both unit and integration tests for new features
- Document public APIs with XML comments

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

- Create an [Issue](https://github.com/donadamus/KnxMonitor/issues) for bug reports
- Start a [Discussion](https://github.com/donadamus/KnxMonitor/discussions) for questions
- Submit a [Pull Request](https://github.com/donadamus/KnxMonitor/pulls) for contributions

---

**Built with ❤️ for the KNX community**
