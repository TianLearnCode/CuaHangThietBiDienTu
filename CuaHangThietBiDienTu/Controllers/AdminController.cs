using CuaHangThietBiDienTu.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CuaHangThietBiDienTu.Controllers
{
    public class AdminController : Controller
    {
        private QL_THIETBIDIENTUEntities db = new QL_THIETBIDIENTUEntities();

        [HttpGet]
        public ActionResult AdminPage()
        {
            // Kiểm tra quyền admin
            if (Session["Role"] == null || Convert.ToInt32(Session["Role"]) != 1)
            {
                return RedirectToAction("Login", "Account");
            }

            // Tạo ViewModel
            var viewModel = new AdminDashboardViewModel
            {
                TongKhachHang = db.NguoiDung.Count(x => x.MaVaiTro == 3),
                TongDoanhThu = db.DonHang
                    .Where(d => d.TinhTrangGiaoHang != "Đã hủy")
                    .Sum(d => (decimal?)d.TongTien) ?? 0,
                TongSanPham = db.SanPham.Count(),
                TongDonHang = db.DonHang.Count(),
                CurrentMonth = DateTime.Now.Month,
                CurrentYear = DateTime.Now.Year,
                DoanhThuThang = db.DonHang
                    .Where(d => d.NgayDat.Month == DateTime.Now.Month && // Sửa: bỏ .Value
                               d.NgayDat.Year == DateTime.Now.Year &&
                               d.TinhTrangGiaoHang != "Đã hủy")
                    .Sum(d => (decimal?)d.TongTien) ?? 0,
                DonHangThang = db.DonHang
                    .Count(d => d.NgayDat.Month == DateTime.Now.Month && // Sửa: bỏ .Value
                               d.NgayDat.Year == DateTime.Now.Year),
                SanPhamBanChay = (from ct in db.ChiTietDonHang
                                  join sp in db.SanPham on ct.MaSP equals sp.MaSP
                                  group ct by new { sp.MaSP, sp.TenSP, sp.HinhAnh } into g
                                  select new SanPhamBanChayViewModel
                                  {
                                      MaSP = g.Key.MaSP,
                                      TenSP = g.Key.TenSP ?? "Không có tên",
                                      HinhAnh = g.Key.HinhAnh ?? "default.jpg",
                                      SoLuongBan = g.Sum(ct => ct.SoLuong ?? 0),
                                      DoanhThu = g.Sum(ct => (ct.SoLuong ?? 0) * (ct.DonGia ?? 0))
                                  })
                                  .OrderByDescending(x => x.SoLuongBan)
                                  .Take(5)
                                  .ToList(),
                DonHangMoiNhat = db.DonHang
                    .Include("NguoiDung")
                    .OrderByDescending(d => d.NgayDat)
                    .Take(10)
                    .ToList()
            };

            return View(viewModel);
        }

        // API để lấy dữ liệu biểu đồ
        [HttpGet]
        public JsonResult GetChartData()
        {
            // Tính ngày bắt đầu (6 tháng trước)
            var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-5);

            // Lấy tất cả đơn hàng trong 6 tháng gần nhất
            var allOrders = db.DonHang
                .Where(d => d.NgayDat >= startDate &&
                           d.TinhTrangGiaoHang != "Đã hủy")
                .Select(d => new { d.NgayDat, d.TongTien })
                .ToList();

            // Nhóm dữ liệu trong memory
            var doanhThuData = allOrders
                .GroupBy(d => new {
                    Year = d.NgayDat.Year, // Sửa: bỏ .Value
                    Month = d.NgayDat.Month
                })
                .Select(g => new
                {
                    Thang = $"{g.Key.Month}/{g.Key.Year}",
                    DoanhThu = g.Sum(d => d.TongTien)
                })
                .OrderBy(x => x.Thang)
                .ToList();

            // Đảm bảo có đủ 6 tháng (thêm tháng không có doanh thu)
            var result = new List<object>();
            for (int i = 0; i < 6; i++)
            {
                var date = startDate.AddMonths(i);
                var thangKey = $"{date.Month}/{date.Year}";
                var thangData = doanhThuData.FirstOrDefault(d => d.Thang == thangKey);

                result.Add(new
                {
                    Thang = thangKey,
                    DoanhThu = thangData?.DoanhThu ?? 0
                });
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult GetQuickStats()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

       
                var allDonHang = db.DonHang.ToList();
                var donHangInRange = db.DonHang
                    .Where(d => d.NgayDat >= today && d.NgayDat < tomorrow)
                    .ToList();

                var khachHangMoi = db.NguoiDung
                    .Where(x => x.MaVaiTro == 3 && x.NgayTao.HasValue)
                    .AsEnumerable()
                    .Where(x => x.NgayTao.Value.Date == today)
                    .ToList();

                Console.WriteLine($"=== DEBUG QUICK STATS ===");
                Console.WriteLine($"Today: {today:yyyy-MM-dd}");
                Console.WriteLine($"Tomorrow: {tomorrow:yyyy-MM-dd}");
                Console.WriteLine($"Total orders in DB: {allDonHang.Count}");
                Console.WriteLine($"Orders in date range: {donHangInRange.Count}");

                foreach (var dh in donHangInRange)
                {
                    Console.WriteLine($"Order {dh.MaDonHang}: {dh.NgayDat:yyyy-MM-dd HH:mm:ss} - {dh.TongTien} - {dh.TinhTrangGiaoHang}");
                }

                Console.WriteLine($"New customers today: {khachHangMoi.Count}");

                var stats = new
                {
                    DoanhThuHomNay = donHangInRange
                        .Where(d => d.TinhTrangGiaoHang != "Đã hủy")
                        .Sum(d => d.TongTien),

                    DonHangHomNay = donHangInRange.Count,

                    KhachHangMoi = khachHangMoi.Count,


                    DebugInfo = new
                    {
                        Today = today.ToString("yyyy-MM-dd"),
                        TotalOrdersInDb = allDonHang.Count,
                        OrdersInRange = donHangInRange.Count,
                        NewCustomersCount = khachHangMoi.Count,
                        OrdersDetails = donHangInRange.Select(d => new
                        {
                            Id = d.MaDonHang,
                            Date = d.NgayDat.ToString("yyyy-MM-dd HH:mm:ss"),
                            Total = d.TongTien,
                            Status = d.TinhTrangGiaoHang
                        }).ToList()
                    }
                };

                return Json(stats, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                }, JsonRequestBehavior.AllowGet);
            }
        }

 
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Details(int id)
        {
            return View();
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public ActionResult Edit(int id)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public ActionResult Delete(int id)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


    }
}