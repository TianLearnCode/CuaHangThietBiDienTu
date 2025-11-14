using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CuaHangThietBiDienTu.Models
{
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public void AddItem(SanPham product, int quantity)
        {
            var existingItem = Items.FirstOrDefault(x => x.ProductId == product.MaSP);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Items.Add(new CartItem
                {
                    ProductId = product.MaSP,
                    ProductName = product.TenSP,
                    Price = product.GiaBan ?? 0,
                    Discount = product.GiamGia ?? 0,
                    Quantity = quantity,
                    Image = product.HinhAnh,
                    Stock = product.SoLuongTon ?? 0
                });
            }
        }

        public void RemoveItem(int productId)
        {
            Items.RemoveAll(x => x.ProductId == productId);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public decimal GetTotalAmount()
        {
            return Items.Sum(x => x.Total);
        }
    }

    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; }
        public int Stock { get; set; }

        public decimal ActualPrice => Price * (1 - Discount / 100);
        public decimal Total => ActualPrice * Quantity;
    }

    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string Address { get; set; }

        public string PaymentMethod { get; set; }
        public string Notes { get; set; }
        public Cart Cart { get; set; }
    }
}