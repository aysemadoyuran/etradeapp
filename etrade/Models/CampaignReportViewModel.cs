using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    public class CampaignReportViewModel
    {
        public string CampaignName { get; set; }
        public DateTime CampaignStartDate { get; set; }
        public DateTime CampaignEndDate { get; set; }
        public decimal TotalSalesDuringCampaign { get; set; }
        public decimal TotalRevenueBeforeCampaign { get; set; }
        public decimal CampaignScore { get; set; }
        public object MostSoldProduct { get; set; }
        public decimal AverageDailyRevenueDuringCampaign { get; set; }
        public decimal AverageDailyRevenueBeforeCampaign { get; set; }
        public List<ProductSalesComparison> ProductSalesComparison { get; set; }
    }
    public class ProductSalesComparison
    {
        public int ProductVariantId { get; set; }
        public int SalesDuringCampaign { get; set; }
        public double AverageSalesBeforeCampaign { get; set; }
        public double SalesDifference { get; set; }
    }

}