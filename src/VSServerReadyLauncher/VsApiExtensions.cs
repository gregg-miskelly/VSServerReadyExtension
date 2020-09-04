using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace VSServerReadyLauncher
{
    internal static class VSApiExtensions
    {
        public static T GetRequiredService<T>(this IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return GetRequiredService<T>(serviceProvider, typeof(T));
        }

        public static T GetRequiredService<T>(this IServiceProvider serviceProvider, Type serviceType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            object result = serviceProvider.GetService(serviceType);
            if (result == null)
            {
                throw new ServiceUnavailableException(serviceType);
            }

            return (T)result;
        }

        public static async Task<T> GetRequiredServiceAsync<T>(this IAsyncServiceProvider serviceProvider, Type serviceType)
        {
            object result = await serviceProvider.GetServiceAsync(serviceType);
            if (result == null)
            {
                throw new ServiceUnavailableException(serviceType);
            }

            return (T)result;
        }
    }

}
