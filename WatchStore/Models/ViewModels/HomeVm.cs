using System.Collections.Generic;

namespace WatchStore.Models.ViewModels
{
    public class HomeVm
    {
        public IEnumerable<Product> NewArrivals { get; set; } = new List<Product>();
        public IEnumerable<Product> BestSellers { get; set; } = new List<Product>();
        public IEnumerable<string> Brands { get; set; } = new List<string>();

        // filter nhanh ở hero
        public string? Q { get; set; }
        public string? Brand { get; set; }
    }
}
