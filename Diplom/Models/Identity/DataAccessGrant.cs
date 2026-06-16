using System;
using System.ComponentModel.DataAnnotations;

namespace Diplom.Models.Identity
{
    public enum SharedDataSection
    {
        Fields = 1,
        Warehouses = 2,
        Equipment = 3,
        Tools = 4
    }

    public enum DataAccessLevel
    {
        View = 1,
        Edit = 2
    }

    public class DataAccessGrant
    {
        public int Id { get; set; }

        [Required]
        public string OwnerUserId { get; set; }

        [Required]
        public string GranteeUserId { get; set; }

        public SharedDataSection Section { get; set; }

        public DataAccessLevel AccessLevel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
