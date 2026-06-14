using RestaurantReservation.Models;

namespace RestaurantReservation.Services.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
