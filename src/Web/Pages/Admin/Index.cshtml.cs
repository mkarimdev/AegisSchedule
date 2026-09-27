using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web.Pages.Admin;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Admin/Courses");
    }
}
