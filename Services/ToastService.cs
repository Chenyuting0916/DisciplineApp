namespace DisciplineApp.Services;

public class ToastService
{
    public event Action<string, string>? OnShow;

    public void ShowToast(string message, string type = "success")
    {
        OnShow?.Invoke(message, type);
    }

    public void ShowSuccess(string message) => ShowToast(message, "success");
    public void ShowError(string message) => ShowToast(message, "danger");
    public void ShowWarning(string message) => ShowToast(message, "warning");
    public void ShowInfo(string message) => ShowToast(message, "info");
}
