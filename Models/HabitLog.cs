namespace DisciplineApp.Models;

public class HabitLog
{
    public int Id { get; set; }
    public int HabitId { get; set; }
    public Habit? Habit { get; set; }
    public DateTime Date { get; set; }
}
