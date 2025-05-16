using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
public class Size
{
    public int Id { get; set; } // Primary Key
    public string? Name { get; set; } // Beden Adı (S, M, L, XL vb.)

    public ICollection<ProductVariant>? ProductVariants { get; set; } // Renk ve beden bazlı stok ilişkisi
}


}