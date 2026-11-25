using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;
using Newtonsoft.Json;

namespace SOLUM_UI.Services
{
    public class AppSettingsData
    {
        public double TextScale { get; set; } = 1.0;
        public bool HighContrast { get; set; } = false;
        public bool BolderText { get; set; } = false;
        public bool ReduceMotion { get; set; } = false;
        public bool SpaciousTouch { get; set; } = false;
        public bool SingleClickOpen { get; set; } = false;
        public bool StrongFocusOutlines { get; set; } = false;
        public bool AnonymizeSubsidy { get; set; } = true;
        public int AutoLockMinutes { get; set; } = 15;
    }

    public class AppSettingsService : System.ComponentModel.INotifyPropertyChanged
    {
        private static readonly Lazy<AppSettingsService> _lazy = new Lazy<AppSettingsService>(() => new AppSettingsService());
        public static AppSettingsService Instance => _lazy.Value;

        private readonly string _settingsPath;
        private AppSettingsData _data = new AppSettingsData();

        private ResourceDictionary _highContrastDict;
        private ResourceDictionary _spaciousDict;
        private ResourceDictionary _focusOutlinesDict;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public event EventHandler SettingsChanged;

        private AppSettingsService()
        {
            try
            {
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string folder = Path.Combine(localApp, "Solace");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                _settingsPath = Path.Combine(folder, "app_settings.json");
                Load();
            }
            catch
            {
                _settingsPath = string.Empty;
            }
        }

        public double TextScale
        {
            get => _data.TextScale;
            set
            {
                if (Math.Abs(_data.TextScale - value) > 0.001)
                {
                    _data.TextScale = value;
                    OnPropertyChanged(nameof(TextScale));
                    OnPropertyChanged(nameof(ScalePercentageText));
                    ApplySettings();
                    Save();
                }
            }
        }

        public string ScalePercentageText => $"{Math.Round(_data.TextScale * 100)}%";

        public bool HighContrast
        {
            get => _data.HighContrast;
            set
            {
                if (_data.HighContrast != value)
                {
                    _data.HighContrast = value;
                    OnPropertyChanged(nameof(HighContrast));
                    ApplySettings();
                    Save();
                }
            }
        }

        public bool BolderText
        {
            get => _data.BolderText;
            set
            {
                if (_data.BolderText != value)
                {
                    _data.BolderText = value;
                    OnPropertyChanged(nameof(BolderText));
                    ApplySettings();
                    Save();
                }
            }
        }

        public bool ReduceMotion
        {
            get => _data.ReduceMotion;
            set
            {
                if (_data.ReduceMotion != value)
                {
                    _data.ReduceMotion = value;
                    OnPropertyChanged(nameof(ReduceMotion));
                    ApplySettings();
                    Save();
                }
            }
        }

        public bool SpaciousTouch
        {
            get => _data.SpaciousTouch;
            set
            {
                if (_data.SpaciousTouch != value)
                {
                    _data.SpaciousTouch = value;
                    OnPropertyChanged(nameof(SpaciousTouch));
                    ApplySettings();
                    Save();
                }
            }
        }

        public bool SingleClickOpen
        {
            get => _data.SingleClickOpen;
            set
            {
                if (_data.SingleClickOpen != value)
                {
                    _data.SingleClickOpen = value;
                    OnPropertyChanged(nameof(SingleClickOpen));
                    ApplySettings();
                    Save();
                }
            }
        }

        public bool StrongFocusOutlines
        {
            get => _data.StrongFocusOutlines;
            set
            {
                if (_data.StrongFocusOutlines != value)
                {
                    _data.StrongFocusOutlines = value;
                    OnPropertyChanged(nameof(StrongFocusOutlines));
                    ApplySettings();
                    Save();
                }
            }
        }

        public bool AnonymizeSubsidy
        {
            get => _data.AnonymizeSubsidy;
            set
            {
                if (_data.AnonymizeSubsidy != value)
                {
                    _data.AnonymizeSubsidy = value;
                    OnPropertyChanged(nameof(AnonymizeSubsidy));
                    ApplySettings();
                    Save();
                }
            }
        }

        public int AutoLockMinutes
        {
            get => _data.AutoLockMinutes;
            set
            {
                if (_data.AutoLockMinutes != value)
                {
                    _data.AutoLockMinutes = value;
                    OnPropertyChanged(nameof(AutoLockMinutes));
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                    Save();
                }
            }
        }

        public void ResetToDefaults()
        {
            _data = new AppSettingsData();
            OnPropertyChanged(string.Empty);
            ApplySettings();
            Save();
        }

        public void Load()
        {
            try
            {
                if (!string.IsNullOrEmpty(_settingsPath) && File.Exists(_settingsPath))
                {
                    string json = File.ReadAllText(_settingsPath);
                    var loaded = JsonConvert.DeserializeObject<AppSettingsData>(json);
                    if (loaded != null)
                    {
                        _data = loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppSettingsService] Load failed: {ex.Message}");
            }
        }

        public void Save()
        {
            try
            {
                if (!string.IsNullOrEmpty(_settingsPath))
                {
                    string json = JsonConvert.SerializeObject(_data, Formatting.Indented);
                    File.WriteAllText(_settingsPath, json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppSettingsService] Save failed: {ex.Message}");
            }
        }

        public void ApplyToWindow(Window win)
        {
            if (win == null) return;
            try
            {
                // Bolder text cascading down the visual tree
                win.SetValue(TextElement.FontWeightProperty, _data.BolderText ? FontWeights.SemiBold : FontWeights.Normal);

                // Interface & Text Scale
                if (win is MainWindow mw)
                {
                    if (mw.ContentArea != null)
                    {
                        if (Math.Abs(_data.TextScale - 1.0) < 0.01)
                            mw.ContentArea.LayoutTransform = Transform.Identity;
                        else
                            mw.ContentArea.LayoutTransform = new ScaleTransform(_data.TextScale, _data.TextScale);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppSettingsService] ApplyToWindow error: {ex.Message}");
            }
        }

        public void ApplySettings(MainWindow window = null)
        {
            try
            {
                // 1. Dynamic Themes (High Contrast, Spacious, Strong Outlines)
                ApplyResourceDictionaries();

                // 2. Apply to MainWindow if provided
                if (window != null)
                {
                    ApplyToWindow(window);
                }

                // 3. Apply to all active windows
                if (Application.Current != null && Application.Current.Windows != null)
                {
                    foreach (Window win in Application.Current.Windows)
                    {
                        ApplyToWindow(win);
                    }
                }

                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppSettingsService] ApplySettings error: {ex.Message}");
            }
        }

        private void ApplyResourceDictionaries()
        {
            if (Application.Current == null) return;
            var merged = Application.Current.Resources.MergedDictionaries;

            // High Contrast
            if (_highContrastDict == null)
            {
                try
                {
                    _highContrastDict = new ResourceDictionary { Source = new Uri("pack://application:,,,/Themes/HighContrastTheme.xaml") };
                }
                catch { }
            }

            if (_highContrastDict != null)
            {
                if (_data.HighContrast && !merged.Contains(_highContrastDict))
                    merged.Add(_highContrastDict);
                else if (!_data.HighContrast && merged.Contains(_highContrastDict))
                    merged.Remove(_highContrastDict);
            }

            // Spacious Buttons & Table Rows
            if (_spaciousDict == null)
            {
                try
                {
                    _spaciousDict = new ResourceDictionary { Source = new Uri("pack://application:,,,/Themes/SpaciousTheme.xaml") };
                }
                catch { }
            }

            if (_spaciousDict != null)
            {
                if (_data.SpaciousTouch && !merged.Contains(_spaciousDict))
                    merged.Add(_spaciousDict);
                else if (!_data.SpaciousTouch && merged.Contains(_spaciousDict))
                    merged.Remove(_spaciousDict);
            }

            // Strong Hover & Focus Outlines
            if (_focusOutlinesDict == null)
            {
                try
                {
                    _focusOutlinesDict = new ResourceDictionary { Source = new Uri("pack://application:,,,/Themes/FocusOutlinesTheme.xaml") };
                }
                catch { }
            }

            if (_focusOutlinesDict != null)
            {
                if (_data.StrongFocusOutlines && !merged.Contains(_focusOutlinesDict))
                    merged.Add(_focusOutlinesDict);
                else if (!_data.StrongFocusOutlines && merged.Contains(_focusOutlinesDict))
                    merged.Remove(_focusOutlinesDict);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
