using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Entity;

namespace etrade.Models
{

    public class CommentViewModel
    {
        public List<Comment> Comments { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int[] StarCounts { get; set; } // [1 yıldız, 2 yıldız, 3 yıldız, 4 yıldız, 5 yıldız]
        public List<string> Images { get; set; } = new(); // Fotoğraf URL'leri için liste

    }
}