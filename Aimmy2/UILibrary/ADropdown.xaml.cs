using Aimmy2.Class;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace UILibrary
{
    /// <summary>
    /// Interaction logic for ADropdown.xaml
    /// </summary>
    public partial class ADropdown : UserControl
    {
        private string main_dictionary_path { get; set; }

        /// <summary>控件的英文标识，作为配置字典的键，不随显示文案变化。</summary>
        public string TitleKey { get; }

        public ADropdown(string title, string dictionary_path, string? tooltip = null)
        {
            InitializeComponent();
            TitleKey = title;
            DropdownTitle.Content = UiText.T(title);
            main_dictionary_path = dictionary_path;

            if (!string.IsNullOrEmpty(tooltip))
            {
                var tt = new System.Windows.Controls.ToolTip { Content = tooltip };
                if (TryFindResource("Tooltip") is System.Windows.Style style)
                    tt.Style = style;
                ToolTip = tt;
            }
        }

        private void DropdownBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItemContent = UiValue.Of(DropdownBox.SelectedItem as ComboBoxItem);
            if (selectedItemContent != null)
            {
                Dictionary.dropdownState[main_dictionary_path] = selectedItemContent;
            }
        }
    }
}