// "Approved design for all appendixes" (2026-09-08, UBIS-Pre-budget-html/Appendix6b.html) -
// drawer/grid interaction layer for the redesigned Appendix VI-B page. Mirrors
// appendix-via-drawer.js's own structure exactly (same drawer slide-in, columns picker, row
// action menu, delete dialog, toast, client-side search/status filter + pagination, Export
// dropdown) - see that file's own header comment for the reasoning behind each piece. Add/Edit/
// Delete/Freeze/Nil all go through the SAME real POST actions
// (SaveAppendixVIB/DeleteAppendixVIB/FreezeAppendixTemplate/SetNilSubmission) the old inline-table
// view already used.
//
// VI-B specific wrinkle VI-A didn't have: the Category->Scheme->SubScheme cascade and the
// edit-populate dispatch both already exist in PreBudget-control-binding.js, but that file's own
// listeners are delegated on the AJAX-fragment CONTAINER, not `document`. Reparenting this
// drawer to document.body (see reparentVibDrawerToBody below, same fix as VI-A's own drawer-
// behind-masthead fix) moves every field inside it - including Category/Scheme/SubScheme - out
// from under that container, so its change/click listeners for those fields stop firing. This
// file re-wires its own document-level listeners for exactly those fields, calling straight into
// PreBudget-control-binding.js's now-exposed loadVIBSchemesForCategory/loadVIBSubSchemesForScheme/
// loadAppendixVIBBe/loadAppendixVIBPreviousYearReference instead of duplicating that fetch logic.
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("vibToast");
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
        $("vibToastTitle").textContent = variant.title;
        $("vibToastIcon").setAttribute("data-lucide", variant.icon);
        $("vibToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("vibToast");
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
        var menu = $("vibExportMenu");
        var button = $("vibExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("vibExportMenu");
        var button = $("vibExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixVib(format, triggerButton) {
        var form = $("appendixVIBForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixVIB?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

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
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixVIB." + format);
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
        var menu = $("vibColumnsMenu");
        var button = $("vibColumnsButton");
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
        var menu = $("vibColumnsMenu");
        var button = $("vibColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("vibColumnsMenu");
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
        var menu = $("vibColumnsMenu");
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
        // Marks this element for pre-budget-meeting.js's document-level submit listener (see its
        // own comment) - the row's Delete <form> lives inside this menu, and would otherwise
        // silently fall back to a real, un-intercepted browser post once reparented here.
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
        var menu = $("vib-menu-" + id);
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
        if (!menu || !menu.id.indexOf) {
            return;
        }
        if (menu.id.indexOf("vib-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("vib-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "vibGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit) - #appendixVIBForm lives inside this drawer's markup. Field population
    // on Edit is done directly here (not via PreBudget-control-binding.js's own
    // "edit-appendix-vib-record" dispatch) for the same reason as VI-A's own drawer: that
    // dispatch is delegated on #partialViewContainer, and reparenting this whole drawer to
    // document.body (see reparentVibDrawerToBody below) takes it - and every click inside it -
    // out from under that container.
    // ----------------------------------------------------------------------------------------
    var VIB_EDIT_FIELD_MAP = [
        ["NewRecord_PendingLiabilityAsOnMarch31", "data-pending-liability-as-on-march31"],
        ["NewRecord_BE", "data-be"],
        ["NewRecord_EstimatedExpenditure", "data-estimated-expenditure"],
        ["NewRecord_Remarks", "data-remarks"]
    ];

    function populateAppendixVibFieldsFromButton(editButton) {
        var idField = $("NewRecord_Id");
        if (idField) {
            idField.value = editButton.getAttribute("data-id") || "";
        }

        VIB_EDIT_FIELD_MAP.forEach(function (pair) {
            var input = $(pair[0]);
            if (input) {
                input.value = editButton.getAttribute(pair[1]) || "";
            }
        });
        updateRemarksCounter();

        var categorySelect = $("CategoryId");
        var categoryId = editButton.getAttribute("data-category-id") || "";
        var schemeId = editButton.getAttribute("data-scheme-id") || "";
        var subSchemeId = editButton.getAttribute("data-sub-scheme-id") || "";
        if (categorySelect && window.PreBudgetControlBinding) {
            categorySelect.value = categoryId;
            if (typeof window.PreBudgetControlBinding.loadVIBSchemesForCategory === "function") {
                window.PreBudgetControlBinding.loadVIBSchemesForCategory(categoryId, schemeId, function () {
                    if (typeof window.PreBudgetControlBinding.loadVIBSubSchemesForScheme === "function") {
                        window.PreBudgetControlBinding.loadVIBSubSchemesForScheme(schemeId, subSchemeId, lockAppendixVibIdentityFields);
                    } else {
                        lockAppendixVibIdentityFields();
                    }
                });
            }
        }

        var form = $("appendixVIBForm");
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

    // Client requirement 2026-09-14: "disable Category, Scheme, Sub-Scheme dropdowns on edit
    // mode" - all three are part of the record's identity (the duplicate check is keyed on
    // Category+Scheme[+SubScheme]). Uses the shared lockSelectForEdit convention (disabled + a
    // hidden mirror input so the value still posts) rather than a plain `.disabled = true`, which
    // would both drop the field from FormData and trip the shared blank-cascade-select submit
    // guard in PreBudget-validation.js (it only excludes selects carrying the
    // appendix-locked-for-edit class) - same root cause already fixed for VI-E/VI-G.
    function lockAppendixVibIdentityFields() {
        if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.lockSelectForEdit === "function") {
            window.PreBudgetControlBinding.lockSelectForEdit("CategoryId");
            window.PreBudgetControlBinding.lockSelectForEdit("NewRecord_SchemeId");
            window.PreBudgetControlBinding.lockSelectForEdit("NewRecord_SubSchemeId");
        } else {
            ["CategoryId", "NewRecord_SchemeId", "NewRecord_SubSchemeId"].forEach(function (id) {
                var el = $(id);
                if (el) { el.disabled = true; }
            });
        }
    }

    function openAppendixVibDrawer(mode, editButton) {
        var drawer = $("vibDrawer");
        var backdrop = $("vibDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixVIBForm");

        if (mode === "edit" && editButton) {
            populateAppendixVibFieldsFromButton(editButton);
        }

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
            var categorySelect = $("CategoryId");
            if (categorySelect) {
                categorySelect.value = "";
            }
            var schemeSelect = $("NewRecord_SchemeId");
            if (schemeSelect) {
                schemeSelect.innerHTML = '<option value="">Select a Category first</option>';
                schemeSelect.disabled = true;
            }
            var subSchemeSelect = $("NewRecord_SubSchemeId");
            if (subSchemeSelect) {
                subSchemeSelect.innerHTML = '<option value="">Select a Scheme first</option>';
                subSchemeSelect.disabled = true;
            }
            updateRemarksCounter();
        }

        var titleEl = $("vibDrawerTitle");
        var subtitleEl = $("vibDrawerSubtitle");
        if (titleEl) {
            titleEl.textContent = mode === "edit" ? "Edit Record" : "Add Record";
        }
        if (subtitleEl) {
            subtitleEl.textContent = mode === "edit"
                ? "Update the selected pending-liability record"
                : "Create a new pending-liability record";
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
            var categoryField = $("CategoryId");
            if (categoryField) {
                categoryField.focus();
            }
        }, 320);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    // Same fix as VI-A's reparentViaDrawerToBody - see appendix-via-drawer.js's own comment for
    // the full reasoning (drawer painted behind the app shell's masthead otherwise).
    function reparentVibDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="vibDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="vibDrawerBackdrop"]');
        var drawer = null;
        allDrawers.forEach(function (node) {
            if (node.parentElement !== document.body) { drawer = node; }
        });
        var backdrop = null;
        allBackdrops.forEach(function (node) {
            if (node.parentElement !== document.body) { backdrop = node; }
        });

        // Bug report 2026-09-08 (see appendix-via-drawer.js's own matching comment): if no fresh
        // (non-body) copy exists, VI-B's markup isn't part of the current fragment any more (the
        // user switched to a different appendix) - remove every leftover instead of leaving a
        // stale, possibly still-open drawer sitting on top of whatever loaded next.
        if (!drawer) {
            allDrawers.forEach(function (node) { node.remove(); });
            allBackdrops.forEach(function (node) { node.remove(); });
            return;
        }

        allDrawers.forEach(function (node) { if (node !== drawer) { node.remove(); } });
        allBackdrops.forEach(function (node) { if (node !== backdrop) { node.remove(); } });

        if (drawer) {
            drawer.classList.add("ubis-modern-appendix");
            // Bug report 2026-09-08 ("after modifying record, upper Demand/Appendix selector
            // gone"): marks this element for pre-budget-meeting.js's document-level submit
            // listener (see its own comment) - #appendixVIBForm lives inside this drawer.
            drawer.setAttribute("data-reparented-fragment", "true");
            if (drawer.parentElement !== document.body) {
                document.body.appendChild(drawer);
            }
        }
        if (backdrop && backdrop.parentElement !== document.body) {
            document.body.insertBefore(backdrop, drawer || null);
        }
    }

    function closeAppendixVibDrawer() {
        var drawer = $("vibDrawer");
        var backdrop = $("vibDrawerBackdrop");
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
        var dialog = $("vibDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("vibDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("vibCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("vibDeleteDialog");
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

    var vibCurrentPage = 1;
    var vibPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("vibSearchInput");
        var statusEl = $("vibStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / vibPageSize));
        if (vibCurrentPage > totalPages) {
            vibCurrentPage = totalPages;
        }
        var start = (vibCurrentPage - 1) * vibPageSize;
        var pageRowSet = matched.slice(start, start + vibPageSize);

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

        var empty = $("vibEmptyState");
        var gridWrap = $("vibGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("vibTotalText");
        var rangeText = $("vibRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixVibPagination(totalPages);
    }

    function renderAppendixVibPagination(totalPages) {
        var holder = $("vibPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - vibCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-vib-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === vibCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("vibPrevBtn");
        var nextBtn = $("vibNextBtn");
        if (prevBtn) {
            prevBtn.disabled = vibCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", vibCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = vibCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", vibCurrentPage === totalPages);
        }
    }

    function goToAppendixVibPage(page) {
        vibCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixVibPage(delta) {
        goToAppendixVibPage(vibCurrentPage + delta);
    }

    function changeAppendixVibPageSize() {
        var sizeEl = $("vibPageSize");
        vibPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        vibCurrentPage = 1;
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

    function clearAppendixVibFilters() {
        var searchEl = $("vibSearchInput");
        var statusEl = $("vibStatusFilter");
        if (searchEl) {
            searchEl.value = "";
        }
        if (statusEl) {
            statusEl.value = "All";
        }
        applyGridFilters();
    }

    function updateRemarksCounter() {
        var remarks = $("NewRecord_Remarks");
        var counter = $("vibCharCount");
        if (remarks && counter) {
            counter.textContent = String(remarks.value.length);
        }
    }

    // ----------------------------------------------------------------------------------------
    // Category -> Scheme -> SubScheme cascade, re-wired at document level (see file header
    // comment) since the drawer housing these selects gets reparented to document.body.
    // ----------------------------------------------------------------------------------------
    document.addEventListener("change", function (event) {
        var pcb = window.PreBudgetControlBinding;
        if (!pcb) {
            return;
        }

        // Bug report 2026-09-10 ("on clicking Add button, multiple hits going to controller"):
        // opening the Add drawer runs PreBudgetControlBinding.exitAppendixEditMode ->
        // hardClearAppendixForm, which dispatches a synthetic `change` on EVERY control in the
        // form to re-run client-side recalcs/dup-checks against the blanked state. Those synthetic
        // events also bubbled in here and re-fired this whole Category/Scheme/SubScheme cascade
        // (loadVIBSchemesForCategory -> loadVIBSubSchemesForScheme -> prev-year ref -> BE lookup)
        // three times per Add click - the same handler location tripping repeatedly, and a live
        // cascade fetch whenever a value hadn't been blanked yet. The cascade only ever needs to
        // react to a real user picking a value; the drawer-open ("add") path and Reset/Cancel
        // already reset the dependent selects directly, and the Edit path calls
        // loadVIBSchemesForCategory itself. So ignore programmatic (untrusted) change events here.
        if (!event.isTrusted) {
            return;
        }

        if (event.target.id === "CategoryId" && event.target.closest("#appendixVIBForm")) {
            if (typeof pcb.loadVIBSchemesForCategory === "function") {
                pcb.loadVIBSchemesForCategory(event.target.value, null);
            }
            var beInput = $("NewRecord_BE");
            if (beInput) {
                beInput.value = "";
            }
            return;
        }

        if (event.target.id === "NewRecord_SchemeId" && event.target.closest("#appendixVIBForm")) {
            var schemeForm = $("appendixVIBForm");
            if (typeof pcb.loadVIBSubSchemesForScheme === "function") {
                pcb.loadVIBSubSchemesForScheme(event.target.value, null);
            }
            if (typeof pcb.loadAppendixVIBPreviousYearReference === "function") {
                pcb.loadAppendixVIBPreviousYearReference(schemeForm ? schemeForm.getAttribute("data-demand-id") : null, event.target.value, null);
            }
            var categorySelect = $("CategoryId");
            if (typeof pcb.loadAppendixVIBBe === "function") {
                pcb.loadAppendixVIBBe(schemeForm ? schemeForm.getAttribute("data-demand-id") : null, categorySelect ? categorySelect.value : null, event.target.value);
            }
            return;
        }

        if (event.target.id === "NewRecord_SubSchemeId" && event.target.closest("#appendixVIBForm")) {
            var subForm = $("appendixVIBForm");
            var schemeSelect = $("NewRecord_SchemeId");
            if (typeof pcb.loadAppendixVIBPreviousYearReference === "function") {
                pcb.loadAppendixVIBPreviousYearReference(subForm ? subForm.getAttribute("data-demand-id") : null, schemeSelect ? schemeSelect.value : null, event.target.value);
            }
        }
    });

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

        // Client requirement 2026-09-14: "on reset [in edit mode] these [Category/Scheme/
        // Sub-Scheme] should retain their value and remain disabled", BE preserved, numeric
        // fields reset to 0.00, text fields blank. A native <button type="reset"> restores every
        // control's page-load initial value (blanking the locked selects while leaving them
        // disabled) and PreBudget-control-binding.js's own generic reset listener (any form inside
        // .ubis-modern-appendix) unconditionally hard-clears the form on top of that via its own
        // setTimeout(0) - racing a "reset" event listener here would only reproduce the same class
        // of bug already hit and fixed on Appendix III-B (see that file's own comment). Intercept
        // the button CLICK instead (before any "reset" event ever fires) and, while still in Edit
        // mode, prevent the native reset entirely and apply the exact field-by-field behavior
        // ourselves - Category/Scheme/Sub-Scheme/Id are left completely untouched.
        var resetVibBtn = event.target.closest('#appendixVIBForm button[type="reset"]');
        if (resetVibBtn) {
            var idFieldForReset = $("NewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                var pendingLiabilityField = $("NewRecord_PendingLiabilityAsOnMarch31");
                var estimatedExpenditureField = $("NewRecord_EstimatedExpenditure");
                var remarksField = $("NewRecord_Remarks");
                if (pendingLiabilityField) { pendingLiabilityField.value = "0.00"; }
                if (estimatedExpenditureField) { estimatedExpenditureField.value = "0.00"; }
                if (remarksField) { remarksField.value = ""; }
                updateRemarksCounter();
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-vib-add-drawer"]')) {
            openAppendixVibDrawer("add");
            return;
        }

        var editVIBButton = event.target.closest('[data-action="edit-appendix-vib-record"]');
        if (editVIBButton) {
            openAppendixVibDrawer("edit", editVIBButton);
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-vib-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var vibCancelForm = $("appendixVIBForm");
            var vibPcb = window.PreBudgetControlBinding;
            var vibIsDirty = !vibPcb || typeof vibPcb.isFormDirtySinceOpen !== "function" || vibPcb.isFormDirtySinceOpen(vibCancelForm);
            if (!vibIsDirty) {
                closeAppendixVibDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixVibDrawer();
                }, false, "No");
            } else {
                closeAppendixVibDrawer();
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

        if (event.target.closest("#vibColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#vibColumnsButton") && !event.target.closest("#vibColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-vib-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-vib"]');
        if (exportOptionBtn) {
            exportAppendixVib(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#vibExportButton") && !event.target.closest("#vibExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-vib-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-vib-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            // Fix 2026-09-08 - see appendix-via-drawer.js's matching comment: the Delete <form> is
            // a SIBLING of this button inside .action-menu, not an ancestor, so closest("form")
            // always returned null and the confirm dialog's Delete button silently no-op'd.
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#vibCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#vibConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-vib-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-vib-filters"]')) {
            clearAppendixVibFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-vib-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-vib-prev-page"]')) {
            changeAppendixVibPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-vib-next-page"]')) {
            changeAppendixVibPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-vib-goto-page"]');
        if (pageBtn) {
            goToAppendixVibPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#chargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#vibToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#vibColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "vibStatusFilter") {
            vibCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "vibPageSize") {
            changeAppendixVibPageSize();
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "vibSearchInput") {
            vibCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "NewRecord_Remarks" && event.target.closest("#appendixVIBForm")) {
            updateRemarksCounter();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("vibExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("vibColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("vibDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("vibDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixVibDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__vibHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("gridBody") && $("appendixVIBForm")) {
                vibCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentVibDrawerToBody();
        };
        window.PreBudgetControlBinding.__vibHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("gridBody") && $("appendixVIBForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentVibDrawerToBody();
})();
