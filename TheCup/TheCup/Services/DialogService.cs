using System.Windows;
using TheCup_Application.Services;

namespace TheCup_Presentation.Services
{
    public class DialogService : IDialogService
    {
        private readonly string viewNamespace = "TheCup_Presentation.Views";

        public bool? ShowDialog(object viewModel, bool modal = false)
        {
            
            var viewTypeName = viewModel.GetType().Name.Replace("ViewModel", "View");
            var fullViewTypeName = $"{viewNamespace}.{viewTypeName}";
            var viewType = Type.GetType(fullViewTypeName);

            if (viewType == null) return null;

            var window = (Window?)Activator.CreateInstance(viewType);

            if (window == null) return null;

            window.DataContext = viewModel;

            if (modal)
            {
                return window.ShowDialog();
            }
            else
            {
                window.Show();
                return null;
            }
        }

    }
}
