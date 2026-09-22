// Drives the Pre-Budget Data and Report page's Demand -> Appendix cascade entirely via fetch -
// no full page postback for any step (picking a Demand, picking an Appendix, or Submit/Reset/
// Freeze/Delete inside the loaded appendix entry screen). Every request is tagged
// X-Requested-With: XMLHttpRequest, which PreBudgetMeetingController.OnActionExecuted uses to
// return a bare HTML fragment (no sidebar/header) instead of a full page.
//
// Split 2026-09-03 (client requirement: separate validation and control-binding concerns so a
// future UX/theme change to any appendix's controls doesn't require touching validation logic,
// and vice versa): this file is now just the SPA-shell orchestrator - Demand/Appendix cascade
// navigation, AJAX fragment load/apply, the session-expiry guard, and the generic Delete/
// Freeze-form submit plumbing. Every per-appendix dropdown cascade, previous-year auto-loader,
// Edit-mode populate function, and live recalculated display lives in
// PreBudget-control-binding.js; every validation rule (required/duplicate/format checks, the
// submit-time validation dialog) lives in PreBudget-validation.js. Both are loaded before this
// file (see _Layout.cshtml) and expose window.PreBudgetControlBinding / window.PreBudgetValidation,
// initialized below once `container` is resolved.
document.addEventListener("DOMContentLoaded", function () {
    var demandSelect = document.getElementById("demandSelect");
    var appendixSelect = document.getElementById("appendixSelect");
    var container = document.getElementById("partialViewContainer");
    var statusEl = document.getElementById("appendixStatus");

    if (!demandSelect || !appendixSelect || !container) {
        return;
    }

    // Set true right before a Delete form is actually submitted (see the submit handler); read
    // and cleared in applyFragment once the refreshed grid comes back. One central hook so every
    // appendix's Delete shows the same themed "Delete Record Successfully" MessageBox - Delete
    // actions return a plain refreshed fragment with no [role="status"] message of their own,
    // unlike Save/Modify.
    var pendingDeleteMessage = false;

    // .../PreBudgetMeeting/PreBudgetMeeting/PreBudgetDataandReport -> the sibling actions below,
    // same Area/Controller, just swapping the action segment.
    var basePath = window.location.pathname.replace(/PreBudgetDataandReport\/?$/, "");
    var entryUrl = basePath + "AppendixEntry";
    var optionsUrl = basePath + "GetAppendixOptions";

    // Every other cascade/previous-year-reference/duplicate-check endpoint this app calls - owned
    // by PreBudget-control-binding.js and PreBudget-validation.js, passed in via their own init()
    // below rather than duplicated there.
    var urls = {
        schemesUrl: basePath + "GetAppendixIIISchemes",
        subSchemesUrl: basePath + "GetAppendixIIISubSchemes",
        appendixIIIBeUrl: basePath + "GetAppendixIIIBe",
        appendixIVBeUrl: basePath + "GetAppendixIVBe",
        ivSubSchemesUrl: basePath + "GetAppendixIVSubSchemes",
        vibSchemesUrl: basePath + "GetAppendixVIBSchemes",
        vibSubSchemesUrl: basePath + "GetAppendixVIBSubSchemes",
        appendixVIBBeUrl: basePath + "GetAppendixVIBBe",
        appendixIAPrevYearUrl: basePath + "GetAppendixIAPreviousYearReference",
        appendixIIPrevYearUrl: basePath + "GetAppendixIIPreviousYearReference",
        appendixIVPrevYearUrl: basePath + "GetAppendixIVPreviousYearReference",
        appendixIVAPrevYearUrl: basePath + "GetAppendixIVAPreviousYearReference",
        appendixIVBPrevYearUrl: basePath + "GetAppendixIVBPreviousYearReference",
        appendixVAPrevYearUrl: basePath + "GetAppendixVAPreviousYearReference",
        appendixVBPrevYearUrl: basePath + "GetAppendixVBPreviousYearReference",
        appendixVIPrevYearUrl: basePath + "GetAppendixVIPreviousYearReference",
        appendixVIAPrevYearUrl: basePath + "GetAppendixVIAPreviousYearReference",
        appendixVIBPrevYearUrl: basePath + "GetAppendixVIBPreviousYearReference",
        appendixVICPrevYearUrl: basePath + "GetAppendixVICPreviousYearReference",
        appendixVIDPrevYearUrl: basePath + "GetAppendixVIDPreviousYearReference",
        appendixVIEPrevYearUrl: basePath + "GetAppendixVIEPreviousYearReference",
        appendixIIIABeUrl: basePath + "GetAppendixIIIABePreviousYear",
        appendixVIIAPrevYearUrl: basePath + "GetAppendixVIIAPreviousYearReference",
        appendixVIIBPrevYearUrl: basePath + "GetAppendixVIIBPreviousYearReference",
        appendixXPrevYearUrl: basePath + "GetAppendixXPreviousYearReference",
        appendixPAPrevYearUrl: basePath + "GetAppendixPAPreviousYearReference",
        appendixVIIBMajorHeadsUrl: basePath + "GetAppendixVIIBMajorHeads",
        appendixVIIBBeActualsUrl: basePath + "GetAppendixVIIBBeActuals",
        checkAutonomousBodyNameUrl: basePath + "CheckAutonomousBodyNameExists"
    };

    function setBusy(isBusy) {
        container.setAttribute("aria-busy", isBusy ? "true" : "false");
    }

    function escapeHtml(text) {
        return String(text == null ? "" : text)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    // Appendix names are often too long for the closed <select> box to show in full (client review
    // 2026-08-06) - a title attribute on the <select> itself surfaces the full text as a native
    // tooltip on hover, matching what the per-<option> title already does for the open dropdown list.
    function updateAppendixSelectTooltip() {
        var selected = appendixSelect.options[appendixSelect.selectedIndex];
        appendixSelect.title = selected ? selected.textContent : "";
    }

    function refreshIcons() {
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    // Client requirement 2026-08-27 (screenshot): "remove whole block for single demand user as
    // there is text label now no dropdown for this user" - a Single Demand user has no Demand
    // <select> to point the "Select a Demand"/"Select an Appendix" nudge panel at (replaced by the
    // .demand-pill label, PreBudgetDataandReport.cshtml), so those two static prompts render
    // nothing instead. "Loading…"/error placeholders still show regardless - real transient state
    // feedback, not asked to be removed.
    var suppressedPlaceholderTitles = { "Select a Demand": true, "Select an Appendix": true };

    function showPlaceholder(title, message) {
        if (container.getAttribute("data-single-demand") === "true" && suppressedPlaceholderTitles[title]) {
            container.innerHTML = "";
            return;
        }
        container.innerHTML =
            '<section class="pre-budget-placeholder">' +
            '<div class="pre-budget-placeholder-icon"><i data-lucide="folder-open"></i></div>' +
            "<div><h3>" + title + "</h3><p>" + message + "</p></div>" +
            "</section>";
        refreshIcons();
    }

    function loadAppendixOptions(demandId) {
        appendixSelect.value = "";
        statusEl.textContent = "";

        if (!demandId) {
            appendixSelect.disabled = true;
            appendixSelect.innerHTML = '<option value="">Select Appendix</option>';
            showPlaceholder("Select a Demand", "Choose a Demand above, then an Appendix, to view or enter Pre-Budget data.");
            return;
        }

        appendixSelect.disabled = true;
        appendixSelect.innerHTML = '<option value="">Loading…</option>';
        showPlaceholder("Loading appendices…", "Please wait.");

        // Client report 2026-08-31: "my page still closed as dropdown of select domain refreshes"
        // - this used to be the one fetch() in this file with no session-expiry guard at all
        // (added 2026-08-25/27 to every other fetch here for exactly this bug class). Now goes
        // through the shared fetchJson() helper (added 2026-08-31 for the same reason, applied
        // file-wide - see that function's own comment) like every other cascade fetch below.
        fetchJson(optionsUrl + "?demandId=" + encodeURIComponent(demandId), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (data) {
                var html = '<option value="">Select Appendix</option>';
                (data.appendices || []).forEach(function (a) {
                    var label = a.code + " - " + a.name;
                    html += '<option value="' + escapeHtml(a.code) + '" title="' + escapeHtml(label) + '">' + escapeHtml(label) + "</option>";
                });
                appendixSelect.innerHTML = html;
                appendixSelect.disabled = false;
                updateAppendixSelectTooltip();

                if (!data.success) {
                    showPlaceholder("Could not load appendices", data.message || "Please try again.");
                    return;
                }

                // If this Demand only has one Appendix to offer, load it directly instead of
                // making the user pick from a dropdown with a single real choice.
                window.PreBudgetControlBinding.autoSelectIfSingleOption(appendixSelect);
                if (appendixSelect.value) {
                    return;
                }

                showPlaceholder("Select an Appendix", "Choose an Appendix above to view or enter Pre-Budget data.");
            })
            .catch(function () {
                appendixSelect.innerHTML = '<option value="">Select Appendix</option>';
                appendixSelect.disabled = false;
                showPlaceholder("Could not load appendices", "Could not reach the server. Please try again.");
            });
    }

    // "Page sometimes automatically refreshes and I lose my Demand/Appendix selection" (client
    // report, 2026-08-25) - ExpiredSessionRedirectAsync now returns 401 for an AJAX request instead
    // of a redirect the fetch() calls below would otherwise follow silently, swapping the full
    // Login page's HTML into #partialViewContainer (looked exactly like an unexplained refresh).
    // Every fetch that can load into that container checks for this and does a REAL, visible
    // navigation instead, so a genuinely expired session reads as "please log in again," not as a
    // mysteriously blanked screen.
    //
    // Follow-up (client report, 2026-08-27 - same symptom, still reproducing on Delete/Save/Freeze):
    // ComplianceInterceptorFilter's forced-password-reset and hardware-IP-mismatch checks run on
    // every authenticated request, including these, and were issuing the exact same kind of
    // ungated redirect ExpiredSessionRedirectAsync was fixed for - just never updated to match. Now
    // AJAX requests get a 401 with `{ redirectUrl }` in the body instead, so this can send the user
    // to the RIGHT page (ForcePasswordReset, or Login with hardwareMismatch=true) instead of always
    // hardcoding /User/Login. A body-less 401 (the plain ExpiredSessionRedirectAsync case) falls
    // through the same way it always did.
    function redirectToLoginIfSessionExpired(res) {
        if (res.status === 401) {
            res.json()
                .then(function (data) { window.location.href = (data && data.redirectUrl) || "/User/Login"; })
                .catch(function () { window.location.href = "/User/Login"; });
            return true;
        }
        return false;
    }

    // Client report 2026-08-31: "demand dropdown refreshes, for some users roles stops loading
    // while working on appendix III" - traced to the many small reference-data/cascade fetch()
    // calls (Scheme/SubScheme dropdown population, prior-year lookups, duplicate-name checks) that,
    // unlike loadAppendixEntry/submitFragmentForm/loadAppendixOptions above, were never guarded
    // with redirectToLoginIfSessionExpired. A 401 from ComplianceInterceptorFilter/
    // ExpiredSessionRedirectAsync mid-session (a role-dependent timing - whichever compliance
    // check fires first for that role/account state) fell into each fetch's own generic .catch(),
    // which just leaves that one dropdown "Loading…"/blank or shows "Could not load X" forever -
    // never the real, visible redirect to Login every other guarded fetch in this file already
    // gets. Every cascade fetch in PreBudget-control-binding.js/PreBudget-validation.js goes
    // through this shared helper (passed into their own init() below) instead of repeating the
    // same res.ok-check inline; the resulting Promise behaves exactly like the old
    // `fetch(...).then(res => res.ok ? res.json() : Promise.reject())` chain (still rejects on a
    // non-2xx or on session-expiry, so every existing .catch() needs no changes), except a 401
    // now redirects first.
    function fetchJson(url, options) {
        return fetch(url, options).then(function (res) {
            if (redirectToLoginIfSessionExpired(res)) {
                return Promise.reject(new Error("session-expired"));
            }
            return res.ok ? res.json() : Promise.reject();
        });
    }

    function loadAppendixEntry(code) {
        var demandId = demandSelect.value;
        if (!code || !demandId) {
            // Client report 2026-08-27 (screenshot): the Appendix dropdown was reset back to its
            // blank "Select Appendix" placeholder, but the appendixStatus line above the
            // placeholder panel kept showing the PREVIOUSLY selected appendix's full name (e.g.
            // "IV-A - Estimates of..."), stale, since nothing on this branch ever cleared it -
            // only appendixSelect's own "change" handler does that (updateAppendixSelectTooltip),
            // which loadAppendixEntry can also be called from directly (e.g. the "Refresh" button).
            statusEl.textContent = "";
            showPlaceholder("Select an Appendix", "Choose an Appendix above to view or enter Pre-Budget data.");
            return;
        }

        setBusy(true);
        showPlaceholder("Loading…", "Please wait while the appendix loads.");

        fetch(entryUrl + "?code=" + encodeURIComponent(code) + "&demandId=" + encodeURIComponent(demandId), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (res) {
                if (redirectToLoginIfSessionExpired(res)) {
                    return Promise.reject(new Error("session-expired"));
                }
                return res.ok ? res.text() : Promise.reject();
            })
            .then(function (html) { applyFragment(html); })
            .catch(function () {
                setBusy(false);
                showPlaceholder("Could not load", "Could not reach the server. Please try again.");
            });
    }

    // "Remove Records Saved Successfully text from all Appendixes, show modal dialog... All failed
    // validations during Save or Modify must be shown [in a] dialog box" (client testing feedback,
    // 2026-08-25) - every appendix already renders the exact same inline `[role="status"]` element
    // for both success and server-side failure (StatusMessage/StatusIsError), so this is a single
    // generic hook rather than a per-appendix view change: read it, show it as a dialog instead,
    // then hide the inline element (the underlying markup/StatusMessage plumbing stays - other code
    // reads Model.StatusIsError for other purposes, e.g. banner styling - this only changes how the
    // message itself is surfaced to the user).
    function showStatusMessageAsDialog(root) {
        var statusEl = root.querySelector('[role="status"]');
        if (!statusEl) {
            return;
        }
        var text = statusEl.textContent.trim();
        if (!text) {
            return;
        }
        var isError = statusEl.classList.contains("text-red-600");
        statusEl.style.display = "none";
        if (!window.openInfoDialog) {
            return;
        }

        if (isError) {
            // Client requirement 2026-08-31 (Appendix VI): "Error MessageBox Title: Record Not
            // Saved, Text: Record existis for selected reciept type and PSU/Receipt Name." - the
            // server's own duplicate-entry rejection text (AppendixVIController.CreateRecord/
            // UpdateRecord) is reproduced verbatim, so match on it specifically rather than
            // widening every save error to this heading.
            var isDuplicate = /existis for selected reciept type/i.test(text);
            window.openInfoDialog(isDuplicate ? "Record Not Saved" : "Could not save", text);
            return;
        }

        // "Dialog Heading: Record Saved, Text: Record has been successfully saved." (client
        // requirement, 2026-08-25, exact wording) - the server's own StatusMessage still
        // distinguishes Create ("Record saved successfully.") from Modify ("Record modified
        // successfully."), so the heading/text pair mirrors that instead of collapsing both into
        // literally the same "Saved" wording on an actual edit.
        var isModify = /modif/i.test(text);
        window.openInfoDialog(
            isModify ? "Record Updated" : "Record Saved",
            isModify ? "Record has been successfully updated." : "Record has been successfully saved."
        );
    }

    function applyFragment(html) {
        // Bug found alongside the 2026-09-08 shell-disappears-after-save fix: any row action-menu
        // reparented to document.body (VI-A/VI-B/VI-C's own positionActionMenu, done to escape
        // the grid's overflow clipping) belongs to a row that no longer exists once this fragment
        // is thrown away - `container.innerHTML = html` below only clears what's still inside the
        // container, so a menu left open at reload time would otherwise float on screen forever,
        // referencing a stale row id. Every genuine row menu lives inside `container` until a user
        // opens it; anything still sitting directly under <body> at this point is always stale.
        Array.prototype.forEach.call(document.body.querySelectorAll(":scope > .action-menu"), function (menu) {
            menu.remove();
        });

        // Bug report 2026-09-15 (Appendix IV-A): "data is not coming from db at new/modify slider
        // for Actual/Actuals-upto-Sept/B.E." - root cause was every reparented drawer being left
        // sitting under <body> forever (each appendix-*-drawer.js's own reparent function only
        // dedupes ITS OWN drawer id against a previous copy of itself, never against a DIFFERENT
        // appendix's drawer). Appendix IV/IV-A/IV-B all reuse the exact same field ids
        // (NewRecord_SchemeId, NewRecord_BE, NewRecord_ActualsUptoSeptPrevYear, ...), so visiting
        // e.g. Appendix IV and then Appendix IV-A in the same page session (no full reload) left
        // IV's own reparented #ivDrawer parked under <body> - document.getElementById always
        // returns the FIRST match in document order, so every AJAX-loaded value (previous-year
        // Actuals/BE) got silently written into that invisible, stale IV drawer instead of the
        // one the user was actually looking at. A fresh fragment always carries its own brand-new
        // (still-closed) drawer copy inside `html` below, which gets reparented again by that
        // appendix's own onFragmentApplied hook right after - so it's always safe to remove every
        // previously reparented drawer/backdrop here before swapping the fragment in.
        Array.prototype.forEach.call(document.body.querySelectorAll('[data-reparented-fragment="true"]'), function (drawer) {
            drawer.remove();
        });
        Array.prototype.forEach.call(document.body.querySelectorAll(':scope > [id$="DrawerBackdrop"]'), function (backdrop) {
            backdrop.remove();
        });

        // Bug report 2026-09-08 ("overlapping issue, dropdown partially visible under header"):
        // opening a VI-A/VI-B/VI-C drawer adds ubis-drawer-page-locked to <body> (background-
        // scroll lock: overflow:hidden !important) - normally removed again by the drawer's own
        // Cancel/X handler, but a Submit/Freeze/Nil never calls that; it reloads the whole
        // fragment instead, and <body> itself is untouched by that reload, so the class (and the
        // scroll lock with it) stuck around forever. With scrolling locked at wherever the user
        // had scrolled the drawer's tall form to, the freshly-reloaded page rendered at that same
        // stale scroll offset - visually shoving its own top content (the Demand/Appendix
        // selector bar) up under the sticky masthead header. Every fresh fragment always carries
        // its own drawer back in the closed state, so this lock is never legitimately still
        // needed at this point - clear it unconditionally on every reload.
        document.body.classList.remove("ubis-drawer-page-locked");

        // Removing the lock above only allows scrolling again - it doesn't move the scroll
        // position itself, which stays wherever the user had scrolled the page to (e.g. down the
        // grid before opening Edit on a lower row) before the drawer opened. Before this bug fix
        // Save/Freeze/Nil/Delete fell through to a real page reload, which naturally resets scroll
        // to top; now that it's a genuine AJAX fragment swap, this needs to be done explicitly the
        // way any SPA navigation would - otherwise the freshly-loaded content (starting with the
        // Demand/Appendix selector bar) renders at that same stale scroll offset, which can put it
        // behind/above the sticky masthead header instead of visible below it.
        window.scrollTo(0, 0);

        container.innerHTML = html;
        setBusy(false);
        refreshIcons();
        showStatusMessageAsDialog(container);

        // Themed confirmation for a Delete that just completed (client requirement 2026-09-10) -
        // same window.openInfoDialog MessageBox as Save/Modify use, wording per the client.
        if (pendingDeleteMessage) {
            pendingDeleteMessage = false;
            if (window.openInfoDialog) {
                window.openInfoDialog("Record Deleted", "Delete Record Successfully");
            }
        }

        window.PreBudgetControlBinding.onFragmentApplied();

        // Client requirement 2026-09-02 ("remove appendix name text from page, it is still
        // showing"): the approved design has no separate "<Appendix> - <Name>" label between the
        // Demand/Appendix selector and the loaded appendix's own highlighted banner (which already
        // shows the same text) - so this box is left empty rather than populated.
    }

    demandSelect.addEventListener("change", function () {
        loadAppendixOptions(this.value);
    });

    // If this user only has one Demand to choose from, pick it directly instead of leaving the
    // dropdown on "Select Demand" - the change event this dispatches is what actually triggers
    // loadAppendixOptions above, exactly as if the user had picked it themselves.
    window.PreBudgetControlBinding.autoSelectIfSingleOption(demandSelect);

    appendixSelect.addEventListener("change", function () {
        updateAppendixSelectTooltip();
        loadAppendixEntry(this.value);
    });

    // Was its own local .confirm-backdrop/.confirm-card implementation, hardcoding "Confirm" as
    // the heading with no way to customize it - a shadow duplicate of the real, shared
    // window.openConfirmDialog (dialogs.js), which this file's own bare (unqualified)
    // `openConfirmDialog(...)` calls were silently resolving to instead of the global one, the
    // exact "ad hoc per-page dialog instead of the single shared control" problem dialogs.js's own
    // header comment describes fixing elsewhere. Found while wiring the Freeze confirmation
    // (client requirement 2026-08-31: "Heading: Freezing Data" - literally impossible with the old
    // hardcoded "Confirm" heading). Now a thin wrapper over the real window.openConfirmDialog
    // (title, message, confirmLabel, onConfirm, danger, cancelLabel) - same call shape as before
    // for this file's own callers, "Delete"-labelled calls (no explicit label) still get the red
    // danger button and a generic "Confirm" title, exactly as before.
    function openConfirmDialog(title, message, onConfirm, confirmLabel, cancelLabel) {
        var label = confirmLabel || "Delete";
        var danger = !confirmLabel;
        window.openConfirmDialog(title, message, label, onConfirm, danger, cancelLabel);
    }

    function submitFragmentForm(form, submitter) {
        setBusy(true);

        // event.submitter is the specific button that triggered this submit - a button with its
        // own formaction (e.g. "Freeze Data" alongside "Submit"/"Reset" in the same <form>) must
        // win over the form's own default action, exactly like a native form submission would.
        var actionUrl = (submitter && submitter.getAttribute("formaction")) || form.getAttribute("action") || window.location.href;

        fetch(actionUrl, {
            method: (form.getAttribute("method") || "post").toUpperCase(),
            headers: { "X-Requested-With": "XMLHttpRequest" },
            body: new FormData(form)
        })
            .then(function (res) {
                if (redirectToLoginIfSessionExpired(res)) {
                    return Promise.reject(new Error("session-expired"));
                }
                return res.ok ? res.text() : Promise.reject();
            })
            .then(function (html) { applyFragment(html); })
            .catch(function () {
                pendingDeleteMessage = false;
                setBusy(false);
                showPlaceholder("Could not save", "Could not reach the server. Please try again.");
            });
    }

    // Delegated handling for anything rendered *inside* the AJAX-loaded fragment: the
    // Save/Freeze/Delete forms and the "Go Back" link on each appendix's entry view. Those views
    // are unchanged, ordinary asp-action forms/links (so they still degrade to real postbacks if
    // JS is ever unavailable) - intercepted here so using them updates just the fragment instead
    // of navigating the whole page away.
    // Bug report 2026-09-08 ("after modifying record, upper Demand/Appendix selector gone"):
    // this listener used to be scoped to `container` (#partialViewContainer) only. The VI-A/
    // VI-B/VI-C drawers reparent their own <form> (and the row action-menu, which carries each
    // row's own Delete <form>) out to document.body - the SAME fix already applied for the
    // drawer-painted-behind-the-masthead bug and the Category/Scheme cascade - so once reparented,
    // a Save/Freeze/Nil/Delete submit no longer bubbles through `container` at all. It fell
    // through as a REAL, un-intercepted browser form post instead, which still round-trips
    // correctly (SaveAppendixVIA etc. don't require the XHR header to work), but the response is
    // a bare `View("AppendixVIA", model)` render under the DEFAULT layout - not this page
    // (PreBudgetDataandReport.cshtml, which owns the Demand/Appendix selector bar around
    // #partialViewContainer) - so the selector bar (and the rest of this page's own chrome)
    // vanished, replaced by a "naked" render of just that one view.
    //
    // Fix: listen on `document` instead, but only actually handle a submit if it's either still
    // inside `container` (the original, already-correct case) or came from an element carrying
    // data-reparented-fragment - the marker appendix-vi{a,b,c}-drawer.js's own reparenting/
    // action-menu-positioning code applies to whatever it moves to document.body. Excludes
    // everything else appearing on this page outside the container (e.g. the header's own Logout
    // form in _UserMenu.cshtml), which carries neither.
    document.addEventListener("submit", function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement)) {
            return;
        }
        if (!container.contains(form) && !form.closest("[data-reparented-fragment]")) {
            return;
        }

        event.preventDefault();
        var submitter = event.submitter;

        // Delete actions are all named Delete* (DeleteAppendixIIIB, DeleteAppendixVIIA, ...) - no
        // Save/Freeze/Nil action URL contains "delete". Set the flag only on the path that
        // actually submits, so a cancelled confirm dialog doesn't leave it armed for the next
        // unrelated fragment load.
        var deleteActionUrl = (submitter && submitter.getAttribute("formaction")) || form.getAttribute("action") || form.action || "";
        var isDeleteSubmission = /delete/i.test(deleteActionUrl);

        var confirmMessage = form.getAttribute("data-confirm");
        if (confirmMessage) {
            // Bug report 2026-08-31 (Appendix IV: "Delete functionality in grid not working" -
            // same shared handler drives every appendix's Delete form, so this was broken
            // everywhere, not just IV). Root cause: this call site still used the OLD 3-arg
            // (message, onConfirm, confirmLabel) shape from before the 2026-08-31 Freeze dialog
            // work redefined the local openConfirmDialog() delegator to (title, message,
            // onConfirm, confirmLabel, cancelLabel) - never updated here. That shifted every
            // argument by one: the onConfirm callback landed in the "message" slot (rendered as
            // garbled function-source text) and the real window.openConfirmDialog got `undefined`
            // for onConfirm, so clicking Delete/Confirm threw "onConfirm is not a function" and
            // silently did nothing - no request ever left the browser.
            var confirmLabel = form.getAttribute("data-confirm-label");
            openConfirmDialog("Delete Record", confirmMessage, function () {
                pendingDeleteMessage = isDeleteSubmission;
                submitFragmentForm(form, submitter);
            }, confirmLabel);
            return;
        }

        pendingDeleteMessage = isDeleteSubmission;
        submitFragmentForm(form, submitter);
    });

    container.addEventListener("click", function (event) {
        // "View Consolidated Report" on the V-A/V-B/V-C tabbed page jumps to the read-only base
        // Appendix V summary - same SPA navigation the Appendix dropdown itself does, since a plain
        // <a> to AppendixEntry would render a bare AJAX fragment (no layout) as a full page.
        var consolidatedButton = event.target.closest('[data-action="view-consolidated-report"]');
        if (consolidatedButton) {
            appendixSelect.value = "V";
            loadAppendixEntry("V");
            return;
        }

        // "Refresh" re-fetches the whole appendix screen fresh from the server (same call the
        // Appendix dropdown itself uses) - distinct from "Edit": Edit re-populates the entry grid
        // from what THIS page already has loaded (instant, no round trip), Refresh re-syncs
        // everything with the database in case it changed elsewhere since the page was loaded.
        var refreshButton = event.target.closest('[data-action="refresh-appendix"]');
        if (refreshButton) {
            loadAppendixEntry(appendixSelect.value);
            return;
        }

        var link = event.target.closest("a[href]");
        if (!link || link.href.indexOf("PreBudgetDataandReport") === -1) {
            return;
        }

        // "Go Back" links point at PreBudgetDataandReport - handled client-side (just clear the
        // appendix selection) instead of actually navigating there.
        event.preventDefault();
        appendixSelect.value = "";
        showPlaceholder("Select an Appendix", "Choose an Appendix above to view or enter Pre-Budget data.");
        statusEl.textContent = "";
    });

    window.PreBudgetControlBinding.init({
        container: container,
        urls: urls,
        fetchJson: fetchJson,
        demandSelect: demandSelect
    });

    window.PreBudgetValidation.init({
        container: container,
        urls: urls,
        fetchJson: fetchJson,
        submitFragmentForm: submitFragmentForm
    });

    // A bookmarked/shared URL (?demandId=...) already has the Demand pre-selected server-side -
    // fetch its appendix list immediately instead of waiting for a change event that will never
    // fire on page load.
    if (demandSelect.value) {
        loadAppendixOptions(demandSelect.value);
    }
});
