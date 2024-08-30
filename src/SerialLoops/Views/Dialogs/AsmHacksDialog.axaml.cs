using Avalonia.Controls;
using SerialLoops.ViewModels.Dialogs;

namespace SerialLoops.Views.Dialogs
{
    public partial class AsmHacksDialog : Window
    {
        public AsmHacksDialogViewModel ViewModel { get; }
        public AsmHacksDialog(AsmHacksDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            ViewModel = viewModel;
        }
    }
}
