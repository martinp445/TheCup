using Microsoft.Win32;
using TheCup_Application.Ports;

namespace TheCup_Presentation.Services;

public sealed class WpfFileSaveDialogService : IFileSaveDialogService
{
    public string? PromptSavePdf(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            FileName = suggestedFileName,
            AddExtension = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
