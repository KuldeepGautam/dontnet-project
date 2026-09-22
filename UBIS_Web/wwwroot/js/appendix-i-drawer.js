// "Approved design for all appendixes" (2026-09-09, UBIS-Pre-budget-html/Appendix-I.html) - drawer/
// grid interaction layer for the redesigned Appendix I page. Mirrors appendix-vig-drawer.js's own
// structure (see that file's header comment, and appendix-via-drawer.js's for the original
// reasoning behind each piece: drawer slide-in, columns picker, row action menu, delete dialog,
// toast, search/status filter + pagination, Export dropdown).
//
// Simpler than most: no cascading selects, and only ONE Financial Year (Model.ActualYear, session
// FinancialYear - 2) is ever addable/editable per the pre-existing business rule (client review
// 2026-08-04/08-06) - the Financial Year field itself is a fixed, server-rendered hidden input, not
// user-editable, so Add/Edit never need to touch it. No shared PreBudget-control-binding.js
// function to lean on for Edit-populate (this appendix's old inline-5-year-matrix form never had
// one), so this file owns that itself.
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("iToast");
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
        $("iToastTitle").textContent = variant.title;
        $("iToastIcon").setAttribute("data-lucide", variant.icon);
        $("iToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("iToast");
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
        var menu = $("iExportMenu");
        var button = $("iExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("iExportMenu");
        var button = $("iExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixI(format, triggerButton) {
        var form = $("appendixIForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixI?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

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
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixI." + format);
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
        var menu = $("iColumnsMenu");
        var button = $("iColumnsButton");
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
        var menu = $("iColumnsMenu");
        var button = $("iColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("iColumnsMenu");
        if (!menu) {
            return;
        }
        Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (checkbox) {
            var column = checkbox.getAttribute("data-column-toggle");
            var show = checkbox.checked;
            document.querySelectorAll('#chargesTable [data-column="' + column + '"]').forEach(function (cell) {
                cell.classList.toggle("column-hidden", !show);
            });
        });
    }

    function resetColumns() {
        var menu = $("iColumnsMenu");
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
        var menu = $("i-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("i-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("i-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "iGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit)
    // ----------------------------------------------------------------------------------------
    var I_EDIT_FIELD_MAP = [
        ["NewRecord_RevenueBE", "data-revenue-be"],
        ["NewRecord_RevenueRE", "data-revenue-re"],
        ["NewRecord_RevenueActuals", "data-revenue-actuals"],
        ["NewRecord_RevenueActualsUptoSept", "data-revenue-actuals-upto-sept"],
        ["NewRecord_CapitalBE", "data-capital-be"],
        ["NewRecord_CapitalRE", "data-capital-re"],
        ["NewRecord_CapitalActuals", "data-capital-actuals"],
        ["NewRecord_CapitalActualsUptoSept", "data-capital-actuals-upto-sept"]
    ];

    function populateAppendixIFieldsFromButton(editButton) {
        var idField = $("NewRecord_Id");
        if (idField) {
            idField.value = editButton.getAttribute("data-id") || "";
        }

        I_EDIT_FIELD_MAP.forEach(function (pair) {
            var input = $(pair[0]);
            if (input) {
                input.value = editButton.getAttribute(pair[1]) || "";
            }
        });

        var form = $("appendixIForm");
        if (form && window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.enterAppendixEditMode === "function") {
            window.PreBudgetControlBinding.enterAppendixEditMode(form);
        } else if (form) {
            var submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn) {
                if (!submitBtn.hasAttribute("data-original-label")) {
                    submitBtn.setAttribute("data-original-label", submitBtn.innerHTML);
                }
                submitBtn.innerHTML = submitBtn.getAttribute("data-original-label").replace(/Save Record|Submit/i, "Modify");
            }
        }
    }

    function openAppendixIDrawer(mode, editButton) {
        var drawer = $("iDrawer");
        var backdrop = $("iDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixIForm");

        if (mode === "edit" && editButton) {
            populateAppendixIFieldsFromButton(editButton);
        }

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
            var idField = $("NewRecord_Id");
            if (idField) {
                idField.value = "";
            }
            // Bug fix 2026-09-17 (client report, screenshot: "Financial Year" box empty on Add) -
            // the shared hardClearAppendixForm (called via exitAppendixEditMode above) blanks
            // every non-hidden <input> in the form, including this readonly Financial Year
            // display box - it has no name/id of its own and posts nothing, so it isn't actually
            // form data, just a label showing Model.ActualYear (session FY - 2). Re-sync it from
            // the hidden NewRecord.FinancialYear field, which hardClearAppendixForm's own "keep
            // hidden Id-pattern fields" guard already leaves untouched.
            var actualYearDisplay = $("iActualYearDisplay");
            var financialYearField = $("NewRecord_FinancialYear");
            if (actualYearDisplay && financialYearField) {
                actualYearDisplay.value = financialYearField.value;
            }
        }

        var titleEl = $("iDrawerTitle");
        var subtitleEl = $("iDrawerSubtitle");
        if (titleEl) {
            titleEl.textContent = mode === "edit" ? "Edit Record" : "Add Record";
        }
        if (subtitleEl) {
            subtitleEl.textContent = mode === "edit"
                ? "Update this year's budget and expenditure trend"
                : "Enter this year's budget and expenditure trend";
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
            var firstField = $("NewRecord_RevenueBE");
            if (firstField && !firstField.disabled) {
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
    function reparentIDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="iDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="iDrawerBackdrop"]');
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

    function closeAppendixIDrawer() {
        var drawer = $("iDrawer");
        var backdrop = $("iDrawerBackdrop");
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
        var dialog = $("iDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("iDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("iCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("iDeleteDialog");
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

    var iCurrentPage = 1;
    var iPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("iSearchInput");
        var statusEl = $("iStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / iPageSize));
        if (iCurrentPage > totalPages) {
            iCurrentPage = totalPages;
        }
        var start = (iCurrentPage - 1) * iPageSize;
        var pageRowSet = matched.slice(start, start + iPageSize);

        matched.forEach(function (row) {
            row.classList.add("hidden");
        });
        pageRowSet.forEach(function (row) {
            row.classList.remove("hidden");
        });

        var empty = $("iEmptyState");
        var gridWrap = $("iGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("iTotalText");
        var rangeText = $("iRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixIPagination(totalPages);
    }

    function renderAppendixIPagination(totalPages) {
        var holder = $("iPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - iCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-i-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === iCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("iPrevBtn");
        var nextBtn = $("iNextBtn");
        if (prevBtn) {
            prevBtn.disabled = iCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", iCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = iCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", iCurrentPage === totalPages);
        }
    }

    function goToAppendixIPage(page) {
        iCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixIPage(delta) {
        goToAppendixIPage(iCurrentPage + delta);
    }

    function changeAppendixIPageSize() {
        var sizeEl = $("iPageSize");
        iPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        iCurrentPage = 1;
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

    function clearAppendixIFilters() {
        var searchEl = $("iSearchInput");
        var statusEl = $("iStatusFilter");
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

        if (event.target.closest('[data-action="open-appendix-i-add-drawer"]')) {
            openAppendixIDrawer("add");
            return;
        }

        // Bug fix 2026-09-17 (client report: "maintain financial year value during edit and
        // reset during edit mode") - same Reset-vs-generic-hardClear race already fixed for every
        // other redesigned appendix this session. A native <button type="reset"> only restores
        // each control's INITIAL server-rendered value, which for NewRecord_Id is blank - so
        // resetting mid-Edit silently dropped back into "Add" (next Submit would create a
        // duplicate row instead of updating the one being edited), and the shared
        // hardClearAppendixForm (PreBudget-control-binding.js's document "reset" listener) would
        // then also blank the read-only Financial Year display box (see appendix-i-drawer.js's
        // openAppendixIDrawer "add" branch for the matching fix on that field). Intercepting the
        // actual click bypasses both: while editing, only the 8 Revenue/Capital data fields are
        // blanked (dispatching input/change so any live recalcs still fire); the record id and
        // the Financial Year display are left exactly as they were.
        var resetIButton = event.target.closest('#appendixIForm button[type="reset"]');
        if (resetIButton) {
            var iIdField = $("NewRecord_Id");
            if (iIdField && iIdField.value) {
                event.preventDefault();
                I_EDIT_FIELD_MAP.forEach(function (pair) {
                    var input = $(pair[0]);
                    if (!input) {
                        return;
                    }
                    input.value = "";
                    input.dispatchEvent(new Event("input", { bubbles: true }));
                    input.dispatchEvent(new Event("change", { bubbles: true }));
                });
            }
            return;
        }

        var editIButton = event.target.closest('[data-action="edit-appendix-i-row"]');
        if (editIButton) {
            openAppendixIDrawer("edit", editIButton);
            return;
        }

        var cancelIButton = event.target.closest('[data-action="close-appendix-i-drawer"]');
        if (cancelIButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify -
            // Cancel no longer closes the drawer directly. Refined same day: skip the confirm
            // entirely if nothing was actually changed since the drawer opened (no data entered on
            // Add, no edits made on Modify).
            var iCancelForm = $("appendixIForm");
            var iPcb = window.PreBudgetControlBinding;
            var iIsDirty = !iPcb || typeof iPcb.isFormDirtySinceOpen !== "function" || iPcb.isFormDirtySinceOpen(iCancelForm);
            if (!iIsDirty) {
                closeAppendixIDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields (they stay exactly as typed, in case the
                    // user reopens the drawer or this was an accidental Cancel).
                    closeAppendixIDrawer();
                }, false, "No");
            } else {
                closeAppendixIDrawer();
            }
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

        if (event.target.closest("#iColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#iColumnsButton") && !event.target.closest("#iColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-i-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-i"]');
        if (exportOptionBtn) {
            exportAppendixI(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#iExportButton") && !event.target.closest("#iExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-i-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-i-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#iCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#iConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-i-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-i-filters"]')) {
            clearAppendixIFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-i-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-i-prev-page"]')) {
            changeAppendixIPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-i-next-page"]')) {
            changeAppendixIPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-i-goto-page"]');
        if (pageBtn) {
            goToAppendixIPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#chargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#iToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#iColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "iStatusFilter") {
            iCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "iPageSize") {
            changeAppendixIPageSize();
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "iSearchInput") {
            iCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("iExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("iColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("iDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("iDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixIDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__iHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("gridBody") && $("appendixIForm")) {
                iCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentIDrawerToBody();
        };
        window.PreBudgetControlBinding.__iHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("gridBody") && $("appendixIForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentIDrawerToBody();
})();
