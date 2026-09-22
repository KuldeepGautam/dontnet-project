// Re-design 2026-09-15 (designer reference: UBIS-drawer-070926 "appendix-IVB.html") - drawer/grid
// interaction layer for Appendix IV-B. Mirrors appendix-iva-drawer.js's structure exactly (same
// drawer slide-in, columns picker, row action menu, delete dialog, toast, Search-only filter +
// pagination pattern - see appendix-via-drawer.js's header comment for the original reasoning
// behind each piece). No Export dropdown, no Demand/Financial Year/Status filters - kept
// consistent with every other redesigned appendix (client decision 2026-09-15, applied first to
// plain Appendix IV: match the established Search-only convention rather than the designer
// mockup's fuller filter bar).
//
// This file owns ONLY the drawer/grid chrome (see ubis2_appendix_redesign_reuse_shared_files
// memory). The Scheme -> Sub-Scheme cascade, the B.E. auto-load from SBE, the live
// Saving/Excess-in-R.E.-over-B.E. calc, and the Edit-populate field mapping are ALL already
// generic, shared logic in PreBudget-control-binding.js (populateSchemeGradedEntryForm /
// schemeGradedAppendixForms / recalcAppendixIVASavingExcess - shared with IV-A, checks both
// #ivaSavingExcess and #ivbSavingExcess - already wired for data-action="edit-appendix-ivb-record"
// and #appendixIVBForm on `document`-scoped listeners) - none of that is duplicated or modified
// here; keeping the same field/element ids (NewRecord_SchemeId, NewRecord_SubSchemeId,
// ivbSavingExcess, form id appendixIVBForm, ...) is what keeps it working unchanged after this
// reparents the form to document.body.
//
// Appendix IV-B has NO single-record rule (duplicate check keys on Scheme(+SubScheme), not
// one-per-Demand). Row action menu = Edit / Delete only - no Freeze (Freeze is header-level, same
// convention as every other appendix this session).
//
// Scheme/Sub-Scheme ARE locked during Edit (client requirement 2026-09-14, overriding the earlier
// "not locked" design this comment used to document) - see
// PreBudget-control-binding.js's populateSchemeGradedEntryForm (lockIdentity=true for this form)
// and this file's own Reset-click interception below.
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("ivbToast");
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
        $("ivbToastTitle").textContent = variant.title;
        $("ivbToastIcon").setAttribute("data-lucide", variant.icon);
        $("ivbToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("ivbToast");
        if (!toast) {
            return;
        }
        toast.classList.remove("ubis-toast-show");
        toast.classList.add("ubis-toast-hide");
    }

    // ----------------------------------------------------------------------------------------
    // Export dropdown (client requirement 2026-09-17: "same functionality as Appendix III") -
    // delegates to the shared wwwroot/js/grid-export.js, which scrapes the live #ivbChargesTable
    // DOM directly (columns-driven, matches whatever the Columns menu currently shows), same
    // pattern as Appendix III/V-A/V-B/V-C's own Export.
    // ----------------------------------------------------------------------------------------
    function toggleExportMenu(event) {
        event.stopPropagation();
        var menu = $("ivbExportMenu");
        var button = $("ivbExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("ivbExportMenu");
        var button = $("ivbExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    // Client requirement 2026-09-21: export must match the same Category->Scheme->Sub-Scheme
    // grouping (with autogenerated Total rows) as Appendix IV/IV-A's own reference-file-driven
    // exports - a shape the old scrape-the-visible-grid exporter (window.UbisGridExport.exportTable)
    // can't produce. Switched to the same real-controller-action fetch pattern as
    // ExportAppendixIV/ExportAppendixIVA.
    function exportAppendixIVB(format, triggerButton) {
        var form = $("appendixIVBForm");
        var demandId = form ? form.getAttribute("data-demand-id") : null;
        if (!demandId) {
            showAppendixToast("Could not determine the current Demand to export.", "error");
            return;
        }
        closeExportMenu();

        var originalHtml = triggerButton ? triggerButton.innerHTML : null;
        if (triggerButton) {
            triggerButton.innerHTML = '<span class="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-slate-300 border-t-ubis-teal"></span><span>Generating...</span>';
            triggerButton.disabled = true;
        }

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixIVB?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

        fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error("Export request failed with status " + response.status);
                }
                var contentType = response.headers.get("Content-Type") || "";
                if (contentType.indexOf("pdf") === -1 && contentType.indexOf("spreadsheet") === -1 && contentType.indexOf("csv") === -1) {
                    throw new Error("Unexpected response content type: " + contentType);
                }
                var disposition = response.headers.get("Content-Disposition") || "";
                var fileNameMatch = /filename="?([^";]+)"?/.exec(disposition);
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixIVB." + format);
                return response.blob().then(function (blob) { return { blob: blob, fileName: fileName }; });
            })
            .then(function (result) {
                var objectUrl = URL.createObjectURL(result.blob);
                var tempLink = document.createElement("a");
                tempLink.href = objectUrl;
                tempLink.download = result.fileName;
                document.body.appendChild(tempLink);
                tempLink.click();
                tempLink.remove();
                URL.revokeObjectURL(objectUrl);
                showAppendixToast("Report has been downloaded successfully.", "success");
            })
            .catch(function () {
                showAppendixToast("Could not generate the export. Please try again.", "error");
            })
            .finally(function () {
                if (triggerButton && originalHtml !== null) {
                    triggerButton.innerHTML = originalHtml;
                    triggerButton.disabled = false;
                }
            });
    }

    // ----------------------------------------------------------------------------------------
    // Columns menu
    // ----------------------------------------------------------------------------------------
    function toggleColumnsMenu(event) {
        event.stopPropagation();
        var menu = $("ivbColumnsMenu");
        var button = $("ivbColumnsButton");
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
        var menu = $("ivbColumnsMenu");
        var button = $("ivbColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("ivbColumnsMenu");
        if (!menu) {
            return;
        }
        Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (checkbox) {
            var column = checkbox.getAttribute("data-column-toggle");
            var show = checkbox.checked;
            document.querySelectorAll('#ivbChargesTable [data-column="' + column + '"]').forEach(function (cell) {
                cell.classList.toggle("column-hidden", !show);
            });
        });
    }

    function resetColumns() {
        var menu = $("ivbColumnsMenu");
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
        var menu = $("ivb-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("ivb-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("ivb-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "ivbGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit) - shell only.
    // ----------------------------------------------------------------------------------------
    function openAppendixIVBDrawer(mode) {
        var drawer = $("ivbDrawer");
        var backdrop = $("ivbDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixIVBForm");

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
        }

        var titleEl = $("ivbDrawerTitle");
        var subtitleEl = $("ivbDrawerSubtitle");
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
            var firstField = $("NewRecord_SchemeId");
            if (firstField && !firstField.readOnly && !firstField.disabled) {
                firstField.focus();
            }
        }, 320);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function reparentIvaDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="ivbDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="ivbDrawerBackdrop"]');
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

    function closeAppendixIVBDrawer() {
        var drawer = $("ivbDrawer");
        var backdrop = $("ivbDrawerBackdrop");
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
        var dialog = $("ivbDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("ivbDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("ivbCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("ivbDeleteDialog");
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
        var body = $("ivbGridBody");
        return body ? Array.prototype.slice.call(body.querySelectorAll("tr[data-row]")) : [];
    }

    var ivbCurrentPage = 1;
    var ivbPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("ivbSearchInput");
        var statusEl = $("ivbStatusFilter");
        var search = (searchEl ? searchEl.value : "").trim().toLowerCase();
        var status = statusEl ? statusEl.value : "All";

        var matched = [];
        rows.forEach(function (row) {
            var haystack = (row.getAttribute("data-search") || "").toLowerCase();
            var rowStatus = row.getAttribute("data-status") || "Active";
            var matchesSearch = !search || haystack.indexOf(search) !== -1;
            var matchesStatus = status === "All" || rowStatus === status;
            if (matchesSearch && matchesStatus) {
                matched.push(row);
            } else {
                row.classList.add("hidden");
            }
        });

        var totalPages = Math.max(1, Math.ceil(matched.length / ivbPageSize));
        if (ivbCurrentPage > totalPages) {
            ivbCurrentPage = totalPages;
        }
        var start = (ivbCurrentPage - 1) * ivbPageSize;
        var pageRowSet = matched.slice(start, start + ivbPageSize);

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

        var empty = $("ivbEmptyState");
        var gridWrap = $("ivbGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("ivbTotalText");
        var rangeText = $("ivbRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixIVBPagination(totalPages);
    }

    function renderAppendixIVBPagination(totalPages) {
        var holder = $("ivbPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - ivbCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-ivb-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === ivbCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("ivbPrevBtn");
        var nextBtn = $("ivbNextBtn");
        if (prevBtn) {
            prevBtn.disabled = ivbCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", ivbCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = ivbCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", ivbCurrentPage === totalPages);
        }
    }

    function goToAppendixIVBPage(page) {
        ivbCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixIVBPage(delta) {
        goToAppendixIVBPage(ivbCurrentPage + delta);
    }

    function changeAppendixIVBPageSize() {
        var sizeEl = $("ivbPageSize");
        ivbPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        ivbCurrentPage = 1;
        applyGridFilters();
    }

    var sortKey = null;
    var sortDir = "asc";

    function sortGridBy(key) {
        var body = $("ivbGridBody");
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
            var cmp = av.toLowerCase().localeCompare(bv.toLowerCase());
            return sortDir === "asc" ? cmp : -cmp;
        });
        rows.forEach(function (row) { body.appendChild(row); });
        applyGridFilters();
    }

    function clearAppendixIVBFilters() {
        var searchEl = $("ivbSearchInput");
        var statusEl = $("ivbStatusFilter");
        if (searchEl) {
            searchEl.value = "";
        }
        if (statusEl) {
            statusEl.value = "All";
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

        // Client requirement 2026-09-14: "do same edit mode scheme name, subscheme name disabled,
        // on reset click, it should remain disabled but retain value" - both are locked via
        // lockSelectForEditScoped in PreBudget-control-binding.js's populateSchemeGradedEntryForm
        // (a deliberate change from this appendix's earlier "not locked" design). Intercept the
        // Reset button's CLICK (before any "reset" event can fire, avoiding the confirmed race with
        // the shared generic hardClearAppendixForm handler - see Appendix III-B/VI-B/VI-D/IV/IV-A's
        // own comments) and, while still in Edit mode, prevent the native reset entirely and blank
        // only the plain editable fields ourselves - Scheme, Sub-Scheme and the record id stay
        // untouched; B.E. is auto-loaded/readonly and stays untouched too. Dispatches input+change
        // on the blanked fields so the existing document-scoped Saving/Excess formula recalc
        // (recalcAppendixIVASavingExcess) picks up the now-empty Proposed R.E.
        var resetIvbBtn = event.target.closest('#appendixIVBForm button[type="reset"]');
        if (resetIvbBtn) {
            var idFieldForReset = $("NewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                ["NewRecord_Actuals", "NewRecord_ActualsUptoSeptPrevYear", "NewRecord_ActualsUptoSept", "NewRecord_ProposedRE", "NewRecord_ProposedNBE", "NewRecord_RemarksMinistry", "NewRecord_RemarksBudget", "NewRecord_BudgetRecommendedRE", "NewRecord_BudgetRecommendedNBE"].forEach(function (id) {
                    var field = $(id);
                    if (!field) {
                        return;
                    }
                    field.value = "";
                    field.dispatchEvent(new Event("input", { bubbles: true }));
                    field.dispatchEvent(new Event("change", { bubbles: true }));
                });
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-ivb-add-drawer"]')) {
            openAppendixIVBDrawer("add");
            return;
        }

        var editIVBButton = event.target.closest('[data-action="edit-appendix-ivb-record"]');
        if (editIVBButton) {
            // Field population + the Scheme->Sub-Scheme cascade + BE auto-load + Saving/Excess
            // recalc is handled by PreBudget-control-binding.js's own (document-scoped, unchanged)
            // dispatch for this same data-action - this just slides the drawer open.
            openAppendixIVBDrawer("edit");
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-ivb-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var ivbCancelForm = $("appendixIVBForm");
            var ivbPcb = window.PreBudgetControlBinding;
            var ivbIsDirty = !ivbPcb || typeof ivbPcb.isFormDirtySinceOpen !== "function" || ivbPcb.isFormDirtySinceOpen(ivbCancelForm);
            if (!ivbIsDirty) {
                closeAppendixIVBDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixIVBDrawer();
                }, false, "No");
            } else {
                closeAppendixIVBDrawer();
            }
            return;
        }

        var rowMenuBtn = event.target.closest(".row-menu-btn[data-action-id]");
        if (rowMenuBtn && rowMenuBtn.closest("#ivbChargesTable")) {
            toggleActionMenu(rowMenuBtn.getAttribute("data-action-id"), rowMenuBtn, event);
            return;
        }
        if (!event.target.closest(".row-menu-btn") && !event.target.closest(".action-menu")) {
            document.querySelectorAll(".action-menu").forEach(function (m) { m.classList.add("hidden"); });
        }

        if (event.target.closest("#ivbColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#ivbColumnsButton") && !event.target.closest("#ivbColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-ivb-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var ivbExportOptionBtn = event.target.closest('[data-action="export-appendix-ivb"]');
        if (ivbExportOptionBtn) {
            exportAppendixIVB(ivbExportOptionBtn.getAttribute("data-format"), ivbExportOptionBtn);
            return;
        }
        if (!event.target.closest("#ivbExportButton") && !event.target.closest("#ivbExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-ivb-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-ivb-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#ivbCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#ivbConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-ivb-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-ivb-filters"]')) {
            clearAppendixIVBFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-ivb-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-ivb-prev-page"]')) {
            changeAppendixIVBPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-ivb-next-page"]')) {
            changeAppendixIVBPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-ivb-goto-page"]');
        if (pageBtn) {
            goToAppendixIVBPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#ivbChargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#ivbToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#ivbColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "ivbStatusFilter") {
            ivbCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "ivbPageSize") {
            changeAppendixIVBPageSize();
        }
    });

    // Native <button type="reset"> (and the Add drawer's own exitAppendixEditMode->form.reset())
    // wipes the previous-year auto-loaded B.E. without re-fetching it, and resets Sub-Scheme back
    // to its placeholder without re-disabling it - re-run the Scheme change dispatch that
    // PreBudget-control-binding.js's own cascade/BE-auto-load listens on. Mostly a no-op guard
    // right after a reset (Scheme is blank then); the real re-fetch happens once the user re-picks
    // a Scheme.
    document.addEventListener("reset", function (event) {
        if (event.target && event.target.id === "appendixIVBForm") {
            window.setTimeout(function () {
                var schemeField = $("NewRecord_SchemeId");
                if (schemeField && schemeField.value) {
                    schemeField.dispatchEvent(new Event("change", { bubbles: true }));
                }
            }, 0);
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "ivbSearchInput") {
            ivbCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var ivbExportMenuEsc = $("ivbExportMenu");
        if (ivbExportMenuEsc && !ivbExportMenuEsc.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("ivbColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("ivbDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("ivbDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixIVBDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__ivbHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("ivbGridBody") && $("appendixIVBForm")) {
                ivbCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentIvaDrawerToBody();
        };
        window.PreBudgetControlBinding.__ivbHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("ivbGridBody") && $("appendixIVBForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentIvaDrawerToBody();
})();
