using FluentValidation.Results;

namespace Zeno.Responses;

public class ServiceResult<T>
{
    public bool IsValid { get; set; }
    public T? Data { get; set; }
    public IEnumerable<ValidationError>? Errors { get; set; }

    public static ServiceResult<T> Ok(T data) => new() { IsValid = true, Data = data };
    public static ServiceResult<T> Fail(IEnumerable<ValidationError> errors) => new() { IsValid = false, Errors = errors };
}

public class ValidationError
{
    public string Property { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
