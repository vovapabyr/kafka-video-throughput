using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using video_frames_analytic.Models;

namespace video_frames_analytic.Controllers;

public class HomeController : Controller
{
    private readonly VideoFramesAnalyticInMemoryStore _analyticStore;
    private readonly ILogger<HomeController> _logger;

    public HomeController(VideoFramesAnalyticInMemoryStore analyticStore, ILogger<HomeController> logger)
    {
        _analyticStore = analyticStore;
        _logger = logger;
    }

    public IActionResult Index()
    {
        var currentThrouput = _analyticStore.GetThroughput();
        var maxLatency = _analyticStore.GetLatency();
        return View((currentThrouput, maxLatency));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
