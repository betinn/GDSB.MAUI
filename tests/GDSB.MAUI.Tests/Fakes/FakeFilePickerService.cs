using GDSB.MAUI.Interfaces;

namespace GDSB.MAUI.Tests.Fakes
{
    internal sealed class FakeFilePickerService : IFilePickerService
    {
        public string? PickFileNameResult { get; set; } = "content://fake-vault";

        public Exception? PickFileNameException { get; set; }

        public string? PickSaveLocationResult { get; set; } = "content://fake-new-vault";

        public Exception? PickSaveLocationException { get; set; }

        public Task<string> PickFileNameAsync() =>
            PickFileNameException is not null
                ? throw PickFileNameException
                : Task.FromResult(PickFileNameResult!);

        public Task<string> PickSaveLocationAsync(string suggestedName) =>
            PickSaveLocationException is not null
                ? throw PickSaveLocationException
                : Task.FromResult(PickSaveLocationResult!);
    }
}
