using System;
using System.Collections.Specialized;
using System.Linq;
using Prism.Commands;
using Prism.Common;
using Prism.Navigation;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Prism.Regions.Behaviors
{
    internal class FlyoutPageFlyoutMenuBehavior : RegionBehavior, IHostAwareRegionBehavior
    {
        /// <summary>Identifies the Page bindable property.</summary>
        /// <remarks>This is assigned to a View that has been wrapped in a <see cref="FlyoutPageFlyoutMenuBehavior"/> so that it
        /// be displayed in a <see cref="MultiPage{T}"/> Region.</remarks>
        private static readonly BindableProperty FlyoutMenuProperty =
            BindableProperty.CreateAttached("FlyoutMenu", typeof(VisualElement), typeof(FlyoutPageFlyoutMenuBehavior), null );

        /// <summary>Gets the page that's associated with the view that's contained in the region. This is a bindable property.</summary>
        private static View GetFlyoutMenu( BindableObject view )
        {
            return (View)view.GetValue( FlyoutMenuProperty );
        }

        /// <summary>Sets the page that's associated with the view that's contained in the region. This is a bindable property.</summary>
        private static void SetFlyoutMenu( BindableObject view, View value )
        {
            view.SetValue( FlyoutMenuProperty, value );
        }

        public static readonly string BehaviorKey = nameof(FlyoutPageFlyoutMenuBehavior);
        private FlyoutPage hostControl;
        private readonly Lazy<IRegion> _menuRegion;

        public FlyoutPageFlyoutMenuBehavior()
        {
            _menuRegion = new Lazy<IRegion>(
                () =>
                {
                    string menuRegionName = Xaml.RegionManager.GetFlyoutRegionName( this.hostControl );

                    return !string.IsNullOrWhiteSpace( menuRegionName ) ?
                        Region.RegionManager.Regions[ menuRegionName ] ??
                            throw new InvalidOperationException(
                                $"Unable to locate Region with name {menuRegionName}" ) :
                        throw new InvalidOperationException(
                            $"Expected {Xaml.RegionManager.FlyoutRegionNameProperty.PropertyName} to be set on FlyoutPage" );
                } );
        }

        protected IRegion MenuRegion
        {
            get { return _menuRegion.Value; }
        }

#region Implementation of IHostAwareRegionBehavior

        /// <inheritdoc />
        public VisualElement HostControl
        {
            get { return this.hostControl; }
            set { this.hostControl = (FlyoutPage)value; }
        }

#endregion

#region Overrides of IRegionBehavior

        /// <inheritdoc />
        protected override void OnAttach()
        {
            void OnFirstItemAdded( object sender, NotifyCollectionChangedEventArgs e )
            {
                if ( e.Action == NotifyCollectionChangedAction.Add && Region.ActiveViews.Count() == 0 )
                {
                    Region.Activate( e.NewItems[ 0 ] as VisualElement );

                    Region.Views.CollectionChanged -= OnFirstItemAdded;
                }
            }

            Region.Views.CollectionChanged += OnFirstItemAdded;

            Region.Views.CollectionChanged += this.Views_CollectionChanged;
            Region.ActiveViews.CollectionChanged += this.ActiveViews_CollectionChanged;

            //TODO: Deal with standard navigation to detail pages. Should they be added to region?
            //hostControl.ChildAdded += this.OnHostControlChildAdded;
        }

#endregion

        private void Views_CollectionChanged( object sender, NotifyCollectionChangedEventArgs e )
        {
            if ( e.Action == NotifyCollectionChangedAction.Add )
            {
                foreach ( VisualElement newItem in e.NewItems )
                {
                    var menuView = (View)FlyoutMenuItemDataTemplate.Instance.CreateContent();

                    menuView.BindingContext = newItem.BindingContext;

                    menuView.GestureRecognizers.Add(
                        new TapGestureRecognizer()
                        {
                            Command = new DelegateCommand<string>(
                                path =>
                                {
                                    Region.RegionManager.RequestNavigate( Region.Name, path );

                                    MvvmHelpers.ViewAndViewModelAction<IFlyoutPageOptions>(
                                        HostControl,
                                        fpo => hostControl.IsPresented = fpo.IsPresentedAfterNavigation );
                                } ),
                            CommandParameter =
                                PageNavigationRegistry.GetPageNavigationInfo( newItem.GetType() ).Name
                        } );

                    SetFlyoutMenu( newItem, menuView );

                    MenuRegion.Add( menuView );
                }
            }
            else if ( e.Action == NotifyCollectionChangedAction.Remove )
            {
                e.OldItems.Cast<VisualElement>()
                 .Select( ve => GetFlyoutMenu( ve ) )
                 .ForEach( m => MenuRegion.Remove( m ) );
            }
        }

        private void ActiveViews_CollectionChanged( object sender, NotifyCollectionChangedEventArgs e )
        {
            if ( e.Action == NotifyCollectionChangedAction.Add )
            {
                hostControl.Detail =
                    e.NewItems[ 0 ] switch
                    {
                        View v => v.Parent as Page ?? MultiPageChildTemplate.WrapInPage( v ),
                        Page p => p,
                        _ => throw new NotSupportedException( "" )
                    };
            }
        }

        private void OnHostControlChildAdded( object sender, ElementEventArgs e )
        {
            // FlyoutPage will only have 2 childern, the Flyout and Detail elements.
            if ( this.HostControl.LogicalChildren.Count > 0 )
            {

            }
        }
    }

    internal class FlyoutMenuItemDataTemplate : DataTemplate
    {
        public static readonly DataTemplate Instance =
            new Lazy<DataTemplate>( () => new FlyoutMenuItemDataTemplate() ).Value;

        private FlyoutMenuItemDataTemplate()
            : base( ViewTemplate )
        { }

        private static View ViewTemplate()
        {
            var view = new StackLayout()
                       {
                           HorizontalOptions = LayoutOptions.FillAndExpand,
                           Padding = new Thickness(15, 10 )
                       };

            var label = new Label()
                        {
                            VerticalOptions = LayoutOptions.FillAndExpand,
                            VerticalTextAlignment = TextAlignment.Center,
                            FontSize = 24
                        };

            // use ViewModel.Title property for text on menuitem.
            label.SetBinding( Label.TextProperty,
                              new Binding( "Title", BindingMode.OneWay ) );

            view.Children.Add( label );

            return view;
        }
    }
}
