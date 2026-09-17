using System.ComponentModel.DataAnnotations;

namespace DisciplineApp.Models;

public class DailyIntention
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    [MaxLength(InputGuard.VowMax)]
    public string Vow { get; set; } = string.Empty;

    [MaxLength(InputGuard.TitleMax)]
    public string OneThing { get; set; } = string.Empty;
}
