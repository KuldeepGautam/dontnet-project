namespace UBIS.Services.Reporting.Application.Interfaces;

using UBIS.Services.Reporting.Domain.Entities;

/// <summary>One implementation per output format (PDF/Excel today; a future CSV export would add a third without touching the other two or the controller).</summary>
public interface IReportGenerator
{
    byte[] Generate(TabularReport report);

    string ContentType { get; }

    string FileExtension { get; }
}
