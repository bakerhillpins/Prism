﻿using Prism.Ioc;
using Xamarin.Forms;

namespace Prism.Regions
{
    /// <inheritdoc />
    public class RegionInitializer : IRegionInitializer
    {
#region Implementation of IRegionInitializer

        /// <inheritdoc />
        public void SetApplicationShell( Page shell )
        {
            Xaml.RegionManager.SetRegionManager( shell, ContainerLocator.Current.Resolve<IRegionManager>() );
            Xaml.RegionManager.UpdateRegions();
        }

#endregion
    }
}
