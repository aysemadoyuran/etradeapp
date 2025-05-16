using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
public class SliderViewModel
{
    public int Id { get; set; }
    public string? TopTitle { get; set; }
    public string? Title { get; set; }
    public string? ButtonTitle { get; set; }
    public string? ButtonUrl { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
}

}