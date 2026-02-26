using MediatR;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Auth.Commands.RotateToken;

public record RotateTokenCommand(string AccessToken, string RefreshToken) : IRequest<Result<RotateTokenResponse>>;

public record RotateTokenResponse(string AccessToken, string RefreshToken);