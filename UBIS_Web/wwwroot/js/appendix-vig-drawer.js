// New appendix (2026-09-09, client instruction, UBIS-Pre-budget-html/Appendix6g.html) - drawer/grid
// interaction layer for Appendix VI-G. Mirrors appendix-vie-drawer.js's own structure (see that
// file's header comment, and appendix-via-drawer.js's for the original reasoning behind each piece:
// drawer slide-in, columns picker, row action menu, delete dialog, toast, search/status filter +
// pagination, Export dropdown). Add/Edit/Delete/Freeze/Nil all go through the real POST actions
// (SaveAppendixVIG/DeleteAppendixVIG/FreezeAppendixTemplate/SetNilSubmission).
//
// Simpler than VI-E: no previous-year-reference auto-fill on Autonomous Body change (not part of
// this appendix's own fields), and no shared PreBudget-control-binding.js function to lean on for
// Edit-populate (brand-new appendix) - this file owns that itself, same
// enterAppendixEditMode/exitAppendixEditMode reuse VI-E's own populate function already uses.
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("vigToast");
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
        $("vigToastTitle").textContent = variant.title;
        $("vigToastIcon").setAttribute("data-lucide", variant.icon);
        $("vigToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("vigToast");
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
        var menu = $("vigExportMenu");
        var button = $("vigExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("vigExportMenu");
        var button = $("vigExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixVig(format, triggerButton) {
        var form = $("appendixVIGForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixVIG?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

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
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixVIG." + format);
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
        var menu = $("vigColumnsMenu");
        var button = $("vigColumnsButton");
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
        var menu = $("vigColumnsMenu");
        var button = $("vigColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("vigColumnsMenu");
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
        var menu = $("vigColumnsMenu");
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
        var menu = $("vig-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("vig-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("vig-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "vigGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit)
    // ----------------------------------------------------------------------------------------
    var VIG_EDIT_FIELD_MAP = [
        ["NewRecord_AutonomousBodyId", "data-autonomous-body-id"],
        ["NewRecord_BriefOnRevenueSources", "data-brief-on-revenue-sources"],
        ["NewRecord_PresentStatus", "data-present-status"],
        ["NewRecord_ReceiptsCollected", "data-receipts-collected"],
        ["NewRecord_TotalRevenueExpenditure", "data-total-revenue-expenditure"],
        ["NewRecord_TotalCapitalExpenditure", "data-total-capital-expenditure"]
    ];

    function populateAppendixVigFieldsFromButton(editButton) {
        var idField = $("NewRecord_Id");
        if (idField) {
            idField.value = editButton.getAttribute("data-id") || "";
        }

        VIG_EDIT_FIELD_MAP.forEach(function (pair) {
            var input = $(pair[0]);
            if (input) {
                input.value = editButton.getAttribute(pair[1]) || "";
            }
        });

        var form = $("appendixVIGForm");
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

    function openAppendixVigDrawer(mode, editButton) {
        var drawer = $("vigDrawer");
        var backdrop = $("vigDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixVIGForm");

        // Bug fix 2026-09-14 (client report: dropdown correctly shown disabled with its selected
        // value on Edit, but clicking Modify still asked to "Select Autonomous Body"): a plain
        // `select.disabled = true` both drops the field from FormData on submit and trips the
        // shared blank-cascade-select submit guard in PreBudget-validation.js (it only excludes
        // selects carrying the appendix-locked-for-edit class) - same root cause already fixed for
        // VI-E. Use the shared lockSelectForEdit/unlockSelectsAfterEdit helpers instead, which add
        // that class and a hidden mirror input so the value still posts.
        if (form && window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.unlockSelectsAfterEdit === "function") {
            window.PreBudgetControlBinding.unlockSelectsAfterEdit(form);
        } else {
            var ddlReset = document.getElementById("NewRecord_AutonomousBodyId");
            if (ddlReset) {
                ddlReset.disabled = false;
            }
        }
        if (mode === "edit" && editButton) {
            populateAppendixVigFieldsFromButton(editButton);
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.lockSelectForEdit === "function") {
                window.PreBudgetControlBinding.lockSelectForEdit("NewRecord_AutonomousBodyId");
            } else {
                var ddlLock = document.getElementById("NewRecord_AutonomousBodyId");
                if (ddlLock) {
                    ddlLock.disabled = true;
                }
            }
        }


        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
        }

        var titleEl = $("vigDrawerTitle");
        var subtitleEl = $("vigDrawerSubtitle");
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
            var bodyField = $("NewRecord_AutonomousBodyId");
            if (bodyField && !bodyField.disabled) {
                bodyField.focus();
            }
        }, 320);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    // Bug fix 2026-09-14 (client report: "in edit mode, reset button should not reset the
    // disabled dropdown"): a native <button type="reset"> restores every control to its page-load
    // initial value, including the locked Autonomous Body select - which would revert it to blank
    // while leaving it disabled (same class of bug already fixed for VI-D's own Reset). Preserve
    // and re-lock the selection (and the record id) when Reset is pressed while still in Edit
    // mode; the other fields reset to blank normally via the native behavior.
    document.addEventListener("reset", function (event) {
        if (!event.target || event.target.id !== "appendixVIGForm") {
            return;
        }
        var idField = $("NewRecord_Id");
        var wasEditMode = !!(idField && idField.value);
        if (!wasEditMode) {
            return;
        }
        var preservedId = idField.value;
        var bodySelect = $("NewRecord_AutonomousBodyId");
        var preservedBodyId = bodySelect ? bodySelect.value : "";

        window.setTimeout(function () {
            if (idField) {
                idField.value = preservedId;
            }
            if (bodySelect) {
                bodySelect.value = preservedBodyId;
            }
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.lockSelectForEdit === "function") {
                window.PreBudgetControlBinding.lockSelectForEdit("NewRecord_AutonomousBodyId");
            } else if (bodySelect) {
                bodySelect.disabled = true;
            }
        }, 0);
    });

    // Same fix as every other redesigned appendix's own drawer reparenting - see
    // appendix-via-drawer.js's comment for the full reasoning (drawer painted behind the app
    // shell's masthead otherwise).
    function reparentVigDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="vigDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="vigDrawerBackdrop"]');
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

    function closeAppendixVigDrawer() {
        var drawer = $("vigDrawer");
        var backdrop = $("vigDrawerBackdrop");
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
        var dialog = $("vigDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("vigDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("vigCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("vigDeleteDialog");
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

    var vigCurrentPage = 1;
    var vigPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("vigSearchInput");
        var statusEl = $("vigStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / vigPageSize));
        if (vigCurrentPage > totalPages) {
            vigCurrentPage = totalPages;
        }
        var start = (vigCurrentPage - 1) * vigPageSize;
        var pageRowSet = matched.slice(start, start + vigPageSize);

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

        var empty = $("vigEmptyState");
        var gridWrap = $("vigGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("vigTotalText");
        var rangeText = $("vigRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixVigPagination(totalPages);
    }

    function renderAppendixVigPagination(totalPages) {
        var holder = $("vigPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - vigCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-vig-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === vigCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("vigPrevBtn");
        var nextBtn = $("vigNextBtn");
        if (prevBtn) {
            prevBtn.disabled = vigCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", vigCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = vigCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", vigCurrentPage === totalPages);
        }
    }

    function goToAppendixVigPage(page) {
        vigCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixVigPage(delta) {
        goToAppendixVigPage(vigCurrentPage + delta);
    }

    function changeAppendixVigPageSize() {
        var sizeEl = $("vigPageSize");
        vigPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        vigCurrentPage = 1;
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

    function clearAppendixVigFilters() {
        var searchEl = $("vigSearchInput");
        var statusEl = $("vigStatusFilter");
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

        // Bug fix 2026-09-14: the "reset" event handler below raced
        // PreBudget-control-binding.js's own generic reset listener (any form inside
        // .ubis-modern-appendix unconditionally hard-clears via its own setTimeout(0)) and proved
        // unreliable - same class of bug confirmed live on Appendix VI-D, fixed there and on
        // III-B/VI-B by intercepting the button CLICK instead (before any "reset" event can fire).
        // While still in Edit mode, prevent the native reset entirely and blank only the plain
        // editable fields ourselves - the Autonomous Body select and the record id are left
        // completely untouched.
        var resetVigBtn = event.target.closest('#appendixVIGForm button[type="reset"]');
        if (resetVigBtn) {
            var idFieldForReset = $("NewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                ["NewRecord_BriefOnRevenueSources", "NewRecord_PresentStatus", "NewRecord_ReceiptsCollected", "NewRecord_TotalRevenueExpenditure", "NewRecord_TotalCapitalExpenditure"].forEach(function (id) {
                    var field = $(id);
                    if (field) { field.value = ""; }
                });
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-vig-add-drawer"]')) {
            openAppendixVigDrawer("add");
            return;
        }

        var editVigButton = event.target.closest('[data-action="edit-appendix-vig-record"]');
        if (editVigButton) {
            openAppendixVigDrawer("edit", editVigButton);
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-vig-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var vigCancelForm = $("appendixVIGForm");
            var vigPcb = window.PreBudgetControlBinding;
            var vigIsDirty = !vigPcb || typeof vigPcb.isFormDirtySinceOpen !== "function" || vigPcb.isFormDirtySinceOpen(vigCancelForm);
            if (!vigIsDirty) {
                closeAppendixVigDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixVigDrawer();
                }, false, "No");
            } else {
                closeAppendixVigDrawer();
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

        if (event.target.closest("#vigColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#vigColumnsButton") && !event.target.closest("#vigColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-vig-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-vig"]');
        if (exportOptionBtn) {
            exportAppendixVig(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#vigExportButton") && !event.target.closest("#vigExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-vig-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-vig-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#vigCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#vigConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-vig-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-vig-filters"]')) {
            clearAppendixVigFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-vig-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-vig-prev-page"]')) {
            changeAppendixVigPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-vig-next-page"]')) {
            changeAppendixVigPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-vig-goto-page"]');
        if (pageBtn) {
            goToAppendixVigPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#chargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#vigToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#vigColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "vigStatusFilter") {
            vigCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "vigPageSize") {
            changeAppendixVigPageSize();
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "vigSearchInput") {
            vigCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("vigExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("vigColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("vigDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("vigDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixVigDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__vigHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("gridBody") && $("appendixVIGForm")) {
                vigCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentVigDrawerToBody();
        };
        window.PreBudgetControlBinding.__vigHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("gridBody") && $("appendixVIGForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentVigDrawerToBody();
})();
