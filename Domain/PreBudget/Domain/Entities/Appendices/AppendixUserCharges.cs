namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix VI-A: List of User Charges levied by Departments/Ministries. Maps to dbo.AppendixUserCharges (was legacy Temp_ListUserCharges).</summary>
public class AppendixUserCharges : AppendixEntityBase
{
    public string? TitleOfCharge { get; set; }
    public string? Service { get; set; }
    public string? OrgDept { get; set; }
    public string? RateOfCharge { get; set; }
    /// <summary>Split out from RateOfCharge 2026-09-07 per the approved design (Appendix6a.html),
    /// which shows "Rate of User Charge" and "Unit of collection" as two separate fields.</summary>
    public string? UnitOfCollection { get; set; }
    public DateTime? DateOfRateFixation { get; set; }
    public string? FixationStatute { get; set; }
    public decimal? TotalRevenueY1 { get; set; }
    public decimal? TotalRevenueY2 { get; set; }
    public decimal? TotalRevenueY3 { get; set; }
    public string? CompetentAuthority { get; set; }
    public string? PeriodOfFixation { get; set; }
    public decimal? Salary { get; set; }
    public decimal? OfficeExpenses { get; set; }
    public decimal? OtherExpenses { get; set; }
    public bool IsCollectionCostHigher { get; set; }
    public bool IsTransCostHigher { get; set; }
    public string? Remarks { get; set; }

    public void UpdateFrom(
        string? titleOfCharge,
        string? service,
        string? orgDept,
        string? rateOfCharge,
        string? unitOfCollection,
        DateTime? dateOfRateFixation,
        string? fixationStatute,
        decimal? totalRevenueY1,
        decimal? totalRevenueY2,
        decimal? totalRevenueY3,
        string? competentAuthority,
        string? periodOfFixation,
        decimal? salary,
        decimal? officeExpenses,
        decimal? otherExpenses,
        bool isCollectionCostHigher,
        bool isTransCostHigher,
        string? remarks)
    {
        TitleOfCharge = titleOfCharge;
        Service = service;
        OrgDept = orgDept;
        RateOfCharge = rateOfCharge;
        UnitOfCollection = unitOfCollection;
        DateOfRateFixation = dateOfRateFixation;
        FixationStatute = fixationStatute;
        TotalRevenueY1 = totalRevenueY1;
        TotalRevenueY2 = totalRevenueY2;
        TotalRevenueY3 = totalRevenueY3;
        CompetentAuthority = competentAuthority;
        PeriodOfFixation = periodOfFixation;
        Salary = salary;
        OfficeExpenses = officeExpenses;
        OtherExpenses = otherExpenses;
        IsCollectionCostHigher = isCollectionCostHigher;
        IsTransCostHigher = isTransCostHigher;
        Remarks = remarks;
    }
}
