using System.Windows;
using System.Windows.Controls;

namespace ShiftScaleAddin {
    /// <summary>
    /// Interaction logic for AttributeControlView.xaml. We can define by event handlers etc. to respond to user inputs from the view.
    /// Note that UserControl class is a class provided by WPF.
    /// </summary>
    public partial class AttributeControlView : UserControl {
        AttributeControlViewModel _viewModel = null;
        
        public AttributeControlView() {
            InitializeComponent();
            _viewModel = this.DataContext as AttributeControlViewModel;
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
        }

        private void SelectionButton_OnClicked(object sender, RoutedEventArgs e) {
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        }

        private void Scale_value_TextChanged(object sender, TextChangedEventArgs e) {
        }
    }


}
