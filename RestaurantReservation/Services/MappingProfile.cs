using AutoMapper;
using RestaurantReservation.Models.DTOs.Reservations;
using RestaurantReservation.Models.DTOs.Tables;
using RestaurantReservation.Models;

namespace RestaurantReservation.Services;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<RestaurantTable, TableDto>();
        CreateMap<CreateTableDto, RestaurantTable>();

        CreateMap<Reservation, ReservationDto>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.User != null ? s.User.FullName : string.Empty))
            .ForMember(d => d.TableNumber, o => o.MapFrom(s => s.Table != null ? s.Table.TableNumber : 0))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
    }
}
