namespace TheCup_Domain.Entities;

public class Team
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public int Points { get; set; } = 0;

    public int Goals { get; set; } = 0;

    public int ConcededGoals { get; set; } = 0;
}
