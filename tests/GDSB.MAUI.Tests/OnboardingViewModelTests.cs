using GDSB.MAUI.Tests.Fakes;
using GDSB.MAUI.ViewModels;
using Xunit;

namespace GDSB.MAUI.Tests
{
    public class OnboardingViewModelTests
    {
        private static (OnboardingViewModel ViewModel, FakePreferencesService Preferences) Build()
        {
            var preferences = new FakePreferencesService();
            return (new OnboardingViewModel(preferences, new FakeLocalizationService()), preferences);
        }

        [Fact]
        public void Slides_AreThreeAndAllFilled()
        {
            var (viewModel, _) = Build();

            Assert.Equal(3, viewModel.Slides.Count);
            Assert.Equal(viewModel.Slides.Count, viewModel.SlideCount);
            Assert.All(viewModel.Slides, slide =>
            {
                Assert.False(string.IsNullOrWhiteSpace(slide.Title));
                Assert.False(string.IsNullOrWhiteSpace(slide.Body));
            });
        }

        [Fact]
        public void MaybeShowOnFirstRun_NeverSeen_Shows()
        {
            var (viewModel, _) = Build();

            viewModel.MaybeShowOnFirstRun();

            Assert.True(viewModel.IsVisible);
            Assert.Equal(0, viewModel.CurrentIndex);
        }

        [Fact]
        public void MaybeShowOnFirstRun_AlreadySeen_StaysHidden()
        {
            var (viewModel, preferences) = Build();
            preferences.SetBool(OnboardingViewModel.SeenPreferenceKey, true);

            viewModel.MaybeShowOnFirstRun();

            Assert.False(viewModel.IsVisible);
        }

        // InitializeAsync roda de novo a cada Window.Resumed - reabrir não pode jogar quem estava
        // no slide 2 de volta pro 1.
        [Fact]
        public void MaybeShowOnFirstRun_AlreadyOpenOnAnotherSlide_KeepsPosition()
        {
            var (viewModel, _) = Build();
            viewModel.ShowFromStart();
            viewModel.AdvanceCommand.Execute(null);

            viewModel.MaybeShowOnFirstRun();

            Assert.True(viewModel.IsVisible);
            Assert.Equal(1, viewModel.CurrentIndex);
        }

        [Fact]
        public void AdvanceCommand_WalksThroughSlidesThenFinishes()
        {
            var (viewModel, preferences) = Build();
            viewModel.ShowFromStart();

            viewModel.AdvanceCommand.Execute(null);
            Assert.Equal(1, viewModel.CurrentIndex);
            Assert.True(viewModel.IsVisible);

            viewModel.AdvanceCommand.Execute(null);
            Assert.Equal(2, viewModel.CurrentIndex);
            Assert.True(viewModel.IsLastSlide);
            Assert.True(viewModel.IsVisible);

            viewModel.AdvanceCommand.Execute(null);
            Assert.False(viewModel.IsVisible);
            Assert.True(preferences.GetBool(OnboardingViewModel.SeenPreferenceKey, false));
        }

        [Fact]
        public void SkipCommand_MarksAsSeenAndCloses()
        {
            var (viewModel, preferences) = Build();
            viewModel.ShowFromStart();

            viewModel.SkipCommand.Execute(null);

            Assert.False(viewModel.IsVisible);
            Assert.True(preferences.GetBool(OnboardingViewModel.SeenPreferenceKey, false));
        }

        [Fact]
        public void CurrentSlide_FollowsCurrentIndex()
        {
            var (viewModel, _) = Build();

            Assert.Equal(viewModel.Slides[0], viewModel.CurrentSlide);

            viewModel.AdvanceCommand.Execute(null);

            Assert.Equal(viewModel.Slides[1], viewModel.CurrentSlide);
        }

        [Fact]
        public void ShowSkip_IsHiddenOnlyOnTheLastSlide()
        {
            var (viewModel, _) = Build();

            Assert.True(viewModel.ShowSkip);
            Assert.Equal("Onboarding_AdvanceButtonNext", viewModel.AdvanceButtonText);

            viewModel.AdvanceCommand.Execute(null);
            viewModel.AdvanceCommand.Execute(null);

            Assert.False(viewModel.ShowSkip);
            Assert.Equal("Onboarding_AdvanceButtonFinish", viewModel.AdvanceButtonText);
        }

        // O link "Como funciona?" é caminho de revisão: quem pediu pra rever quer rever, mesmo já
        // tendo marcado como visto.
        [Fact]
        public void ShowFromStart_AfterBeingSeen_ShowsAgainFromTheFirstSlide()
        {
            var (viewModel, preferences) = Build();
            preferences.SetBool(OnboardingViewModel.SeenPreferenceKey, true);
            viewModel.CurrentIndex = 2;

            viewModel.ShowFromStart();

            Assert.True(viewModel.IsVisible);
            Assert.Equal(0, viewModel.CurrentIndex);
        }
    }
}
