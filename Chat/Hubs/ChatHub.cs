using Chat.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(AppDbContext context, ILogger<ChatHub> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task JoinChatGroup(string currentUserId, string otherUserId)
    {
        try
        {
            // Создаем универсальное имя группы (сортировка ID)
            var groupName = GetGroupName(currentUserId, otherUserId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation("User {UserId} joined group {GroupName}", currentUserId, groupName);

            // Отправляем историю сообщений
            var messages = await _context.Messages
                .Where(m => (m.SenderId == int.Parse(currentUserId) && m.ReceiverId == int.Parse(otherUserId)) ||
                           (m.SenderId == int.Parse(otherUserId) && m.ReceiverId == int.Parse(currentUserId)))
                .OrderBy(m => m.SentTime)
                .ToListAsync();

            foreach (var message in messages)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    Content = message.Content,
                    SentTime = message.SentTime,
                    IsRead = message.IsRead
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining chat group");
            throw new HubException("Failed to join chat group");
        }
    }

    public async Task SendMessage(int receiverId, string messageContent)
    {
        try
        {
            var senderId = int.Parse(Context.UserIdentifier);

            if (string.IsNullOrWhiteSpace(messageContent))
                throw new HubException("Message cannot be empty");

            // Сохраняем сообщение в БД
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = messageContent,
                SentTime = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Получаем имя группы (одинаковое для обоих пользователей)
            var groupName = GetGroupName(senderId.ToString(), receiverId.ToString());

            // Отправляем сообщение всем в группе (включая отправителя)
            await Clients.Group(groupName).SendAsync("ReceiveMessage", new
            {
                Id = message.Id,
                SenderId = senderId,
                Content = messageContent,
                SentTime = message.SentTime,
                IsRead = false
            });

            // Отдельное подтверждение отправителю
            await Clients.Caller.SendAsync("MessageSent", new
            {
                Id = message.Id,
                ReceiverId = receiverId,
                Content = messageContent,
                SentTime = message.SentTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            throw new HubException("Error sending message");
        }
    }

    private string GetGroupName(string userId1, string userId2)
    {
        // Универсальное имя группы (сортировка ID для одинакового имени)
        return string.Compare(userId1, userId2) < 0
            ? $"chat_{userId1}_{userId2}"
            : $"chat_{userId2}_{userId1}";
    }

    public async Task SendTypingNotification(int receiverId)
    {
        try
        {
            var senderId = int.Parse(Context.UserIdentifier);
            await Clients.User(receiverId.ToString()).SendAsync("UserTyping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending typing notification");
        }
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var user = await _context.Users.FindAsync(int.Parse(userId));
        if (user != null)
        {
            user.IsOnline = true;
            await _context.SaveChangesAsync();

            await Clients.Others.SendAsync("UserStatusChanged", new
            {
                UserId = userId,
                IsOnline = true,
                LastOnline = user.LastOnline
            });
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        var user = await _context.Users.FindAsync(int.Parse(userId));
        if (user != null)
        {
            user.IsOnline = false;
            user.LastOnline = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await Clients.Others.SendAsync("UserStatusChanged", new
            {
                UserId = userId,
                IsOnline = false,
                LastOnline = user.LastOnline
            });
        }
        await base.OnDisconnectedAsync(exception);
    }
}