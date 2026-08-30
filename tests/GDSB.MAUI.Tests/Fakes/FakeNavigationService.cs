using GDSB.MAUI.Services;

namespace GDSB.MAUI.Tests.Fakes
{
    internal sealed class FakeNavigationService : INavigationService
    {
        public List<(string Route, IDictionary<string, object>? Parameters)> NavigateToCalls { get; } = new();

        public List<(string Route, IDictionary<string, object>? Parameters)> NavigateToRootCalls { get; } = new();

        public int GoHomeCallCount { get; private set; }

        public int GoBackCallCount { get; private set; }

        public Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            NavigateToCalls.Add((route, parameters));
            return Task.CompletedTask;
        }

        public Task NavigateToRootAsync(string route, IDictionary<string, object>? parameters = null)
        {
            NavigateToRootCalls.Add((route, parameters));
            return Task.CompletedTask;
        }

        public Task GoHomeAsync()
        {
            GoHomeCallCount++;
            return Task.CompletedTask;
        }

        public Task GoBackAsync()
        {
            GoBackCallCount++;
            return Task.CompletedTask;
        }
    }
}
