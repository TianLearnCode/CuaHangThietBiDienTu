using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CuaHangThietBiDienTu.Models
{
    public class User
    {
        public int MaNguoiDung { get; set; }
        public int MaVaiTro { get; set; }
        public string HoTen { get; set; }
        public DateTime NgaySinh { get; set; }
        public string GioiTinh { get; set; }
        public string DienThoai { get; set; }
        public string Email { get; set; }
        public string MatKhau { get; set; }
        public string DiaChi { get; set; }

        public string AVT { get; set; }
        public string VaiTro { get; set; }
    }
}