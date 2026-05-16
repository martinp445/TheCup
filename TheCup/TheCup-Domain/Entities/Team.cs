namespace TheCup_Domain.Entities;

public class Team
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
}
