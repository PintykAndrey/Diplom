using System;
using System.ComponentModel.DataAnnotations;

namespace Diplom.Models.Identity
{
    public class Friendship
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string FriendUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
