using System;
using Prism.Navigation;
using Prism.Regions.Navigation;
using Xamarin.Forms;

namespace Prism.Common
{
    internal static class MvvmHelpers
    {
        public static void AutowireViewModel(object view) => PageUtilities.SetAutowireViewModel((VisualElement)view);

        public static bool ViewAndViewModelAction<T>( object view, Action<T> action )
            where T : class
        {
            bool actionExecuted = false;

            if ( view is T viewAsT )
            {
                actionExecuted = true;
                action(viewAsT);
            }

            if ( view is BindableObject { BindingContext: T vmAsT } )
            {
                actionExecuted = true;
                action(vmAsT);
            }

            return actionExecuted;
        }

        public static T GetImplementerFromViewOrViewModel<T>(object view)
            where T : class
        {
            return view switch
                   {
                       T viewAsT => viewAsT,
                       VisualElement { BindingContext: T vmAsT } => vmAsT,
                       _ => null
                   };
        }

        public static bool IsNavigationTarget(object view, INavigationContext navigationContext)
        {
            if (view is IRegionAware viewAsRegionAware)
            {
                return viewAsRegionAware.IsNavigationTarget(navigationContext);
            }

            if (view is BindableObject { BindingContext: IRegionAware vmAsRegionAware } )
            {
                return vmAsRegionAware.IsNavigationTarget(navigationContext);
            }

            var uri = navigationContext.Uri;
            if (!uri.IsAbsoluteUri)
                uri = new Uri(new Uri("app://prism.regions"), uri);
            var path = uri.LocalPath.Substring(1);
            var viewType = view.GetType();

            return path == viewType.Name || path == viewType.FullName;
        }

        public static void OnNavigatedFrom(object view, INavigationContext navigationContext)
        {
            if ( !ViewAndViewModelAction<IRegionAware>(
                    view, x => x.OnNavigatedFrom( navigationContext ) ) )
            {
                ViewAndViewModelAction<INavigatedAware>(
                    view, x => x.OnNavigatedFrom( navigationContext.Parameters ) );
            }
        }

        public static void OnNavigatedTo(object view, INavigationContext navigationContext)
        {
            if ( !ViewAndViewModelAction<IRegionAware>(
                    view, x => x.OnNavigatedTo( navigationContext ) ) )
            {
                ViewAndViewModelAction<INavigatedAware>(
                    view, x => x.OnNavigatedTo( navigationContext.Parameters ) );
            }
        }
    }
}
