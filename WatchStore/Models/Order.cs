using System.ComponentModel.DataAnnotations;

namespace WatchStore.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required, StringLength(160)]
        public string ReceiverName { get; set; } = default!;

        [Required, StringLength(300)]
        public string ShippingAddress { get; set; } = default!;

        [Required, StringLength(20)]
        public string Phone { get; set; } = default!;

        public decimal TotalAmount { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public string Status { get; set; } = "New";
    }

    
}
