using Microsoft.AspNetCore.Mvc;

namespace AdImpactOs.Dashboard.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Overview";
        return View();
    }

    public IActionResult Error()
    {
        ViewData["Title"] = "Error";
        return View();
    }
}
