using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class ChatController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public ChatController(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("Chat/Conversation/{userId:int}")] // Явно указываем тип параметра
    public async Task<IActionResult> Conversation(int userId)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge(); // Перенаправит на страницу входа

            var otherUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (otherUser == null) return NotFound();

            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == userId) ||
                           (m.SenderId == userId && m.ReceiverId == currentUser.Id))
                .OrderBy(m => m.SentTime)
                .AsNoTracking()
                .ToListAsync();

            return View(new ChatViewModel
            {
                CurrentUser = currentUser,
                OtherUser = otherUser,
                Messages = messages
            });
        }
        catch (Exception ex)
        {
            // Логирование ошибки
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpGet("Chat")]
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var users = await _context.Users
            .Where(u => u.Id != currentUser.Id)
            .OrderByDescending(u => u.IsOnline)
            .ThenBy(u => u.UserName)
            .AsNoTracking()
            .ToListAsync();

        return View(users);
    }
}
