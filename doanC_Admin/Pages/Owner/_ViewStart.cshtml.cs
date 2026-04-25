using doanC_Admin.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace doanC_Admin.Pages.Owner
{
    [doanC_Admin.Helpers.Authorize("Owner", "Manager")]
    public class _ViewStartModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}

