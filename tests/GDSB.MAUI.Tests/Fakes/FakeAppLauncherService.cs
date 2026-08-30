using GDSB.MAUI.Services;

namespace GDSB.MAUI.Tests.Fakes
{
    internal sealed class FakeAppLauncherService : IAppLauncherService
    {
        public List<Uri> Calls { get; } = new();

        public Exception? OpenException { get; set; }

        public Task OpenAsync(Uri uri)
        {
            if (OpenException is not null)
                throw OpenException;

            Calls.Add(uri);
            return Task.CompletedTask;
        }
    }
}
