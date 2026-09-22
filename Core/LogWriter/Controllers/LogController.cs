namespace UBIS.Services.LogWriter.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Services.LogWriter.Domain;
using UBIS.Services.LogWriter.Persistence;

[ApiController]
[Route("api/logs")]
public class LogController : ControllerBase
{
    private readonly LogDbContext _db;

    public LogController(LogDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] LogEntry entry)
    {
        if (entry == null) return BadRequest();
        entry.Id = 0;
        entry.CreatedAt = DateTime.UtcNow;
        _db.Logs.Add(entry);
        await _db.SaveChangesAsync();
        var level = ParseLevel(entry.Level);
        var exObj = string.IsNullOrWhiteSpace(entry.Exception) ? null : new Exception(entry.Exception);
        Serilog.Log.ForContext("Source", "Api").Write(level, exObj, "{Message} {Properties}", entry.Message, entry.Properties);
        return Accepted(entry);
    }

    private static Serilog.Events.LogEventLevel ParseLevel(string level)
    {
        if (Enum.TryParse<Serilog.Events.LogEventLevel>(level, true, out var lv)) return lv;
        return Serilog.Events.LogEventLevel.Information;
    }
}
