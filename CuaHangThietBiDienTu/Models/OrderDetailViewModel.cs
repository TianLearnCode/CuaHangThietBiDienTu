using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CuaHangThietBiDienTu.Models
{
    public class OrderDetailViewModel
    {
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
        public string Image { get; set; }
    }

}