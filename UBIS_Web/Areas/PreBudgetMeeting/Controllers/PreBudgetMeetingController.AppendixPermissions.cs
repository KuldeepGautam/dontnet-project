// Shared appendix-level permission helper. Not specific to any single appendix - per its own doc
// comment below it is reusable across VII-A/VII-B/XI/PA-ReceiptPayment (and any future appendix
// that opts in), so it gets its own file rather than living inside one appendix's file.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    /// <summary>
    /// Appendix-level permission framework: fetches the caller's role-based CRUD flags for
    /// appendixCode from PreBudget's api/appendix-permissions and copies them onto the ViewModel.
    /// Reusable across VII-A/VII-B/XI/PA-ReceiptPayment (and any future appendix that opts in) so the
    /// Razor views only ever need to check Model.HasEntryPermission / Model.Can*. On any call failure
    /// (network error, non-2xx) the base ViewModel's fail-open defaults (all Can* = true) are left in
    /// place rather than silently failing closed here - the server-side controllers are the real
    /// enforcement point (they return 403 regardless of what this page shows), so a transient failure
    /// here degrades to "show the form, let the API reject the write" rather than hiding the whole
    /// page for everyone whenever the permission lookup itself has a hiccup.
    /// </summary>
    private async Task ApplyAppendixPermissionsAsync(AppendixBaseViewModel model, UbisSessionData session, string appendixCode, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixPermissionsAsync(session.Token, appendixCode, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            return;
        }

        model.CanView = result.Data.CanView;
        model.CanCreate = result.Data.CanCreate;
        model.CanEdit = result.Data.CanEdit;
        model.CanDelete = result.Data.CanDelete;
        model.CanSubmit = result.Data.CanSubmit;
        model.CanApprove = result.Data.CanApprove;
    }
}
