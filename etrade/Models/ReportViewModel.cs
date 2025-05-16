using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    public class ReportViewModel
    {
        public List<dynamic>? CategoryReport { get; set; }
        public List<dynamic>? SubCategoryReport { get; set; }
    }
}