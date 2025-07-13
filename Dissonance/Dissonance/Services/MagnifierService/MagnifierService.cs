using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Dissonance.ViewModels;

namespace Dissonance.Services.MagnifierService
{
	public class MagnifierService : IMagnifierService
	{
		private const float Unzoomed = 1.0f;
		private float _lastNonOneZoom = 2.0f;
		private float _currentZoom = 1.0f;
		public event EventHandler ZoomChanged;

		[DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern bool MagInitialize();

		[DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern bool MagUninitialize();

		[DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern bool MagGetFullscreenTransform(out float magLevel, out int xOffset, out int yOffset);

		[DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern bool MagSetFullscreenTransform(float magLevel, int xOffset, int yOffset);

		private bool _initialized;

		public MagnifierService()
		{
			_initialized = MagInitialize();
			if (_initialized)
			{
				float mag;
				int x, y;
				if (MagGetFullscreenTransform(out mag, out x, out y))
				{
					_currentZoom = mag;
					if (mag != 1.0f)
						_lastNonOneZoom = mag;
				}
			}
		}

		public double GetCurrentZoom()
		{
			float mag;
			int x, y;
			if (MagGetFullscreenTransform(out mag, out x, out y))
			{
				_currentZoom = mag;
				if (mag != 1.0f)
					_lastNonOneZoom = mag;
			}
			return _currentZoom;
		}

		public void ToggleZoom()
		{
			float newZoom;
			if (_currentZoom == Unzoomed)
			{
				newZoom = _lastNonOneZoom;
			}
			else
			{
				newZoom = Unzoomed;
			}
			if (MagSetFullscreenTransform(newZoom, 0, 0))
			{
				_currentZoom = newZoom;
				if (newZoom != 1.0f)
					_lastNonOneZoom = newZoom;
				ZoomChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
