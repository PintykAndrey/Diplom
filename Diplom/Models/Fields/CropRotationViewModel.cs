using Diplom.Models.Tools;
using System.ComponentModel.DataAnnotations.Schema;

namespace Diplom.Models.Fields
{
    public class CropRotationViewModel
    {
        public List<FieldEntity> Fields { get; set; } = new();
        public List<int> Years { get; set; } = new();
        public List<EncyclopediaItem> Crops { get; set; } = new(); 
    }   
}
