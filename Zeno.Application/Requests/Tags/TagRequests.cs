namespace Zeno.Application.Requests.Tags;

public class CreateTagRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateTagRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
