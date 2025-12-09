using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webChamcong.Services;

namespace webChamcong.Pages
{
    public class LoginModel : PageModel
    {
        private readonly    ChamCongService _chamCongService;
        public string Message { get; set; }
        public LoginModel(ChamCongService chamCongService)
        {
            _chamCongService = chamCongService;
        }
        public void OnGet()
        {
           
        }
        public IActionResult OnPost(string phone, string password)
        {
            int employeeId = _chamCongService.Login(phone, password);
            if (employeeId != -1)
            {
                HttpContext.Session.SetInt32("EmpId", employeeId);
                // Đăng nhập thành công
                Message = "Đăng nhập thành công! ID Nhân viên: " + employeeId;
                // Chuyển hướng hoặc lưu session tùy theo yêu cầu
                return RedirectToPage("/Index");
            }
            else
            {
                // Đăng nhập thất bại
                Message = "Sai số điện thoại hoặc mật khẩu!";
                return Page();
            }

        }
    }
}
