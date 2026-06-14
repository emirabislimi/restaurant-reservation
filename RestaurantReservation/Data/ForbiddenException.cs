namespace RestaurantReservation.Data;

/// <summary>Thrown when a user attempts an action they are not allowed to perform (HTTP 403).</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
