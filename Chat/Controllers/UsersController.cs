using Chat.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Chat.Models; // Добавьте эту строку для доступа к моделям

namespace Chat.Controllers
{
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.IsOnline)
                .ThenBy(u => u.UserName)  // Изменили Username на UserName (из IdentityUser)
                .ToListAsync();

            return View(users);
        }
        [Authorize]
        public async Task<IActionResult> Chat(int id)
        {
            var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdString))
            {
                return Challenge();
            }
            var currentUserId = int.Parse(currentUserIdString);

            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == id) ||
                           (m.SenderId == id && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentTime)
                .ToListAsync();

            var currentUser = await _context.Users.FindAsync(currentUserId);
            var otherUser = await _context.Users.FindAsync(id);

            if (currentUser == null || otherUser == null)
            {
                return NotFound();
            }

            var viewModel = new ChatViewModel
            {
                CurrentUser = currentUser,
                OtherUser = otherUser,
                Messages = messages
            };

            return View(viewModel);
        }
    }
}