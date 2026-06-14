using System.Security.Claims;
using RestaurantReservation.Models.Enums;

namespace RestaurantReservation.Data;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id)
            ? id
            : throw new UnauthorizedAccessException("User id claim is missing or invalid.");
    }

    public static UserRole GetUserRole(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<UserRole>(raw, out var role) ? role : UserRole.Customer;
    }
}
