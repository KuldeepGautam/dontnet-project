// Pre-budget - Add Allocation screen (PreBudgetMeeting/Allocation/REMeetingAllocation.cshtml).
// CSP fix (client requirement 2026-08-28, discovered while fixing that same page): this was
// previously an inline <script> block, which the app's own "script-src 'self'" CSP silently
// blocks entirely - same class of bug already documented elsewhere in this app (see site.js's
// initAutoSubmitSelects/initInputMasking comments). That meant "Select All Demands" never actually
// worked at all before this fix, not just the new features added alongside it.
//
// Split 2026-09-03 (client requirement, same pass as the Appendix PreBudget-control-binding.js/
// PreBudget-validation.js split): this page's own control wiring (Select-All/edit-row toggle) and
// validation rule (Target Date required) now live in those two shared files -
// window.PreBudgetControlBinding.initAllocationControls()/window.PreBudgetValidation.
// initAllocationValidation() - loaded before this one (see REMeetingAllocation.cshtml). This file
// keeps only what's genuinely specific to a one-shot page load: converting the server's inline
// save-result banner into this app's shared dialog convention.
(function () {
    // Client requirement 2026-08-28: "loader runs and stop, no modal messagebox come to tell if
    // data is saved or not, if there are any errors" - this page previously only showed the
    // server's StatusMessage as a plain inline <p role="status"> banner. Per this app's own
    // convention (dialogs.js's openInfoDialog/openConfirmDialog "use these for every save/
    // validation/confirm dialog across every module instead of ad hoc alerts or per-page markup"),
    // converted to the same pattern pre-budget-meeting.js's showStatusMessageAsDialog already uses
    // for the other Appendix screens: read the inline status element once on load, hide it, show
    // it as a dialog instead. This page does a real full-page POST-then-render (not an AJAX
    // fragment swap like the Appendix screens), so this runs once when the resulting page loads
    // rather than after an AJAX response.
    //
    // IMPORTANT: must use the id, not a bare [role="status"] selector - _Layout.cshtml's own
    // site-wide #navLoaderOverlay ALSO carries role="status" and renders earlier in the DOM, so a
    // bare attribute selector matched that instead of this page's own status paragraph (caught
    // live: it was hiding the nav loader overlay on every page load and reading its empty/stale
    // text as if it were this page's save-result message).
    var statusEl = document.getElementById("allocationStatusMessage");
    if (statusEl && window.openInfoDialog) {
        var text = (statusEl.textContent || "").trim();
        if (text) {
            var isError = statusEl.classList.contains("text-red-600");
            statusEl.style.display = "none";
            if (isError) {
                window.openInfoDialog("Could not save", text);
            } else {
                var isUpdate = /updat/i.test(text);
                window.openInfoDialog(isUpdate ? "Allocation Updated" : "Allocation Saved", text);
            }
        }
    }

    window.PreBudgetControlBinding.initAllocationControls();
    window.PreBudgetValidation.initAllocationValidation();
})();
