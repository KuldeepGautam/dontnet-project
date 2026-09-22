// Re-design 2026-09-14 (designer reference: UBIS-drawer-070926 "Appendix-V-B.html") - drawer/grid
// interaction layer for Appendix V-B. Split out of the former tabbed AppendixVGroup.cshtml into its
// own standalone page/file - see appendix-va-drawer.js's header comment for the full reasoning
// behind each shared piece (toast, columns picker, row action menu, delete dialog, Search-only
// filter + pagination, drawer open/close/reparenting, columns-driven Export via grid-export.js).
//
// This file owns ONLY the drawer/grid chrome. The Object-Head-keyed previous-year auto-load (BE +
// Actuals both resolved from dbo.DDG via PrevDemandId), the Edit-populate field mapping
// (populateAppendixVBEntryForm), and lockSelectForEdit/unlockSelectsAfterEdit are ALL already
// generic, shared logic in PreBudget-control-binding.js - none of that is duplicated or modified
// here; keeping the same field ids (VBNewRecord_ObjectHeadId, VBNewRecord_Actuals, ..., form id
// appendixVBForm) is what keeps it working unchanged after this reparents the form to document.body.
//
// Appendix V-B has NO single-record rule (duplicate check keys on ObjectHeadId, not one-per-Demand).
// Row action menu = Edit / Delete only - no Freeze (Freeze is header-level).
//
// Object Head IS locked during Edit (client requirement, unchanged from the former tabbed page -
// PreBudget-control-binding.js already calls lockSelectForEdit("VBNewRecord_ObjectHeadId") on
// Edit-populate) - what's new here is the Reset-click interception below, replacing the old "reset"
// event listener race against the shared generic hardClearAppendixForm handler (the same bug class
// found and fixed across III-B/VI-B/VI-D/IV/IV-A/IV-B/V-A this session).
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("vbToast");
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
        $("vbToastTitle").textContent = variant.title;
        $("vbToastIcon").setAttribute("data-lucide", variant.icon);
        $("vbToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("vbToast");
        if (!toast) {
            return;
        }
        toast.classList.remove("ubis-toast-show");
        toast.classList.add("ubis-toast-hide");
    }

    // ----------------------------------------------------------------------------------------
    // Export dropdown - columns-driven (grid-export.js), not a hand-maintained MVC action.
    // ----------------------------------------------------------------------------------------
    function toggleExportMenu(event) {
        event.stopPropagation();
        var menu = $("vbExportMenu");
        var button = $("vbExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("vbExportMenu");
        var button = $("vbExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixVB(format, triggerButton) {
        var form = $("appendixVBForm");
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
            tableSelector: "#vbChargesTable",
            format: format,
            title: "Appendix V-B - Details of Establishment Expenditure - Object Head wise",
            subtitle: "Demand ID: " + (demandId || ""),
            fileName: "AppendixVB-" + new Date().toISOString().replace(/[:.]/g, "-"),
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
        var menu = $("vbColumnsMenu");
        var button = $("vbColumnsButton");
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
        var menu = $("vbColumnsMenu");
        var button = $("vbColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("vbColumnsMenu");
        if (!menu) {
            return;
        }
        Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (checkbox) {
            var column = checkbox.getAttribute("data-column-toggle");
            var show = checkbox.checked;
            document.querySelectorAll('#vbChargesTable [data-column="' + column + '"]').forEach(function (cell) {
                cell.classList.toggle("column-hidden", !show);
            });
        });
    }

    function resetColumns() {
        var menu = $("vbColumnsMenu");
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
        var menu = $("vb-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("vb-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("vb-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="vb-' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "vbGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit) - shell only.
    // ----------------------------------------------------------------------------------------
    function openAppendixVBDrawer(mode) {
        var drawer = $("vbDrawer");
        var backdrop = $("vbDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixVBForm");

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
        }

        var titleEl = $("vbDrawerTitle");
        var subtitleEl = $("vbDrawerSubtitle");
        if (titleEl) {
            titleEl.textContent = mode === "edit" ? "Edit Record" : "Add Record";
        }
        if (subtitleEl) {
            subtitleEl.textContent = mode === "edit" ? "Update the selected record" : "Create a new record";
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
            var firstField = $("VBNewRecord_ObjectHeadId");
            if (firstField && !firstField.disabled) {
                firstField.focus();
            }
        }, 320);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function reparentVbDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="vbDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="vbDrawerBackdrop"]');
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

    function closeAppendixVBDrawer() {
        var drawer = $("vbDrawer");
        var backdrop = $("vbDrawerBackdrop");
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
        var dialog = $("vbDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("vbDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("vbCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("vbDeleteDialog");
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
    // Search + pagination over the already server-rendered rows.
    // ----------------------------------------------------------------------------------------
    function currentGridRows() {
        var body = $("vbGridBody");
        return body ? Array.prototype.slice.call(body.querySelectorAll("tr[data-row]")) : [];
    }

    var vbCurrentPage = 1;
    var vbPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("vbSearchInput");
        var search = (searchEl ? searchEl.value : "").trim().toLowerCase();

        var matched = [];
        rows.forEach(function (row) {
            var haystack = (row.getAttribute("data-search") || "").toLowerCase();
            if (!search || haystack.indexOf(search) !== -1) {
                matched.push(row);
            } else {
                row.classList.add("hidden");
            }
        });

        var totalPages = Math.max(1, Math.ceil(matched.length / vbPageSize));
        if (vbCurrentPage > totalPages) {
            vbCurrentPage = totalPages;
        }
        var start = (vbCurrentPage - 1) * vbPageSize;
        var pageRowSet = matched.slice(start, start + vbPageSize);

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

        var empty = $("vbEmptyState");
        var gridWrap = $("vbGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("vbTotalText");
        var rangeText = $("vbRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixVBPagination(totalPages);
    }

    function renderAppendixVBPagination(totalPages) {
        var holder = $("vbPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - vbCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-vb-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === vbCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("vbPrevBtn");
        var nextBtn = $("vbNextBtn");
        if (prevBtn) {
            prevBtn.disabled = vbCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", vbCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = vbCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", vbCurrentPage === totalPages);
        }
    }

    function goToAppendixVBPage(page) {
        vbCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixVBPage(delta) {
        goToAppendixVBPage(vbCurrentPage + delta);
    }

    function changeAppendixVBPageSize() {
        var sizeEl = $("vbPageSize");
        vbPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        vbCurrentPage = 1;
        applyGridFilters();
    }

    var sortKey = null;
    var sortDir = "asc";

    function sortGridBy(key) {
        var body = $("vbGridBody");
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

    function clearAppendixVBFilters() {
        var searchEl = $("vbSearchInput");
        if (searchEl) {
            searchEl.value = "";
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

        // Client requirement: "Object Head is unique, on edit, disable it, on reset, retain value
        // and keep it disabled." The select is already locked on Edit by
        // PreBudget-control-binding.js's lockSelectForEdit. Intercept the Reset button's CLICK
        // (before any "reset" event can fire, avoiding the confirmed race with the shared generic
        // hardClearAppendixForm handler - see Appendix III-B/VI-B/VI-D/IV/V-A's own comments) and,
        // while still in Edit mode, prevent the native reset entirely and blank only the plain
        // editable fields ourselves - Object Head and the record id stay untouched.
        var resetVbBtn = event.target.closest('#appendixVBForm button[type="reset"]');
        if (resetVbBtn) {
            var idFieldForReset = $("VBNewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                ["VBNewRecord_ActualsUptoSeptPrevYear", "VBNewRecord_ActualsUptoSept", "VBNewRecord_ProposedRE", "VBNewRecord_ProposedNBE", "VBNewRecord_Remarks"].forEach(function (id) {
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

        if (event.target.closest('[data-action="open-appendix-vb-add-drawer"]')) {
            openAppendixVBDrawer("add");
            return;
        }

        var editVBButton = event.target.closest('[data-action="edit-appendix-vb-record"]');
        if (editVBButton) {
            // Field population + Object Head lock + BE/Actuals previous-year auto-load is handled
            // by PreBudget-control-binding.js's own (document-scoped, unchanged) dispatch for this
            // same data-action - this just slides the drawer open.
            openAppendixVBDrawer("edit");
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-vb-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var vbCancelForm = $("appendixVBForm");
            var vbPcb = window.PreBudgetControlBinding;
            var vbIsDirty = !vbPcb || typeof vbPcb.isFormDirtySinceOpen !== "function" || vbPcb.isFormDirtySinceOpen(vbCancelForm);
            if (!vbIsDirty) {
                closeAppendixVBDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixVBDrawer();
                }, false, "No");
            } else {
                closeAppendixVBDrawer();
            }
            return;
        }

        var rowMenuBtn = event.target.closest(".row-menu-btn[data-action-id]");
        if (rowMenuBtn && rowMenuBtn.closest("#vbChargesTable")) {
            toggleActionMenu(rowMenuBtn.getAttribute("data-action-id").replace(/^vb-/, ""), rowMenuBtn, event);
            return;
        }
        if (!event.target.closest(".row-menu-btn") && !event.target.closest(".action-menu")) {
            document.querySelectorAll(".action-menu").forEach(function (m) { m.classList.add("hidden"); });
        }

        if (event.target.closest("#vbColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#vbColumnsButton") && !event.target.closest("#vbColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-vb-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-vb"]');
        if (exportOptionBtn) {
            exportAppendixVB(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#vbExportButton") && !event.target.closest("#vbExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-vb-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-vb-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#vbCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#vbConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-vb-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-vb-filters"]')) {
            clearAppendixVBFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-vb-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-vb-prev-page"]')) {
            changeAppendixVBPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-vb-next-page"]')) {
            changeAppendixVBPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-vb-goto-page"]');
        if (pageBtn) {
            goToAppendixVBPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#vbChargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#vbToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#vbColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "vbPageSize") {
            changeAppendixVBPageSize();
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "vbSearchInput") {
            vbCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("vbExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("vbColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("vbDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("vbDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixVBDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__vbHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("vbGridBody") && $("appendixVBForm")) {
                vbCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentVbDrawerToBody();
        };
        window.PreBudgetControlBinding.__vbHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("vbGridBody") && $("appendixVBForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentVbDrawerToBody();
})();
