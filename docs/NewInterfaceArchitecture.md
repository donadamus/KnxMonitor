# Nowa architektura interfejsów KNX

## Filozofia: Kompozycja zamiast dziedziczenia

Zamiast skomplikowanych hierarchii z nadpisaniami, używamy prostych interfejsów funkcjonalnych.

## Interfejsy funkcjonalne (building blocks)

### `IKnxDeviceBase`
- Podstawowa funkcjonalność każdego urządzenia KNX
- Id, Name, SubGroup, LastUpdated
- Initialize, Save/Restore state

### `ISwitchable` 
- Urządzenia które można włączać/wyłączać
- TurnOn/Off, Toggle, ReadSwitchState

### `IPercentageControllable`
- Urządzenia sterowane procentowo (0-100%)
- SetPercentage, ReadPercentage, AdjustPercentage
- **Uniwersalne**: jasność dimmerów + pozycja rolet!

### `ILockableDevice`
- Urządzenia które można blokować
- Lock/Unlock, ReadLockState

## Kompozycja urządzeń

### `ILightDevice`
```csharp
ILightDevice : IKnxDeviceBase + ISwitchable + ILockableDevice
```
- Podstawowe światło: włącz/wyłącz + blokada

### `IDimmerDevice` 
```csharp
IDimmerDevice : ILightDevice + IPercentageControllable
```
- Dimmer = światło + sterowanie jasnością (0-100%)

### `IShutterDevice`
```csharp
IShutterDevice : IKnxDeviceBase + IPercentageControllable + ILockableDevice
```
- Roleta = pozycja (0-100%) + blokada
- **Nie** potrzebuje ISwitchable - pozycja załatwia wszystko

## Korzyści

✅ **Brak nadpisań** - każdy interface ma własne właściwości
✅ **Reużywalność** - IPercentageControllable dla dimmerów I rolet  
✅ **Prostota** - łatwe zrozumienie co urządzenie umie
✅ **Testowanie** - osobne testy dla każdej funkcji
✅ **Rozszerzalność** - łatwo dodać nowe kombinacje

## Przykłady użycia

```csharp
// Test tylko funkcji przełączania
void TestSwitching(ISwitchable device) { ... }

// Test tylko funkcji procentowych
void TestPercentage(IPercentageControllable device) { ... }

// Test kompletnego dimmera
void TestDimmer(IDimmerDevice dimmer) {
    // Może używać wszystkich funkcji:
    await dimmer.TurnOnAsync();       // z ISwitchable
    await dimmer.SetPercentageAsync(50); // z IPercentageControllable  
    await dimmer.LockAsync();         // z ILockableDevice
}
```
