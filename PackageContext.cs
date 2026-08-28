using System.Runtime.InteropServices;

namespace WinFlow;

internal static class PackageContext
{
    private const int ErrorInsufficientBuffer = 122;

    internal static bool IsPackaged
    {
        get
        {
            uint length = 0;
            return GetCurrentPackageFullName(ref length, null) == ErrorInsufficientBuffer;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, char[]? packageFullName);
}
