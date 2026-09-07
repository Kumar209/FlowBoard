using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Identity.Service.Application.DTOs;
using Identity.Service.Application.Interfaces;

namespace Identity.Service.Application.Commands;

public record RefreshCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;

public class RefreshCommandHandler : IRequestHandler<RefreshCommand, Result<AuthResponse>>
{
    private readonly IAuthService _authService;
    public RefreshCommandHandler(IAuthService authService) => _authService = authService;
    public Task<Result<AuthResponse>> Handle(RefreshCommand request, CancellationToken ct)
        => _authService.RefreshAsync(request.RefreshToken, ct);
}
