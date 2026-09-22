// Extracted 2026-09-03 from pre-budget-meeting.js (see PreBudget-control-binding.js's own header
// comment for the split rationale). This file owns every validation RULE for the PreBudget
// appendix forms: numeric-field format sanitization (typed, pasted, and as-you-type), the
// submit-time "collect every invalid field into one dialog" engine (required/blank-cascade/
// Freeze/Appendix-X-row checks), the Autonomous Body duplicate-name live check, Appendix IV/IV-A's
// client-side duplicate-Scheme guard, and Appendix V-C's Name on-blur normalize rule. Everything
// that WIRES a control up (dropdown cascades, Edit-mode population/locking, event delegation for
// non-validation purposes) lives in PreBudget-control-binding.js instead. Both are plain
// global-namespace modules (window.PreBudgetValidation / window.PreBudgetControlBinding), loaded
// before pre-budget-meeting.js's own <script> tag; that file remains the SPA-shell orchestrator and
// calls into both namespaces' init() entry points.
window.PreBudgetValidation = (function () {
    "use strict";

    var container, urls, fetchJson, submitFragmentForm;

    // ----------------------------------------------------------------------------------------
    // Numeric-field format rules
    // ----------------------------------------------------------------------------------------

    // Enforces "numbers only, at most 2 decimal places" on every type="number" amount field
    // across all appendices (the 5-year AppendixI grid included, via delegation - no per-view JS
    // needed). HTML5 number inputs already reject most letters, but NOT e/E/+/- (valid in HTML5's
    // number syntax, e.g. "1e5") and enforce no cap at all on decimal-place COUNT, so a browser
    // alone would happily accept "123.456789". This is a plain string sieve, not a rich editor:
    // strip anything that isn't a digit or a single ".", then truncate to 2 digits after it.
    // allowNegative (added 2026-08-27 for Appendix IV-A's ActualsUptoSept, the one field this app
    // lets go negative - see data-allow-negative) preserves a single leading "-"; every other
    // caller passes nothing, so every other field keeps stripping "-" exactly as before.
    function sanitizeNumberString(raw, allowNegative) {
        var value = String(raw == null ? "" : raw);
        var negative = allowNegative && value.trim().charAt(0) === "-";

        // Keep digits and dots only, then collapse every dot after the first into nothing (so
        // "1.2.3" -> "1.23", not rejected outright - simplest safe behavior for a budget entry
        // field, not meant to be a strict parser).
        value = value.replace(/[^0-9.]/g, "");
        var firstDot = value.indexOf(".");
        if (firstDot !== -1) {
            value = value.slice(0, firstDot + 1) + value.slice(firstDot + 1).replace(/\./g, "");
        }

        var parts = value.split(".");
        if (parts.length === 2) {
            value = parts[0] + "." + parts[1].slice(0, 2);
        }

        return (negative && value ? "-" : "") + value;
    }

    // ----------------------------------------------------------------------------------------
    // Autonomous Body duplicate-name live check
    // ----------------------------------------------------------------------------------------

    // Autonomous Master duplicate-name check (client requirement 2026-08-10): as the admin/user
    // types into either the direct-Add or Request-it Name field, checks the typed value against
    // the Redis-cached list of this Demand's existing Autonomous Bodies (populated server-side on
    // page load - see PreBudgetMeetingController.AutonomousMasterforAppendixVICandVIE) rather than
    // hitting the DB on every keystroke. Debounced (300ms) so a fast typist doesn't fire a request
    // per character; the submit-guard below is what actually blocks a duplicate, this is just the
    // live inline warning.
    var autonomousBodyNameCheckTimers = {};
    var autonomousBodyNameIsDuplicate = {
        addAutonomousBodyName: false,
        addAutonomousBodyHName: false,
        requestAutonomousBodyName: false
    };
    var autonomousBodyNameCheckIds = ["addAutonomousBodyName", "addAutonomousBodyHName", "requestAutonomousBodyName"];

    // The Add form's own "Select Demand" dropdown (client requirement 2026-08-10: matches the
    // legacy Add screen, a real selectable field rather than derived from the page's outer Demand
    // filter) - the Request form has no such dropdown of its own, so falls back to its static
    // data-demand-id (set from the page's outer Demand filter, unchanged from before).
    function resolveAutonomousBodyFormDemandId(form) {
        var demandSelect = form.querySelector("#addAutonomousBodyDemandId");
        if (demandSelect) {
            return demandSelect.value;
        }
        return form.getAttribute("data-demand-id");
    }

    function checkAutonomousBodyNameDuplicate(input) {
        var form = input.closest("#addAutonomousBodyForm, #requestAutonomousBodyForm");
        var errorEl = document.getElementById(input.id + "Error");
        if (!form || !errorEl) {
            return;
        }

        var demandId = resolveAutonomousBodyFormDemandId(form);
        var name = input.value.trim();
        if (!demandId || !name) {
            errorEl.classList.add("hidden");
            autonomousBodyNameIsDuplicate[input.id] = false;
            return;
        }

        var field = input.getAttribute("data-field") || "Name";
        fetchJson(urls.checkAutonomousBodyNameUrl + "?demandId=" + encodeURIComponent(demandId) + "&name=" + encodeURIComponent(name) + "&field=" + encodeURIComponent(field), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (data) {
                var isDuplicate = !!(data && data.exists);
                autonomousBodyNameIsDuplicate[input.id] = isDuplicate;
                errorEl.classList.toggle("hidden", !isDuplicate);
            })
            .catch(function () {
                // Network hiccup - leave whatever state was already shown; the submit-guard below
                // only blocks on a confirmed "true", so a failed check never falsely blocks Submit.
            });
    }

    // ----------------------------------------------------------------------------------------
    // Appendix IV/IV-A client-side duplicate-Scheme guard
    // ----------------------------------------------------------------------------------------

    // Appendix IV/IV-A duplicate-entry guard (client requirement 2026-08-27): "Unique row entry for
    // Demand and Scheme, if there is sub-scheme, then uniqueness validation for Demand - Scheme -
    // Subscheme. Hide Save button instead." - rather than let the user submit and show them a
    // validation-error dialog (the pattern used everywhere else in this module), this appendix
    // proactively hides Submit the moment the selected Scheme(+SubScheme) already has a saved row,
    // before they ever get that far. The server-side check in AppendixIVController/
    // AppendixIVAController is still the authoritative guard (this is convenience only, matching
    // this codebase's "server-side enforcement, never UI-hiding alone" rule) - if this client check
    // is ever wrong or bypassed, the save attempt still gets rejected with the same message.
    // IV-B is deliberately NOT included - only IV/IV-A were named in this requirement, and IV-B's
    // own saved-grid duplicate handling (if any) is out of scope here.
    function updateAppendixIVDuplicateGuard() {        
        // Bug fix 2026-09-14 (client report: "unique entry for domain+scheme+sub-scheme" for
        // IV/IV-A/IV-B) - IV-B's own form was never included here, so the live client-side
        // duplicate guard silently did nothing for it (server-side enforcement still applied, but
        // Save never got hidden/disabled ahead of time the way IV/IV-A already do).
        var form = document.getElementById("appendixIVForm") || document.getElementById("appendixIVAForm") || document.getElementById("appendixIVBForm");
        if (!form) {
            return;
        }

        // Bug fix 2026-09-14 (client report, screenshot: Save/Modify button missing entirely in
        // Edit on IV/IV-A/IV-B) - these 3 forms all reparent their own drawer to document.body
        // (see appendix-iv-drawer.js's own header comment), and each one's reparent function only
        // cleans up STALE COPIES OF ITS OWN drawer id - it has no knowledge of the other two
        // appendixes' drawers. Since all 3 share the exact same field ids
        // (NewRecord_Id/NewRecord_SchemeId/NewRecord_SubSchemeId), a leftover orphaned drawer from
        // a PREVIOUSLY visited sibling appendix (e.g. an old #vaDrawer still sitting in
        // document.body after navigating IV-A -> IV) could make a global document.getElementById
        // resolve to that stale node's Id field instead of the current form's - reading a wrong/
        // blank currentId broke the "exclude my own row" check below, so editing a record whose
        // Scheme(+Sub-Scheme) matched its own saved row looked like a duplicate of itself and hid
        // the Submit button. Scoped to `form` now, same fix already applied to
        // lockSelectForEditScoped/populateAppendixIVEntryForm/populateSchemeGradedEntryForm.
        var idField = form.querySelector("#NewRecord_Id");
        var schemeSelect = form.querySelector("#NewRecord_SchemeId");
        var subSchemeSelect = form.querySelector("#NewRecord_SubSchemeId");
        var submitButton = form.querySelector('button[type="submit"]:not([formaction])');
        if (!schemeSelect || !submitButton) {
            return;
        }

        var currentId = idField ? idField.value : "";
        var schemeId = schemeSelect.value || "";
        // Bug fix 2026-09-14 (client report: "unique entry for domain+scheme+sub-scheme" broken):
        // this used to treat ANY disabled Sub-Scheme select as "no sub-scheme" - correct for the
        // genuine "this Scheme has no Sub-Schemes" placeholder state, but wrong now that Edit mode
        // also disables it via lockSelectForEditScoped while it legitimately holds a real value.
        // Distinguish the two: a select disabled specifically for that lock (marked with the
        // appendix-locked-for-edit class) still contributes its real .value to the duplicate key;
        // any other disabled state (the no-sub-schemes placeholder) still counts as blank.
        var subSchemeId = subSchemeSelect
            ? ((subSchemeSelect.disabled && !subSchemeSelect.classList.contains("appendix-locked-for-edit")) ? "" : (subSchemeSelect.value || ""))
            : "";

        // Client requirement 2026-08-27: "Hide div id=appendixIVDuplicateWarning" - the inline red
        // warning text is no longer shown at all, only the Submit button itself hides/shows. Kept
        // as a real (permanently hidden) element rather than removed outright, in case anything
        // else ever needs to target #appendixIVDuplicateWarning by id.
        var warning = form.querySelector("#appendixIVDuplicateWarning");
        if (!warning) {
            warning = document.createElement("p");
            warning.id = "appendixIVDuplicateWarning";
            warning.className = "text-sm font-semibold text-red-600 text-center mt-2 hidden";
            warning.textContent = "Data for this Scheme/Sub-Scheme already exists for this Demand.";
            submitButton.parentElement.insertBefore(warning, submitButton.nextSibling);
        }
        warning.classList.add("hidden");

        if (!schemeId) {
            submitButton.classList.remove("hidden");
            return;
        }

        // Scoped to this form's own grid body (ivGridBody/ivaGridBody/ivbGridBody), not a global
        // document.querySelectorAll - same stale-DOM reasoning as the field lookups above.
        var gridBodyId = form.id === "appendixIVForm" ? "ivGridBody" : (form.id === "appendixIVAForm" ? "ivaGridBody" : "ivbGridBody");
        var gridBody = document.getElementById(gridBodyId);
        var isDuplicate = Array.prototype.some.call((gridBody || document).querySelectorAll("tr[data-scheme-id]"), function (row) {
            if (row.getAttribute("data-scheme-id") !== schemeId) {
                return false;
            }
            if ((row.getAttribute("data-sub-scheme-id") || "") !== subSchemeId) {
                return false;
            }
            var rowIdEl = row.querySelector("[data-id]");
            var rowId = rowIdEl ? rowIdEl.getAttribute("data-id") : null;
            return !(currentId && rowId === currentId);
        });
       //Comment below code to  show Save/Modify button 
       // submitButton.classList.toggle("hidden", isDuplicate);
    }

    // ----------------------------------------------------------------------------------------
    // Submit-time validation dialog engine
    // ----------------------------------------------------------------------------------------

    // "Submit button should tell all validations at once in a single dialog box" (client testing
    // feedback, 2026-08-24) - the browser's native constraint-validation UI only surfaces one
    // invalid field at a time (fix it, resubmit, hit the next one). Intercepted at the submit
    // button's click (capture phase, so it runs before the browser's own default-action validation
    // would) rather than the form's "submit" event, because an invalid form never dispatches
    // "submit" at all - there'd be nothing to listen for. Skips buttons that opt out of validation
    // (formnovalidate - Freeze/Nil-Data) so those keep working exactly as before.
    function humanizeFieldName(el) {
        var label = el.closest("label");
        if (label) {
            var span = label.querySelector("span");
            if (span && span.textContent.trim()) {
                return span.textContent.trim();
            }
        }
        // Client requirement 2026-08-31 (Appendix III: "change validation message by showing
        // names of controls to select value example Select Scheme, Sub-Scheme"): several
        // appendices (III's Balance Type/Scheme/Sub-Scheme, VII-B's Departmental Commercial
        // Undertaking/Type of Transaction) lay their entry fields out as a plain 2-column
        // <table><tr><td>Field Name</td><td><select>/<input></td></tr>, not the <label><span>
        // wrapper the branch above reads - falls back to that preceding cell's own text (trimmed
        // of a trailing "*" some views use to mark a field visually required) before falling
        // through to the raw property-name derivation below.
        var cell = el.closest("td");
        var prevCell = cell && cell.previousElementSibling && cell.previousElementSibling.tagName === "TD"
            ? cell.previousElementSibling
            : null;
        // Only trust the preceding cell as a label if it holds plain text, not another field's own
        // input - a wide multi-column data-entry row (every <td> is its own input, labelled by a
        // <th> in <thead> instead) would otherwise borrow whatever value happens to be sitting in
        // the PREVIOUS field as this field's "label".
        if (prevCell && !prevCell.querySelector("input, select, textarea") && prevCell.textContent.trim()) {
            return prevCell.textContent.trim().replace(/\*$/, "").trim();
        }
        var raw = (el.name || el.id || "field").replace(/^NewRecord[_.]/, "");
        var humanized = raw.replace(/([a-z0-9])([A-Z])/g, "$1 $2").trim();
        // Bug report 2026-08-27 (Appendix VII-B, no explicit <label>/span wrapper here so this
        // fallback is what actually ran): a field bound to "...Id" (e.g. NewRecord.MajorHeadId,
        // the underlying FK column) surfaced to the user as "Major Head Id" - meaningless to
        // someone who never sees or thinks in terms of the numeric id. Trailing " Id" is always
        // this ASP.NET Core FK-property-naming convention, never a real word the user typed a
        // label for, so it's safe to strip generically here rather than patching one view's markup
        // at a time - fixes every other appendix's *Id field the same way, not just this one.
        return humanized.replace(/\s+Id$/, "");
    }

    // Also catches a fully blank submission (client testing feedback, 2026-08-24: "Empty record
    // submit, it should [show] validations in Dialog Box, All Appendixes"). Several appendices
    // (X, I) bind a list of per-row fields where every individual input is deliberately optional
    // (only rows the user actually touched should save - see SaveAppendixX's hasAnyValue skip),
    // so form.checkValidity() alone passes on a totally empty submit and the save silently no-ops
    // instead of telling the user anything went wrong.
    function isFormCompletelyBlank(form) {
        // Two different questions, not one: "is there anything the user could still type into"
        // (hasAnyFillable - excludes disabled/readOnly, matches every prior use of this function)
        // and "is there already a real value sitting in this form" (anyFilled - must NOT exclude
        // disabled/readOnly, or a page like Appendix I-A that auto-fills its prior-year BE boxes
        // read-only from a server lookup gets misclassified as "completely blank" the moment the
        // user hasn't typed anything new yet themselves, even though real data is already showing
        // on screen. Bug report 2026-08-27: that misclassification skipped this function's own
        // event.preventDefault() and fell straight through to the browser's native one-field-at-a-
        // time validation bubble instead of any dialog at all.)
        var hasAnyFillable = false;
        var anyFilled = false;
        Array.prototype.forEach.call(form.querySelectorAll("input, select, textarea"), function (el) {
            if (el.type === "hidden" || el.type === "submit" || el.type === "button") {
                return;
            }
            if (!el.disabled && !el.readOnly) {
                hasAnyFillable = true;
            }
            if (el.tagName === "SELECT") {
                anyFilled = anyFilled || !!el.value;
            } else if (el.type === "checkbox" || el.type === "radio") {
                anyFilled = anyFilled || el.checked;
            } else {
                anyFilled = anyFilled || el.value.trim() !== "";
            }
        });
        return hasAnyFillable && !anyFilled;
    }

    // "Default value 0.00, not null value is allowed in numeric fields. If user leaves numeric
    // field textbox blank, default 0.00 should be filled and saved into db" (client testing
    // feedback, 2026-08-25) - applied to every appendix, generically, until reversed 2026-08-31
    // ("all appendixes including appendix 1: if user enters blank value, and submit, it should
    // ask user to enter value and highlight the cell") - a blank `required` numeric field is now
    // a validation failure everywhere, not an auto-0.00. No longer called anywhere; kept in case a
    // future appendix genuinely wants this exact default back.
    function fillBlankNumericFieldsWithZero(form) {
        Array.prototype.forEach.call(form.querySelectorAll('input[type="number"]'), function (el) {
            if (!el.disabled && !el.readOnly && el.value.trim() === "") {
                el.value = "0.00";
            }
        });
    }

    // Client requirement 2026-08-31: every appendix's Freeze button (identified by its
    // formaction, identical across all 19 appendix views - see FreezeAppendixTemplate) is easy to
    // spot without needing a new data attribute on every one of those views.
    function isFreezeButton(btn) {
        var formaction = btn.getAttribute("formaction") || "";
        return formaction.indexOf("FreezeAppendixTemplate") !== -1;
    }

    // Same spot-by-formaction convention as isFreezeButton above, for the Nil button being added
    // to VI-A/VI-B/VI-C (2026-09-08). Formaction-based detection also picks up AppendixI/I-A's
    // pre-existing Nil button, which - like Freeze before this file's own 2026-08-31 fix - marks
    // itself with a per-BUTTON data-confirm/data-confirm-label attribute that the generic
    // form-level data-confirm handler in pre-budget-meeting.js never reads (that handler only
    // checks the FORM's own data-confirm, and Nil shares its form with Submit/Reset, which has
    // none) - so today's Nil buttons submit with no confirmation dialog at all. Fixed here the
    // same way Freeze was fixed, for every current and future Nil button at once.
    function isNilButton(btn) {
        var formaction = btn.getAttribute("formaction") || "";
        return formaction.indexOf("SetNilSubmission") !== -1;
    }

    // Client requirement 2026-09-17: "click on Modify Button, Modal dialog will show ... Title:
    // Modification of Record. Message: 'Are you sure you want to modify?'" - detected the same way
    // enterAppendixEditMode (PreBudget-control-binding.js) marks a submit button as being in Edit
    // mode: its own label text was swapped from "Save Record"/"Submit" to "Modify" (no separate
    // formaction of its own like Freeze/Nil - Modify shares the plain Save/Submit action).
    function isModifyButton(btn) {
        return /\bModify\b/i.test((btn.textContent || "").trim());
    }

    // Collects the same "Select {Field}"/"Enter values for A, B, C" clauses the normal Submit
    // validation dialog uses (client wording, 2026-08-25) and applies the same .field-missing red
    // highlight site.js's initRequiredFieldHighlight() already uses everywhere else - "validation
    // failed text boxes should be red as already in place for multiple appendixes" (client
    // requirement 2026-08-31). `extraInvalid(el)` lets the caller flag additional fields beyond
    // native checkValidity() (Freeze's own "0.00 counts as missing" rule - see isFreezeButton
    // below).
    function collectValidationClauses(form, extraInvalid) {
        var selectClauses = [];
        var enterValuesFields = [];
        Array.prototype.forEach.call(form.querySelectorAll("input, select, textarea"), function (el) {
            var invalid = (el.willValidate && !el.validity.valid) || (extraInvalid && extraInvalid(el));
            if (el.classList) {
                el.classList.toggle("field-missing", !!invalid && !el.disabled);
            }
            if (!invalid) {
                return;
            }
            if (el.tagName === "SELECT") {
                selectClauses.push("Select " + humanizeFieldName(el));
            } else {
                enterValuesFields.push(humanizeFieldName(el));
            }
        });

        var clauses = selectClauses.slice();
        if (enterValuesFields.length) {
            clauses.push("Enter values for " + enterValuesFields.join(", "));
        }
        return clauses;
    }

    function showValidationDialog(clauses) {
        if (window.openInfoDialog) {
            window.openInfoDialog("Please Enter required Fields", clauses.join(", "));
        } else {
            alert("Please Enter required Fields: " + clauses.join(", "));
        }
    }

    // Client requirement 2026-09-10: drop the aggregated "Please Enter required Fields" dialog
    // and let the browser show its own per-control constraint-validation bubble ("Please fill
    // out this field.") against each offending input instead. `extraInvalidEls` are the fields
    // that fail one of this app's stricter-than-HTML5 rules (Freeze's "0.00 counts as missing",
    // VI-D's explicit-0, Appendix X's partially-filled row); they get a temporary
    // setCustomValidity so reportValidity() aims a bubble at them too, cleared on the field's
    // next input/change so nothing stays stuck invalid. Returns form.reportValidity()'s result.
    function reportRequiredFieldsNatively(form, extraInvalidEls) {
        (extraInvalidEls || []).forEach(function (el) {
            if (!el || el.disabled || el.readOnly) {
                return;
            }
            el.setCustomValidity("Please fill out this field.");
            if (!el.__nativeValidityClearBound) {
                el.__nativeValidityClearBound = true;
                var clear = function () { el.setCustomValidity(""); };
                el.addEventListener("input", clear);
                el.addEventListener("change", clear);
            }
        });
        return form.reportValidity();
    }

    // ----------------------------------------------------------------------------------------
    // Event wiring
    // ----------------------------------------------------------------------------------------

    function attachListeners() {
        container.addEventListener("input", function (event) {
            var input = event.target;
            if (autonomousBodyNameCheckIds.indexOf(input.id) === -1) {
                return;
            }

            if (autonomousBodyNameCheckTimers[input.id]) {
                clearTimeout(autonomousBodyNameCheckTimers[input.id]);
            }
            autonomousBodyNameCheckTimers[input.id] = setTimeout(function () {
                checkAutonomousBodyNameDuplicate(input);
            }, 300);
        });

        // Re-checks both Name fields when the Add form's own Demand dropdown changes, since "duplicate"
        // is scoped per-Demand - a name typed before picking a Demand (or picked a different one) needs
        // re-validating against the newly selected Demand's cache.
        container.addEventListener("change", function (event) {
            if (event.target.id !== "addAutonomousBodyDemandId") {
                return;
            }
            ["addAutonomousBodyName", "addAutonomousBodyHName"].forEach(function (id) {
                var input = document.getElementById(id);
                if (input && input.value.trim()) {
                    checkAutonomousBodyNameDuplicate(input);
                }
            });
        });

        // On `document`, not `container` (client report 2026-09-10: "Freeze not working properly on
        // all appendixes, VI-A perfect"). The Freeze/Nil confirm-then-submit below only ran for
        // buttons the container-scoped listener could see; the same container-vs-document
        // reparenting issue already forced this file's input/keydown listeners onto `document`.
        // Every guard here (type="submit" + btn.form + the isFreeze/isNil formaction checks) keeps
        // it safe for the whole (PreBudget-only) page.
        document.addEventListener("click", function (event) {
            var btn = event.target.closest('button[type="submit"]');
            if (!btn) {
                return;
            }
            // Freeze/Nil moved out of the drawer to sit next to Add (2026-09-08, designer
            // markup) - they're no longer nested inside <form>, only associated with it via the
            // HTML `form="..."` attribute, so closest("form") would return null for them (same
            // sibling-not-ancestor bug class already fixed for the row action-menu's Delete
            // button in appendix-vi*-drawer.js). `btn.form` is the standards-based way to resolve
            // a submit button's associated form either way - nested or by the form attribute.
            var form = btn.form;
            if (!form) {
                return;
            }

            var isFreeze = isFreezeButton(btn);
            var isNil = isNilButton(btn);
            // Client requirement 2026-09-17: Modify still runs every normal validation check below -
            // only once the form is confirmed valid does it need the extra "Are you sure you want to
            // modify?" confirmation before actually submitting (unlike Freeze/Nil, which confirm
            // BEFORE validating and use their own separate validation rules).
            var isModify = !isFreeze && !isNil && isModifyButton(btn);
            if (isModify) {
                event.preventDefault();
            }

            // Client requirement 2026-08-31: "Freeze data button click should show Modal Dialog box
            // with Heading: Freezing Data, Text: Are you sure to Freeze Data? ... Yes and No buttons.
            // On click Yes, it should Freeze data ... on cancel, it should not do anything." Was
            // already marked formnovalidate + data-confirm/data-confirm-label on every appendix's
            // Freeze button, but nothing ever actually read those two attributes (the only data-confirm
            // handler in this file checks the FORM's attribute, not the submitter BUTTON's - Freeze's
            // shares the same <form> as Submit/Reset, which has no data-confirm of its own) - clicking
            // Freeze froze immediately, with no confirmation and no validation at all. Handled here,
            // ahead of the formnovalidate skip below, specifically for Freeze.
            if (isFreeze) {
                event.preventDefault();
                // Bug found 2026-09-08, while relocating VI-A/B/C/D/E's Freeze button from the
                // drawer footer to the page header (designer markup): the blank/0.00 validation
                // below reads the CURRENT state of this <form>'s own fields - meaningful when
                // Freeze sat inside the same form as the entry fields being frozen (Appendix I/
                // I-A/III-A's fixed-grid forms, where the button is still nested inside <form>
                // today), but the VI-A..E family's Freeze button is now only form-associated via
                // the HTML form="..." attribute while that form (its Add/Edit drawer) sits closed
                // and blank most of the time - so this check was permanently blocking every
                // header Freeze click with "please fill all necessary fields" for a form the user
                // was never trying to submit. Only run it when the button is a real DOM
                // descendant of the form it's freezing.
                var freezeButtonIsInsideForm = form.contains(btn);
                window.openConfirmDialog("Freezing Data", "Are you sure to Freeze Data?", "Yes", function () {
                    if (freezeButtonIsInsideForm) {
                        // "If blank or 0.00 values are being stored, its validation fails" - unlike a
                        // normal Submit (blank numeric fields silently default to 0.00, client
                        // requirement 2026-08-25), freezing treats blank AND already-0.00 numeric
                        // fields as missing, so fillBlankNumericFieldsWithZero is deliberately NOT
                        // called on this path.
                        var freezeMissing = function (el) {
                            return el.type === "number" && !el.disabled && !el.readOnly &&
                                (el.value.trim() === "" || parseFloat(el.value) === 0);
                        };
                        collectValidationClauses(form, freezeMissing);
                        var freezeInvalidEls = Array.prototype.filter.call(
                            form.querySelectorAll("input, select, textarea"), freezeMissing);
                        if (!reportRequiredFieldsNatively(form, freezeInvalidEls)) {
                            return;
                        }
                    }
                    submitFragmentForm(form, btn);
                }, false, "No");
                return;
            }

            // Nil button (2026-09-08, VI-A/VI-B/VI-C - see isNilButton above): same
            // formnovalidate-submit-button-sharing-a-form situation as Freeze, needs the same
            // explicit confirm-then-submit handling rather than the generic form-level
            // data-confirm path (which can't see a per-button attribute).
            if (isNilButton(btn)) {
                event.preventDefault();
                window.openConfirmDialog(
                    "Mark as Nil",
                    "Mark this Appendix as Nil for this Demand and Financial Year? All fields will be disabled.",
                    "Mark as Nil",
                    function () { submitFragmentForm(form, btn); },
                    false,
                    "Cancel"
                );
                return;
            }

            if (btn.hasAttribute("formnovalidate")) {
                return;
            }

            if (isFormCompletelyBlank(form)) {
                event.preventDefault();
                // Client requirement 2026-09-10: no "Nothing to submit" modal here either - show
                // the browser's own per-field bubble. reportValidity() flags the first required
                // field on a blank form; if the form happens to have no required fields, still
                // block the empty submit and put a bubble on the first editable control so the
                // click gives per-control feedback rather than silently doing nothing.
                if (!reportRequiredFieldsNatively(form, [])) {
                    return;
                }
                var firstField = form.querySelector(
                    'input:not([type="hidden"]):not([disabled]):not([readonly]), select:not([disabled]), textarea:not([disabled]):not([readonly])');
                if (firstField) {
                    reportRequiredFieldsNatively(form, [firstField]);
                }
                return;
            }

            // Client requirement 2026-08-31 (Appendix X: "0 value not allowed for the rows that are
            // updating or adding... highlight mandatory fields like other appendixes"): unlike every
            // other appendix, this form has no single NewRecord - it's 3 fixed sub-head rows (see
            // AppendixXViewModel.FixedSubHeadNames), each independently create-or-update
            // (PreBudgetMeetingController.AppendixX.cs). A row left completely untouched is legitimately
            // skipped server-side (nothing to save there), but a row with ANY value must have EVERY
            // value, and 0 doesn't count as a real value either - none of these inputs are marked
            // `required` (a blank row is fine), so this can't reuse the generic required/checkValidity
            // path and needs its own per-row check.
            if (form.id === "appendixXForm") {
                var xInvalidEls = [];
                Array.prototype.forEach.call(form.querySelectorAll("tbody tr[data-sub-head]"), function (row) {
                    var inputs = Array.prototype.slice.call(row.querySelectorAll('input[type="number"]'));
                    var anyFilled = inputs.some(function (el) { return el.value.trim() !== ""; });
                    if (!anyFilled) {
                        inputs.forEach(function (el) { el.classList.remove("field-missing"); });
                        return;
                    }
                    inputs.forEach(function (el) {
                        var invalid = el.value.trim() === "" || parseFloat(el.value) === 0;
                        el.classList.toggle("field-missing", invalid);
                        if (invalid) {
                            xInvalidEls.push(el);
                        }
                    });
                });
                if (xInvalidEls.length) {
                    event.preventDefault();
                    reportRequiredFieldsNatively(form, xInvalidEls);
                    return;
                }
                // Every row is either fully valid or intentionally blank (and skippable) - nothing
                // else in this generic handler applies to X's row shape (no `required` fields, no
                // cascade selects), so submit now rather than falling into the generic
                // zeroOrBlankInvalid scan below, which doesn't know a blank SKIPPED row is fine.
                return;
            }

            // Client requirement 2026-08-31: "all appendixes including appendix 1: if user enters
            // blank value, and submit, it should ask user to enter value and highlight the cell."
            // Reverses the 2026-08-25 "blank numeric field defaults to 0.00" rule - fillBlankNumericFieldsWithZero
            // (still defined above, unused as of this change) used to run here, silently patching
            // every blank numeric field to "0.00" BEFORE form.checkValidity() ever ran, which meant a
            // `required` numeric field could never actually fail validation on being left blank - it
            // just saved as 0 instead. Removed entirely: a blank `required` numeric field now fails
            // native constraint validation like every other required field, and gets the same
            // .field-missing red highlight + "Please Enter required Fields" dialog via
            // collectValidationClauses below - no separate plumbing needed, this was already built
            // for every non-numeric required field.
            //
            // VI-D additionally treats an EXPLICIT 0 as missing too (its own stricter rule, client
            // requirement earlier the same day) - every other appendix still allows a genuinely-typed
            // 0 to save. (Appendix X has the same stricter 0-or-blank rule but already returned above
            // via its own row-by-row check - this generic path never runs for it.)
            var zeroOrBlankInvalid = form.id === "appendixVIDForm"
                ? function (el) {
                      return el.type === "number" && !el.disabled && !el.readOnly &&
                          (el.value.trim() === "" || parseFloat(el.value) === 0);
                  }
                : null;
            var hasZeroOrBlank = zeroOrBlankInvalid &&
                Array.prototype.some.call(form.querySelectorAll("input, select, textarea"), zeroOrBlankInvalid);

            // Client report 2026-08-31 (Appendix VII-B: "data without filling data ... should not
            // saved"): a `required` cascade dropdown (VII-B's Major Head, VI-B's Scheme) starts out
            // `disabled` in the markup until the user picks the field it cascades from - and a
            // `disabled` field is barred from HTML5 constraint validation entirely, so
            // form.checkValidity() below reports the form valid even while that dropdown is still on
            // its blank "Select..."/"Loading..." placeholder (confirmed live: DemandId 1042's
            // Appendix_VIIB_CommercialUndertakingReceipts rows 1062/1063 saved with MajorHeadId NULL).
            // Excludes `.appendix-locked-for-edit` selects - those are deliberately disabled with a
            // real, already-chosen value carried through via a hidden mirror input (lockSelectForEdit),
            // not a blank cascade placeholder.
            var blankCascadeSelects = Array.prototype.filter.call(
                form.querySelectorAll("select[required]"),
                function (el) { return el.disabled && !el.classList.contains("appendix-locked-for-edit"); }
            );

            if (form.checkValidity() && !blankCascadeSelects.length && !hasZeroOrBlank) {
                // Client requirement 2026-09-17: the form is valid and ready to submit - for a plain
                // Add/Save this just returns and lets the native submit proceed unchanged. For Modify
                // (isModify, preventDefault()'d above), show the confirm dialog first; the actual
                // submit only happens from its Yes callback, using the same submitFragmentForm path
                // Freeze/Nil already use for a script-driven (non-native) submit.
                if (isModify) {
                    window.openConfirmDialog("Modification of Record", "Are you sure you want to modify?", "Yes", function () {
                        submitFragmentForm(form, btn);
                    }, false, "No");
                }
                return;
            }

            event.preventDefault();
            // Client requirement 2026-09-10: show the browser's own per-control bubble
            // ("Please fill out this field.") on each offending input instead of the old
            // aggregated "Please Enter required Fields" dialog. Keep applying the .field-missing
            // red highlight (collectValidationClauses does that as a side effect).
            collectValidationClauses(form, zeroOrBlankInvalid);
            // VI-D's rule also treats an explicit 0 as missing - hand those enabled number
            // inputs to the native reporter so they get a bubble like any blank required field.
            var extraInvalidEls = [];
            if (zeroOrBlankInvalid) {
                Array.prototype.forEach.call(form.querySelectorAll('input[type="number"]'), function (el) {
                    if (zeroOrBlankInvalid(el)) {
                        extraInvalidEls.push(el);
                    }
                });
            }
            var nativelyValid = reportRequiredFieldsNatively(form, extraInvalidEls);
            // A `required` cascade <select> still `disabled` on its placeholder is invisible to
            // native constraint validation (disabled controls don't validate) - if that's the
            // only thing still wrong, name it explicitly since it can't host its own bubble.
            if (nativelyValid && blankCascadeSelects.length) {
                showValidationDialog(blankCascadeSelects.map(function (el) {
                    return "Select " + humanizeFieldName(el);
                }));
            }
        }, true);

        container.addEventListener("submit", function (event) {
            var form = event.target;
            if (form.id !== "addAutonomousBodyForm" && form.id !== "requestAutonomousBodyForm") {
                return;
            }

            var blocked = autonomousBodyNameCheckIds.some(function (id) {
                return form.querySelector("#" + id) && autonomousBodyNameIsDuplicate[id];
            });
            if (blocked) {
                event.preventDefault();
            }
        });

        // Blocks the specific keys that HTML5's own number-input validation lets through but which
        // this codebase never wants in a budget amount field: e/E (scientific notation), +/- (sign
        // - every amount here is min="0", never negative), and a second "." once one is already
        // present (native number inputs otherwise happily let you type "1.2.3" then just report the
        // whole field as invalid rather than blocking the second dot as you type it).
        // Bound to `document`, not `container` (client report 2026-09-10: "e" still typeable in the
        // redesigned drawers) - same reason the "input" sanitizer below was widened on 2026-09-08:
        // a redesigned appendix's drawer form is reparented to document.body while open, so a
        // container-scoped listener never sees its keystrokes. The `input.type === "number"` guard
        // keeps this safe for the whole document.
        document.addEventListener("keydown", function (event) {
            var input = event.target;
            if (!(input instanceof HTMLInputElement) || input.type !== "number") {
                return;
            }

            // Appendix IV-A's ActualsUptoSept (client requirement 2026-08-27: "Actuals upto 9/2025 can
            // be negative") opts out of the sign block via data-allow-negative - every other amount
            // field in this app is still never allowed a "-".
            var blockedKeys = input.hasAttribute("data-allow-negative") ? ["e", "E", "+"] : ["e", "E", "+", "-"];
            if (blockedKeys.indexOf(event.key) !== -1) {
                event.preventDefault();
                return;
            }

            if (event.key === "." && input.value.indexOf(".") !== -1) {
                event.preventDefault();
            }
        });

        // Sanitizes pasted text the same way manual typing is sanitized above - a paste bypasses
        // keydown entirely, so without this a user could paste "abc1.234e5" straight past the keydown
        // guard. On `document` for the same reparented-drawer reason as the keydown guard above.
        document.addEventListener("paste", function (event) {
            var input = event.target;
            if (!(input instanceof HTMLInputElement) || input.type !== "number") {
                return;
            }

            if (!event.clipboardData) {
                return;
            }

            event.preventDefault();
            var allowNegative = input.hasAttribute("data-allow-negative");
            var pasted = sanitizeNumberString(event.clipboardData.getData("text"), allowNegative);
            var start = input.selectionStart == null ? input.value.length : input.selectionStart;
            var end = input.selectionEnd == null ? input.value.length : input.selectionEnd;
            var merged = sanitizeNumberString(input.value.slice(0, start) + pasted + input.value.slice(end), allowNegative);
            input.value = merged;
            var newCaret = Math.min(start + pasted.length, merged.length);
            input.setSelectionRange(newCaret, newCaret);
        });

        // Sanitizes every typed number field as-you-type, then hands off to
        // PreBudgetControlBinding.recalcForNumberInput so whichever appendix's live "Auto" display
        // depends on this field updates too - that dispatch itself is control-binding's concern (it
        // knows which appendix has which display), this listener's own job is just the sanitize rule.
        // Widened from `container` to `document` (2026-09-08, same reasoning as the edit-populate
        // dispatcher in PreBudget-control-binding.js): a redesigned appendix's drawer form is
        // reparented to document.body while open, so typing in it never reached a container-scoped
        // listener. Safe to widen - the only check here is `input.type === "number"`, and every
        // downstream recalc in recalcForNumberInput already re-scopes itself via
        // `input.closest("#appendixXForm")`.
        document.addEventListener("input", function (event) {
            var input = event.target;
            if (!(input instanceof HTMLInputElement) || input.type !== "number") {
                return;
            }

            var sanitized = sanitizeNumberString(input.value, input.hasAttribute("data-allow-negative"));
            // Native maxlength is ignored on type="number" - enforce it here (client 2026-09-10,
            // Appendix VI: "numeric text box ... upto 15 characters"). No other number field sets
            // maxlength today, so this is a no-op everywhere else.
            var numMaxLen = parseInt(input.getAttribute("maxlength"), 10);
            if (!isNaN(numMaxLen) && numMaxLen > 0 && sanitized.length > numMaxLen) {
                sanitized = sanitized.slice(0, numMaxLen);
            }
            if (sanitized !== input.value) {
                // Cursor position is naturally preserved when the sanitized value is the same length
                // up to the caret (the common case - removing one disallowed character just typed at
                // the end); a full selection-range restore isn't worth the complexity for a numeric
                // budget field where the caret is almost always at the end anyway.
                var caret = input.selectionStart;
                input.value = sanitized;
                if (caret !== null && caret <= sanitized.length) {
                    input.setSelectionRange(caret, caret);
                }
            }

            window.PreBudgetControlBinding.recalcForNumberInput(input);
        });

        // Client requirement 2026-09-02: on blur, Appendix V-C's Name field is normalized -
        // whitespace-collapsed, trimmed, first letter capitalized - matching exactly what
        // AppendixVCController.NormalizeName does server-side, so what the user sees typed is what
        // actually gets compared/saved (no surprise mismatch between the visible text and the stored
        // value). `focusout` is used (not `blur`, which doesn't bubble) since every appendix listener
        // here is delegated on the shared fragment container.
        container.addEventListener("focusout", function (event) {
            if (event.target.id !== "VCNewRecord_Name") {
                return;
            }
            var collapsed = event.target.value.trim().replace(/\s+/g, " ");
            event.target.value = collapsed.length === 0 ? collapsed : collapsed.charAt(0).toUpperCase() + collapsed.slice(1);
        });
    }

    function init(ctx) {
        container = ctx.container;
        urls = ctx.urls;
        fetchJson = ctx.fetchJson;
        submitFragmentForm = ctx.submitFragmentForm;
        attachListeners();
    }

    // ----------------------------------------------------------------------------------------
    // Allocation page (PreBudgetMeeting/Allocation/REMeetingAllocation.cshtml) - extracted
    // 2026-09-03 alongside the appendix split above, same validation/control-binding boundary.
    // See PreBudget-control-binding.js's initAllocationControls() for that page's DOM wiring;
    // this is its one validation rule. Attaches directly to `document` (plain server-rendered
    // page, no `container`/init() context needed) - safe to call unconditionally, since the
    // selector below already no-ops if no matching date input is on the page.
    // ----------------------------------------------------------------------------------------

    function initAllocationValidation() {
        // Client requirement 2026-09-01: grid edit-row Target Date is required, and that must be
        // enforced at Save/Modify click time with a message asking the user to enter a date - the
        // modification must not go through when it's blank. The date <input> already carries
        // `required`; clicking a submit button for an invalid form-associated field fires the
        // browser's own constraint-validation "invalid" event on that field *before* the "submit"
        // event would ever fire (submission never reaches "submit" at all while the form is
        // invalid), so a "submit" listener here would never run for this case. Listening for
        // "invalid" instead, and calling preventDefault() on it, only suppresses the browser's own
        // native validation bubble - the submission stays blocked exactly as before - so it can be
        // replaced with this app's own shared dialog convention (dialogs.js's openInfoDialog,
        // loaded globally via _Layout.cshtml) instead of the inconsistent native tooltip.
        document.querySelectorAll('input[type="date"][form^="editForm-"]').forEach(function (dateInput) {
            dateInput.addEventListener("invalid", function (event) {
                event.preventDefault();
                if (window.openInfoDialog) {
                    window.openInfoDialog("Target Date required", "Please enter a Target Date before saving.");
                }
            });
        });
    }

    return {
        init: init,
        sanitizeNumberString: sanitizeNumberString,
        isFormCompletelyBlank: isFormCompletelyBlank,
        fillBlankNumericFieldsWithZero: fillBlankNumericFieldsWithZero,
        updateAppendixIVDuplicateGuard: updateAppendixIVDuplicateGuard,
        initAllocationValidation: initAllocationValidation
    };
})();
