using System;
using Prism.Ioc;
using Prism.Regions.Behaviors;
using Xamarin.Forms;

namespace Prism.Regions.Adapters
{
    /// <summary>
    /// Adapter that creates a new <see cref="Region"/> and monitors its
    /// active view to set it on the adapted <see cref="CollectionView"/>.
    /// </summary>
    public class CollectionViewRegionAdapter : RegionAdapterBase<CollectionView>
    {
        private IContainerProvider _container { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="CollectionViewRegionAdapter"/>.
        /// </summary>
        /// <param name="regionBehaviorFactory">The factory used to create the region behaviors to attach to the created regions.</param>
        /// <param name="container">The <see cref="IContainerProvider"/> used to resolve a new Region.</param>
        public CollectionViewRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory, IContainerProvider container)
            : base(regionBehaviorFactory)
        {
            _container = container;
        }

        /// <summary>
        /// Adapts a <see cref="CollectionView"/> to an <see cref="IRegion"/>.
        /// </summary>
        /// <param name="region">The new region being used.</param>
        /// <param name="regionTarget">The object to adapt.</param>
        protected override void Adapt(IRegion region, CollectionView regionTarget)
        {
        }

        /// <summary>
        /// Attach new behaviors.
        /// </summary>
        /// <param name="region">The region being used.</param>
        /// <param name="regionTarget">The object to adapt.</param>
        /// <remarks>
        /// This class attaches the base behaviors and also listens for changes in the
        /// activity of the region or the control selection and keeps the in sync.
        /// </remarks>
        protected override void AttachBehaviors( IRegion region, CollectionView regionTarget )
        {
            if ( region == null )
                throw new ArgumentNullException( nameof(region) );

            if (regionTarget == null)
                throw new ArgumentNullException(nameof(regionTarget));

            // Add the behavior that syncs the items source items with the rest of the items
            region.Behaviors.Add( SelectableItemsViewSourceSyncBehavior.BehaviorKey,
                                  new SelectableItemsViewSourceSyncBehavior() { HostControl = regionTarget } );

            base.AttachBehaviors( region, regionTarget );
        }

        /// <summary>
        /// Creates a new instance of <see cref="IRegion"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="Region"/>.</returns>
        protected override IRegion CreateRegion() =>
            _container.Resolve<Region>();
    }
}
