using FluentAssertions;
using KnxModel;
using KnxService;
using System.Threading.Tasks;

namespace KnxTest
{
    [Collection("KnxService collection")]
    public class UnitTest1
    {
        private IKnxService _knxService { get; }

        public UnitTest1(KnxServiceFixture fixture)
        {
            _knxService = fixture.KnxService;
        }

        [Theory]
        [InlineData("1", "1", "01", "101")]
        [InlineData("1", "1", "02", "102")]
        [InlineData("1", "1", "03", "103")]
        [InlineData("1", "1", "04", "104")]
        [InlineData("1", "1", "05", "105")]
        [InlineData("1", "1", "06", "106")]
        [InlineData("1", "1", "07", "107")]
        [InlineData("1", "1", "08", "108")]
        [InlineData("1", "1", "09", "109")]
        [InlineData("1", "1", "10", "110")]
        [InlineData("1", "1", "11", "111")]
        [InlineData("1", "1", "12", "112")]
        [InlineData("1", "1", "13", "113")]
        [InlineData("1", "1", "14", "114")]
        [InlineData("1", "1", "15", "115")]
        public async Task CanToggleLightAndRestoreOriginalState(string mainGroup, string middleGroup, string subGroupControl, string subGroupReceived)
        {
            
            // Arrange
            var controlAddress = $"{mainGroup}/{middleGroup}/{subGroupControl}";
            var feedbackAddress = $"{mainGroup}/{middleGroup}/{subGroupReceived}";

            var initialValue = await _knxService.RequestGroupValue(mainGroup, middleGroup, subGroupReceived);

            var expectedValues = new[] { "0", "1" };
            Assert.Contains(initialValue, expectedValues);

            // Invert the current value for the test
            var initialBool = (initialValue == "1");
            var testValue = !initialBool;

            async Task<bool> SendAndVerify( bool value)
            {
                var taskCompletionSource = new TaskCompletionSource<KnxGroupEventArgs>();
                EventHandler<KnxGroupEventArgs> handler = (sender, args) =>
                {
                    if (args.Destination == feedbackAddress)
                    {
                        taskCompletionSource.SetResult(args);
                    }
                };
                // Subscribe to the event
                _knxService.GroupMessageReceived += handler;
                try
                {
                    // Write the group value
                    _knxService.WriteGroupValue(controlAddress, value);
                    var response = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(2));
                    // Assert
                    return response.Destination == feedbackAddress && response.Value == (value ? "1" : "0");
                }
                finally
                {
                    _knxService.GroupMessageReceived -= handler;
                }
            }

            // Assert
            // Act + Assert (1): Zmiana na przeciwną wartość
            var resultChanged = await SendAndVerify(testValue);
            Assert.True(resultChanged, $"Expected feedback value {(testValue ? "1" : "0")} not received.");
            Thread.Sleep(1000); // Wait for the change to propagate
            // Act + Assert (2): Przywrócenie oryginalnej wartości
            var resultRestored = await SendAndVerify(initialBool);
            Assert.True(resultRestored, $"Failed to restore original state {(initialBool ? "1" : "0")}.");

        }


        [Theory]
        [InlineData("1", "1", "101")]
        [InlineData("1", "1", "102")]
        [InlineData("1", "1", "103")]
        [InlineData("1", "1", "104")]
        [InlineData("1", "1", "105")]
        [InlineData("1", "1", "106")]
        [InlineData("1", "1", "107")]
        [InlineData("1", "1", "108")]
        [InlineData("1", "1", "109")]
        [InlineData("1", "1", "110")]
        [InlineData("1", "1", "111")]
        [InlineData("1", "1", "112")]
        [InlineData("1", "1", "113")]
        [InlineData("1", "1", "114")]
        [InlineData("1", "1", "115")]
        //[InlineData("1", "1", "116")]
        //[InlineData("1", "1", "117")]
        //[InlineData("1", "1", "118")]
        //[InlineData("1", "1", "119")]
        //[InlineData("1", "1", "120")]
        public async Task CanReadLightFeedback(string mainGroup, string middleGroup, string subGroup)
        {

            // Arrange

            var expectedDestination = $"{mainGroup}/{middleGroup}/{subGroup}";

            var taskCompletionSource = new TaskCompletionSource<KnxGroupEventArgs>();

            EventHandler<KnxGroupEventArgs> handler = (sender, args) =>
            {
                if (args.Destination == expectedDestination)
                {
                    taskCompletionSource.SetResult(args);
                }
            };

            _knxService.GroupMessageReceived += handler;
            // Act

            try
            {
                var currentValue = await _knxService.RequestGroupValue(mainGroup, middleGroup, subGroup);

                var expectedValues = new[] { "0", "1" };

                var response = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(2));

                // Assert
                Assert.Equal(expectedDestination, response.Destination);
                Assert.Contains(response.Value, expectedValues);
            }
            finally
            {
                _knxService.GroupMessageReceived -= handler;
            }
        }

        [Theory]
        [InlineData("4", "2", "101")]
        [InlineData("4", "2", "102")]
        [InlineData("4", "2", "103")]
        [InlineData("4", "2", "104")]
        [InlineData("4", "2", "105")]
        [InlineData("4", "2", "106")]
        [InlineData("4", "2", "107")]
        [InlineData("4", "2", "108")]
        [InlineData("4", "2", "109")]
        [InlineData("4", "2", "110")]
        [InlineData("4", "2", "111")]
        [InlineData("4", "2", "112")]
        [InlineData("4", "2", "113")]
        [InlineData("4", "2", "114")]
        [InlineData("4", "2", "115")]
        [InlineData("4", "2", "116")]
        [InlineData("4", "2", "117")]
        [InlineData("4", "2", "118")]
        public async Task CanReadShutterPosition(string mainGroup, string middleGroup, string subGroup)
        {

            // Arrange

            var expectedDestination = $"{mainGroup}/{middleGroup}/{subGroup}";

            var taskCompletionSource = new TaskCompletionSource<KnxGroupEventArgs>();

            EventHandler<KnxGroupEventArgs> handler = (sender, args) =>
            {
                if (args.Destination == expectedDestination)
                {
                    taskCompletionSource.SetResult(args);
                }
            };

            _knxService.GroupMessageReceived += handler;
            // Act

            try
            {
                var currentValue = await _knxService.RequestGroupValue<Percent>(expectedDestination);

                var expectedValues = new[] { "0", "1" };

                var response = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(2));

                // Assert
                Assert.Equal(expectedDestination, response.Destination);
                currentValue.Value.Should().BeInRange(0, 100); // Assuming shutter position is a percentage value between 0 and 100

            }
            finally
            {
                _knxService.GroupMessageReceived -= handler;
            }
        }
    }
    public class KnxServiceFixture : IDisposable
    {
        public IKnxService KnxService { get; private set; }
        public KnxServiceFixture()
        {
            KnxService = new KnxService.KnxService();
        }
        public void Dispose()
        {
            KnxService.Dispose();
        }
    }


    [CollectionDefinition("KnxService collection")]
    public class KnxServiceCollection : ICollectionFixture<KnxServiceFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}