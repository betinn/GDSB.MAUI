using GDSB.Domain.Entities;
using GDSB.MAUI.Tests.Fakes;
using GDSB.MAUI.ViewModels;
using Xunit;

namespace GDSB.MAUI.Tests
{
    public class VaultViewModelTests
    {
        private const string Location = "content://fake-vault";
        private const string Password = "senha-do-cofre-123";

        private sealed class Sut
        {
            public FakeClipboardService ClipboardService { get; } = new();
            public FakeAlertService AlertService { get; } = new();
            public FakeProfileFileService ProfileFileService { get; } = new();
            public FakeNavigationService NavigationService { get; } = new();
            public FakeAppLauncherService AppLauncherService { get; } = new();
            public FakeVaultSessionService VaultSessionService { get; } = new();
            public VaultViewModel ViewModel { get; }

            public Sut()
            {
                ViewModel = new VaultViewModel(ClipboardService, AlertService, ProfileFileService, NavigationService, AppLauncherService, VaultSessionService);
            }

            public void LoadProfile(Profile profile)
            {
                ViewModel.ApplyQueryAttributes(new Dictionary<string, object>
                {
                    ["Profile"] = profile,
                    ["Location"] = Location,
                    ["Password"] = Password,
                });
            }
        }

        private static Profile ProfileWithBoxes(params SecretBox[] boxes) => new()
        {
            Nome = "Cofre de teste",
            Boxes = boxes.ToList(),
        };

        private static SecretBox Box(string name, bool favorito = false) => new()
        {
            BoxName = name,
            Url = $"{name}.com",
            User = "user",
            Pass = "pass",
            Favorito = favorito,
        };

        [Fact]
        public void ApplyQueryAttributes_SetsVaultNameAndItems()
        {
            var sut = new Sut();
            sut.LoadProfile(ProfileWithBoxes(Box("Netflix"), Box("Github")));

            Assert.Equal("Cofre de teste", sut.ViewModel.VaultName);
            Assert.Equal(2, sut.ViewModel.Items.Count);
        }

        [Fact]
        public void SearchText_FiltersItemsByName()
        {
            var sut = new Sut();
            sut.LoadProfile(ProfileWithBoxes(Box("Netflix"), Box("Github")));

            sut.ViewModel.SearchText = "net";

            Assert.Single(sut.ViewModel.Items);
            Assert.Equal("Netflix", sut.ViewModel.Items[0].BoxName);
        }

        [Fact]
        public void FilterFavoritesOnly_ShowsOnlyFavorites()
        {
            var sut = new Sut();
            sut.LoadProfile(ProfileWithBoxes(Box("Netflix", favorito: true), Box("Github")));

            sut.ViewModel.FilterFavoritesOnly = true;

            Assert.Single(sut.ViewModel.Items);
            Assert.Equal("Netflix", sut.ViewModel.Items[0].BoxName);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_TogglesAndPersists()
        {
            var sut = new Sut();
            var box = Box("Netflix");
            sut.LoadProfile(ProfileWithBoxes(box));
            var item = sut.ViewModel.Items[0];

            await sut.ViewModel.ToggleFavoriteCommand.ExecuteAsync(item);

            Assert.True(box.Favorito);
            Assert.Single(sut.ProfileFileService.SaveCalls);
        }

        [Fact]
        public void AddNewItem_EntersEditingModeWithClearedFields()
        {
            var sut = new Sut();
            sut.LoadProfile(ProfileWithBoxes());

            sut.ViewModel.AddNewItemCommand.Execute(null);

            Assert.True(sut.ViewModel.IsEditingItem);
            Assert.True(sut.ViewModel.IsEditorOpen);
            Assert.Null(sut.ViewModel.SelectedItem);
            Assert.Equal(string.Empty, sut.ViewModel.EditBoxName);
        }

        [Fact]
        public async Task SaveItemAsync_MissingName_SetsValidationError()
        {
            var sut = new Sut();
            sut.LoadProfile(ProfileWithBoxes());
            sut.ViewModel.AddNewItemCommand.Execute(null);
            sut.ViewModel.EditBoxName = string.Empty;
            sut.ViewModel.EditPassword = "pass";

            await sut.ViewModel.SaveItemCommand.ExecuteAsync(null);

            Assert.Equal("Informe um nome para o item.", sut.ViewModel.ValidationError);
            Assert.Empty(sut.ProfileFileService.SaveCalls);
        }

        [Fact]
        public async Task SaveItemAsync_NewItem_AddsToProfileAndPersists()
        {
            var sut = new Sut();
            var profile = ProfileWithBoxes();
            sut.LoadProfile(profile);
            sut.ViewModel.AddNewItemCommand.Execute(null);
            sut.ViewModel.EditBoxName = "Netflix";
            sut.ViewModel.EditPassword = "segredo";

            await sut.ViewModel.SaveItemCommand.ExecuteAsync(null);

            Assert.Single(profile.Boxes);
            Assert.Equal("Netflix", profile.Boxes[0].BoxName);
            Assert.False(sut.ViewModel.IsEditorOpen);
            Assert.Single(sut.ProfileFileService.SaveCalls);
        }

        [Fact]
        public void EditItem_PopulatesFieldsFromExistingBox()
        {
            var sut = new Sut();
            sut.LoadProfile(ProfileWithBoxes(Box("Netflix")));
            var item = sut.ViewModel.Items[0];

            sut.ViewModel.EditItemCommand.Execute(item);

            Assert.True(sut.ViewModel.IsEditingItem);
            Assert.Equal("Netflix", sut.ViewModel.EditBoxName);
            Assert.Equal("pass", sut.ViewModel.EditPassword);
        }

        [Fact]
        public async Task ConfirmDeleteAsync_RemovesItemAndPersists()
        {
            var sut = new Sut();
            var profile = ProfileWithBoxes(Box("Netflix"));
            sut.LoadProfile(profile);
            var item = sut.ViewModel.Items[0];

            await sut.ViewModel.ConfirmDeleteCommand.ExecuteAsync(item);

            Assert.Empty(profile.Boxes);
            Assert.Empty(sut.ViewModel.Items);
            Assert.Single(sut.ProfileFileService.SaveCalls);
        }

        [Fact]
        public async Task CopyUserAsync_CopiesUserAndRaisesToast()
        {
            var sut = new Sut();
            sut.LoadProfile(ProfileWithBoxes(Box("Netflix")));
            var item = sut.ViewModel.Items[0];
            string? toastMessage = null;
            sut.ViewModel.ToastRequested += (_, message) => toastMessage = message;

            await sut.ViewModel.CopyUserCommand.ExecuteAsync(item);

            Assert.Equal(new[] { "user" }, sut.ClipboardService.Calls);
            Assert.Equal("Usuário copiado", toastMessage);
        }

        [Fact]
        public async Task CopyPasswordAsync_CopiesPasswordAndRaisesToast()
        {
            var sut = new Sut();
            sut.LoadProfile(ProfileWithBoxes(Box("Netflix")));
            var item = sut.ViewModel.Items[0];
            string? toastMessage = null;
            sut.ViewModel.ToastRequested += (_, message) => toastMessage = message;

            await sut.ViewModel.CopyPasswordCommand.ExecuteAsync(item);

            Assert.Equal(new[] { "pass" }, sut.ClipboardService.Calls);
            Assert.Equal("Senha copiada", toastMessage);
        }

        [Fact]
        public async Task OpenUrlAsync_OpensHttpsUrlThroughLauncher()
        {
            var sut = new Sut();
            sut.LoadProfile(ProfileWithBoxes(Box("Netflix")));
            var item = sut.ViewModel.Items[0];

            await sut.ViewModel.OpenUrlCommand.ExecuteAsync(item);

            var opened = Assert.Single(sut.AppLauncherService.Calls);
            Assert.Equal("https://netflix.com/", opened.ToString());
        }

        [Fact]
        public async Task OpenUrlAsync_LauncherThrows_ShowsAlert()
        {
            var sut = new Sut();
            sut.LoadProfile(ProfileWithBoxes(Box("Netflix")));
            var item = sut.ViewModel.Items[0];
            sut.AppLauncherService.OpenException = new InvalidOperationException("no browser");

            await sut.ViewModel.OpenUrlCommand.ExecuteAsync(item);

            Assert.Single(sut.AlertService.Calls);
        }

        [Fact]
        public async Task GoHomeAsync_ClearsVaultSession()
        {
            var sut = new Sut();
            sut.LoadProfile(ProfileWithBoxes());

            await sut.ViewModel.GoHomeCommand.ExecuteAsync(null);

            Assert.Equal(1, sut.VaultSessionService.ClearCallCount);
            Assert.Equal(1, sut.NavigationService.GoHomeCallCount);
        }

        [Fact]
        public void OnSizeChanged_TogglesIsWideLayout()
        {
            var sut = new Sut();

            sut.ViewModel.OnSizeChanged(900);
            Assert.True(sut.ViewModel.IsWideLayout);

            sut.ViewModel.OnSizeChanged(400);
            Assert.False(sut.ViewModel.IsWideLayout);
        }
    }
}
