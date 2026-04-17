using System;

namespace TasteAtDoor.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }

        public int ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }
    }
}