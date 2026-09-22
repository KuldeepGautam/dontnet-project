// New appendix, 2026-09-09 (client instruction, Appendix_Papes_I_to IIIB/Appendix-III-B.html) -
// drawer/grid interaction layer for Appendix III-B. Mirrors appendix-vif-drawer.js/
// appendix-iii-drawer.js's own structure (see appendix-via-drawer.js's header comment for the
// original reasoning behind each piece: drawer slide-in, columns picker, row action menu, delete
// dialog, toast, search/status filter + pagination, Export dropdown).
//
// This file owns ONLY the drawer/grid chrome (see ubis2_appendix_redesign_reuse_shared_files
// memory). The Edit-populate field mapping is handled by PreBudget-control-binding.js's own
// (document-scoped) dispatch for data-action="edit-appendix-iiib-record" via populateSimpleEntryForm
// - not duplicated here.
//
// Like VI-F, III-B has NO single-record rule - Add stays available whenever the appendix isn't
// locked (see AppendixIIIB.cshtml's header, gated only on !locked), since III-B's duplicate check
// keys on the Category + Scheme combination, not one-per-Demand (see
// ubis2_appendix_redesign_business_rules_checklist memory).
//
// Category + Scheme are cascading dropdowns as of 2026-09-10 (client instruction): Category is
// server-rendered from dbo.M_Category (current FY, SerialNo II/IV); Scheme cascades from it via
// GetAppendixIIIBSchemes. This file owns that cascade (loadAppendixIIIBSchemes) and the live
// duplicate check (now keyed on CategoryId + SchemeId).
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("iiibToast");
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
        $("iiibToastTitle").textContent = variant.title;
        $("iiibToastIcon").setAttribute("data-lucide", variant.icon);
        $("iiibToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("iiibToast");
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
        var menu = $("iiibExportMenu");
        var button = $("iiibExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("iiibExportMenu");
        var button = $("iiibExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixIIIB(format, triggerButton) {
        var form = $("appendixIIIBForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixIIIB?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

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
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixIIIB." + format);
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
        var menu = $("iiibColumnsMenu");
        var button = $("iiibColumnsButton");
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
        var menu = $("iiibColumnsMenu");
        var button = $("iiibColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("iiibColumnsMenu");
        if (!menu) {
            return;
        }
        Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (checkbox) {
            var column = checkbox.getAttribute("data-column-toggle");
            var show = checkbox.checked;
            document.querySelectorAll('#iiibChargesTable [data-column="' + column + '"]').forEach(function (cell) {
                cell.classList.toggle("column-hidden", !show);
            });
        });
    }

    function resetColumns() {
        var menu = $("iiibColumnsMenu");
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
        var menu = $("iiib-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("iiib-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("iiib-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "iiibGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Category -> Scheme cascade (client instruction 2026-09-10). Category is a real dropdown
    // (dbo.M_Category, current FY, SerialNo II/IV - server-rendered into the <select>); Scheme
    // cascades from it via GetAppendixIIIBSchemes, same reference shape as Appendix III.
    // ----------------------------------------------------------------------------------------
    function resetAppendixIIIBSchemeSelect() {
        var schemeEl = $("NewRecord_IIIBSchemeId");
        if (!schemeEl) {
            return;
        }
        schemeEl.innerHTML = '<option value="">Select a Category first</option>';
        schemeEl.value = "";
        schemeEl.disabled = true;
    }

    function loadAppendixIIIBSchemes(categoryId, selectedSchemeId, lockForEdit) {
        var schemeEl = $("NewRecord_IIIBSchemeId");
        var form = $("appendixIIIBForm");
        if (!schemeEl || !form) {
            return;
        }
        if (!categoryId) {
            resetAppendixIIIBSchemeSelect();
            checkAppendixIIIBDuplicate();
            return;
        }
        var demandId = form.getAttribute("data-demand-id");
        schemeEl.disabled = true;
        schemeEl.innerHTML = '<option value="">Loading...</option>';

        fetch("/PreBudgetMeeting/PreBudgetMeeting/GetAppendixIIIBSchemes?demandId=" + encodeURIComponent(demandId) + "&categoryId=" + encodeURIComponent(categoryId), {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (res) {
                if (!res.ok) {
                    throw new Error("Scheme lookup failed with status " + res.status);
                }
                return res.json();
            })
            .then(function (schemes) {
                var opts = ['<option value="">Select a Scheme</option>'];
                (schemes || []).forEach(function (s) {
                    opts.push('<option value="' + s.schemeId + '">' + String(s.schemeName || "").replace(/</g, "&lt;") + "</option>");
                });
                schemeEl.innerHTML = opts.join("");
                schemeEl.disabled = false;
                if (selectedSchemeId) {
                    schemeEl.value = String(selectedSchemeId);
                }
                if (lockForEdit) {
                    lockAppendixIIIBIdentityFields();
                }
                checkAppendixIIIBDuplicate();
            })
            .catch(function () {
                schemeEl.innerHTML = '<option value="">Could not load Schemes - try again</option>';
                schemeEl.disabled = false;
                if (lockForEdit) {
                    lockAppendixIIIBIdentityFields();
                }
                checkAppendixIIIBDuplicate();
            });
    }

    // Client requirement 2026-09-14: "on edit mode, disable Scheme/Category" - both are part of
    // the record's identity (the duplicate check is keyed on Category+Scheme), same lockSelectForEdit
    // convention as every other appendix's identity fields (VI-B/VI-D's Autonomous Body, etc.).
    // Locking Scheme happens only once its options have actually loaded (see loadAppendixIIIBSchemes's
    // lockForEdit param) - locking it earlier would freeze it on the placeholder before its real
    // value is set.
    function lockAppendixIIIBIdentityFields() {
        if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.lockSelectForEdit === "function") {
            window.PreBudgetControlBinding.lockSelectForEdit("NewRecord_IIIBCategoryId");
            window.PreBudgetControlBinding.lockSelectForEdit("NewRecord_IIIBSchemeId");
        } else {
            var categoryEl = $("NewRecord_IIIBCategoryId");
            var schemeEl = $("NewRecord_IIIBSchemeId");
            if (categoryEl) { categoryEl.disabled = true; }
            if (schemeEl) { schemeEl.disabled = true; }
        }
    }

    // ----------------------------------------------------------------------------------------
    // Live duplicate check (client requirement 2026-09-09, re-keyed on ids 2026-09-10): unique
    // record per Demand + Category + Scheme (server enforces this too - AppendixIIIBController's
    // ExistsMatchingAsync - this is purely a UX layer disabling Save before submit). Matches on
    // the grid rows' data-dup-category-id / data-dup-scheme-id, excluding the row currently being
    // edited (by NewRecord.Id) so re-saving a record's own unchanged Category/Scheme isn't flagged
    // as a duplicate of itself.
    // ----------------------------------------------------------------------------------------
    function checkAppendixIIIBDuplicate() {
        var saveButton = $("iiibSaveButton");
        var categoryEl = $("NewRecord_IIIBCategoryId");
        var schemeEl = $("NewRecord_IIIBSchemeId");
        var idEl = $("NewRecord_Id");
        if (!saveButton || !categoryEl || !schemeEl) {
            return;
        }

        var categoryId = (categoryEl.value || "").trim();
        var schemeId = (schemeEl.value || "").trim();
        var editingId = idEl && idEl.value ? idEl.value : null;

        var isDuplicate = false;
        if (categoryId && schemeId) {
            var rows = currentGridRows();
            isDuplicate = rows.some(function (row) {
                var rowId = row.getAttribute("data-record-id");
                if (editingId && rowId === editingId) {
                    return false;
                }
                return (row.getAttribute("data-dup-category-id") || "") === categoryId &&
                    (row.getAttribute("data-dup-scheme-id") || "") === schemeId;
            });
        }

        saveButton.disabled = isDuplicate;
        saveButton.title = isDuplicate ? "Duplicate entry for Category and Scheme." : "";
    }

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit) - shell only. Field population on Edit is handled by the existing
    // PreBudget-control-binding.js logic (see file header).
    // ----------------------------------------------------------------------------------------
    function openAppendixIIIBDrawer(mode) {
        var drawer = $("iiibDrawer");
        var backdrop = $("iiibDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixIIIBForm");

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
            // form.reset() restores the Category <select> to its blank option but leaves any
            // previously-loaded Scheme options in the DOM - clear them back to the disabled
            // placeholder so a fresh Add starts with no Scheme choices.
            resetAppendixIIIBSchemeSelect();
        }

        var titleEl = $("iiibDrawerTitle");
        var subtitleEl = $("iiibDrawerSubtitle");
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

        // Field population for "edit" (PreBudget-control-binding.js's own click listener) runs
        // before this one - see file header - so the fields already hold the row's values here.
        checkAppendixIIIBDuplicate();

        // Client requirement 2026-09-17: baseline snapshot for the Cancel-confirmation dirty
        // check - captured once the form is fully at its Add-blank/Edit-populated starting point.
        if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.captureCancelSnapshot === "function") {
            window.PreBudgetControlBinding.captureCancelSnapshot(form);
        }

        setTimeout(function () {
            var firstField = $("NewRecord_IIIBCategoryId");
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
    function reparentIiibDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="iiibDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="iiibDrawerBackdrop"]');
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

    function closeAppendixIIIBDrawer() {
        var drawer = $("iiibDrawer");
        var backdrop = $("iiibDrawerBackdrop");
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
        var dialog = $("iiibDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("iiibDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("iiibCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("iiibDeleteDialog");
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
        var body = $("iiibGridBody");
        return body ? Array.prototype.slice.call(body.querySelectorAll("tr[data-row]")) : [];
    }

    var iiibCurrentPage = 1;
    var iiibPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("iiibSearchInput");
        var statusEl = $("iiibStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / iiibPageSize));
        if (iiibCurrentPage > totalPages) {
            iiibCurrentPage = totalPages;
        }
        var start = (iiibCurrentPage - 1) * iiibPageSize;
        var pageRowSet = matched.slice(start, start + iiibPageSize);

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

        var empty = $("iiibEmptyState");
        var gridWrap = $("iiibGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("iiibTotalText");
        var rangeText = $("iiibRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixIIIBPagination(totalPages);
    }

    function renderAppendixIIIBPagination(totalPages) {
        var holder = $("iiibPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - iiibCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-iiib-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === iiibCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("iiibPrevBtn");
        var nextBtn = $("iiibNextBtn");
        if (prevBtn) {
            prevBtn.disabled = iiibCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", iiibCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = iiibCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", iiibCurrentPage === totalPages);
        }
    }

    function goToAppendixIIIBPage(page) {
        iiibCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixIIIBPage(delta) {
        goToAppendixIIIBPage(iiibCurrentPage + delta);
    }

    function changeAppendixIIIBPageSize() {
        var sizeEl = $("iiibPageSize");
        iiibPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        iiibCurrentPage = 1;
        applyGridFilters();
    }

    var sortKey = null;
    var sortDir = "asc";

    function sortGridBy(key) {
        var body = $("iiibGridBody");
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

    function clearAppendixIIIBFilters() {
        var searchEl = $("iiibSearchInput");
        var statusEl = $("iiibStatusFilter");
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

        // Bug fix 2026-09-14 (client report: Reset in Edit mode still blanked AND re-enabled the
        // locked Category/Scheme dropdowns despite the dedicated "reset" event handler below):
        // PreBudget-control-binding.js's own generic reset listener (any form inside
        // .ubis-modern-appendix) unconditionally hard-clears the form via its own setTimeout(0),
        // and that handler's own synthetic re-dispatch of "reset" (part of hardClearAppendixForm)
        // re-triggers this file's reset listener a SECOND time with the record id already blanked,
        // making it think Add mode and re-blank the Scheme select - a race no amount of listener
        // re-ordering fully closes. Simplest robust fix: intercept the actual button CLICK (before
        // any "reset" event is ever dispatched) and, while still in Edit mode, prevent the native
        // reset entirely and blank only the 3 plain editable fields ourselves - Category/Scheme/Id
        // are left completely untouched, so they can't be raced. Add mode is unaffected: no
        // preventDefault, the native reset + existing "reset" event handler run exactly as before.
        var resetBtn = event.target.closest('#appendixIIIBForm button[type="reset"]');
        if (resetBtn) {
            var idFieldForReset = $("NewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                var statusField = $("NewRecord_StatusOfFreshAppraisalApproval");
                var dateField = $("NewRecord_SchemeApprovalValidUpto");
                var remarksField = $("NewRecord_Remarks");
                if (statusField) { statusField.value = ""; }
                if (dateField) { dateField.value = ""; }
                if (remarksField) { remarksField.value = ""; }
                checkAppendixIIIBDuplicate();
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-iiib-add-drawer"]')) {
            openAppendixIIIBDrawer("add");
            return;
        }

        var editIIIBButton = event.target.closest('[data-action="edit-appendix-iiib-record"]');
        if (editIIIBButton) {
            // The 3 text fields + NewRecord_Id + edit-mode are populated by
            // PreBudget-control-binding.js's own (document-scoped) dispatch for this data-action.
            // The Category/Scheme cascade is owned here: set the Category <select> from the row's
            // data-category-id, then load its Schemes and pre-select data-scheme-id.
            var catId = editIIIBButton.getAttribute("data-category-id") || "";
            var schemeId = editIIIBButton.getAttribute("data-scheme-id") || "";
            var categoryEl = $("NewRecord_IIIBCategoryId");
            if (categoryEl) {
                categoryEl.value = catId;
            }
            openAppendixIIIBDrawer("edit");
            loadAppendixIIIBSchemes(catId, schemeId, true);
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-iiib-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var iiibCancelForm = $("appendixIIIBForm");
            var iiibPcb = window.PreBudgetControlBinding;
            var iiibIsDirty = !iiibPcb || typeof iiibPcb.isFormDirtySinceOpen !== "function" || iiibPcb.isFormDirtySinceOpen(iiibCancelForm);
            if (!iiibIsDirty) {
                closeAppendixIIIBDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixIIIBDrawer();
                }, false, "No");
            } else {
                closeAppendixIIIBDrawer();
            }
            return;
        }

        var rowMenuBtn = event.target.closest(".row-menu-btn[data-action-id]");
        if (rowMenuBtn && rowMenuBtn.closest("#iiibChargesTable")) {
            toggleActionMenu(rowMenuBtn.getAttribute("data-action-id"), rowMenuBtn, event);
            return;
        }
        if (!event.target.closest(".row-menu-btn") && !event.target.closest(".action-menu")) {
            document.querySelectorAll(".action-menu").forEach(function (m) { m.classList.add("hidden"); });
        }

        if (event.target.closest("#iiibColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#iiibColumnsButton") && !event.target.closest("#iiibColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-iiib-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-iiib"]');
        if (exportOptionBtn) {
            exportAppendixIIIB(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#iiibExportButton") && !event.target.closest("#iiibExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-iiib-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-iiib-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#iiibCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#iiibConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-iiib-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-iiib-filters"]')) {
            clearAppendixIIIBFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-iiib-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-iiib-prev-page"]')) {
            changeAppendixIIIBPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-iiib-next-page"]')) {
            changeAppendixIIIBPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-iiib-goto-page"]');
        if (pageBtn) {
            goToAppendixIIIBPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#iiibChargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#iiibToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#iiibColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "iiibStatusFilter") {
            iiibCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "iiibPageSize") {
            changeAppendixIIIBPageSize();
        }
        // Category -> Scheme cascade + live duplicate check on both selects.
        if (event.target.id === "NewRecord_IIIBCategoryId" && event.target.closest("#appendixIIIBForm")) {
            loadAppendixIIIBSchemes(event.target.value, null);
        }
        if (event.target.id === "NewRecord_IIIBSchemeId" && event.target.closest("#appendixIIIBForm")) {
            checkAppendixIIIBDuplicate();
        }
    });

    // Native <button type="reset"> restores every control to its page-load initial value - fine
    // for the 3 plain text/date/textarea fields (Add's own blank baseline), but wrong for Category/
    // Scheme when they're locked for Edit: reset would revert them to blank while leaving them
    // disabled (same class of bug fixed for VI-D's Autonomous Body - see that file's own reset
    // handler comment), stranding the drawer on a frozen, empty identity pair with no way to fix
    // it. Client requirement 2026-09-14: while in Edit mode, Reset must PRESERVE the locked
    // Category/Scheme values (and keep them locked) and only blank the actual editable fields -
    // which native reset already does correctly for the 3 non-disabled fields here, so nothing
    // extra is needed for those.
    //
    // Bug fix 2026-09-14 (this same requirement, reported broken live: dropdowns were blanked AND
    // re-enabled by Reset despite this handler existing): PreBudget-control-binding.js's own
    // generic reset listener (any form inside .ubis-modern-appendix) unconditionally hard-clears
    // the form via a setTimeout(0)-deferred hardClearAppendixForm call, which unlocks + blanks
    // Category/Scheme right back. This handler used to be registered on the CAPTURE phase (a
    // trailing `true`), which made it run - and schedule ITS OWN setTimeout(0) restore - before
    // the generic bubble-phase handler even ran, so the generic handler's later-scheduled clear
    // always executed last and silently won. Registering as a normal bubble-phase listener instead
    // (this file loads after PreBudget-control-binding.js, so this handler now fires, and its
    // restore is scheduled, AFTER the generic one - same ordering VI-D/VI-G's own reset fixes
    // already rely on) makes this handler's restore run last and actually stick.
    document.addEventListener("reset", function (event) {
        if (!event.target || event.target.id !== "appendixIIIBForm") {
            return;
        }
        var idField = $("NewRecord_Id");
        var wasEditMode = !!(idField && idField.value);
        var categoryEl = $("NewRecord_IIIBCategoryId");
        var schemeEl = $("NewRecord_IIIBSchemeId");
        var preservedId = wasEditMode ? idField.value : null;
        var preservedCategoryId = wasEditMode && categoryEl ? categoryEl.value : null;
        var preservedSchemeId = wasEditMode && schemeEl ? schemeEl.value : null;
        var preservedSchemeOptionsHtml = wasEditMode && schemeEl ? schemeEl.innerHTML : null;

        setTimeout(function () {
            if (!wasEditMode) {
                resetAppendixIIIBSchemeSelect();
                checkAppendixIIIBDuplicate();
                return;
            }
            if (idField) {
                idField.value = preservedId;
            }
            if (categoryEl) {
                categoryEl.value = preservedCategoryId;
            }
            if (schemeEl) {
                schemeEl.innerHTML = preservedSchemeOptionsHtml;
                schemeEl.value = preservedSchemeId;
            }
            lockAppendixIIIBIdentityFields();
            checkAppendixIIIBDuplicate();
        }, 0);
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "iiibSearchInput") {
            iiibCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("iiibExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("iiibColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("iiibDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("iiibDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixIIIBDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__iiibHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("iiibGridBody") && $("appendixIIIBForm")) {
                iiibCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentIiibDrawerToBody();
        };
        window.PreBudgetControlBinding.__iiibHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("iiibGridBody") && $("appendixIIIBForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentIiibDrawerToBody();
})();
