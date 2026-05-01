namespace TasteAtDoor.Models
{
    public class CustomizationSelection
    {
        public int GroupId { get; set; }
        public string GroupTitle { get; set; } = string.Empty;
        public int OptionId { get; set; }
        public string OptionName { get; set; } = string.Empty;
        public decimal PriceChange { get; set; }
    }
}