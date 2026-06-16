using Diplom.Models.Identity;

namespace Diplom.Models.Navigation
{
    public class UserQuickAction : IOwnedEntity
    {
        public int Id { get; set; }
        public string OwnerUserId { get; set; }
        public string UserId { get; set; }  
        public string ActionKey { get; set; }  
        public bool IsEnabled { get; set; }  
        public int DisplayOrder { get; set; }  
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public string Url { get; set; }
    }
}
