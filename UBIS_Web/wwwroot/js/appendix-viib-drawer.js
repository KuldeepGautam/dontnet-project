// "Approved design for all appendixes" (2026-09-10) - drawer/grid interaction layer for the
// redesigned Appendix VII-B page. Mirrors appendix-viia-drawer.js's structure (see
// appendix-via-drawer.js's header comment for the original reasoning behind each piece).
//
// This file owns ONLY the drawer/grid chrome (see ubis2_appendix_redesign_reuse_shared_files
// memory). The Scheme -> Major Head AJAX cascade (loadAppendixVIIBMajorHeads), the
// Scheme+MajorHead+TransactionType BE/Actuals auto-load (loadAppendixVIIBBeActuals), the RE-BE
// IncreasedBE recalc (recalcAppendixVIIBIncreasedBE), the saved-records grid filter
// (filterAppendixVIIBGrid, which targets "#appendixVIIBGrid tbody" - kept as the table id below),
// the previous-year auto-load and the Edit-populate field mapping are all handled by
// PreBudget-control-binding.js's own document-scoped dispatch - not duplicated here.
//
// Row action menu = Edit / Delete only - no Freeze, no Duplicate (client instruction 2026-09-10).
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }
    var GRID_TABLE = "#appendixVIIBGrid";

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("viibToast");
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
        $("viibToastTitle").textContent = variant.title;
        $("viibToastIcon").setAttribute("data-lucide", variant.icon);
        $("viibToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("viibToast");
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
        var menu = $("viibExportMenu");
        var button = $("viibExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("viibExportMenu");
        var button = $("viibExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixVIIB(format, triggerButton) {
        var form = $("appendixVIIBForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixVIIB?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

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
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixVIIB." + format);
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
        var menu = $("viibColumnsMenu");
        var button = $("viibColumnsButton");
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
        var menu = $("viibColumnsMenu");
        var button = $("viibColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("viibColumnsMenu");
        if (!menu) {
            return;
        }
        Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (checkbox) {
            var column = checkbox.getAttribute("data-column-toggle");
            var show = checkbox.checked;
            document.querySelectorAll(GRID_TABLE + ' [data-column="' + column + '"]').forEach(function (cell) {
                cell.classList.toggle("column-hidden", !show);
            });
        });
    }

    function resetColumns() {
        var menu = $("viibColumnsMenu");
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
        var menu = $("viib-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("viib-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("viib-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "viibGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit) - shell only.
    // ----------------------------------------------------------------------------------------
    function openAppendixVIIBDrawer(mode) {
        var drawer = $("viibDrawer");
        var backdrop = $("viibDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixVIIBForm");

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
        }

        var titleEl = $("viibDrawerTitle");
        var subtitleEl = $("viibDrawerSubtitle");
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

    function reparentViibDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="viibDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="viibDrawerBackdrop"]');
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

    function closeAppendixVIIBDrawer() {
        var drawer = $("viibDrawer");
        var backdrop = $("viibDrawerBackdrop");
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
        var dialog = $("viibDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("viibDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("viibCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("viibDeleteDialog");
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
        var body = $("viibGridBody");
        return body ? Array.prototype.slice.call(body.querySelectorAll("tr[data-row]")) : [];
    }

    var viibCurrentPage = 1;
    var viibPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("viibSearchInput");
        var statusEl = $("viibStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / viibPageSize));
        if (viibCurrentPage > totalPages) {
            viibCurrentPage = totalPages;
        }
        var start = (viibCurrentPage - 1) * viibPageSize;
        var pageRowSet = matched.slice(start, start + viibPageSize);

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

        var empty = $("viibEmptyState");
        var gridWrap = $("viibGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("viibTotalText");
        var rangeText = $("viibRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixVIIBPagination(totalPages);
    }

    function renderAppendixVIIBPagination(totalPages) {
        var holder = $("viibPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - viibCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-viib-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === viibCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("viibPrevBtn");
        var nextBtn = $("viibNextBtn");
        if (prevBtn) {
            prevBtn.disabled = viibCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", viibCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = viibCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", viibCurrentPage === totalPages);
        }
    }

    function goToAppendixVIIBPage(page) {
        viibCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixVIIBPage(delta) {
        goToAppendixVIIBPage(viibCurrentPage + delta);
    }

    function changeAppendixVIIBPageSize() {
        var sizeEl = $("viibPageSize");
        viibPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        viibCurrentPage = 1;
        applyGridFilters();
    }

    var sortKey = null;
    var sortDir = "asc";

    function sortGridBy(key) {
        var body = $("viibGridBody");
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

    function clearAppendixVIIBFilters() {
        var searchEl = $("viibSearchInput");
        var statusEl = $("viibStatusFilter");
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

        // Client requirement 2026-09-14: "VII-B: edit mode disable all dropdowns and retain their
        // value on reset button click" - Scheme/Major Head/Transaction Type are locked via
        // lockSelectForEdit during Edit (PreBudget-control-binding.js's populateAppendixVIIBEntryForm).
        // A native <button type="reset"> restores every control's page-load initial value
        // regardless (blanking them while leaving them disabled), and the shared generic reset
        // listener (any form inside .ubis-modern-appendix) unconditionally hard-clears the form on
        // top of that - racing either with a "reset" event listener proved unreliable elsewhere
        // (see Appendix III-B/VI-B/VI-D/VI-E/VI-F/VI-G/VII-A's own comments for the confirmed
        // race). Intercept the button CLICK instead and, while still in Edit mode, prevent the
        // native reset entirely and blank only the plain editable fields ourselves - the 3 locked
        // dropdowns, the record id, and the readonly/auto-computed fields (ActualsY2, BE,
        // IncreasedBE - unaffected since Scheme/Major Head/Transaction Type don't change) are all
        // left completely untouched.
        var resetViibBtn = event.target.closest('#appendixVIIBForm button[type="reset"]');
        if (resetViibBtn) {
            var idFieldForReset = $("NewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                ["NewRecord_ActualsY1", "NewRecord_ActualsUptoSeptPrevYear", "NewRecord_ActualsUptoSept", "NewRecord_RE", "NewRecord_NBE"].forEach(function (id) {
                    var field = $(id);
                    if (field) { field.value = ""; }
                });
                // RE just blanked above - its own dependent IncreasedBE (RE - BE) display, owned by
                // PreBudget-control-binding.js's recalcAppendixVIIBIncreasedBE, needs a matching
                // "input" dispatch to recompute rather than being left showing a stale figure.
                var reField = $("NewRecord_RE");
                if (reField) {
                    reField.dispatchEvent(new Event("input", { bubbles: true }));
                }
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-viib-add-drawer"]')) {
            openAppendixVIIBDrawer("add");
            return;
        }

        var editVIIBButton = event.target.closest('[data-action="edit-appendix-viib-record"]');
        if (editVIIBButton) {
            openAppendixVIIBDrawer("edit");
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-viib-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var viibCancelForm = $("appendixVIIBForm");
            var viibPcb = window.PreBudgetControlBinding;
            var viibIsDirty = !viibPcb || typeof viibPcb.isFormDirtySinceOpen !== "function" || viibPcb.isFormDirtySinceOpen(viibCancelForm);
            if (!viibIsDirty) {
                closeAppendixVIIBDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixVIIBDrawer();
                }, false, "No");
            } else {
                closeAppendixVIIBDrawer();
            }
            return;
        }

        var rowMenuBtn = event.target.closest(".row-menu-btn[data-action-id]");
        if (rowMenuBtn && rowMenuBtn.closest(GRID_TABLE)) {
            toggleActionMenu(rowMenuBtn.getAttribute("data-action-id"), rowMenuBtn, event);
            return;
        }
        if (!event.target.closest(".row-menu-btn") && !event.target.closest(".action-menu")) {
            document.querySelectorAll(".action-menu").forEach(function (m) { m.classList.add("hidden"); });
        }

        if (event.target.closest("#viibColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#viibColumnsButton") && !event.target.closest("#viibColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-viib-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-viib"]');
        if (exportOptionBtn) {
            exportAppendixVIIB(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#viibExportButton") && !event.target.closest("#viibExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-viib-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-viib-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#viibCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#viibConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-viib-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-viib-filters"]')) {
            clearAppendixVIIBFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-viib-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-viib-prev-page"]')) {
            changeAppendixVIIBPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-viib-next-page"]')) {
            changeAppendixVIIBPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-viib-goto-page"]');
        if (pageBtn) {
            goToAppendixVIIBPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest(GRID_TABLE)) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#viibToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#viibColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "viibStatusFilter") {
            viibCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "viibPageSize") {
            changeAppendixVIIBPageSize();
        }
    });

    // Native <button type="reset"> (and the Add drawer's own exitAppendixEditMode->form.reset())
    // wipes the Scheme-cascaded Major Head list and the auto-loaded BE/Actuals without re-fetching
    // them - re-trigger the Scheme change dispatch that PreBudget-control-binding.js's cascade +
    // loadAppendixVIIBPreviousYearReference listen on. (No-op right after a reset since Scheme is
    // blank then; the real re-fetch happens once the user re-picks a Scheme.)
    document.addEventListener("reset", function (event) {
        if (event.target && event.target.id === "appendixVIIBForm") {
            window.setTimeout(function () {
                var schemeField = $("NewRecord_SchemeId");
                if (schemeField && schemeField.value) {
                    schemeField.dispatchEvent(new Event("change", { bubbles: true }));
                }
            }, 0);
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "viibSearchInput") {
            viibCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("viibExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("viibColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("viibDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("viibDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixVIIBDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__viibHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("viibGridBody") && $("appendixVIIBForm")) {
                viibCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentViibDrawerToBody();
        };
        window.PreBudgetControlBinding.__viibHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("viibGridBody") && $("appendixVIIBForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentViibDrawerToBody();
})();
