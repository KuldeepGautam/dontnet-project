// Autonomous Master for Appendix VI-C/VI-E (PreBudgetMeeting/PreBudgetMeeting/
// AutonomousMasterforAppendixVICandVIE.cshtml). External file per this app's CSP (script-src
// 'self', no unsafe-inline) - same reasoning as every other externalized page script this session.
//
// Client requirement 2026-08-31: "After saving in db, it should clear textbox and show MessageBox
// in Dialog: Title: Save Record. Text: 'Autonomous Body Name is Saved Successfully'. then textbox
// should be cleared and in focus." This is a full-page POST-then-redirect flow (not AJAX), so the
// textbox is already empty by the time this runs (fresh GET-rendered page) - this just shows the
// dialog and focuses it. Reads TempData's values off the hidden #autonomousMasterStatusMessage
// element's data attributes (set by PreBudgetMeetingController.CreateAutonomousBody) rather than a
// bare [role="status"] selector - _Layout.cshtml's own #navLoaderOverlay also carries
// role="status" and would collide (caught live on a different page earlier this session).
(function () {
    var statusEl = document.getElementById("autonomousMasterStatusMessage");
    if (statusEl) {
        var text = statusEl.getAttribute("data-status");
        if (text && window.openInfoDialog) {
            var isError = statusEl.getAttribute("data-status-is-error") === "true";
            window.openInfoDialog(isError ? "Could not save" : "Save Record", text);
        }

        if (statusEl.getAttribute("data-focus-name") === "true") {
            var nameInput = document.getElementById("addAutonomousBodyDirectName");
            if (nameInput) {
                nameInput.focus();
            }
        }
    }
})();
