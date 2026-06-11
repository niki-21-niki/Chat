using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chat.Models
{
    public class Message
    {
        public int Id { get; set; } //унакальный номер диалога

        [ForeignKey("Sender")]
        public int SenderId { get; set; } // отправитель

        [ForeignKey("Receiver")]
        public int ReceiverId { get; set; } //получатель

        [Required]
        public string Content { get; set; } = string.Empty; //текст сообщения

        public DateTime SentTime { get; set; } //время отправки
        public bool IsRead { get; set; } //статус сообщения
        public DateTime? ReadTime { get; set; } 

        public virtual User Sender { get; set; } = null!; //ссылка на отправителя
        public virtual User Receiver { get; set; } = null!; //сссылка на получателя
    }
}