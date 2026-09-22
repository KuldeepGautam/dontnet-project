// "Approved design for all appendixes" (2026-09-08, UBIS-Pre-budget-html/Appendix6c.html) -
// drawer/grid interaction layer for the redesigned Appendix VI-C page. Mirrors
// appendix-via-drawer.js's own structure (see that file's header comment for the reasoning behind
// each piece: drawer slide-in, columns picker, row action menu, delete dialog, toast, search/
// status filter + pagination, Export dropdown). Add/Edit/Delete/Freeze/Nil all go through the
// SAME real POST actions (SaveAppendixVIC/DeleteAppendixVIC/FreezeAppendixTemplate/
// SetNilSubmission) the old inline-table view already used.
//
// No Category/Scheme cascade here (Autonomous Body is a flat dropdown) - the one piece of
// PreBudget-control-binding.js wiring this drawer's own reparenting-to-body breaks is the
// previous-year-reference auto-fill on Autonomous Body change, re-wired at document level below
// (same reasoning as appendix-vib-drawer.js's own, larger cascade re-wiring).
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("vicToast");
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
        $("vicToastTitle").textContent = variant.title;
        $("vicToastIcon").setAttribute("data-lucide", variant.icon);
        $("vicToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("vicToast");
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
        var menu = $("vicExportMenu");
        var button = $("vicExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("vicExportMenu");
        var button = $("vicExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixVic(format, triggerButton) {
        var form = $("appendixVICForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixVIC?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

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
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixVIC." + format);
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
        var menu = $("vicColumnsMenu");
        var button = $("vicColumnsButton");
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
        var menu = $("vicColumnsMenu");
        var button = $("vicColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("vicColumnsMenu");
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
        var menu = $("vicColumnsMenu");
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
        var menu = $("vic-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("vic-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("vic-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "vicGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    var VIC_EDIT_FIELD_MAP = [
        ["NewRecord_AutonomousBodyId", "data-autonomous-body-id"],
        ["NewRecord_AccumulatedBalancePrevYear", "data-accumulated-balance-prev-year"],
        ["NewRecord_AccumulatedBalance", "data-accumulated-balance"],
        ["NewRecord_ActualExpenditureY1", "data-actual-expenditure-y1"],
        ["NewRecord_ActualExpenditureY2", "data-actual-expenditure-y2"],
        ["NewRecord_ActualExpenditureY3", "data-actual-expenditure-y3"],
        ["NewRecord_AllocationInBE", "data-allocation-in-be"],
        ["NewRecord_ExpenditureTillSept", "data-expenditure-till-sept"],
        ["NewRecord_ReasonForCorpusFund", "data-reason-for-corpus-fund"]
    ];

    var originalAutonomousBodyValue = null;

    function populateAppendixVicFieldsFromButton(editButton) {
        var idField = $("NewRecord_Id");
        if (idField) {
            idField.value = editButton.getAttribute("data-id") || "";
        }

        VIC_EDIT_FIELD_MAP.forEach(function (pair) {
            var input = $(pair[0]);
            if (input) {
                input.value = editButton.getAttribute(pair[1]) || "";
            }
        });

        setSegmentByGroup("vicPub", editButton.getAttribute("data-is-public-account") === "true" ? "YES" : "NO");

        var autonomousBodyField = $("NewRecord_AutonomousBodyId");
        if (autonomousBodyField) {
            originalAutonomousBodyValue = autonomousBodyField.value;
            autonomousBodyField.disabled = true;
        }

        var form = $("appendixVICForm");
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

    function openAppendixVicDrawer(mode, editButton) {
        var drawer = $("vicDrawer");
        var backdrop = $("vicDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixVICForm");

        if (mode === "edit" && editButton) {
            populateAppendixVicFieldsFromButton(editButton);
        }

        if (mode === "add" && form) {
            originalAutonomousBodyValue = null;
            var autonomousBodyField = $("NewRecord_AutonomousBodyId")






                ;
            if (autonomousBodyField) {
                autonomousBodyField.disabled = false;
            }

            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
            setSegmentByGroup("vicPub", "NO");
        }

        var titleEl = $("vicDrawerTitle");
        var subtitleEl = $("vicDrawerSubtitle");
        if (titleEl) {
            titleEl.textContent = mode === "edit" ? "Edit Record" : "Add Record";
        }
        if (subtitleEl) {
            subtitleEl.textContent = mode === "edit"
                ? "Update the selected corpus-fund record"
                : "Create a new corpus-fund record";
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
            var autonomousField = $("NewRecord_AutonomousBodyId");
            if (autonomousField) {
                autonomousField.focus();
            }
        }, 320);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    // Same fix as VI-A/VI-B's own drawer reparenting - see appendix-via-drawer.js's comment for
    // the full reasoning (drawer painted behind the app shell's masthead otherwise).
    function reparentVicDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="vicDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="vicDrawerBackdrop"]');
        var drawer = null;
        allDrawers.forEach(function (node) {
            if (node.parentElement !== document.body) { drawer = node; }
        });
        var backdrop = null;
        allBackdrops.forEach(function (node) {
            if (node.parentElement !== document.body) { backdrop = node; }
        });

        // Bug report 2026-09-08 (see appendix-via-drawer.js's own matching comment): if no fresh
        // (non-body) copy exists, VI-C's markup isn't part of the current fragment any more (the
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
            drawer.setAttribute("data-reparented-fragment", "true");
            if (drawer.parentElement !== document.body) {
                document.body.appendChild(drawer);
            }
        }
        if (backdrop && backdrop.parentElement !== document.body) {
            document.body.insertBefore(backdrop, drawer || null);
        }
    }

    function closeAppendixVicDrawer() {
        var drawer = $("vicDrawer");
        var backdrop = $("vicDrawerBackdrop");
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
        var dialog = $("vicDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("vicDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("vicCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("vicDeleteDialog");
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

    var vicCurrentPage = 1;
    var vicPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("vicSearchInput");
        var statusEl = $("vicStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / vicPageSize));
        if (vicCurrentPage > totalPages) {
            vicCurrentPage = totalPages;
        }
        var start = (vicCurrentPage - 1) * vicPageSize;
        var pageRowSet = matched.slice(start, start + vicPageSize);

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

        var empty = $("vicEmptyState");
        var gridWrap = $("vicGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("vicTotalText");
        var rangeText = $("vicRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixVicPagination(totalPages);
    }

    function renderAppendixVicPagination(totalPages) {
        var holder = $("vicPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - vicCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-vic-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === vicCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("vicPrevBtn");
        var nextBtn = $("vicNextBtn");
        if (prevBtn) {
            prevBtn.disabled = vicCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", vicCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = vicCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", vicCurrentPage === totalPages);
        }
    }

    function goToAppendixVicPage(page) {
        vicCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixVicPage(delta) {
        goToAppendixVicPage(vicCurrentPage + delta);
    }

    function changeAppendixVicPageSize() {
        var sizeEl = $("vicPageSize");
        vicPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        vicCurrentPage = 1;
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

    function clearAppendixVicFilters() {
        var searchEl = $("vicSearchInput");
        var statusEl = $("vicStatusFilter");
        if (searchEl) {
            searchEl.value = "";
        }
        if (statusEl) {
            statusEl.value = "All";
        }
        applyGridFilters();
    }

    // ----------------------------------------------------------------------------------------
    // Segmented Yes/No (Public Account)
    // ----------------------------------------------------------------------------------------
    // Public Account Yes/No (design: Appendix6c.html) is color-coded green(Yes)/red(No), unlike
    // VI-A's own Yes/No questions which use the shared navy .seg-active highlight - handled with
    // its own class set here so this stays scoped to this one control.
    var VICPUB_ACTIVE_CLASSES = {
        YES: ["bg-green-600", "text-white", "border-green-600"],
        NO: ["bg-red-600", "text-white", "border-red-600"]
    };
    var VICPUB_INACTIVE_CLASSES = ["border-gray-300", "text-gray-600"];

    function setSegment(button) {
        var group = button.getAttribute("data-group");
        var value = button.textContent.trim();
        document.querySelectorAll('[data-group="' + group + '"]').forEach(function (btn) {
            var btnValue = btn.textContent.trim();
            btn.classList.remove.apply(btn.classList, VICPUB_ACTIVE_CLASSES.YES.concat(VICPUB_ACTIVE_CLASSES.NO));
            if (btnValue === value) {
                btn.classList.add.apply(btn.classList, VICPUB_ACTIVE_CLASSES[value] || VICPUB_INACTIVE_CLASSES);
            } else {
                btn.classList.add.apply(btn.classList, VICPUB_INACTIVE_CLASSES);
            }
        });
        var targetId = button.getAttribute("data-target");
        if (targetId) {
            var radio = document.getElementById(targetId);
            if (radio) {
                radio.checked = true;
            }
        }
    }

    function setSegmentByGroup(group, value) {
        document.querySelectorAll('#appendixVICForm .seg-btn-vicpub[data-group="' + group + '"]').forEach(function (btn) {
            var active = btn.textContent.trim() === value;
            btn.classList.remove.apply(btn.classList, VICPUB_ACTIVE_CLASSES.YES.concat(VICPUB_ACTIVE_CLASSES.NO).concat(VICPUB_INACTIVE_CLASSES));
            btn.classList.add.apply(btn.classList, active ? (VICPUB_ACTIVE_CLASSES[value] || VICPUB_INACTIVE_CLASSES) : VICPUB_INACTIVE_CLASSES);
            if (active) {
                var targetId = btn.getAttribute("data-target");
                if (targetId) {
                    var radio = document.getElementById(targetId);
                    if (radio) {
                        radio.checked = true;
                    }
                }
            }
        });
    }

    // ----------------------------------------------------------------------------------------
    // Previous-year-reference auto-fill on Autonomous Body change, re-wired at document level
    // (see file header comment) since the drawer housing this select gets reparented to body.
    // ----------------------------------------------------------------------------------------
    document.addEventListener("change", function (event) {
        if (event.target.id !== "NewRecord_AutonomousBodyId" || !event.target.closest("#appendixVICForm")) {
            return;
        }
        var pcb = window.PreBudgetControlBinding;
        if (!pcb || typeof pcb.loadAppendixVICPreviousYearReference !== "function") {
            return;
        }
        var form = $("appendixVICForm");
        pcb.loadAppendixVICPreviousYearReference(form ? form.getAttribute("data-demand-id") : null, event.target.value);
    });

    // ----------------------------------------------------------------------------------------
    // Reset handling for Edit mode: restore original Autonomous Body value on form reset
    // so users cannot lose the original selection in Edit mode.
    // ----------------------------------------------------------------------------------------
    document.addEventListener("reset", function (event) {
        var form = event.target;
        if (form.id !== "appendixVICForm") {
            return;
        }
        // If in Edit mode and we have a stored original Autonomous Body value, restore it
        if (originalAutonomousBodyValue !== null) {
            var autonomousBodyField = $("NewRecord_AutonomousBodyId");
            if (autonomousBodyField) {
                // Use setTimeout to ensure restore happens after form reset completes
                setTimeout(function () {
                    autonomousBodyField.value = originalAutonomousBodyValue;
                    autonomousBodyField.disabled = true;
                }, 0);
            }
        }
    }, true);

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

        if (event.target.closest('[data-action="open-appendix-vic-add-drawer"]')) {
            openAppendixVicDrawer("add");
            return;
        }

        var editVICButton = event.target.closest('[data-action="edit-appendix-vic-record"]');
        if (editVICButton) {
            openAppendixVicDrawer("edit", editVICButton);
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-vic-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var vicCancelForm = $("appendixVICForm");
            var vicPcb = window.PreBudgetControlBinding;
            var vicIsDirty = !vicPcb || typeof vicPcb.isFormDirtySinceOpen !== "function" || vicPcb.isFormDirtySinceOpen(vicCancelForm);
            if (!vicIsDirty) {
                closeAppendixVicDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixVicDrawer();
                }, false, "No");
            } else {
                closeAppendixVicDrawer();
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

        if (event.target.closest("#vicColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#vicColumnsButton") && !event.target.closest("#vicColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-vic-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-vic"]');
        if (exportOptionBtn) {
            exportAppendixVic(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#vicExportButton") && !event.target.closest("#vicExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-vic-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-vic-trigger"]');
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

        if (event.target.closest("#vicCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#vicConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-vic-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-vic-filters"]')) {
            clearAppendixVicFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-vic-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-vic-prev-page"]')) {
            changeAppendixVicPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-vic-next-page"]')) {
            changeAppendixVicPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-vic-goto-page"]');
        if (pageBtn) {
            goToAppendixVicPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#chargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        var segBtn = event.target.closest("#appendixVICForm .seg-btn-vicpub[data-group]");
        if (segBtn) {
            setSegment(segBtn);
            return;
        }

        if (event.target.closest("#vicToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#vicColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "vicStatusFilter") {
            vicCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "vicPageSize") {
            changeAppendixVicPageSize();
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "vicSearchInput") {
            vicCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("vicExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("vicColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("vicDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("vicDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixVicDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__vicHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("gridBody") && $("appendixVICForm")) {
                vicCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentVicDrawerToBody();
        };
        window.PreBudgetControlBinding.__vicHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("gridBody") && $("appendixVICForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentVicDrawerToBody();
})();
