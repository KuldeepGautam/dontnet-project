namespace UBIS.Services.Ecl.Infrastructure.Services;

/// <summary>Binds to "Storage:EclDocumentsPath". Analogous in spirit to AIM's Data Protection keys being persisted outside wwwroot — a config-driven path outside the web root, created if missing.</summary>
public class EclDocumentStorageOptions
{
    public string EclDocumentsPath { get; set; } = @"C:\UBIS2-Uploads\ECL\";
}
