using Aimmy2.AILogic;
using Aimmy2.Class;
using Aimmy2.UILibrary;
using Other;
using System.Windows;
using System.Windows.Controls;
using UILibrary;
using Visuality;
using LogLevel = Other.LogManager.LogLevel;

namespace Aimmy2.Controls
{
    public partial class SettingsMenuControl : UserControl
    {
        private MainWindow? _mainWindow;
        private bool _isInitialized;
        private bool _suppressImageSizeSelectionChanged;

        // Local minimize state management
        private readonly Dictionary<string, bool> _localMinimizeState = new()
        {
            { "Model Settings", false },
            { "Settings Menu", false },
            { "Theme Settings", false },
            { "Screen Settings", false }
        };

        // Public properties for MainWindow access
        public StackPanel ModelSettingsPanel => ModelSettings;
        public StackPanel SettingsConfigPanel => SettingsConfig;
        public StackPanel ThemeMenuPanel => ThemeMenu;
        public StackPanel DisplaySelectMenuPanel => DisplaySelectMenu;
        public ScrollViewer SettingsMenuScrollViewer => SettingsMenu;

        public SettingsMenuControl()
        {
            InitializeComponent();
        }

        public void Initialize(MainWindow mainWindow)
        {
            if (_isInitialized) return;

            _mainWindow = mainWindow;
            _isInitialized = true;

            // Load minimize states from global dictionary if they exist
            LoadMinimizeStatesFromGlobal();

            LoadModelSettings();
            LoadSettingsConfig();
            LoadThemeMenu();
            LoadDisplaySelectMenu();

            // Apply minimize states after loading
            ApplyMinimizeStates();

            // Subscribe to display changes
            DisplayManager.DisplayChanged += OnDisplayChanged;

            // Subscribe to AI class updates for Target Class dropdown
            AIManager.ClassesUpdated += OnClassesChanged;

            // Subscribe to dynamic model status changes
            AIManager.DynamicModelStatusChanged += OnDynamicModelStatusChanged;

            // Set visibility based on current model status (handles case where model loaded before panel opened)
            UpdateDynamicModelDropdownsVisibility(AIManager.CurrentModelIsDynamic);
            UpdateTargetClassDropdown(_mainWindow!.uiManager.D_TargetClass!);
        }

        #region Minimize State Management

        private void LoadMinimizeStatesFromGlobal()
        {
            foreach (var key in _localMinimizeState.Keys.ToList())
            {
                if (Dictionary.minimizeState.ContainsKey(key))
                {
                    _localMinimizeState[key] = Dictionary.minimizeState[key];
                }
            }
        }

        private void SaveMinimizeStatesToGlobal()
        {
            foreach (var kvp in _localMinimizeState)
            {
                Dictionary.minimizeState[kvp.Key] = kvp.Value;
            }
        }

        private void ApplyMinimizeStates()
        {
            ApplyPanelState("Model Settings", ModelSettingsPanel);
            ApplyPanelState("Settings Menu", SettingsConfigPanel);
            ApplyPanelState("Theme Settings", ThemeMenuPanel);
            ApplyPanelState("Screen Settings", DisplaySelectMenuPanel);
        }

        private void ApplyPanelState(string stateName, StackPanel panel)
        {
            if (_localMinimizeState.TryGetValue(stateName, out bool isMinimized))
            {
                SetPanelVisibility(panel, !isMinimized);
            }
        }

        private void SetPanelVisibility(StackPanel panel, bool isVisible)
        {
            foreach (UIElement child in panel.Children)
            {
                // Keep titles, spacers, and bottom rectangles always visible
                bool shouldStayVisible = child is ATitle || child is ASpacer || child is ARectangleBottom;

                child.Visibility = shouldStayVisible
                    ? Visibility.Visible
                    : (isVisible ? Visibility.Visible : Visibility.Collapsed);
            }
        }

        private void TogglePanel(string stateName, StackPanel panel)
        {
            if (!_localMinimizeState.ContainsKey(stateName)) return;

            // Toggle the state
            _localMinimizeState[stateName] = !_localMinimizeState[stateName];

            // Apply the new visibility
            SetPanelVisibility(panel, !_localMinimizeState[stateName]);

            // Save to global dictionary
            SaveMinimizeStatesToGlobal();
        }

        #endregion

        #region Menu Section Loaders

        private void LoadModelSettings()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, ModelSettings);

            builder
                .AddTitle("Model Settings", true, t =>
                {
                    uiManager.AT_ModelSettings = t;
                    t.Minimize.Click += (s, e) => TogglePanel("Model Settings", ModelSettingsPanel);
                })
                .AddDropdown("Image Size", d =>
                {
                    uiManager.D_ImageSize = d;

                    // Add size options
                    _mainWindow.AddDropdownItem(d, "640");
                    _mainWindow.AddDropdownItem(d, "512");
                    _mainWindow.AddDropdownItem(d, "416");
                    _mainWindow.AddDropdownItem(d, "320");
                    _mainWindow.AddDropdownItem(d, "256");
                    _mainWindow.AddDropdownItem(d, "160");

                    // Set default to current value
                    var currentSize = Dictionary.dropdownState["Image Size"];
                    for (int i = 0; i < d.DropdownBox.Items.Count; i++)
                    {
                        if (UiValue.Of(d.DropdownBox.Items[i] as ComboBoxItem) == currentSize)
                        {
                            d.DropdownBox.SelectedIndex = i;
                            break;
                        }
                    }

                    // Handle selection change
                    d.DropdownBox.SelectionChanged += async (s, e) =>
                    {
                        if (_suppressImageSizeSelectionChanged)
                            return;

                        if (d.DropdownBox.SelectedItem == null || e.AddedItems.Count == 0)
                            return;

                        var newSize = UiValue.Of(d.DropdownBox.SelectedItem as ComboBoxItem);
                        if (string.IsNullOrEmpty(newSize))
                            return;

                        await _mainWindow!.ChangeImageSizeAsync(newSize);
                    };
                }, tooltip: "AI 检测使用的分辨率，越小越快但精度越低。")
                .AddSlider("AI FPS Limit", "FPS", 5, 5, 0, 240, s =>
                {
                    uiManager.S_AIFpsLimit = s;
                    s.SetValueFormatter(value => value <= 0 ? "无限制" : $"{value:F0} FPS");
                }, tooltip: "限制 AI 循环以降低 CPU/GPU 负载，0 表示全速运行。")
                .AddButton("Run Performance Helper", b =>
                {
                    b.Reader.Click += (s, e) => _mainWindow!.ShowPerformanceHelper();
                }, tooltip: "为当前加载的模型重新打开性能助手。")
                .AddDropdown("Target Class", d =>
                {
                    d.DropdownBox.SelectedIndex = 0;
                    uiManager.D_TargetClass = d;
                    _mainWindow.AddDropdownItem(d, "Best Confidence");
                    UpdateTargetClassDropdown(d);
                }, tooltip: "瞄准哪一类目标，最佳置信度取最确定的一个检测。")
                .AddSlider("AI Minimum Confidence", "% Confidence", 1, 1, 1, 100, s =>
                {
                    uiManager.S_AIMinimumConfidence = s;
                    s.Slider.PreviewMouseLeftButtonUp += (sender, e) =>
                    {
                        var value = s.Slider.Value;
                        if (value >= 95)
                            LogManager.Log(LogLevel.Warning, "设置的 AI 最低置信度过高，可能无法检测到玩家。", true);
                        else if (value <= 35)
                            LogManager.Log(LogLevel.Warning, "设置的 AI 最低置信度过低，容易出现误检。", true);
                    };
                }, tooltip: "AI 判定目标的把握程度，越高误检越少但可能漏掉目标。")
                .AddToggle("Enable Model Switch Keybind", t => uiManager.T_EnableModelSwitchKeybind = t,
                    tooltip: "允许用快捷键在 AI 模型之间切换。")
                .AddKeyChanger("Model Switch Keybind", k => uiManager.C_ModelSwitchKeybind = k,
                    tooltip: "按下此键循环切换可用的 AI 模型。")
                .AddKeyChanger("Emergency Stop Keybind", k => uiManager.C_EmergencyKeybind = k,
                    tooltip: "按下此键立即停止所有辅助瞄准功能。")
                .AddSeparator();
        }

        private void LoadSettingsConfig()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, SettingsConfig);

            builder
                .AddTitle("Settings Menu", true, t =>
                {
                    uiManager.AT_SettingsMenu = t;
                    t.Minimize.Click += (s, e) => TogglePanel("Settings Menu", SettingsConfigPanel);
                })
                .AddToggle("Collect Data While Playing", t => uiManager.T_CollectDataWhilePlaying = t,
                    tooltip: "保存检测截图，用于训练新的 AI 模型。")
                .AddToggle("Auto Label Data", t => uiManager.T_AutoLabelData = t,
                    tooltip: "自动为采集的截图标注检测数据。")
                .AddToggle("Mouse Background Effect", t => uiManager.T_MouseBackgroundEffect = t,
                    tooltip: "移动鼠标时界面显示特效。")
                .AddToggle("UI TopMost", t => uiManager.T_UITopMost = t,
                    tooltip: "让本窗口始终显示在其他窗口之上。")
                .AddToggle("Debug Mode", t => uiManager.T_DebugMode = t,
                    tooltip: "显示排查问题用的额外信息。")
                .AddButton("Save Config", b =>
                {
                    uiManager.B_SaveConfig = b;
                    b.Reader.Click += (s, e) => new ConfigSaver().ShowDialog();
                }, tooltip: "把当前设置保存成文件，之后可再加载。")
                .AddSeparator();
        }

        private void LoadDisplaySelectMenu()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, DisplaySelectMenu);

            builder
                .AddTitle("Screen Settings", true, t =>
                {
                    uiManager.AT_DisplaySelector = t;
                    t.Minimize.Click += (s, e) =>
                        TogglePanel("Screen Settings", DisplaySelectMenuPanel);
                })
                .AddDropdown("Screen Capture Method", d =>
                {
                    d.DropdownBox.SelectedIndex = -1;  // Prevent auto-selection that overwrites saved state
                    uiManager.D_ScreenCaptureMethod = d;
                    _mainWindow.AddDropdownItem(d, "DirectX");
                    _mainWindow.AddDropdownItem(d, "GDI+");
                }, tooltip: "屏幕捕获方式，DirectX 更快，GDI+ 兼容性更好。")
                .AddToggle("StreamGuard", t => uiManager.T_StreamGuard = t,
                    tooltip: "在录屏和直播中隐藏覆盖层，会显示在系统托盘。")
                .AddSeparator();

            // Handle DisplaySelector separately as it's a custom control
            uiManager.DisplaySelector = new ADisplaySelector();
            uiManager.DisplaySelector.RefreshDisplays();

            // Insert after title but before separator
            var insertIndex = DisplaySelectMenu.Children.Count - 2;
            DisplaySelectMenu.Children.Insert(insertIndex, uiManager.DisplaySelector);

            // Add refresh button after DisplaySelector
            var refreshButton = new APButton("Refresh Displays", "更新可用的显示器列表。");
            refreshButton.Reader.Click += (s, e) =>
            {
                try
                {
                    DisplayManager.RefreshDisplays();
                    uiManager.DisplaySelector.RefreshDisplays();
                    LogManager.Log(LogLevel.Info, "显示器列表已刷新。", true);
                }
                catch (Exception ex)
                {
                    LogManager.Log(LogLevel.Error, $"刷新显示器失败：{ex.Message}", true);
                }
            };
            DisplaySelectMenu.Children.Insert(insertIndex + 1, refreshButton);
        }

        private void LoadThemeMenu()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, ThemeMenu);

            builder
                .AddTitle("Theme Settings", true, t =>
                {
                    uiManager.AT_ThemeColorWheel = t;
                    t.Minimize.Click += (s, e) =>
                        TogglePanel("Theme Settings", ThemeMenuPanel);
                })
                .AddSeparator();

            // Handle ColorWheel separately as it's a custom control
            uiManager.ThemeColorWheel = new AColorWheel();

            //--
            var arrowButton = uiManager.ThemeColorWheel.FindName("ArrowButton") as Button;
            arrowButton.Visibility = Visibility.Visible;
            //--

            // Insert before separator
            var insertIndex = ThemeMenu.Children.Count - 2;
            ThemeMenu.Children.Insert(insertIndex, uiManager.ThemeColorWheel);
        }

        #endregion

        #region Helper Methods

        private void OnDisplayChanged(object? sender, DisplayChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    LogManager.Log(LogLevel.Info, $"AI 焦点已切换到显示器 {e.DisplayIndex + 1}（{e.Bounds.Width}x{e.Bounds.Height}）", true);
                    UpdateDisplayRelatedSettings(e);
                }
                catch (Exception ex)
                {
                }
            });
        }

        private void UpdateDisplayRelatedSettings(DisplayChangedEventArgs e)
        {
            Dictionary.sliderSettings["SelectedDisplay"] = e.DisplayIndex;
        }

        private async Task ResetToMouseEvent()
        {
            await Task.Delay(500);
            _mainWindow!.uiManager.D_MouseMovementMethod!.DropdownBox.SelectedIndex = 0;
        }

        public void UpdateImageSizeDropdown(string newSize, bool suppressSelectionChanged = true)
        {
            if (_mainWindow?.uiManager.D_ImageSize != null)
            {
                var dropdown = _mainWindow.uiManager.D_ImageSize;
                _suppressImageSizeSelectionChanged = suppressSelectionChanged;
                try
                {
                    for (int i = 0; i < dropdown.DropdownBox.Items.Count; i++)
                    {
                        if (UiValue.Of(dropdown.DropdownBox.Items[i] as ComboBoxItem) == newSize)
                        {
                            dropdown.DropdownBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
                finally
                {
                    _suppressImageSizeSelectionChanged = false;
                }
            }
        }

        private void OnClassesChanged(Dictionary<int, string> classes)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_mainWindow?.uiManager.D_TargetClass != null)
                {
                    UpdateTargetClassDropdown(_mainWindow.uiManager.D_TargetClass, classes);
                }
            });
        }

        private void OnDynamicModelStatusChanged(bool isDynamic)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateDynamicModelDropdownsVisibility(isDynamic);
            });
        }

        private void UpdateDynamicModelDropdownsVisibility(bool isDynamic)
        {
            // Only Image Size depends on dynamic model - it's hidden for static models
            // Target Class is always visible since both static and dynamic models can have multiple classes
            var imageSizeVisibility = isDynamic ? Visibility.Visible : Visibility.Collapsed;

            if (_mainWindow?.uiManager.D_ImageSize != null)
            {
                _mainWindow.uiManager.D_ImageSize.Visibility = imageSizeVisibility;
            }
        }

        private void UpdateTargetClassDropdown(ADropdown dropdown, Dictionary<int, string>? _classes = null)
        {
            if (dropdown?.DropdownBox == null) return;
            var visibility = _classes != null && _classes.Count > 1
                ? Visibility.Visible
                : Visibility.Collapsed;
            dropdown.Visibility = visibility;
            _mainWindow!.uiManager.D_TargetClass!.Visibility = visibility;

            string? selection = UiValue.Of(dropdown.DropdownBox.SelectedItem as ComboBoxItem);

            var removedItems = dropdown.DropdownBox.Items.Cast<ComboBoxItem>()
                .Where(item => UiValue.Of(item) != "Best Confidence")
                .ToList();

            foreach (var item in removedItems)
            {
                dropdown.DropdownBox.Items.Remove(item);
            }

            var classes = _classes ?? FileManager.AIManager?.ModelClasses ?? new Dictionary<int, string>();

            foreach (var kvp in classes.OrderBy(x => x.Key))
            {
                _mainWindow!.AddDropdownItem(dropdown, kvp.Value);
            }

            if (!string.IsNullOrEmpty(selection)) // tries to restore the selection
            {
                for (int i = 0; i < dropdown.DropdownBox.Items.Count; i++)
                {
                    if (UiValue.Of(dropdown.DropdownBox.Items[i] as ComboBoxItem) == selection)
                    {
                        dropdown.DropdownBox.SelectedIndex = i;
                        return;
                    }
                }
            }

            dropdown.DropdownBox.SelectedIndex = 0;
        }

        public void Dispose()
        {
            DisplayManager.DisplayChanged -= OnDisplayChanged;
            AIManager.ClassesUpdated -= OnClassesChanged;
            _mainWindow?.uiManager.DisplaySelector?.Dispose();

            // Save minimize states before disposing
            SaveMinimizeStatesToGlobal();
        }

        #endregion

        #region Control Creation Methods

        private AToggle CreateToggle(string title, string? tooltip = null)
        {
            var toggle = new AToggle(title, tooltip);
            _mainWindow!.toggleInstances[title] = toggle;

            // Set initial state
            if (Dictionary.toggleState[title])
                toggle.EnableSwitch();
            else
                toggle.DisableSwitch();

            // Handle click
            toggle.Reader.Click += (sender, e) =>
            {
                Dictionary.toggleState[title] = !Dictionary.toggleState[title];
                _mainWindow.UpdateToggleUI(toggle, Dictionary.toggleState[title]);
                _mainWindow.Toggle_Action(title);
            };

            return toggle;
        }

        //copied & Pasted from other class
        private AKeyChanger CreateKeyChanger(string title, string keybind, string? tooltip = null)
        {
            var keyChanger = new AKeyChanger(title, keybind, tooltip);

            keyChanger.Reader.Click += (sender, e) =>
            {
                keyChanger.KeyNotifier.Content = "...";
                _mainWindow!.bindingManager.StartListeningForBinding(title);

                Action<string, string>? bindingSetHandler = null;
                bindingSetHandler = (bindingId, key) =>
                {
                    if (bindingId == title)
                    {
                        keyChanger.KeyNotifier.Content = KeybindNameManager.ConvertToRegularKey(key);
                        Dictionary.bindingSettings[bindingId] = key;
                        _mainWindow.bindingManager.OnBindingSet -= bindingSetHandler;
                    }
                };

                _mainWindow.bindingManager.OnBindingSet += bindingSetHandler;
            };

            return keyChanger;
        }

        private ASlider CreateSlider(string title, string label, double frequency, double buttonSteps,
            double min, double max, string? tooltip = null)
        {
            var slider = new ASlider(title, label, buttonSteps, tooltip)
            {
                Slider = { Minimum = min, Maximum = max, TickFrequency = frequency }
            };

            slider.Slider.Value = Dictionary.sliderSettings.TryGetValue(title, out var value) ? value : min;
            slider.Slider.ValueChanged += (s, e) => Dictionary.sliderSettings[title] = slider.Slider.Value;

            return slider;
        }

        private ADropdown CreateDropdown(string title, string? tooltip = null) => new(title, title, tooltip);

        #endregion

        #region Section Builder

        private class SectionBuilder
        {
            private readonly SettingsMenuControl _parent;
            private readonly StackPanel _panel;

            public SectionBuilder(SettingsMenuControl parent, StackPanel panel)
            {
                _parent = parent;
                _panel = panel;
            }

            public SectionBuilder AddTitle(string title, bool canMinimize, Action<ATitle>? configure = null)
            {
                var titleControl = new ATitle(title, canMinimize);
                configure?.Invoke(titleControl);
                _panel.Children.Add(titleControl);
                return this;
            }

            public SectionBuilder AddToggle(string title, Action<AToggle>? configure = null, string? tooltip = null)
            {
                var toggle = _parent.CreateToggle(title, tooltip);
                configure?.Invoke(toggle);
                _panel.Children.Add(toggle);
                return this;
            }

            public SectionBuilder AddKeyChanger(string title, Action<AKeyChanger>? configure = null, string? defaultKey = null, string? tooltip = null)
            {
                var key = defaultKey ?? Dictionary.bindingSettings[title];
                var keyChanger = _parent.CreateKeyChanger(title, key, tooltip);
                configure?.Invoke(keyChanger);
                _panel.Children.Add(keyChanger);
                return this;
            }

            public SectionBuilder AddSlider(string title, string label, double frequency, double buttonSteps,
                double min, double max, Action<ASlider>? configure = null, string? tooltip = null)
            {
                var slider = _parent.CreateSlider(title, label, frequency, buttonSteps, min, max, tooltip);
                configure?.Invoke(slider);
                _panel.Children.Add(slider);
                return this;
            }

            public SectionBuilder AddDropdown(string title, Action<ADropdown>? configure = null, string? tooltip = null)
            {
                var dropdown = _parent.CreateDropdown(title, tooltip);
                configure?.Invoke(dropdown);
                _panel.Children.Add(dropdown);
                return this;
            }

            public SectionBuilder AddButton(string title, Action<APButton>? configure = null, string? tooltip = null)
            {
                string icon = title == "Run Performance Helper" ? "\uE768" : "\uE8B0";
                var button = new APButton(title, tooltip, icon);
                configure?.Invoke(button);
                _panel.Children.Add(button);
                return this;
            }

            public SectionBuilder AddSeparator()
            {
                _panel.Children.Add(new ARectangleBottom());
                _panel.Children.Add(new ASpacer());
                return this;
            }
        }

        #endregion
    }
}
