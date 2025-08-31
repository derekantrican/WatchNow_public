using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace WatchNow.Helpers
{
	public class WinApi
	{
		#region Windows 11 Rounded Corners
		// The enum flag for DwmSetWindowAttribute's second parameter, which tells the function what attribute to set.
		// Copied from dwmapi.h
		public enum DWMWINDOWATTRIBUTE
		{
			DWMWA_WINDOW_CORNER_PREFERENCE = 33
		}
		// The DWM_WINDOW_CORNER_PREFERENCE enum for DwmSetWindowAttribute's third parameter, which tells the function
		// what value of the enum to set.
		// Copied from dwmapi.h
		public enum DWM_WINDOW_CORNER_PREFERENCE
		{
			DWMWCP_DEFAULT = 0,
			DWMWCP_DONOTROUND = 1,
			DWMWCP_ROUND = 2,
			DWMWCP_ROUNDSMALL = 3
		}
		// Import dwmapi.dll and define DwmSetWindowAttribute in C# corresponding to the native function.
		[DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
		internal static extern void DwmSetWindowAttribute(IntPtr hwnd,
														 DWMWINDOWATTRIBUTE attribute,
														 ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
														 uint cbAttribute);

		//Remove Win11 rounded windows corners https://stackoverflow.com/a/72499984/2246411
		public static void RemoveRoundedCorners(nint handle)
		{
			var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
			var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND; // DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND
			DwmSetWindowAttribute(handle, attribute, ref preference, sizeof(uint));
		}
		#endregion Windows 11 Rounded Corners

		#region Enum Display Monitors
		[DllImport("user32.dll")]
		private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

		private delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

		[DllImport("User32.dll", CharSet = CharSet.Auto)]
		static extern bool GetMonitorInfo(IntPtr hMonitor, [In, Out] MONITORINFOEX lpmi);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
		private class MONITORINFOEX
		{
			public int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
			public RECT rcMonitor = new RECT();
			public RECT rcWork = new RECT();
			public int dwFlags = 0;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public char[] szDevice = new char[32];
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public int width  => right - left;
			public int height  => bottom - top; //Windows coordinates are 0 are the top
		}

		public class DisplayInfo
		{
			public bool IsPrimary { get; set; }
			public string ScreenHeight { get; set; }
			public string ScreenWidth { get; set; }
			public RECT MonitorArea { get; set; }
			public RECT WorkingArea { get; set; }
			public string DisplayName { get; set; }
		}

		// This was a lot of work for not a lot of result, but I wanted something with the same
		// info as System.Windows.Forms.Screen without a reference to System.Windows.Forms .
		// This still gives a mediocre DisplayName for each monitor, but at least it's
		// *something* (Avalonia's built-in Avalonia.Platform.Screen doesn't have anything
		// https://github.com/AvaloniaUI/Avalonia/issues/11512)
		public static List<DisplayInfo> GetDisplayDevices()
		{
			List<DisplayInfo> displays = new List<DisplayInfo>();

			EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
			{
				MONITORINFOEX mi = new MONITORINFOEX();
				bool success = GetMonitorInfo(hMonitor, mi);
				if (success)
				{
					DisplayInfo di = new DisplayInfo();
					di.ScreenWidth = (mi.rcMonitor.right - mi.rcMonitor.left).ToString();
					di.ScreenHeight = (mi.rcMonitor.bottom - mi.rcMonitor.top).ToString();
					di.MonitorArea = mi.rcMonitor;
					di.WorkingArea = mi.rcWork;
					di.DisplayName = new string(mi.szDevice.Where(c => c != char.MinValue).ToArray());
					di.IsPrimary = mi.dwFlags == 1;
					displays.Add(di);
				}

				return true;
			}, IntPtr.Zero);

			return displays;
		}
		#endregion Enum Display Monitors
	}
}
