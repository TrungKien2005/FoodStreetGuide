using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace doanC_Admin.Filters
{
    public class SessionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var path = context.HttpContext.Request.Path.Value ?? "";

            // ✅ THÊM "/api" VÀO DANH SÁCH CÔNG KHAI
            var publicPaths = new[] { "/Login", "/Index", "/Error", "/AccessDenied", "/css", "/js", "/lib", "/favicon", "/swagger", "/api" };

            if (publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var adminId = context.HttpContext.Session.GetString("AdminId");

            if (string.IsNullOrEmpty(adminId))
            {
                context.Result = new RedirectResult("/Login");
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}