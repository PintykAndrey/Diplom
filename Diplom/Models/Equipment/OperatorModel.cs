using Diplom.Models.Identity;
using System.ComponentModel.DataAnnotations;

namespace Diplom.Models
{
    public class OperatorModel : IOwnedEntity
    {
        public int Id { get; set; }

        public string OwnerUserId { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string Surname { get; set; }  
    }
}