namespace TasteAtDoor.Models
{
    public class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public int CatererId { get; set; }
        public Caterer? Caterer { get; set; }
    }
}