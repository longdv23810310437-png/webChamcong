using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webChamcong.Services;
using MySql.Data.MySqlClient;

namespace webChamcong.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ChamCongService _service;
        private readonly IConfiguration _configuration; // Để lấy kết nối DB lấy tên

        public string Message { get; set; }
        public string TenNhanVien { get; set; } = "Bạn"; // Mặc định là 'Bạn'

        public IndexModel(ChamCongService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }

        public void OnGet()
        {
            int? empId = HttpContext.Session.GetInt32("EmpId");
            if (empId == null)
            {
                Response.Redirect("/Login");
                return;
            }

            // Lấy tên nhân viên từ Database để hiện thị
            TenNhanVien = GetTenNhanVien(empId.Value);
        }

        // Xử lý nút CHECK IN
        public IActionResult OnPostCheckIn()
        {
            int? empId = HttpContext.Session.GetInt32("EmpId");
            if (empId == null) return RedirectToPage("/Login");

            // Gọi hàm chấm công với cờ isCheckOut = false
            Message = _service.CheckInOrOut(empId.Value, isCheckOut: false);
            TenNhanVien = GetTenNhanVien(empId.Value); // Lấy lại tên để hiện
            return Page();
        }

        // Xử lý nút CHECK OUT
        public IActionResult OnPostCheckOut()
        {
            int? empId = HttpContext.Session.GetInt32("EmpId");
            if (empId == null) return RedirectToPage("/Login");

            // Gọi hàm chấm công với cờ isCheckOut = true
            Message = _service.CheckInOrOut(empId.Value, isCheckOut: true);
            TenNhanVien = GetTenNhanVien(empId.Value);
            return Page();
        }

        // Hàm phụ: Lấy tên nhân viên từ ID
        private string GetTenNhanVien(int id)
        {
            try
            {
                using (var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();
                    string sql = "SELECT ho_ten FROM nhan_vien WHERE id = @Id";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        var result = cmd.ExecuteScalar();
                        return result != null ? result.ToString() : "Bạn";
                    }
                }
            }
            catch
            {
                return "Bạn";
            }
        }
    }
}