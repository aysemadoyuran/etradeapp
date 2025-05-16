using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{

    public class LinkModel
    {
        public string Text { get; set; }
        public string Url { get; set; }
    }

    public class TopBarSection
    {
        public List<LinkModel> Links { get; set; }
    }

    public class StoreSettingsFullModel
    {
        public TopBarSection Topbar { get; set; }
    }

    // Add this model class to your project
    public class StoreSettingsModel
    {
        public MobileMenuModel MobileMenu { get; set; }
        // Add other sections as needed (Topbar, Footer, etc.)
    }

    public class MobileMenuModel
    {
        public string AddressTitle { get; set; }
        public string Address { get; set; }
        public string ButtonTitle { get; set; }
        public string ButtonUrl { get; set; }
        public string Mail { get; set; }
        public string Phone { get; set; }
        public List<LinkModel> Links { get; set; }
    }
    public class StoreSettingUpdateModel
    {
        public IFormFile LogoPath { get; set; } // Logo dosyası
        public IFormFile LogoWhitePath { get; set; } // Beyaz Logo dosyası
        public string IyzicoApiKey { get; set; }
        public string IyzicoSecretKey { get; set; }
        public string IyzicoBaseUrl { get; set; }
    }
    public class FooterModel
    {
        public string Address { get; set; }
        public string ButtonTitle { get; set; }
        public string ButtonUrl { get; set; }
        public string Mail { get; set; }
        public string Phone { get; set; }
    }

    public class SiteMapModel
    {
        public List<SiteMapLinkModel> Links { get; set; }
    }

    public class SiteMapLinkModel
    {
        public string Text { get; set; }
        public string Url { get; set; }
    }

    public class PolicyModel
    {
        public List<PolicyLinkModel> Links { get; set; }
    }

    public class PolicyLinkModel
    {
        public string Text { get; set; }
        public string Url { get; set; }
    }

    public class StoreSettingModel
    {
        public FooterModel Footer { get; set; }
        public SiteMapModel SiteMaps { get; set; }
        public PolicyModel Policies { get; set; }
    }

    public class InvoiceInfoUpdateModel
    {
        public string StoreName { get; set; }
        public string CityCountry { get; set; }
        public string Tel { get; set; }
        public string Email { get; set; }
        public IFormFile LogoPath { get; set; }
    }
    public class StoreInfo
    {
        public InvoiceInfo InvoiceInfo { get; set; }
    }

    public class InvoiceInfo
    {
        public string logoPath { get; set; }
        public string storeName { get; set; }
        public string cityCountry { get; set; }
        public ContactInfo contactInfo { get; set; }
    }

    public class ContactInfo
    {
        public string tel { get; set; }
        public string email { get; set; }
    }

}