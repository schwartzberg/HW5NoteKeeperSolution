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
        /// Updates <c>ViewData["AppVersion"]</c> with the assembly version before each page handler executes.
        /// </summary>
        /// <param name="context">The page handler executing context provided by the Razor Pages framework.</param>
        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            base.OnPageHandlerExecuting(context);
            ViewData["AppVersion"] = GetAppVersion();
        }

        /// <summary>
        /// Gets the application version from the executing assembly's informational version attribute.
        /// </summary>
        /// <returns>The semantic version string (without the build metadata suffix), or <c>"Unknown"</c> if unavailable.</returns>
        public static string GetAppVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
            string simpleVersion = version.Split('+')[0];
            return simpleVersion;
        }
    }
}

