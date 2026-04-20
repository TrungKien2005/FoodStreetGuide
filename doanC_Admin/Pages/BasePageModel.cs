using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace doanC_Admin.Pages
{
    public class BasePageModel : PageModel
    {
        public bool IsLoggedIn => !string.IsNullOrEmpty(HttpContext.Session.GetString("AdminId"));
        public string? UserRole => HttpContext.Session.GetString("Role");
        public string? Username => HttpContext.Session.GetString("Username");

        public IActionResult CheckLogin()
        {
            if (!IsLoggedIn)
            {
                return RedirectToPage("/Login");
            }
            return null;
        }

        public IActionResult CheckRole(params string[] allowedRoles)
        {
            if (!IsLoggedIn)
                return RedirectToPage("/Login");

            if (allowedRoles.Length > 0 && !allowedRoles.Contains(UserRole))
            {
                if (UserRole == "Owner")
                    return RedirectToPage("/Owner/Dashboard");
                return RedirectToPage("/Dashboard");
            }
            return null;
        }
    }
}