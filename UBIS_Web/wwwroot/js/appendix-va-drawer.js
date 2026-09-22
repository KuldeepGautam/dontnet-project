// Re-design 2026-09-14 (designer reference: UBIS-drawer-070926 "Appendix-V-A.html") - drawer/grid
// interaction layer for Appendix V-A. Split out of the former tabbed AppendixVGroup.cshtml (which
// covered V-A/V-B/V-C in one page) into its own standalone page/file, mirroring
// appendix-iv-drawer.js's structure (toast, columns picker, row action menu, delete dialog,
// Search-only filter + pagination, drawer open/close/reparenting - see appendix-via-drawer.js's
// header comment for the original reasoning behind each piece) plus VI-F's columns-driven Export
// wiring (grid-export.js), neither of which existed on the old tabbed page.
//
// This file owns ONLY the drawer/grid chrome (see ubis2_appendix_redesign_reuse_shared_files
// memory). The Autonomous-Body-keyed previous-year auto-load, the Edit-populate field mapping
// (populateAppendixVAEntryForm), and lockSelectForEdit/unlockSelectsAfterEdit are ALL already
// generic, shared logic in PreBudget-control-binding.js - none of that is duplicated or modified
// here; keeping the same field/element ids (VANewRecord_AutonomousBodyId, the reflection-bound
// VANewRecord_GiaGeneral*/GiaCca*/GiaSalary* fields, form id appendixVAForm, ...) is what keeps it
// working unchanged after this reparents the form to document.body.
//
// Appendix V-A has NO single-record rule (duplicate check keys on AutonomousBodyId, not
// one-per-Demand). Row action menu = Edit / Delete only - no Freeze (Freeze is header-level).
//
// Name of Autonomous Body IS locked during Edit (client requirement, unchanged from the former
// tabbed page - PreBudget-control-binding.js already calls lockSelectForEdit("VANewRecord_AutonomousBodyId")
// on Edit-populate) - what's new here is the Reset-click interception below, replacing the old
// "reset" event listener race against the shared generic hardClearAppendixForm handler (the same
// bug class found and fixed across III-B/VI-B/VI-D/IV/IV-A/IV-B this session).
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    var VA_SECTION_PREFIXES = ["GiaGeneral", "GiaCca", "GiaSalary"];
    var VA_SECTION_SUFFIXES = ["Actuals", "ActualsUptoSeptPrevYear", "BE", "ActualsUptoSept", "RE", "NBE"];

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("vaToast");
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
        $("vaToastTitle").textContent = variant.title;
        $("vaToastIcon").setAttribute("data-lucide", variant.icon);
        $("vaToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("vaToast");
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
        var menu = $("vaExportMenu");
        var button = $("vaExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("vaExportMenu");
        var button = $("vaExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixVA(format, triggerButton) {
        var form = $("appendixVAForm");
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
            tableSelector: "#vaChargesTable",
            format: format,
            title: "Appendix V-A - Grant in Aid to Autonomous and other Bodies",
            subtitle: "Demand ID: " + (demandId || ""),
            fileName: "AppendixVA-" + new Date().toISOString().replace(/[:.]/g, "-"),
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
        var menu = $("vaColumnsMenu");
        var button = $("vaColumnsButton");
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
        var menu = $("vaColumnsMenu");
        var button = $("vaColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("vaColumnsMenu");
        if (!menu) {
            return;
        }
        Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (checkbox) {
            var column = checkbox.getAttribute("data-column-toggle");
            var show = checkbox.checked;
            document.querySelectorAll('#vaChargesTable [data-column="' + column + '"]').forEach(function (cell) {
                cell.classList.toggle("column-hidden", !show);
            });
        });
    }

    function resetColumns() {
        var menu = $("vaColumnsMenu");
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
        var menu = $("va-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("va-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("va-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="va-' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "vaGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit) - shell only.
    // ----------------------------------------------------------------------------------------
    function openAppendixVADrawer(mode) {        
       var drawer = $("vaDrawer");       
        var backdrop = $("vaDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixVAForm");

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
        }

        var titleEl = $("vaDrawerTitle");
        var subtitleEl = $("vaDrawerSubtitle");
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
            var firstField = $("VANewRecord_AutonomousBodyId");
            if (firstField && !firstField.disabled) {
                firstField.focus();
            }
        }, 320);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function reparentVaDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="vaDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="vaDrawerBackdrop"]');
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

    function closeAppendixVADrawer() {
        var drawer = $("vaDrawer");
        var backdrop = $("vaDrawerBackdrop");
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
        var dialog = $("vaDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("vaDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("vaCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("vaDeleteDialog");
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
        var body = $("vaGridBody");
        return body ? Array.prototype.slice.call(body.querySelectorAll("tr[data-row]")) : [];
    }

    var vaCurrentPage = 1;
    var vaPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("vaSearchInput");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / vaPageSize));
        if (vaCurrentPage > totalPages) {
            vaCurrentPage = totalPages;
        }
        var start = (vaCurrentPage - 1) * vaPageSize;
        var pageRowSet = matched.slice(start, start + vaPageSize);

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

        var empty = $("vaEmptyState");
        var gridWrap = $("vaGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("vaTotalText");
        var rangeText = $("vaRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixVAPagination(totalPages);
    }

    function renderAppendixVAPagination(totalPages) {
        var holder = $("vaPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - vaCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-va-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === vaCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("vaPrevBtn");
        var nextBtn = $("vaNextBtn");
        if (prevBtn) {
            prevBtn.disabled = vaCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", vaCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = vaCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", vaCurrentPage === totalPages);
        }
    }

    function goToAppendixVAPage(page) {
        vaCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixVAPage(delta) {
        goToAppendixVAPage(vaCurrentPage + delta);
    }

    function changeAppendixVAPageSize() {
        var sizeEl = $("vaPageSize");
        vaPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        vaCurrentPage = 1;
        applyGridFilters();
    }

    var sortKey = null;
    var sortDir = "asc";

    function sortGridBy(key) {
        var body = $("vaGridBody");
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

    function clearAppendixVAFilters() {
        var searchEl = $("vaSearchInput");
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

        // Client requirement: "Name of Autonomous Body is unique, no duplicate entry allowed. edit
        // mode disable the control, reset button click: retain value and disabled." The select is
        // already locked on Edit by PreBudget-control-binding.js's lockSelectForEdit. A native
        // <button type="reset"> restores every control's page-load initial value regardless
        // (blanking it while leaving it disabled), and the shared generic reset listener (any form
        // inside .ubis-modern-appendix) unconditionally hard-clears the form on top of that -
        // racing either with a "reset" event listener proved unreliable elsewhere (see Appendix
        // III-B/VI-B/VI-D/IV's own comments for the confirmed race). Intercept the button CLICK
        // instead and, while still in Edit mode, prevent the native reset entirely and blank only
        // the plain editable fields ourselves - Autonomous Body and the record id stay untouched.
        var resetVaBtn = event.target.closest('#appendixVAForm button[type="reset"]');
        if (resetVaBtn) {
            var idFieldForReset = $("VANewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                VA_SECTION_PREFIXES.forEach(function (prefix) {
                    VA_SECTION_SUFFIXES.forEach(function (suffix) {
                        var field = $("VANewRecord_" + prefix + suffix);
                        if (!field) {
                            return;
                        }
                        field.value = "";
                        field.dispatchEvent(new Event("input", { bubbles: true }));
                        field.dispatchEvent(new Event("change", { bubbles: true }));
                    });
                });
                var salaryTotal = $("VANewRecord_GiaSalaryTotal");
                if (salaryTotal) {
                    salaryTotal.value = "";
                }
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-va-add-drawer"]')) {
            openAppendixVADrawer("add");
            return;
        }

        var editVAButton = event.target.closest('[data-action="edit-appendix-va-record"]');
        if (editVAButton) {
            // Field population + Autonomous Body lock is handled by PreBudget-control-binding.js's
            // own (document-scoped, unchanged) dispatch for this same data-action - this just
            // slides the drawer open.
            openAppendixVADrawer("edit");
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-va-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var vaCancelForm = $("appendixVAForm");
            var vaPcb = window.PreBudgetControlBinding;
            var vaIsDirty = !vaPcb || typeof vaPcb.isFormDirtySinceOpen !== "function" || vaPcb.isFormDirtySinceOpen(vaCancelForm);
            if (!vaIsDirty) {
                closeAppendixVADrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixVADrawer();
                }, false, "No");
            } else {
                closeAppendixVADrawer();
            }
            return;
        }

        var rowMenuBtn = event.target.closest(".row-menu-btn[data-action-id]");
        if (rowMenuBtn && rowMenuBtn.closest("#vaChargesTable")) {
            toggleActionMenu(rowMenuBtn.getAttribute("data-action-id").replace(/^va-/, ""), rowMenuBtn, event);
            return;
        }
        if (!event.target.closest(".row-menu-btn") && !event.target.closest(".action-menu")) {
            document.querySelectorAll(".action-menu").forEach(function (m) { m.classList.add("hidden"); });
        }

        if (event.target.closest("#vaColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#vaColumnsButton") && !event.target.closest("#vaColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-va-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-va"]');
        if (exportOptionBtn) {
            exportAppendixVA(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#vaExportButton") && !event.target.closest("#vaExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-va-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-va-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#vaCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#vaConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-va-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-va-filters"]')) {
            clearAppendixVAFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-va-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-va-prev-page"]')) {
            changeAppendixVAPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-va-next-page"]')) {
            changeAppendixVAPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-va-goto-page"]');
        if (pageBtn) {
            goToAppendixVAPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#vaChargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#vaToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#vaColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "vaPageSize") {
            changeAppendixVAPageSize();
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "vaSearchInput") {
            vaCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("vaExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("vaColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("vaDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("vaDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixVADrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__vaHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("vaGridBody") && $("appendixVAForm")) {
                vaCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentVaDrawerToBody(); 
        };
        window.PreBudgetControlBinding.__vaHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("vaGridBody") && $("appendixVAForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentVaDrawerToBody();
})();
