using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    public class DemoRequestViewModel
    {
        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Telefon zorunludur.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Şifre tekrar zorunludur.")]
        [Compare("Password", ErrorMessage = "Şifreler uyuşmuyor.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Firma adı zorunludur.")]
        public string CompanyName { get; set; }

        public string TaxNumber { get; set; }
        public string TaxOffice { get; set; }
        public string Address { get; set; }

        [Required(ErrorMessage = "Şehir zorunludur.")]
        public sbyte IlId { get; set; }

        public string ZipCode { get; set; }
        public string RequestNote { get; set; }
        public int DemoDays { get; set; } = 7;

        [Range(typeof(bool), "true", "true", ErrorMessage = "KVKK onayı gereklidir.")]
        public bool KvkkApproval { get; set; }
        public bool PromotionEmails { get; set; }
    }
}