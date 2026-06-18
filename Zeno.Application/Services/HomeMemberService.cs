using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Homes;
using Zeno.Application.Validators;
using Zeno.Domain.Home;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class HomeMemberService : Zeno.Application.Interfaces.IHomeMemberService
{
    private readonly IHomeRepository _repository;

    public HomeMemberService(IHomeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<HomeMember>> GetMembers(Guid userId, Guid homeId)
    {
        await ValidateMembershipAsync(homeId, userId);
        return await _repository.GetMembersByHomeAsync(homeId);
    }

    public async Task InviteMember(Guid adminUserId, Guid homeId, AddHomeMemberRequest request)
    {
        var isAdmin = await _repository.IsAdminAsync(homeId, adminUserId);
        if (!isAdmin)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Apenas o administrador pode convidar membros.")
                }));

        var alreadyMember = await _repository.IsMemberAsync(homeId, request.UserId);
        if (alreadyMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.UserId), "Este usuário já é membro da casa.")
                }));

        await _repository.AddMemberAsync(homeId, request.UserId, (int)Zeno.Domain.Enum.HomeRole.Member);
    }

    public async Task RemoveMember(Guid adminUserId, Guid homeId, Guid memberUserId)
    {
        var isAdmin = await _repository.IsAdminAsync(homeId, adminUserId);
        if (!isAdmin)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Apenas o administrador pode remover membros.")
                }));

        if (adminUserId == memberUserId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(memberUserId), "O administrador não pode remover a si mesmo.")
                }));

        await _repository.RemoveMemberAsync(homeId, memberUserId);
    }

    private async Task ValidateMembershipAsync(Guid homeId, Guid userId)
    {
        var isMember = await _repository.IsMemberAsync(homeId, userId);
        if (!isMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Você não é membro desta casa.")
                }));
    }
}