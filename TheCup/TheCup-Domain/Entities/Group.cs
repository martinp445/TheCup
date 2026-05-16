namespace TheCup_Domain.Entities;

public class Group
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public List<Guid> TeamIds { get; } = [];
}
