using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace CuaHangThietBiDienTu.Models
{
    public class AdminDashboardViewModel
    {
        public int TongKhachHang { get; set; }
        public decimal TongDoanhThu { get; set; }
        public int TongSanPham { get; set; }
        public int TongDonHang { get; set; }
        public decimal DoanhThuThang { get; set; }
        public int DonHangThang { get; set; }
        public int CurrentMonth { get; set; }
        public int CurrentYear { get; set; }
        public List<SanPhamBanChayViewModel> SanPhamBanChay { get; set; }
        public List<DonHang> DonHangMoiNhat { get; set; }
    }

    public class SanPhamBanChayViewModel
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public string HinhAnh { get; set; }
        public int SoLuongBan { get; set; }
        public decimal DoanhThu { get; set; }
    }
}