using Chat.Models;
using System.Collections.Generic;

public class ChatViewModel
{
    public User CurrentUser { get; set; } = null!; // текущий пользователь
    public User OtherUser { get; set; } = null!; // собеседник
    public List<Message> Messages { get; set; } = new List<Message>(); // история сообщений
    public string NewMessage { get; set; } = string.Empty; //новое сообщение
}