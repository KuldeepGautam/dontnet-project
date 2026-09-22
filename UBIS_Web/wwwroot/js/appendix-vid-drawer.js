// "Approved design for all appendixes" (2026-09-08, UBIS-Pre-budget-html/Appendix6d.html) -
// drawer/grid interaction layer for the redesigned Appendix VI-D page. Mirrors
// appendix-vic-drawer.js's own structure (see that file's header comment, and
// appendix-via-drawer.js's for the original reasoning behind each piece: drawer slide-in,
// columns picker, row action menu, delete dialog, toast, search/status filter + pagination,
// Export dropdown). Add/Edit/Delete/Freeze/Nil all go through the SAME real POST actions
// (SaveAppendixVID/DeleteAppendixVID/FreezeAppendixTemplate/SetNilSubmission) the old
// inline-table view already used.
//
// Two VI-D-specific business rules, both previously handled inside the container-scoped
// PreBudget-control-binding.js and re-wired here at document level since this drawer's own
// reparenting-to-body breaks container-scoped delegation (same reasoning as VI-B/VI-C):
//   1. The Autonomous Body select is locked (disabled + hidden-mirror-input) during Edit mode,
//      since it's part of the record's identity - see lockSelectForEdit's own comment.
//   2. Changing Autonomous Body clears the "Expected" figures + Remarks (2026-08-31 requirement)
//      and re-fetches the previous-year reference figures for the newly selected body.
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("vidToast");
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
        $("vidToastTitle").textContent = variant.title;
        $("vidToastIcon").setAttribute("data-lucide", variant.icon);
        $("vidToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("vidToast");
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
        var menu = $("vidExportMenu");
        var button = $("vidExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("vidExportMenu");
        var button = $("vidExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixVid(format, triggerButton) {
        var form = $("appendixVIDForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixVID?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

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
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixVID." + format);
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
        var menu = $("vidColumnsMenu");
        var button = $("vidColumnsButton");
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
        var menu = $("vidColumnsMenu");
        var button = $("vidColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("vidColumnsMenu");
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
        var menu = $("vidColumnsMenu");
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
        var menu = $("vid-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("vid-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("vid-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "vidGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit)
    // ----------------------------------------------------------------------------------------
    var VID_EDIT_FIELD_MAP = [
        ["NewRecord_AsOnMarch31", "data-as-on-march31"],
        ["NewRecord_AsOnJune30", "data-as-on-june30"],
        ["NewRecord_ExpectedNextMarch31", "data-expected-next-march31"],
        ["NewRecord_ExpectedNextFY", "data-expected-next-fy"],
        ["NewRecord_Remarks", "data-remarks"]
    ];

    function populateAppendixVidFieldsFromButton(editButton) {
        var idField = $("NewRecord_Id");
        if (idField) {
            idField.value = editButton.getAttribute("data-id") || "";
        }

        var bodySelect = $("NewRecord_AutonomousBodyId");
        if (bodySelect) {
            bodySelect.value = editButton.getAttribute("data-autonomous-body-id") || "";
        }

        VID_EDIT_FIELD_MAP.forEach(function (pair) {
            var input = $(pair[0]);
            if (input) {
                input.value = editButton.getAttribute(pair[1]) || "";
            }
        });

        // VI-D's own Edit mode locks the Autonomous Body select (it's part of the record's
        // identity, per the 2026-08-31 requirement) via a disabled-select + hidden-mirror-input
        // pair - needed here since this drawer populates its own fields directly rather than
        // through PreBudget-control-binding.js's container-scoped dispatch.
        if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.lockSelectForEdit === "function") {
            window.PreBudgetControlBinding.lockSelectForEdit("NewRecord_AutonomousBodyId");
        }

        var form = $("appendixVIDForm");
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

    function openAppendixVidDrawer(mode, editButton) {
        var drawer = $("vidDrawer");
        var backdrop = $("vidDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixVIDForm");

        if (mode === "edit" && editButton) {
            populateAppendixVidFieldsFromButton(editButton);
        }

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.unlockSelectsAfterEdit === "function") {
                window.PreBudgetControlBinding.unlockSelectsAfterEdit();
            }
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
        }

        var titleEl = $("vidDrawerTitle");
        var subtitleEl = $("vidDrawerSubtitle");
        if (titleEl) {
            titleEl.textContent = mode === "edit" ? "Edit Record" : "Add Record";
        }
        if (subtitleEl) {
            subtitleEl.textContent = mode === "edit"
                ? "Update the selected internal-resources record"
                : "Create a new internal-resources record";
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

    // Same fix as VI-A/VI-B/VI-C's own drawer reparenting - see appendix-via-drawer.js's comment
    // for the full reasoning (drawer painted behind the app shell's masthead otherwise).
    function reparentVidDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="vidDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="vidDrawerBackdrop"]');
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

    function closeAppendixVidDrawer() {
        var drawer = $("vidDrawer");
        var backdrop = $("vidDrawerBackdrop");
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
        var dialog = $("vidDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("vidDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("vidCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("vidDeleteDialog");
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

    var vidCurrentPage = 1;
    var vidPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("vidSearchInput");
        var statusEl = $("vidStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / vidPageSize));
        if (vidCurrentPage > totalPages) {
            vidCurrentPage = totalPages;
        }
        var start = (vidCurrentPage - 1) * vidPageSize;
        var pageRowSet = matched.slice(start, start + vidPageSize);

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

        var empty = $("vidEmptyState");
        var gridWrap = $("vidGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("vidTotalText");
        var rangeText = $("vidRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixVidPagination(totalPages);
    }

    function renderAppendixVidPagination(totalPages) {
        var holder = $("vidPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - vidCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-vid-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === vidCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("vidPrevBtn");
        var nextBtn = $("vidNextBtn");
        if (prevBtn) {
            prevBtn.disabled = vidCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", vidCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = vidCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", vidCurrentPage === totalPages);
        }
    }

    function goToAppendixVidPage(page) {
        vidCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixVidPage(delta) {
        goToAppendixVidPage(vidCurrentPage + delta);
    }

    function changeAppendixVidPageSize() {
        var sizeEl = $("vidPageSize");
        vidPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        vidCurrentPage = 1;
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

    function clearAppendixVidFilters() {
        var searchEl = $("vidSearchInput");
        var statusEl = $("vidStatusFilter");
        if (searchEl) {
            searchEl.value = "";
        }
        if (statusEl) {
            statusEl.value = "All";
        }
        applyGridFilters();
    }

    // ----------------------------------------------------------------------------------------
    // Autonomous Body change: clear the "Expected" figures + Remarks (2026-08-31 requirement)
    // and re-fetch the previous-year reference, re-wired at document level (see file header
    // comment) since the drawer housing this select gets reparented to body.
    // ----------------------------------------------------------------------------------------
    document.addEventListener("change", function (event) {
        if (event.target.id !== "NewRecord_AutonomousBodyId" || !event.target.closest("#appendixVIDForm")) {
            return;
        }

        ["NewRecord_ExpectedNextMarch31", "NewRecord_ExpectedNextFY", "NewRecord_Remarks"].forEach(function (id) {
            var field = $(id);
            if (field) {
                field.value = "";
            }
        });
        var charCount = $("vidCharCount");
        if (charCount) {
            charCount.textContent = "0";
        }

        var pcb = window.PreBudgetControlBinding;
        if (!pcb || typeof pcb.loadAppendixVIDPreviousYearReference !== "function") {
            return;
        }
        var form = $("appendixVIDForm");
        pcb.loadAppendixVIDPreviousYearReference(form ? form.getAttribute("data-demand-id") : null, event.target.value);
    });

    // Bug fix 2026-09-14 (client report: "reset mode, disable dropdown Name of GranteeBody/
    // Autonomous Institution"): the generic VI/VI-A/VI-C/VI-D/VI-E reset handler in
    // PreBudget-control-binding.js just blanks NewRecord_Id - it doesn't know this form locks its
    // Autonomous Body select during Edit (see file header comment #1). A native <button
    // type="reset"> restores every control's INITIAL value, including the select's original blank
    // placeholder, while leaving the `disabled` attribute untouched - so after Reset in Edit mode
    // the dropdown was stuck disabled on a blank "Select" option with no value and no way to fix
    // it. Registered after PreBudget-control-binding.js's own reset listener (script load order in
    // PreBudgetDataandReport.cshtml), so this handler's setTimeout runs after that one's and wins:
    // restores the record id + the locked selection once the native reset has already run.
    document.addEventListener("reset", function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement) || form.id !== "appendixVIDForm") {
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

        // Bug fix 2026-09-14 (client report: "reset button should not clear value of dropdown
        // Name of GranteeBody/Autonomous Institution ... it should retain the value" - still
        // happening despite the "reset" event handler above): PreBudget-control-binding.js's own
        // generic reset listener (any form inside .ubis-modern-appendix) unconditionally
        // hard-clears the form via its own setTimeout(0) on top of the native reset, and racing it
        // via another "reset" event listener proved unreliable (same class of bug confirmed and
        // fixed with a click-level intercept on Appendix III-B/VI-B - see those files' own
        // comments). Intercept the button CLICK instead (before any "reset" event can fire at all)
        // and, while still in Edit mode, prevent the native reset entirely and blank only the
        // plain editable fields ourselves - the Autonomous Body select and the record id are left
        // completely untouched, so they can't be raced or reverted.
        var resetVidBtn = event.target.closest('#appendixVIDForm button[type="reset"]');
        if (resetVidBtn) {
            var idFieldForReset = $("NewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                ["NewRecord_AsOnMarch31", "NewRecord_AsOnJune30", "NewRecord_ExpectedNextMarch31", "NewRecord_ExpectedNextFY", "NewRecord_Remarks"].forEach(function (id) {
                    var field = $(id);
                    if (field) { field.value = ""; }
                });
                var charCount = $("vidCharCount");
                if (charCount) {
                    charCount.textContent = "0";
                }
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-vid-add-drawer"]')) {
            openAppendixVidDrawer("add");
            return;
        }

        var editVIDButton = event.target.closest('[data-action="edit-appendix-vid-record"]');
        if (editVIDButton) {
            openAppendixVidDrawer("edit", editVIDButton);
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-vid-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var vidCancelForm = $("appendixVIDForm");
            var vidPcb = window.PreBudgetControlBinding;
            var vidIsDirty = !vidPcb || typeof vidPcb.isFormDirtySinceOpen !== "function" || vidPcb.isFormDirtySinceOpen(vidCancelForm);
            if (!vidIsDirty) {
                closeAppendixVidDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixVidDrawer();
                }, false, "No");
            } else {
                closeAppendixVidDrawer();
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

        if (event.target.closest("#vidColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#vidColumnsButton") && !event.target.closest("#vidColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-vid-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-vid"]');
        if (exportOptionBtn) {
            exportAppendixVid(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#vidExportButton") && !event.target.closest("#vidExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-vid-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-vid-trigger"]');
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

        if (event.target.closest("#vidCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#vidConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-vid-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-vid-filters"]')) {
            clearAppendixVidFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-vid-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-vid-prev-page"]')) {
            changeAppendixVidPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-vid-next-page"]')) {
            changeAppendixVidPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-vid-goto-page"]');
        if (pageBtn) {
            goToAppendixVidPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#chargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#vidToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#vidColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "vidStatusFilter") {
            vidCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "vidPageSize") {
            changeAppendixVidPageSize();
        }
        if (event.target.id === "NewRecord_Remarks") {
            var charCount = $("vidCharCount");
            if (charCount) {
                charCount.textContent = String(event.target.value.length);
            }
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "vidSearchInput") {
            vidCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "NewRecord_Remarks") {
            var charCount = $("vidCharCount");
            if (charCount) {
                charCount.textContent = String(event.target.value.length);
            }
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("vidExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("vidColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("vidDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("vidDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixVidDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__vidHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("gridBody") && $("appendixVIDForm")) {
                vidCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentVidDrawerToBody();
        };
        window.PreBudgetControlBinding.__vidHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("gridBody") && $("appendixVIDForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentVidDrawerToBody();
})();
