using System.Collections.Generic;
using System.Windows.Controls;

namespace Aimmy2.Class
{
    /// <summary>
    /// 界面文案映射。键是控件内部的英文标识，同时被用作配置字典的键、
    /// 持久化值和 switch 匹配串，因此这里只映射显示文本，绝不改动传入的键。
    /// </summary>
    internal static class UiText
    {
        private static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            // 分区标题
            ["Aim Assist"] = "辅助瞄准",
            ["Aim Config"] = "瞄准设置",
            ["Predictions"] = "预测",
            ["Auto Trigger"] = "自动扳机",
            ["FOV Config"] = "视野设置",
            ["ESP Config"] = "透视显示",
            ["Model Settings"] = "模型设置",
            ["Settings Menu"] = "常规设置",
            ["Screen Settings"] = "屏幕设置",
            ["Theme Settings"] = "主题设置",

            // 开关
            ["Constant AI Tracking"] = "持续 AI 追踪",
            ["Sticky Aim"] = "粘性瞄准",
            ["Y Axis Percentage Adjustment"] = "Y 轴百分比调整",
            ["X Axis Percentage Adjustment"] = "X 轴百分比调整",
            ["EMA Smoothening"] = "EMA 平滑",
            ["Cursor Check"] = "准星检测",
            ["Spray Mode"] = "连发模式",
            ["Only When Held"] = "仅按住时",
            ["FOV"] = "视野圈",
            ["Dynamic FOV"] = "动态视野",
            ["Third Person Support"] = "第三人称支持",
            ["Show Detected Player"] = "显示检测目标",
            ["Show AI Confidence"] = "显示 AI 置信度",
            ["Show Tracers"] = "显示追踪线",
            ["Enable Model Switch Keybind"] = "启用模型切换快捷键",
            ["Collect Data While Playing"] = "游戏中采集数据",
            ["Auto Label Data"] = "自动标注数据",
            ["Mouse Background Effect"] = "鼠标背景特效",
            ["UI TopMost"] = "界面置顶",
            ["Debug Mode"] = "调试模式",
            ["StreamGuard"] = "直播保护",

            // 滑块
            ["Sticky Aim Threshold"] = "粘性瞄准阈值",
            ["Mouse Sensitivity (+/-)"] = "鼠标灵敏度（+/-）",
            ["Mouse Jitter"] = "鼠标抖动",
            ["Y Offset (Up/Down)"] = "Y 偏移（上/下）",
            ["Y Offset (%)"] = "Y 偏移（%）",
            ["X Offset (Left/Right)"] = "X 偏移（左/右）",
            ["X Offset (%)"] = "X 偏移（%）",
            ["Kalman Lead Time"] = "卡尔曼提前量",
            ["WiseTheFox Lead Time"] = "WiseTheFox 提前量",
            ["Shalloe Lead Multiplier"] = "Shalloe 提前倍数",
            ["Auto Trigger Delay"] = "自动扳机延迟",
            ["FOV Size"] = "视野大小",
            ["Dynamic FOV Size"] = "动态视野大小",
            ["AI Confidence Font Size"] = "置信度字号",
            ["Corner Radius"] = "圆角半径",
            ["Border Thickness"] = "边框粗细",
            ["Opacity"] = "不透明度",
            ["AI FPS Limit"] = "AI 帧率上限",
            ["AI Minimum Confidence"] = "AI 最低置信度",

            // 下拉标题
            ["Mouse Movement Method"] = "鼠标移动方式",
            ["Movement Path"] = "移动路径",
            ["Detection Area Type"] = "检测区域类型",
            ["Aiming Boundaries Alignment"] = "瞄准边界对齐",
            ["Prediction Method"] = "预测方式",
            ["FOV Style"] = "视野样式",
            ["Tracer Position"] = "追踪线位置",
            ["Image Size"] = "图像尺寸",
            ["Target Class"] = "目标类别",
            ["Screen Capture Method"] = "屏幕捕获方式",

            // 按键绑定
            ["Aim Keybind"] = "瞄准快捷键",
            ["Second Aim Keybind"] = "备用瞄准快捷键",
            ["Dynamic FOV Keybind"] = "动态视野快捷键",
            ["Model Switch Keybind"] = "模型切换快捷键",
            ["Emergency Stop Keybind"] = "紧急停止快捷键",

            // 颜色项
            ["FOV Color"] = "视野颜色",
            ["Detected Player Color"] = "检测目标颜色",

            // 按钮
            ["Run Performance Helper"] = "运行性能助手",
            ["Save Config"] = "保存配置",
            ["Refresh Displays"] = "刷新显示器",

            // 下拉选项
            ["Mouse Event"] = "鼠标事件",
            ["Razer Synapse (Require Razer Peripheral)"] = "Razer Synapse（需雷蛇外设）",
            ["ddxoft Virtual Input Driver"] = "ddxoft 虚拟输入驱动",
            ["Cubic Bezier"] = "三次贝塞尔",
            ["Exponential"] = "指数",
            ["Linear"] = "线性",
            ["Adaptive"] = "自适应",
            ["Perlin Noise"] = "柏林噪声",
            ["Closest to Center Screen"] = "最接近屏幕中心",
            ["Closest to Mouse"] = "最接近鼠标",
            ["Center"] = "居中",
            ["Top"] = "顶部",
            ["Bottom"] = "底部",
            ["Middle"] = "中间",
            ["Kalman Filter"] = "卡尔曼滤波",
            ["Shall0e's Prediction"] = "Shall0e 预测",
            ["wisethef0x's EMA Prediction"] = "wisethef0x 的 EMA 预测",
            ["Circle"] = "圆形",
            ["Rectangle"] = "矩形",
            ["Best Confidence"] = "最佳置信度",

            // 滑块单位后缀
            ["Pixels"] = "像素",
            ["Sensitivity"] = "灵敏度",
            ["Jitter"] = "抖动",
            ["Offset"] = "偏移",
            ["Percent"] = "百分比",
            ["Seconds"] = "秒",
            ["Frames"] = "帧",
            ["Amount"] = "数值",
            ["Size"] = "尺寸",
            ["Radius"] = "半径",
            ["Thickness"] = "粗细",
            ["% Confidence"] = "置信度（%）",

            // 其余界面文案
            ["Unlimited"] = "无限制",
        };

        /// <summary>把英文标识转换成中文显示文本，未收录的原样返回。</summary>
        public static string T(string? key)
        {
            if (string.IsNullOrEmpty(key)) return key ?? string.Empty;
            return Map.TryGetValue(key, out string? value) ? value : key;
        }
    }

    /// <summary>下拉项取值：优先取 Tag 里的英文标识，退回显示文本。</summary>
    internal static class UiValue
    {
        public static string? Of(ComboBoxItem? item) =>
            item?.Tag as string ?? item?.Content?.ToString();
    }
}
