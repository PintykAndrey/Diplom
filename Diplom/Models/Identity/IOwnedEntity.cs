namespace Diplom.Models.Identity
{
    public interface IOwnedEntity
    {
        string OwnerUserId { get; set; }
    }
}
