using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WatchStore.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required] public string UserId { get; set; } = default!;
        public IdentityUser User { get; set; } = default!;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required, StringLength(160)]
        public string ReceiverName { get; set; } = default!;

        [Required, StringLength(300)]
        public string ShippingAddress { get; set; } = default!;

        [Required, StringLength(20)]
        public string Phone { get; set; } = default!;

        public decimal TotalAmount { get; set; }

        [StringLength(40)] public string Status { get; set; } = "Pending";
        [StringLength(400)] public string? AdminNote { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
 

    }
}
