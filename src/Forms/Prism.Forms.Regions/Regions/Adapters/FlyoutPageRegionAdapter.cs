using Prism.Ioc;
using Prism.Regions.Behaviors;
using Xamarin.Forms;

namespace Prism.Regions.Adapters
{
    internal class FlyoutPageRegionAdapter : RegionAdapterBase<FlyoutPage>
    {
        private readonly IContainerProvider _container;

        /// <inheritdoc />
        public FlyoutPageRegionAdapter( IRegionBehaviorFactory regionBehaviorFactory, IContainerProvider container )
            : base( regionBehaviorFactory )
        {
            _container = container;
        }

#region Overrides of RegionAdapterBase<FlyoutPage>

        /// <inheritdoc />
        protected override void Adapt( IRegion region, FlyoutPage regionTarget )
        {
            this.SynchronizeItems( region, regionTarget );
        }

        /// <inheritdoc />
        protected override void AttachBehaviors( IRegion region, FlyoutPage regionTarget )
        {
            region.Behaviors.Add( FlyoutPageFlyoutMenuBehavior.BehaviorKey,
                                  new FlyoutPageFlyoutMenuBehavior() { HostControl = regionTarget } );

            base.AttachBehaviors( region, regionTarget );
        }

        /// <inheritdoc />
        protected override IRegion CreateRegion() =>
            _container.Resolve<SingleActiveRegion>();

#endregion

        private void SynchronizeItems( IRegion region, FlyoutPage regionTarget )
        {
            /// So this is messy as Detail is required to have a value, but it could just be a filler.
            if ( regionTarget.Detail != null )
            {
                //region.Add( regionTarget.Detail );
            }
        }
    }
}
