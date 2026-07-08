namespace Portfolio.Domain;

/// <summary>One deployment, recorded at startup when the running git SHA changes.</summary>
public sealed class Deploy
{
    public int Id { get; set; }
    public required string GitSha { get; set; }
    public DateTime DeployedAtUtc { get; set; }
}
