namespace DisciplineApp.Services.Interfaces;

public interface IDataExportService
{
    Task<string> ExportJsonAsync(string userId);
}
