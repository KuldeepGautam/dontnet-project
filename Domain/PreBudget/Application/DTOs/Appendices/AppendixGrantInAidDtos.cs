namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

public class AppendixGrantInAidDto
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }

    public decimal? GiaGeneralActuals { get; set; }
    public decimal? GiaGeneralActualsUptoSeptPrevYear { get; set; }
    public decimal? GiaGeneralBE { get; set; }
    public decimal? GiaGeneralActualsUptoSept { get; set; }
    public decimal? GiaGeneralRE { get; set; }
    public decimal? GiaGeneralNBE { get; set; }

    public decimal? GiaCcaActuals { get; set; }
    public decimal? GiaCcaActualsUptoSeptPrevYear { get; set; }
    public decimal? GiaCcaBE { get; set; }
    public decimal? GiaCcaActualsUptoSept { get; set; }
    public decimal? GiaCcaRE { get; set; }
    public decimal? GiaCcaNBE { get; set; }

    public decimal? GiaSalaryActuals { get; set; }
    public decimal? GiaSalaryActualsUptoSeptPrevYear { get; set; }
    public decimal? GiaSalaryTotal { get; set; }
    public decimal? GiaSalaryBE { get; set; }
    public decimal? GiaSalaryActualsUptoSept { get; set; }
    public decimal? GiaSalaryRE { get; set; }
    public decimal? GiaSalaryNBE { get; set; }
    public bool IsFrozen { get; set; }
}

[NonNegativeAmounts]
public class SaveAppendixGrantInAidDto
{
    public int DemandId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public int AutonomousBodyId { get; set; }

    public decimal? GiaGeneralActuals { get; set; }
    public decimal? GiaGeneralActualsUptoSeptPrevYear { get; set; }
    public decimal? GiaGeneralBE { get; set; }
    public decimal? GiaGeneralActualsUptoSept { get; set; }
    public decimal? GiaGeneralRE { get; set; }
    public decimal? GiaGeneralNBE { get; set; }

    public decimal? GiaCcaActuals { get; set; }
    public decimal? GiaCcaActualsUptoSeptPrevYear { get; set; }
    public decimal? GiaCcaBE { get; set; }
    public decimal? GiaCcaActualsUptoSept { get; set; }
    public decimal? GiaCcaRE { get; set; }
    public decimal? GiaCcaNBE { get; set; }

    public decimal? GiaSalaryActuals { get; set; }
    public decimal? GiaSalaryActualsUptoSeptPrevYear { get; set; }
    public decimal? GiaSalaryTotal { get; set; }
    public decimal? GiaSalaryBE { get; set; }
    public decimal? GiaSalaryActualsUptoSept { get; set; }
    public decimal? GiaSalaryRE { get; set; }
    public decimal? GiaSalaryNBE { get; set; }
}
