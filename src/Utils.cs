using System.Reflection;

namespace Lostwave.Tools
{
    public static class Utils
    {
        public static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
