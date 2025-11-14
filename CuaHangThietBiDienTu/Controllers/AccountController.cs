using CuaHangThietBiDienTu.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CuaHangThietBiDienTu.Controllers
{
    public class AccountController : Controller
    {
        private QL_THIETBIDIENTUEntities db = new QL_THIETBIDIENTUEntities();

        [HttpGet]
        public ActionResult EditProfile()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            int maNguoiDung = (int)Session["UserID"];
            var nguoiDung = db.NguoiDung.FirstOrDefault(x => x.MaNguoiDung == maNguoiDung);

            if (nguoiDung == null)
                return HttpNotFound();

            return View(nguoiDung);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(NguoiDung model, HttpPostedFileBase AvatarFile)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            int maNguoiDung = (int)Session["UserID"];
            var existingUser = db.NguoiDung.Find(maNguoiDung);

            if (existingUser == null)
                return HttpNotFound();

            if (ModelState.IsValid)
            {
                existingUser.HoTen = model.HoTen;
                existingUser.NgaySinh = model.NgaySinh;
                existingUser.GioiTinh = model.GioiTinh;
                existingUser.DienThoai = model.DienThoai;
                existingUser.DiaChi = model.DiaChi;

                if (AvatarFile != null && AvatarFile.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(AvatarFile.FileName);
                    string extension = Path.GetExtension(AvatarFile.FileName);
                    fileName = fileName + "_" + DateTime.Now.Ticks + extension;

                    string path = Path.Combine(Server.MapPath("~/Content/Images/UserAVT/"), fileName);
                    AvatarFile.SaveAs(path);

                    existingUser.AVT = fileName;
                }

                db.SaveChanges();
                ViewBag.Message = "Cập nhật thông tin thành công!";
                return View(existingUser);
            }

            return View(model);
        }
        
        public ActionResult ForgotPassword()
        {

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            
            var user = db.TaiKhoan.FirstOrDefault(n => n.Email == email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Email không tồn tại trong hệ thống.";
                return View();
            }

            Session["ResetEmail"] = email;
            return RedirectToAction("ResetPassword");
        }

        public ActionResult ResetPassword()
        {
            if (Session["ResetEmail"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string newPassword, string confirmPassword)
        {
            if (Session["ResetEmail"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }

            string email = Session["ResetEmail"].ToString();

            if (newPassword != confirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu xác nhận không khớp.";
                return View();
            }

           var user = db.TaiKhoan.FirstOrDefault(tk => tk.Email == email);

            if (user != null)
            {
                user.MatKhau = newPassword;
                
                db.SaveChanges();

                Session["ResetEmail"] = null;
                ViewBag.SuccessMessage = "Mật khẩu đã được đổi thành công! Bạn có thể đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }
            else
            {
                ViewBag.ErrorMessage = "Không tìm thấy tài khoản.";
            }

            return View();
        }


        //Trang cá nhân khách hàng
        public ActionResult Profile()
        {
            // Kiểm tra đăng nhập

            if (Session["Email"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            string email = Session["Email"].ToString();
            var user = (from tk in db.TaiKhoan
                      join nd in db.NguoiDung on tk.MaNguoiDung equals nd.MaNguoiDung
                      join vt in db.VaiTro on nd.MaVaiTro equals vt.MaVaiTro
                      where tk.Email == email
                      select new ProfileUserModel
                      {
                          HoTen = nd.HoTen,
                          Email = tk.Email,
                          DienThoai = nd.DienThoai,
                          DiaChi = nd.DiaChi,
                          GioiTinh = nd.GioiTinh,
                          NgaySinh = nd.NgaySinh,
                          AVT = nd.AVT,
                          TenVaiTro = vt.TenVaiTro
                      }).FirstOrDefault();


                      

            if (user == null)
            {
                return HttpNotFound("Không tìm thấy thông tin người dùng");
            }

            return View(user);
        }

        public ActionResult Login()
        {
            return View();
        }
        // POST: Account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            if(string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ email và mật khẩu.";
                return View();
            }
            var user = (from tk in db.TaiKhoan
                        join nd in db.NguoiDung on tk.MaNguoiDung equals nd.MaNguoiDung
                        where tk.Email == email && tk.MatKhau == password
                        select new
                        {
                            nd.MaNguoiDung,
                            nd.HoTen,
                            tk.Email,
                            tk.MatKhau,
                            nd.MaVaiTro
                        }).FirstOrDefault();
            if (user == null)
            {
                ViewBag.ErrorMessage1 = "Email hoặc mật khẩu không đúng.";
                return View();

            }
            Session["UserID"] = user.MaNguoiDung;
            Session["UserName"] = user.HoTen;
            Session["Email"] = user.Email;
            Session["Password"] = user.MatKhau;
            Session["Role"] = user.MaVaiTro;
            switch (user.MaVaiTro)
            {
                case 1:
                    return RedirectToAction("AdminPage", "Admin");
                case 2:
                    return RedirectToAction("NhanVienPage", "NhanVien");
                default:
                    return RedirectToAction("TrangChu", "SanPhams");
                
            }
            
        }

        //register
        public ActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Register(TaiKhoan kh)
        {
            if (ModelState.IsValid)
            {
                var existingEmail = db.TaiKhoan.FirstOrDefault(x => x.Email == kh.Email);
                if (existingEmail != null)
                {
                    ViewBag.ErrorMessage = "Email này đã được sử dụng. Vui lòng dùng email khác!";
                    return View(kh);
                }

                db.TaiKhoan.Add(kh);
                db.SaveChanges();

                return RedirectToAction("Login");
            }
            return View(kh);

           

        }
        //Logout
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
        

        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        // GET: Account/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Account/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Account/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Account/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Account/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Account/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Account/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
