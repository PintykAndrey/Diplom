using System;
using System.ComponentModel.DataAnnotations;

namespace Diplom.Models.Identity
{
    public class FriendRequest
    {
        public int Id { get; set; }

        [Required]
        public string SenderUserId { get; set; }

        [Required]
        public string ReceiverUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
