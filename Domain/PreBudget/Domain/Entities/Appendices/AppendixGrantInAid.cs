namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>Appendix V-A: Grant in Aid to Autonomous and Other Bodies. Maps to dbo.AppendixGrantInAid (was legacy Temp_GiA_AB). AutonomousBodyId FKs to this service's own M_AutonomousBody (FR-004).</summary>
public class AppendixGrantInAid : AppendixEntityBase
{
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

    /// <summary>
    /// GiaSalaryTotal ("Total Salary in [year] as per accounts of AB") reverted to a genuine
    /// user-entered field (client requirement, 2026-08-25 - "must be editable"), overriding the
    /// earlier 2026-08-04 decision to server-compute it as a sum of this row's other Salary
    /// fields. It's a distinct fact reported by the Autonomous Body itself, not derivable from
    /// this cycle's own BE/RE/NBE figures.
    /// </summary>
    public void UpdateFrom(
        int autonomousBodyId,
        decimal? giaGeneralActuals,
        decimal? giaGeneralActualsUptoSeptPrevYear,
        decimal? giaGeneralBE,
        decimal? giaGeneralActualsUptoSept,
        decimal? giaGeneralRE,
        decimal? giaGeneralNBE,
        decimal? giaCcaActuals,
        decimal? giaCcaActualsUptoSeptPrevYear,
        decimal? giaCcaBE,
        decimal? giaCcaActualsUptoSept,
        decimal? giaCcaRE,
        decimal? giaCcaNBE,
        decimal? giaSalaryActuals,
        decimal? giaSalaryActualsUptoSeptPrevYear,
        decimal? giaSalaryTotal,
        decimal? giaSalaryBE,
        decimal? giaSalaryActualsUptoSept,
        decimal? giaSalaryRE,
        decimal? giaSalaryNBE)
    {
        AutonomousBodyId = autonomousBodyId;
        GiaGeneralActuals = giaGeneralActuals;
        GiaGeneralActualsUptoSeptPrevYear = giaGeneralActualsUptoSeptPrevYear;
        GiaGeneralBE = giaGeneralBE;
        GiaGeneralActualsUptoSept = giaGeneralActualsUptoSept;
        GiaGeneralRE = giaGeneralRE;
        GiaGeneralNBE = giaGeneralNBE;
        GiaCcaActuals = giaCcaActuals;
        GiaCcaActualsUptoSeptPrevYear = giaCcaActualsUptoSeptPrevYear;
        GiaCcaBE = giaCcaBE;
        GiaCcaActualsUptoSept = giaCcaActualsUptoSept;
        GiaCcaRE = giaCcaRE;
        GiaCcaNBE = giaCcaNBE;
        GiaSalaryActuals = giaSalaryActuals;
        GiaSalaryActualsUptoSeptPrevYear = giaSalaryActualsUptoSeptPrevYear;
        GiaSalaryTotal = giaSalaryTotal;
        GiaSalaryBE = giaSalaryBE;
        GiaSalaryActualsUptoSept = giaSalaryActualsUptoSept;
        GiaSalaryRE = giaSalaryRE;
        GiaSalaryNBE = giaSalaryNBE;
    }
}
