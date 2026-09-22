// "Approved design for all appendixes" (2026-09-09) - drawer/grid interaction layer for the
// redesigned Appendix III-A page. Mirrors appendix-iii-drawer.js's own structure (see that file's
// header comment, and appendix-via-drawer.js's for the original reasoning behind each piece:
// drawer slide-in, columns picker, row action menu, delete dialog, toast, search/status filter +
// pagination, Export dropdown).
//
// Smaller than VI-A..E's own drawer files by design (see ubis2_appendix_redesign_reuse_shared_files
// memory - "most of the design must come from reusing shared css/js, not new bespoke code"): this
// file owns ONLY the new drawer/grid chrome. The Entity Name field, the BE auto-load AJAX call, the
// Edit-populate field mapping, and the Unspent Assignment live recalc are NOT duplicated here -
// they're still handled by PreBudget-control-binding.js's existing
// loadAppendixIIIABePreviousYear/populateSimpleEntryForm's III-A branch/recalcAppendixIIIAUnspentAssignment,
// all of them already document-safe (getElementById-based) so they keep working once this drawer
// reparents to document.body. This file only needs to open/close the drawer shell itself and reset
// it to Add mode.
//
// Like Appendix III, III-A has NO single-record rule - Add stays available whenever the appendix
// isn't locked (see AppendixIIIA.cshtml's header, gated only on !locked), since III-A's duplicate
// check keys on free-text Entity Name, not one-per-Demand (see
// ubis2_appendix_redesign_business_rules_checklist memory).
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("iiiaToast");
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
        $("iiiaToastTitle").textContent = variant.title;
        $("iiiaToastIcon").setAttribute("data-lucide", variant.icon);
        $("iiiaToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("iiiaToast");
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
        var menu = $("iiiaExportMenu");
        var button = $("iiiaExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("iiiaExportMenu");
        var button = $("iiiaExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixIIIA(format, triggerButton) {
        var form = $("appendixIIIAForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixIIIA?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

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
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixIIIA." + format);
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
        var menu = $("iiiaColumnsMenu");
        var button = $("iiiaColumnsButton");
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
        var menu = $("iiiaColumnsMenu");
        var button = $("iiiaColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("iiiaColumnsMenu");
        if (!menu) {
            return;
        }
        Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (checkbox) {
            var column = checkbox.getAttribute("data-column-toggle");
            var show = checkbox.checked;
            document.querySelectorAll('#iiiaChargesTable [data-column="' + column + '"]').forEach(function (cell) {
                cell.classList.toggle("column-hidden", !show);
            });
        });
    }

    function resetColumns() {
        var menu = $("iiiaColumnsMenu");
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
        var menu = $("iiia-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("iiia-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("iiia-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "iiiaGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit) - shell only. The Entity Name field, BE auto-load, field population on
    // Edit, and the Unspent Assignment recalc are all handled by the existing
    // PreBudget-control-binding.js logic (see file header).
    // ----------------------------------------------------------------------------------------
    function openAppendixIIIADrawer(mode) {
        var drawer = $("iiiaDrawer");
        var backdrop = $("iiiaDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixIIIAForm");

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
            var unspentDisplay = $("iiiaUnspentAssignment");
            if (unspentDisplay) {
                unspentDisplay.value = "";
            }
        }

        // Client 2026-09-10: "Name of the Entity" is the record's identity (the duplicate guard
        // keys on Demand + FY + EntityName) - lock it in Edit mode so it can't be changed mid-
        // edit, the same intent as Appendix III's edit-locked Balance Type / Scheme / Sub-Scheme.
        // readOnly (not disabled) so its value still posts with the update.
        var entityNameInput = $("NewRecord_EntityName");
        if (entityNameInput) {
            var lockEntity = mode === "edit";
            entityNameInput.readOnly = lockEntity;
            entityNameInput.classList.toggle("bg-slate-100", lockEntity);
            entityNameInput.classList.toggle("cursor-not-allowed", lockEntity);
        }

        var titleEl = $("iiiaDrawerTitle");
        var subtitleEl = $("iiiaDrawerSubtitle");
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
            var firstField = $("NewRecord_EntityName");
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
    function reparentIiiaDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="iiiaDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="iiiaDrawerBackdrop"]');
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

    function closeAppendixIIIADrawer() {
        var drawer = $("iiiaDrawer");
        var backdrop = $("iiiaDrawerBackdrop");
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
        var dialog = $("iiiaDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("iiiaDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("iiiaCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("iiiaDeleteDialog");
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
        var body = $("iiiaGridBody");
        return body ? Array.prototype.slice.call(body.querySelectorAll("tr[data-row]")) : [];
    }

    var iiiaCurrentPage = 1;
    var iiiaPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("iiiaSearchInput");
        var statusEl = $("iiiaStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / iiiaPageSize));
        if (iiiaCurrentPage > totalPages) {
            iiiaCurrentPage = totalPages;
        }
        var start = (iiiaCurrentPage - 1) * iiiaPageSize;
        var pageRowSet = matched.slice(start, start + iiiaPageSize);

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

        var empty = $("iiiaEmptyState");
        var gridWrap = $("iiiaGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("iiiaTotalText");
        var rangeText = $("iiiaRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixIIIAPagination(totalPages);
    }

    function renderAppendixIIIAPagination(totalPages) {
        var holder = $("iiiaPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - iiiaCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-iiia-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === iiiaCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("iiiaPrevBtn");
        var nextBtn = $("iiiaNextBtn");
        if (prevBtn) {
            prevBtn.disabled = iiiaCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", iiiaCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = iiiaCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", iiiaCurrentPage === totalPages);
        }
    }

    function goToAppendixIIIAPage(page) {
        iiiaCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixIIIAPage(delta) {
        goToAppendixIIIAPage(iiiaCurrentPage + delta);
    }

    function changeAppendixIIIAPageSize() {
        var sizeEl = $("iiiaPageSize");
        iiiaPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        iiiaCurrentPage = 1;
        applyGridFilters();
    }

    var sortKey = null;
    var sortDir = "asc";

    function sortGridBy(key) {
        var body = $("iiiaGridBody");
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

    function clearAppendixIIIAFilters() {
        var searchEl = $("iiiaSearchInput");
        var statusEl = $("iiiaStatusFilter");
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

        if (event.target.closest('[data-action="open-appendix-iiia-add-drawer"]')) {
            openAppendixIIIADrawer("add");
            return;
        }

        var editIIIAButton = event.target.closest('[data-action="edit-appendix-iiia-record"]');
        if (editIIIAButton) {
            // Field population + the BE auto-load + Unspent Assignment recalc is handled by
            // PreBudget-control-binding.js's own (document-scoped, unchanged) dispatch for this
            // same data-action - this just slides the drawer open; the two listeners run
            // independently and don't conflict.
            openAppendixIIIADrawer("edit");
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-iiia-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var iiiaCancelForm = $("appendixIIIAForm");
            var iiiaPcb = window.PreBudgetControlBinding;
            var iiiaIsDirty = !iiiaPcb || typeof iiiaPcb.isFormDirtySinceOpen !== "function" || iiiaPcb.isFormDirtySinceOpen(iiiaCancelForm);
            if (!iiiaIsDirty) {
                closeAppendixIIIADrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixIIIADrawer();
                }, false, "No");
            } else {
                closeAppendixIIIADrawer();
            }
            return;
        }

        // Reset button - mode-aware (client 2026-09-10):
        //  * Edit mode -> stay in Edit mode. Keep NewRecord.Id and the "Modify" button, keep the
        //    locked Entity Name and every read-only / auto-derived field (BE auto-load, Unspent
        //    Assignment); blank only the user-editable inputs so they can be re-entered for the
        //    same record.
        //  * Add mode  -> full clear: exitAppendixEditMode (blank all, drop the id / Modify
        //    label), unlock Entity Name, clear BE and the Unspent display.
        // event.preventDefault() so the generic reset listeners don't also fire.
        if (event.target.closest("#appendixIIIAReset")) {
            event.preventDefault();
            var iiiaForm = $("appendixIIIAForm");
            if (!iiiaForm) {
                return;
            }
            var iiiaIdField = $("NewRecord_Id");
            var iiiaInEditMode = !!(iiiaIdField && iiiaIdField.value);

            if (iiiaInEditMode) {
                Array.prototype.forEach.call(
                    iiiaForm.querySelectorAll('input:not([type="hidden"]):not([type="submit"]):not([type="reset"]):not([type="button"]), textarea'),
                    function (el) {
                        if (el.disabled || el.readOnly) {
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
                var iiiaUnspentEdit = $("iiiaUnspentAssignment");
                if (iiiaUnspentEdit) {
                    iiiaUnspentEdit.value = "";
                }
                return;
            }

            var pcb = window.PreBudgetControlBinding;
            if (pcb && typeof pcb.exitAppendixEditMode === "function") {
                pcb.exitAppendixEditMode(iiiaForm);
            } else {
                iiiaForm.reset();
            }
            var iiiaEntity = $("NewRecord_EntityName");
            if (iiiaEntity) {
                iiiaEntity.readOnly = false;
                iiiaEntity.classList.remove("bg-slate-100", "cursor-not-allowed");
            }
            var iiiaBe = $("NewRecord_BE");
            if (iiiaBe) {
                iiiaBe.value = "";
            }
            var iiiaUnspent = $("iiiaUnspentAssignment");
            if (iiiaUnspent) {
                iiiaUnspent.value = "";
            }
            return;
        }

        var rowMenuBtn = event.target.closest(".row-menu-btn[data-action-id]");
        if (rowMenuBtn && rowMenuBtn.closest("#iiiaChargesTable")) {
            toggleActionMenu(rowMenuBtn.getAttribute("data-action-id"), rowMenuBtn, event);
            return;
        }
        if (!event.target.closest(".row-menu-btn") && !event.target.closest(".action-menu")) {
            document.querySelectorAll(".action-menu").forEach(function (m) { m.classList.add("hidden"); });
        }

        if (event.target.closest("#iiiaColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#iiiaColumnsButton") && !event.target.closest("#iiiaColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-iiia-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-iiia"]');
        if (exportOptionBtn) {
            exportAppendixIIIA(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#iiiaExportButton") && !event.target.closest("#iiiaExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-iiia-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-iiia-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#iiiaCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#iiiaConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-iiia-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-iiia-filters"]')) {
            clearAppendixIIIAFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-iiia-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-iiia-prev-page"]')) {
            changeAppendixIIIAPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-iiia-next-page"]')) {
            changeAppendixIIIAPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-iiia-goto-page"]');
        if (pageBtn) {
            goToAppendixIIIAPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#iiiaChargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#iiiaToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#iiiaColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "iiiaStatusFilter") {
            iiiaCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "iiiaPageSize") {
            changeAppendixIIIAPageSize();
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "iiiaSearchInput") {
            iiiaCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("iiiaExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("iiiaColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("iiiaDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("iiiaDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixIIIADrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__iiiaHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("iiiaGridBody") && $("appendixIIIAForm")) {
                iiiaCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentIiiaDrawerToBody();
        };
        window.PreBudgetControlBinding.__iiiaHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("iiiaGridBody") && $("appendixIIIAForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentIiiaDrawerToBody();
})();
