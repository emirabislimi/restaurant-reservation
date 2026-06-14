namespace RestaurantReservation.Data;

/// <summary>Thrown when a domain/business rule is violated (mapped to HTTP 400/409).</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
