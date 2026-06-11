using Chat.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    

    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Настройка отношений для Message
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)  // Указываем навигационное свойство в User
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();  // Явно указываем, что отправитель обязателен

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)  // Указываем навигационное свойство в User
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();  // Явно указываем, что получатель обязателен

        // Дополнительные настройки (если нужно)
        modelBuilder.Entity<Message>()
            .Property(m => m.SentTime)
            .HasDefaultValueSql("GETDATE()");  // Автоматическое установление времени отправки
    }
}
