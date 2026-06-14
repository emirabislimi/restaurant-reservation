using AutoMapper;
using RestaurantReservation.Data;
using RestaurantReservation.Models.DTOs.Reservations;
using RestaurantReservation.Models;
using RestaurantReservation.Models.Enums;
using RestaurantReservation.Repositories.Interfaces;
using RestaurantReservation.Services.Interfaces;

namespace RestaurantReservation.Services.Implementations;

/// <summary>
/// Holds the reservation domain logic:
///  - <see cref="CreateAsync"/> is the "business logic" (validation beyond CRUD).
///  - <see cref="AutoReserveAsync"/> is the "complex business logic" (orchestrates
///    multiple repositories to find and book the best available table atomically).
/// </summary>
public class ReservationService : IReservationService
{
    private const int MinCancelLeadMinutes = 60;   // can't cancel within 1h of start
    private const int MaxAdvanceDays = 60;         // can't book more than 60 days ahead

    private readonly IReservationRepository _reservations;
    private readonly ITableRepository _tables;
    private readonly IUserRepository _users;
    private readonly IMapper _mapper;

    public ReservationService(
        IReservationRepository reservations,
        ITableRepository tables,
        IUserRepository users,
        IMapper mapper)
    {
        _reservations = reservations;
        _tables = tables;
        _users = users;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ReservationDto>> GetAllAsync(CancellationToken ct = default)
    {
        var all = await _reservations.GetAllWithDetailsAsync(ct);
        return all.Select(_mapper.Map<ReservationDto>).ToList();
    }

    public async Task<IReadOnlyList<ReservationDto>> GetMineAsync(Guid userId, CancellationToken ct = default)
    {
        var mine = await _reservations.GetForUserAsync(userId, ct);
        return mine.Select(_mapper.Map<ReservationDto>).ToList();
    }

    public async Task<ReservationDto> GetByIdAsync(Guid id, Guid requesterId, UserRole requesterRole,
        CancellationToken ct = default)
    {
        var reservation = await _reservations.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException($"Reservation {id} was not found.");

        // Authorization rule: customers may only view their own reservations.
        if (requesterRole != UserRole.Admin && reservation.UserId != requesterId)
            throw new ForbiddenException("You are not allowed to view this reservation.");

        return _mapper.Map<ReservationDto>(reservation);
    }

    // ----- Business logic: reserve a specific table -----
    public async Task<ReservationDto> CreateAsync(Guid userId, CreateReservationDto dto,
        CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ValidateWindow(dto.ReservationStartUtc, dto.DurationMinutes);

        if (!await _users.ExistsAsync(u => u.Id == userId, ct))
            throw new NotFoundException("The requesting user no longer exists.");

        var table = await _tables.GetByIdAsync(dto.TableId, ct)
            ?? throw new NotFoundException($"Table {dto.TableId} was not found.");

        if (!table.IsActive)
            throw new BusinessRuleException("This table is not currently available for booking.");

        if (dto.PartySize > table.Capacity)
            throw new BusinessRuleException(
                $"Party size {dto.PartySize} exceeds table capacity of {table.Capacity}.");

        if (await _reservations.HasOverlappingReservationAsync(table.Id, startUtc, endUtc, null, ct))
            throw new BusinessRuleException("The selected table is already booked for that time.");

        var reservation = new Reservation
        {
            UserId = userId,
            TableId = table.Id,
            ReservationStartUtc = startUtc,
            ReservationEndUtc = endUtc,
            PartySize = dto.PartySize,
            Status = ReservationStatus.Confirmed
        };

        await _reservations.AddAsync(reservation, ct);
        await _reservations.SaveChangesAsync(ct);

        return await ReloadAsync(reservation.Id, ct);
    }

    // ----- Complex business logic: auto-assign the best free table -----
    public async Task<ReservationDto> AutoReserveAsync(Guid userId, AutoReserveDto dto,
        CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ValidateWindow(dto.ReservationStartUtc, dto.DurationMinutes);

        if (!await _users.ExistsAsync(u => u.Id == userId, ct))
            throw new NotFoundException("The requesting user no longer exists.");

        // 1. Pull every active table large enough for the party (smallest first),
        //    optionally filtered by a preferred location.
        var candidates = await _tables.GetCandidateTablesAsync(dto.PartySize, dto.PreferredLocation, ct);
        if (candidates.Count == 0)
            throw new BusinessRuleException(
                "No table can accommodate this party size for the requested criteria.");

        // 2. Walk candidates and pick the first one with no overlapping booking.
        //    Choosing the smallest fitting table keeps larger tables free for bigger groups.
        RestaurantTable? chosen = null;
        foreach (var table in candidates)
        {
            var taken = await _reservations.HasOverlappingReservationAsync(table.Id, startUtc, endUtc, null, ct);
            if (!taken)
            {
                chosen = table;
                break;
            }
        }

        if (chosen is null)
            throw new BusinessRuleException("All suitable tables are booked for the requested time.");

        // 3. Book it.
        var reservation = new Reservation
        {
            UserId = userId,
            TableId = chosen.Id,
            ReservationStartUtc = startUtc,
            ReservationEndUtc = endUtc,
            PartySize = dto.PartySize,
            Status = ReservationStatus.Confirmed
        };

        await _reservations.AddAsync(reservation, ct);
        await _reservations.SaveChangesAsync(ct);

        return await ReloadAsync(reservation.Id, ct);
    }

    // ----- Business logic: cancel with ownership + timing rules -----
    public async Task<ReservationDto> CancelAsync(Guid id, Guid requesterId, UserRole requesterRole,
        CancellationToken ct = default)
    {
        var reservation = await _reservations.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Reservation {id} was not found.");

        if (requesterRole != UserRole.Admin && reservation.UserId != requesterId)
            throw new ForbiddenException("You are not allowed to cancel this reservation.");

        if (reservation.Status == ReservationStatus.Cancelled)
            throw new BusinessRuleException("This reservation is already cancelled.");

        if (reservation.Status == ReservationStatus.Completed)
            throw new BusinessRuleException("A completed reservation cannot be cancelled.");

        // Customers must cancel at least 1 hour before the start. Admins may override.
        if (requesterRole != UserRole.Admin &&
            reservation.ReservationStartUtc - DateTime.UtcNow < TimeSpan.FromMinutes(MinCancelLeadMinutes))
        {
            throw new BusinessRuleException(
                $"Reservations can only be cancelled at least {MinCancelLeadMinutes} minutes in advance.");
        }

        reservation.Status = ReservationStatus.Cancelled;
        _reservations.Update(reservation);
        await _reservations.SaveChangesAsync(ct);

        return await ReloadAsync(reservation.Id, ct);
    }

    // ----- Admin-only status transition -----
    public async Task<ReservationDto> UpdateStatusAsync(Guid id, ReservationStatus status,
        CancellationToken ct = default)
    {
        var reservation = await _reservations.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Reservation {id} was not found.");

        if (reservation.Status == ReservationStatus.Cancelled && status != ReservationStatus.Cancelled)
            throw new BusinessRuleException("A cancelled reservation cannot be reactivated.");

        reservation.Status = status;
        _reservations.Update(reservation);
        await _reservations.SaveChangesAsync(ct);

        return await ReloadAsync(reservation.Id, ct);
    }

    // ----- Helpers -----
    private (DateTime startUtc, DateTime endUtc) ValidateWindow(DateTime startUtc, int durationMinutes)
    {
        // Normalise to UTC so comparisons are consistent regardless of client input kind.
        startUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);

        if (startUtc <= DateTime.UtcNow)
            throw new BusinessRuleException("Reservation time must be in the future.");

        if (startUtc > DateTime.UtcNow.AddDays(MaxAdvanceDays))
            throw new BusinessRuleException($"Reservations can be made at most {MaxAdvanceDays} days in advance.");

        if (durationMinutes < 15 || durationMinutes > 600)
            throw new BusinessRuleException("Duration must be between 15 and 600 minutes.");

        return (startUtc, startUtc.AddMinutes(durationMinutes));
    }

    private async Task<ReservationDto> ReloadAsync(Guid id, CancellationToken ct)
    {
        var fresh = await _reservations.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException($"Reservation {id} was not found.");
        return _mapper.Map<ReservationDto>(fresh);
    }
}
