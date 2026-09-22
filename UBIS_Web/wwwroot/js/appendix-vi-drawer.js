// "Approved design for all appendixes" (2026-09-10, per Appendix6.html) - drawer/grid interaction
// layer for the redesigned Appendix VI page. Mirrors appendix-iii-drawer.js/appendix-iiib-drawer.js's
// own structure (see appendix-via-drawer.js's header comment for the original reasoning behind each
// piece: drawer slide-in, columns picker, row action menu, delete dialog, toast, search/status
// filter + pagination, Export dropdown).
//
// This file owns ONLY the drawer/grid chrome (see ubis2_appendix_redesign_reuse_shared_files
// memory). The Receipt Type -> Actuals/BE/ActualsUptoSept previous-year auto-load
// (loadAppendixVIPreviousYearReference) and the Edit-populate field mapping are handled by
// PreBudget-control-binding.js's own (document-scoped) dispatch for
// data-action="edit-appendix-vi-record" / NewRecord_ReceiptType's change event - not duplicated
// here.
//
// Like III/III-B, VI has NO single-record rule - Add stays available whenever the appendix isn't
// locked (see AppendixVI.cshtml's header, gated only on !locked), since VI's duplicate check keys
// on the Receipt Type/PSU-Receipt-Name combination, not one-per-Demand (see
// ubis2_appendix_redesign_business_rules_checklist memory).
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("viToast");
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
        $("viToastTitle").textContent = variant.title;
        $("viToastIcon").setAttribute("data-lucide", variant.icon);
        $("viToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("viToast");
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
        var menu = $("viExportMenu");
        var button = $("viExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("viExportMenu");
        var button = $("viExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixVI(format, triggerButton) {
        var form = $("appendixVIForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixVI?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

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
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixVI." + format);
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
        var menu = $("viColumnsMenu");
        var button = $("viColumnsButton");
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
        var menu = $("viColumnsMenu");
        var button = $("viColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("viColumnsMenu");
        if (!menu) {
            return;
        }
        Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (checkbox) {
            var column = checkbox.getAttribute("data-column-toggle");
            var show = checkbox.checked;
            document.querySelectorAll('#viChargesTable [data-column="' + column + '"]').forEach(function (cell) {
                cell.classList.toggle("column-hidden", !show);
            });
        });
    }

    function resetColumns() {
        var menu = $("viColumnsMenu");
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
        var menu = $("vi-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("vi-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("vi-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "viGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit) - shell only. The Receipt Type previous-year auto-load and field
    // population on Edit are all handled by the existing PreBudget-control-binding.js logic (see
    // file header).
    // ----------------------------------------------------------------------------------------
    function openAppendixVIDrawer(mode) {
        var drawer = $("viDrawer");
        var backdrop = $("viDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixVIForm");

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
            // Receipt Type + PSU/Receipt Name are locked in Edit mode (client 2026-09-10 - they are
            // the record identity: Demand + Receipt Type + PSU/Receipt Name must be unique). Undo
            // that here for a fresh Add. The Receipt Type <select> is unlocked by
            // exitAppendixEditMode -> unlockSelectsAfterEdit; only the text input needs a manual reset.
            var viPsuAdd = $("NewRecord_PsuReceiptName");
            if (viPsuAdd) {
                viPsuAdd.readOnly = false;
                viPsuAdd.classList.remove("bg-slate-100", "cursor-not-allowed");
            }
        }

        var titleEl = $("viDrawerTitle");
        var subtitleEl = $("viDrawerSubtitle");
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
            var firstField = $("NewRecord_ReceiptTypeId");
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
    function reparentViDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="viDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="viDrawerBackdrop"]');
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

    function closeAppendixVIDrawer() {
        var drawer = $("viDrawer");
        var backdrop = $("viDrawerBackdrop");
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
        var dialog = $("viDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("viDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("viCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("viDeleteDialog");
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
        var body = $("viGridBody");
        return body ? Array.prototype.slice.call(body.querySelectorAll("tr[data-row]")) : [];
    }

    var viCurrentPage = 1;
    var viPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("viSearchInput");
        var statusEl = $("viStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / viPageSize));
        if (viCurrentPage > totalPages) {
            viCurrentPage = totalPages;
        }
        var start = (viCurrentPage - 1) * viPageSize;
        var pageRowSet = matched.slice(start, start + viPageSize);

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

        var empty = $("viEmptyState");
        var gridWrap = $("viGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("viTotalText");
        var rangeText = $("viRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixVIPagination(totalPages);
    }

    function renderAppendixVIPagination(totalPages) {
        var holder = $("viPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - viCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-vi-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === viCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("viPrevBtn");
        var nextBtn = $("viNextBtn");
        if (prevBtn) {
            prevBtn.disabled = viCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", viCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = viCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", viCurrentPage === totalPages);
        }
    }

    function goToAppendixVIPage(page) {
        viCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixVIPage(delta) {
        goToAppendixVIPage(viCurrentPage + delta);
    }

    function changeAppendixVIPageSize() {
        var sizeEl = $("viPageSize");
        viPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        viCurrentPage = 1;
        applyGridFilters();
    }

    var sortKey = null;
    var sortDir = "asc";

    function sortGridBy(key) {
        var body = $("viGridBody");
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

    function clearAppendixVIFilters() {
        var searchEl = $("viSearchInput");
        var statusEl = $("viStatusFilter");
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

        // Client requirement 2026-09-14: "bind receipt type dropdown from grid and disable it and
        // it should retain its value and be disabled on reset button click" - Receipt Type (and
        // PSU/Receipt Name, same identity pair) are already locked on Edit (see
        // PreBudget-control-binding.js's edit-appendix-vi-record dispatch: lockSelectForEdit +
        // readOnly). A native <button type="reset"> restores every control's page-load initial
        // value regardless (blanking the select while leaving it disabled, and clearing the
        // readOnly text input's value), and the shared generic reset listener (any form inside
        // .ubis-modern-appendix) unconditionally hard-clears the form on top of that - racing
        // either with a "reset" event listener proved unreliable elsewhere (see Appendix
        // III-B/VI-B/VI-D/VI-E/VI-F/VI-G/VII-A/VII-B's own comments for the confirmed race).
        // Intercept the button CLICK instead and, while still in Edit mode, prevent the native
        // reset entirely and blank only the plain editable fields ourselves - Receipt Type, PSU/
        // Receipt Name and the record id are left completely untouched.
        var resetViBtn = event.target.closest('#appendixVIForm button[type="reset"]');
        if (resetViBtn) {
            var idFieldForReset = $("NewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                ["NewRecord_Actuals", "NewRecord_BE", "NewRecord_ActualsUptoSept", "NewRecord_ProposedBE", "NewRecord_ProposedCollectionQ3", "NewRecord_ProposedCollectionQ4", "NewRecord_Remarks"].forEach(function (id) {
                    var field = $(id);
                    if (field) { field.value = ""; }
                });
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-vi-add-drawer"]')) {
            openAppendixVIDrawer("add");
            return;
        }

        var editVIButton = event.target.closest('[data-action="edit-appendix-vi-record"]');
        if (editVIButton) {
            // Field population + the previous-year auto-load is handled by
            // PreBudget-control-binding.js's own (document-scoped, unchanged) dispatch for this
            // same data-action - this just slides the drawer open; the two listeners run
            // independently and don't conflict.
            openAppendixVIDrawer("edit");
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-vi-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var viCancelForm = $("appendixVIForm");
            var viPcb = window.PreBudgetControlBinding;
            var viIsDirty = !viPcb || typeof viPcb.isFormDirtySinceOpen !== "function" || viPcb.isFormDirtySinceOpen(viCancelForm);
            if (!viIsDirty) {
                closeAppendixVIDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixVIDrawer();
                }, false, "No");
            } else {
                closeAppendixVIDrawer();
            }
            return;
        }

        var rowMenuBtn = event.target.closest(".row-menu-btn[data-action-id]");
        if (rowMenuBtn && rowMenuBtn.closest("#viChargesTable")) {
            toggleActionMenu(rowMenuBtn.getAttribute("data-action-id"), rowMenuBtn, event);
            return;
        }
        if (!event.target.closest(".row-menu-btn") && !event.target.closest(".action-menu")) {
            document.querySelectorAll(".action-menu").forEach(function (m) { m.classList.add("hidden"); });
        }

        if (event.target.closest("#viColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#viColumnsButton") && !event.target.closest("#viColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-vi-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-vi"]');
        if (exportOptionBtn) {
            exportAppendixVI(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#viExportButton") && !event.target.closest("#viExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-vi-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-vi-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#viCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#viConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-vi-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-vi-filters"]')) {
            clearAppendixVIFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-vi-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-vi-prev-page"]')) {
            changeAppendixVIPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-vi-next-page"]')) {
            changeAppendixVIPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-vi-goto-page"]');
        if (pageBtn) {
            goToAppendixVIPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#viChargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#viToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#viColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "viStatusFilter") {
            viCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "viPageSize") {
            changeAppendixVIPageSize();
        }
    });

    // Native <button type="reset"> (and the Add drawer's own exitAppendixEditMode->form.reset())
    // wipes the previous-year auto-loaded Actuals/BE/ActualsUptoSept without re-fetching them -
    // same fix already applied to Appendix I-A/II's own previous-year boxes (client report
    // 2026-09-10 pattern). PreBudget-control-binding.js owns loadAppendixVIPreviousYearReference
    // itself, so this just needs to re-trigger it via the same ReceiptType change dispatch once
    // there's actually a ReceiptType to look up again (there won't be right after a reset, so this
    // is mostly a no-op guard for symmetry with the other appendixes - the real re-fetch happens
    // once the user re-picks a Receipt Type, which already fires "change" natively).
    document.addEventListener("reset", function (event) {
        if (event.target && event.target.id === "appendixVIForm") {
            window.setTimeout(function () {
                var receiptTypeField = $("NewRecord_ReceiptTypeId");
                if (receiptTypeField && receiptTypeField.value) {
                    receiptTypeField.dispatchEvent(new Event("change", { bubbles: true }));
                }
            }, 0);
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "viSearchInput") {
            viCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("viExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("viColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("viDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("viDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixVIDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__viHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("viGridBody") && $("appendixVIForm")) {
                viCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentViDrawerToBody();
        };
        window.PreBudgetControlBinding.__viHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("viGridBody") && $("appendixVIForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentViDrawerToBody();
})();
