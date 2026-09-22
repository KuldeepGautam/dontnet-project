// "Approved design for all appendixes" (2026-09-10) - drawer/grid interaction layer for the
// redesigned Appendix VII-A page. Mirrors appendix-vi-drawer.js's structure exactly (see
// appendix-via-drawer.js's header comment for the original reasoning behind each piece: drawer
// slide-in, columns picker, row action menu, delete dialog, toast, search/status filter +
// pagination, Export dropdown).
//
// This file owns ONLY the drawer/grid chrome (see ubis2_appendix_redesign_reuse_shared_files
// memory). The Scheme Name -> Actuals previous-year auto-load
// (loadAppendixVIIAPreviousYearReference) and the Edit-populate field mapping are handled by
// PreBudget-control-binding.js's own (document-scoped) dispatch for
// data-action="edit-appendix-viia-record" / NewRecord_SchemeName's change event - not duplicated
// here.
//
// VII-A has NO single-record rule - Add stays available whenever the appendix isn't locked AND the
// role has entry permission (see AppendixVIIA.cshtml's header, gated on canEnter), since VII-A's
// duplicate check keys on the Major Head + Scheme Name combination, not one-per-Demand (see
// ubis2_appendix_redesign_business_rules_checklist memory). Row action menu = Edit / Delete only -
// no Freeze, no Duplicate (client instruction 2026-09-10).
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("viiaToast");
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
        $("viiaToastTitle").textContent = variant.title;
        $("viiaToastIcon").setAttribute("data-lucide", variant.icon);
        $("viiaToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("viiaToast");
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
        var menu = $("viiaExportMenu");
        var button = $("viiaExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("viiaExportMenu");
        var button = $("viiaExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixVIIA(format, triggerButton) {
        var form = $("appendixVIIAForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixVIIA?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

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
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixVIIA." + format);
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
        var menu = $("viiaColumnsMenu");
        var button = $("viiaColumnsButton");
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
        var menu = $("viiaColumnsMenu");
        var button = $("viiaColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("viiaColumnsMenu");
        if (!menu) {
            return;
        }
        Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (checkbox) {
            var column = checkbox.getAttribute("data-column-toggle");
            var show = checkbox.checked;
            document.querySelectorAll('#viiaChargesTable [data-column="' + column + '"]').forEach(function (cell) {
                cell.classList.toggle("column-hidden", !show);
            });
        });
    }

    function resetColumns() {
        var menu = $("viiaColumnsMenu");
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
        var menu = $("viia-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("viia-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("viia-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "viiaGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit) - shell only.
    // ----------------------------------------------------------------------------------------
    function openAppendixVIIADrawer(mode) {
        var drawer = $("viiaDrawer");
        var backdrop = $("viiaDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixVIIAForm");

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
        }

        var titleEl = $("viiaDrawerTitle");
        var subtitleEl = $("viiaDrawerSubtitle");
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
            var firstField = $("NewRecord_MajorHeadId");
            if (firstField && !firstField.readOnly && !firstField.disabled) {
                firstField.focus();
            }
        }, 320);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function reparentViiaDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="viiaDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="viiaDrawerBackdrop"]');
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

    function closeAppendixVIIADrawer() {
        var drawer = $("viiaDrawer");
        var backdrop = $("viiaDrawerBackdrop");
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
        var dialog = $("viiaDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("viiaDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("viiaCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("viiaDeleteDialog");
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
        var body = $("viiaGridBody");
        return body ? Array.prototype.slice.call(body.querySelectorAll("tr[data-row]")) : [];
    }

    var viiaCurrentPage = 1;
    var viiaPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("viiaSearchInput");
        var statusEl = $("viiaStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / viiaPageSize));
        if (viiaCurrentPage > totalPages) {
            viiaCurrentPage = totalPages;
        }
        var start = (viiaCurrentPage - 1) * viiaPageSize;
        var pageRowSet = matched.slice(start, start + viiaPageSize);

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

        var empty = $("viiaEmptyState");
        var gridWrap = $("viiaGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("viiaTotalText");
        var rangeText = $("viiaRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixVIIAPagination(totalPages);
    }

    function renderAppendixVIIAPagination(totalPages) {
        var holder = $("viiaPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - viiaCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-viia-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === viiaCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("viiaPrevBtn");
        var nextBtn = $("viiaNextBtn");
        if (prevBtn) {
            prevBtn.disabled = viiaCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", viiaCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = viiaCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", viiaCurrentPage === totalPages);
        }
    }

    function goToAppendixVIIAPage(page) {
        viiaCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixVIIAPage(delta) {
        goToAppendixVIIAPage(viiaCurrentPage + delta);
    }

    function changeAppendixVIIAPageSize() {
        var sizeEl = $("viiaPageSize");
        viiaPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        viiaCurrentPage = 1;
        applyGridFilters();
    }

    var sortKey = null;
    var sortDir = "asc";

    function sortGridBy(key) {
        var body = $("viiaGridBody");
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

    function clearAppendixVIIAFilters() {
        var searchEl = $("viiaSearchInput");
        var statusEl = $("viiaStatusFilter");
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

        // Client requirement 2026-09-14: "major head value should not reset, it should retain its
        // value on reset button click" - Major Head is locked via lockSelectForEdit during Edit
        // (PreBudget-control-binding.js's edit-appendix-viia-record dispatch), but a native
        // <button type="reset"> restores every control's page-load initial value regardless
        // (blanking it while leaving it disabled), and PreBudget-control-binding.js's own generic
        // reset listener (any form inside .ubis-modern-appendix) unconditionally hard-clears the
        // form on top of that - racing either with a "reset" event listener proved unreliable (see
        // Appendix III-B/VI-B/VI-D/VI-E/VI-F/VI-G's own comments for the confirmed race).
        // Intercept the button CLICK instead (before any "reset" event can fire) and, while still
        // in Edit mode, prevent the native reset entirely and blank only the plain editable fields
        // ourselves - Major Head and the record id are left completely untouched.
        var resetViiaBtn = event.target.closest('#appendixVIIAForm button[type="reset"]');
        if (resetViiaBtn) {
            var idFieldForReset = $("NewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                ["NewRecord_SchemeName", "NewRecord_Actuals", "NewRecord_BE", "NewRecord_RE", "NewRecord_NBE"].forEach(function (id) {
                    var field = $(id);
                    if (field) { field.value = ""; }
                });
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-viia-add-drawer"]')) {
            openAppendixVIIADrawer("add");
            return;
        }

        var editVIIAButton = event.target.closest('[data-action="edit-appendix-viia-record"]');
        if (editVIIAButton) {
            openAppendixVIIADrawer("edit");
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-viia-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var viiaCancelForm = $("appendixVIIAForm");
            var viiaPcb = window.PreBudgetControlBinding;
            var viiaIsDirty = !viiaPcb || typeof viiaPcb.isFormDirtySinceOpen !== "function" || viiaPcb.isFormDirtySinceOpen(viiaCancelForm);
            if (!viiaIsDirty) {
                closeAppendixVIIADrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixVIIADrawer();
                }, false, "No");
            } else {
                closeAppendixVIIADrawer();
            }
            return;
        }

        var rowMenuBtn = event.target.closest(".row-menu-btn[data-action-id]");
        if (rowMenuBtn && rowMenuBtn.closest("#viiaChargesTable")) {
            toggleActionMenu(rowMenuBtn.getAttribute("data-action-id"), rowMenuBtn, event);
            return;
        }
        if (!event.target.closest(".row-menu-btn") && !event.target.closest(".action-menu")) {
            document.querySelectorAll(".action-menu").forEach(function (m) { m.classList.add("hidden"); });
        }

        if (event.target.closest("#viiaColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#viiaColumnsButton") && !event.target.closest("#viiaColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-viia-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-viia"]');
        if (exportOptionBtn) {
            exportAppendixVIIA(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#viiaExportButton") && !event.target.closest("#viiaExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-viia-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-viia-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#viiaCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#viiaConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-viia-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-viia-filters"]')) {
            clearAppendixVIIAFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-viia-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-viia-prev-page"]')) {
            changeAppendixVIIAPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-viia-next-page"]')) {
            changeAppendixVIIAPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-viia-goto-page"]');
        if (pageBtn) {
            goToAppendixVIIAPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#viiaChargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#viiaToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#viiaColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "viiaStatusFilter") {
            viiaCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "viiaPageSize") {
            changeAppendixVIIAPageSize();
        }
    });

    // Native <button type="reset"> (and the Add drawer's own exitAppendixEditMode->form.reset())
    // wipes the previous-year auto-loaded Actuals without re-fetching it - re-trigger the Scheme
    // Name change dispatch that PreBudget-control-binding.js's loadAppendixVIIAPreviousYearReference
    // listens on. (Mostly a no-op guard right after a reset since Scheme Name is blank then; the
    // real re-fetch happens once the user re-types a Scheme Name.)
    document.addEventListener("reset", function (event) {
        if (event.target && event.target.id === "appendixVIIAForm") {
            window.setTimeout(function () {
                var schemeField = $("NewRecord_SchemeName");
                if (schemeField && schemeField.value) {
                    schemeField.dispatchEvent(new Event("change", { bubbles: true }));
                }
            }, 0);
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "viiaSearchInput") {
            viiaCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("viiaExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("viiaColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("viiaDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("viiaDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixVIIADrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__viiaHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("viiaGridBody") && $("appendixVIIAForm")) {
                viiaCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentViiaDrawerToBody();
        };
        window.PreBudgetControlBinding.__viiaHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("viiaGridBody") && $("appendixVIIAForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentViiaDrawerToBody();
})();
