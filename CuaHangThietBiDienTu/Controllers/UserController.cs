using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using CuaHangThietBiDienTu.Models;

namespace CuaHangThietBiDienTu.Controllers
{
    public class UserController : Controller
    {
        private QL_WEB_THIETBIDIENTUEntities db = new QL_WEB_THIETBIDIENTUEntities();

        // GET: User


        public ActionResult DSKhachHang()
        {
            var nguoiDungs = (from nd in db.NguoiDungs
                              join tk in db.TaiKhoans on nd.MaNguoiDung equals tk.MaNguoiDung
                              where nd.MaVaiTro == 3
                              select new User
                              {
                                  MaNguoiDung = nd.MaNguoiDung,
                                  MaVaiTro = nd.MaVaiTro,
                                  HoTen = nd.HoTen,
                                  NgaySinh = nd.NgaySinh ?? DateTime.Now,
                                  GioiTinh = nd.GioiTinh,
                                  DienThoai = nd.DienThoai,
                                  Email = tk.Email,
                                  MatKhau = tk.MatKhau,
                                  DiaChi = nd.DiaChi,
                                  AVT = nd.AVT,
                                  VaiTro = "Khách hàng"
                              }).ToList();


            return View(nguoiDungs);
        }
        public ActionResult DSNhanVien()
        {
            var nguoiDungs = (from nd in db.NguoiDungs
                              join tk in db.TaiKhoans on nd.MaNguoiDung equals tk.MaNguoiDung
                              where nd.MaVaiTro == 2
                              select new User
                              {
                                  MaNguoiDung = nd.MaNguoiDung,
                                  MaVaiTro = nd.MaVaiTro,
                                  HoTen = nd.HoTen,
                                  NgaySinh = nd.NgaySinh ?? DateTime.Now,
                                  GioiTinh = nd.GioiTinh,
                                  DienThoai = nd.DienThoai,
                                  Email = tk.Email,
                                  MatKhau = tk.MatKhau,
                                  DiaChi = nd.DiaChi,
                                  AVT = nd.AVT,
                                  VaiTro = "Nhân viên"
                              }).ToList();
            return View(nguoiDungs);
        }
        public ActionResult Index()
        {
            var nguoiDungs = db.NguoiDungs.Include(n => n.VaiTro);
            return View(nguoiDungs.ToList());
        }

        // GET: User/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NguoiDung nguoiDung = db.NguoiDungs.Find(id);
            if (nguoiDung == null)
            {
                return HttpNotFound();
            }
            return View(nguoiDung);
        }

        // GET: User/Create
        public ActionResult Create()
        {
            ViewBag.MaVaiTro = new SelectList(db.VaiTroes, "MaVaiTro", "TenVaiTro");
            return View();
        }

        // POST: User/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create([Bind(Include = "MaNguoiDung,HoTen,NgaySinh,GioiTinh,DienThoai,DiaChi,MaVaiTro,Email,MatKhau")] NguoiDung nguoiDung, HttpPostedFileBase AnhDaiDien)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        if(AnhDaiDien!=null && AnhDaiDien.ContentLength > 0)
        //        {
        //            var filename = Path.GetFileName(AnhDaiDien.FileName);
        //            var path = Path.Combine(Server.MapPath("~/Content/Images/UserAVT"), filename);

        //            AnhDaiDien.SaveAs(path);
        //            nguoiDung.AVT = filename;
        //        }
        //        else
        //        {
        //            nguoiDung.AVT = "defaultAVT.jpg";
        //        }
        //            db.NguoiDungs.Add(nguoiDung);

        //        db.SaveChanges();
        //        if (nguoiDung.MaVaiTro == 2)
        //        {
        //            return RedirectToAction("DSNhanVien");
        //        }
        //        else if (nguoiDung.MaVaiTro == 3) {
        //            return RedirectToAction("DSKhachHang");

        //        }
        //        else
        //        {
        //            return RedirectToAction("AdminPage", "Admin");

        //        }
        //    }

        //    ViewBag.MaVaiTro = new SelectList(db.VaiTroes, "MaVaiTro", "TenVaiTro", nguoiDung.MaVaiTro);
        //    return View(nguoiDung);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(User model, HttpPostedFileBase AnhDaiDien)
        {
            if (ModelState.IsValid)
            {
                var nguoiDung = new NguoiDung
                {
                    HoTen = model.HoTen,
                    NgaySinh = model.NgaySinh,
                    GioiTinh = model.GioiTinh,
                    DienThoai = model.DienThoai,
                    DiaChi = model.DiaChi,
                    MaVaiTro = model.MaVaiTro
                };

                if (AnhDaiDien != null && AnhDaiDien.ContentLength > 0)
                {
                    var filename = Path.GetFileName(AnhDaiDien.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/Images/UserAVT"), filename);
                    AnhDaiDien.SaveAs(path);

                    nguoiDung.AVT = filename;
                }
                else
                {
                    nguoiDung.AVT = "defaultAVT.jpg";
                }

                db.NguoiDungs.Add(nguoiDung);
                db.SaveChanges();   

                var taiKhoan = new TaiKhoan
                {
                    Email = model.Email,
                    MatKhau = model.MatKhau,  
                    MaNguoiDung = nguoiDung.MaNguoiDung
                };

                db.TaiKhoans.Add(taiKhoan);
                db.SaveChanges();

                // 3. Điều hướng theo vai trò
                if (model.MaVaiTro == 2)
                {
                    return RedirectToAction("DSNhanVien");
                }
                else if (model.MaVaiTro == 3)
                {
                    return RedirectToAction("DSKhachHang");
                }
                else
                {
                    return RedirectToAction("AdminPage", "Admin");
                }
            }

            ViewBag.MaVaiTro = new SelectList(db.VaiTroes, "MaVaiTro", "TenVaiTro", model.MaVaiTro);
            return View(model);
        }


        // GET: User/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NguoiDung nguoiDung = db.NguoiDungs.Find(id);
            if (nguoiDung == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaVaiTro = new SelectList(db.VaiTroes, "MaVaiTro", "TenVaiTro", nguoiDung.MaVaiTro);
            return View(nguoiDung);
        }

        // POST: User/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaNguoiDung,HoTen,NgaySinh,GioiTinh,DienThoai,DiaChi,MaVaiTro,AVT")] NguoiDung nguoiDung)
        {
            if (ModelState.IsValid)
            {
                db.Entry(nguoiDung).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                if(nguoiDung.MaVaiTro == 2)
                {
                    return RedirectToAction("DSNhanVien");
                }
                else if(nguoiDung.MaVaiTro == 3)
                {
                    return RedirectToAction("DSKhachHang");
                }
            }
            ViewBag.MaVaiTro = new SelectList(db.VaiTroes, "MaVaiTro", "TenVaiTro", nguoiDung.MaVaiTro);
            return View(nguoiDung);
        }

        // GET: User/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NguoiDung nguoiDung = db.NguoiDungs.Find(id);
            if (nguoiDung == null)
            {
                return HttpNotFound();
            }
            return View(nguoiDung);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            NguoiDung nguoiDung = db.NguoiDungs.Find(id);
            db.NguoiDungs.Remove(nguoiDung);
            db.SaveChanges();
            return RedirectToAction("Index");
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
