using Prism.DI.Forms.Tests.Mocks.ViewModels;
using Prism.DI.Forms.Tests.Mocks.Views;
using Prism.Ioc;
using Xunit;
using Xunit.Abstractions;

namespace Prism.DI.Forms.Tests.Fixtures.Regions
{
    public class RegionFixture : FixtureBase, IPlatformInitializer
    {
        private PrismApplicationMock _app;

        public RegionFixture(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            _app = new PrismApplicationRegionMock<Issue2415Page>(this);
        }

        //TODO: The old Navigation and the new Region paradigms do not merge at all. Revisit this Test.
        [Fact]
        public void RegionWorksWhenContentViewIsTopChild()
        {
            Assert.NotNull(_app.MainPage);
            Assert.IsType<Issue2415Page>(_app.MainPage);

            var vm = _app.MainPage.BindingContext as Issue2415PageViewModel;

            Assert.NotNull(vm.Result);
            Assert.True(vm.Result.Result);
        }

        void IPlatformInitializer.RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(_testOutputHelper);
            containerRegistry.RegisterForNavigation<Issue2415Page, Issue2415PageViewModel>();
            containerRegistry.RegisterForRegionNavigation<Issue2415RegionView, Issue2415RegionViewModel>();
        }
    }
}
