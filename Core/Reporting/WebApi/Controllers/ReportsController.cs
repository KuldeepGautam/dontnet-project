namespace UBIS.Services.Reporting.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.Reporting.Application.DTOs;
using UBIS.Services.Reporting.Domain.Entities;
using UBIS.Services.Reporting.Infrastructure.Services;

/// <summary>
/// Generic export surface for any consumer page in the solution: POST a TabularReportDto (title +
/// columns + grouped rows), get a PDF or Excel file back. No knowledge of ECL, PreBudget, or any
/// other domain — a new consumer page needs zero changes here, only its own aggregation code that
/// maps its data into this shape.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly PdfReportGenerator _pdfGenerator;
    private readonly ExcelReportGenerator _excelGenerator;
    private readonly CsvReportGenerator _csvGenerator;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(PdfReportGenerator pdfGenerator, ExcelReportGenerator excelGenerator, CsvReportGenerator csvGenerator, ILogger<ReportsController> logger)
    {
        _pdfGenerator = pdfGenerator;
        _excelGenerator = excelGenerator;
        _csvGenerator = csvGenerator;
        _logger = logger;
    }

    [HttpPost("pdf")]
    public IActionResult Pdf([FromBody] TabularReportDto request) => Export(request, _pdfGenerator);

    [HttpPost("excel")]
    public IActionResult Excel([FromBody] TabularReportDto request) => Export(request, _excelGenerator);

    [HttpPost("csv")]
    public IActionResult Csv([FromBody] TabularReportDto request) => Export(request, _csvGenerator);

    private IActionResult Export<TGenerator>(TabularReportDto request, TGenerator generator)
        where TGenerator : Application.Interfaces.IReportGenerator
    {
        var report = new TabularReport
        {
            Title = request.Title,
            TitleBoxLines = request.TitleBoxLines,
            Subtitle = request.Subtitle,
            UnitNote = request.UnitNote,
            ParaNo = request.ParaNo,
            Remarks = request.Remarks,
            ColumnGroups = request.ColumnGroups?.Select(g => new ReportColumnGroup { Label = g.Label, ColumnSpan = g.ColumnSpan }).ToList(),
            Columns = request.Columns,
            RightAlignedColumns = request.RightAlignedColumns,
            CenterAlignedColumns = request.CenterAlignedColumns,
            Groups = request.Groups.Select(g => new ReportGroup
            {
                Label = g.Label,
                Rows = g.Rows,
                Totals = g.Totals.Select(t => new ReportTotalRow { Cells = t.Cells }).ToList()
            }).ToList(),
            Sections = request.Sections?.Select(s => new ReportSection
            {
                Title = s.Title,
                ParaNo = s.ParaNo,
                UnitNote = s.UnitNote,
                ColumnGroups = s.ColumnGroups?.Select(g => new ReportColumnGroup { Label = g.Label, ColumnSpan = g.ColumnSpan }).ToList(),
                Columns = s.Columns,
                RightAlignedColumns = s.RightAlignedColumns,
                CenterAlignedColumns = s.CenterAlignedColumns,
                Groups = s.Groups.Select(g => new ReportGroup
                {
                    Label = g.Label,
                    Rows = g.Rows,
                    Totals = g.Totals.Select(t => new ReportTotalRow { Cells = t.Cells }).ToList()
                }).ToList()
            }).ToList()
        };

        try
        {
            report.EnsureValid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Result<string>.Failure(Error.ValidationError("report", ex.Message)));
        }

        byte[] bytes;
        try
        {
            bytes = generator.Generate(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate {ContentType} export for report '{Title}'.", generator.ContentType, request.Title);
            return StatusCode(500, Result<string>.Failure(Error.InternalError("Could not generate the export. Please try again.")));
        }

        var fileName = $"{request.FileName}.{generator.FileExtension}";
        return File(bytes, generator.ContentType, fileName);
    }
}
