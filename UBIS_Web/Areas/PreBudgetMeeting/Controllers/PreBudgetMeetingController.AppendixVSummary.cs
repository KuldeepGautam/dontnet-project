// View-model builder for Appendix V: the read-only consolidated report auto-populated from
// Appendices V-A/V-B/V-C. No Save/Freeze/Delete actions - this appendix has no entry form of its
// own.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix V: read-only consolidated report (auto-populated from V-A/V-B/V-C) ---
    private async Task<AppendixVSummaryViewModel> BuildAppendixVSummaryViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var vaResult = await _preBudgetClient.GetAppendixVAAsync(session.Token, demandId, session.FinancialYear, ct);
        var vbResult = await _preBudgetClient.GetAppendixVBAsync(session.Token, demandId, session.FinancialYear, ct);
        var vcResult = await _preBudgetClient.GetAppendixVCAsync(session.Token, demandId, session.FinancialYear, ct);

        var va = vaResult.Data ?? new();
        var vb = vbResult.Data ?? new();
        var vc = vcResult.Data ?? new();

        // "Salary" = Object Head code 01 (Salaries); everything else on V-B is "Non-Salary" - same
        // split the legacy sp_Appendix5BDemandWiseData's caller used ObjectHeadCode for.
        var salaryRecords = vb.Where(r => r.ObjectHeadCode == "01").ToList();
        var nonSalaryRecords = vb.Where(r => r.ObjectHeadCode != "01").ToList();

        AppendixVSummaryRow Sum(string label, IEnumerable<(decimal? Actual, decimal? ActualUptoSeptPrev, decimal? BE, decimal? ActualUptoSept, decimal? RE, decimal? NBE)> rows, bool isTotal = false)
        {
            var list = rows.ToList();
            var row = new AppendixVSummaryRow
            {
                Label = label,
                IsTotal = isTotal,
                Actual = list.Sum(r => r.Actual ?? 0),
                ActualsUptoSeptPrevYear = list.Sum(r => r.ActualUptoSeptPrev ?? 0),
                BE = list.Sum(r => r.BE ?? 0),
                ActualsUptoSept = list.Sum(r => r.ActualUptoSept ?? 0),
                ProposedRE = list.Sum(r => r.RE ?? 0),
                ProposedNBE = list.Sum(r => r.NBE ?? 0)
            };
            row.PercentWrtBE = row.BE is > 0 ? Math.Round((row.ActualsUptoSept ?? 0) / row.BE.Value * 100, 2) : null;
            return row;
        }

        var salary = Sum("Salary", salaryRecords.Select(r => (r.Actuals, r.ActualsUptoSeptPrevYear, r.BE, r.ActualsUptoSept, r.ProposedRE, r.ProposedNBE)));
        var nonSalary = Sum("Non-Salary", nonSalaryRecords.Select(r => (r.Actuals, r.ActualsUptoSeptPrevYear, r.BE, r.ActualsUptoSept, r.ProposedRE, r.ProposedNBE)));
        var totalEstt = Sum("Total (Estt. Exp.)", new[] { salary, nonSalary }.Select(r => (r.Actual, r.ActualsUptoSeptPrevYear, r.BE, r.ActualsUptoSept, r.ProposedRE, r.ProposedNBE)), isTotal: true);

        var giaGeneral = Sum("GiA General", va.Select(r => (r.GiaGeneralActuals, r.GiaGeneralActualsUptoSeptPrevYear, r.GiaGeneralBE, r.GiaGeneralActualsUptoSept, (decimal?)r.GiaGeneralRE, (decimal?)r.GiaGeneralNBE)));
        var giaCca = Sum("GiA for Cap. Assets", va.Select(r => (r.GiaCcaActuals, r.GiaCcaActualsUptoSeptPrevYear, r.GiaCcaBE, r.GiaCcaActualsUptoSept, (decimal?)r.GiaCcaRE, (decimal?)r.GiaCcaNBE)));
        var giaSalary = Sum("GiA Salary", va.Select(r => (r.GiaSalaryActuals, r.GiaSalaryActualsUptoSeptPrevYear, r.GiaSalaryBE, r.GiaSalaryActualsUptoSept, (decimal?)r.GiaSalaryRE, (decimal?)r.GiaSalaryNBE)));
        var totalGia = Sum("Total (ABs)", new[] { giaGeneral, giaCca, giaSalary }.Select(r => (r.Actual, r.ActualsUptoSeptPrevYear, r.BE, r.ActualsUptoSept, r.ProposedRE, r.ProposedNBE)), isTotal: true);

        var totalOtherThanAb = Sum("Total (Other than AB)", vc.Select(r => (r.Actuals, r.ActualsUptoSeptPrevYear, r.BE, r.ActualsUptoSept, r.ProposedRE, r.ProposedNBE)), isTotal: true);

        var grandTotal = Sum("Grand Total", new[] { totalEstt, totalGia, totalOtherThanAb }.Select(r => (r.Actual, r.ActualsUptoSeptPrevYear, r.BE, r.ActualsUptoSept, r.ProposedRE, r.ProposedNBE)), isTotal: true);

        return new AppendixVSummaryViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Rows =
            [
                new AppendixVSummaryRow { Label = "Establishment Exp.", IsSectionHeader = true },
                salary,
                nonSalary,
                totalEstt,
                new AppendixVSummaryRow { Label = "Other Central Exp.", IsSectionHeader = true },
                giaGeneral,
                giaCca,
                giaSalary,
                totalGia,
                totalOtherThanAb,
                grandTotal
            ]
        };
    }
}
