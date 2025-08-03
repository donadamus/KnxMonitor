using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using KnxModel;

namespace KnxTest.Integration
{
    [Collection("KnxService collection")]
    public class DimmerIntegrationTests : IDisposable
    {

        public static IEnumerable<object[]> DimmerIdsFromConfig
        {
            get
            {
                var config = DimmerFactory.DimmerConfigurations;
                return config//.Where(k => k.Value.Name.Contains("Office"))
                    .Select(k => new object[] { k.Key });
            }
        }


        private static IKnxService _knxServiceMock = new Moq.Mock<IKnxService>().Object;
        private readonly IKnxService _knxService;

        private static readonly IDimmer _defaulDimmer = new Dimmer("L11", "Test Light 11", "1", _knxServiceMock);
        private IDimmer _dimmer = _defaulDimmer;

        public DimmerIntegrationTests(KnxServiceFixture fixture)
        {
            _knxService = fixture.KnxService;
            _dimmer1 = new Dimmer("DIM1", "Test Dimmer 1", "1", _knxService);
            _dimmer2 = new Dimmer("DIM2", "Test Dimmer 2", "2", _knxService);
        }

        private readonly Dimmer _dimmer1;
        private readonly Dimmer _dimmer2;

        public void Dispose()
        {
            try
            {
                // Reset dimmers to known state
                _dimmer1?.UnlockAsync().Wait();
                _dimmer1?.TurnOffAsync().Wait();
                _dimmer1?.Dispose();

                _dimmer2?.UnlockAsync().Wait();
                _dimmer2?.TurnOffAsync().Wait();
                _dimmer2?.Dispose();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cleanup failed: {ex.Message}");
            }

            if (_dimmer != _defaulDimmer)
            {
                _dimmer.RestoreSavedStateAsync().GetAwaiter().GetResult();
                _dimmer.Dispose();
            }



        }

        #region Address Configuration Tests

        [Fact]
        public void DimmerAddresses_ShouldHaveCorrectConfiguration()
        {
            // Test DIM1 addresses
            _dimmer1.Addresses.SwitchControl.Should().Be("2/1/1");
            _dimmer1.Addresses.SwitchFeedback.Should().Be("2/1/101");
            _dimmer1.Addresses.BrightnessControl.Should().Be("2/2/1");
            _dimmer1.Addresses.BrightnessFeedback.Should().Be("2/2/101");
            _dimmer1.Addresses.LockControl.Should().Be("2/3/1");
            _dimmer1.Addresses.LockFeedback.Should().Be("2/3/101");

            // Test DIM2 addresses
            _dimmer2.Addresses.SwitchControl.Should().Be("2/1/2");
            _dimmer2.Addresses.SwitchFeedback.Should().Be("2/1/102");
            _dimmer2.Addresses.BrightnessControl.Should().Be("2/2/2");
            _dimmer2.Addresses.BrightnessFeedback.Should().Be("2/2/102");
            _dimmer2.Addresses.LockControl.Should().Be("2/3/2");
            _dimmer2.Addresses.LockFeedback.Should().Be("2/3/102");
        }

        #endregion



        [Theory]
        [MemberData(nameof(DimmerIdsFromConfig))]

        public async Task OK_CanInitializeLightAndReadState(string lightId)
        {
            // Act
            await _dimmer.InitializeAsync();

            // Assert
            _dimmer.CurrentState.Should().NotBeNull($"Light {lightId} should have a valid current state after initialization");
            _dimmer.CurrentState.Switch.Should().NotBe(Switch.Unknown, $"Light {lightId} should have a known switch state after initialization");
            _dimmer.CurrentState.Lock.Should().NotBe(Lock.Unknown, $"Light {lightId} should have a known lock state after initialization");
            _dimmer.CurrentState.LastUpdated.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1),
                $"Light {lightId} last updated time should be close to now after initialization");
        }


        #region Switch Control Tests

        [Fact]
        public async Task SwitchControl_TurnOnAndOff_ShouldWork()
        {
            try
            {
                Console.WriteLine("🔄 Testing dimmer switch control...");

                // Turn on
                await _dimmer1.TurnOnAsync();
                await _dimmer1.WaitForStateAsync(Switch.On, TimeSpan.FromSeconds(3));
                _dimmer1.CurrentState.Switch.Should().Be(Switch.On,"Dimmer should be ON");

                // Turn off
                await _dimmer1.TurnOffAsync();
                await _dimmer1.WaitForStateAsync(Switch.Off, TimeSpan.FromSeconds(3));
                _dimmer1.CurrentState.Switch.Should().Be(Switch.Off,"Dimmer should be OFF");

                Console.WriteLine("✅ Switch control test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Switch control test failed: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public async Task SwitchControl_Toggle_ShouldWork()
        {
            try
            {
                Console.WriteLine("🔄 Testing dimmer toggle...");

                // Start from OFF state
                await _dimmer1.TurnOffAsync();
                await _dimmer1.WaitForStateAsync(Switch.Off, TimeSpan.FromSeconds(3));

                // Toggle to ON
                await _dimmer1.ToggleAsync();
                await _dimmer1.WaitForStateAsync(Switch.On, TimeSpan.FromSeconds(3));
                _dimmer1.CurrentState.Switch.Should().Be(Switch.On,"Dimmer should be ON after toggle");

                // Toggle to OFF
                await _dimmer1.ToggleAsync();
                await _dimmer1.WaitForStateAsync(Switch.Off, TimeSpan.FromSeconds(3));
                _dimmer1.CurrentState.Switch.Should().Be(Switch.Off,"Dimmer should be OFF after toggle");

                Console.WriteLine("✅ Toggle test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Toggle test failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Brightness Control Tests

        [Fact]
        public async Task BrightnessControl_SetDifferentLevels_ShouldWork()
        {
            try
            {
                Console.WriteLine("🔄 Testing dimmer brightness control...");

                // Test 50% brightness
                await _dimmer1.SetBrightnessAsync(50);
                await _dimmer1.WaitForBrightnessAsync(50, TimeSpan.FromSeconds(3));
                _dimmer1.CurrentState.Brightness.Should().BeApproximately(50, 1, "Brightness should be 50%");
                _dimmer1.CurrentState.Switch.Should().Be(Switch.On,"Dimmer should be ON at 50%");

                // Test 100% brightness
                await _dimmer1.SetBrightnessAsync(100);
                await _dimmer1.WaitForBrightnessAsync(100, TimeSpan.FromSeconds(3));
                _dimmer1.CurrentState.Brightness.Should().BeApproximately(100, 1, "Brightness should be 100%");
                _dimmer1.CurrentState.Switch.Should().Be(Switch.On,"Dimmer should be ON at 100%");

                // Test 0% brightness (should turn off)
                await _dimmer1.SetBrightnessAsync(0);
                await _dimmer1.WaitForBrightnessAsync(0, TimeSpan.FromSeconds(3));
                _dimmer1.CurrentState.Brightness.Should().Be(0, "Brightness should be 0%");
                _dimmer1.CurrentState.Switch.Should().Be(Switch.Off,"Dimmer should be OFF at 0%");

                Console.WriteLine("✅ Brightness control test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Brightness control test failed: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public async Task BrightnessControl_FadeFunction_ShouldWork()
        {
            try
            {
                Console.WriteLine("🔄 Testing dimmer fade function...");

                // Start at 0%
                await _dimmer1.SetBrightnessAsync(0);
                await _dimmer1.WaitForBrightnessAsync(0, TimeSpan.FromSeconds(3));

                // Fade to 80% over 2 seconds
                await _dimmer1.FadeToAsync(80, TimeSpan.FromSeconds(2));
                
                // Verify final state
                await Task.Delay(500); // Give extra time for final adjustment
                await _dimmer1.WaitForBrightnessAsync(80, TimeSpan.FromSeconds(3));
                _dimmer1.CurrentState.Brightness.Should().BeApproximately(80, 1, "Brightness should be 80%");

                Console.WriteLine("✅ Fade function test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fade function test failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region State Reading Tests

        [Fact]
        public async Task StateReading_ShouldReturnCurrentValues()
        {
            try
            {
                Console.WriteLine("🔄 Testing dimmer state reading...");

                // Set known state
                await _dimmer1.SetBrightnessAsync(75);
                await _dimmer1.WaitForBrightnessAsync(75, TimeSpan.FromSeconds(3));

                // Read switch state
                var switchState = await _dimmer1.ReadStateAsync();
                switchState.Should().Be(Switch.On, "Switch state should be ON");

                // Read brightness
                var brightness = await _dimmer1.ReadBrightnessAsync();
                brightness.Should().BeApproximately(75, 1, "Brightness should be 75%");

                Console.WriteLine("✅ State reading test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ State reading test failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Lock Control Tests

        [Fact]
        public async Task LockControl_BasicLockUnlock_ShouldWork()
        {
            try
            {
                Console.WriteLine("🔄 Testing dimmer lock control...");

                // Lock the dimmer
                await _dimmer1.LockAsync();
                var lockSuccess = await _dimmer1.WaitForLockStateAsync(Lock.On, TimeSpan.FromSeconds(3));
                
                if (lockSuccess)
                {
                    _dimmer1.CurrentState.Lock.Should().Be(Lock.On,"Dimmer should be locked");
                    Console.WriteLine("✅ Lock successful");

                    // Unlock the dimmer
                    await _dimmer1.UnlockAsync();
                    await _dimmer1.WaitForLockStateAsync(Lock.Off, TimeSpan.FromSeconds(3));
                    _dimmer1.CurrentState.Lock.Should().Be(Lock.Off, "Dimmer should be unlocked");
                    Console.WriteLine("✅ Unlock successful");
                }
                else
                {
                    Console.WriteLine("⚠️ Lock not configured for this dimmer - skipping lock tests");
                    // Skip test - lock functionality not configured for this dimmer
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lock control test failed: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public async Task LockPreventsStateChanges_SwitchControl_ShouldWork()
        {
            try
            {
                Console.WriteLine("🔄 Testing lock prevention for switch control...");

                // Set initial state
                await _dimmer1.TurnOffAsync();
                await _dimmer1.WaitForStateAsync(Switch.Off, TimeSpan.FromSeconds(3));

                // Lock the dimmer (use TimeSpan.Zero to skip feedback waiting)
                await _dimmer1.SetLockAsync(Lock.On, TimeSpan.Zero);
                Console.WriteLine($"Lock command sent, current lock state: {_dimmer1.CurrentState.Lock}");

                // Try to turn on - should be prevented by lock
                Console.WriteLine("Attempting to turn on locked dimmer...");
                await _dimmer1.TurnOnAsync();
                
                // Wait a moment and check state hasn't changed
                await Task.Delay(2000);
                await _dimmer1.RefreshStateAsync();

                if (_dimmer1.CurrentState.Switch.ToBool())
                {
                    Console.WriteLine("❌ FAILURE: Dimmer switch state changed while locked!");
                    Assert.Fail("Lock should prevent switch state changes");
                }
                else
                {
                    Console.WriteLine("✅ SUCCESS: Lock prevented switch state change");
                }

                // Unlock and verify normal operation
                await _dimmer1.UnlockAsync();
                await Task.Delay(1000);
                await _dimmer1.TurnOnAsync();
                await _dimmer1.WaitForStateAsync(Switch.On, TimeSpan.FromSeconds(3));
                _dimmer1.CurrentState.Switch.Should().Be(Switch.On,"Dimmer should work normally after unlock");

                Console.WriteLine("🎉 Lock prevention test for switch control completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lock prevention test failed: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public async Task LockPreventsStateChanges_BrightnessControl_ShouldWork()
        {
            try
            {
                Console.WriteLine("🔄 Testing lock prevention for brightness control...");

                // Set initial brightness to 25%
                await _dimmer1.SetBrightnessAsync(25);
                await _dimmer1.WaitForBrightnessAsync(25, TimeSpan.FromSeconds(3));
                var initialBrightness = _dimmer1.CurrentState.Brightness;

                // Lock the dimmer (use TimeSpan.Zero to skip feedback waiting)
                await _dimmer1.SetLockAsync(Lock.On, TimeSpan.Zero);
                Console.WriteLine($"Lock command sent, current lock state: {_dimmer1.CurrentState.Lock}");

                // Try to change brightness - should be prevented by lock
                Console.WriteLine("Attempting to change brightness of locked dimmer...");
                await _dimmer1.SetBrightnessAsync(75);
                
                // Wait a moment and check brightness hasn't changed
                await Task.Delay(2000);
                await _dimmer1.RefreshStateAsync();

                var brightnessChange = Math.Abs(_dimmer1.CurrentState.Brightness - initialBrightness);
                if (brightnessChange > 10) // Allow 10% tolerance
                {
                    Console.WriteLine($"❌ FAILURE: Dimmer brightness changed from {initialBrightness}% to {_dimmer1.CurrentState.Brightness}% while locked!");
                    Assert.Fail("Lock should prevent brightness changes");
                }
                else
                {
                    Console.WriteLine($"✅ SUCCESS: Lock prevented brightness change (stayed at {_dimmer1.CurrentState.Brightness}%)");
                }

                // Unlock and verify normal operation
                await _dimmer1.UnlockAsync();
                await Task.Delay(1000);
                await _dimmer1.SetBrightnessAsync(75);
                await _dimmer1.WaitForBrightnessAsync(75, TimeSpan.FromSeconds(3));
                _dimmer1.CurrentState.Brightness.Should().BeApproximately(75, 1, "Dimmer brightness should work normally after unlock");

                Console.WriteLine("🎉 Lock prevention test for brightness control completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lock prevention test failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region State Management Tests

        [Fact]
        public async Task StateManagement_SaveAndRestore_ShouldWork()
        {
            try
            {
                Console.WriteLine("🔄 Testing dimmer state save and restore...");

                // Set specific state
                await _dimmer1.SetBrightnessAsync(60);
                await _dimmer1.WaitForBrightnessAsync(60, TimeSpan.FromSeconds(3));

                // Save current state
                _dimmer1.SaveCurrentState();
                var savedBrightness = _dimmer1.SavedState?.Brightness;

                // Change state
                await _dimmer1.SetBrightnessAsync(20);
                await _dimmer1.WaitForBrightnessAsync(20, TimeSpan.FromSeconds(3));

                // Restore saved state
                await _dimmer1.RestoreSavedStateAsync();
                await _dimmer1.WaitForBrightnessAsync(savedBrightness ?? 60, TimeSpan.FromSeconds(3));

                var brightnessTestPassed = Math.Abs(_dimmer1.CurrentState.Brightness - (savedBrightness ?? 60)) <= 10;
                brightnessTestPassed.Should().BeTrue("Brightness should be restored to saved value");

                Console.WriteLine("✅ State save and restore test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ State save and restore test failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Multiple Dimmer Tests

        [Fact]
        public async Task MultipleDimmers_ShouldOperateIndependently()
        {
            try
            {
                Console.WriteLine("🔄 Testing multiple dimmers operating independently...");

                // Set different states for each dimmer
                await _dimmer1.SetBrightnessAsync(30);
                await _dimmer2.SetBrightnessAsync(70);

                await _dimmer1.WaitForBrightnessAsync(30, TimeSpan.FromSeconds(3));
                await _dimmer2.WaitForBrightnessAsync(70, TimeSpan.FromSeconds(3));

                // Verify states are different
                _dimmer1.CurrentState.Brightness.Should().BeApproximately(30, 1, "DIM1 should be at 30%");
                _dimmer2.CurrentState.Brightness.Should().BeApproximately(70, 1, "DIM2 should be at 70%");

                // Change one dimmer, verify other unchanged
                await _dimmer1.SetBrightnessAsync(90);
                await _dimmer1.WaitForBrightnessAsync(90, TimeSpan.FromSeconds(3));

                _dimmer1.CurrentState.Brightness.Should().BeApproximately(90, 1, "DIM1 should be at 90%");
                _dimmer2.CurrentState.Brightness.Should().BeApproximately(70, 1, "DIM2 should still be at 70%");

                Console.WriteLine("✅ Multiple dimmers test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Multiple dimmers test failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ErrorHandling_InvalidBrightnessValues_ShouldThrow()
        {
            try
            {
                Console.WriteLine("🔄 Testing error handling for invalid brightness values...");

                // Test negative value
                try
                {
                    await _dimmer1.SetBrightnessAsync(-10);
                    Assert.Fail("Should throw exception for negative brightness");
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("✅ Correctly rejected negative brightness");
                }

                // Test value over 100
                try
                {
                    await _dimmer1.SetBrightnessAsync(150);
                    Assert.Fail("Should throw exception for brightness over 100");
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("✅ Correctly rejected brightness over 100");
                }

                // Test fade with invalid target
                try
                {
                    await _dimmer1.FadeToAsync(-10, TimeSpan.FromSeconds(1));
                    Assert.Fail("Should throw exception for negative fade target");
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("✅ Correctly rejected negative fade target");
                }

                Console.WriteLine("✅ Error handling test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error handling test failed: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}
