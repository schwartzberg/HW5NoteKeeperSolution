using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;

namespace HW5NoteKeeperSolution.Pages
{
    /// <summary>
    /// Base Razor Page model that injects the application version into every page's <c>ViewData</c>.
    /// </summary>
    public class BasePageModel : PageModel
    {
        /// <summary>
        /// Update the view data with the app version
        /// </summary>
        /// <param name="context"></param>
        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            base.OnPageHandlerExecuting(context);
            ViewData["AppVersion"] = GetAppVersion();
        }

        /// <summary>
        /// Get the app version from the assembly
        /// </summary>
        /// <returns></returns>
        public static string GetAppVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
            string simpleVersion = version.Split('+')[0];
            return simpleVersion;
        }
    }
}

