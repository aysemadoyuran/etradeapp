using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class Slider
    {
        public int Id { get; set; }
        public string? TopTitle { get; set; }
        public string? Title { get; set; }
        public string? ButtonTitle { get; set; }
        public string? ButtonUrl { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public string? SliderCategory { get; set; }
    }
}