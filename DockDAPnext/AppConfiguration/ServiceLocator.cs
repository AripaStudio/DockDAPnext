using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DockDAPnext.Service;
using DockDAPnext.Service.InterFaces;
using DockDAPnext.ViewModels;

namespace DockDAPnext.AppConfiguration
{
    public static class ServiceLocator
    {
        public static IServiceCollection ConfigurationServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IdubManagerService, APIdockAP>();
            serviceCollection.AddSingleton<BaseViewModel>(); 
            serviceCollection.AddSingleton<MainViewModel>();

            serviceCollection.AddSingleton<VMaboutMe>();
            serviceCollection.AddSingleton<VMbuildDubFile>();
            serviceCollection.AddSingleton<VMshowDubFile>();
            serviceCollection.AddSingleton<VMprojectDubManager>();

            return serviceCollection;

        }
    }
}
