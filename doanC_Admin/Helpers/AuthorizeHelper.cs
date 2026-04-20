using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace doanC_Admin.Helpers
{
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;
        private readonly bool _requireLoginOnly;

        public AuthorizeAttribute(params string[] roles)
        {
            _allowedRoles = roles;
            _requireLoginOnly = roles == null || roles.Length == 0;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;
            var currentPath = httpContext.Request.Path.Value ?? "";

            // Danh sách đường dẫn công khai (bỏ qua kiểm tra)
            var publicPaths = new[] { "/Login", "/Index", "/Error", "/AccessDenied", "/css", "/js", "/lib", "/favicon", "/swagger" };

            if (publicPaths.Any(p => currentPath.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var isLoggedIn = !string.IsNullOrEmpty(httpContext.Session.GetString("AdminId"));
            var userRole = httpContext.Session.GetString("Role") ?? "";

            // Chưa đăng nhập
            if (!isLoggedIn)
            {
                context.Result = new RedirectToPageResult("/Login");
                return;
            }

            // Chỉ cần đăng nhập, không yêu cầu role cụ thể
            if (_requireLoginOnly)
            {
                return;
            }

            // Kiểm tra role nếu có yêu cầu
            if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
            {
                // Redirect về trang phù hợp với role
                if (userRole == "Owner" || userRole == "Manager")
                {
                    context.Result = new RedirectToPageResult("/Owner/Dashboard");
                }
                else
                {
                    context.Result = new RedirectToPageResult("/Dashboard");
                }
                return;
            }
        }
    }
}