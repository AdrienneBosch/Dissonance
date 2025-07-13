using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Dissonance.Infrastructure.Constants;
using Microsoft.Extensions.Logging;

namespace Dissonance.Services.HotkeyService
{
	internal class HotkeyService : IHotkeyService, IDisposable
	{
		private readonly object _lock = new object();
		private readonly ILogger<HotkeyService> _logger;
		private readonly Dissonance.Services.MessageService.IMessageService _messageService;
		private HwndSource _source;
		private IntPtr _windowHandle;

		private readonly Dictionary<string, int> _hotkeyIds = new();
		private readonly Dictionary<int, Action> _hotkeyCallbacks = new();
		private int _nextHotkeyId = 1;

		public HotkeyService(ILogger<HotkeyService> logger, Dissonance.Services.MessageService.IMessageService messageService)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
		}

		[DllImport("user32.dll")]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll")]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		private uint ParseModifiers(string modifiers)
		{
			if (string.IsNullOrWhiteSpace(modifiers))
				throw new ArgumentException("Modifiers cannot be null or empty.", nameof(modifiers));

			uint mod = 0;
			var parts = modifiers.Split(new[] { '+', ',' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var part in parts)
			{
				switch (part.Trim().ToLower())
				{
					case "alt": mod |= Infrastructure.Constants.ModifierKeys.Alt; break;
					case "ctrl": mod |= Infrastructure.Constants.ModifierKeys.Control; break;
					case "shift": mod |= Infrastructure.Constants.ModifierKeys.Shift; break;
					case "win": mod |= Infrastructure.Constants.ModifierKeys.Win; break;
					default:
						throw new ArgumentException($"Unknown modifier: {part}", nameof(modifiers));
				}
			}

			if (mod == 0)
				throw new ArgumentException("Hotkey must include at least one modifier.");

			return mod;
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WindowsMessages.Hotkey)
			{
				int hotkeyId = wParam.ToInt32();
				if (_hotkeyCallbacks.TryGetValue(hotkeyId, out var callback))
				{
					_logger.LogInformation($"Hotkey pressed (id={hotkeyId}).");
					Application.Current.Dispatcher.BeginInvoke(callback);
				}
				handled = true;
			}
			return IntPtr.Zero;
		}

		public void Initialize(Window mainWindow)
		{
			if (mainWindow == null)
				throw new ArgumentNullException(nameof(mainWindow), "MainWindow cannot be null.");

			var helper = new WindowInteropHelper(mainWindow);
			_windowHandle = helper.Handle;

			if (_windowHandle == IntPtr.Zero)
				throw new InvalidOperationException("Failed to get a valid window handle.");

			_source = HwndSource.FromHwnd(_windowHandle);
			_source.AddHook(WndProc);
		}

		public void RegisterHotkey(string id, AppSettings.HotkeySettings hotkey, Action callback)
		{
			if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
			if (hotkey == null) throw new ArgumentNullException(nameof(hotkey));
			if (callback == null) throw new ArgumentNullException(nameof(callback));
			lock (_lock)
			{
				// Unregister previous hotkey for this id if exists
				if (_hotkeyIds.TryGetValue(id, out int oldHotkeyId))
				{
					UnregisterHotKey(_windowHandle, oldHotkeyId);
					_hotkeyCallbacks.Remove(oldHotkeyId);
				}

				try
				{
					uint mod = ParseModifiers(hotkey.Modifiers);
					uint vk = (uint)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), hotkey.Key, true));
					int hotkeyId = _nextHotkeyId++;
					if (RegisterHotKey(_windowHandle, hotkeyId, mod, vk))
					{
						_hotkeyIds[id] = hotkeyId;
						_hotkeyCallbacks[hotkeyId] = callback;
						_logger.LogDebug($"Hotkey registered: {hotkey.Modifiers} + {hotkey.Key} (id={id})");
					}
					else
					{
						_messageService.DissonanceMessageBoxShowWarning(MessageBoxTitles.HotkeyServiceWarning, $"Failed to register hotkey: {hotkey.Modifiers} + {hotkey.Key}. It might already be in use by another application.");
					}
				}
				catch (ArgumentException ex)
				{
					_messageService.DissonanceMessageBoxShowError(MessageBoxTitles.HotkeyServiceError, $"Failed to register hotkey: {hotkey.Modifiers} + {hotkey.Key}.", ex);
				}
				catch (Exception ex)
				{
					_messageService.DissonanceMessageBoxShowError(MessageBoxTitles.HotkeyServiceError, $"Failed to register hotkey: {hotkey.Modifiers} + {hotkey.Key}. An unexpected error occurred.", ex);
				}
			}
		}

		public void UnregisterHotkey(string id)
		{
			lock (_lock)
			{
				if (_hotkeyIds.TryGetValue(id, out int hotkeyId))
				{
					UnregisterHotKey(_windowHandle, hotkeyId);
					_hotkeyCallbacks.Remove(hotkeyId);
					_hotkeyIds.Remove(id);
					_logger.LogDebug($"Hotkey unregistered with Id: {hotkeyId} (id={id})");
				}
			}
		}

		public void Dispose()
		{
			_source?.RemoveHook(WndProc);
			lock (_lock)
			{
				foreach (var id in _hotkeyIds.Values)
				{
					UnregisterHotKey(_windowHandle, id);
				}
				_hotkeyIds.Clear();
				_hotkeyCallbacks.Clear();
			}
			_logger.LogInformation("HotkeyService disposed.");
		}
	}
}