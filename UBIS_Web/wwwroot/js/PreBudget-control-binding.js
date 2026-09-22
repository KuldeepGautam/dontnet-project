// Extracted 2026-09-03 from pre-budget-meeting.js (client requirement: separate validation and
// control-binding concerns so a future UX/theme change to any appendix's controls doesn't require
// touching validation logic, and vice versa). This file owns every piece of DOM "wiring" for the
// PreBudget appendix forms: dropdown cascades (Scheme/SubScheme/Category/Major Head), the
// previous-year reference auto-loaders (fetch + populate + lock), live recalculated display
// fields, the Edit-button -> entry-form populate functions for all 22 appendices, the Edit-mode
// select-lock/unlock mechanism, and the generic row-highlight/Reset-cleanup plumbing shared across
// every appendix. Pure validation rules (required/duplicate/format checks, the submit-time
// validation dialog) live in PreBudget-validation.js instead - see that file's own header comment
// for the split rationale. Both files are plain global-namespace modules (window.PreBudgetControlBinding
// / window.PreBudgetValidation), loaded before pre-budget-meeting.js's own <script> tag; that file
// remains the SPA-shell orchestrator (Demand/Appendix cascade navigation, AJAX fragment load/apply,
// session-expiry guard) and calls into both namespaces' init()/onFragmentApplied() entry points.
window.PreBudgetControlBinding = (function () {
    "use strict";

    var container, urls, fetchJson, demandSelect;

    // ----------------------------------------------------------------------------------------
    // Generic select helpers
    // ----------------------------------------------------------------------------------------

    // Auto-picks a dropdown's only real choice instead of leaving it on the "Select..." placeholder
    // (added 2026-08-03) - counts every <option> with a non-empty value; if exactly one exists,
    // selects it and fires a real "change" event (bubbling, so the existing delegated change
    // handlers on #partialViewContainer still cascade a dependent Scheme/SubScheme/etc. dropdown
    // exactly as if the user had picked it themselves). Deliberately does NOT gate on select.disabled
    // - populateSelect's callers re-populate a still-disabled "Loading…" select and only flip
    // disabled=false afterward, so checking it here would skip every AJAX-cascaded dropdown this is
    // mainly for. A still-genuinely-waiting cascade select (e.g. "Select a Category first") only
    // ever has its single placeholder option, which the empty-value filter below already excludes,
    // so it naturally never reaches the length===1 branch regardless of its disabled state.
    function autoSelectIfSingleOption(select) {
        if (!select) {
            return;
        }

        var realOptions = Array.prototype.filter.call(select.options, function (opt) {
            return opt.value !== "";
        });

        if (realOptions.length !== 1 || select.value === realOptions[0].value) {
            return;
        }

        select.value = realOptions[0].value;
        select.dispatchEvent(new Event("change", { bubbles: true }));
    }

    function autoSelectSingleOptionsIn(root) {
        Array.prototype.forEach.call(root.querySelectorAll("select"), autoSelectIfSingleOption);
    }

    // `selectedLabel` (bug fix, client testing feedback 2026-08-24 - "Major Head ddl not getting
    // bound"): a cascading dropdown's option list is scoped to whatever the current Scheme/Category
    // selection allows right now, which doesn't always include a value that was actually saved on
    // the record being edited (e.g. the saved Major Head no longer applies to the currently-selected
    // Scheme). Previously the select just silently landed on the placeholder in that case, appearing
    // to have lost the saved value. If selectedValue isn't among items, synthesize an extra option
    // for it (using selectedLabel if the caller has it, else the raw id) so the value always round-trips.
    function populateSelect(select, items, valueKey, labelKey, placeholder, selectedValue, selectedLabel) {
        var html = '<option value="">' + placeholder + "</option>";
        var foundSelected = false;
        items.forEach(function (item) {
            var value = String(item[valueKey]);
            var isSelected = selectedValue != null && String(selectedValue) === value;
            if (isSelected) {
                foundSelected = true;
            }
            html += '<option value="' + value + '"' + (isSelected ? " selected" : "") + ">" + item[labelKey] + "</option>";
        });
        if (!foundSelected && selectedValue != null && selectedValue !== "") {
            html += '<option value="' + selectedValue + '" selected>' + (selectedLabel || selectedValue) + "</option>";
        }
        select.innerHTML = html;
        autoSelectIfSingleOption(select);
    }

    // ----------------------------------------------------------------------------------------
    // Previous-year reference auto-loaders (fetch + populate + lock read-only)
    // ----------------------------------------------------------------------------------------

    // Appendix I-A's "Previous Year" Revenue/Capital BE boxes - pre-filled via
    // GetAppendixIAPreviousYearReference (resolved server-side from dbo.SBEData through the
    // Demand's PrevDemandId chain, per the client's reference SQL, 2026-08-07), then locked
    // read-only since they're historical fact. Revenue/Capital RE are deliberately NOT in this map
    // - the client's own note says "RE to be filled by user, BE data is auto-filled" - so those
    // boxes stay plain editable inputs, never touched by this function. If the prior year's demand
    // can't be resolved or has no SBEData rows (e.g. this Demand's first-ever filing), the BE boxes
    // are left blank and editable as a fallback.
    function loadAppendixIAPreviousYearReference(form) {
        var demandId = form.getAttribute("data-demand-id");
        if (!demandId) {
            return;
        }

        var fieldMap = {
            revenueBE: "NewRecord_PrevYrMinRevenueBE",
            capitalBE: "NewRecord_PrevYrMinCapitalBE"
        };

        fetchJson(urls.appendixIAPrevYearUrl + "?demandId=" + encodeURIComponent(demandId), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (data) {
                if (!data || !data.found) {
                    return;
                }

                Object.keys(fieldMap).forEach(function (key) {
                    var input = document.getElementById(fieldMap[key]);
                    if (!input || data[key] === null || data[key] === undefined) {
                        return;
                    }
                    input.value = data[key];
                    input.readOnly = true;
                    input.classList.add("bg-slate-100", "dark:bg-slate-800", "cursor-not-allowed");
                    input.title = "From Appendix I, financial year " + data.financialYear + " - not editable here.";
                });

                // Bug fix (client report 2026-08-27): these fields are set programmatically above,
                // which doesn't fire an "input" event, so the Total column's recalc listener never
                // ran and the row loaded with Total blank. totalBE itself now comes straight from
                // the server (EF-summed revenueBE + capitalBE) rather than being re-derived client
                // side; recalc still runs to also cover Total RE once the user starts typing it.
                var prevTotalBEDisplay = document.getElementById("appendixIAPrevTotalBE");
                if (prevTotalBEDisplay && data.totalBE !== null && data.totalBE !== undefined) {
                    prevTotalBEDisplay.value = data.totalBE;
                }
                recalcAppendixIATotals();
            })
            .catch(function () {
                // Leave the boxes blank/editable - same fallback as Appendix I having no prior row.
            });
    }

    // Appendix II's "Previous FY" Q1/Q2 Approved-QEP/Actuals boxes - same pre-fill-and-lock
    // treatment as Appendix I-A above. Approved-QEP is resolved server-side from dbo.T_QEPData
    // through the Demand's PrevDemandId chain (client reference SQL, 2026-08-07); Actuals has no
    // client-provided source and stays self-referential (this demand's own Appendix II row for the
    // prior year) - same JSON shape either way, this function doesn't need to know which is which.
    function loadAppendixIIPreviousYearReference(form) {
        var demandId = form.getAttribute("data-demand-id");
        if (!demandId) {
            return;
        }

        var fieldMap = {
            q1ApprovedQep: "NewRecord_Q1ApprovedQepPrevYear",
            q1Actuals: "NewRecord_Q1ActualsPrevYear",
            q2ApprovedQep: "NewRecord_Q2ApprovedQepPrevYear",
            q2Actuals: "NewRecord_Q2ActualsPrevYear"
        };

        fetchJson(urls.appendixIIPrevYearUrl + "?demandId=" + encodeURIComponent(demandId), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (data) {
                if (!data || !data.found) {
                    return;
                }

                Object.keys(fieldMap).forEach(function (key) {
                    var input = document.getElementById(fieldMap[key]);
                    if (!input || data[key] === null || data[key] === undefined) {
                        return;
                    }
                    input.value = data[key];
                    input.readOnly = true;
                    input.classList.add("bg-slate-100", "dark:bg-slate-800", "cursor-not-allowed");
                    input.title = "From Appendix II, financial year " + data.financialYear + " - not editable here.";
                });

                recalcAppendixIITotals();
            })
            .catch(function () {
                // Leave the boxes blank/editable - same fallback as no prior-year row existing.
            });
    }

    // Appendix IV/IV-A/IV-B's "Actuals upto Sept" reference boxes (IV added 2026-08-04, IV-A/IV-B
    // generalized from it the same day - all three entities share the identical Actuals/
    // ActualsUptoSeptPrevYear/BE/ActualsUptoSept field set) - keyed by Scheme+SubScheme (not just
    // DemandId, since these are one row per scheme) against that same Scheme/SubScheme's own row
    // for the prior year, in this same appendix. Called from the Scheme/SubScheme change handlers
    // below rather than once on fragment load, since the values depend on which Scheme is picked.
    // `label` is a short per-appendix key (e.g. "IV", "IV-A") both for the per-appendix sequence
    // guard below and for the input's title tooltip.
    var appendixSchemeRefRequestSeq = {};

    // `scopeEl` (added 2026-09-15, client report "BE/Actuals not loading in Appendix IV/IV-A"):
    // Appendix IV, IV-A, IV-B (and VI-B's own BE loader) all reuse the exact same element ids
    // (NewRecord_BE, NewRecord_Actuals, ...) by design - each appendix's own drawer form is the
    // only one visible/attached at a time in the normal case, but a plain `document.getElementById`
    // resolves to the FIRST matching id anywhere in the document, so if more than one of these
    // forms is ever present at once (a stale reparented drawer left over from switching appendixes,
    // a timing gap around a fragment reload, a browser back/forward-cache restore, etc.) the fetched
    // value can silently land in the wrong, invisible copy instead of the one on screen. Scoping the
    // lookup to the specific form element the event came from (`formEl.querySelector('#id')` only
    // searches that form's own descendants) removes this whole class of bug regardless of what left
    // the duplicate behind. Every call site below now passes its own `formEl`/`form`; `scopeEl`
    // defaults to `document` only so this stays backward-compatible if ever called without one.
    function loadAppendixSchemeActualsReference(label, url, demandId, schemeId, subSchemeId, scopeEl) {
        if (!demandId || !schemeId) {
            return;
        }
        var scope = scopeEl || document;

        // "actuals" is only ever populated for Appendix IV-A's endpoint (client report 2026-09-15:
        // "Actual 2024-2025 not coming from db") - IV/IV-B's own GetAppendixIV*PreviousYearReference
        // don't return that key, so this is a silent no-op for them (see the null/undefined guard
        // below) and their Actuals column stays manually entered exactly as before.
        var fieldMap = {
            actuals: "NewRecord_Actuals",
            actualsUptoSeptPrevYear: "NewRecord_ActualsUptoSeptPrevYear",
            actualsUptoSept: "NewRecord_ActualsUptoSept"
        };

        var fetchUrl = url + "?demandId=" + encodeURIComponent(demandId) +
            "&schemeId=" + encodeURIComponent(schemeId) +
            (subSchemeId ? "&subSchemeId=" + encodeURIComponent(subSchemeId) : "");

        // Clearing the previous selection's values here (not just resetting readOnly/classes)
        // matters because switching Scheme/SubScheme reuses the same 2 boxes - without this, a
        // scheme with no prior-year row would keep displaying the last scheme's stale figures.
        function resetField(input) {
            input.value = "";
            input.readOnly = false;
            input.classList.remove("bg-slate-100", "dark:bg-slate-800", "cursor-not-allowed");
            input.title = "";
        }

        Object.keys(fieldMap).forEach(function (key) {
            var input = scope.querySelector("#" + fieldMap[key]);
            if (input) {
                resetField(input);
            }
        });
        recalcAppendixIVCalculations(scope);

        // Sequence guard (per appendix, via `label`): switching Scheme/SubScheme quickly can fire
        // two overlapping fetches, and network timing gives no guarantee the older request's
        // response arrives first. Without this, a slow response for the previously-selected
        // SubScheme could land after (and overwrite) the fields the current SubScheme's own,
        // faster response already populated.
        var requestId = (appendixSchemeRefRequestSeq[label] = (appendixSchemeRefRequestSeq[label] || 0) + 1);

        fetchJson(fetchUrl, { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (data) {
                if (requestId !== appendixSchemeRefRequestSeq[label] || !data || !data.found) {
                    return;
                }

                Object.keys(fieldMap).forEach(function (key) {
                    var input = scope.querySelector("#" + fieldMap[key]);
                    if (!input || data[key] === null || data[key] === undefined) {
                        return;
                    }
                    input.value = data[key];
                    input.readOnly = true;
                    input.classList.add("bg-slate-100", "dark:bg-slate-800", "cursor-not-allowed");
                    input.title = "From Appendix " + label + ", financial year " + data.financialYear + " - not editable here.";
                });
                recalcAppendixIVCalculations(scope);
            })
            .catch(function () {
                // Already reset above - nothing further to do on a failed lookup.
            });
    }

    // Appendix V-A/V-B's historical reference boxes (added 2026-08-04) - same self-referential
    // prior-year lookup as above, but keyed by a single dropdown (Autonomous Body for V-A, Object
    // Head for V-B) rather than a Scheme/SubScheme pair, and pre-fills ALL 4 historical fields per
    // group (Actuals/ActualsUptoSeptPrevYear/BE/ActualsUptoSept) rather than just the 2
    // Actuals-upto-Sept ones - V-A/V-B's RE/NBE columns are this year's live proposals so stay
    // manual, but Actuals and BE are already-known historical fact same as the other 2.
    var previousYearRequestSeq = {};

    function loadPreviousYearFields(seqKey, url, queryParams, fieldMap) {
        function resetField(input) {
           // input.value = "";
            input.readOnly = false;
            input.classList.remove("bg-slate-100", "dark:bg-slate-800", "cursor-not-allowed");
            input.title = "";
        }

        Object.keys(fieldMap).forEach(function (key) {
            var input = document.getElementById(fieldMap[key]);
            if (input) {
                resetField(input);
            }
        });

        var requestId = (previousYearRequestSeq[seqKey] = (previousYearRequestSeq[seqKey] || 0) + 1);
        var qs = Object.keys(queryParams)
            .map(function (k) { return k + "=" + encodeURIComponent(queryParams[k]); })
            .join("&");

        fetchJson(url + "?" + qs, { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (data) {
                if (requestId !== previousYearRequestSeq[seqKey] || !data || !data.found) {
                    return;
                }

                Object.keys(fieldMap).forEach(function (key) {
                    var input = document.getElementById(fieldMap[key]);
                    if (!input || data[key] === null || data[key] === undefined) {
                        return;
                    }
                    input.value = data[key];
                    input.readOnly = true;
                    input.classList.add("bg-slate-100", "dark:bg-slate-800", "cursor-not-allowed");
                    input.title = "From financial year " + data.financialYear + " - not editable here.";
                });
            })
            .catch(function () {
                // Already reset above - nothing further to do on a failed lookup.
            });
    }

    var appendixVAFieldMap = {
        giaGeneralActuals: "VANewRecord_GiaGeneralActuals",
        giaGeneralActualsUptoSeptPrevYear: "VANewRecord_GiaGeneralActualsUptoSeptPrevYear",
        giaGeneralBE: "VANewRecord_GiaGeneralBE",
        giaGeneralActualsUptoSept: "VANewRecord_GiaGeneralActualsUptoSept",
        giaCcaActuals: "VANewRecord_GiaCcaActuals",
        giaCcaActualsUptoSeptPrevYear: "VANewRecord_GiaCcaActualsUptoSeptPrevYear",
        giaCcaBE: "VANewRecord_GiaCcaBE",
        giaCcaActualsUptoSept: "VANewRecord_GiaCcaActualsUptoSept",
        giaSalaryActuals: "VANewRecord_GiaSalaryActuals",
        giaSalaryActualsUptoSeptPrevYear: "VANewRecord_GiaSalaryActualsUptoSeptPrevYear",
        giaSalaryBE: "VANewRecord_GiaSalaryBE",
        giaSalaryActualsUptoSept: "VANewRecord_GiaSalaryActualsUptoSept"
    };

    var appendixVBFieldMap = {
        actuals: "VBNewRecord_Actuals",
        actualsUptoSeptPrevYear: "VBNewRecord_ActualsUptoSeptPrevYear",
        be: "VBNewRecord_BE",
        actualsUptoSept: "VBNewRecord_ActualsUptoSept"
    };

    function loadAppendixVAPreviousYearReference(demandId, autonomousBodyId) {
        if (!demandId || !autonomousBodyId) {
            return;
        }
        loadPreviousYearFields("va", urls.appendixVAPrevYearUrl, { demandId: demandId, autonomousBodyId: autonomousBodyId }, appendixVAFieldMap);
    }

    function loadAppendixVBPreviousYearReference(demandId, objectHeadId) {
        if (!demandId || !objectHeadId) {
            return;
        }
        loadPreviousYearFields("vb", urls.appendixVBPrevYearUrl, { demandId: demandId, objectHeadId: objectHeadId }, appendixVBFieldMap);
    }

    // Appendix VI family (added 2026-08-04) - same self-referential prior-year pre-fill via
    // loadPreviousYearFields, one field map + wrapper per appendix since each has a different key
    // and a different subset of "historical" fields (see the matching MVC actions'
    // GetAppendixVI*PreviousYearReference doc-comments for exactly which fields are historical vs.
    // this cycle's own live figures for each).
    var appendixVIFieldMap = { actuals: "NewRecord_Actuals", be: "NewRecord_BE", actualsUptoSept: "NewRecord_ActualsUptoSept" };
    var appendixVIAFieldMap = { totalRevenueY1: "NewRecord_TotalRevenueY1", totalRevenueY2: "NewRecord_TotalRevenueY2", totalRevenueY3: "NewRecord_TotalRevenueY3" };
    var appendixVIBFieldMap = { pendingLiabilityAsOnMarch31: "NewRecord_PendingLiabilityAsOnMarch31" };
    var appendixVICFieldMap = {
        accumulatedBalancePrevYear: "NewRecord_AccumulatedBalancePrevYear",
        accumulatedBalance: "NewRecord_AccumulatedBalance",
        actualExpenditureY1: "NewRecord_ActualExpenditureY1",
        actualExpenditureY2: "NewRecord_ActualExpenditureY2",
        actualExpenditureY3: "NewRecord_ActualExpenditureY3"
    };
    var appendixVIDFieldMap = { asOnMarch31: "NewRecord_AsOnMarch31", asOnJune30: "NewRecord_AsOnJune30" };
    var appendixVIEFieldMap = { corpusFundBalance1: "NewRecord_CorpusFundBalance1" };

    function loadAppendixVIPreviousYearReference(demandId, receiptTypeId) {
        if (!demandId || !receiptTypeId) {
            return;
        }
        loadPreviousYearFields("vi", urls.appendixVIPrevYearUrl, { demandId: demandId, receiptTypeId: receiptTypeId }, appendixVIFieldMap);
    }

    function loadAppendixVIAPreviousYearReference(demandId) {
        if (!demandId) {
            return;
        }
        loadPreviousYearFields("via", urls.appendixVIAPrevYearUrl, { demandId: demandId }, appendixVIAFieldMap);
    }

    function loadAppendixVIBPreviousYearReference(demandId, schemeId, subSchemeId) {
        if (!demandId || !schemeId) {
            return;
        }
        loadPreviousYearFields("vib", urls.appendixVIBPrevYearUrl,
            subSchemeId ? { demandId: demandId, schemeId: schemeId, subSchemeId: subSchemeId } : { demandId: demandId, schemeId: schemeId },
            appendixVIBFieldMap);
    }

    function loadAppendixVICPreviousYearReference(demandId, autonomousBodyId) {
        if (!demandId || !autonomousBodyId) {
            return;
        }
        loadPreviousYearFields("vic", urls.appendixVICPrevYearUrl, { demandId: demandId, autonomousBodyId: autonomousBodyId }, appendixVICFieldMap);
    }

    function loadAppendixVIDPreviousYearReference(demandId, autonomousBodyId) {
        if (!demandId || !autonomousBodyId) {
            return;
        }
        loadPreviousYearFields("vid", urls.appendixVIDPrevYearUrl, { demandId: demandId, autonomousBodyId: autonomousBodyId }, appendixVIDFieldMap);
    }

    function loadAppendixVIEPreviousYearReference(demandId, autonomousBodyId) {
        if (!demandId || !autonomousBodyId) {
            return;
        }
        loadPreviousYearFields("vie", urls.appendixVIEPrevYearUrl, { demandId: demandId, autonomousBodyId: autonomousBodyId }, appendixVIEFieldMap);
    }

    // VII-A/VII-B/X/PA-ReceiptPayment (added 2026-08-04) - same self-referential prior-year
    // pre-fill. VII-A and X are keyed by free-text (SchemeName/SubHeadName - neither is an FK),
    // VII-B by SchemeId, PA by MajorHeadId.
    var appendixIIIAFieldMap = { be: "NewRecord_BE" };
    var appendixVIIAFieldMap = { actuals: "NewRecord_Actuals" };
    // actualsY1 deliberately NOT in this map (2026-08-07): the client's reference SQL gives a
    // specific, different source for it (dbo.SBEData.Actual_plan via PrevDemandId, scoped by
    // Scheme+MajorHead+TransactionType - see loadAppendixVIIBBeActuals) superseding this
    // self-referential "same demand's own prior VII-B row" source for that one field. actualsY2/
    // actualsUptoSeptPrevYear/actualsUptoSept have no client-provided correction and stay as-is.
    var appendixVIIBFieldMap = {
        actualsY2: "NewRecord_ActualsY2",
        actualsUptoSeptPrevYear: "NewRecord_ActualsUptoSeptPrevYear",
        actualsUptoSept: "NewRecord_ActualsUptoSept"
    };
    var appendixXFieldMap = { actualsY1: "NewRecord_ActualsY1", actualsY2: "NewRecord_ActualsY2", actualsY3: "NewRecord_ActualsY3" };
    var appendixPAFieldMap = {
        actualReceipt: "NewRecord_ActualReceipt",
        actualPayment: "NewRecord_ActualPayment",
        balanceAtEndReceipt: "NewRecord_BalanceAtEndReceipt",
        balanceAtEndPayment: "NewRecord_BalanceAtEndPayment"
    };

    // Appendix III-A's BE is for Current FY-2, auto-loaded from this Entity's own III-A record two
    // years prior (client review 2026-08-06) - same "readonly once found" pattern as the other
    // previous-year fields, just a -2 year offset instead of -1.
    function loadAppendixIIIABePreviousYear(demandId, entityName) {
        if (!demandId || !entityName) {
            return;
        }
        loadPreviousYearFields("iiia-be", urls.appendixIIIABeUrl, { demandId: demandId, entityName: entityName }, appendixIIIAFieldMap);
    }

    function loadAppendixVIIAPreviousYearReference(demandId, schemeName) {
        if (!demandId || !schemeName) {
            return;
        }
        loadPreviousYearFields("viia", urls.appendixVIIAPrevYearUrl, { demandId: demandId, schemeName: schemeName }, appendixVIIAFieldMap);
    }

    function loadAppendixVIIBPreviousYearReference(demandId, schemeId) {
        if (!demandId || !schemeId) {
            return;
        }
        loadPreviousYearFields("viib", urls.appendixVIIBPrevYearUrl, { demandId: demandId, schemeId: schemeId }, appendixVIIBFieldMap);
    }

    function loadAppendixXPreviousYearReference(demandId, subHeadName) {
        if (!demandId || !subHeadName) {
            return;
        }
        loadPreviousYearFields("x", urls.appendixXPrevYearUrl, { demandId: demandId, subHeadName: subHeadName }, appendixXFieldMap);
    }

    function loadAppendixPAPreviousYearReference(demandId, majorHeadId) {
        if (!demandId || !majorHeadId) {
            return;
        }
        loadPreviousYearFields("pa", urls.appendixPAPrevYearUrl, { demandId: demandId, majorHeadId: majorHeadId }, appendixPAFieldMap);
    }

    // ----------------------------------------------------------------------------------------
    // Scheme/SubScheme/Category/Major-Head cascade loaders
    // ----------------------------------------------------------------------------------------

    // Loads the Scheme dropdown for a given Balance Type, optionally pre-selecting one scheme
    // (used both by the Balance Type change handler and by "Edit", which needs the same cascade
    // replayed against the record being edited before it can select that record's own SchemeId).
    function loadSchemesForCategory(categoryType, selectedSchemeId, onDone) {
        var schemeSelect = document.getElementById("NewRecord_SchemeId");
        var subSchemeSelect = document.getElementById("NewRecord_SubSchemeId");
        if (!schemeSelect || !subSchemeSelect) {
            return;
        }

        subSchemeSelect.innerHTML = '<option value="">Select Scheme first</option>';
        subSchemeSelect.disabled = true;

        if (!categoryType || !demandSelect.value) {
            schemeSelect.innerHTML = '<option value="">Select...</option>';
            schemeSelect.disabled = true;
            return;
        }

        schemeSelect.disabled = true;
        schemeSelect.innerHTML = '<option value="">Loading…</option>';

        fetchJson(urls.schemesUrl + "?demandId=" + encodeURIComponent(demandSelect.value) + "&categoryType=" + encodeURIComponent(categoryType), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (items) {
                populateSelect(schemeSelect, items, "schemeId", "schemeName", "Select Scheme", selectedSchemeId);
                schemeSelect.disabled = false;
                if (onDone) {
                    onDone();
                }
            })
            .catch(function () {
                schemeSelect.innerHTML = '<option value="">Could not load schemes</option>';
                schemeSelect.disabled = false;
            });
    }

    // Loads the SubScheme dropdown for a given Scheme, optionally pre-selecting one (see above).
    function loadSubSchemesForScheme(schemeId, selectedSubSchemeId, onDone) {
        var subSelect = document.getElementById("NewRecord_SubSchemeId");
        if (!subSelect) {
            return;
        }

        if (!schemeId) {
            subSelect.innerHTML = '<option value="">Select Scheme first</option>';
            subSelect.disabled = true;
            subSelect.required = false;
            if (onDone) { onDone(); }
            return;
        }

        subSelect.disabled = true;
        subSelect.innerHTML = '<option value="">Loading…</option>';

        fetchJson(urls.subSchemesUrl + "?schemeId=" + encodeURIComponent(schemeId), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (items) {
                // Client requirement 2026-08-31: "sub-scheme is required only when there are
                // sub-scheme present in list after selecting scheme" - same pattern already used
                // by Appendix VI-B's own Scheme/SubScheme cascade (loadVIBSubSchemesForScheme).
                if (items.length === 0) {
                    subSelect.innerHTML = '<option value="">No Sub-Schemes for this Scheme</option>';
                    subSelect.disabled = true;
                    subSelect.required = false;
                    if (onDone) { onDone(); }
                    return;
                }

                populateSelect(subSelect, items, "subSchemeId", "subSchemeName", "Select", selectedSubSchemeId);
                subSelect.disabled = false;
                subSelect.required = true;
                if (onDone) { onDone(); }
            })
            .catch(function () {
                subSelect.innerHTML = '<option value="">Could not load sub-schemes</option>';
                subSelect.disabled = false;
                subSelect.required = false;
                if (onDone) { onDone(); }
            });
    }

    // BE auto-load (client review 2026-08-05): SUM(NBE_plan) from SBEData for the selected Scheme +
    // current FY, read-only on the form - see AppendixIII.cshtml's NewRecord.BE input.
    // `scopeEl` (2026-09-15, same fix as the IV family's loadAppendixBe) - NewRecord_BE is reused
    // by Appendix III/IV/IV-A/IV-B/VI-B, all by design; scoping to the specific form the caller
    // knows about (rather than a global document.getElementById) stops a stale duplicate left in
    // a different appendix's own form from silently swallowing the fetched value.
    function loadAppendixIIIBe(demandId, schemeId, scopeEl) {
        var scope = scopeEl || document;
        var beInput = scope.querySelector("#NewRecord_BE");
        if (!beInput) {
            return;
        }

        if (!demandId || !schemeId) {
            beInput.value = "";
            return;
        }

        fetchJson(urls.appendixIIIBeUrl + "?demandId=" + encodeURIComponent(demandId) + "&schemeId=" + encodeURIComponent(schemeId), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (data) { beInput.value = data.be || 0; })
            .catch(function () { beInput.value = ""; });
    }

    // BE auto-load for the Appendix IV family (client review 2026-08-05): sums by Scheme, or by
    // SubScheme once one is selected ("in case sub scheme is present, we will use sum of subschemeid").
    // `scopeEl` - see loadAppendixSchemeActualsReference's comment above; NewRecord_BE is also
    // reused by VI-B's own loadAppendixVIBBe, widening the duplicate-id collision surface further.
    function loadAppendixBe(beUrl, demandId, schemeId, subSchemeId, scopeEl) {
        var scope = scopeEl || document;
        var beInput = scope.querySelector("#NewRecord_BE");
        if (!beInput || !beUrl) {
            return;
        }

        if (!demandId || !schemeId) {
            beInput.value = "";
            recalcAppendixIVASavingExcess(scope);
            recalcAppendixIVCalculations(scope);
            return;
        }

        var url = beUrl + "?demandId=" + encodeURIComponent(demandId) + "&schemeId=" + encodeURIComponent(schemeId);
        if (subSchemeId) {
            url += "&subSchemeId=" + encodeURIComponent(subSchemeId);
        }

        fetchJson(url, { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (data) { beInput.value = data.be || 0; recalcAppendixIVASavingExcess(scope); recalcAppendixIVCalculations(scope); })
            .catch(function () { beInput.value = ""; recalcAppendixIVASavingExcess(scope); recalcAppendixIVCalculations(scope); });
    }

    // BE auto-load for Appendix VI-B (client reference SQL, 2026-08-25): "select sum(BE_Plan) from
    // SBEData where DemandId=... and CategoryId=... and SchemeID=...". Fires once both Category and
    // Scheme are chosen - the BE field itself is readonly (see AppendixVIB.cshtml).
    function loadAppendixVIBBe(demandId, categoryId, schemeId) {
        var beInput = document.getElementById("NewRecord_BE");
        if (!beInput) {
            return;
        }

        if (!demandId || !categoryId || !schemeId) {
            beInput.value = "";
            return;
        }

        var url = urls.appendixVIBBeUrl + "?demandId=" + encodeURIComponent(demandId) +
            "&categoryId=" + encodeURIComponent(categoryId) + "&schemeId=" + encodeURIComponent(schemeId);
        fetchJson(url, { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (data) { beInput.value = data.be || 0; })
            .catch(function () { beInput.value = ""; });
    }

    // Loads the SubScheme dropdown for Appendix IV's Scheme -> SubScheme cascade (no Balance Type
    // step, unlike Appendix III - every active Scheme for the demand is already server-rendered
    // into the Name* dropdown, so only SubScheme needs an AJAX round trip).
    // Client requirement 2026-08-27 (Appendix IV only - not IV-A/IV-B, out of scope here): "input
    // text boxes should be clear on change of dropdowns of scheme and subscheme except BE that is
    // bind from db." ActualsUptoSeptPrevYear/ActualsUptoSept are already reset by
    // loadAppendixSchemeActualsReference (then possibly re-filled from the prior-year reference
    // lookup); this covers the remaining manually-typed fields that function doesn't touch. BE is
    // deliberately left alone - it's the one field genuinely bound from SBEData, not user-entered.
    function clearAppendixIVManualFields(scopeEl) {
        var scope = scopeEl || document;
        ["NewRecord_Actuals", "NewRecord_ProposedRE", "NewRecord_ProposedNBE", "NewRecord_RemarksMinistry"].forEach(function (id) {
            var input = scope.querySelector("#" + id);
            if (input) {
                input.value = "";
            }
        });
        recalcAppendixIVCalculations(scope);
    }

    // `scopeEl` - see loadAppendixSchemeActualsReference's comment above (NewRecord_SubSchemeId is
    // also shared by Appendix III's own cascade).
    function loadIVSubSchemesForScheme(schemeId, selectedSubSchemeId, scopeEl, onDone) {       
        var scope = scopeEl || document;
        var subSelect = scope.querySelector("#NewRecord_SubSchemeId");
        if (!subSelect) {
            return;
        }

        if (!schemeId) {
            subSelect.innerHTML = '<option value="">Select a Scheme first</option>';
            subSelect.disabled = true;
            subSelect.required = false;
            window.PreBudgetValidation.updateAppendixIVDuplicateGuard();
            if (onDone) { onDone(); }
            return;
        }

        subSelect.disabled = true;
        subSelect.innerHTML = '<option value="">Loading…</option>';

        fetchJson(urls.ivSubSchemesUrl + "?schemeId=" + encodeURIComponent(schemeId), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (items) {
                // Most schemes have no Sub-Schemes at all - that's a valid, common state (not an
                // error), so it gets its own message and stays disabled rather than offering a
                // dropdown with nothing but a "Select" placeholder in it.
                // Client requirement 2026-09-21: "make sub-scheme entry must if it loads with
                // sub-schemes. if scheme do not have sub-schemes, it will not load and not enabled
                // and not mandatory" - same required-only-when-populated rule Appendix III's own
                // loadSubSchemesForScheme already applies, extended here to IV/IV-A/IV-B.
                if (items.length === 0) {
                    subSelect.innerHTML = '<option value="">No Sub-Schemes for this Scheme</option>';
                    subSelect.disabled = true;
                    subSelect.required = false;
                    window.PreBudgetValidation.updateAppendixIVDuplicateGuard();
                    if (onDone) { onDone(); }
                    return;
                }

                populateSelect(subSelect, items, "subSchemeId", "subSchemeName", "Select", selectedSubSchemeId);
                subSelect.disabled = false;
                subSelect.required = true;
                window.PreBudgetValidation.updateAppendixIVDuplicateGuard();
                if (onDone) { onDone(); }
            })
            .catch(function () {
                subSelect.innerHTML = '<option value="">Could not load sub-schemes</option>';
                subSelect.disabled = false;
                subSelect.required = false;
                window.PreBudgetValidation.updateAppendixIVDuplicateGuard();
                if (onDone) { onDone(); }
            });
    }

    // Appendix VI-B's Category -> Scheme step (added 2026-07-30 - the designer's approved layout
    // put a Category picker ahead of Scheme/SubScheme). Mirrors loadSchemesForCategory above but
    // against VI-B's own schemes endpoint (which filters by demandId + categoryId).
    function loadVIBSchemesForCategory(categoryId, selectedSchemeId, onDone) {
        var schemeSelect = document.getElementById("NewRecord_SchemeId");
        var subSchemeSelect = document.getElementById("NewRecord_SubSchemeId");
        if (!schemeSelect || !subSchemeSelect) {
            return;
        }

        subSchemeSelect.innerHTML = '<option value="">Select a Scheme first</option>';
        subSchemeSelect.disabled = true;

        if (!categoryId) {
            schemeSelect.innerHTML = '<option value="">Select a Category first</option>';
            schemeSelect.disabled = true;
            return;
        }

        schemeSelect.disabled = true;
        schemeSelect.innerHTML = '<option value="">Loading…</option>';

        fetchJson(urls.vibSchemesUrl + "?demandId=" + encodeURIComponent(demandSelect.value) + "&categoryId=" + encodeURIComponent(categoryId), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (items) {
                populateSelect(schemeSelect, items, "schemeId", "schemeName", "Select", selectedSchemeId);
                schemeSelect.disabled = false;
                if (onDone) {
                    onDone();
                }
            })
            .catch(function () {
                schemeSelect.innerHTML = '<option value="">Could not load schemes</option>';
                schemeSelect.disabled = false;
            });
    }

    // Same plain Scheme -> SubScheme cascade as Appendix IV (no Balance Type step), just posting to
    // Appendix VI-B's own subschemes endpoint.
    function loadVIBSubSchemesForScheme(schemeId, selectedSubSchemeId, onDone) {
        var subSelect = document.getElementById("NewRecord_SubSchemeId");
        if (!subSelect) {
            return;
        }

        if (!schemeId) {
            subSelect.innerHTML = '<option value="">Select a Scheme first</option>';
            subSelect.disabled = true;
            subSelect.required = false;
            if (onDone) { onDone(); }
            return;
        }

        subSelect.disabled = true;
        subSelect.innerHTML = '<option value="">Loading…</option>';

        fetchJson(urls.vibSubSchemesUrl + "?schemeId=" + encodeURIComponent(schemeId), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (items) {
                if (items.length === 0) {
                    subSelect.innerHTML = '<option value="">No Sub-Schemes for this Scheme</option>';
                    subSelect.disabled = true;
                    // Client requirement 2026-08-27: "category, scheme and sub-scheme are required
                    // fields" - but only ever true when the selected Scheme actually has a
                    // Sub-Scheme to pick; this Scheme has none, so it stays optional (matches the
                    // server-side check in AppendixVIBController.ValidateRequiredSelectionsAsync).
                    subSelect.required = false;
                    if (onDone) { onDone(); }
                    return;
                }

                populateSelect(subSelect, items, "subSchemeId", "subSchemeName", "Select", selectedSubSchemeId);
                subSelect.disabled = false;
                subSelect.required = true;
                if (onDone) { onDone(); }
            })
            .catch(function () {
                subSelect.innerHTML = '<option value="">Could not load sub-schemes</option>';
                subSelect.disabled = false;
                subSelect.required = false;
                if (onDone) { onDone(); }
            });
    }

    // Appendix VII-B's Major Head dropdown, cascading from the selected Scheme (client reference
    // SQL, 2026-08-07) - mirrors the Balance Type -> Scheme cascade pattern elsewhere in this file.
    function loadAppendixVIIBMajorHeads(demandId, schemeId, selectedMajorHeadId, selectedMajorHeadCode, onDone) {
        var majorHeadSelect = document.getElementById("NewRecord_MajorHeadId");
        if (!majorHeadSelect) {
            return;
        }

        if (!demandId || !schemeId) {
            majorHeadSelect.innerHTML = '<option value="">Select</option>';
            majorHeadSelect.disabled = true;
            if (onDone) { onDone(); }
            return;
        }

        majorHeadSelect.disabled = true;
        majorHeadSelect.innerHTML = '<option value="">Loading…</option>';

        fetchJson(urls.appendixVIIBMajorHeadsUrl + "?demandId=" + encodeURIComponent(demandId) + "&schemeId=" + encodeURIComponent(schemeId), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (items) {
                populateSelect(majorHeadSelect, items, "majorHeadId", "majorHeadLabel", "Select Major Head", selectedMajorHeadId, selectedMajorHeadCode);
                majorHeadSelect.disabled = false;
                loadAppendixVIIBBeActuals();
                if (onDone) { onDone(); }
            })
            .catch(function () {
                majorHeadSelect.innerHTML = '<option value="">Could not load Major Heads</option>';
                majorHeadSelect.disabled = false;
                if (onDone) { onDone(); }
            });
    }

    // BE/Actuals auto-load once Scheme + Major Head + Transaction Type are all chosen, per the
    // client's reference SQL (2026-08-07). Reads the current selections directly off the form
    // rather than taking parameters, since any of the 3 change handlers can trigger this and all 3
    // values are always needed together. Populates ActualsY2 (confirmed against a live screenshot
    // of this page - the OLDEST/leftmost Actuals column, not ActualsY1). Recalculates the
    // Increase/Decrease-over-BE display afterward since BE just changed.
    function loadAppendixVIIBBeActuals() {
        var form = document.getElementById("appendixVIIBForm");
        var beInput = document.getElementById("NewRecord_BE");
        var actualsY2Input = document.getElementById("NewRecord_ActualsY2");
        if (!form || !beInput || !actualsY2Input) {
            return;
        }

        var demandId = form.getAttribute("data-demand-id");
        var schemeId = document.getElementById("NewRecord_SchemeId").value;
        var majorHeadId = document.getElementById("NewRecord_MajorHeadId").value;
        var transactionType = document.getElementById("NewRecord_TransactionType").value;

        if (!demandId || !schemeId || !majorHeadId || !transactionType) {
            beInput.value = "";
            actualsY2Input.value = "";
            recalcAppendixVIIBIncreasedBE();
            return;
        }

        fetchJson(urls.appendixVIIBBeActualsUrl
                + "?demandId=" + encodeURIComponent(demandId)
                + "&schemeId=" + encodeURIComponent(schemeId)
                + "&majorHeadId=" + encodeURIComponent(majorHeadId)
                + "&transactionType=" + encodeURIComponent(transactionType), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (data) {
                // Bug fix 2026-09-14 (client report: "autofill Actual and BE for prev years data
                // that was working previously"): AppendixVIIBController.GetBeActuals returns
                // { be, actuals } - this read `data.actualsY2`, a property that has never existed
                // on the response, so ActualsY2 silently fell back to 0 on every single load (BE
                // "worked" only because its own field name happened to match).
                beInput.value = data.be || 0;
                actualsY2Input.value = data.actuals || 0;
                recalcAppendixVIIBIncreasedBE();
            })
            .catch(function () {
                beInput.value = "";
                actualsY2Input.value = "";
                recalcAppendixVIIBIncreasedBE();
            });
    }

    // Client requirement 2026-08-27 ("input dropt down not filtering grid table" on Appendix
    // VII-B): filters the saved-records grid by whichever of the 3 entry-form dropdowns
    // (Departmental Commercial Undertaking / Major Head / Type of Transaction) currently have a
    // value - AND across all 3, ignoring any left on "Select". Same "reuse the entry form's own
    // selects" pattern as the VI-D/VI-E Autonomous Body filter below, generalized to 3 dropdowns
    // instead of 1 since VII-B's grid has 3 independent filterable columns.
    function filterAppendixVIIBGrid() {
        var schemeId = document.getElementById("NewRecord_SchemeId")?.value || "";
        var majorHeadId = document.getElementById("NewRecord_MajorHeadId")?.value || "";
        var transactionType = document.getElementById("NewRecord_TransactionType")?.value || "";
        var grid = document.querySelector("#appendixVIIBGrid tbody");
        if (!grid) {
            return;
        }
        Array.prototype.forEach.call(grid.querySelectorAll("tr[data-scheme-id]"), function (row) {
            var matches = (!schemeId || row.getAttribute("data-scheme-id") === schemeId) &&
                (!majorHeadId || row.getAttribute("data-major-head-id") === majorHeadId) &&
                (!transactionType || row.getAttribute("data-transaction-type") === transactionType);
            row.classList.toggle("hidden", !matches);
        });
    }

    // Scopes the read-only history grid below Appendix IV/IV-A/IV-B's entry form to just the
    // Scheme currently picked in the Name* dropdown (IV added 2026-08-04, IV-A/IV-B generalized
    // from it the same day) - the grid otherwise shows every Scheme's rows for the demand+year at
    // once, which is confusing once a Scheme has more than a couple of rows on file. Purely a
    // client-side show/hide (the rows are already all rendered server-side with a `data-scheme-id`
    // attribute) so it reacts instantly with no round trip.
    // subSchemeId is optional (client testing feedback, 2026-08-24: "Grid records should filter on
    // subscheme too") - omitted/falsy call sites keep the original scheme-only filtering behavior.
    function filterAppendixSchemeGridByScheme(formId, schemeId, subSchemeId) {
        // The old sibling selector (`#form ~ .overflow-x-auto table tbody`) only works when the
        // form is a page-level sibling of the grid, as in the pre-drawer layout. Appendix III's
        // 2026-09-08 drawer redesign moved the form into a reparented-to-body <aside>, no longer a
        // DOM sibling of the grid section at all - so the drawer view tags its own grid table with
        // data-scheme-grid-for="<formId>" and this falls back to that lookup when the sibling
        // selector finds nothing. IV/IV-A/IV-B (still old-style) keep working via the original path.
        var grid = document.querySelector('#' + formId + ' ~ .overflow-x-auto table tbody')
            || document.querySelector('table[data-scheme-grid-for="' + formId + '"] tbody');
        if (!grid) {
            return;
        }

        var rows = grid.querySelectorAll("tr[data-scheme-id]");
        Array.prototype.forEach.call(rows, function (row) {
            var schemeMatches = !schemeId || row.getAttribute("data-scheme-id") === String(schemeId);
            var subSchemeMatches = !subSchemeId || row.getAttribute("data-sub-scheme-id") === String(subSchemeId);
            row.classList.toggle("hidden", !(schemeMatches && subSchemeMatches));
        });

        // Client requirement 2026-08-27 (Appendix III only): "Grid Sr No should be recalculated
        // based on dropdown change event of scheme and sub-scheme." The S.No. column is rendered
        // server-side as a plain sequential index over every saved row, so filtering rows client-
        // side (above) leaves gaps in the visible numbering (e.g. 1, 4, 7) instead of a clean 1, 2,
        // 3 over whatever's actually showing. Scoped to appendixIIIForm's grid only - this function
        // is shared with IV/IV-A/IV-B/VI-B, none of which asked for this.
        if (formId === "appendixIIIForm") {
            var visibleSrNo = 1;
            Array.prototype.forEach.call(rows, function (row) {
                if (row.classList.contains("hidden")) {
                    return;
                }
                var srNoCell = row.querySelector(".row-sr-no");
                if (srNoCell) {
                    srNoCell.textContent = String(visibleSrNo++);
                }
            });
        }
    }

    // ----------------------------------------------------------------------------------------
    // Live recalculated display fields
    // ----------------------------------------------------------------------------------------

    // Appendix IV-A's "Saving/Excess in R.E. over B.E." (client review 2026-08-06) - was a static
    // "Auto" placeholder with no value shown until after Submit; now recalculates live as R.E. is
    // typed (BE is already read-only/auto-loaded, so R.E. is the only field that changes this).
    // Appendix II's "Totals" row (client reference screenshot, 2026-08-10) - Q1 + Q2 per column,
    // matching the legacy app's disabled Totals textboxes. Recalculated live as any Q1/Q2 field is
    // typed, same computed-readonly-display pattern as recalcAppendixIVASavingExcess.
    function recalcAppendixIITotal(q1Id, q2Id, totalId) {
        var totalInput = document.getElementById(totalId);
        if (!totalInput) {
            return;
        }
        var q1 = parseFloat(document.getElementById(q1Id)?.value);
        var q2 = parseFloat(document.getElementById(q2Id)?.value);
        totalInput.value = isNaN(q1) && isNaN(q2) ? "" : ((isNaN(q1) ? 0 : q1) + (isNaN(q2) ? 0 : q2)).toFixed(2);
    }

    function recalcAppendixIITotals() {
        recalcAppendixIITotal("NewRecord_Q1ApprovedQepPrevYear", "NewRecord_Q2ApprovedQepPrevYear", "appendixIITotalApprovedQepPrevYear");
        recalcAppendixIITotal("NewRecord_Q1ActualsPrevYear", "NewRecord_Q2ActualsPrevYear", "appendixIITotalActualsPrevYear");
        recalcAppendixIITotal("NewRecord_Q1ApprovedQep", "NewRecord_Q2ApprovedQep", "appendixIITotalApprovedQep");
        recalcAppendixIITotal("NewRecord_Q1Actuals", "NewRecord_Q2Actuals", "appendixIITotalActuals");
    }

    // Appendix IV's three "Auto" columns (client testing feedback, 2026-08-24) - the backend
    // (AppendixEstimatesOfSchemes entity) already computes and persists these exact formulas for
    // the grid below; this just mirrors them live in the entry form before Submit so the user isn't
    // staring at a static "Auto" placeholder. % w.r.t B.E. = 100 - ((B.E. - Actuals upto Sept)/B.E.)
    // x 100 (algebraically (Actuals upto Sept / B.E.) x 100, kept in the spec's literal form here).
    function recalcAppendixIVCalculations(scopeEl) {
        var scope = scopeEl || document;
        var pctDisplay = scope.querySelector("#ivPercentWrtBE");
        var addlReDisplay = scope.querySelector("#ivAddlReSought");
        var addlNbeDisplay = scope.querySelector("#ivAddlNbeSought");
        if (!pctDisplay && !addlReDisplay && !addlNbeDisplay) {
            return;
        }

        var be = parseFloat(scope.querySelector("#NewRecord_BE")?.value);
        var actualsUptoSept = parseFloat(scope.querySelector("#NewRecord_ActualsUptoSept")?.value);
        var proposedRE = parseFloat(scope.querySelector("#NewRecord_ProposedRE")?.value);
        var proposedNBE = parseFloat(scope.querySelector("#NewRecord_ProposedNBE")?.value);

        if (pctDisplay) {
            pctDisplay.value = (isNaN(be) || be === 0 || isNaN(actualsUptoSept))
                ? ""
                : (100 - ((be - actualsUptoSept) / be) * 100).toFixed(2);
        }
        if (addlReDisplay) {
            addlReDisplay.value = (isNaN(be) || isNaN(proposedRE)) ? "" : (proposedRE - be).toFixed(2);
        }
        if (addlNbeDisplay) {
            addlNbeDisplay.value = (isNaN(be) || isNaN(proposedNBE)) ? "" : (proposedNBE - be).toFixed(2);
        }
    }

    // Appendix III-A's "Unspent Assignment" column (client testing feedback, 2026-08-24: clarify
    // as "TSA Assignment as on 30th Sept - Actual Expenditure upto Sept") - was a static "Auto"
    // placeholder with no live value; mirrors recalcAppendixIVCalculations' pattern.
    function recalcAppendixIIIAUnspentAssignment() {
        var display = document.getElementById("iiiaUnspentAssignment");
        if (!display) {
            return;
        }
        var tsa = parseFloat(document.getElementById("NewRecord_TsaAssignmentAsOnSept")?.value);
        var actual = parseFloat(document.getElementById("NewRecord_ActualExpenditureUptoSept")?.value);
        display.value = isNaN(tsa) || isNaN(actual) ? "" : (tsa - actual).toFixed(2);
    }

    function recalcAppendixIVASavingExcess(scopeEl) {
        var scope = scopeEl || document;
        var display = scope.querySelector("#ivaSavingExcess") || scope.querySelector("#ivbSavingExcess");
        if (!display) {
            return;
        }
        var be = parseFloat(scope.querySelector("#NewRecord_BE")?.value);
        var re = parseFloat(scope.querySelector("#NewRecord_ProposedRE")?.value);
        display.value = isNaN(be) || isNaN(re) ? "" : (re - be).toFixed(2);
    }

    // Appendix I-A's Total BE/RE columns (client reference screenshot, 2026-08-21) - Revenue + Capital
    // per year-row, same live disabled-display pattern as recalcAppendixIITotal. The proposed-year row
    // has no RE fields at all (CurrYrMin* has no RE variant), so only its Total BE is ever computed.
    function sumIntoDisplay(displayId, valueIds) {
        var display = document.getElementById(displayId);
        if (!display) {
            return;
        }
        var values = valueIds.map(function (id) { return parseFloat(document.getElementById(id)?.value); });
        var anyEntered = values.some(function (v) { return !isNaN(v); });
        display.value = anyEntered ? values.reduce(function (sum, v) { return sum + (isNaN(v) ? 0 : v); }, 0).toFixed(2) : "";
    }

    function recalcAppendixIATotals() {
        sumIntoDisplay("appendixIAPrevTotalBE", ["NewRecord_PrevYrMinRevenueBE", "NewRecord_PrevYrMinCapitalBE"]);
        sumIntoDisplay("appendixIAPrevTotalRE", ["NewRecord_PrevYrMinRevenueRE", "NewRecord_PrevYrMinCapitalRE"]);
        sumIntoDisplay("appendixIACurrTotalBE", ["NewRecord_CurrYrMinRevenueBE", "NewRecord_CurrYrMinCapitalBE"]);
    }

    function recalcAppendixVIIBIncreasedBE() {
        var display = document.getElementById("NewRecord_IncreasedBE");
        if (!display) {
            return;
        }
        var be = parseFloat(document.getElementById("NewRecord_BE")?.value);
        var re = parseFloat(document.getElementById("NewRecord_RE")?.value);
        display.value = isNaN(be) || isNaN(re) ? "" : (re - be).toFixed(2);
    }

    // Dispatches a just-typed number field to whichever recalc(s) its appendix wants live -
    // PreBudget-validation.js's own "input" listener handles sanitizing the typed value; this is
    // the control-binding half of that same event, called from there right after sanitizing so a
    // field's live "Auto" display always reflects what was just typed.
    function recalcForNumberInput(input) {
        if (input.id === "NewRecord_RE" && input.closest("#appendixVIIBForm")) {
            recalcAppendixVIIBIncreasedBE();
        }

        if (input.id === "NewRecord_ProposedRE" && (input.closest("#appendixIVAForm") || input.closest("#appendixIVBForm"))) {
            recalcAppendixIVASavingExcess(input.closest("#appendixIVAForm") || input.closest("#appendixIVBForm"));
        }

        var ivFormScope = input.closest("#appendixIVForm");
        if (ivFormScope && /^NewRecord_(ActualsUptoSept|ProposedRE|ProposedNBE)$/.test(input.id)) {
            recalcAppendixIVCalculations(ivFormScope);
        }

        if (input.closest("#appendixIIIAForm") && /^NewRecord_(TsaAssignmentAsOnSept|ActualExpenditureUptoSept)$/.test(input.id)) {
            recalcAppendixIIIAUnspentAssignment();
        }

        if (input.closest("#appendixIIForm") && /^NewRecord_Q[12](ApprovedQep|Actuals)(PrevYear)?$/.test(input.id)) {
            recalcAppendixIITotals();
        }

        if (input.closest("#appendixIAForm") && /^NewRecord_(PrevYrMin(Revenue|Capital)(BE|RE)|CurrYrMin(Revenue|Capital)BE)$/.test(input.id)) {
            recalcAppendixIATotals();
        }
    }

    // ----------------------------------------------------------------------------------------
    // Edit-mode: select lock/unlock, enter/exit, and every appendix's populate*EntryForm
    // ----------------------------------------------------------------------------------------

    // PropertyName -> data-property-name, so a Razor view only ever needs to emit
    // data-<kebab-case> attributes matching its own field names - this file never hardcodes any
    // particular appendix's fields, so the same JS drives all 22 appendices unchanged.
    function toDataAttrName(propertyName) {
        return "data-" + propertyName.replace(/([a-z0-9])([A-Z])/g, "$1-$2").toLowerCase();
    }

    // "Edit" on a history-table row brings that year's saved values into the entry grid above -
    // the grid already shows the same 5 years inline-editable, so this just makes sure the row is
    // in view/focused and (defensively) re-copies the values in case the two tables ever drift.
    // Generic across every appendix: it reads whichever fields the target row's inputs actually
    // have (via each input's own name="Rows[i].PropertyName"), not a fixed list.
    function populateRowFromButton(editButton, targetRow) {
        var firstInput = null;
        targetRow.querySelectorAll("input, select, textarea").forEach(function (input) {
            var name = input.getAttribute("name") || "";
            var lastDot = name.lastIndexOf(".");
            if (lastDot === -1) {
                return;
            }

            var attrName = toDataAttrName(name.slice(lastDot + 1));
            if (!editButton.hasAttribute(attrName)) {
                return;
            }

            input.value = editButton.getAttribute(attrName) || "";
            firstInput = firstInput || input;
        });

        targetRow.scrollIntoView({ behavior: "smooth", block: "center" });
        if (firstInput) {
            firstInput.focus();
        }
    }

    // Shared Edit -> Modify/Cancel behavior (client requirement 2026-08-11), called from the end of
    // every appendix's own populate*EntryForm function (which already knows how to fill in that
    // appendix's specific fields) rather than duplicating field-population logic here. Submit's
    // label is swapped to "Modify" (restored via a data-original-label attribute stashed on first
    // use, since a fragment reload naturally provides a fresh "Submit"-labeled button anyway - see
    // below), and a Cancel button (present-but-hidden in the form's own markup, `data-action=
    // "cancel-edit"`) is revealed. exitAppendixEditMode is the inverse, wired to that Cancel button
    // - reusable as-is across every appendix, since undoing an edit never needs appendix-specific
    // field knowledge (a plain form.reset() covers it).
    function enterAppendixEditMode(form) {
        if (!form) {
            return;
        }

        var submitButton = form.querySelector('button[type="submit"]');
        if (submitButton) {
            if (!submitButton.hasAttribute("data-original-label")) {
                submitButton.setAttribute("data-original-label", submitButton.innerHTML);
            }
            submitButton.innerHTML = submitButton.getAttribute("data-original-label").replace(/Save Record|Submit/i, "Modify");
        }

        var cancelButton = form.querySelector('[data-action="cancel-edit"]');
        if (cancelButton) {
            cancelButton.classList.remove("hidden");
        }
    }

    function exitAppendixEditMode(form) {
        if (!form) {
            return;
        }

        // Was form.reset() - which only restores each control to its INITIAL server-rendered
        // value, not blank (client report 2026-09-10: "Reset and Cancel should clear all fields
        // ... in case of duplicate validation the field is not clearing"). A form re-rendered
        // with populated NewRecord.* (a failed save that kept ModelState, or an edit) would
        // therefore come back populated, and a Save left disabled by the live duplicate check
        // stayed disabled. hardClearAppendixForm blanks everything and re-runs the dependent
        // logic (cascades / dup-check) against the empty form.
        hardClearAppendixForm(form);

        var submitButton = form.querySelector('button[type="submit"]');
        if (submitButton && submitButton.hasAttribute("data-original-label")) {
            submitButton.innerHTML = submitButton.getAttribute("data-original-label");
        }

        var cancelButton = form.querySelector('[data-action="cancel-edit"]');
        if (cancelButton) {
            cancelButton.classList.add("hidden");
        }
    }

    // Truly blanks every field of an appendix entry/drawer form - unlike the native
    // <button type="reset"> / form.reset(), which only restore INITIAL values. Used by
    // exitAppendixEditMode (Add-drawer-open, edit-cancel) and by the generic Reset/Cancel
    // handlers in attachListeners so every redesigned drawer's Reset and Cancel behave the same:
    // clear all fields, drop the record id, re-enable a duplicate-disabled Save, and re-run the
    // form's cascades / recalcs / duplicate checks against the now-empty state.
    var _appendixHardClearing = false;
    function hardClearAppendixForm(form) {
        if (!form || _appendixHardClearing) {
            return;
        }
        _appendixHardClearing = true;
        try {
            // Re-enable any <select> that edit mode disabled (lockSelectForEdit) and drop its
            // hidden mirror, so the blanking below actually takes and the control is usable again.
            unlockSelectsAfterEdit(form);

            var controls = Array.prototype.slice.call(form.querySelectorAll("input, select, textarea"));
            controls.forEach(function (el) {
                if (el.type === "hidden") {
                    // Keep the antiforgery token, demandId, code and appendixId; only blank the
                    // record id so the next submit is treated as a fresh Add.
                    if (/(^|[_.])Id$/.test(el.id) || el.name === "NewRecord.Id") {
                        el.value = "";
                    }
                    return;
                }
                if (el.type === "checkbox" || el.type === "radio") {
                    el.checked = el.defaultChecked;
                } else if (el.tagName === "SELECT") {
                    el.selectedIndex = el.options.length ? 0 : -1;
                } else {
                    el.value = "";
                }
            });

            // A Save/Submit button disabled by a live duplicate check (e.g. Appendix III-B) must
            // come back enabled once the fields it was validating are gone.
            var submit = form.querySelector('button[type="submit"], input[type="submit"]');
            if (submit) {
                submit.disabled = false;
                submit.title = "";
            }

            // Let every dependent behaviour recompute against the empty form the same way it
            // would if the user had cleared each field by hand: cascade dropdowns
            // (Category -> Scheme, Balance Type -> Scheme, ...), IncreasedBE/total recalcs, and
            // the live duplicate checks all listen on input/change of these same controls.
            controls.forEach(function (el) {
                if (el.type === "hidden") {
                    return;
                }
                el.dispatchEvent(new Event("input", { bubbles: true }));
                el.dispatchEvent(new Event("change", { bubbles: true }));
            });

            // Fire a reset event too (without actually re-resetting) so each appendix's own
            // "reset" cleanup listeners - AJAX-loaded <select> placeholders, edit-row highlight,
            // previous-year re-fetch - still run. The _appendixHardClearing guard stops the
            // generic reset handler in attachListeners from recursing back into here.
            form.dispatchEvent(new Event("reset", { bubbles: true }));
        } finally {
            _appendixHardClearing = false;
        }
    }

    // Disables a <select> during edit mode while keeping its value submitted, via a same-named
    // hidden mirror input (a disabled control is excluded from FormData entirely). Paired with
    // unlockSelectsAfterEdit below, run on every form reset/edit-cancel.
    function lockSelectForEdit(selectId) {
        var select = document.getElementById(selectId);
        if (!select) {
            return;
        }
        select.disabled = true;
        select.classList.add("appendix-locked-for-edit");
        var mirror = document.getElementById(selectId + "_editMirror");
        if (!mirror) {
            mirror = document.createElement("input");
            mirror.type = "hidden";
            mirror.id = selectId + "_editMirror";
            mirror.name = select.getAttribute("name") || "";
            select.insertAdjacentElement("afterend", mirror);
        }
        mirror.value = select.value;
    }

    function unlockSelectsAfterEdit(form) {
        if (!form) {
            return;
        }
        Array.prototype.forEach.call(form.querySelectorAll("select.appendix-locked-for-edit"), function (select) {
            select.disabled = false;
            select.classList.remove("appendix-locked-for-edit");
            // Scoped to `form` (not document.getElementById) - IV/IV-A/IV-B share the exact same
            // field ids, so a global lookup could remove a different form's own mirror instead.
            var mirror = form.querySelector("#" + select.id + "_editMirror");
            if (mirror) {
                mirror.remove();
            }
        });
    }

    // Scoped variant of lockSelectForEdit above, for appendixes that share field ids across
    // multiple forms (IV/IV-A/IV-B all use "NewRecord_SchemeId"/"NewRecord_SubSchemeId") - a
    // global document.getElementById lookup there could target a stale duplicate left over from a
    // different one of those forms (same class of bug as the 2026-09-15 "BE not loading" fix
    // above). unlockSelectsAfterEdit already scopes its own removal via form.querySelectorAll, so
    // it correctly cleans up mirrors created here.
    function lockSelectForEditScoped(form, selectId) {
        var select = form.querySelector("#" + selectId);
        if (!select) {
            return;
        }
        select.disabled = true;
        select.classList.add("appendix-locked-for-edit");
        var mirrorId = selectId + "_editMirror";
        var mirror = form.querySelector("#" + mirrorId);
        if (!mirror) {
            mirror = document.createElement("input");
            mirror.type = "hidden";
            mirror.id = mirrorId;
            mirror.name = select.getAttribute("name") || "";
            select.insertAdjacentElement("afterend", mirror);
        }
        mirror.value = select.value;
    }

    function setRadioGroup(name, value) {
        var input = container.querySelector('input[name="' + name + '"][value="' + value + '"]');
        if (input) {
            input.checked = true;
        }
    }

    // "Edit" on an Appendix III grid row brings that record's values into the entry form above,
    // replaying the same Balance Type -> Scheme -> SubScheme AJAX cascade the user would have gone
    // through manually, so the two dependent dropdowns end up populated with the right options
    // AND the record's actual Scheme/SubScheme selected - then submitting the form (with
    // NewRecord.Id now populated) calls SaveAppendixIII, which updates rather than creates.
    function populateAppendixIIIEntryForm(editButton) {
        // Scoped to this Edit button's own form (2026-09-15, same fix as the IV family) - was a
        // global document.getElementById, which could resolve to a stale duplicate NewRecord_Id/
        // NewRecord_CategoryType left over in a different appendix's own form.
        var appendixIIIFormEl = editButton.closest("#appendixIIIForm") || document.getElementById("appendixIIIForm");
        if (!appendixIIIFormEl) {
            return;
        }
        var idField = appendixIIIFormEl.querySelector("#NewRecord_Id");
        var categorySelect = appendixIIIFormEl.querySelector("#NewRecord_CategoryType");
        if (!idField || !categorySelect) {
            return;
        }

        idField.value = editButton.getAttribute("data-id") || "";
        categorySelect.value = editButton.getAttribute("data-category-type") || "";
        try { categorySelect.dispatchEvent(new Event('change', { bubbles: true })); } catch (e) { }

        var schemeId = editButton.getAttribute("data-scheme-id");
        var subSchemeId = editButton.getAttribute("data-sub-scheme-id");
        loadSchemesForCategory(categorySelect.value, schemeId, function () {
            loadSubSchemesForScheme(schemeId, subSchemeId, function () {
                // Client requirement 2026-08-31: "edit mode Balance Type, Scheme, Sub-Scheme
                // should be freezed" - all 3 make up this row's identity (the duplicate-entry
                // guard keys on exactly this combination), so none of them should change mid-
                // edit; same lockSelectForEdit/hidden-mirror-input pattern as Appendix VII-A's
                // Major Head. Locked only after the Scheme/SubScheme cascade finishes (not
                // before), so the correct final SubScheme options/selection are already in
                // place when the field freezes.
                lockSelectForEdit("NewRecord_CategoryType");
                lockSelectForEdit("NewRecord_SchemeId");
                lockSelectForEdit("NewRecord_SubSchemeId");

                // Client requirement 2026-09-15: "pre-fetch BE data into save and modify drawer
                // screen" - BE used to only carry over the row's own saved value (data-be, set
                // below) on Edit, unlike Add mode which always live-fetches from SBE on Scheme
                // selection. Re-runs the same live fetch here so Modify shows SBE's CURRENT figure
                // (in case it changed since this record was last saved), not a stale snapshot.
                // Runs after the cascade above so the field isn't fighting the load/disabled
                // states loadSchemesForCategory/loadSubSchemesForScheme set on their own inputs.
                loadAppendixIIIBe(appendixIIIFormEl.getAttribute("data-demand-id"), schemeId, appendixIIIFormEl);
            });
        });

        [
            ["NewRecord_BE", "data-be"],
            ["NewRecord_BalanceAsOnAprilOpening", "data-balance-as-on-april-opening"],
            ["NewRecord_ReleasesDuringFY", "data-releases-during-fy"],
            ["NewRecord_BalanceAsOnSeptClosing", "data-balance-as-on-sept-closing"],
            ["NewRecord_DateOfLastRelease", "data-date-of-last-release"],
            ["NewRecord_AmountOfLastRelease", "data-amount-of-last-release"],
            ["NewRecord_NotTransferredToSnaAsOnSept", "data-not-transferred-to-sna-as-on-sept"],
            ["NewRecord_ReasonForExemption", "data-reason-for-exemption"]
        ].forEach(function (pair) {
            var input = appendixIIIFormEl.querySelector("#" + pair[0]);
            if (input) {
                input.value = editButton.getAttribute(pair[1]) || "";
            }
        });

        // "On Edit mode, save button should show modify text" (client testing feedback, 2026-08-25) -
        // every other appendix's edit-populate function already calls this; III's own hand-written
        // populate function (it has an extra Category/Scheme/SubScheme cascade the generic
        // populateSimpleEntryForm doesn't know how to replay) had been missing it.
        enterAppendixEditMode(appendixIIIFormEl);
        appendixIIIFormEl.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    // "Edit" on an Appendix IV grid row brings that record's values into the entry form above.
    // Scheme itself is a plain server-rendered dropdown (no AJAX needed to select it), only
    // SubScheme needs the cascade replayed before it can be selected.
    function populateAppendixIVEntryForm(editButton) {
        // Scoped to this Edit button's own form (2026-09-15 fix, client report "BE not loading in
        // Appendix IV/IV-A") - was a global document.getElementById, which could resolve to a
        // stale duplicate NewRecord_Id/NewRecord_SchemeId left over in a different appendix's own
        // (identically-named) form.
        var form = editButton.closest("#appendixIVForm") || document.getElementById("appendixIVForm");
        if (!form) {
            return;
        }
        var idField = form.querySelector("#NewRecord_Id");
        var schemeSelect = form.querySelector("#NewRecord_SchemeId");
        if (!idField || !schemeSelect) {
            return;
        }

        idField.value = editButton.getAttribute("data-id") || "";
        schemeSelect.value = editButton.getAttribute("data-scheme-id") || "";
        // Client requirement 2026-09-14: "edit mode scheme and subscheme should be disabled" -
        // both are part of the record's identity. Scheme locks immediately (its value is already
        // known); Sub-Scheme locks only once its cascaded AJAX load has actually populated it -
        // same "lock after cascade" convention used for Appendix III-B/VI-B/VII-B's own
        // Scheme/Sub-Scheme/Major Head. Scoped to `form` (not the shared lockSelectForEdit, which
        // does a global document.getElementById) since IV/IV-A/IV-B all share these exact field
        // ids - same stale-duplicate risk the 2026-09-15 BE-not-loading fix above addressed.
        lockSelectForEditScoped(form, "NewRecord_SchemeId");        

        //Call in add mode only, Prevent to call in edit mode
        if (idField.value = "") {
            loadIVSubSchemesForScheme(schemeSelect.value, editButton.getAttribute("data-sub-scheme-id"), form, function () {
                lockSelectForEditScoped(form, "NewRecord_SubSchemeId");
            });
        }

        [
            ["NewRecord_Actuals", "data-actuals"],
            ["NewRecord_ActualsUptoSeptPrevYear", "data-actuals-upto-sept-prev-year"],
            ["NewRecord_BE", "data-be"],
            ["NewRecord_ActualsUptoSept", "data-actuals-upto-sept"],
            ["NewRecord_ProposedRE", "data-proposed-re"],
            ["NewRecord_ProposedNBE", "data-proposed-nbe"],
            ["NewRecord_RemarksMinistry", "data-remarks-ministry"],
            ["NewRecord_RemarksBudget", "data-remarks-budget"],
            ["NewRecord_BudgetRecommendedRE", "data-budget-recommended-re"],
            ["NewRecord_BudgetRecommendedNBE", "data-budget-recommended-nbe"]
        ].forEach(function (pair) {
            var input = form.querySelector("#" + pair[0]);
            if (input) {
                input.value = editButton.getAttribute(pair[1]) || "";
            }
        });

        recalcAppendixIVCalculations(form);

        enterAppendixEditMode(form);
        form.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    // Shared Edit-populate for IV-A/IV-B (Phase 2, 2026-08-14) - same Scheme/SubScheme-graded shape
    // as Appendix IV (identical entity fields, same shared Scheme/SubScheme reference data and
    // loadIVSubSchemesForScheme cascade - see schemeGradedAppendixForms above), generalized instead
    // of duplicating populateAppendixIVEntryForm's body a second and third time. `recalcSavingExcess`
    // is true for IV-A and IV-B (both have a live Saving/Excess display field; plain IV does not).
    function populateSchemeGradedEntryForm(editButton, formId, fieldPairs, recalcSavingExcess, lockIdentity) {       
        // Scoped to `form` (2026-09-15 fix, client report "BE not loading in Appendix IV/IV-A") -
        // was global document.getElementById for NewRecord_Id/NewRecord_SchemeId/every fieldPairs
        // target, which could resolve to a stale duplicate left over in a DIFFERENT one of
        // IV/IV-A/IV-B's forms (they all share these exact ids by design).
        var form = editButton.closest("#" + formId) || document.getElementById(formId);
        if (!form) {
            return;
        }
        var idField = form.querySelector("#NewRecord_Id");
        var schemeSelect = form.querySelector("#NewRecord_SchemeId");
        if (!idField || !schemeSelect) {
            return;
        }

        idField.value = editButton.getAttribute("data-id") || "";
        schemeSelect.value = editButton.getAttribute("data-scheme-id") || "";
        // Client requirement 2026-09-14 (Appendix IV-A only, via lockIdentity): "edit mode scheme
        // and subscheme should be disabled" - scoped to `form`, same reasoning/risk as
        // populateAppendixIVEntryForm's own identical fix above.
        if (lockIdentity) {
            lockSelectForEditScoped(form, "NewRecord_SchemeId");
        }      

        //call in add mode only, Prevent to call in edit mode
        if (idField.value == "") {
            loadIVSubSchemesForScheme(schemeSelect.value, editButton.getAttribute("data-sub-scheme-id"), form, lockIdentity ? function () {
                lockSelectForEditScoped(form, "NewRecord_SubSchemeId");
            } : undefined);
        }
        fieldPairs.forEach(function (pair) {
            var input = form.querySelector("#" + pair[0]);
            if (input) {
                input.value = editButton.getAttribute(pair[1]) || "";
            }
        });

        if (recalcSavingExcess) {
            recalcAppendixIVASavingExcess(form);
        }

        enterAppendixEditMode(form);
        form.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    // "Edit" on an Appendix V-B grid row brings that record's values into the V-B entry form.
    // Object Head is a plain server-rendered dropdown (no cascade), so this is a direct field copy.
    // Appendix I-A's Edit button (client requirement, 2026-08-07): loads a saved row back into the
    // entry form. "Previous Year" Revenue/Capital BE are historical fact pulled from dbo.SBEData
    // (see loadAppendixIAPreviousYearReference) - the client's own note says this pre-filled data
    // "should not be edited", so those two boxes are populated from the ROW'S OWN saved value (not
    // re-fetched, since the row may be from an older year than the current auto-load's target) and
    // forced readonly here, same as the auto-load does on page load. Revenue/Capital RE and every
    // Current Year Ministry Projection field are genuinely user-editable, populated plainly.
    function populateAppendixIAEntryForm(editButton) {
        var idField = document.getElementById("NewRecord_Id");
        if (!idField) {
            return;
        }

        idField.value = editButton.getAttribute("data-id") || "";

        [
            ["NewRecord_PrevYrMinRevenueBE", "data-prev-yr-min-revenue-be", true],
            ["NewRecord_PrevYrMinRevenueRE", "data-prev-yr-min-revenue-re", false],
            ["NewRecord_PrevYrMinCapitalBE", "data-prev-yr-min-capital-be", true],
            ["NewRecord_PrevYrMinCapitalRE", "data-prev-yr-min-capital-re", false],
            ["NewRecord_CurrYrMinRevenueBE", "data-curr-yr-min-revenue-be", false],
            ["NewRecord_CurrYrMinCapitalBE", "data-curr-yr-min-capital-be", false],
            ["NewRecord_CurrYrMinMtefRevenueBE", "data-curr-yr-min-mtef-revenue-be", false],
            ["NewRecord_CurrYrMinMtefCapitalBE", "data-curr-yr-min-mtef-capital-be", false]
        ].forEach(function (entry) {
            var input = document.getElementById(entry[0]);
            if (!input) {
                return;
            }
            input.value = editButton.getAttribute(entry[1]) || "";
            if (entry[2]) {
                input.readOnly = true;
                input.classList.add("bg-slate-100", "dark:bg-slate-800", "cursor-not-allowed");
                input.title = "Historical figure from this saved record - not editable.";
            }
        });

        recalcAppendixIATotals();

        var form = document.getElementById("appendixIAForm");
        if (form) {
            // Client report 2026-09-10: editing an older record whose PrevYrMinRevenueBE/
            // PrevYrMinCapitalBE were saved blank (fetch hadn't found SBEData yet, or the row
            // predates this auto-load) left those 2 boxes permanently blank AND locked read-only
            // above - there was no way to ever recover the right figure once that happened. Re-run
            // the same live SBEData fetch the Add flow already uses (loadAppendixIAPreviousYearReference)
            // right after populating from the saved row - it overwrites these 2 fields with
            // whatever the DB has right now if found, and simply leaves the just-set value alone
            // otherwise (see that function's own "if (!data || !data.found) return;" guard).
            loadAppendixIAPreviousYearReference(form);
            enterAppendixEditMode(form);
            form.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    }

    // Client rule (2026-08-07), confirmed via `select * from Temp_EstExp_ObjHeadwise where
    // FinancialYear=... and DemandID=(select demandid from M_Demand where DemandNo=... and
    // FinancialYear=...)`: Edit reloads THIS SAME ROW's own already-saved values (no external
    // legacy-table lookup involved - Actuals is just a plain figure typed in once when the row was
    // first created, semantically meaning "FY-2"). Once loaded for editing, Actuals/Actuals-upto-
    // Sept (both years) and BE are pre-filled and frozen (read-only) - only Proposed R.E./Proposed
    // B.E. (NBE)/Remarks stay editable, matching "actuals should be pre-filled frozen and proposed
    // to be filled by user".
    function populateAppendixXEntryForm(editButton) {
        var subHead = editButton.getAttribute("data-sub-head");
        var targetRow = document.querySelector('#appendixXForm tr[data-sub-head="' + subHead + '"]');
        if (!targetRow) {
            return;
        }

        var idInput = targetRow.querySelector('input[name$=".Id"]');
        if (idInput) {
            idInput.value = editButton.getAttribute("data-id") || "";
        }

        [
            ["ActualsY1", "data-actuals-y1"],
            ["ActualsY2", "data-actuals-y2"],
            ["ActualsY3", "data-actuals-y3"],
            ["ActualsUptoSept", "data-actuals-upto-sept"],
            ["BE", "data-be"],
            ["RE", "data-re"],
            ["NBE", "data-nbe"]
        ].forEach(function (pair) {
            var input = targetRow.querySelector('input[name$=".' + pair[0] + '"]');
            if (!input) {
                return;
            }
            input.value = editButton.getAttribute(pair[1]) || "";
        });

        targetRow.scrollIntoView({ behavior: "smooth", block: "center" });
        var firstInput = targetRow.querySelector("input:not([type=hidden])");
        if (firstInput) {
            firstInput.focus();
        }
    }

    function populateAppendixVAEntryForm(editButton) {
        var idField = document.getElementById("VANewRecord_Id");
        if (!idField) {
            return;
        }

        idField.value = editButton.getAttribute("data-id") || "";

        [
            ["VANewRecord_AutonomousBodyId", "data-autonomous-body-id"],
            ["VANewRecord_GiaGeneralActuals", "data-gia-general-actuals"],
            ["VANewRecord_GiaGeneralActualsUptoSeptPrevYear", "data-gia-general-actuals-upto-sept-prev-year"],
            ["VANewRecord_GiaGeneralBE", "data-gia-general-be"],
            ["VANewRecord_GiaGeneralActualsUptoSept", "data-gia-general-actuals-upto-sept"],
            ["VANewRecord_GiaGeneralRE", "data-gia-general-re"],
            ["VANewRecord_GiaGeneralNBE", "data-gia-general-nbe"],
            ["VANewRecord_GiaCcaActuals", "data-gia-cca-actuals"],
            ["VANewRecord_GiaCcaActualsUptoSeptPrevYear", "data-gia-cca-actuals-upto-sept-prev-year"],
            ["VANewRecord_GiaCcaBE", "data-gia-cca-be"],
            ["VANewRecord_GiaCcaActualsUptoSept", "data-gia-cca-actuals-upto-sept"],
            ["VANewRecord_GiaCcaRE", "data-gia-cca-re"],
            ["VANewRecord_GiaCcaNBE", "data-gia-cca-nbe"],
            ["VANewRecord_GiaSalaryActuals", "data-gia-salary-actuals"],
            ["VANewRecord_GiaSalaryActualsUptoSeptPrevYear", "data-gia-salary-actuals-upto-sept-prev-year"],
            ["VANewRecord_GiaSalaryBE", "data-gia-salary-be"],
            ["VANewRecord_GiaSalaryActualsUptoSept", "data-gia-salary-actuals-upto-sept"],
            ["VANewRecord_GiaSalaryRE", "data-gia-salary-re"],
            ["VANewRecord_GiaSalaryNBE", "data-gia-salary-nbe"],
            ["VANewRecord_GiaSalaryTotal", "data-gia-salary-total"]
        ].forEach(function (pair) {
            var input = document.getElementById(pair[0]);
            if (!input) {
                return;
            }
            input.value = editButton.getAttribute(pair[1]) || "";
        });

        // Client requirement 2026-09-02 ("on record modification: Name of Autonomous Body dropdown
        // should be freezed"): same locked-but-submitted pattern as VII-A's Major Head and VB's
        // Object Head (see lockSelectForEdit) - the generic reset handler below already calls
        // unlockSelectsAfterEdit on every appendix form's Reset, so no extra reset-side code is
        // needed here to unfreeze it.
        lockSelectForEdit("VANewRecord_AutonomousBodyId");

        var form = document.getElementById("appendixVAForm");
        if (form) {
            enterAppendixEditMode(form);
            form.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    }

    function populateAppendixVCEntryForm(editButton) {
        var idField = document.getElementById("VCNewRecord_Id");
        if (!idField) {
            return;
        }

        idField.value = editButton.getAttribute("data-id") || "";

        [
            ["VCNewRecord_Name", "data-name"],
            ["VCNewRecord_Actuals", "data-actuals"],
            ["VCNewRecord_ActualsUptoSeptPrevYear", "data-actuals-upto-sept-prev-year"],
            ["VCNewRecord_BE", "data-be"],
            ["VCNewRecord_ActualsUptoSept", "data-actuals-upto-sept"],
            ["VCNewRecord_ProposedRE", "data-proposed-re"],
            ["VCNewRecord_ProposedNBE", "data-proposed-nbe"],
            ["VCNewRecord_Remarks", "data-remarks"]
        ].forEach(function (pair) {
            var input = document.getElementById(pair[0]);
            if (!input) {
                return;
            }
            input.value = editButton.getAttribute(pair[1]) || "";
        });

        // Client requirement 2026-09-02 ("on edit mode, this textbox should be freezed"): Name is
        // the uniqueness key (see the server-side duplicate check in AppendixVCController), so it
        // can't be changed mid-edit without risking a silent identity swap - same readonly-freeze
        // treatment as V-B's Actuals/BE (see populateAppendixVBEntryForm). Un-frozen on Reset by
        // the dedicated appendixVCForm reset handler below (readOnly toggling isn't covered by the
        // generic unlockSelectsAfterEdit, which only handles <select> elements). The on-blur
        // Name-normalize rule itself lives in PreBudget-validation.js.
        var vcNameInput = document.getElementById("VCNewRecord_Name");
        if (vcNameInput) {
            vcNameInput.readOnly = true;
            vcNameInput.classList.add("bg-slate-100", "dark:bg-slate-800", "cursor-not-allowed");
            vcNameInput.title = "Name cannot be changed while editing an existing record.";
        }

        var form = document.getElementById("appendixVCForm");
        if (form) {
            enterAppendixEditMode(form);
            form.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    }

    function populateAppendixVBEntryForm(editButton) {
        var idField = document.getElementById("VBNewRecord_Id");
        if (!idField) {
            return;
        }

        idField.value = editButton.getAttribute("data-id") || "";

        // "On edit mode, make textboxes editable: Actuals upto 9/2024, Actuals upto 9/2025" (client
        // testing feedback, 2026-08-25) - only Actuals and BE stay locked as historical/auto-loaded
        // fact; the two "upto Sept" figures are now editable during Edit too.
        var frozenFields = ["VBNewRecord_Actuals", "VBNewRecord_BE"];

        [
            ["VBNewRecord_ObjectHeadId", "data-object-head-id"],
            ["VBNewRecord_Actuals", "data-actuals"],
            ["VBNewRecord_ActualsUptoSeptPrevYear", "data-actuals-upto-sept-prev-year"],
            ["VBNewRecord_BE", "data-be"],
            ["VBNewRecord_ActualsUptoSept", "data-actuals-upto-sept"],
            ["VBNewRecord_ProposedRE", "data-proposed-re"],
            ["VBNewRecord_ProposedNBE", "data-proposed-nbe"],
            ["VBNewRecord_Remarks", "data-remarks"]
        ].forEach(function (pair) {
            var input = document.getElementById(pair[0]);
            if (!input) {
                return;
            }
            input.value = editButton.getAttribute(pair[1]) || "";
            if (frozenFields.indexOf(pair[0]) !== -1) {
                input.readOnly = true;
                input.classList.add("bg-slate-100", "dark:bg-slate-800", "cursor-not-allowed");
                input.title = "Historical figure from this saved record - not editable.";
            }
        });

        // "On edit mode, disable Objecthead dropdown" (client testing feedback, 2026-08-25) - same
        // locked-but-submitted pattern as VII-A's Major Head (see lockSelectForEdit).
        lockSelectForEdit("VBNewRecord_ObjectHeadId");

        var form = document.getElementById("appendixVBForm");
        if (form) {
            enterAppendixEditMode(form);
            form.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    }

    // "Edit" on an Appendix VII-B grid row. BE/ActualsY2 are deliberately NOT copied from the
    // button's own data attributes - those two are always a LIVE auto-load from SBEData for
    // whatever Scheme/Major Head/Transaction Type is currently selected (see loadAppendixVIIBBeActuals),
    // not a frozen historical snapshot, matching the view's own "Auto-loaded..." field titles - so
    // replaying the Scheme->MajorHead cascade naturally repopulates them correctly via the existing
    // mechanism instead of needing a second, competing data source here.
    function populateAppendixVIIBEntryForm(editButton) {
        var idField = document.getElementById("NewRecord_Id");
        var schemeSelect = document.getElementById("NewRecord_SchemeId");
        var transactionTypeSelect = document.getElementById("NewRecord_TransactionType");
        var form = document.getElementById("appendixVIIBForm");
        if (!idField || !schemeSelect || !transactionTypeSelect || !form) {
            return;
        }

        idField.value = editButton.getAttribute("data-id") || "";
        schemeSelect.value = editButton.getAttribute("data-scheme-id") || "";
        transactionTypeSelect.value = editButton.getAttribute("data-transaction-type") || "";
        // Client requirement 2026-09-14: "VII-B: edit mode disable all dropdowns and retain their
        // value on reset button click" - Scheme/Transaction Type lock immediately (their values are
        // already known synchronously); Major Head locks only once loadAppendixVIIBMajorHeads' AJAX
        // load has actually populated it, matching the same "lock after cascade" convention used
        // for Appendix III-B/VI-B's own Scheme/Sub-Scheme.
        lockSelectForEdit("NewRecord_SchemeId");
        lockSelectForEdit("NewRecord_TransactionType");
        loadAppendixVIIBMajorHeads(form.getAttribute("data-demand-id"), schemeSelect.value, editButton.getAttribute("data-major-head-id"), editButton.getAttribute("data-major-head-code"), function () {
            lockSelectForEdit("NewRecord_MajorHeadId");
        });

        [
            ["NewRecord_ActualsY1", "data-actuals-y1"],
            ["NewRecord_ActualsUptoSept", "data-actuals-upto-sept"],
            ["NewRecord_ActualsUptoSeptPrevYear", "data-actuals-upto-sept-prev-year"],
            ["NewRecord_RE", "data-re"],
            ["NewRecord_NBE", "data-nbe"]
        ].forEach(function (pair) {
            var input = document.getElementById(pair[0]);
            if (input) {
                input.value = editButton.getAttribute(pair[1]) || "";
            }
        });

        recalcAppendixVIIBIncreasedBE();
        enterAppendixEditMode(form);
        form.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    function populateSimpleEntryForm(editButton, formId, fieldPairs, skipId) {
        if (!skipId) {
            var idField = document.getElementById("NewRecord_Id");
            if (idField) {
                idField.value = editButton.getAttribute("data-id") || "";
            }
        }

        fieldPairs.forEach(function (pair) {
            var input = document.getElementById(pair[0]);
            if (!input) {
                return;
            }
            var raw = editButton.getAttribute(pair[1]) || "";
            if (input.type === "checkbox") {
                input.checked = raw.toLowerCase() === "true";
            } else {
                input.value = raw;
            }
        });

        var form = document.getElementById(formId);
        if (form) {
            // Client requirement 2026-08-11 (Phase 2 rollout): every appendix using this shared
            // populate function now also gets the Modify/Cancel treatment - see
            // enterAppendixEditMode's own doc comment.
            enterAppendixEditMode(form);
            form.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    }

    // ----------------------------------------------------------------------------------------
    // Event wiring
    // ----------------------------------------------------------------------------------------

    function attachListeners() {
        // "On Edit Click, row should be selected and highlighted (All Appendix)" (client testing
        // feedback, 2026-08-24) - generic across every appendix's history grid: any button whose
        // data-action starts with "edit-" highlights its own <tr> (clearing the highlight from every
        // other row in the same table first). Runs independently of - and before - the specific
        // populate*EntryForm dispatch below, since it only needs the clicked element, not any
        // appendix-specific field knowledge.
        container.addEventListener("click", function (event) {
            var editButton = event.target.closest('[data-action^="edit-"]');
            if (!editButton) {
                return;
            }
            var row = editButton.closest("tr");
            var table = row && row.closest("table");
            if (!table) {
                return;
            }
            Array.prototype.forEach.call(table.querySelectorAll("tbody tr.appendix-row-editing"), function (r) {
                r.classList.remove("appendix-row-editing");
            });
            row.classList.add("appendix-row-editing");
        });

        // Clears the edit-row highlight whenever edit mode itself is exited (Reset, or the dead
        // cancel-edit button if a view ever adds one) - see the "reset" listener pairing this with
        // exitAppendixEditMode's label-revert below.
        container.addEventListener("reset", function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement)) {
                return;
            }
            Array.prototype.forEach.call(container.querySelectorAll("tr.appendix-row-editing"), function (r) {
                r.classList.remove("appendix-row-editing");
            });
        });

        // Client report 2026-09-10 ("Reset and Cancel not working properly for the newer drawer
        // design - should clear ALL fields; on a duplicate validation the field is not clearing").
        // The drawer footer's <button type="reset"> only restores INITIAL values - a generic hook
        // here (on `document`, since drawer forms are reparented out of `container`) makes it
        // hard-clear every redesigned drawer's fields - identified by the .ubis-modern-appendix
        // class every reparented drawer <aside> carries.
        document.addEventListener("reset", function (event) {
            if (_appendixHardClearing) {
                return;
            }
            var form = event.target;
            if (!(form instanceof HTMLFormElement) || !form.closest(".ubis-modern-appendix")) {
                return;
            }
            // Run after the browser's own native reset has restored initial values.
            window.setTimeout(function () { hardClearAppendixForm(form); }, 0);
        });

        // Client requirement 2026-09-17 (reversal of the 2026-09-10 behavior above): Cancel
        // ([data-action="close-appendix-*-drawer"]) must NOT clear the form's fields anymore - it
        // only slides the drawer shut (each appendix's own drawer JS handles the close itself,
        // now behind a "Do you want to Cancel?" confirmation). The generic hard-clear-on-Cancel
        // hook that used to live here was removed; nothing else needs to run for this click.

        // Client report 2026-09-10: pressing Enter in a drawer Add/Modify field fired the FREEZE
        // confirmation instead of Save. The redesigned views moved Freeze/Nil out of the <form>
        // into the page header, associated back to it via form="appendixXxxForm" - so they're in
        // the form's submit-button list, and because implicit form submission ("Enter" in a text
        // field) uses the *first submit button in DOM/tree order*, the header's Freeze button
        // (which sits before the drawer, and ends up even earlier once the drawer is reparented to
        // <body>) won that race. Intercept Enter inside a redesigned drawer form and submit it via
        // the form's OWN Save/Submit button (the one with no formaction -> posts to SaveAppendixXxx).
        document.addEventListener("keydown", function (event) {
            if (event.key !== "Enter" || event.isComposing) {
                return;
            }
            var el = event.target;
            if (!(el instanceof HTMLElement)) {
                return;
            }
            var tag = el.tagName;
            // Enter in a textarea is a newline; on a button/link it's that element's own activation.
            if (tag !== "INPUT" && tag !== "SELECT") {
                return;
            }
            if (tag === "INPUT" && /^(button|submit|reset|checkbox|radio|file)$/i.test(el.type)) {
                return;
            }
            var form = el.closest("form");
            if (!form || !form.id || !/^appendix.*Form$/i.test(form.id) || !form.closest(".ubis-modern-appendix")) {
                return;
            }
            var submitBtn = form.querySelector('button[type="submit"]:not([formaction]), input[type="submit"]:not([formaction])');
            if (!submitBtn || submitBtn.disabled) {
                return;
            }
            event.preventDefault();
            if (typeof form.requestSubmit === "function") {
                form.requestSubmit(submitBtn);
            } else {
                submitBtn.click();
            }
        });

        // Widened from `container` to `document` (2026-09-08) - every branch below dispatches on a
        // specific, unique data-action value (never a bare selector like 'button[type=submit]'), so
        // this is safe to fire for the whole document: no branch can accidentally match an unrelated
        // element elsewhere on the page. Needed because a redesigned appendix's Edit button lives
        // inside its row's "..." action-menu, which gets reparented to document.body when opened
        // (same fix class as the Delete-button/Freeze-validation bugs fixed the same day) - a
        // container-scoped listener stops seeing it the moment that reparenting happens.
        // IMPORTANT for future appendix redesigns: once an appendix here gets its OWN dedicated
        // drawer JS file (appendix-vi{a,b,c,d,e}-drawer.js is the existing example), remove that
        // appendix's own branch from this shared dispatcher - leaving it in place would double-fire
        // alongside the new dedicated handler now that both are document-scoped (this is exactly
        // why VI-A/B/C/D/E's own branches were removed from here on this same date).
        document.addEventListener("click", function (event) {
            var editButton = event.target.closest('[data-action="edit-row"]');
            if (editButton) {
                var targetRow = container.querySelector('tr[data-year="' + editButton.getAttribute("data-year") + '"]');
                if (targetRow) {
                    populateRowFromButton(editButton, targetRow);
                }
                return;
            }

            var editIIIButton = event.target.closest('[data-action="edit-appendix-iii-record"]');
            if (editIIIButton) {
                populateAppendixIIIEntryForm(editIIIButton);
                return;
            }

            var editIIIAButton = event.target.closest('[data-action="edit-appendix-iiia-record"]');
            if (editIIIAButton) {
                populateSimpleEntryForm(editIIIAButton, "appendixIIIAForm", [
                    ["NewRecord_EntityName", "data-entity-name"],
                    ["NewRecord_BE", "data-be"],
                    ["NewRecord_TsaAssignmentAsOnSept", "data-tsa-assignment-as-on-sept"],
                    ["NewRecord_ActualExpenditureUptoSept", "data-actual-expenditure-upto-sept"],
                    ["NewRecord_DateOfLastAssignment", "data-date-of-last-assignment"],
                    ["NewRecord_AmountOfLastAssignment", "data-amount-of-last-assignment"]
                ]);
                // Client report 2026-09-10 ("auto calculate BE for add/edit"): BE is only ever
                // auto-fetched live (loadAppendixIIIABePreviousYear, from this Entity's own III-A
                // record one year prior) on the "change" event of NewRecord_EntityName - which only
                // fires from a real user keystroke+blur, never from populateSimpleEntryForm setting
                // .value programmatically above. Edit was therefore always showing this row's own
                // saved BE (data-be) rather than the current live calculation. Re-running the same
                // fetch here refreshes it, same fix pattern as Appendix I-A/II's own PrevYear boxes.
                var iiiaFormForBeRefetch = document.getElementById("appendixIIIAForm");
                if (iiiaFormForBeRefetch) {
                    loadAppendixIIIABePreviousYear(iiiaFormForBeRefetch.getAttribute("data-demand-id"), editIIIAButton.getAttribute("data-entity-name") || "");
                    // Bug report 2026-09-10 (screenshot): loadPreviousYearFields blanks BE
                    // synchronously before its async lookup, and when this Entity has no
                    // prior-year III-A record the lookup returns nothing - leaving BE empty on
                    // Edit. Restore this row's own saved BE right after so it's the fallback; a
                    // successful prior-year lookup still overwrites it a moment later.
                    var iiiaBeInput = document.getElementById("NewRecord_BE");
                    if (iiiaBeInput) {
                        iiiaBeInput.value = editIIIAButton.getAttribute("data-be") || "";
                    }
                }
                recalcAppendixIIIAUnspentAssignment();
                return;
            }

            var editIIIBButton = event.target.closest('[data-action="edit-appendix-iiib-record"]');
            if (editIIIBButton) {
                // Category + Scheme are now cascading <select>s (2026-09-10) - the Category value
                // and the Scheme AJAX cascade are handled in appendix-iiib-drawer.js's own
                // edit-appendix-iiib-record listener; here we only populate NewRecord_Id, the 3
                // text fields and enter edit mode. NewRecord_IIIBCategoryId (not the bare id) - see
                // AppendixIIIB.cshtml's comment: the bare "NewRecord_CategoryId" collides with
                // Appendix III's Balance Type <select>.
                populateSimpleEntryForm(editIIIBButton, "appendixIIIBForm", [
                    ["NewRecord_IIIBCategoryId", "data-category-id"],
                    ["NewRecord_StatusOfFreshAppraisalApproval", "data-status-of-fresh-appraisal-approval"],
                    ["NewRecord_SchemeApprovalValidUpto", "data-scheme-approval-valid-upto"],
                    ["NewRecord_Remarks", "data-remarks"]
                ]);
                return;
            }

            var editIVButton = event.target.closest('[data-action="edit-appendix-iv-record"]');
            if (editIVButton) {
                populateAppendixIVEntryForm(editIVButton);
                return;
            }

            var editIAButton = event.target.closest('[data-action="edit-appendix-ia-row"]');
            if (editIAButton) {
                populateAppendixIAEntryForm(editIAButton);
                return;
            }

            // Appendix X's history grid "Edit" (restored 2026-08-24, client testing feedback) - the
            // Client requirement 2026-08-27: entry rows no longer pre-fill from the DB (duplicate
            // additions must be allowed), so Edit now has to actually copy the clicked grid row's
            // saved values into the matching fixed sub-head row above - same populate-then-Submit
            // pattern every other appendix's Edit button uses - and set that row's hidden Id so
            // Submit updates it instead of creating a duplicate.
            var editXButton = event.target.closest('[data-action="edit-appendix-x-row"]');
            if (editXButton) {
                populateAppendixXEntryForm(editXButton);
                return;
            }

            var editIIButton = event.target.closest('[data-action="edit-appendix-ii-record"]');
            if (editIIButton) {
                populateSimpleEntryForm(editIIButton, "appendixIIForm", [
                    ["NewRecord_Q1ApprovedQepPrevYear", "data-q1-approved-qep-prev-year"],
                    ["NewRecord_Q1ActualsPrevYear", "data-q1-actuals-prev-year"],
                    ["NewRecord_Q1ApprovedQep", "data-q1-approved-qep"],
                    ["NewRecord_Q1Actuals", "data-q1-actuals"],
                    ["NewRecord_RemarksQ1", "data-remarks-q1"],
                    ["NewRecord_Q1HasDeviation", "data-q1-has-deviation"],
                    ["NewRecord_Q1MofApprovalDetails", "data-q1-mof-approval-details"],
                    ["NewRecord_Q2ApprovedQepPrevYear", "data-q2-approved-qep-prev-year"],
                    ["NewRecord_Q2ActualsPrevYear", "data-q2-actuals-prev-year"],
                    ["NewRecord_Q2ApprovedQep", "data-q2-approved-qep"],
                    ["NewRecord_Q2Actuals", "data-q2-actuals"],
                    ["NewRecord_RemarksQ2", "data-remarks-q2"],
                    ["NewRecord_Q2HasDeviation", "data-q2-has-deviation"],
                    ["NewRecord_Q2MofApprovalDetails", "data-q2-mof-approval-details"]
                ]);
                // "Total should auto-calculate in edit mode too" (client testing feedback, 2026-08-24) -
                // populateSimpleEntryForm sets .value directly, which doesn't dispatch an "input" event,
                // so the live Totals row (recalcAppendixIITotals, wired to typing) never ran after Edit.
                recalcAppendixIITotals();
                // Bug report 2026-09-10: same "no event dispatched" gap for the per-quarter
                // Deviation checkboxes - populateSimpleEntryForm sets .checked directly, so
                // appendix-ii-drawer.js's applyAppendixIIDeviationUI (wired to the checkbox
                // "change") never ran, leaving the "Remarks (mandatory if deviation)" box hidden
                // on Edit even when the row was saved WITH a deviation. Fire the change now that
                // both checkboxes hold the row's saved state and the MoF-details value is set.
                ["NewRecord_Q1HasDeviation", "NewRecord_Q2HasDeviation"].forEach(function (id) {
                    var cb = document.getElementById(id);
                    if (cb) {
                        cb.dispatchEvent(new Event("change", { bubbles: true }));
                    }
                });
                // Client report 2026-09-10 ("auto fetch values of Approved QEP... working before
                // redesign"): same root cause/fix as Appendix I-A's populate function - the Q1/Q2
                // ApprovedQepPrevYear boxes just set above came from THIS ROW's own saved value,
                // which is blank whenever the record predates this auto-load or the prior year's
                // QEPData wasn't available yet at save time - and since they're then locked read-
                // only, the user has no way to fix it by hand. Re-running the same live dbo.T_QEPData
                // fetch the Add flow already uses re-populates them from the DB right now.
                var appendixIIFormForRefetch = document.getElementById("appendixIIForm");
                if (appendixIIFormForRefetch) {
                    loadAppendixIIPreviousYearReference(appendixIIFormForRefetch);
                }
                return;
            }

            var editVAButton = event.target.closest('[data-action="edit-appendix-va-record"]');
            if (editVAButton) {
                populateAppendixVAEntryForm(editVAButton);
                return;
            }

            var editVBButton = event.target.closest('[data-action="edit-appendix-vb-record"]');
            if (editVBButton) {
                populateAppendixVBEntryForm(editVBButton);
                return;
            }

            var editVCButton = event.target.closest('[data-action="edit-appendix-vc-record"]');
            if (editVCButton) {
                populateAppendixVCEntryForm(editVCButton);
                return;
            }

            var editVIIBButton = event.target.closest('[data-action="edit-appendix-viib-record"]');
            if (editVIIBButton) {
                populateAppendixVIIBEntryForm(editVIIBButton);
                return;
            }

            var editPAButton = event.target.closest('[data-action="edit-appendix-pa-record"]');
            if (editPAButton) {
                populateSimpleEntryForm(editPAButton, "appendixPAForm", [
                    ["NewRecord_MajorHeadId", "data-major-head-id"],
                    ["NewRecord_ActualReceipt", "data-actual-receipt"],
                    ["NewRecord_ActualPayment", "data-actual-payment"],
                    ["NewRecord_BalanceAtEndReceipt", "data-balance-at-end-receipt"],
                    ["NewRecord_BalanceAtEndPayment", "data-balance-at-end-payment"],
                    ["NewRecord_BEReceipt", "data-be-receipt"],
                    ["NewRecord_BEPayment", "data-be-payment"],
                    ["NewRecord_AdjustmentReceipt", "data-adjustment-receipt"],
                    ["NewRecord_AdjustmentPayment", "data-adjustment-payment"],
                    ["NewRecord_REReceipt", "data-re-receipt"],
                    ["NewRecord_REPayment", "data-re-payment"],
                    ["NewRecord_NBEReceipt", "data-nbe-receipt"],
                    ["NewRecord_NBEPayment", "data-nbe-payment"],
                    ["NewRecord_RemarksReceipt", "data-remarks-receipt"],
                    ["NewRecord_RemarksPayment", "data-remarks-payment"]
                ]);
                return;
            }

            var editVIIAButton = event.target.closest('[data-action="edit-appendix-viia-record"]');
            if (editVIIAButton) {
                populateSimpleEntryForm(editVIIAButton, "appendixVIIAForm", [
                    ["NewRecord_MajorHeadId", "data-major-head-id"],
                    ["NewRecord_SchemeName", "data-scheme-name"],
                    ["NewRecord_Actuals", "data-actuals"],
                    ["NewRecord_BE", "data-be"],
                    ["NewRecord_RE", "data-re"],
                    ["NewRecord_NBE", "data-nbe"]
                ]);
                // "On Edit Mode, Major Head should be selected properly but disabled" (client testing
                // feedback, 2026-08-24) - Major Head is part of the uniqueness key (Major Head + Scheme
                // Name) so it shouldn't change mid-edit, but a disabled <select> is excluded from
                // FormData entirely, so a hidden mirror input carries its value through the AJAX
                // submit instead. See the matching "reset" cleanup below.
                lockSelectForEdit("NewRecord_MajorHeadId");
                return;
            }

            var editIVAButton = event.target.closest('[data-action="edit-appendix-iva-record"]');
            if (editIVAButton) {
                populateSchemeGradedEntryForm(editIVAButton, "appendixIVAForm", [
                    ["NewRecord_Actuals", "data-actuals"],
                    ["NewRecord_ActualsUptoSeptPrevYear", "data-actuals-upto-sept-prev-year"],
                    ["NewRecord_BE", "data-be"],
                    ["NewRecord_ActualsUptoSept", "data-actuals-upto-sept"],
                    ["NewRecord_ProposedRE", "data-proposed-re"],
                    ["NewRecord_ProposedNBE", "data-proposed-nbe"],
                    ["NewRecord_RemarksMinistry", "data-remarks-ministry"],
                    ["NewRecord_RemarksBudget", "data-remarks-budget"],
                    ["NewRecord_BudgetRecommendedRE", "data-budget-recommended-re"],
                    ["NewRecord_BudgetRecommendedNBE", "data-budget-recommended-nbe"]
                ], true, true);
                return;
            }

            var editIVBButton = event.target.closest('[data-action="edit-appendix-ivb-record"]');
            if (editIVBButton) {
                populateSchemeGradedEntryForm(editIVBButton, "appendixIVBForm", [
                    ["NewRecord_Actuals", "data-actuals"],
                    ["NewRecord_ActualsUptoSeptPrevYear", "data-actuals-upto-sept-prev-year"],
                    ["NewRecord_BE", "data-be"],
                    ["NewRecord_ActualsUptoSept", "data-actuals-upto-sept"],
                    ["NewRecord_ProposedRE", "data-proposed-re"],
                    ["NewRecord_ProposedNBE", "data-proposed-nbe"],
                    ["NewRecord_RemarksMinistry", "data-remarks-ministry"],
                    ["NewRecord_RemarksBudget", "data-remarks-budget"],
                    ["NewRecord_BudgetRecommendedRE", "data-budget-recommended-re"],
                    ["NewRecord_BudgetRecommendedNBE", "data-budget-recommended-nbe"]
                ], true, true);
                return;
            }

            var editVIButton = event.target.closest('[data-action="edit-appendix-vi-record"]');
            if (editVIButton) {
                populateSimpleEntryForm(editVIButton, "appendixVIForm", [
                    ["NewRecord_ReceiptTypeId", "data-receipt-type-id"],
                    ["NewRecord_PsuReceiptName", "data-psu-receipt-name"],
                    ["NewRecord_Actuals", "data-actuals"],
                    ["NewRecord_BE", "data-be"],
                    ["NewRecord_ActualsUptoSept", "data-actuals-upto-sept"],
                    ["NewRecord_ProposedBE", "data-proposed-be"],
                    ["NewRecord_ProposedCollectionQ3", "data-q3"],
                    ["NewRecord_ProposedCollectionQ4", "data-q4"],
                    ["NewRecord_Remarks", "data-remarks"]
                ]);
                // Client report 2026-09-10 pattern (same fix as Appendix I-A/II's own PrevYear
                // boxes): Actuals/BE/ActualsUptoSept are auto-fetched live from this Receipt Type's
                // own history one year prior - re-run that fetch here so Edit shows the current DB
                // value instead of trusting this row's own saved (possibly stale) value.
                var viFormForRefetch = document.getElementById("appendixVIForm");
                if (viFormForRefetch) {
                    loadAppendixVIPreviousYearReference(viFormForRefetch.getAttribute("data-demand-id"), editVIButton.getAttribute("data-receipt-type-id") || "");
                }
                // Client 2026-09-10: Demand + Receipt Type + PSU/Receipt Name is the record's
                // identity (the dup key), so lock both in Edit mode - the <select> via
                // lockSelectForEdit (disabled + hidden mirror so it still posts), the text input
                // read-only. appendix-vi-drawer.js's Add-mode branch undoes both.
                lockSelectForEdit("NewRecord_ReceiptTypeId");
                var viPsuLock = document.getElementById("NewRecord_PsuReceiptName");
                if (viPsuLock) {
                    viPsuLock.readOnly = true;
                    viPsuLock.classList.add("bg-slate-100", "cursor-not-allowed");
                }
                return;
            }

            // VI-A/B/C/D/E's own edit-populate branches lived here until 2026-09-08 - removed as
            // dead code once each got its own redesigned drawer (appendix-vi{a,b,c,d,e}-drawer.js),
            // which owns this exact dispatch itself (data-action="edit-appendix-vi*-record") via its
            // own document-level listener. Left in place here, they'd now double-fire alongside
            // those dedicated handlers the moment this listener below is widened from `container` to
            // `document` (needed so every OTHER appendix's edit-populate keeps working once its own
            // drawer reparents to body) - see this function's own header comment for that widening.

            // Appendix V-A/V-B/V-C share one page with 3 tabs (approved design 2026-07-30) - all 3
            // tabs' data is already loaded server-side in one shot, so switching tabs is just a
            // show/hide, no round trip.
            var tabButton = event.target.closest('[data-action="switch-appendix-v-tab"]');
            if (tabButton) {
                var targetTab = tabButton.getAttribute("data-tab");
                container.querySelectorAll("[data-tab-panel]").forEach(function (panel) {
                    panel.style.display = panel.getAttribute("data-tab-panel") === targetTab ? "block" : "none";
                });
                container.querySelectorAll('[data-action="switch-appendix-v-tab"]').forEach(function (btn) {
                    var isActive = btn === tabButton;
                    btn.classList.toggle("border-teal", isActive);
                    btn.classList.toggle("text-teal", isActive);
                    btn.classList.toggle("border-transparent", !isActive);
                    btn.classList.toggle("text-slate-500", !isActive);
                });
                return;
            }
        });

        // Client report 2026-09-10: "auto fetch values working during edit but not during add new
        // value". Root cause: opening the Add drawer (appendix-ia-drawer.js's openAppendixIADrawer)
        // calls exitAppendixEditMode(form), which calls form.reset() - wiping the
        // PrevYrMinRevenueBE/PrevYrMinCapitalBE values loadAppendixIAPreviousYearReference had
        // already fetched and set at page load, with nothing re-fetching them afterward (the fields
        // are also readonly, so the user can't just retype them either). A programmatic form.reset()
        // call fires a real "reset" event same as a native <button type="reset"> click (per DOM
        // spec, unlike submit()), so this single document-level listener catches both: the Add-
        // drawer's own reset-to-blank AND a literal Reset-button click while already on this form.
        document.addEventListener("reset", function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement) || form.id !== "appendixIAForm") {
                return;
            }
            window.setTimeout(function () {
                loadAppendixIAPreviousYearReference(form);
            }, 0);
        });

        // Same fix, same bug class, Appendix II's own Q1/Q2 ApprovedQepPrevYear boxes (client
        // report 2026-09-10) - see the appendixIAForm listener above for the full root-cause
        // explanation (opening Add/clicking Reset calls form.reset(), which wipes these fields
        // with nothing re-fetching them afterward).
        document.addEventListener("reset", function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement) || form.id !== "appendixIIForm") {
                return;
            }
            window.setTimeout(function () {
                loadAppendixIIPreviousYearReference(form);
            }, 0);
        });

        // Clears the record being edited (NewRecord.Id) and restores Scheme/SubScheme to their
        // disabled placeholder state - a native <button type="reset"> only resets input VALUES, it
        // doesn't know to also re-disable/re-populate the two dependent dropdowns or blank the hidden
        // edit-id field.
        // Widened container -> document 2026-09-08 (Appendix III drawer redesign): the branch below
        // already self-scopes via form.id === "appendixIIIForm", so a reparented-to-body III drawer's
        // native reset button still fires this handler correctly.
        document.addEventListener("reset", function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement) || form.id !== "appendixIIIForm") {
                return;
            }

            window.setTimeout(function () {
                var idField = document.getElementById("NewRecord_Id");
                if (idField) {
                    idField.value = "";
                }

                var schemeSelect = document.getElementById("NewRecord_SchemeId");
                if (schemeSelect) {
                    schemeSelect.innerHTML = '<option value="">Select Balance Type first</option>';
                    schemeSelect.disabled = true;
                }

                var subSchemeSelect = document.getElementById("NewRecord_SubSchemeId");
                if (subSchemeSelect) {
                    subSchemeSelect.innerHTML = '<option value="">Select Scheme first</option>';
                    subSchemeSelect.disabled = true;
                }
            }, 0);
        });

        // Appendix IV: Scheme is a plain server-rendered dropdown, but since 2026-08-24 it now carries
        // the just-submitted selection forward across Submit (see PreBudgetMeetingController.AppendixIV.cs),
        // so a native reset alone would restore that carried-forward Scheme instead of clearing it -
        // client testing feedback explicitly wants Reset to blank the Scheme dropdown too, along with
        // every non-DB-sourced textbox (native reset already handles those, since NewRecord's other
        // fields default blank) and the three live "Auto" calculated displays.
        container.addEventListener("reset", function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement) || form.id !== "appendixIVForm") {
                return;
            }

            window.setTimeout(function () {
                var idField = document.getElementById("NewRecord_Id");
                if (idField) {
                    idField.value = "";
                }

                var schemeSelect = document.getElementById("NewRecord_SchemeId");
                if (schemeSelect) {
                    schemeSelect.value = "";
                }

                var subSchemeSelect = document.getElementById("NewRecord_SubSchemeId");
                if (subSchemeSelect) {
                    subSchemeSelect.innerHTML = '<option value="">Select a Scheme first</option>';
                    subSchemeSelect.disabled = true;
                }

                recalcAppendixIVCalculations();
                filterAppendixSchemeGridByScheme("appendixIVForm", "", "");
            }, 0);
        });

        // Same idea again for Appendix VI-B.
        container.addEventListener("reset", function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement) || form.id !== "appendixVIBForm") {
                return;
            }

            window.setTimeout(function () {
                var idField = document.getElementById("NewRecord_Id");
                if (idField) {
                    idField.value = "";
                }

                var categorySelect = document.getElementById("CategoryId");
                if (categorySelect) {
                    categorySelect.value = "";
                }

                var schemeSelect = document.getElementById("NewRecord_SchemeId");
                if (schemeSelect) {
                    schemeSelect.innerHTML = '<option value="">Select a Category first</option>';
                    schemeSelect.disabled = true;
                }

                var subSchemeSelect = document.getElementById("NewRecord_SubSchemeId");
                if (subSchemeSelect) {
                    subSchemeSelect.innerHTML = '<option value="">Select a Scheme first</option>';
                    subSchemeSelect.disabled = true;
                }
            }, 0);
        });

        // Appendix VI/VI-A/VI-C/VI-D/VI-E have no dependent dropdown to reset - just the hidden edit-id.
        ["appendixVIForm", "appendixVIAForm", "appendixVICForm", "appendixVIDForm", "appendixVIEForm"].forEach(function (formId) {
            container.addEventListener("reset", function (event) {
                var form = event.target;
                if (!(form instanceof HTMLFormElement) || form.id !== formId) {
                    return;
                }

                window.setTimeout(function () {
                    var idField = document.getElementById("NewRecord_Id");
                    if (idField) {
                        idField.value = "";
                    }
                }, 0);
            });
        });

        // Appendix V-B: clears the hidden edit-id on Reset (Object Head has no cascade to reset).
        container.addEventListener("reset", function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement) || form.id !== "appendixVBForm") {
                return;
            }

            window.setTimeout(function () {
                var idField = document.getElementById("VBNewRecord_Id");
                if (idField) {
                    idField.value = "";
                }
            }, 0);
        });

        container.addEventListener("click", function (event) {
            var cancelEditButton = event.target.closest('[data-action="cancel-edit"]');
            if (cancelEditButton) {
                exitAppendixEditMode(cancelEditButton.closest("form"));
                // Restore any saved filters for Appendix III saved when the drawer
                // was opened for edit so the grid remains as the user filtered it.
                try {
                    var saved = window._iiiSavedFilters || null;
                    if (saved) {
                        var bt = document.getElementById("iiiBalanceTypeFilter");
                        var sch = document.getElementById("iiiSchemeFilter");
                        var sub = document.getElementById("iiiSubSchemeFilter");
                        if (bt && saved.balanceType !== null) { bt.value = saved.balanceType; }
                        if (sch && saved.scheme !== null) { sch.value = saved.scheme; }
                        if (sub && saved.subScheme !== null) { sub.value = saved.subScheme; }
                        if (typeof applyGridFilters === "function") { applyGridFilters(); }
                    }
                } catch (e) { /* ignore */ }
            }
        });

        // "Reset is disabled even after Modify button, for all Appendix" (client testing feedback,
        // 2026-08-24): none of the appendix views actually have a `[data-action="cancel-edit"]` button
        // (enterAppendixEditMode's own doc comment describes one, but it was never built into any
        // view), so the native Reset button was the only way out of edit mode - and clicking it only
        // ran the browser's own form.reset() (clears field VALUES), never exitAppendixEditMode (which
        // reverts the Submit button's label back from "Modify" and clears the hidden Id field). The
        // button stayed stuck on "Modify" after Reset, so the next Submit tried to update the
        // just-discarded record instead of creating a new one - reads as "Reset doesn't work"/
        // "disabled" even though the field values themselves did clear. Runs for every appendix form,
        // generically, alongside (not instead of) each form's own more specific reset handler above.
        container.addEventListener("reset", function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement)) {
                return;
            }
            // Inlines exitAppendixEditMode's label-revert/Id-clear steps only, deliberately NOT calling
            // form.reset() again here - the browser already ran it (that's why this listener fired),
            // and exitAppendixEditMode's own form.reset() call would otherwise re-dispatch this same
            // "reset" event and recurse.
            window.setTimeout(function () {
                var idField = form.querySelector('input[id$="_Id"]');
                if (idField) {
                    idField.value = "";
                }

                var submitButton = form.querySelector('button[type="submit"]');
                if (submitButton && submitButton.hasAttribute("data-original-label")) {
                    submitButton.innerHTML = submitButton.getAttribute("data-original-label");
                }

                var cancelButton = form.querySelector('[data-action="cancel-edit"]');
                if (cancelButton) {
                    cancelButton.classList.add("hidden");
                }

                unlockSelectsAfterEdit(form);
            }, 0);
        });

        // Client requirement 2026-09-02: on blur, Appendix V-C's Name field is normalized - see
        // PreBudget-validation.js for that rule. This listener only un-freezes the field on Reset
        // (see the readonly-freeze set in populateAppendixVCEntryForm above) - same shape as VII-A's
        // Actuals-unlock reset handler below.
        container.addEventListener("reset", function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement) || form.id !== "appendixVCForm") {
                return;
            }
            var vcNameInput = document.getElementById("VCNewRecord_Name");
            if (vcNameInput) {
                vcNameInput.readOnly = false;
                vcNameInput.classList.remove("bg-slate-100", "dark:bg-slate-800", "cursor-not-allowed");
                vcNameInput.title = "";
            }
        });

        // Appendix III's Balance Type -> Scheme -> SubScheme cascade, Appendix IV's, and Appendix
        // VI-B's plain Scheme -> SubScheme cascades all share the same field ids (all bind to
        // "NewRecord.SchemeId"/"NewRecord.SubSchemeId") since only one of their fragments is ever
        // loaded into `container` at a time - disambiguated here by which form is actually present, so
        // each appendix's own cascade endpoint gets called instead of another's.
        // Widened from `container` to `document` (2026-09-08, same reasoning as the click
        // dispatcher above): every branch here checks a specific field id + `.closest("#form")`
        // ancestor, so this is safe application-wide - needed once a redesigned appendix's drawer
        // (and the Category/Scheme select living inside it) is reparented to document.body.
        document.addEventListener("change", function (event) {
            // Appendix VI-D/VI-E's saved-records grid filter (client requirement 2026-08-27, VI-D
            // first, VI-E asked the same day: "filter not working based of dropdown" on VI-E's own
            // identical Select Autonomous Body / grid pair) - reuses the entry form's own Autonomous
            // Body select (used to add a new record) to also filter the grid, same "one select does
            // double duty" pattern Appendix III/IV use for their own Scheme dropdowns. Not the shared
            // filterAppendixSchemeGridByScheme other appendixes use, since this filters by Autonomous
            // Body, not Scheme. VI-C is deliberately NOT included - out of scope, not asked for.
            // Bug fix 2026-08-31 (Appendix VI-D: "next input text boxes should be blank" not actually
            // clearing on Autonomous Body change): this block used to `return` unconditionally the
            // instant it matched VI-D/VI-E's own form, which silently skipped the LATER
            // "NewRecord_AutonomousBodyId" block below (loadAppendixVICPreviousYearReference/
            // loadAppendixVIEPreviousYearReference/loadAppendixVIDPreviousYearReference, plus this
            // fix's own field-clearing) for every single Autonomous Body change on VI-D/VI-E - dead
            // code ever since grid filtering was added, not just for this new requirement. Filters the
            // grid here, but deliberately does NOT return, so execution falls through to that block.
            var autonomousBodyFilterGrids = { appendixVIDForm: "appendixVIDGrid", appendixVIEForm: "appendixVIEGrid" };
            if (event.target.id === "NewRecord_AutonomousBodyId") {
                for (var abFormId in autonomousBodyFilterGrids) {
                    if (!event.target.closest("#" + abFormId)) {
                        continue;
                    }
                    var abBodyId = event.target.value;
                    var abGrid = document.querySelector("#" + autonomousBodyFilterGrids[abFormId] + " tbody");
                    if (abGrid) {
                        Array.prototype.forEach.call(abGrid.querySelectorAll("tr[data-autonomous-body-id]"), function (row) {
                            var matches = !abBodyId || row.getAttribute("data-autonomous-body-id") === abBodyId;
                            row.classList.toggle("hidden", !matches);
                        });
                    }
                    break;
                }
            }

            if (event.target.id === "NewRecord_CategoryType" && event.target.closest("#appendixIIIForm")) {
                loadSchemesForCategory(event.target.value, null);
                return;
            }

            // VI-B's own CategoryId->Scheme cascade branch lived here until 2026-09-08 - removed
            // as dead code once VI-B got its own redesigned drawer (appendix-vib-drawer.js), which
            // owns this exact dispatch itself via its own document-level listener (same reasoning
            // as the edit-populate dead-code removal earlier this same date - leaving it here would
            // now double-fire once this listener is widened from `container` to `document` below).

            if (event.target.id === "VANewRecord_AutonomousBodyId" && event.target.closest("#appendixVAForm")) {
                var vaForm = event.target.closest("#appendixVAForm");
                loadAppendixVAPreviousYearReference(vaForm.getAttribute("data-demand-id"), event.target.value);
                return;
            }

            if (event.target.id === "VBNewRecord_ObjectHeadId" && event.target.closest("#appendixVBForm")) {
                var vbForm = event.target.closest("#appendixVBForm");
                loadAppendixVBPreviousYearReference(vbForm.getAttribute("data-demand-id"), event.target.value);
                return;
            }

            if (event.target.id === "NewRecord_ReceiptTypeId" && event.target.closest("#appendixVIForm")) {
                var viForm = event.target.closest("#appendixVIForm");
                loadAppendixVIPreviousYearReference(viForm.getAttribute("data-demand-id"), event.target.value);
                return;
            }

            if (event.target.id === "NewRecord_AutonomousBodyId") {
                var vicForm = event.target.closest("#appendixVICForm");
                if (vicForm) {
                    loadAppendixVICPreviousYearReference(vicForm.getAttribute("data-demand-id"), event.target.value);
                    return;
                }

                var vieForm = event.target.closest("#appendixVIEForm");
                if (vieForm) {
                    loadAppendixVIEPreviousYearReference(vieForm.getAttribute("data-demand-id"), event.target.value);
                    return;
                }

                var vidForm = event.target.closest("#appendixVIDForm");
                if (vidForm) {
                    // Client requirement 2026-08-31: "in adding new record, if I change dropdown value
                    // of Name of GranteeBody/Autonomous Institution, next input text boxes should be
                    // blank to get new value." loadAppendixVIDPreviousYearReference already resets/
                    // re-fills AsOnMarch31/AsOnJune30 (its own field map), but ExpectedNextMarch31/
                    // ExpectedNextFY/Remarks are this cycle's own forward-looking figures - never
                    // looked up from prior years, so nothing was clearing them when a different
                    // Autonomous Body was picked, leaving the previous body's manually-typed values
                    // sitting on screen looking like they belonged to the new selection.
                    ["NewRecord_ExpectedNextMarch31", "NewRecord_ExpectedNextFY", "NewRecord_Remarks"].forEach(function (id) {
                        var el = document.getElementById(id);
                        if (el) { el.value = ""; }
                    });
                    loadAppendixVIDPreviousYearReference(vidForm.getAttribute("data-demand-id"), event.target.value);
                    return;
                }
            }

            if (event.target.id === "NewRecord_EntityName" && event.target.closest("#appendixIIIAForm")) {
                var iiiaForm = event.target.closest("#appendixIIIAForm");
                loadAppendixIIIABePreviousYear(iiiaForm.getAttribute("data-demand-id"), event.target.value);
                return;
            }

            if (event.target.id === "NewRecord_SchemeName" && event.target.closest("#appendixVIIAForm")) {
                var viiaForm = event.target.closest("#appendixVIIAForm");
                loadAppendixVIIAPreviousYearReference(viiaForm.getAttribute("data-demand-id"), event.target.value);
                return;
            }
        });

        // Client report 2026-08-31 ("appendix vii-a, reset not working"): typing a Scheme Name locks
        // the Actuals box read-only (loadAppendixVIIAPreviousYearReference above, once a prior-year
        // match is found) by setting `readOnly`/CSS classes directly via JS - a native form.reset()
        // only restores field VALUES, so after Reset the Actuals box stayed visibly locked/greyed
        // (stale "not editable here" styling) even though every value did clear, reading as "Reset
        // doesn't work". Un-locks it back to a normal editable box, same reset-field shape
        // loadAppendixVIIAPreviousYearReference itself already uses when the Scheme Name changes.
        container.addEventListener("reset", function (event) {
            var form = event.target;
            if (!(form instanceof HTMLFormElement) || form.id !== "appendixVIIAForm") {
                return;
            }
            var actuals = document.getElementById("NewRecord_Actuals");
            if (actuals) {
                actuals.readOnly = false;
                actuals.classList.remove("bg-slate-100", "dark:bg-slate-800", "cursor-not-allowed");
                actuals.title = "";
            }
        });

        // Widened from `container` to `document` (2026-09-08, same reasoning as above) - every
        // branch here checks a specific field id + `.closest("#form")` ancestor.
        document.addEventListener("change", function (event) {

            if (event.target.id === "NewRecord_MajorHeadId" && event.target.closest("#appendixPAForm")) {
                var paForm = event.target.closest("#appendixPAForm");
                loadAppendixPAPreviousYearReference(paForm.getAttribute("data-demand-id"), event.target.value);
                return;
            }

            if (event.target.id === "NewRecord_MajorHeadId" && event.target.closest("#appendixVIIBForm")) {
                loadAppendixVIIBBeActuals();
                filterAppendixVIIBGrid();
                return;
            }

            if (event.target.id === "NewRecord_TransactionType" && event.target.closest("#appendixVIIBForm")) {
                loadAppendixVIIBBeActuals();
                filterAppendixVIIBGrid();
                return;
            }

            // Appendix IV/IV-A/IV-B are the same Scheme-graded shape (identical entity fields, same
            // shared Scheme/SubScheme reference data) - one lookup table drives the Scheme/SubScheme
            // change handling for all three instead of triplicating the branch.
            var schemeGradedAppendixForms = {
                appendixIVForm: { label: "IV", url: urls.appendixIVPrevYearUrl, beUrl: urls.appendixIVBeUrl },
                appendixIVAForm: { label: "IV-A", url: urls.appendixIVAPrevYearUrl, beUrl: urls.appendixIVBeUrl },
                appendixIVBForm: { label: "IV-B", url: urls.appendixIVBPrevYearUrl, beUrl: urls.appendixIVBeUrl }
            };

            if (event.target.id === "NewRecord_SchemeId") {
                var appendixIIIForm = event.target.closest("#appendixIIIForm");
                if (appendixIIIForm) {
                    loadSubSchemesForScheme(event.target.value, null);
            
                    loadAppendixIIIBe(appendixIIIForm.getAttribute("data-demand-id"), event.target.value);
                    return;
                }

                // VI-B's own SchemeId->SubScheme+BE+PrevYear cascade branch lived here until
                // 2026-09-08 - removed as dead code, same reasoning as its CategoryType branch
                // above (superseded by appendix-vib-drawer.js's own document-level listener).

                var viibSchemeForm = event.target.closest("#appendixVIIBForm");
                if (viibSchemeForm) {
                    loadAppendixVIIBPreviousYearReference(viibSchemeForm.getAttribute("data-demand-id"), event.target.value);
                    loadAppendixVIIBMajorHeads(viibSchemeForm.getAttribute("data-demand-id"), event.target.value, null);
                    filterAppendixVIIBGrid();
                    return;
                }

                for (var formId in schemeGradedAppendixForms) {
                    var formEl = event.target.closest("#" + formId);
                    if (formEl) {
                        if (formId === "appendixIVForm") {
                            clearAppendixIVManualFields(formEl);
                        }                       
                        loadIVSubSchemesForScheme(event.target.value, null, formEl);
                        loadAppendixSchemeActualsReference(schemeGradedAppendixForms[formId].label, schemeGradedAppendixForms[formId].url,
                            formEl.getAttribute("data-demand-id"), event.target.value, null, formEl);
                        filterAppendixSchemeGridByScheme(formId, event.target.value);
                        loadAppendixBe(schemeGradedAppendixForms[formId].beUrl, formEl.getAttribute("data-demand-id"), event.target.value, null, formEl);
                        window.PreBudgetValidation.updateAppendixIVDuplicateGuard();
                        break;
                    }
                }
                return;
            }

            if (event.target.id === "NewRecord_SubSchemeId") {
                // VI-B's own SubSchemeId->PrevYear branch lived here until 2026-09-08 - removed as
                // dead code, same reasoning as its sibling branches above.

                for (var subFormId in schemeGradedAppendixForms) {
                    var subFormEl = event.target.closest("#" + subFormId);
                    if (subFormEl) {
                        if (subFormId === "appendixIVForm") {
                            clearAppendixIVManualFields(subFormEl);
                        }
                        // Scoped to subFormEl (2026-09-15 fix, client report "BE not loading") -
                        // was a global document.getElementById, which could resolve to a stale
                        // duplicate NewRecord_SchemeId left over in a different appendix's own
                        // (also NewRecord_SchemeId-named) form, silently keying the BE/Actuals
                        // lookup off the wrong Scheme entirely.
                        var schemeSelect = subFormEl.querySelector("#NewRecord_SchemeId");
                        loadAppendixSchemeActualsReference(schemeGradedAppendixForms[subFormId].label, schemeGradedAppendixForms[subFormId].url,
                            subFormEl.getAttribute("data-demand-id"), schemeSelect ? schemeSelect.value : null, event.target.value, subFormEl);
                        loadAppendixBe(schemeGradedAppendixForms[subFormId].beUrl, subFormEl.getAttribute("data-demand-id"),
                            schemeSelect ? schemeSelect.value : null, event.target.value, subFormEl);
                        // "Grid records should filter on subscheme too" (client testing feedback, 2026-08-24).
                        filterAppendixSchemeGridByScheme(subFormId, schemeSelect ? schemeSelect.value : null, event.target.value);
                        window.PreBudgetValidation.updateAppendixIVDuplicateGuard();
                        break;
                    }
                }
            }
        });
    }

    // ----------------------------------------------------------------------------------------
    // Replay-on-fragment-load: whatever was previously inline in pre-budget-meeting.js's own
    // applyFragment(), now consolidated into one call the orchestrator makes after every fragment
    // swap (Load/Save/Freeze/Delete all re-render the same fragment from scratch).
    // ----------------------------------------------------------------------------------------

    function onFragmentApplied() {
        autoSelectSingleOptionsIn(container);

        var appendixIAForm = document.getElementById("appendixIAForm");
        if (appendixIAForm) {
            loadAppendixIAPreviousYearReference(appendixIAForm);
        }

        var appendixIIForm = document.getElementById("appendixIIForm");
        if (appendixIIForm) {
            loadAppendixIIPreviousYearReference(appendixIIForm);
        }

        recalcAppendixIVASavingExcess();
        recalcAppendixIVCalculations();
        recalcAppendixVIIBIncreasedBE();
        recalcAppendixIITotals();

        // "Total Revenue from User Charges in: textbox should be blank and editable" (client
        // testing feedback, 2026-08-24) - this used to auto-pull last year's figures via
        // loadAppendixVIAPreviousYearReference and lock the fields read-only once found, but
        // Total Revenue is this cycle's own fresh figure, not a carried-forward historical fact
        // (unlike Actuals/BE elsewhere) - removed so the field always starts blank and editable.

        // Bug fix 2026-08-24 (client testing feedback: "Sub-Scheme should not clear/lock after
        // Submit"): the Scheme dropdown is server-rendered (asp-for) so it keeps its selection
        // across the Save round trip on its own, but the SubScheme dropdown is AJAX-populated and
        // came back empty/disabled on every fragment reload - same bug class Appendix III already
        // had fixed below. Replay the SubScheme cascade + BE auto-load + grid filter (now also by
        // SubScheme, "Grid records should filter on subscheme too") against the just-submitted
        // selection, carried forward via data-selected-* (see PreBudgetMeetingController.AppendixIV*.cs).
        ["appendixIVForm", "appendixIVAForm", "appendixIVBForm"].forEach(function (formId) {
            var form = document.getElementById(formId);
            if (!form) {
                return;
            }
            // Scoped to `form` (2026-09-15 fix, client report "BE not loading in Appendix
            // IV/IV-A") - was a global document.getElementById("NewRecord_SchemeId"), which on
            // every single Save/Freeze/Delete fragment reload could resolve to a stale duplicate
            // left over in a different one of these three forms (they all share this same id by
            // design) instead of the one actually on screen, silently keying the BE/Actuals
            // re-fetch off the wrong Scheme - or a blank one - after every save.
            var schemeSelectEl = form.querySelector("#NewRecord_SchemeId");
            if (!schemeSelectEl) {
                return;
            }
            var selectedSchemeId = form.getAttribute("data-selected-scheme-id") || schemeSelectEl.value || "";
            var selectedSubSchemeId = form.getAttribute("data-selected-subscheme-id") || "";
            if (selectedSchemeId) {
                loadIVSubSchemesForScheme(selectedSchemeId, selectedSubSchemeId, form);
                loadAppendixBe(urls.appendixIVBeUrl, form.getAttribute("data-demand-id"), selectedSchemeId, selectedSubSchemeId, form);
            }
            filterAppendixSchemeGridByScheme(formId, selectedSchemeId, selectedSubSchemeId);
        });

        // Client report 2026-08-31 (Appendix VII-B): "if correctly show dialog box but if freeze
        // major head dropdown. If user want to select other Major Head to do data entry, he
        // couldn't" - same bug class as IV/III above. Major Head's <select> is AJAX-populated
        // (cascading from Scheme, see loadAppendixVIIBMajorHeads) and has no @foreach in its
        // markup, so a fragment reload (e.g. after a duplicate-entry rejection) always came back
        // to its static disabled/blank state with nothing to re-trigger the cascade - the user was
        // stuck unable to pick any Major Head at all, not just unable to change it. Replays the
        // cascade against the just-submitted Scheme (carried forward via data-selected-scheme-id,
        // see PreBudgetMeetingController.AppendixVIIB.cs), which re-enables the dropdown either way.
        var appendixVIIBFormEl = document.getElementById("appendixVIIBForm");
        if (appendixVIIBFormEl) {
            var viibSelectedSchemeId = appendixVIIBFormEl.getAttribute("data-selected-scheme-id") || "";
            if (viibSelectedSchemeId) {
                loadAppendixVIIBMajorHeads(appendixVIIBFormEl.getAttribute("data-demand-id"), viibSelectedSchemeId, appendixVIIBFormEl.getAttribute("data-selected-major-head-id") || "", null);
            }
        }

        // Bug fix 2026-08-06 (tester report, pre-drawer layout): Save re-rendered the fragment from
        // scratch, so the Scheme/Sub-Scheme dropdowns (AJAX-cascaded, not server-rendered <option>s)
        // came back empty/disabled - replays the cascade from the data-selected-* attributes
        // SaveAppendixIII carries the just-submitted Scheme/Sub-Scheme forward into, so the drawer's
        // own fields are ready to go if the user reopens Add/Edit.
        //
        // Bug fix 2026-09-15 (client report: "after modification, grid only shows the edited
        // record, it should refresh to show all data"): this block used to ALSO call
        // filterAppendixSchemeGridByScheme("appendixIIIForm", selectedSchemeId) here - a leftover
        // from the pre-2026-09-08 single-page layout where the entry form's own Scheme dropdown
        // doubled as the grid's only filter. Appendix III's drawer redesign gave the grid its own,
        // independent Filter & Context section (Balance Type/Scheme/Sub-Scheme/Search, reset to
        // "All" on every fragment load by appendix-iii-drawer.js's own applyGridFilters) - this
        // leftover call ran asynchronously (inside the AJAX cascade's callback) AFTER that reset
        // had already shown every row, and re-hid every row except the just-saved record's own
        // Scheme, with no visible connection to the now-independent grid filter dropdowns (which
        // still correctly said "All Schemes"). Removed - the grid's own filter section is the only
        // thing that should ever hide grid rows now.
        var appendixIIIForm = document.getElementById("appendixIIIForm");
        if (appendixIIIForm) {
            var iiiCategorySelect = document.getElementById("NewRecord_CategoryType");
            var selectedSchemeId = appendixIIIForm.getAttribute("data-selected-scheme-id") || "";
            var selectedSubSchemeId = appendixIIIForm.getAttribute("data-selected-subscheme-id") || "";
            if (iiiCategorySelect && iiiCategorySelect.value) {
                loadSchemesForCategory(iiiCategorySelect.value, selectedSchemeId, function () {
                    loadSubSchemesForScheme(selectedSchemeId, selectedSubSchemeId);
                    loadAppendixIIIBe(appendixIIIForm.getAttribute("data-demand-id"), selectedSchemeId);
                });
            }
        }
    }

    function init(ctx) {
        container = ctx.container;
        urls = ctx.urls;
        fetchJson = ctx.fetchJson;
        demandSelect = ctx.demandSelect;
        attachListeners();
    }

    // ----------------------------------------------------------------------------------------
    // Allocation page (PreBudgetMeeting/Allocation/REMeetingAllocation.cshtml) - extracted
    // 2026-09-03 alongside the appendix split above, same validation/control-binding boundary.
    // This is a plain server-rendered page (full POST-then-render, no AJAX fragment/`container`),
    // so these attach directly to `document` rather than needing init()'s appendix-SPA context -
    // safe to call unconditionally from any page, since every selector below already no-ops if
    // its element isn't present (see re-meeting-allocation.js's own bootstrap call).
    // ----------------------------------------------------------------------------------------

    function initAllocationControls() {
        var selectAll = document.getElementById("selectAllDemands");
        var demandCheckboxes = document.querySelectorAll(".demand-checkbox");
        if (selectAll) {
            selectAll.addEventListener("change", function () {
                demandCheckboxes.forEach(function (cb) { cb.checked = selectAll.checked; });
            });

            // Client requirement 2026-08-28: "On Select All checkbox click, it will select all
            // checkboxes. On clicking any individual checkbox after selecting all, only that
            // checkbox with Select all checkbox will be deselected." - unchecking one individual
            // checkbox drops "Select All" back to unchecked too (the set is no longer "all"),
            // without touching any of the other individual checkboxes; re-checking every
            // individual one manually restores "Select All" to checked.
            demandCheckboxes.forEach(function (cb) {
                cb.addEventListener("change", function () {
                    if (!cb.checked) {
                        selectAll.checked = false;
                    } else {
                        selectAll.checked = Array.prototype.every.call(demandCheckboxes, function (c) { return c.checked; });
                    }
                });
            });
        }

        // Inline row edit (client requirement 2026-08-28: Edit icon per grid row) - no AJAX on this
        // page, so this just toggles which of the two rows (display/edit) already rendered
        // server-side for that Demand is visible; the edit row's own form still does a normal POST to
        // UpdateAllocation.
        document.addEventListener("click", function (event) {
            var editButton = event.target.closest('[data-action="edit-allocation-row"]');
            if (editButton) {
                var id = editButton.getAttribute("data-row-id");
                var displayRow = document.getElementById("displayRow-" + id);
                var editRow = document.getElementById("editRow-" + id);
                if (displayRow) displayRow.classList.add("hidden");
                if (editRow) editRow.classList.remove("hidden");
                return;
            }

            var cancelButton = event.target.closest('[data-action="cancel-allocation-edit"]');
            if (cancelButton) {
                var cancelId = cancelButton.getAttribute("data-row-id");
                var cancelDisplayRow = document.getElementById("displayRow-" + cancelId);
                var cancelEditRow = document.getElementById("editRow-" + cancelId);
                if (cancelEditRow) cancelEditRow.classList.add("hidden");
                if (cancelDisplayRow) cancelDisplayRow.classList.remove("hidden");
            }
        });
    }

    // ----------------------------------------------------------------------------------------
    // Cancel-confirmation dirty check (client requirement 2026-09-17): "Are you sure you want to
    // cancel?" should only appear if the user actually changed something. Add mode: no confirm if
    // every editable field is still at its blank/default state. Edit mode: no confirm if nothing
    // was changed from the record as it was loaded. Deliberately skips disabled/readOnly fields -
    // those are auto-derived (BE auto-load, locked identity fields on Edit) and can legitimately
    // get populated/re-populated by the app itself (not the user) at any point, including after
    // the snapshot below is taken - counting them would produce false "dirty" results.
    function snapshotEditableFormValues(form) {
        if (!form) {
            return "";
        }
        var parts = [];
        Array.prototype.forEach.call(form.querySelectorAll("input, select, textarea"), function (el) {
            if (el.type === "hidden" || el.type === "submit" || el.type === "button" || el.type === "reset") {
                return;
            }
            if (el.disabled || el.readOnly) {
                return;
            }
            var value = (el.type === "checkbox" || el.type === "radio") ? (el.checked ? "1" : "0") : el.value;
            parts.push((el.id || el.name || "") + "=" + value);
        });
        return parts.join("|");
    }

    // Call once the drawer's fields are fully set (blank for Add, populated for Edit) - stores the
    // "starting point" snapshot on the form itself so isFormDirtySinceOpen can compare against it
    // later regardless of which appendix's drawer file is asking.
    function captureCancelSnapshot(form) {
        if (form) {
            form.setAttribute("data-cancel-snapshot", snapshotEditableFormValues(form));
        }
    }

    // Returns true (safe default: show the confirm dialog) if no snapshot was ever captured for
    // this form, or if the current editable-field values differ from the captured snapshot.
    function isFormDirtySinceOpen(form) {
        if (!form) {
            return true;
        }
        var snapshot = form.getAttribute("data-cancel-snapshot");
        if (snapshot === null) {
            return true;
        }
        return snapshotEditableFormValues(form) !== snapshot;
    }

    return {
        init: init,
        onFragmentApplied: onFragmentApplied,
        recalcForNumberInput: recalcForNumberInput,
        autoSelectIfSingleOption: autoSelectIfSingleOption,
        autoSelectSingleOptionsIn: autoSelectSingleOptionsIn,
        initAllocationControls: initAllocationControls,
        // Exposed for appendix-via-drawer.js (2026-09-03): the redesigned Appendix VI-A drawer
        // populates its own fields directly (see that file's own comment for why - the reparented
        // action-menu breaks this file's container-scoped click delegation for that page), but
        // still wants the exact same Submit->Modify/Cancel-button treatment every other appendix
        // gets on Edit, rather than a second, separately-maintained copy of it.
        enterAppendixEditMode: enterAppendixEditMode,
        exitAppendixEditMode: exitAppendixEditMode,
        // Exposed for appendix-vib-drawer.js/appendix-vic-drawer.js (2026-09-08): same reasoning
        // as above, one level deeper - reparenting the WHOLE drawer (not just an action-menu) to
        // document.body for VI-B/VI-C means every field living inside it (Category/Scheme/
        // SubScheme/Autonomous Body selects included) no longer bubbles its "change" through
        // #partialViewContainer, so this file's own container-scoped cascade listeners
        // (loadVIBSchemesForCategory etc.) never fire for those reparented drawers. Each new
        // drawer file re-wires its own document-level "change" listener for its cascade selects,
        // calling straight into this same underlying fetch logic instead of duplicating it.
        loadVIBSchemesForCategory: loadVIBSchemesForCategory,
        loadVIBSubSchemesForScheme: loadVIBSubSchemesForScheme,
        loadAppendixVIBBe: loadAppendixVIBBe,
        loadAppendixVIBPreviousYearReference: loadAppendixVIBPreviousYearReference,
        loadAppendixVICPreviousYearReference: loadAppendixVICPreviousYearReference,
        // Exposed for appendix-vid-drawer.js/appendix-vie-drawer.js (2026-09-08), same reasoning
        // as the VI-B/VI-C exports above.
        loadAppendixVIDPreviousYearReference: loadAppendixVIDPreviousYearReference,
        // Exposed for every appendix-*-drawer.js's Cancel handler (2026-09-17): shared "did the
        // user actually change anything" check backing the Cancel-confirmation dialog suppression.
        captureCancelSnapshot: captureCancelSnapshot,
        isFormDirtySinceOpen: isFormDirtySinceOpen,
        loadAppendixVIEPreviousYearReference: loadAppendixVIEPreviousYearReference,
        // VI-D's own Edit mode locks the Autonomous Body select (it's part of the record's
        // identity, per the 2026-08-31 requirement quoted at that call site) via a disabled-select
        // + hidden-mirror-input pair - needed here too since the new drawer populates/resets its
        // own fields directly rather than through this file's container-scoped dispatch.
        lockSelectForEdit: lockSelectForEdit,
        unlockSelectsAfterEdit: unlockSelectsAfterEdit
    };
})();
