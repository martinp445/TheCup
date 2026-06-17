namespace TheCup_Application.Ports;

public interface IFileSaveDialogService
{
    string? PromptSavePdf(string suggestedFileName);
}
