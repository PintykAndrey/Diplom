namespace Diplom.Models.Fields
{
    public class FieldDetailsViewModel
    {
        public List<FieldEntity> Fields { get; set; }
        public FieldEntity SelectedField { get; set; }

        public List<FieldWorkLogPlanModel> WorkLogs { get; set; }
        public List<FieldInspectionNote> InspectionNotes { get; set; } = new();
    }

    public class FieldInspectionNote
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public byte[] Photo { get; set; }
    }
}