namespace UBIS.Services.Ecl.Domain.Entities;

/// <summary>
/// Maps to dbo.ECL_T_Actuals_Log — write-only audit trail. One row is written here every time
/// actuals are saved/changed on an EclSchemeOutlay row (see EclOutlayRepository.RecordActualsAsync).
/// Column casing/shape confirmed 2026-08-18 via INFORMATION_SCHEMA.COLUMNS.
/// </summary>
public class EclActualsLogEntry
{
    public int DataId { get; set; }

    public int RowId { get; set; }

    public string? FinancialYear { get; set; }

    public int? CategoryId { get; set; }

    public int? SubCategoryId { get; set; }

    public int? DemandId { get; set; }

    public int? SchemeId { get; set; }

    public int? SubSchemeId { get; set; }

    public decimal? Fy1Actuals { get; set; }
    public decimal? Fy2Actuals { get; set; }
    public decimal? Fy3Actuals { get; set; }
    public decimal? Fy4Actuals { get; set; }
    public decimal? Fy5Actuals { get; set; }
    public decimal? Fy6Actuals { get; set; }
    public decimal? Fy7Actuals { get; set; }
    public decimal? Fy8Actuals { get; set; }
    public decimal? Fy9Actuals { get; set; }
    public decimal? Fy10Actuals { get; set; }

    public int? ActualsUserId { get; set; }

    public string? ActualsIp { get; set; }

    public DateTime? ActualsEntryDate { get; set; }

    /// <summary>Insert/Update/Delete tracking. Convention: 'I' = insert, 'U' = update, 'D' = delete.</summary>
    public string? Action { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedIp { get; set; }

    public bool? IsActive { get; set; }

    public int? UserIdCreatedBy { get; set; }
    public DateTime? CreatedOnDate { get; set; }
    public int? UserIdModifyBy { get; set; }
    public DateTime? ModifiedOnDate { get; set; }

    public bool IsDeleted { get; set; }

    public int? DeletedByUserId { get; set; }

    public string? DeletedByIp { get; set; }

    public DateTime? DeletedOnDate { get; set; }

    /// <summary>Well-known Action values — see column doc comment above.</summary>
    public static class Actions
    {
        public const string Insert = "I";
        public const string Update = "U";
        public const string Delete = "D";
    }
}
