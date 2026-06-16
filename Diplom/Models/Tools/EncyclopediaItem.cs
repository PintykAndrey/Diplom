using System.ComponentModel.DataAnnotations;
using Diplom.Models.Identity;

namespace Diplom.Models.Tools
{
    public class EncyclopediaItem : IOwnedEntity
    {
        public int Id { get; set; }

        public string OwnerUserId { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        public string Name { get; set; }
    }
}