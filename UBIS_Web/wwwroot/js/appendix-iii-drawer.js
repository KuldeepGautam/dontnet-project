// "Approved design for all appendixes" (extended 2026-09-08 to Appendix III) - drawer/grid
// interaction layer for the redesigned Appendix III page. Mirrors appendix-ia-drawer.js's own
// structure (see that file's header comment, and appendix-via-drawer.js's for the original
// reasoning behind each piece: drawer slide-in, columns picker, row action menu, delete dialog,
// toast, search/status filter + pagination, Export dropdown).
//
// Smaller than VI-A..E's own drawer files by design (see ubis2_appendix_redesign_reuse_shared_files
// memory - "most of the design must come from reusing shared css/js, not new bespoke code"): this
// file owns ONLY the new drawer/grid chrome. The Balance Type -> Scheme -> Sub-Scheme cascade, the
// BE auto-load, the Edit-populate field mapping, and the "freeze Category/Scheme/Sub-Scheme during
// Edit" rule are NOT duplicated here - they're still handled by PreBudget-control-binding.js's
// existing loadSchemesForCategory/loadSubSchemesForScheme/loadAppendixIIIBe/
// populateAppendixIIIEntryForm/lockSelectForEdit, plus the appendixIIIForm reset handler - all of
// them already document-safe (getElementById-based) or already widened 2026-09-08 so they keep
// working once this drawer reparents to document.body. This file only needs to open/close the
// drawer shell itself and reset it to Add mode.
//
// Unlike I-A/II, Appendix III has NO single-record rule - Add stays available whenever the
// appendix isn't locked (see AppendixIII.cshtml's header, gated only on !locked), since III's
// duplicate-check keys on the Balance Type/Scheme/Sub-Scheme combination, not one-per-Demand (see
// ubis2_appendix_redesign_business_rules_checklist memory).
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("iiiToast");
        if (!toast) {
            return;
        }
        var variants = {
            success: { title: "Success", icon: "check-circle-2" },
            update: { title: "Updated", icon: "refresh-cw" },
            delete: { title: "Deleted", icon: "trash-2" },
            warning: { title: "Attention required", icon: "triangle-alert" },
            error: { title: "Unable to continue", icon: "circle-alert" }
        };
        var variant = variants[type] || variants.success;
        ["success", "update", "delete", "warning", "error"].forEach(function (name) {
            toast.classList.remove("ubis-toast-" + name);
        });
        toast.classList.add("ubis-toast-" + type);
        $("iiiToastTitle").textContent = variant.title;
        $("iiiToastIcon").setAttribute("data-lucide", variant.icon);
        $("iiiToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("iiiToast");
        if (!toast) {
            return;
        }
        toast.classList.remove("ubis-toast-show");
        toast.classList.add("ubis-toast-hide");
    }

    // ----------------------------------------------------------------------------------------
    // Export dropdown
    // ----------------------------------------------------------------------------------------
    function toggleExportMenu(event) {
        event.stopPropagation();
        var menu = $("iiiExportMenu");
        var button = $("iiiExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("iiiExportMenu");
        var button = $("iiiExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function getCurrentColumnDefinitions() {
        var table = $("chargesTable");
        if (!table) {
            return [];
        }
        var definitions = [];
        var byColumn = {};
        table.querySelectorAll("thead th[data-column]").forEach(function (header) {
            var column = header.getAttribute("data-column");
            if (!column || header.classList.contains("column-hidden-by-balance-type")) {
                return;
            }
            var label = header.innerHTML
                .replace(/<br\s*\/?>(\s*)/gi, " ")
                .replace(/<[^>]*>/g, "")
                .replace(/\s+/g, " ")
                .trim();
            if (byColumn[column]) {
                byColumn[column].label += " / " + label;
                return;
            }
            byColumn[column] = { key: column, label: label };
            definitions.push(byColumn[column]);
        });
        return definitions;
    }

    function rebuildColumnsMenu(preserveVisibility) {
        var options = $("iiiColumnsOptions");
        if (!options) {
            return;
        }
        var previousState = {};
        if (preserveVisibility) {
            options.querySelectorAll("[data-column-toggle]").forEach(function (checkbox) {
                previousState[checkbox.getAttribute("data-column-toggle")] = checkbox.checked;
            });
        }
        options.innerHTML = "";
        getCurrentColumnDefinitions().forEach(function (definition) {
            var label = document.createElement("label");
            label.className = "flex cursor-pointer items-center gap-3 rounded-lg px-2.5 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50";
            var checkbox = document.createElement("input");
            checkbox.type = "checkbox";
            checkbox.checked = preserveVisibility && Object.prototype.hasOwnProperty.call(previousState, definition.key)
                ? previousState[definition.key]
                : true;
            checkbox.setAttribute("data-column-toggle", definition.key);
            checkbox.className = "h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-200";
            label.appendChild(checkbox);
            label.appendChild(document.createTextNode(definition.label));
            options.appendChild(label);
        });
    }

    function resetCurrentColumnVisibility() {
        document.querySelectorAll("#chargesTable [data-column]").forEach(function (cell) {
            cell.classList.remove("column-hidden");
            cell.classList.remove("column-hidden-by-balance-type");
        });
    }

    function setBalanceColumnState(column, hidden, resetVisibility) {
        document.querySelectorAll('#chargesTable [data-column="' + column + '"]').forEach(function (cell) {
            cell.classList.toggle("column-hidden-by-balance-type", hidden);
            if (resetVisibility && hidden) {
                cell.classList.add("column-hidden");
            }
        });
    }

    // 2026-09-15 (client instruction: "export functionality should depend of columns of grid
    // being export, not hard-code... any update on grid columns should not change export
    // functionality") - was its own fetch to a hand-maintained ExportAppendixIII MVC action with a
    // separate C# column list (which had already drifted out of sync with the grid once - see the
    // earlier "export should match same headers of grid" fix); now delegates to the shared
    // wwwroot/js/grid-export.js, which scrapes the live #chargesTable DOM directly.
    function exportAppendixIII(format, triggerButton) {
        var form = $("appendixIIIForm");
        var demandId = form ? form.getAttribute("data-demand-id") : null;
        closeExportMenu();

        var originalHtml = triggerButton ? triggerButton.innerHTML : null;
        if (triggerButton) {
            triggerButton.innerHTML = '<span class="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-slate-300 border-t-ubis-teal"></span><span>Generating...</span>';
            triggerButton.disabled = true;
        }

        function reset() {
            if (triggerButton && originalHtml !== null) {
                triggerButton.innerHTML = originalHtml;
                triggerButton.disabled = false;
            }
        }

        window.UbisGridExport.exportTable({
            tableSelector: "#chargesTable",
            format: format,
            title: "Appendix III - CNA/SNA Balances of Schemes",
            subtitle: "Demand ID: " + (demandId || ""),
            fileName: "AppendixIII-" + new Date().toISOString().replace(/[:.]/g, "-"),
            onError: function (message) {
                showAppendixToast(message, "error");
                reset();
            }
        });
        setTimeout(function () {
            reset();
            showAppendixToast("Report has been downloaded successfully.", "success");
        }, 800);
    }

    // ----------------------------------------------------------------------------------------
    // Columns menu
    // ----------------------------------------------------------------------------------------
    function toggleColumnsMenu(event) {
        event.stopPropagation();
        var menu = $("iiiColumnsMenu");
        var button = $("iiiColumnsButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeExportMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
        if (opening) {
            var first = menu.querySelector("input");
            if (first) {
                first.focus();
            }
        }
    }

    function closeColumnsMenu() {
        var menu = $("iiiColumnsMenu");
        var button = $("iiiColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("iiiColumnsMenu");
        if (!menu) {
            return;
        }
        Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (checkbox) {
            var column = checkbox.getAttribute("data-column-toggle");
            var show = checkbox.checked;
            document.querySelectorAll('#chargesTable [data-column="' + column + '"]').forEach(function (cell) {
                cell.classList.toggle("column-hidden", !show || cell.classList.contains("column-hidden-by-balance-type"));
            });
        });
    }

    // When the Balance Type filter selects an exemption category, hide numeric/financial
    // columns (BE, Opening, Releases, Closing, NotTransferred, LastRelease). This is a
    // minimal, focused change so the grid shows only identity + reason columns for
    // ExemptedFromCna/ExemptedFromSna filters.
    function adjustColumnsForBalanceType(balanceType, resetVisibility) {
        if (resetVisibility) {
            resetCurrentColumnVisibility();
        }
        // Exemption categories hide all numeric financial columns and show only the Reason
        // for Exemption column. Additionally the product requirement: when the filter is the
        // Central Sector Schemes/Projects balance type, hide the "Not Transferred to SNA"
        // column and the "Reason for Exemption" column as they are not relevant.
        var exempted = balanceType === "ExemptedFromCna" || balanceType === "ExemptedFromSna";
        var isCentral = balanceType === "CentralSectorScheme";
        var isSna = balanceType === "CentrallySponsoredScheme";

        // Columns hidden for exemption categories
        var financialCols = ["be", "opening", "releases", "closing", "lastrelease-date", "lastrelease-amount"];
        financialCols.forEach(function (col) {
            setBalanceColumnState(col, exempted, resetVisibility);
        });

        // The Not-Transferred column should be hidden for exemptions and also when the
        // Balance Type filter is CentralSectorScheme per requirement.
        setBalanceColumnState("nottransferred", exempted || isCentral, resetVisibility);

        // Reason for Exemption column is visible only for exemption categories.
        setBalanceColumnState("reason", !exempted, resetVisibility);

        // Update the opening/closing column labels to show CNA or SNA depending on the
        // selected Balance Type (Centrally Sponsored Schemes use SNA labels).
        try {
            var labelPrefix = isSna ? "SNA" : "CNA";
            document.querySelectorAll('#chargesTable th[data-column="opening"], #chargesTable th[data-column="closing"]').forEach(function (th) {
                if (th && th.innerHTML) {
                    // Replace any existing CNA/SNA token with the correct prefix.
                    th.innerHTML = th.innerHTML.replace(/\bCNA\b|\bSNA\b/g, labelPrefix);
                }
            });
        } catch (e) { /* ignore DOM issues */ }

        rebuildColumnsMenu(!resetVisibility);
        applyColumnVisibility();
    }

    function resetColumns() {
        var menu = $("iiiColumnsMenu");
        if (!menu) {
            return;
        }
        Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (checkbox) {
            checkbox.checked = true;
        });
        applyColumnVisibility();
        showAppendixToast("All data-grid columns are visible.", "update");
    }

    // ----------------------------------------------------------------------------------------
    // Row "..." action menu
    // ----------------------------------------------------------------------------------------
    function positionActionMenu(menu, button) {
        if (!menu || !button) {
            return;
        }
        if (menu.parentElement !== document.body) {
            document.body.appendChild(menu);
        }
        menu.setAttribute("data-reparented-fragment", "true");
        menu.classList.remove("hidden");
        menu.style.position = "fixed";
        menu.style.zIndex = "9999";
        menu.style.visibility = "hidden";
        var br = button.getBoundingClientRect();
        var mr = menu.getBoundingClientRect();
        var gap = 6;
        var pad = 8;
        var below = window.innerHeight - br.bottom - pad;
        var above = br.top - pad;
        var up = below < mr.height + gap && above > below;
        var top = up ? br.top - mr.height - gap : br.bottom + gap;
        top = Math.max(pad, Math.min(top, window.innerHeight - mr.height - pad));
        var left = br.right - mr.width;
        left = Math.max(pad, Math.min(left, window.innerWidth - mr.width - pad));
        menu.style.left = left + "px";
        menu.style.top = top + "px";
        menu.style.visibility = "visible";
    }

    function toggleActionMenu(id, button, event) {
        event.stopPropagation();
        var menu = $("iii-menu-" + id);
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        document.querySelectorAll(".action-menu").forEach(function (m) {
            if (m !== menu) {
                m.classList.add("hidden");
                m.style.visibility = "";
            }
        });
        if (!opening) {
            menu.classList.add("hidden");
            menu.style.visibility = "";
            return;
        }
        positionActionMenu(menu, button);
    }

    function repositionOpenActionMenu() {
        var menu = document.querySelector(".action-menu:not(.hidden)");
        if (!menu || !menu.id || menu.id.indexOf("iii-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("iii-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "iiiGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Balance Type -> field-set switch (client screenshots 2026-09-09): Central Sector Schemes/
    // Projects and Centrally Sponsored Schemes both use the numeric "Financial Details"/"Last
    // Release" sections (CNA vs SNA labels + an extra "Not Transferred to SNA" field for the
    // Centrally Sponsored case); "Schemes exempted from CNA"/"Schemes exempted from SNA" show only
    // a Reason For Exemption field, no numeric figures at all. The Balance Type -> Scheme cascade
    // itself is unchanged (still PreBudget-control-binding.js's loadSchemesForCategory) - this
    // function only toggles which of this drawer's own field groups are visible/required.
    // ----------------------------------------------------------------------------------------
    var FINANCIAL_REQUIRED_FIELD_IDS = [
        "NewRecord_BE", "NewRecord_BalanceAsOnAprilOpening", "NewRecord_ReleasesDuringFY",
        "NewRecord_BalanceAsOnSeptClosing", "NewRecord_DateOfLastRelease", "NewRecord_AmountOfLastRelease"
    ];

    function applyAppendixIIIBalanceTypeUI(categoryType) {
        var isExempted = categoryType === "ExemptedFromCna" || categoryType === "ExemptedFromSna";
        var isSna = categoryType === "CentrallySponsoredScheme";

        document.querySelectorAll('#appendixIIIForm [data-balance-section="financial"]').forEach(function (section) {
            section.classList.toggle("hidden", isExempted);
        });
        document.querySelectorAll('#appendixIIIForm [data-balance-section="exemption"]').forEach(function (section) {
            section.classList.toggle("hidden", !isExempted);
        });

        FINANCIAL_REQUIRED_FIELD_IDS.forEach(function (id) {
            var field = $(id);
            if (field) {
                field.required = !isExempted;
            }
        });
        var reasonField = $("NewRecord_ReasonForExemption");
        if (reasonField) {
            reasonField.required = isExempted;
        }

        document.querySelectorAll('#appendixIIIForm [data-balance-field="not-transferred"]').forEach(function (wrap) {
            wrap.classList.toggle("hidden", !isSna);
        });
        var notTransferredField = $("NewRecord_NotTransferredToSnaAsOnSept");
        if (notTransferredField) {
            notTransferredField.required = isSna && !isExempted;
        }

        var balanceLabel = isSna ? "SNA" : "CNA";
        var openingLabel = $("iiiBalanceOpeningLabel");
        var closingLabel = $("iiiBalanceClosingLabel");
        if (openingLabel) {
            openingLabel.textContent = balanceLabel;
        }
        if (closingLabel) {
            closingLabel.textContent = balanceLabel;
        }

        var exemptionCategoryDisplay = $("iiiExemptionCategoryDisplay");
        if (exemptionCategoryDisplay) {
            exemptionCategoryDisplay.value = categoryType === "ExemptedFromCna" ? "CNA" : (categoryType === "ExemptedFromSna" ? "SNA" : "");
        }
    }

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit) - shell only. The Balance Type -> Scheme -> Sub-Scheme cascade, field
    // population on Edit, and the locked-select-during-edit behavior are all handled by the
    // existing PreBudget-control-binding.js logic (see file header).
    // ----------------------------------------------------------------------------------------
    function openAppendixIIIDrawer(mode) {
        var drawer = $("iiiDrawer");
        var backdrop = $("iiiDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixIIIForm");
        // Save the current grid filters when opening Add or Edit so they can be
        // restored after the fragment reloads. This prevents a successful save
        // from resetting the Filter & Context section to its defaults.
        try {
            if (mode === "add" || mode === "edit") {
                var bt = document.getElementById("iiiBalanceTypeFilter");
                var sch = document.getElementById("iiiSchemeFilter");
                var sub = document.getElementById("iiiSubSchemeFilter");
                window._iiiSavedFilters = {
                    balanceType: bt ? bt.value : null,
                    scheme: sch ? sch.value : null,
                    subScheme: sub ? sub.value : null
                };
            }
        } catch (e) { /* ignore */ }

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
        }

        // Field population for "edit" (PreBudget-control-binding.js's own click listener) runs
        // before this one - see file header - so NewRecord_CategoryType already holds the row's
        // value here; for "add" it's back to blank after the reset above.
        var categorySelect = $("NewRecord_CategoryType");
        applyAppendixIIIBalanceTypeUI(categorySelect ? categorySelect.value : "");

        var titleEl = $("iiiDrawerTitle");
        var subtitleEl = $("iiiDrawerSubtitle");
        if (titleEl) {
            titleEl.textContent = mode === "edit" ? "Edit Record" : "Add Record";
        }
        if (subtitleEl) {
            subtitleEl.textContent = mode === "edit"
                ? "Update the selected record"
                : "Create a new record";
        }

        drawer.classList.remove("ubis-drawer-closed");
        drawer.classList.add("ubis-drawer-open");
        drawer.setAttribute("aria-hidden", "false");
        backdrop.classList.remove("ubis-backdrop-hide");
        backdrop.classList.add("ubis-backdrop-show");
        document.body.classList.add("ubis-drawer-page-locked");

        // Client requirement 2026-09-17: baseline snapshot for the Cancel-confirmation dirty
        // check - captured once the form is fully at its Add-blank/Edit-populated starting point.
        if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.captureCancelSnapshot === "function") {
            window.PreBudgetControlBinding.captureCancelSnapshot(form);
        }

        setTimeout(function () {
            var firstField = $("NewRecord_CategoryType");
            if (firstField && !firstField.readOnly && !firstField.disabled) {
                firstField.focus();
            }
        }, 320);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    // Same fix as every other redesigned appendix's own drawer reparenting - see
    // appendix-via-drawer.js's comment for the full reasoning (drawer painted behind the app
    // shell's masthead otherwise).
    function reparentIiiDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="iiiDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="iiiDrawerBackdrop"]');
        var drawer = null;
        allDrawers.forEach(function (node) {
            if (node.parentElement !== document.body) { drawer = node; }
        });
        var backdrop = null;
        allBackdrops.forEach(function (node) {
            if (node.parentElement !== document.body) { backdrop = node; }
        });

        if (!drawer) {
            allDrawers.forEach(function (node) { node.remove(); });
            allBackdrops.forEach(function (node) { node.remove(); });
            return;
        }

        allDrawers.forEach(function (node) { if (node !== drawer) { node.remove(); } });
        allBackdrops.forEach(function (node) { if (node !== backdrop) { node.remove(); } });

        if (drawer) {
            drawer.classList.add("ubis-modern-appendix");
            drawer.setAttribute("data-reparented-fragment", "true");
            if (drawer.parentElement !== document.body) {
                document.body.appendChild(drawer);
            }
        }
        if (backdrop && backdrop.parentElement !== document.body) {
            document.body.insertBefore(backdrop, drawer || null);
        }
    }

    function closeAppendixIIIDrawer() {
        var drawer = $("iiiDrawer");
        var backdrop = $("iiiDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        drawer.classList.remove("ubis-drawer-open");
        drawer.classList.add("ubis-drawer-closed");
        drawer.setAttribute("aria-hidden", "true");
        backdrop.classList.remove("ubis-backdrop-show");
        backdrop.classList.add("ubis-backdrop-hide");
        document.body.classList.remove("ubis-drawer-page-locked");
    }

    // ----------------------------------------------------------------------------------------
    // Delete confirmation dialog
    // ----------------------------------------------------------------------------------------
    var pendingDeleteForm = null;

    function openDeleteDialog(form, label) {
        var dialog = $("iiiDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var balanceTypeFilter = $("iiiBalanceTypeFilter");
        var schemeFilter = $("iiiSchemeFilter");
        var subSchemeFilter = $("iiiSubSchemeFilter");
        window._iiiSavedFilters = {
            balanceType: balanceTypeFilter ? balanceTypeFilter.value : null,
            scheme: schemeFilter ? schemeFilter.value : null,
            subScheme: subSchemeFilter ? subSchemeFilter.value : null
        };
        var nameEl = $("iiiDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("iiiCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("iiiDeleteDialog");
        if (!dialog) {
            return;
        }
        dialog.classList.remove("ubis-dialog-show");
        dialog.classList.add("ubis-dialog-hide");
        dialog.setAttribute("aria-hidden", "true");
        pendingDeleteForm = null;
    }

    function confirmDelete() {
        if (!pendingDeleteForm) {
            return;
        }
        var form = pendingDeleteForm;
        closeDeleteDialog();
        if (form.requestSubmit) {
            form.requestSubmit();
        } else {
            form.submit();
        }
    }

    // ----------------------------------------------------------------------------------------
    // Search / status filter + pagination over the already server-rendered rows.
    // ----------------------------------------------------------------------------------------
    function currentGridRows() {
        var body = $("gridBody");
        return body ? Array.prototype.slice.call(body.querySelectorAll("tr[data-row]")) : [];
    }

    function syncAppendixIIIGridToolbarState(balanceType) {
        var toolbar = $("iiiGridToolbar");
        var enabled = !!balanceType && balanceType !== "All";

        if (toolbar) {
            toolbar.classList.toggle("hidden", !enabled);
        }
    }

    var iiiCurrentPage = 1;
    var iiiPageSize = 10;

    // ----------------------------------------------------------------------------------------
    // Filter section's own Scheme/Sub-Scheme cascade (client instruction 2026-09-21: "implement
    // same logic of balance type, scheme name and sub scheme name in appendix III filter" - i.e.
    // the same grid-driven approach just built for Appendix IV/IV-A/IV-B: Scheme/Sub-Scheme
    // dropdown *options* are recomputed from what's actually present in the grid (respecting the
    // upstream Balance Type/Scheme selection and the current Search/Status filters), entirely
    // client-side, no AJAX round trip. Superseded the earlier GetAppendixIIISchemes/
    // GetAppendixIIISubSchemes AJAX-backed cascade (this filter no longer needs to see schemes
    // that have no row in the currently-loaded grid at all). The Add/Edit drawer's own Scheme/
    // Sub-Scheme cascade in PreBudget-control-binding.js is unrelated and untouched.
    // ----------------------------------------------------------------------------------------
    function rowMatchesForOptions(row, upstream) {
        var searchEl = $("iiiSearchInput");
        var statusEl = $("iiiStatusFilter");
        var search = (searchEl ? searchEl.value : "").trim().toLowerCase();
        var status = statusEl ? statusEl.value : "All";
        var haystack = (row.getAttribute("data-search") || "").toLowerCase();
        var rowStatus = row.getAttribute("data-status") || "Active";
        if (search && haystack.indexOf(search) === -1) {
            return false;
        }
        if (status !== "All" && rowStatus !== status) {
            return false;
        }
        if (upstream.balanceType && upstream.balanceType !== "All" && row.getAttribute("data-category-type") !== upstream.balanceType) {
            return false;
        }
        if (upstream.scheme && upstream.scheme !== "All" && row.getAttribute("data-scheme-id") !== upstream.scheme) {
            return false;
        }
        return true;
    }

    // Client report 2026-09-21 (screenshot): with no Balance Type chosen yet, every real Scheme
    // was still fully selectable in this filter - unlike the Add drawer's own Category -> Scheme
    // -> Sub-Scheme cascade, which disables Scheme outright until a real Category is picked. The
    // bug was that an empty/"All" Balance Type short-circuited the row-matching filter (treated as
    // "no filter", i.e. every scheme in the grid counted as valid) instead of "nothing is valid
    // yet". Scheme (and, transitively, Sub-Scheme) is now disabled outright - not just filtered -
    // whenever its prerequisite has no real value, matching the drawer exactly.
    function rebuildAppendixIIIDependentFilterOptions() {
        var balanceTypeEl = $("iiiBalanceTypeFilter");
        var schemeEl = $("iiiSchemeFilter");
        var subSchemeEl = $("iiiSubSchemeFilter");
        if (!schemeEl || !subSchemeEl) {
            return;
        }
        var rows = currentGridRows();
        var balanceType = balanceTypeEl ? balanceTypeEl.value : "";
        var balanceTypeChosen = !!balanceType && balanceType !== "All";

        schemeEl.disabled = !balanceTypeChosen;
        var schemeAllOption = schemeEl.querySelector('option[value="All"]');
        if (schemeAllOption) {
            schemeAllOption.textContent = balanceTypeChosen ? "All Schemes" : "Select Balance Type first";
        }

        var validSchemeIds = {};
        if (balanceTypeChosen) {
            rows.forEach(function (row) {
                if (rowMatchesForOptions(row, { balanceType: balanceType })) {
                    var sid = row.getAttribute("data-scheme-id");
                    if (sid) {
                        validSchemeIds[sid] = true;
                    }
                }
            });
        }
        var schemeStillValid = false;
        Array.prototype.forEach.call(schemeEl.querySelectorAll("option"), function (option) {
            if (option.value === "All") {
                return;
            }
            var belongs = balanceTypeChosen && !!validSchemeIds[option.value];
            option.classList.toggle("hidden", !belongs);
            option.disabled = !belongs;
            if (belongs && option.value === schemeEl.value) {
                schemeStillValid = true;
            }
        });
        if (schemeEl.value !== "All" && !schemeStillValid) {
            schemeEl.value = "All";
        }

        var selectedScheme = schemeEl.value;
        var schemeChosen = balanceTypeChosen && selectedScheme !== "All";
        subSchemeEl.disabled = !schemeChosen;
        var subSchemeAllOption = subSchemeEl.querySelector('option[value="All"]');
        if (subSchemeAllOption) {
            subSchemeAllOption.textContent = schemeChosen ? "All Sub-Schemes" : "Select Scheme first";
        }

        var validSubSchemeIds = {};
        if (schemeChosen) {
            rows.forEach(function (row) {
                if (rowMatchesForOptions(row, { balanceType: balanceType, scheme: selectedScheme })) {
                    var ssid = row.getAttribute("data-sub-scheme-id");
                    if (ssid) {
                        validSubSchemeIds[ssid] = true;
                    }
                }
            });
        }
        var subSchemeStillValid = false;
        Array.prototype.forEach.call(subSchemeEl.querySelectorAll("option[data-scheme-id]"), function (option) {
            var belongsToScheme = schemeChosen && option.getAttribute("data-scheme-id") === selectedScheme;
            var presentInGrid = !!validSubSchemeIds[option.value];
            var belongs = belongsToScheme && presentInGrid;
            option.classList.toggle("hidden", !belongs);
            option.disabled = !belongs;
            if (belongs && option.value === subSchemeEl.value) {
                subSchemeStillValid = true;
            }
        });
        if (subSchemeEl.value !== "All" && !subSchemeStillValid) {
            subSchemeEl.value = "All";
        }
    }

    // Re-applies a filter selection saved (window._iiiSavedFilters) before an Add/Edit/Delete
    // triggered a fragment reload - no AJAX needed any more, rebuildAppendixIIIDependentFilterOptions
    // (called from applyGridFilters) validates the restored Scheme/Sub-Scheme against the freshly
    // reloaded grid on its own.
    function restoreAppendixIIIFilters() {
        var saved = window._iiiSavedFilters;
        var balanceTypeEl = $("iiiBalanceTypeFilter");
        var schemeEl = $("iiiSchemeFilter");
        var subSchemeEl = $("iiiSubSchemeFilter");
        if (saved) {
            if (balanceTypeEl) {
                balanceTypeEl.value = saved.balanceType || "";
            }
            if (schemeEl) {
                schemeEl.value = saved.scheme || "All";
            }
            if (subSchemeEl) {
                subSchemeEl.value = saved.subScheme || "All";
            }
        }
        window._iiiSavedFilters = null;
        iiiCurrentPage = 1;
        applyGridFilters();
    }

    function applyGridFilters() {
        rebuildAppendixIIIDependentFilterOptions();
        var rows = currentGridRows();
        var searchEl = $("iiiSearchInput");
        var statusEl = $("iiiStatusFilter");
        var balanceTypeEl = $("iiiBalanceTypeFilter");
        var schemeEl = $("iiiSchemeFilter");
        var subSchemeEl = $("iiiSubSchemeFilter");
        var search = (searchEl ? searchEl.value : "").trim().toLowerCase();
        var status = statusEl ? statusEl.value : "All";
        var balanceType = balanceTypeEl ? balanceTypeEl.value : "";
        var scheme = schemeEl ? schemeEl.value : "All";
        var subScheme = subSchemeEl ? subSchemeEl.value : "All";

        var matched = [];
        rows.forEach(function (row) {
            var haystack = (row.getAttribute("data-search") || "").toLowerCase();
            var rowStatus = row.getAttribute("data-status") || "Active";
            var matchesSearch = !search || haystack.indexOf(search) !== -1;
            var matchesStatus = status === "All" || rowStatus === status;
            var matchesBalanceType = balanceType === "All" || (balanceType && row.getAttribute("data-category-type") === balanceType);;
            var matchesScheme = scheme === "All" || row.getAttribute("data-scheme-id") === scheme;
            var matchesSubScheme = subScheme === "All" || row.getAttribute("data-sub-scheme-id") === subScheme;
            if (matchesSearch && matchesStatus && matchesBalanceType && matchesScheme && matchesSubScheme) {
                matched.push(row);
            } else {
                row.classList.add("hidden");
            }
        });

        var totalPages = Math.max(1, Math.ceil(matched.length / iiiPageSize));
        if (iiiCurrentPage > totalPages) {
            iiiCurrentPage = totalPages;
        }
        var start = (iiiCurrentPage - 1) * iiiPageSize;
        var pageRowSet = matched.slice(start, start + iiiPageSize);

        matched.forEach(function (row) {
            row.classList.add("hidden");
        });
        pageRowSet.forEach(function (row, index) {
            row.classList.remove("hidden");
            var cell = row.querySelector("[data-srno]");
            if (cell) {
                cell.textContent = String(start + index + 1);
            }
        });

        var empty = $("iiiEmptyState");
        var gridWrap = $("iiiGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("iiiTotalText");
        var rangeText = $("iiiRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }
        renderAppendixIIIPagination(totalPages);
        syncAppendixIIIGridToolbarState(balanceType);
        // After filtering rows, also adjust which columns are visible when the filter is an
        // exemption category so the grid shows only the minimal columns per requirements.
        try { adjustColumnsForBalanceType(balanceType, false); } catch (e) { }
    }

    function renderAppendixIIIPagination(totalPages) {
        var holder = $("iiiPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - iiiCurrentPage) <= 1) {
                    pages.push(i);
                }
            }
            var last = 0;
            pages.forEach(function (p) {
                if (last && p - last > 1) {
                    var dots = document.createElement("span");
                    dots.className = "px-1 text-xs text-slate-400";
                    dots.textContent = "…";
                    holder.appendChild(dots);
                }
                var b = document.createElement("button");
                b.type = "button";
                b.setAttribute("data-action", "appendix-iii-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === iiiCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("iiiPrevBtn");
        var nextBtn = $("iiiNextBtn");
        if (prevBtn) {
            prevBtn.disabled = iiiCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", iiiCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = iiiCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", iiiCurrentPage === totalPages);
        }
    }

    function goToAppendixIIIPage(page) {
        iiiCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixIIIPage(delta) {
        goToAppendixIIIPage(iiiCurrentPage + delta);
    }

    function changeAppendixIIIPageSize() {
        var sizeEl = $("iiiPageSize");
        iiiPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        iiiCurrentPage = 1;
        applyGridFilters();
    }

    var sortKey = null;
    var sortDir = "asc";

    function sortGridBy(key) {
        var body = $("gridBody");
        if (!body) {
            return;
        }
        if (sortKey === key) {
            sortDir = sortDir === "asc" ? "desc" : "asc";
        } else {
            sortKey = key;
            sortDir = "asc";
        }
        var rows = currentGridRows();
        rows.sort(function (a, b) {
            var av = a.getAttribute("data-sort-" + key) || "";
            var bv = b.getAttribute("data-sort-" + key) || "";
            var an = parseFloat(av);
            var bn = parseFloat(bv);
            var cmp;
            if (!isNaN(an) && !isNaN(bn) && av !== "" && bv !== "") {
                cmp = an - bn;
            } else {
                cmp = av.toLowerCase().localeCompare(bv.toLowerCase());
            }
            return sortDir === "asc" ? cmp : -cmp;
        });
        rows.forEach(function (row) { body.appendChild(row); });
        applyGridFilters();
    }

    function clearAppendixIIIFilters() {
        var searchEl = $("iiiSearchInput");
        var statusEl = $("iiiStatusFilter");
        var balanceTypeEl = $("iiiBalanceTypeFilter");
        var schemeEl = $("iiiSchemeFilter");
        var subSchemeEl = $("iiiSubSchemeFilter");
        if (searchEl) {
            searchEl.value = "";
        }
        if (statusEl) {
            statusEl.value = "All";
        }
        // Client requirement 2026-09-18: Balance Type has no "All" option any more - Clear resets
        // it to the first Balance Type in the list (index 0) instead, same as its default on load.
        if (balanceTypeEl) {
            balanceTypeEl.selectedIndex = 0;
        }
        if (schemeEl) {
            schemeEl.value = "All";
        }
        if (subSchemeEl) {
            subSchemeEl.value = "All";
        }
        applyGridFilters();
    }

    // ----------------------------------------------------------------------------------------
    // Event wiring - delegated on document, survives AJAX fragment reloads.
    // ----------------------------------------------------------------------------------------
    document.addEventListener("click", function (event) {
        if (event.target.closest(".action-menu button, .action-menu a")) {
            document.querySelectorAll(".action-menu").forEach(function (m) {
                m.classList.add("hidden");
                m.style.visibility = "";
            });
        }

        if (event.target.closest('[data-action="open-appendix-iii-add-drawer"]')) {
            openAppendixIIIDrawer("add");
            return;
        }

        var editIIIButton = event.target.closest('[data-action="edit-appendix-iii-record"]');
        if (editIIIButton) {
            // Field population + the Balance Type/Scheme/Sub-Scheme cascade + edit-mode locking is
            // handled by PreBudget-control-binding.js's own (document-scoped, unchanged) dispatch
            // for this same data-action - this just slides the drawer open; the two listeners run
            // independently and don't conflict.
            openAppendixIIIDrawer("edit");
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-iii-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var iiiCancelForm = $("appendixIIIForm");
            var iiiPcb = window.PreBudgetControlBinding;
            var iiiIsDirty = !iiiPcb || typeof iiiPcb.isFormDirtySinceOpen !== "function" || iiiPcb.isFormDirtySinceOpen(iiiCancelForm);
            if (!iiiIsDirty) {
                closeAppendixIIIDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixIIIDrawer();
                }, false, "No");
            } else {
                closeAppendixIIIDrawer();
            }
            return;
        }

        // Reset button - mode-aware (client 2026-09-10, refined):
        //  * Edit mode  -> stay in Edit mode. Keep NewRecord.Id and the "Modify" button, keep the
        //    three edit-locked dropdowns (Balance Type / Scheme / Sub-Scheme) and the auto-loaded
        //    read-only BE at their loaded values; blank only the user-editable inputs so the user
        //    can re-enter them for this same record.
        //  * Add mode   -> full clear: blank every field, drop the record id / Modify label, put
        //    Balance Type back to its placeholder and Scheme/Sub-Scheme to their disabled
        //    "pick the parent first" state.
        // event.preventDefault() so the racing generic/III reset listeners don't also fire.
        if (event.target.closest("#appendixIIIReset")) {
            event.preventDefault();
            var iiiForm = $("appendixIIIForm");
            if (!iiiForm) {
                return;
            }
            var iiiIdField = $("NewRecord_Id");
            var iiiInEditMode = !!(iiiIdField && iiiIdField.value);

            if (iiiInEditMode) {
                Array.prototype.forEach.call(
                    iiiForm.querySelectorAll('input:not([type="hidden"]):not([type="submit"]):not([type="reset"]):not([type="button"]), textarea'),
                    function (el) {
                        // Keep disabled / read-only fields and the auto-loaded BE (it's derived
                        // from the record's Scheme, not user-typed) - only clear what the user
                        // actually enters for this record.
                        if (el.disabled || el.readOnly || el.id === "NewRecord_BE") {
                            return;
                        }
                        if (el.type === "checkbox" || el.type === "radio") {
                            el.checked = el.defaultChecked;
                        } else {
                            el.value = "";
                        }
                        el.classList.remove("field-missing");
                        el.dispatchEvent(new Event("input", { bubbles: true }));
                        el.dispatchEvent(new Event("change", { bubbles: true }));
                    }
                );
                return;
            }

            var pcb = window.PreBudgetControlBinding;
            if (pcb && typeof pcb.exitAppendixEditMode === "function") {
                pcb.exitAppendixEditMode(iiiForm);
            } else {
                iiiForm.reset();
            }
            var iiiScheme = $("NewRecord_SchemeId");
            if (iiiScheme) {
                iiiScheme.innerHTML = '<option value="">Select Balance Type first</option>';
                iiiScheme.disabled = true;
            }
            var iiiSub = $("NewRecord_SubSchemeId");
            if (iiiSub) {
                iiiSub.innerHTML = '<option value="">Select Scheme first</option>';
                iiiSub.disabled = true;
            }
            var iiiCat = $("NewRecord_CategoryType");
            if (iiiCat) {
                iiiCat.selectedIndex = 0;
            }
            var iiiBe = $("NewRecord_BE");
            if (iiiBe) {
                iiiBe.value = "";
            }
            applyAppendixIIIBalanceTypeUI("");
            return;
        }

        var rowMenuBtn = event.target.closest(".row-menu-btn[data-action-id]");
        if (rowMenuBtn && rowMenuBtn.closest("#chargesTable")) {
            toggleActionMenu(rowMenuBtn.getAttribute("data-action-id"), rowMenuBtn, event);
            return;
        }
        if (!event.target.closest(".row-menu-btn") && !event.target.closest(".action-menu")) {
            document.querySelectorAll(".action-menu").forEach(function (m) { m.classList.add("hidden"); });
        }

        if (event.target.closest("#iiiColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#iiiColumnsButton") && !event.target.closest("#iiiColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-iii-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-iii"]');
        if (exportOptionBtn) {
            exportAppendixIII(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#iiiExportButton") && !event.target.closest("#iiiExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-iii-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-iii-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#iiiCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#iiiConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-iii-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-iii-filters"]')) {
            clearAppendixIIIFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-iii-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-iii-prev-page"]')) {
            changeAppendixIIIPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-iii-next-page"]')) {
            changeAppendixIIIPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-iii-goto-page"]');
        if (pageBtn) {
            goToAppendixIIIPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#chargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#iiiToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#iiiColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "iiiStatusFilter") {
            iiiCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "iiiPageSize") {
            changeAppendixIIIPageSize();
        }
        if (event.target.id === "iiiBalanceTypeFilter") {
            iiiCurrentPage = 1;
            applyGridFilters();
            try { adjustColumnsForBalanceType(event.target.value, true); } catch (e) { }
        } else if (event.target.id === "iiiSchemeFilter") {
            iiiCurrentPage = 1;
            applyGridFilters();
        } else if (event.target.id === "iiiSubSchemeFilter") {
            iiiCurrentPage = 1;
            applyGridFilters();
        }
        // Runs alongside PreBudget-control-binding.js's own NewRecord_CategoryType change handler
        // (which replays the Scheme cascade) - this one only switches which field group the drawer
        // shows, no conflict between the two listeners.
        if (event.target.id === "NewRecord_CategoryType" && event.target.closest("#appendixIIIForm")) {
            applyAppendixIIIBalanceTypeUI(event.target.value);
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "iiiSearchInput") {
            iiiCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("iiiExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("iiiColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("iiiDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("iiiDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixIIIDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__iiiHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("gridBody") && $("appendixIIIForm")) {
                iiiCurrentPage = 1;               
                restoreAppendixIIIFilters();
                adjustColumnsForBalanceType($("iiiBalanceTypeFilter") ? $("iiiBalanceTypeFilter").value : "", true);
            }
            reparentIiiDrawerToBody();
        };
        window.PreBudgetControlBinding.__iiiHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("gridBody") && $("appendixIIIForm")) {
        adjustColumnsForBalanceType($("iiiBalanceTypeFilter") ? $("iiiBalanceTypeFilter").value : "", true);
        applyGridFilters();
        syncAppendixIIIGridToolbarState($("iiiBalanceTypeFilter") ? $("iiiBalanceTypeFilter").value : "");
    }
    reparentIiiDrawerToBody();
})();
