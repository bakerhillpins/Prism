﻿using System;
using System.Collections.Specialized;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Prism.Regions.Behaviors
{
    /// <summary>
    /// Defines the attached behavior that keeps the CurrentPage of the <see cref="MultiPage{T}"/> host control in synchronization with the <see cref="IRegion"/>.
    /// 
    /// This behavior makes sure that, if you activate a view in a region, the CurrentPage is set. If you set the CurrentPage
    /// then this behavior will also call Activate on the selected page. 
    /// <remarks>
    /// When calling Activate on a view, you can only select a single active view at a time.
    /// </remarks>
    /// </summary>
    public class MultiPageCurrentPageSyncBehavior : RegionBehavior, IHostAwareRegionBehavior
    {
        /// <summary>
        /// Name that identifies the SelectorItemsSourceSyncBehavior behavior in a collection of RegionsBehaviors. 
        /// </summary>
        public static readonly string BehaviorKey = nameof(MultiPageCurrentPageSyncBehavior);
        private bool updatingActiveViewsInHostControlCurrentPageChanged;
        private ISelectablePage hostControl;
        private Page previousPage;

        /// <summary>
        /// Gets or sets the <see cref="VisualElement"/> that the <see cref="IRegion"/> is attached to.
        /// </summary>
        /// <value>
        /// A <see cref="VisualElement"/> that the <see cref="IRegion"/> is attached to.
        /// </value>
        /// <remarks>For this behavior, the host control must always be a <see cref="MultiPage{T}"/> or an inherited class.</remarks>
        public VisualElement HostControl
        {
            get
            {
                return this.hostControl.VisualElement;
            }
            set
            {
                this.hostControl = value.WrapAsSelectablePage();
            }
        }

        /// <summary>
        /// Starts to monitor the <see cref="IRegion"/> to keep it in sync with the items of the <see cref="HostControl"/>.
        /// </summary>
        protected override void OnAttach()
        {
            this.hostControl.PropertyChanging += this.HostControlPropertyChanging;
            this.hostControl.CurrentPageChanged += this.HostControlCurrentPageChanged;
            this.Region.ActiveViews.CollectionChanged += this.ActiveViews_CollectionChanged;
        }

        private void HostControlPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            // CurrentPage is changing, record current value.
            if (e.PropertyName == nameof(ISelectablePage.CurrentPage))
            {
                this.previousPage = this.hostControl.CurrentPage;
            }
        }

        private void HostControlCurrentPageChanged(object sender, EventArgs eventArgs)
        {
            try
            {
                // Record the fact that we are now updating active views in the HostControlSelectionChanged method. 
                // This is needed to prevent the ActiveViews_CollectionChanged() method from firing. 
                this.updatingActiveViewsInHostControlCurrentPageChanged = true;

                // Activate will automatically deactivate a SingleActiveRegion view.
                if (this.hostControl.CurrentPage != null)
                {
                    this.Region.Views
                        .Where( v => v == this.hostControl.CurrentPage ||
                                     v.Parent == this.hostControl.CurrentPage )
                        .ForEach( v => this.Region.Activate( v ) );
                }
                else if ( this.previousPage != null )
                {
                    this.Region.ActiveViews
                        .Where( v => v == this.previousPage || v.Parent == this.previousPage )
                        .ForEach( v => this.Region.Deactivate( v ) );
                }
            }
            finally
            {
                this.updatingActiveViewsInHostControlCurrentPageChanged = false;
            }
        }

        private void ActiveViews_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.updatingActiveViewsInHostControlCurrentPageChanged)
            {
                // If we are updating the ActiveViews collection in the HostControlSelectionChanged, that 
                // means the user has set the SelectedItem or SelectedItems himself and we don't need to do that here now
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                this.hostControl.CurrentPage = e.NewItems[ 0 ] switch
                                               {
                                                   Page p => p,
                                                   VisualElement ve => ve.Parent as Page
                                               };
            }
        }
    }

    /// <summary>
    /// DataTemplate used to describe the way in which <see cref="View"/>'s injected into the region must be wrapped up
    /// so that they can be displayed as a Page.
    /// </summary>
    internal class MultiPageChildTemplate : DataTemplate
    {
        /// <summary>
        /// Wrap the current <see cref="View"/> in a page.
        /// </summary>
        /// <param name="view">Region View to be wrapped.</param>
        /// <returns></returns>
        public static ContentPage WrapInPage( View view )
        {
            ContentPage wrapper = (ContentPage)MultiPageChildTemplate.Instance.CreateContent();

            wrapper.Content = view;

            return wrapper;
        }

        public static readonly DataTemplate Instance =
            new Lazy<DataTemplate>( () => new MultiPageChildTemplate() ).Value;

        private MultiPageChildTemplate()
            : base( ViewTemplate )
        { }

        private static VisualElement ViewTemplate()
        {
            var view = new ContentPage();

            // NOTE: Binding the Parent ContentPage.BindingContext to the View's ViewModel allows IApplicationLifecycleAware support.
            // Promote the View's BindingContext/ViewModel up to the parent Page's BindingContext so that
            // wrapped Views are consistent with <see cref="Page"/>s that are added to the region directly.
            view.SetBinding(
                BindableObject.BindingContextProperty,
                new Binding( "Content.BindingContext",
                             BindingMode.OneWay,
                             source: new RelativeBindingSource( RelativeBindingSourceMode.Self ) ) );
            return view;
        }
    }

    /// <summary>
    /// An abstraction for the <see cref="MultiPage{T}"/> to remove the type parameter from the Behavior code
    /// provide a simplified <see cref="Page"/> based interface.
    /// </summary>
    internal interface ISelectablePage
    {
        VisualElement VisualElement { get; }

        Page CurrentPage { get; set; }

        event PropertyChangingEventHandler PropertyChanging;

        event EventHandler CurrentPageChanged;
    }

    /// <summary>
    /// Factory pattern to create the <see cref="ISelectablePage"/> implementation.
    /// </summary>
    internal static class SelectablePageFactory
    {
        public static ISelectablePage WrapAsSelectablePage( this VisualElement element )
        {
            return element switch
                   {
                       MultiPage<Page> multiPage => new SelectableWrapper<Page>( multiPage ),
                       MultiPage<ContentPage> multiContent => new SelectableWrapper<ContentPage>( multiContent ),
                       _ => throw new NotSupportedException()
                   };
        }

        /// <summary>
        /// Forward to the wrapped <see cref="MultiPage{T}"/> implementation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class SelectableWrapper<T> : ISelectablePage where T : Page
        {
            private readonly MultiPage<T> _multiPage;

            public SelectableWrapper( MultiPage<T> multiPage )
            {
                _multiPage = multiPage;
            }

#region Implementation of ISelectableItems

            /// <inheritdoc />
            public VisualElement VisualElement
            {
                get { return this._multiPage; }
            }

            /// <inheritdoc />
            public Page CurrentPage
            {
                get { return this._multiPage.CurrentPage; }
                set { this._multiPage.CurrentPage = (T)value; }
            }

            /// <inheritdoc />
            public event PropertyChangingEventHandler PropertyChanging
            {
                add { this._multiPage.PropertyChanging += value; }
                remove { this._multiPage.PropertyChanging -= value; }
            }

            /// <inheritdoc />
            public event EventHandler CurrentPageChanged
            {
                add { this._multiPage.CurrentPageChanged += value; }
                remove { this._multiPage.CurrentPageChanged -= value; }
            }

#endregion
        }
    }
}

