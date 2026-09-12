namespace Zeno.Services.Push;

public class PushOptions
{
    public const string SectionName = "Push";

    public FirebaseOptions Firebase { get; set; } = new();
}

public class FirebaseOptions
{
    /// <summary>project_id da service account do Firebase.</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>client_email da service account.</summary>
    public string ClientEmail { get; set; } = string.Empty;

    /// <summary>private_key da service account, em PEM. Aceita a forma com \n escapado que vem do JSON baixado do Firebase.</summary>
    public string PrivateKey { get; set; } = string.Empty;

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(ProjectId) &&
        !string.IsNullOrWhiteSpace(ClientEmail) &&
        !string.IsNullOrWhiteSpace(PrivateKey);
}
