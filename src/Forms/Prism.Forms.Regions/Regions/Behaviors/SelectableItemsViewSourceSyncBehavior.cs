using System;
using System.Collections.Specialized;
using System.Linq;
using Prism.Properties;
using Prism.Regions.Adapters;
using Xamarin.Forms;

namespace Prism.Regions.Behaviors
{
    /// <summary>
    /// Defines the attached behavior that keeps the items of the <see cref="SelectableItemsView"/> host control in synchronization with the <see cref="IRegion"/>.
    /// 
    /// This behavior also makes sure that, if you activate a view in a region, the SelectedItem is set. If you set the SelectedItem or SelectedItems
    /// then this behavior will also call Activate on the selected items. 
    /// <remarks>
    /// When calling Activate on a view, you can only select a single active view at a time. By setting the SelectedItems property, you can set
    /// multiple views to active. 
    /// </remarks>
    /// </summary>
    public class SelectableItemsViewSourceSyncBehavior : RegionBehavior, IHostAwareRegionBehavior
    {
        /// <summary>
        /// Name that identifies the SelectorItemsSourceSyncBehavior behavior in a collection of RegionsBehaviors. 
        /// </summary>
        public static readonly string BehaviorKey = "SelectableItemsViewSourceSyncBehavior";
        private bool updatingActiveViewsInHostControlSelectionChanged;
        private SelectableItemsView hostControl;

        /// <summary>
        /// Gets or sets the <see cref="VisualElement"/> that the <see cref="IRegion"/> is attached to.
        /// </summary>
        /// <value>
        /// A <see cref="VisualElement"/> that the <see cref="IRegion"/> is attached to.
        /// </value>
        /// <remarks>For this behavior, the host control must always be a <see cref="SelectableItemsView"/> or an inherited class.</remarks>
        public VisualElement HostControl
        {
            get
            {
                return this.hostControl;
            }
            set
            {
                this.hostControl = value as SelectableItemsView;
            }
        }

        /// <summary>
        /// Starts to monitor the <see cref="IRegion"/> to keep it in sync with the items of the <see cref="HostControl"/>.
        /// </summary>
        protected override void OnAttach()
        {
            bool itemsSourceIsSet = this.hostControl.ItemsSource != null ||
                                    this.hostControl.IsSet( ItemsView.ItemsSourceProperty );

            if ( itemsSourceIsSet )
            {
                throw new InvalidOperationException(Resources.SelectableItemsViewHasItemsSourceException);
            }

            bool itemTemplateIsSet = this.hostControl.ItemTemplate != null ||
                                     this.hostControl.IsSet( ItemsView.ItemTemplateProperty );

            if ( itemTemplateIsSet )
            {
                throw new InvalidOperationException(Resources.SelectableItemsViewHasItemTemplateException);
            }

            // As ItemsView.ItemSource is the only access to the Items, and it's required to be unset/unbound,
            // there's no need to Synchronize with existing items.
            this.hostControl.ItemsSource = this.Region.Views;
            this.hostControl.ItemTemplate = new RegionItemsSourceTemplate();

            //TODO: Look into monitoring the SelectionModeProperty for Active Views?
            // None = All Views Active,
            // Single = just the single,
            // Multiple = all selected??
            //SelectableItemsView.SelectionModeProperty

            //TODO: Should first item added result in Activate?

            this.hostControl.SelectionChanged += this.HostControlSelectionChanged;
            this.Region.ActiveViews.CollectionChanged += this.ActiveViews_CollectionChanged;
        }

        private void ActiveViews_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.updatingActiveViewsInHostControlSelectionChanged)
            {
                // If we are updating the ActiveViews collection in the HostControlSelectionChanged, that 
                // means the user has set the SelectedItem or SelectedItems himself and we don't need to do that here now
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (this.hostControl.SelectedItem != null
                    && this.hostControl.SelectedItem != e.NewItems[0]
                    && this.Region.ActiveViews.Contains(this.hostControl.SelectedItem))
                {
                    this.Region.Deactivate(this.hostControl.SelectedItem as VisualElement);
                }

                this.hostControl.SelectedItem = e.NewItems[0];
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove &&
                     e.OldItems.Contains(this.hostControl.SelectedItem))
            {
                this.hostControl.SelectedItem = null;
            }
        }

        private void HostControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Record the fact that we are now updating active views in the HostControlSelectionChanged method. 
                // This is needed to prevent the ActiveViews_CollectionChanged() method from firing. 
                this.updatingActiveViewsInHostControlSelectionChanged = true;

                foreach ( VisualElement item in e.PreviousSelection )
                {
                    // check if the view is in both Views and ActiveViews collections (there may be out of sync)
                    if ( this.Region.Views.Contains( item ) && this.Region.ActiveViews.Contains( item ) )
                    {
                        this.Region.Deactivate( item );
                    }
                }

                foreach ( VisualElement item in e.CurrentSelection )
                {
                    if ( this.Region.Views.Contains( item ) && !this.Region.ActiveViews.Contains( item ) )
                    {
                        this.Region.Activate( item );
                    }
                }
            }
            finally
            {
                this.updatingActiveViewsInHostControlSelectionChanged = false;
            }
        }
    }
}
