using System;

public static class WinApi
{
    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    internal static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);
}

