using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Shop.Services
{
    public class RefundService
    {
        private readonly EtradeContext _db;

        public RefundService(EtradeContext db)
        {
            _db = db;
        }
        public class RefundItemDto
        {
            public int ProductVariantId { get; set; } // İade edilecek ürün varyantının ID'si
            public int Quantity { get; set; } // İade edilecek miktar
        }

        public async Task<RefundRequest> CreateRefundAsync(int orderId, List<RefundItemDto> items, int paymentMethodId, string iban)
        {
            // 1. Siparişi ve ürünleri kontrol et
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                throw new Exception("Sipariş bulunamadı!");

            // 2. İade edilecek ürünleri kontrol et
            var refundedItems = new List<RefundedItem>();
            decimal totalRefund = 0;

            foreach (var item in items)
            {
                var orderItem = order.OrderItems.FirstOrDefault(oi => oi.ProductVariantId == item.ProductVariantId);
                if (orderItem == null)
                    throw new Exception($"Ürün (ID: {item.ProductVariantId}) bu siparişte yok!");

                if (item.Quantity > orderItem.Quantity)
                    throw new Exception($"İade miktarı geçersiz! Siparişte {orderItem.Quantity} adet var.");

                var refundedItem = new RefundedItem
                {
                    ProductVariantId = item.ProductVariantId,
                    Quantity = item.Quantity,
                    TotalPrice = orderItem.Price * item.Quantity
                };

                refundedItems.Add(refundedItem);
                totalRefund += refundedItem.TotalPrice;
            }

            // 3. İade talebini oluştur
            var refundRequest = new RefundRequest
            {
                OrderId = orderId,
                PaymentMethodId = paymentMethodId,
                Iban = iban,
                TotalPrice = totalRefund,
                RefundStatus = "Pending",
                RefundRequestDate = DateTime.Now,
                RefundedItems = refundedItems
            };

            _db.RefundRequests.Add(refundRequest);
            await _db.SaveChangesAsync();

            return refundRequest;
        }
    }
}