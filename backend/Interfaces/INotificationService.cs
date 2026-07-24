
using backend.DTOs;

namespace backend.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationResponseDto>> GetUnreadNotificationsAsync(int userId);
        Task MarkAllReadAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
    }
}