using Chat.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class User : IdentityUser<int>
{
    [Required]
    [MaxLength(50)]
    public string DisplayName { get; set; } = string.Empty; //отображаемое имя

    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow; //дата регистации
    public DateTime LastOnline { get; set; } = DateTime.UtcNow; //последний раз в сети
    public bool IsOnline { get; set; } //онлайн статус
    public string? AvatarUrl { get; set; }

    [NotMapped] 
    public string? ConnectionId { get; set; } //временный ID подключения SignalR

    // Навигационные свойства
    public virtual ICollection<Message> SentMessages { get; set; } = new HashSet<Message>(); //отправленные сообщения
    public virtual ICollection<Message> ReceivedMessages { get; set; } = new HashSet<Message>(); //полученные сообщения
}