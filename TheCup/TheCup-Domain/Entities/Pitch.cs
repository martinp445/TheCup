namespace TheCup_Domain.Entities;

public class Pitch
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
}
