using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using CuaHangThietBiDienTu.Models;
using EntityState = System.Data.Entity.EntityState;

namespace CuaHangThietBiDienTu.Controllers
{
    public class SanPhamsController : Controller
    {
        private QL_THIETBIDIENTUEntities db = new QL_THIETBIDIENTUEntities();

        public ActionResult Index()
        {
            var sanPhams = db.SanPham
                .Include(s => s.LoaiSanPham)
                .Include(s => s.NhaCungCap)
                .OrderByDescending(s => s.NgayCapNhat)
                .ToList();
            return View(sanPhams);
        }

        public ActionResult TheoLoaiSP()
        {
            var lstLoaiSP = db.LoaiSanPham.ToList();
            ViewBag.TongSoSanPham = db.SanPham.Count();
            return PartialView("_MenuLoaiSanPham", lstLoaiSP);
        }

        public ActionResult TheoNCC()
        {
            var lstNCC = db.NhaCungCap.ToList();
            return PartialView("_MenuNhaCungCap", lstNCC);
        }

        // GET: Tìm sản phẩm theo nhà cung cấp
        public ActionResult TimTheoNCC(int? mancc)
        {
            if (mancc == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var sanphams = db.SanPham
                .Where(x => x.MaNCC == mancc)
                .Include(s => s.LoaiSanPham)
                .Include(s => s.NhaCungCap)
                .ToList();

            ViewBag.TieuDe = $"Sản phẩm theo nhà cung cấp: {db.NhaCungCap.Find(mancc)?.TenNCC}";
            return View("SanPham", sanphams);
        }

        // GET: Tìm sản phẩm theo loại
        public ActionResult TimTheoLoaiSP(int? maLoai)
        {
            if (maLoai == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var sanPhams = db.SanPham
                .Where(s => s.MaLoai == maLoai)
                .Include(s => s.LoaiSanPham)
                .Include(s => s.NhaCungCap)
                .ToList();

            ViewBag.TieuDe = $"Sản phẩm theo loại: {db.LoaiSanPham.Find(maLoai)?.TenLoai}";
            return View("SanPham", sanPhams);
        }

        // GET: Trang chủ - hiển thị sản phẩm mới nhất
        public ActionResult TrangChu()
        {
            var spMoiNhat = db.SanPham
                .Include(s => s.LoaiSanPham)
                .Include(s => s.NhaCungCap)
                .OrderByDescending(s => s.NgayCapNhat)
                .Take(8) // Hiển thị 8 sản phẩm mới nhất
                .ToList();

            ViewBag.SanPhamGiamGia = db.SanPham
                .Where(s => s.GiamGia > 0)
                .OrderByDescending(s => s.GiamGia)
                .Take(4)
                .ToList();

            return View(spMoiNhat);
        }

        // GET: Trang danh sách sản phẩm
        public ActionResult SanPham(string searchString, int? maLoai, int? maNCC, string sortPrice)
        {
            // Lấy danh sách sản phẩm theo điều kiện lọc
            var sanPhams = db.SanPham.AsQueryable();

            // Lọc theo tìm kiếm
            if (!String.IsNullOrEmpty(searchString))
            {
                sanPhams = sanPhams.Where(s => s.TenSP.Contains(searchString));
            }

            // Lọc theo loại sản phẩm
            if (maLoai.HasValue)
            {
                sanPhams = sanPhams.Where(s => s.MaLoai == maLoai.Value);
            }

            // Lọc theo nhà cung cấp
            if (maNCC.HasValue)
            {
                sanPhams = sanPhams.Where(s => s.MaNCC == maNCC.Value);
            }

            // Sắp xếp theo giá
            if (!String.IsNullOrEmpty(sortPrice))
            {
                if (sortPrice == "asc")
                {
                    sanPhams = sanPhams.OrderBy(s => s.GiaBan);
                }
                else if (sortPrice == "desc")
                {
                    sanPhams = sanPhams.OrderByDescending(s => s.GiaBan);
                }
            }

            // Giữ lại các giá trị lọc hiện tại để hiển thị trong ViewBag
            ViewBag.CurrentMaLoai = maLoai;
            ViewBag.CurrentMaNCC = maNCC;
            ViewBag.CurrentSortPrice = sortPrice;

            // Các ViewBag khác
            ViewBag.MaLoai = new SelectList(db.LoaiSanPham, "MaLoai", "TenLoai", maLoai);
            ViewBag.MaNCC = new SelectList(db.NhaCungCap, "MaNCC", "TenNCC", maNCC);
            ViewBag.SearchString = searchString;
            ViewBag.SortPrice = sortPrice;

            return View(sanPhams.ToList());
        }

        // GET: Chi tiết sản phẩm
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            SanPham sanPham = db.SanPham
                .Include(s => s.LoaiSanPham)
                .Include(s => s.NhaCungCap)
                .Include(s => s.DanhGia)
                .Include(s => s.DanhGia.Select(d => d.NguoiDung))
                .FirstOrDefault(s => s.MaSP == id);

            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // Lấy sản phẩm cùng loại
            ViewBag.SanPhamCungLoai = db.SanPham
                .Where(s => s.MaLoai == sanPham.MaLoai && s.MaSP != id)
                .Take(4)
                .ToList();

            return View(sanPham);
        }

        // GET: Tạo sản phẩm mới (Admin)
        [Authorize(Roles = "Admin,NhanVien")]
        public ActionResult Create()
        {
            ViewBag.MaLoai = new SelectList(db.LoaiSanPham, "MaLoai", "TenLoai");
            ViewBag.MaNCC = new SelectList(db.NhaCungCap, "MaNCC", "TenNCC");
            return View();
        }

        // POST: Tạo sản phẩm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public ActionResult Create([Bind(Include = "MaSP,TenSP,GiaBan,GiamGia,MoTa,NgayCapNhat,HinhAnh,SoLuongTon,MaLoai,MaNCC")] SanPham sanPham)
        {
            if (ModelState.IsValid)
            {
                sanPham.NgayCapNhat = DateTime.Now;
                db.SanPham.Add(sanPham);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.MaLoai = new SelectList(db.LoaiSanPham, "MaLoai", "TenLoai", sanPham.MaLoai);
            ViewBag.MaNCC = new SelectList(db.NhaCungCap, "MaNCC", "TenNCC", sanPham.MaNCC);
            return View(sanPham);
        }

        // GET: Sửa sản phẩm
        [Authorize(Roles = "Admin,NhanVien")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            SanPham sanPham = db.SanPham.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }

            ViewBag.MaLoai = new SelectList(db.LoaiSanPham, "MaLoai", "TenLoai", sanPham.MaLoai);
            ViewBag.MaNCC = new SelectList(db.NhaCungCap, "MaNCC", "TenNCC", sanPham.MaNCC);
            return View(sanPham);
        }

        // POST: Sửa sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public ActionResult Edit([Bind(Include = "MaSP,TenSP,GiaBan,GiamGia,MoTa,NgayCapNhat,HinhAnh,SoLuongTon,MaLoai,MaNCC")] SanPham sanPham)
        {
            if (ModelState.IsValid)
            {
                sanPham.NgayCapNhat = DateTime.Now;
                db.Entry(sanPham).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.MaLoai = new SelectList(db.LoaiSanPham, "MaLoai", "TenLoai", sanPham.MaLoai);
            ViewBag.MaNCC = new SelectList(db.NhaCungCap, "MaNCC", "TenNCC", sanPham.MaNCC);
            return View(sanPham);
        }

        // GET: Xóa sản phẩm
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            SanPham sanPham = db.SanPham
                .Include(s => s.LoaiSanPham)
                .Include(s => s.NhaCungCap)
                .FirstOrDefault(s => s.MaSP == id);

            if (sanPham == null)
            {
                return HttpNotFound();
            }

            return View(sanPham);
        }

        // POST: Xóa sản phẩm
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            SanPham sanPham = db.SanPham.Find(id);

            // Kiểm tra xem sản phẩm có trong đơn hàng nào không
            var coTrongDonHang = db.ChiTietDonHang.Any(ct => ct.MaSP == id);
            var coTrongGioHang = db.ChiTietGioHang.Any(ct => ct.MaSP == id);

            if (coTrongDonHang || coTrongGioHang)
            {
                TempData["ErrorMessage"] = "Không thể xóa sản phẩm vì đã có trong đơn hàng hoặc giỏ hàng!";
                return RedirectToAction("Delete", new { id = id });
            }

            db.SanPham.Remove(sanPham);
            db.SaveChanges();
            TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
            return RedirectToAction("Index");
        }

        // Tìm kiếm sản phẩm (Ajax)
        public ActionResult Search(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<string>(), JsonRequestBehavior.AllowGet);
            }

            var results = db.SanPham
                .Where(s => s.TenSP.Contains(term))
                .Select(s => new {
                    id = s.MaSP,
                    text = s.TenSP,
                    price = s.GiaBan,
                    discount = s.GiamGia
                })
                .Take(10)
                .ToList();

            return Json(results, JsonRequestBehavior.AllowGet);
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