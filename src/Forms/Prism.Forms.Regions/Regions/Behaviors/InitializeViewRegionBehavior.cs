using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Prism.Common;
using Prism.Navigation;

namespace Prism.Regions.Behaviors
{
    /// <summary>
    /// Behavior that monitors a <see cref="IRegion"/> object and calls the <see cref="IInitialize"/> and <see cref="IInitializeAsync"/>
    /// interfaces when a view is added to it.
    /// </summary>
    public class InitializeViewRegionBehavior : RegionBehavior
    {
        /// <summary>
        /// The key of this behavior.
        /// </summary>
        public const string BehaviorKey = nameof(InitializeViewRegionBehavior);

#region Overrides of RegionBehavior

        /// <inheritdoc />
        protected override void OnAttach()
        {
            Region.Views.CollectionChanged += this.OnRegionViewsChanged;
        }

#endregion

        private void OnRegionViewsChanged( object sender, NotifyCollectionChangedEventArgs e )
        {
            if ( e.Action == NotifyCollectionChangedAction.Add )
            {
                INavigationParameters np = new NavigationParameters();

                // TODO: Friend assembly?
                //np.GetNavigationParametersInternal()
                //          .Add( KnownInternalParameters.NavigationMode, NavigationMode.New );
                ((INavigationParametersInternal)np).Add("__NavigationMode", NavigationMode.New );

                Task.WhenAll( e.NewItems
                               .Cast<object>()
                               .Select( v => PageUtilities.OnInitializedAsync( v, np ) ) );
            }
        }
    }
}
