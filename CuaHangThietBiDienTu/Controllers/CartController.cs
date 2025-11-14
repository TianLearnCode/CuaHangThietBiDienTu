using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CuaHangThietBiDienTu.Models;

namespace CuaHangThietBiDienTu.Controllers
{
    public class CartController : Controller
    {
        private QL_THIETBIDIENTUEntities db = new QL_THIETBIDIENTUEntities();

        // GET: Cart
        public ActionResult Index()
        {
            var cart = GetCurrentCart();
            return View(cart);
        }

        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            var product = db.SanPham.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            if (product.SoLuongTon < quantity)
            {
                return Json(new { success = false, message = "Số lượng tồn kho không đủ" });
            }

            var cart = GetCurrentCart();
            cart.AddItem(product, quantity);
            SaveCart(cart);

            return Json(new
            {
                success = true,
                message = "Đã thêm vào giỏ hàng",
                cartCount = cart.Items.Sum(x => x.Quantity)
            });
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCurrentCart();
            var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                var product = db.SanPham.Find(productId);
                if (product != null && product.SoLuongTon < quantity)
                {
                    return Json(new { success = false, message = "Số lượng tồn kho không đủ" });
                }

                if (quantity <= 0)
                {
                    cart.RemoveItem(productId);
                }
                else
                {
                    item.Quantity = quantity;
                }

                SaveCart(cart);

                return Json(new
                {
                    success = true,
                    itemTotal = item.Total.ToString("N0"),
                    totalAmount = cart.GetTotalAmount().ToString("N0"),
                    cartCount = cart.Items.Sum(x => x.Quantity)
                });
            }

            return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng" });
        }

        [HttpPost]
        public ActionResult RemoveFromCart(int productId)
        {
            var cart = GetCurrentCart();
            cart.RemoveItem(productId);
            SaveCart(cart);

            return Json(new
            {
                success = true,
                totalAmount = cart.GetTotalAmount().ToString("N0"),
                cartCount = cart.Items.Sum(x => x.Quantity)
            });
        }



        public ActionResult GetCartCount()
        {
            var cart = GetCurrentCart();
            return Content(cart.Items.Sum(x => x.Quantity).ToString());
        }

        // Thanh toán
        public ActionResult Checkout()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                Session["ReturnUrl"] = Url.Action("Checkout", "Cart");
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để thanh toán";
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCurrentCart();
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống";
                return RedirectToAction("Index");
            }

            // Kiểm tra số lượng tồn kho
            foreach (var item in cart.Items)
            {
                var product = db.SanPham.Find(item.ProductId);
                if (product == null || product.SoLuongTon < item.Quantity)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm {item.ProductName} không đủ số lượng tồn kho";
                    return RedirectToAction("Index");
                }
            }

            // Lấy thông tin người dùng
            int maNguoiDung = (int)Session["UserID"];
            var user = db.NguoiDung.Find(maNguoiDung);

            var viewModel = new CheckoutViewModel
            {
                Cart = cart,
                FullName = user?.HoTen,
                Email = Session["Email"]?.ToString(),
                Phone = user?.DienThoai,
                Address = user?.DiaChi
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutViewModel model)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                model.Cart = GetCurrentCart();
                return View(model);
            }

            try
            {
                var cart = GetCurrentCart();
                if (!cart.Items.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống";
                    return RedirectToAction("Index");
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Tạo đơn hàng
                        var order = new DonHang
                        {
                            MaKH = (int)Session["UserID"],
                            NgayDat = DateTime.Now,
                            TongTien = cart.GetTotalAmount(),
                            TinhTrangGiaoHang = "Đang xử lý"
                        };

                        db.DonHang.Add(order);
                        db.SaveChanges();

                        // Thêm thông tin đặt hàng
                        var thongTinDatHang = new ThongTinDatHang
                        {
                            MaDonHang = order.MaDonHang,
                            TenNguoiNhan = model.FullName,
                            DiaChiNhanHang = model.Address,
                            SoDienThoai = model.Phone,
                            GhiChu = model.Notes
                        };

                        db.ThongTinDatHang.Add(thongTinDatHang);

                        // Thêm chi tiết đơn hàng và cập nhật tồn kho
                        foreach (var item in cart.Items)
                        {
                            var orderDetail = new ChiTietDonHang
                            {
                                MaDonHang = order.MaDonHang,
                                MaSP = item.ProductId,
                                SoLuong = item.Quantity,
                                DonGia = item.ActualPrice
                            };

                            // Cập nhật số lượng tồn kho
                            var product = db.SanPham.Find(item.ProductId);
                            if (product != null)
                            {
                                product.SoLuongTon -= item.Quantity;
                            }

                            db.ChiTietDonHang.Add(orderDetail);
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        // Xóa giỏ hàng
                        ClearCart();

                        TempData["SuccessMessage"] = "Đặt hàng thành công! Mã đơn hàng: " + order.MaDonHang;
                        return RedirectToAction("OrderConfirmation", new { id = order.MaDonHang });
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi đặt hàng: " + ex.Message);
                model.Cart = GetCurrentCart();
                return View(model);
            }
        }

        public ActionResult OrderConfirmation(int id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.DonHang.Find(id);
            if (order == null || order.MaKH != (int)Session["UserID"])
            {
                return HttpNotFound();
            }

            // Lấy thông tin đặt hàng
            var thongTinDatHang = db.ThongTinDatHang.FirstOrDefault(t => t.MaDonHang == id);
            ViewBag.ThongTinDatHang = thongTinDatHang;

            return View(order);
        }

        // Helper methods
        private Cart GetCurrentCart()
        {
            var cart = Session["Cart"] as Cart;
            if (cart == null)
            {
                cart = new Cart();
                Session["Cart"] = cart;
            }
            return cart;
        }

        private void SaveCart(Cart cart)
        {
            Session["Cart"] = cart;
            Session["CartCount"] = cart.Items.Sum(x => x.Quantity);
        }

        private void ClearCart()
        {
            Session["Cart"] = new Cart();
            Session["CartCount"] = 0;
        }
    }
}