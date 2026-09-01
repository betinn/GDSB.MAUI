using GDSB.MAUI.Interfaces;

namespace GDSB.MAUI.Tests.Fakes
{
    internal sealed class FakeFilePickerService : IFilePickerService
    {
        public PickedFile? PickFileNameResult { get; set; } = new("content://fake-vault", "cofre.GDSBX");

        public Exception? PickFileNameException { get; set; }

        public string? PickSaveLocationResult { get; set; } = "content://fake-new-vault";

        public Exception? PickSaveLocationException { get; set; }

        public Task<PickedFile?> PickFileNameAsync() =>
            PickFileNameException is not null
                ? throw PickFileNameException
                : Task.FromResult(PickFileNameResult);

        public Task<string> PickSaveLocationAsync(string suggestedName) =>
            PickSaveLocationException is not null
                ? throw PickSaveLocationException
                : Task.FromResult(PickSaveLocationResult!);
    }
}
