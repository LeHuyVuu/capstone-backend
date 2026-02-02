using AutoMapper;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class NotificationProfile : Profile
    {
        public NotificationProfile()
        {
            CreateMap<CreateNotificationRequest, Notification>();
        }
    }
}
