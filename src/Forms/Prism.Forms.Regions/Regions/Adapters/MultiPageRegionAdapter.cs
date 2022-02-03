using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Prism.Ioc;
using Prism.Properties;
using Prism.Regions.Behaviors;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Prism.Regions.Adapters
{
    /// <summary>
    /// Adapter that creates a new <see cref="SingleActiveRegion"/> and monitors its active view to apply to the adapted
    /// <see cref="MultiPage{T}"/>.
    /// <typeparam name="T">The type of pages supported as Children of the <see cref="MultiPage{T}"/>.</typeparam>
    /// </summary>
    public class MultiPageRegionAdapter<T> : RegionAdapterBase<MultiPage<T>> where T : Page
    {
        private IContainerProvider _container { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MultiPageRegionAdapter{T}"/>.
        /// </summary>
        /// <param name="regionBehaviorFactory">The factory used to create the region behaviors to attach to the created regions.</param>
        /// <param name="container">The <see cref="IContainerProvider"/> used to resolve a new Region.</param>
        public MultiPageRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory, IContainerProvider container)
            : base(regionBehaviorFactory)
        {
            _container = container;
        }


#region Overrides of RegionAdapterBase<MultiPage<T>>

        /// <summary>
        /// Adapts a <see cref="MultiPage{T}"/> to an <see cref="IRegion"/>.
        /// </summary>
        /// <param name="region">The new region being used.</param>
        /// <param name="regionTarget">The object to adapt.</param>
        protected override void Adapt(IRegion region, MultiPage<T> regionTarget)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            if (regionTarget == null)
                throw new ArgumentNullException(nameof(regionTarget));

            bool itemsSourceIsSet = regionTarget.ItemsSource != null ||
                                    regionTarget.IsSet( ItemsView.ItemsSourceProperty );

            if ( itemsSourceIsSet )
            {
                throw new InvalidOperationException( Resources.MultiPageHasItemsSourceException );
            }

            bool itemTemplateIsSet = regionTarget.ItemTemplate != null ||
                                     regionTarget.IsSet( ItemsView.ItemTemplateProperty );

            if ( itemTemplateIsSet )
            {
                throw new InvalidOperationException( Resources.MultiPageHasItemTemplateException);
            }

            // Can only support page types: ContentPage, NavigationPage in addition to Region "Views".
            this.SynchronizeItems(region, regionTarget);

            region.Views.CollectionChanged +=
                (o, a) => this.Views_CollectionChanged(regionTarget, a);
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
        protected override void AttachBehaviors(IRegion region, MultiPage<T> regionTarget)
        {
            // Add the behavior that syncs the CurrentPage with the ActiveView.
            region.Behaviors.Add( MultiPageCurrentPageSyncBehavior.BehaviorKey,
                                  new MultiPageCurrentPageSyncBehavior() { HostControl = regionTarget });

            base.AttachBehaviors(region, regionTarget);
        }

        /// <summary>
        /// Creates a new instance of <see cref="IRegion"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="Region"/>.</returns>
        protected override IRegion CreateRegion() =>
            _container.Resolve<SingleActiveRegion>();

#endregion

        private void SynchronizeItems( IRegion region, MultiPage<T> regionTarget )
        {
            // save existing pages/"views" to include in region.
            List<VisualElement> existingItems = new(regionTarget.Children);

            foreach ( VisualElement view in region.Views )
            {
                regionTarget.Children.Add(
                    (T)( view switch
                         {
                             View v => MultiPageChildTemplate.WrapInPage( v ),

                             //BUG? CarouselPage only supports Content pages. Should this be flagged here?
                             Page p and (ContentPage or NavigationPage) => p,
                             _ => throw new NotSupportedException( "" )
                         } ) );
            }

            foreach ( VisualElement existingItem in existingItems )
            {
                region.Add( existingItem );
            }
        }

        private void Views_CollectionChanged( MultiPage<T> regionTarget, NotifyCollectionChangedEventArgs e )
        {
            if ( e.Action == NotifyCollectionChangedAction.Add )
            {
                int startIndex = e.NewStartingIndex;
                foreach ( VisualElement newItem in e.NewItems )
                {
                    Page toAdd = newItem switch
                                 {
                                     View v => MultiPageChildTemplate.WrapInPage( v ),
                                     Page p and (ContentPage or NavigationPage) => p,
                                     _ => throw new NotSupportedException( "" )
                                 };

                    regionTarget.Children.Insert( startIndex, (T)toAdd );
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                e.OldItems
                 .Cast<VisualElement>()
                 .Select(o => o is Page p ? p : MultiPageCurrentPageSyncBehavior.GetPage(o))
                 .ForEach(p => regionTarget.Children.Remove((T)p));
            }
        }
    }
}
