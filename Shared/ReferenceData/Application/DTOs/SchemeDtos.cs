namespace UBIS.Services.ReferenceData.Application.DTOs;

public class SchemeDto
{
    public int SchemeId { get; set; }
    public int DemandId { get; set; }
    public string SchemeName { get; set; } = string.Empty;
    public string? HSchemeName { get; set; }
    public bool IsUmbrella { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSchemeRequest
{
    public int DemandId { get; set; }
    public string SchemeName { get; set; } = string.Empty;
    public string? HSchemeName { get; set; }
    public bool IsUmbrella { get; set; }
}

public class SubSchemeDto
{
    public int SubSchemeId { get; set; }
    public int SchemeId { get; set; }
    public string SubSchemeName { get; set; } = string.Empty;
    public string? HSubSchemeName { get; set; }
    public string? SubSchemeCode { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSubSchemeRequest
{
    public int SchemeId { get; set; }
    public string SubSchemeName { get; set; } = string.Empty;
    public string? HSubSchemeName { get; set; }
    public string? SubSchemeCode { get; set; }
}

public class MajorHeadDto
{
    public int MajorHeadId { get; set; }
    public string MajorHeadCode { get; set; } = string.Empty;
    public string MajorHeadName { get; set; } = string.Empty;
    public string? HMajorHeadName { get; set; }
    public bool IsActive { get; set; }
}

public class CreateMajorHeadRequest
{
    public string MajorHeadCode { get; set; } = string.Empty;
    public string MajorHeadName { get; set; } = string.Empty;
    public string? HMajorHeadName { get; set; }
}

public class ObjectHeadDto
{
    public int ObjectHeadId { get; set; }
    public string ObjectHeadCode { get; set; } = string.Empty;
    public string ObjectHeadName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateObjectHeadRequest
{
    public string ObjectHeadCode { get; set; } = string.Empty;
    public string ObjectHeadName { get; set; } = string.Empty;
}
