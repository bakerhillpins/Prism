using Prism.Ioc;
using Xamarin.Forms;

namespace Prism.DI.Forms.Tests
{
    public class PrismApplicationRegionMock<T> : PrismApplicationMock where T : Page
    {
        public PrismApplicationRegionMock( IPlatformInitializer platformInitializer )
            : base( platformInitializer )
        {}

        protected override void RegisterTypes( IContainerRegistry containerRegistry )
        {
            containerRegistry.RegisterRegionServices();
        }

#region Overrides of PrismApplicationBase

        /// <inheritdoc />
        protected override Page CreateShell()
        {
            return Container.Resolve<T>();
        }

#endregion
    }
}
