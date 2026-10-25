using Aimmy2;
using Aimmy2.AILogic;
using Aimmy2.Class;
using Aimmy2.Theme;
using Other;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Visuality
{
    public partial class PerformanceHelperWindow : Window
    {
        private const double CompactWidth = 460;
        private const double CompactHeight = 178;
        private const double ExpandedWidth = 700;
        private const double ExpandedHeight = 474;
        private const int ExpandDurationMilliseconds = 860;
        private const int AutoExpandDelayMilliseconds = 260;

        internal enum CompactPromptAction
        {
            StayCompact,
            ExpandToHelper,
            SaveSkipAndClose
        }

        internal enum LaunchMode
        {
            CompactPrompt,
            FullHelper
        }

        internal readonly record struct LaunchBehavior(
            bool StartCompact,
            bool ShowCompactPrompt,
            bool AutoExpand);

        internal readonly record struct ExpansionAnimationPlan(
            double WindowLeft,
            double WindowTop,
            double WindowWidth,
            double WindowHeight,
            double SurfaceStartWidth,
            double SurfaceStartHeight,
            double SurfaceEndWidth,
            double SurfaceEndHeight,
            bool AnimateWindowBounds);

        private readonly MainWindow _mainWindow;
        private readonly AIManager _aiManager;
        private CancellationTokenSource? _benchmarkCancellation;
        private PerformanceBenchmarkReport? _report;
        private PerformanceBenchmarkSizeResult? _selectedResult;
        private PerformanceGoal _selectedGoal = PerformanceGoal.Balanced;
        private bool _benchmarkRunning;
        private bool _closeAfterBenchmarkCancel;
        private readonly LaunchMode _launchMode;

        internal PerformanceHelperWindow(MainWindow mainWindow, AIManager aiManager, LaunchMode launchMode = LaunchMode.CompactPrompt)
        {
            InitializeComponent();

            _mainWindow = mainWindow;
            _aiManager = aiManager;
            _launchMode = launchMode;

            if (!ResolveLaunchBehavior(launchMode).StartCompact)
            {
                Width = ExpandedWidth;
                Height = ExpandedHeight;
            }

            ThemeManager.TrackWindow(this);
            UpdateThemeColors();
            ThemeManager.ThemeChanged += OnThemeChanged;
            UpdateGoalButtons();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var behavior = ResolveLaunchBehavior(_launchMode);
            if (behavior.StartCompact)
            {
                PrepareCompactNotice(behavior.ShowCompactPrompt);
            }
            else
            {
                PrepareFullHelper();
            }

            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            if (behavior.AutoExpand)
            {
                AutoExpandAfterLoad();
            }
        }

        internal static LaunchBehavior ResolveLaunchBehavior(LaunchMode launchMode)
        {
            return launchMode switch
            {
                LaunchMode.CompactPrompt => new LaunchBehavior(true, true, false),
                LaunchMode.FullHelper => new LaunchBehavior(true, false, true),
                _ => new LaunchBehavior(false, false, false)
            };
        }

        private void PrepareCompactNotice(bool showPrompt)
        {
            Opacity = 0;
            FullContent.Visibility = Visibility.Hidden;
            FullContent.IsHitTestVisible = false;
            FullContent.Opacity = 0;
            MiniPanel.Visibility = Visibility.Visible;
            MiniPanel.IsHitTestVisible = true;
            MiniPanel.Opacity = 1;
            MiniOpenButton.IsEnabled = true;
            MiniSkipButton.IsEnabled = true;
            MiniCloseButton.IsEnabled = true;
            MainBorder.BeginAnimation(FrameworkElement.WidthProperty, null);
            MainBorder.BeginAnimation(FrameworkElement.HeightProperty, null);
            MainBorder.HorizontalAlignment = HorizontalAlignment.Center;
            MainBorder.VerticalAlignment = VerticalAlignment.Center;
            MainBorder.Width = CompactWidth;
            MainBorder.Height = CompactHeight;

            if (showPrompt)
            {
                MiniPromptTitle.Text = "运行性能助手？";
                MiniPromptSubtitle.Text = "为这台电脑做一次性能测试，给出尺寸和帧率上限。";
                MiniFooterText.Text = "关闭后稍后会再次提醒。";
                MiniFooter.Visibility = Visibility.Visible;
                MiniOpenButton.Visibility = Visibility.Visible;
                MiniSkipButton.Visibility = Visibility.Visible;
                return;
            }

            MiniPromptTitle.Text = "性能助手";
            MiniPromptSubtitle.Text = "正在打开性能测试面板。";
            MiniFooterText.Text = string.Empty;
            MiniFooter.Visibility = Visibility.Visible;
            MiniOpenButton.Visibility = Visibility.Collapsed;
            MiniSkipButton.Visibility = Visibility.Collapsed;
            MiniOpenButton.IsEnabled = false;
            MiniSkipButton.IsEnabled = false;
        }

        private async void AutoExpandAfterLoad()
        {
            await Task.Delay(AutoExpandDelayMilliseconds);

            if (!IsVisible)
                return;

            MiniOpenButton.IsEnabled = false;
            MiniSkipButton.IsEnabled = false;
            MiniCloseButton.IsEnabled = false;
            BeginExpandAnimation();
        }

        private void PrepareFullHelper()
        {
            Opacity = 0;
            Width = ExpandedWidth;
            Height = ExpandedHeight;

            MiniPanel.BeginAnimation(OpacityProperty, null);
            MiniPanel.Visibility = Visibility.Collapsed;
            MiniPanel.IsHitTestVisible = false;
            MiniPanel.Opacity = 0;

            FullContent.BeginAnimation(OpacityProperty, null);
            FullContent.Visibility = Visibility.Visible;
            FullContent.IsHitTestVisible = true;
            FullContent.Opacity = 1;

            MainBorder.BeginAnimation(FrameworkElement.WidthProperty, null);
            MainBorder.BeginAnimation(FrameworkElement.HeightProperty, null);
            MainBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
            MainBorder.VerticalAlignment = VerticalAlignment.Stretch;
            MainBorder.Width = double.NaN;
            MainBorder.Height = double.NaN;
        }

        internal static CompactPromptAction ResolveCompactPromptAction(bool openRequested, bool skipRequested)
        {
            if (skipRequested)
                return CompactPromptAction.SaveSkipAndClose;

            return openRequested
                ? CompactPromptAction.ExpandToHelper
                : CompactPromptAction.StayCompact;
        }

        private void MiniOpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResolveCompactPromptAction(openRequested: true, skipRequested: false) != CompactPromptAction.ExpandToHelper)
                return;

            MiniOpenButton.IsEnabled = false;
            MiniSkipButton.IsEnabled = false;
            MiniCloseButton.IsEnabled = false;
            BeginExpandAnimation();
        }

        private void MiniSkipButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResolveCompactPromptAction(openRequested: false, skipRequested: true) == CompactPromptAction.SaveSkipAndClose)
            {
                PerformanceHelperState.SaveChoice("Skipped");
                Close();
            }
        }

        private void MiniCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BeginExpandAnimation()
        {
            var plan = CreateExpansionAnimationPlan(
                Left,
                Top,
                ActualWidth > 0 ? ActualWidth : Width,
                ActualHeight > 0 ? ActualHeight : Height,
                CompactWidth,
                CompactHeight,
                ExpandedWidth,
                ExpandedHeight,
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight);

            MainBorder.BeginAnimation(FrameworkElement.WidthProperty, null);
            MainBorder.BeginAnimation(FrameworkElement.HeightProperty, null);
            MainBorder.HorizontalAlignment = HorizontalAlignment.Center;
            MainBorder.VerticalAlignment = VerticalAlignment.Center;
            MainBorder.Width = plan.SurfaceStartWidth;
            MainBorder.Height = plan.SurfaceStartHeight;

            Width = plan.WindowWidth;
            Height = plan.WindowHeight;
            Left = plan.WindowLeft;
            Top = plan.WindowTop;
            UpdateLayout();

            FullContent.Visibility = Visibility.Visible;
            FullContent.IsHitTestVisible = false;
            FullContent.Opacity = 0;

            var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
            var duration = TimeSpan.FromMilliseconds(ExpandDurationMilliseconds);

            var heightAnimation = CreateAnimation(plan.SurfaceStartHeight, plan.SurfaceEndHeight, duration, ease);
            heightAnimation.Completed += (_, _) => CompleteExpandAnimation();
            MainBorder.BeginAnimation(FrameworkElement.WidthProperty, CreateAnimation(plan.SurfaceStartWidth, plan.SurfaceEndWidth, duration, ease));
            MainBorder.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);

            MiniPanel.BeginAnimation(OpacityProperty, CreateOpacityAnimation(1, 0, 220, 360));
            FullContent.BeginAnimation(OpacityProperty, CreateOpacityAnimation(0, 1, 320, 500));
        }

        internal static ExpansionAnimationPlan CreateExpansionAnimationPlan(
            double currentLeft,
            double currentTop,
            double currentWidth,
            double currentHeight,
            double compactWidth,
            double compactHeight,
            double expandedWidth,
            double expandedHeight,
            double? workAreaLeft = null,
            double? workAreaTop = null,
            double? workAreaWidth = null,
            double? workAreaHeight = null)
        {
            double centerX = currentLeft + currentWidth / 2.0;
            double centerY = currentTop + currentHeight / 2.0;
            double targetLeft = centerX - expandedWidth / 2.0;
            double targetTop = centerY - expandedHeight / 2.0;

            if (workAreaLeft.HasValue &&
                workAreaTop.HasValue &&
                workAreaWidth.HasValue &&
                workAreaHeight.HasValue)
            {
                double minLeft = workAreaLeft.Value;
                double minTop = workAreaTop.Value;
                double maxLeft = minLeft + workAreaWidth.Value - expandedWidth;
                double maxTop = minTop + workAreaHeight.Value - expandedHeight;

                targetLeft = maxLeft < minLeft
                    ? minLeft
                    : Math.Clamp(targetLeft, minLeft, maxLeft);
                targetTop = maxTop < minTop
                    ? minTop
                    : Math.Clamp(targetTop, minTop, maxTop);
            }

            return new ExpansionAnimationPlan(
                targetLeft,
                targetTop,
                expandedWidth,
                expandedHeight,
                compactWidth,
                compactHeight,
                expandedWidth,
                expandedHeight,
                false);
        }

        private static DoubleAnimation CreateAnimation(double from, double to, TimeSpan duration, IEasingFunction ease)
        {
            return new DoubleAnimation(from, to, duration)
            {
                EasingFunction = ease
            };
        }

        private static DoubleAnimation CreateOpacityAnimation(
            double from,
            double to,
            int durationMilliseconds,
            int beginMilliseconds)
        {
            return new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(durationMilliseconds))
            {
                BeginTime = TimeSpan.FromMilliseconds(beginMilliseconds),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
        }

        private void CompleteExpandAnimation()
        {
            MainBorder.BeginAnimation(FrameworkElement.WidthProperty, null);
            MainBorder.BeginAnimation(FrameworkElement.HeightProperty, null);
            MainBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
            MainBorder.VerticalAlignment = VerticalAlignment.Stretch;
            MainBorder.Width = double.NaN;
            MainBorder.Height = double.NaN;

            MiniPanel.BeginAnimation(OpacityProperty, null);
            MiniPanel.Opacity = 0;
            MiniPanel.Visibility = Visibility.Collapsed;

            FullContent.BeginAnimation(OpacityProperty, null);
            FullContent.Visibility = Visibility.Visible;
            FullContent.IsHitTestVisible = true;
            FullContent.Opacity = 1;
        }

        private void OnThemeChanged(object? sender, Color newColor)
        {
            Dispatcher.Invoke(UpdateThemeColors);
        }

        private void UpdateThemeColors()
        {
            ThemeGradientStop.Color = ThemeManager.ThemeColorDark;
            var themeBrush = new SolidColorBrush(ThemeManager.ThemeColor);
            MiniOpenButton.Background = themeBrush;
            RunTestButton.Background = themeBrush;
            ApplyButton.Background = new SolidColorBrush(ThemeManager.ThemeColor);
            BenchmarkProgressBar.Foreground = new SolidColorBrush(ThemeManager.ThemeColorLight);
            UpdateGoalButtons();
            RefreshResultRows();
        }

        private void GoalButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not string goalName ||
                !Enum.TryParse(goalName, out PerformanceGoal goal))
            {
                return;
            }

            _selectedGoal = goal;
            UpdateGoalButtons();
        }

        private void UpdateGoalButtons()
        {
            if (FastestGoalButton == null ||
                BalancedGoalButton == null ||
                LowestGoalButton == null)
            {
                return;
            }

            UpdateGoalButton(FastestGoalButton, _selectedGoal == PerformanceGoal.FastestDetections);
            UpdateGoalButton(BalancedGoalButton, _selectedGoal == PerformanceGoal.Balanced);
            UpdateGoalButton(LowestGoalButton, _selectedGoal == PerformanceGoal.LowestResources);
        }

        private static void UpdateGoalButton(Button button, bool selected)
        {
            Color color = selected ? ThemeManager.ThemeColor : Color.FromArgb(36, 255, 255, 255);
            Color border = selected ? ThemeManager.ThemeColorLight : Color.FromArgb(52, 255, 255, 255);
            button.Background = new SolidColorBrush(Color.FromArgb(selected ? (byte)150 : (byte)24, color.R, color.G, color.B));
            button.BorderBrush = new SolidColorBrush(border);
        }

        private async void RunTestButton_Click(object sender, RoutedEventArgs e)
        {
            _benchmarkRunning = true;
            _closeAfterBenchmarkCancel = false;
            _benchmarkCancellation = new CancellationTokenSource();
            IntroPanel.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            ApplyButton.Visibility = Visibility.Collapsed;
            RunTestButton.Visibility = Visibility.Visible;
            NoThanksButton.IsEnabled = false;
            RunTestButton.IsEnabled = false;
            RunTestButton.Content = "测试中";
            FooterText.Text = string.Empty;

            var progress = new Progress<PerformanceBenchmarkProgress>(UpdateProgress);

            try
            {
                _report = await Task.Run(
                    () => _aiManager.RunPerformanceBenchmarkAsync(progress, _benchmarkCancellation.Token, _selectedGoal),
                    _benchmarkCancellation.Token);

                ShowResults(_report);
                PerformanceHelperState.SaveChoice("Run");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _closeAfterBenchmarkCancel = false;
                ProgressTitle.Text = "性能测试已停止";
                ProgressSubtitle.Text = ex.Message;
                FooterText.Text = "可以关闭此窗口，或加载模型后重试。";
                RunTestButton.Content = "重试";
                RunTestButton.IsEnabled = true;
                NoThanksButton.IsEnabled = true;
            }
            finally
            {
                bool closeAfterCancel = _closeAfterBenchmarkCancel;
                _benchmarkRunning = false;
                _closeAfterBenchmarkCancel = false;
                _benchmarkCancellation?.Dispose();
                _benchmarkCancellation = null;

                if (closeAfterCancel && IsVisible)
                {
                    Close();
                }
            }
        }

        private void UpdateProgress(PerformanceBenchmarkProgress progress)
        {
            int remaining = Math.Max(0, progress.TotalSteps - progress.StepIndex);
            double percent = progress.TotalSteps == 0
                ? 0
                : ((progress.StepIndex - 1 + Math.Clamp(progress.StepProgress, 0, 1)) * 100.0) / progress.TotalSteps;

            if (progress.Status.Equals("Complete", StringComparison.OrdinalIgnoreCase))
            {
                percent = 100;
            }

            ProgressTitle.Text = $"第 {progress.StepIndex} 步，共 {progress.TotalSteps} 步";
            ProgressSubtitle.Text = $"{TranslateProgressStatus(progress.Status)}，剩余 {remaining} 个尺寸。";
            BenchmarkProgressBar.Value = percent;
        }

        // AIManager 上报的状态串是英文固定格式，只在显示层转换，不改上报方
        private static readonly Regex ProgressStatusPattern = new(
            @"^(?<phase>Preparing|Warming up|Testing) (?<size>\d+)px(?<model> model)?(?: \((?<seconds>\d+)s (?:minimum, (?<frames>\d+) frames|left)\))?$",
            RegexOptions.Compiled);

        private static string TranslateProgressStatus(string status)
        {
            Match match = ProgressStatusPattern.Match(status);
            if (!match.Success)
                return status.Equals("Complete", StringComparison.OrdinalIgnoreCase) ? "已完成" : status;

            string phase = match.Groups["phase"].Value switch
            {
                "Preparing" => "正在准备",
                "Warming up" => "正在预热",
                _ => "正在测试"
            };

            string text = $"{phase} {match.Groups["size"].Value}px";
            if (match.Groups["model"].Success)
                text += " 模型";
            if (!match.Groups["seconds"].Success)
                return text;

            return match.Groups["frames"].Success
                ? $"{text}（最少 {match.Groups["seconds"].Value} 秒，{match.Groups["frames"].Value} 帧）"
                : $"{text}（剩 {match.Groups["seconds"].Value} 秒）";
        }

        private void ShowResults(PerformanceBenchmarkReport report)
        {
            ProgressPanel.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Visible;
            NoThanksButton.Content = "关闭";
            NoThanksButton.IsEnabled = true;
            FooterText.Text = string.Empty;

            ResultsStack.Children.Clear();

            _selectedResult = report.Results
                .FirstOrDefault(result =>
                    result.ImageSize == report.Recommendation.SuggestedImageSize &&
                    result.FrameCount > 0 &&
                    result.AverageFps > 0) ??
                report.Results.FirstOrDefault(result => result.FrameCount > 0 && result.AverageFps > 0) ??
                report.Results.FirstOrDefault();

            foreach (var result in report.Results.OrderByDescending(result => result.ImageSize))
            {
                AddResultRow(result, result == _selectedResult);
            }

            UpdateSelectedResultSummary();
            UpdateCompletedRunButtons(report);

            FixedModelNote.Text = report.IsFixedSizeModel
                ? "固定尺寸模型：不更换模型就无法修改图像尺寸。"
                : string.Empty;
        }

        private void UpdateCompletedRunButtons(PerformanceBenchmarkReport report)
        {
            bool hasStableResult = report.Results.Any(result => result.FrameCount > 0 && result.AverageFps > 0);
            ApplyButton.Visibility = hasStableResult ? Visibility.Visible : Visibility.Collapsed;
            RunTestButton.Visibility = hasStableResult ? Visibility.Collapsed : Visibility.Visible;
            RunTestButton.Content = hasStableResult ? "开始测试" : "再测一次";
            RunTestButton.IsEnabled = !hasStableResult;
        }

        private void AddResultRow(PerformanceBenchmarkSizeResult result, bool selected)
        {
            bool usable = result.FrameCount > 0 && result.AverageFps > 0;
            var border = new Border
            {
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(selected ? ThemeManager.ThemeColorLight : Color.FromArgb(38, 255, 255, 255)),
                Background = new SolidColorBrush(selected ? Color.FromArgb(34, ThemeManager.ThemeColor.R, ThemeManager.ThemeColor.G, ThemeManager.ThemeColor.B) : Color.FromArgb(14, 255, 255, 255)),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = usable ? Cursors.Hand : Cursors.Arrow,
                Opacity = usable ? 1 : 0.58
            };

            if (usable)
            {
                border.PreviewMouseLeftButtonDown += (_, _) => SelectResult(result);
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(154) });

            AddCell(grid, $"{result.ImageSize}px", 0, true);
            AddPairCell(grid, result, 1);
            AddBadgeCell(grid, result, selected, 2);

            border.Child = grid;
            ResultsStack.Children.Add(border);
        }

        private void SelectResult(PerformanceBenchmarkSizeResult result)
        {
            if (_report == null || result.FrameCount <= 0 || result.AverageFps <= 0)
                return;

            _selectedResult = result;
            ResultsStack.Children.Clear();

            foreach (var item in _report.Results.OrderByDescending(item => item.ImageSize))
            {
                AddResultRow(item, item == _selectedResult);
            }

            UpdateSelectedResultSummary();
        }

        private void RefreshResultRows()
        {
            if (_report == null ||
                ResultsStack == null ||
                ResultsPanel.Visibility != Visibility.Visible)
            {
                return;
            }

            ResultsStack.Children.Clear();

            foreach (var item in _report.Results.OrderByDescending(item => item.ImageSize))
            {
                AddResultRow(item, item == _selectedResult);
            }
        }

        private void UpdateSelectedResultSummary()
        {
            if (_report == null || _selectedResult == null || _selectedResult.FrameCount <= 0 || _selectedResult.AverageFps <= 0)
            {
                MaxFpsValue.Text = "-";
                CpuValue.Text = "-";
                GpuValue.Text = "-";
                CapValue.Text = "-";
                ApplyButton.IsEnabled = false;
                ApplyButton.Visibility = Visibility.Collapsed;
                FooterText.Text = "可以关闭此窗口，或等模型稳定后重新测试。";
                RecommendationText.Text = "没有采集到稳定的 AI 循环样本。";
                WarningText.Text = _report?.Recommendations.HasLimitedHardwareWarning == true
                    ? $"硬件提示：{_report.Recommendations.HardwareWarning}"
                    : string.Empty;
                WarningText.Visibility = _report?.Recommendations.HasLimitedHardwareWarning == true
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                return;
            }

            MaxFpsValue.Text = $"{_selectedResult.AverageFps:F0}";
            CpuValue.Text = FormatPercent(_selectedResult.AverageCpuPercent);
            GpuValue.Text = _selectedResult.GpuAvailable ? FormatPercent(_selectedResult.AverageGpuPercent) : "N/A";
            int selectedCap = GetSuggestedCap(_selectedResult);
            CapValue.Text = $"{selectedCap} 帧";
            ApplyButton.IsEnabled = _selectedResult.FrameCount > 0 && _selectedResult.AverageFps > 0;
            ApplyButton.Visibility = Visibility.Visible;
            FooterText.Text = "选择极速、均衡或最低占用方案，然后应用这组设置。";

            string prefix = GetSelectionTitle(_selectedResult, _report);
            string summary = CreateSelectionRecommendation(_selectedResult, _report).Summary;
            RecommendationText.Text =
                $"{prefix}：{_selectedResult.ImageSize}px + 上限 {selectedCap} 帧。全速 AI 循环平均 {_selectedResult.AverageFps:F0} 帧。{summary}";

            WarningText.Text = _report.Recommendations.HasLimitedHardwareWarning
                ? $"硬件提示：{_report.Recommendations.HardwareWarning}"
                : string.Empty;
            WarningText.Visibility = _report.Recommendations.HasLimitedHardwareWarning
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void AddPairCell(Grid grid, PerformanceBenchmarkSizeResult result, int column)
        {
            int cap = GetSuggestedCap(result);
            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(column == 0 ? 0 : 18, 0, 0, 0)
            };

            stack.Children.Add(new TextBlock
            {
                Text = result.FrameCount > 0 ? $"全速：平均 {result.AverageFps:F0} 帧 → 上限 {cap} 帧" : "无稳定样本",
                Foreground = new SolidColorBrush(Colors.White),
                FontFamily = new FontFamily("Atkinson Hyperlegible"),
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            stack.Children.Add(new TextBlock
            {
                Text = result.FrameCount > 0
                    ? $"组合：{result.ImageSize}px + 上限 {cap} 帧   CPU {FormatPercent(result.AverageCpuPercent)}   GPU {(result.GpuAvailable ? FormatPercent(result.AverageGpuPercent) : "N/A")}"
                    : "等模型稳定后再试一次。",
                Foreground = new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)),
                FontFamily = new FontFamily("Atkinson Hyperlegible"),
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            Grid.SetColumn(stack, column);
            grid.Children.Add(stack);
        }

        private int GetSuggestedCap(PerformanceBenchmarkSizeResult result)
        {
            return _report == null
                ? result.SuggestedFpsLimit
                : CreateSelectionRecommendation(result, _report).SuggestedFpsLimit;
        }

        private readonly record struct ResultBadge(string Text, Color Color);

        private void AddBadgeCell(Grid grid, PerformanceBenchmarkSizeResult result, bool selected, int column)
        {
            var panel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0)
            };

            foreach (var badge in GetResultBadges(result, selected))
            {
                panel.Children.Add(CreateBadge(badge));
            }

            Grid.SetColumn(panel, column);
            grid.Children.Add(panel);
        }

        private IEnumerable<ResultBadge> GetResultBadges(PerformanceBenchmarkSizeResult result, bool selected)
        {
            var badges = new List<ResultBadge>();
            if (_report == null || result.FrameCount <= 0 || result.AverageFps <= 0)
                return badges;

            if (selected)
                badges.Add(new ResultBadge("已选", ThemeManager.ThemeColorLight));

            if (_report.IsFixedSizeModel && MatchesRecommendation(result, _report.Recommendation))
            {
                badges.Add(new ResultBadge("固定尺寸·全部方案", Color.FromRgb(196, 181, 253)));
                return badges;
            }

            if (AllRecommendationModesUseSameResult(_report) && MatchesRecommendation(result, _report.Recommendations.Primary))
            {
                badges.Add(new ResultBadge("全部方案", Color.FromRgb(255, 210, 122)));
                return badges;
            }

            var modeBadge = GetPrimaryModeBadge(result, _report);
            if (modeBadge != null)
            {
                badges.Add(modeBadge.Value);
            }

            if (MatchesRecommendation(result, _report.Recommendations.Primary))
                badges.Add(new ResultBadge("推荐", ThemeManager.ThemeColor));

            return badges;
        }

        private static ResultBadge? GetPrimaryModeBadge(
            PerformanceBenchmarkSizeResult result,
            PerformanceBenchmarkReport report)
        {
            if (MatchesRecommendation(result, report.Recommendations.Primary))
                return CreateModeBadge(report.Recommendations.Primary.Mode);

            if (MatchesRecommendation(result, report.Recommendations.FastestDetections))
                return CreateModeBadge(PerformanceRecommendationMode.FastestDetections);

            if (MatchesRecommendation(result, report.Recommendations.Balanced))
                return CreateModeBadge(PerformanceRecommendationMode.Balanced);

            if (MatchesRecommendation(result, report.Recommendations.LowestResources))
                return CreateModeBadge(PerformanceRecommendationMode.LowestResources);

            if (MatchesRecommendation(result, report.Recommendations.Quality))
                return CreateModeBadge(PerformanceRecommendationMode.Quality);

            if (MatchesRecommendation(result, report.Recommendations.HighSpeed))
                return CreateModeBadge(PerformanceRecommendationMode.HighSpeed);

            return null;
        }

        private static ResultBadge CreateModeBadge(PerformanceRecommendationMode mode)
        {
            return mode switch
            {
                PerformanceRecommendationMode.FastestDetections => new ResultBadge("极速", Color.FromRgb(124, 199, 255)),
                PerformanceRecommendationMode.Quality => new ResultBadge("画质", Color.FromRgb(124, 199, 255)),
                PerformanceRecommendationMode.LowestResources => new ResultBadge("最低占用", Color.FromRgb(94, 234, 212)),
                PerformanceRecommendationMode.HighSpeed => new ResultBadge("高帧", Color.FromRgb(94, 234, 212)),
                _ => new ResultBadge("均衡", Color.FromRgb(167, 139, 250))
            };
        }

        private static Border CreateBadge(ResultBadge badge)
        {
            return new Border
            {
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(145, badge.Color.R, badge.Color.G, badge.Color.B)),
                Background = new SolidColorBrush(Color.FromArgb(34, badge.Color.R, badge.Color.G, badge.Color.B)),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(4, 2, 0, 2),
                Child = new TextBlock
                {
                    Text = badge.Text,
                    Foreground = new SolidColorBrush(Colors.White),
                    FontFamily = new FontFamily("Atkinson Hyperlegible"),
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.NoWrap
                }
            };
        }

        private static void AddCell(Grid grid, string text, int column, bool strong)
        {
            var block = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(strong ? Colors.White : Color.FromArgb(190, 255, 255, 255)),
                FontFamily = new FontFamily("Atkinson Hyperlegible"),
                FontWeight = strong ? FontWeights.SemiBold : FontWeights.Normal,
                FontSize = strong ? 14 : 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(column == 0 ? 0 : 18, 0, 0, 0)
            };

            Grid.SetColumn(block, column);
            grid.Children.Add(block);
        }

        private static string GetSelectionTitle(PerformanceBenchmarkSizeResult result, PerformanceBenchmarkReport report)
        {
            if (AllRecommendationModesUseSameResult(report) && MatchesRecommendation(result, report.Recommendations.Primary))
                return "全部方案相同";

            var recommendation = GetMatchingRecommendation(result, report);
            if (recommendation != null)
                return $"{recommendation.Label}方案";

            return "当前组合";
        }

        private static PerformanceRecommendation? GetMatchingRecommendation(
            PerformanceBenchmarkSizeResult result,
            PerformanceBenchmarkReport report)
        {
            if (MatchesRecommendation(result, report.Recommendations.Primary))
                return report.Recommendations.Primary;

            if (MatchesRecommendation(result, report.Recommendations.FastestDetections))
                return report.Recommendations.FastestDetections;

            if (MatchesRecommendation(result, report.Recommendations.Balanced))
                return report.Recommendations.Balanced;

            if (MatchesRecommendation(result, report.Recommendations.LowestResources))
                return report.Recommendations.LowestResources;

            if (MatchesRecommendation(result, report.Recommendations.Quality))
                return report.Recommendations.Quality;

            if (MatchesRecommendation(result, report.Recommendations.HighSpeed))
                return report.Recommendations.HighSpeed;

            return null;
        }

        private static bool AllRecommendationModesUseSameResult(PerformanceBenchmarkReport report)
        {
            return SameRecommendationPair(report.Recommendations.FastestDetections, report.Recommendations.Balanced) &&
                   SameRecommendationPair(report.Recommendations.Balanced, report.Recommendations.LowestResources);
        }

        private static bool SameRecommendationPair(
            PerformanceRecommendation left,
            PerformanceRecommendation right)
        {
            return left.SuggestedImageSize == right.SuggestedImageSize &&
                   left.SuggestedFpsLimit == right.SuggestedFpsLimit;
        }

        private static bool MatchesRecommendation(
            PerformanceBenchmarkSizeResult result,
            PerformanceRecommendation recommendation)
        {
            return result.ImageSize == recommendation.SuggestedImageSize &&
                   PerformanceRecommendationBuilder.BuildFpsCap(result, GetGoalForMode(recommendation.Mode)) == recommendation.SuggestedFpsLimit;
        }

        private static PerformanceGoal GetGoalForMode(PerformanceRecommendationMode mode)
        {
            return mode switch
            {
                PerformanceRecommendationMode.FastestDetections => PerformanceGoal.FastestDetections,
                PerformanceRecommendationMode.LowestResources => PerformanceGoal.LowestResources,
                _ => PerformanceGoal.Balanced
            };
        }

        private static string FormatPercent(double value)
        {
            if (value > 0 && value < 1)
                return "<1%";

            if (value < 10)
                return $"{value:F1}%";

            return $"{value:F0}%";
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_report == null || _selectedResult == null)
                return;

            ApplyButton.IsEnabled = false;
            FooterText.Text = "正在应用所选尺寸和帧率上限…";

            bool applied = await _mainWindow.ApplyPerformanceRecommendationAsync(CreateSelectionRecommendation(_selectedResult, _report));
            if (!applied)
            {
                FooterText.Text = "应用所选组合失败，模型设置未改动。";
                ApplyButton.IsEnabled = true;
                return;
            }

            PerformanceHelperState.SaveChoice("Applied");
            Close();
        }

        private static PerformanceRecommendation CreateSelectionRecommendation(
            PerformanceBenchmarkSizeResult result,
            PerformanceBenchmarkReport report)
        {
            var knownRecommendation = GetMatchingRecommendation(result, report);
            if (knownRecommendation != null)
            {
                return knownRecommendation with
                {
                    CanChangeImageSize = !report.IsFixedSizeModel && result.ImageSize != AimSettings.ImageSize
                };
            }

            bool canChangeImageSize = !report.IsFixedSizeModel && result.ImageSize != AimSettings.ImageSize;
            string summary = report.IsFixedSizeModel
                ? "固定模型锁定了图像尺寸，帧率上限仍能降低发热。"
                : result.ImageSize < AimSettings.ImageSize
                    ? "降低图像尺寸可减少占用，但小而远的目标可能丢失细节。"
                    : result.ImageSize > AimSettings.ImageSize
                        ? "提高图像尺寸能改善细节，但占用更高。"
                        : "保持当前图像尺寸，靠帧率上限减少多余的 CPU/GPU 开销。";

            return new PerformanceRecommendation(
                result.ImageSize,
                PerformanceRecommendationBuilder.BuildFpsCap(result, report.Goal),
                canChangeImageSize,
                summary,
                "已选择",
                GetSelectedMode(report.Goal));
        }

        private static PerformanceRecommendationMode GetSelectedMode(PerformanceGoal goal)
        {
            return goal switch
            {
                PerformanceGoal.FastestDetections => PerformanceRecommendationMode.FastestDetections,
                PerformanceGoal.LowestResources => PerformanceRecommendationMode.LowestResources,
                _ => PerformanceRecommendationMode.Balanced
            };
        }

        private void NoThanksButton_Click(object sender, RoutedEventArgs e)
        {
            if (_report == null)
            {
                PerformanceHelperState.SaveChoice("Skipped");
            }

            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            RequestBenchmarkCancelAndClose();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed &&
                e.OriginalSource is DependencyObject source &&
                !IsInsideButton(source))
            {
                DragMove();
            }
        }

        private static bool IsInsideButton(DependencyObject source)
        {
            DependencyObject? current = source;
            while (current != null)
            {
                if (current is Button)
                    return true;

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        protected override void OnClosed(EventArgs e)
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            if (_benchmarkRunning)
            {
                _benchmarkCancellation?.Cancel();
            }

            base.OnClosed(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_benchmarkRunning)
            {
                e.Cancel = true;
                RequestBenchmarkCancelAndClose();
                return;
            }

            base.OnClosing(e);
        }

        private void RequestBenchmarkCancelAndClose()
        {
            if (!_benchmarkRunning)
            {
                Close();
                return;
            }

            if (_closeAfterBenchmarkCancel)
                return;

            _closeAfterBenchmarkCancel = true;
            CloseButton.IsEnabled = false;
            NoThanksButton.IsEnabled = false;
            RunTestButton.IsEnabled = false;
            ApplyButton.IsEnabled = false;
            ProgressTitle.Text = "正在停止性能测试";
            ProgressSubtitle.Text = "关闭前恢复已加载的模型。";
            FooterText.Text = string.Empty;
            _benchmarkCancellation?.Cancel();
        }
    }
}
