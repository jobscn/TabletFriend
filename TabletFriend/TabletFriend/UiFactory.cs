using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TabletFriend.Actions;
using TabletFriend.Models;
using WpfAppBar;


using System.Runtime.InteropServices;
using System.IO;
using System.Text;

namespace TabletFriend
{
	public static class UiFactory
	{
		public static void CreateUi(LayoutModel layout, MainWindow window)
		{
			Debug.WriteLine("UI created!");
			ToggleManager.ClearButtons();
			var theme = AppState.CurrentTheme;

			window.MainCanvas.Children.Clear();

			var isDocked = AppState.Settings.DockingMode != DockingMode.None;

			if (!isDocked)
			{
				window.MainBorder.CornerRadius = new CornerRadius(theme.Rounding);
			}
			else
			{
				window.MainBorder.CornerRadius = new CornerRadius(0);
			}
			var sizes = layout.Buttons.GetSizes(AppState.Settings.DockingMode);
			var positions = Packer.Pack(sizes, layout.LayoutWidth);

			var size = Packer.GetSize(positions, sizes);


			var rotateLayout = false;
			var layoutVertical = size.Y > size.X;
			if (AppState.Settings.DockingMode != DockingMode.None)
			{
				var dockingVertical = AppState.Settings.DockingMode == DockingMode.Left
					|| AppState.Settings.DockingMode == DockingMode.Right;

				if (layoutVertical != dockingVertical)
				{
					rotateLayout = true;
				}
			}

			var titlebarHeight = TitlebarManager.GetTitlebarHeight(layout);

			var newWidth = window.Width;
			var newHeight = window.Height;


			if (rotateLayout)
			{
				newHeight = size.X * layout.CellSize + layout.Margin + titlebarHeight;
				newWidth = size.Y * layout.CellSize + layout.Margin;
			}
			else
			{
				newWidth = size.X * layout.CellSize + layout.Margin;
				newHeight = size.Y * layout.CellSize + layout.Margin + titlebarHeight;
			}


			var windowSizeChanged = newWidth != window.Width || newHeight != window.Height;

			var wasMinimized = TitlebarManager.Minimized;
			if (windowSizeChanged)
			{
				if (
					   AppState.Settings.DockingMode == DockingMode.Left
					|| AppState.Settings.DockingMode == DockingMode.Right
					|| AppState.Settings.DockingMode == DockingMode.None
				)
				{
					window.Width = newWidth;
				}
				if (
					   AppState.Settings.DockingMode == DockingMode.Top
					|| AppState.Settings.DockingMode == DockingMode.Bottom
					|| AppState.Settings.DockingMode == DockingMode.None
				)
				{
					if (!wasMinimized)
					{
						window.Height = newHeight;
					}
				}
			}

			var offset = Vector2.Zero;
			if (AppState.Settings.DockingMode != DockingMode.None)
			{
				if (AppState.Settings.DockingMode == DockingMode.Top || AppState.Settings.DockingMode == DockingMode.Bottom)
				{
					offset.X = (float)(SystemParameters.PrimaryScreenWidth - newWidth) / 2;
				}
				else
				{
					offset.Y = (float)(SystemParameters.PrimaryScreenHeight - newHeight) / 2;
				}
			}
			else
			{
				offset.Y = (float)titlebarHeight;
			}

			if (AppState.Settings.DockingMode != DockingMode.None)
			{
				window.MinOpacity = layout.MaxOpacity;
			}
			else
			{
				window.MinOpacity = layout.MinOpacity;
			}
			window.MaxOpacity = layout.MaxOpacity;
			window.BeginAnimation(UIElement.OpacityProperty, null);
			window.Opacity = layout.MaxOpacity;
			if (window.IsMouseOver)
			{
				window.BeginAnimation(UIElement.OpacityProperty, window.FadeIn);
			}
			else
			{
				window.BeginAnimation(UIElement.OpacityProperty, window.FadeOut);
			}

			Application.Current.Resources["PrimaryHueMidBrush"] = new SolidColorBrush(theme.PrimaryColor);
			Application.Current.Resources["PrimaryHueMidForegroundBrush"] = new SolidColorBrush(theme.SecondaryColor);
			Application.Current.Resources["MaterialDesignToolForeground"] = new SolidColorBrush(theme.SecondaryColor);

			Application.Current.Resources["MaterialDesignPaper"] = new SolidColorBrush(theme.BackgroundColor);
			Application.Current.Resources["MaterialDesignFont"] = new SolidColorBrush(theme.SecondaryColor);
			Application.Current.Resources["MaterialDesignBody"] = new SolidColorBrush(theme.SecondaryColor);

			window.MainBorder.Background = new SolidColorBrush(theme.BackgroundColor);

			var visibleButtons = new List<ButtonModel>();

			foreach (var button in layout.Buttons)
			{
				if (button.IsVisible(AppState.Settings.DockingMode))
				{
					visibleButtons.Add(button);
				}
			}

			for (var i = 0; i < positions.Length; i += 1)
			{
				var button = visibleButtons[i];


				if (button.Spacer)
				{
					continue;
				}
				var buttonPosition = positions[i];
				var buttonSize = sizes[i];

				if (rotateLayout)
				{
					var buffer = buttonPosition.X;
					buttonPosition.X = buttonPosition.Y;
					buttonPosition.Y = buffer;

					buffer = buttonSize.X;
					buttonSize.X = buttonSize.Y;
					buttonSize.Y = buffer;
				}

				CreateButton(layout, window, button, buttonPosition, buttonSize, offset);
			}


			TitlebarManager.CreateTitlebar(window, theme, layout, newHeight, wasMinimized);
		}

		private static void CreateButton(
			LayoutModel layout,
			MainWindow window,
			ButtonModel button,
			Vector2 position,
			Vector2 size,
			Vector2 offset
		)
		{
			var theme = AppState.CurrentTheme;

			ButtonBase uiButton;
			var isToggle = button.Action is ToggleAction;
			var isRepeat = button.Action is RepeatAction;

			if (isToggle)
			{
				uiButton = new ToggleButton();
			}
			else
			{
				if (isRepeat)
				{
					uiButton = new RepeatButton();
					Stylus.SetIsPressAndHoldEnabled(uiButton, false);
				}
				else
				{
					uiButton = new Button();
				}
			}

			// Stylus.SetIsPressAndHoldEnabled(uiButton, false);

			uiButton.Width = layout.CellSize * size.X - layout.Margin;

			uiButton.Height = layout.CellSize * size.Y - layout.Margin;

			var font = button.Font;
			if (font == null)
			{
				font = AppState.CurrentTheme.DefaultFont;
			}
			var fontSize = button.FontSize;
			if (fontSize == 0)
			{
				fontSize = AppState.CurrentTheme.DefaultFontSize;
			}
			var fontWeight = button.FontWeight;
			if (fontWeight == 0)
			{
				fontWeight = AppState.CurrentTheme.DefaultFontWeight;
			}

			var text = new TextBlock();
			text.Text = button.Text;
			if (fontSize > 0)
			{
				text.FontSize = fontSize;
			}
			if (font != null)
			{
				text.FontFamily = new FontFamily(font);
			}
			if (fontWeight > 0)
			{
				text.FontWeight = FontWeight.FromOpenTypeWeight(Math.Min(999, fontWeight));
			}

			uiButton.Content = text;

			if (button.Icon != null)
			{
				uiButton.Content = button.Icon;
				if (!string.IsNullOrEmpty(button.Text))
				{
					uiButton.ToolTip = new ToolTip()
					{
						Style = Application.Current.Resources["tool_tip"] as Style,
						Content = button.Text,
						HasDropShadow = true,
					};
				}
			}

			var style = button.Style;
			if (style == null)
			{
				style = theme.DefaultStyle;
			}

			if (isToggle)
			{
				uiButton.Style = Application.Current.Resources["toggle"] as Style;

				var key = ((ToggleAction)button.Action).Key;
				var toggle = (ToggleButton)uiButton;
				if (ToggleManager.IsHeld(key))
				{
					toggle.IsChecked = true;
				}
				ToggleManager.AddButton(key, toggle);
			}
			else
			{
				if (style == null)
				{
					uiButton.Style = null;
				}
				else
				{
					uiButton.Style = Application.Current.Resources[style] as Style;
				}
			}

			if (button.Action != null)
			{

				uiButton.PreviewMouseLeftButtonDown += (e, o) =>
				{
					PlayUiClickSound();
				};
				uiButton.Click += (e, o) =>
				{
					_ = button.Action.Invoke();
				};
			}

			Canvas.SetTop(uiButton, layout.CellSize * position.Y + layout.Margin + offset.Y);
			Canvas.SetLeft(uiButton, layout.CellSize * position.X + layout.Margin + offset.X);
			window.MainCanvas.Children.Add(uiButton);
		}


		// ---- Sound Playback Additions with Caching Logic ----
		[DllImport("winmm.dll")]
		private static extern long mciSendString(string lpstrCommand, StringBuilder lpstrReturnString, int uReturnLength, IntPtr hwndCallback);

		private static readonly string SoundAlias = "TF_UiClickSound"; // 固定别名
		private static bool _isSoundInitialized = false;
		private static string _soundFilePath = null;
		private static object _soundLock = new object(); // 用于线程安全初始化

		// Helper to log errors
		private static void LogSoundError(string message)
		{
			System.Diagnostics.Debug.WriteLine($"SOUND ERROR: {message}");
		}

		/// <summary>
		/// Initializes the sound system by opening the MP3 file with a persistent alias.
		/// This should be called before the first play.
		/// </summary>
		private static void InitializeSoundSystem()
		{
			// Double-check locking for thread safety if this can be called from multiple threads concurrently
			if (_isSoundInitialized) return;

			lock (_soundLock)
			{
				if (_isSoundInitialized) return; // Check again inside lock

				try
				{
					string basePath = AppDomain.CurrentDomain.BaseDirectory;
					_soundFilePath = Path.Combine(basePath, "files", "vfx", "ui-click.mp3");

					if (!File.Exists(_soundFilePath))
					{
						LogSoundError($"Sound file not found for initialization: {_soundFilePath}");
						return;
					}

					// Ensure any previous instance with the same alias is closed (defensive)
					mciSendString($"close {SoundAlias}", null, 0, IntPtr.Zero);

					string commandOpen = $"open \"{_soundFilePath}\" type mpegvideo alias {SoundAlias}";
					long err = mciSendString(commandOpen, null, 0, IntPtr.Zero);

					if (err != 0)
					{
						LogSoundError($"MCI Error opening file '{_soundFilePath}' for persistent alias '{SoundAlias}'. Code: {err}");
						_soundFilePath = null; // Mark as not successfully opened
						return;
					}
					_isSoundInitialized = true;
					LogSoundError($"Sound system initialized successfully for alias '{SoundAlias}'.");

					// Register a cleanup method for when the application domain unloads (e.g., app exit)
					AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
					AppDomain.CurrentDomain.DomainUnload += OnProcessExit; // Also for domain unload scenarios
				}
				catch (Exception ex)
				{
					LogSoundError($"Exception in InitializeSoundSystem: {ex.Message}");
					_isSoundInitialized = false;
					_soundFilePath = null;
				}
			}
		}

		/// <summary>
		/// Cleans up the sound system by closing the MCI alias.
		/// </summary>
		private static void CleanupSoundSystem()
		{
			lock (_soundLock)
			{
				if (_isSoundInitialized && !string.IsNullOrEmpty(_soundFilePath))
				{
					long err = mciSendString($"close {SoundAlias}", null, 0, IntPtr.Zero);
					if (err != 0)
					{
						LogSoundError($"MCI Error closing alias '{SoundAlias}'. Code: {err}");
					}
					else
					{
						LogSoundError($"Sound system cleaned up for alias '{SoundAlias}'.");
					}
					_isSoundInitialized = false;
					_soundFilePath = null;
				}
			}
		}

		private static void OnProcessExit(object sender, EventArgs e)
		{
			CleanupSoundSystem();
			// Unregister to prevent multiple calls if DomainUnload and ProcessExit both fire for the same shutdown.
			AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
			AppDomain.CurrentDomain.DomainUnload -= OnProcessExit;
		}


		/// <summary>
		/// Plays the predefined UI click sound asynchronously using the initialized MCI alias.
		/// </summary>
		private static void PlayUiClickSound()
		{
			if (!_isSoundInitialized)
			{
				InitializeSoundSystem();
				if (!_isSoundInitialized)
				{
					return;
				}
			}

			if (string.IsNullOrEmpty(_soundFilePath))
			{
				LogSoundError("Sound file path is missing or sound system not properly initialized. Cannot play sound.");
				return;
			}

			// Play the sound from the beginning using the persistent alias.
			// Removing "wait" makes the command return immediately, playing the sound in the background.
			string commandPlay = $"play {SoundAlias} from 0"; // REMOVED "wait"
			long err = mciSendString(commandPlay, null, 0, IntPtr.Zero);

			if (err != 0)
			{
				LogSoundError($"MCI Error playing alias '{SoundAlias}'. Code: {err}. Sound file: {_soundFilePath}");
				// Consider if re-initialization is needed for certain errors,
				// but be careful not to create an infinite loop if initialization itself is the problem.
				// Example: if (IsRecoverableError(err)) { CleanupSoundSystem(); /* And maybe re-initialize on next play attempt */ }
			}
		}
		// ---- End Sound Playback Additions ----
	}
}
