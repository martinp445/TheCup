namespace TheCup_Application.Services
{
    public interface IDialogService
    {
        bool? ShowDialog(object viewModel, bool modal = false);
    }
}
