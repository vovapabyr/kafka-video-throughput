using System.Data;
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
        var currentThrouput = _analyticStore.GetAvgThroughput();
        var maxLatency = _analyticStore.GetLatency();
        return View((currentThrouput, maxLatency));
    }

    [HttpPost]
    public ActionResult DownloadThroughputCsv()
    {   
        _logger.LogInformation("Downloading throughput CSV.");
        DataTable dataTable = _analyticStore.GetThroughputAsDatatable();

        var output = dataTable.ToCsvByteArray();

        return new FileContentResult(output, "text/csv")
        {
            FileDownloadName = "Throughput.csv"
        };
    }

    [HttpPost]
    public ActionResult DownloadLatencyCsv()
    {   
        _logger.LogInformation("Downloading latency CSV.");
        DataTable dataTable = _analyticStore.GetLatencyAsDatatable();

        var output = dataTable.ToCsvByteArray();

        return new FileContentResult(output, "text/csv")
        {
            FileDownloadName = "Latency.csv"
        };
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
