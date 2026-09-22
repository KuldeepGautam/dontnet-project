// New appendix (2026-09-09, client instruction, UBIS-Pre-budget-html/Appendix6f.html) - drawer/grid
// interaction layer for Appendix VI-F. Mirrors appendix-vie-drawer.js's own structure (see that
// file's header comment, and appendix-via-drawer.js's for the original reasoning behind each piece:
// drawer slide-in, columns picker, row action menu, delete dialog, toast, search/status filter +
// pagination, Export dropdown).
//
// Unlike every other redesigned appendix, VI-F has no shared PreBudget-control-binding.js function
// to lean on for Edit-populate (it's a brand-new appendix, not a reskin of an existing one) - this
// file owns that too, plus the Minor Head autocomplete textbox (client requirement 2026-09-09:
// "Instead of dropdown, use textbox, user type in and after 4 characters, automatic suggestion...
// comes. On selected/typed in correct id, its name should [be] shown in a label").
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("vifToast");
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
        $("vifToastTitle").textContent = variant.title;
        $("vifToastIcon").setAttribute("data-lucide", variant.icon);
        $("vifToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("vifToast");
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
        var menu = $("vifExportMenu");
        var button = $("vifExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("vifExportMenu");
        var button = $("vifExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    // 2026-09-15 (client instruction: "export functionality should depend of columns of grid
    // being export, not hard-code... any update on grid columns should not change export
    // functionality") - was its own fetch to a hand-maintained ExportAppendixVIF MVC action with a
    // separate C# column list; now delegates to the shared wwwroot/js/grid-export.js, which scrapes
    // the live #chargesTable DOM (headers, visible columns, right-alignment) so this grid's export
    // never needs touching again as its columns evolve.
    function exportAppendixVIF(format, triggerButton) {
        var form = $("appendixVIFForm");
        var demandId = form ? form.getAttribute("data-demand-id") : null;
        closeExportMenu();

        var originalHtml = triggerButton ? triggerButton.innerHTML : null;
        if (triggerButton) {
            triggerButton.innerHTML = '<span class="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-slate-300 border-t-ubis-teal"></span><span>Generating...</span>';
            triggerButton.disabled = true;
        }

        function reset() {
            if (triggerButton && originalHtml !== null) {
                triggerButton.innerHTML = originalHtml;
                triggerButton.disabled = false;
            }
        }

        window.UbisGridExport.exportTable({
            tableSelector: "#chargesTable",
            format: format,
            title: "Appendix VI-F - User Charges of Ministries/Departments",
            subtitle: "Demand ID: " + (demandId || ""),
            fileName: "AppendixVIF-" + new Date().toISOString().replace(/[:.]/g, "-"),
            onError: function (message) {
                showAppendixToast(message, "error");
                reset();
            }
        });
        // exportTable's own fetch resolves/downloads asynchronously with no completion callback on
        // success (a blob download has no further UI state to reach) - reset the button once the
        // browser's had a moment to start the download rather than leaving it spinning forever.
        setTimeout(function () {
            reset();
            showAppendixToast("Report has been downloaded successfully.", "success");
        }, 800);
    }

    // ----------------------------------------------------------------------------------------
    // Columns menu
    // ----------------------------------------------------------------------------------------
    function toggleColumnsMenu(event) {
        event.stopPropagation();
        var menu = $("vifColumnsMenu");
        var button = $("vifColumnsButton");
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
        var menu = $("vifColumnsMenu");
        var button = $("vifColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("vifColumnsMenu");
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
        var menu = $("vifColumnsMenu");
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
        var menu = $("vif-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("vif-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("vif-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "vifGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Minor Head autocomplete (client requirement 2026-09-09 - see this file's header comment).
    // ----------------------------------------------------------------------------------------
    var minorHeadSearchTimer;
    var minorHeadAbortController;

    function clearMinorHeadSuggestions() {
        var box = $("vifMinorHeadSuggestions");
        if (box) {
            box.classList.add("hidden");
            box.innerHTML = "";
        }
    }

    function renderMinorHeadSuggestions(codes) {
        var box = $("vifMinorHeadSuggestions");
        if (!box) {
            return;
        }
        if (!codes.length) {
            clearMinorHeadSuggestions();
            return;
        }
        box.innerHTML = "";
        codes.forEach(function (item) {
            var row = document.createElement("button");
            row.type = "button";
            row.className = "block w-full px-3 py-2 text-left text-xs font-semibold text-slate-700 hover:bg-indigo-50 hover:text-indigo-700";
            row.textContent = item.code;
            row.addEventListener("click", function () {
                var input = $("vifMinorHeadInput");
                if (input) {
                    input.value = item.code;
                }
                clearMinorHeadSuggestions();
                resolveMinorHeadName(item.code);
            });
            box.appendChild(row);
        });
        box.classList.remove("hidden");
    }

    function searchMinorHeads(query) {
        var form = $("appendixVIFForm");
        var demandId = form ? form.getAttribute("data-demand-id") : null;
        if (!demandId || !query || query.length < 4) {
            clearMinorHeadSuggestions();
            return;
        }
        if (minorHeadAbortController) {
            minorHeadAbortController.abort();
        }
        minorHeadAbortController = new AbortController();
        var url = "/PreBudgetMeeting/PreBudgetMeeting/SearchAppendixVIFMinorHeads?demandId=" + encodeURIComponent(demandId) + "&query=" + encodeURIComponent(query);
        fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" }, signal: minorHeadAbortController.signal })
            .then(function (response) { return response.ok ? response.json() : []; })
            .then(function (items) { renderMinorHeadSuggestions(items || []); })
            .catch(function (err) {
                if (err && err.name !== "AbortError") {
                    clearMinorHeadSuggestions();
                }
            });
    }

    // Guards against duplicate/overlapping requests - picking a suggestion (click) also blurs the
    // input, so both the click handler and the blur listener below would otherwise fire this at
    // once; whichever response lands last would silently win, occasionally showing "No name found"
    // even when the code does resolve. Tracking the in-flight code and aborting any earlier request
    // makes this idempotent regardless of how many callers ask for the same code in quick succession.
    var minorHeadNameAbortController;
    var lastRequestedMinorHeadCode;

    function resolveMinorHeadName(code) {
        var label = $("vifMinorHeadNameLabel");
        if (!label) {
            return;
        }
        if (!code || code.length < 4) {
            label.textContent = "";
            lastRequestedMinorHeadCode = null;
            return;
        }
        if (code === lastRequestedMinorHeadCode) {
            return;
        }
        lastRequestedMinorHeadCode = code;
        if (minorHeadNameAbortController) {
            minorHeadNameAbortController.abort();
        }
        minorHeadNameAbortController = new AbortController();
        var url = "/PreBudgetMeeting/PreBudgetMeeting/GetAppendixVIFMinorHeadName?minorHeadCode=" + encodeURIComponent(code);
        fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" }, signal: minorHeadNameAbortController.signal })
            .then(function (response) { return response.ok ? response.json() : { found: false }; })
            .then(function (result) {
                label.textContent = result && result.found ? result.name : "No name found for this Minor Head.";
            })
            .catch(function (err) {
                if (err && err.name !== "AbortError") {
                    label.textContent = "";
                }
            });
    }

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit)
    // ----------------------------------------------------------------------------------------
    function resetVifForm() {
        lastRequestedMinorHeadCode = null;
        var form = $("appendixVIFForm");
        if (form) {
            form.reset();
        }
        var idField = $("NewRecord_Id");
        if (idField) {
            idField.value = "";
        }
        var label = $("vifMinorHeadNameLabel");
        if (label) {
            label.textContent = "";
        }
        clearMinorHeadSuggestions();
        unlockVifMinorHead();
        restoreVifSaveButtonLabel();
    }

    // Client requirement 2026-09-14: "on edit mode, disable minor head textbox, and button to
    // Modify" - Minor Head is part of the record's identity (the grid/duplicate check is keyed on
    // it), same convention as every other appendix's identity field (VI-A's Title of Charge). Uses
    // readOnly, not disabled: a disabled control is dropped from FormData entirely on submit,
    // while readOnly still posts its value with no hidden-mirror plumbing needed.
    function lockVifMinorHead() {
        var minorHeadInput = $("vifMinorHeadInput");
        if (minorHeadInput) {
            minorHeadInput.readOnly = true;
        }
    }

    function unlockVifMinorHead() {
        var minorHeadInput = $("vifMinorHeadInput");
        if (minorHeadInput) {
            minorHeadInput.readOnly = false;
        }
    }

    function setVifSaveButtonModifyLabel() {
        var form = $("appendixVIFForm");
        var submitBtn = form ? form.querySelector('button[type="submit"]') : null;
        if (!submitBtn) {
            return;
        }
        if (!submitBtn.hasAttribute("data-original-label")) {
            submitBtn.setAttribute("data-original-label", submitBtn.innerHTML);
        }
        submitBtn.innerHTML = submitBtn.getAttribute("data-original-label").replace(/Save Record|Submit/i, "Modify");
    }

    function restoreVifSaveButtonLabel() {
        var form = $("appendixVIFForm");
        var submitBtn = form ? form.querySelector('button[type="submit"]') : null;
        if (submitBtn && submitBtn.hasAttribute("data-original-label")) {
            submitBtn.innerHTML = submitBtn.getAttribute("data-original-label");
        }
    }

    function openAppendixVIFDrawer(mode) {
        var drawer = $("vifDrawer");
        var backdrop = $("vifDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }

        if (mode === "add") {
            resetVifForm();
        }

        var titleEl = $("vifDrawerTitle");
        var subtitleEl = $("vifDrawerSubtitle");
        if (titleEl) {
            titleEl.textContent = mode === "edit" ? "Edit Record" : "Add Record";
        }
        if (subtitleEl) {
            subtitleEl.textContent = mode === "edit" ? "Update the selected record" : "Create a new record";
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
            window.PreBudgetControlBinding.captureCancelSnapshot($("appendixVIFForm"));
        }

        setTimeout(function () {
            var firstField = $("vifMinorHeadInput");
            if (firstField && !firstField.readOnly && !firstField.disabled) {
                firstField.focus();
            }
        }, 320);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function populateVifEntryForm(editButton) {
        var idField = $("NewRecord_Id");
        var minorHeadInput = $("vifMinorHeadInput");
        if (!idField || !minorHeadInput) {
            return;
        }

        idField.value = editButton.getAttribute("data-id") || "";
        minorHeadInput.value = editButton.getAttribute("data-minor-head-code") || "";

        var label = $("vifMinorHeadNameLabel");
        if (label) {
            label.textContent = editButton.getAttribute("data-minor-head-name") || "";
        }

        [
            ["NewRecord_BriefOnReceipts", "data-brief-on-receipts"],
            ["NewRecord_PresentStatus", "data-present-status"],
            ["NewRecord_NoOfTransactions", "data-no-of-transactions"],
            ["NewRecord_RateOfService", "data-rate-of-service"],
            ["NewRecord_ReceiptsCollection", "data-receipts-collection"],
            ["NewRecord_ActionTakenPlan", "data-action-taken-plan"]
        ].forEach(function (pair) {
            var input = $(pair[0]);
            if (input) {
                input.value = editButton.getAttribute(pair[1]) || "";
            }
        });

        lockVifMinorHead();
        setVifSaveButtonModifyLabel();
    }

    // Same fix as every other redesigned appendix's own drawer reparenting - see
    // appendix-via-drawer.js's comment for the full reasoning (drawer painted behind the app
    // shell's masthead otherwise).
    function reparentVifDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="vifDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="vifDrawerBackdrop"]');
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

    function closeAppendixVIFDrawer() {
        var drawer = $("vifDrawer");
        var backdrop = $("vifDrawerBackdrop");
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
        var dialog = $("vifDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("vifDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("vifCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("vifDeleteDialog");
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

    var vifCurrentPage = 1;
    var vifPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("vifSearchInput");
        var statusEl = $("vifStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / vifPageSize));
        if (vifCurrentPage > totalPages) {
            vifCurrentPage = totalPages;
        }
        var start = (vifCurrentPage - 1) * vifPageSize;
        var pageRowSet = matched.slice(start, start + vifPageSize);

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

        var empty = $("vifEmptyState");
        var gridWrap = $("vifGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("vifTotalText");
        var rangeText = $("vifRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixVIFPagination(totalPages);
    }

    function renderAppendixVIFPagination(totalPages) {
        var holder = $("vifPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - vifCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-vif-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === vifCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("vifPrevBtn");
        var nextBtn = $("vifNextBtn");
        if (prevBtn) {
            prevBtn.disabled = vifCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", vifCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = vifCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", vifCurrentPage === totalPages);
        }
    }

    function goToAppendixVIFPage(page) {
        vifCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixVIFPage(delta) {
        goToAppendixVIFPage(vifCurrentPage + delta);
    }

    function changeAppendixVIFPageSize() {
        var sizeEl = $("vifPageSize");
        vifPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        vifCurrentPage = 1;
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

    function clearAppendixVIFFilters() {
        var searchEl = $("vifSearchInput");
        var statusEl = $("vifStatusFilter");
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
        // III-B/VI-B/VI-G by intercepting the button CLICK instead (before any "reset" event can
        // fire). While still in Edit mode, prevent the native reset entirely and blank only the
        // plain editable fields ourselves - the locked Minor Head input and the record id are left
        // completely untouched.
        var resetVifBtn = event.target.closest('#appendixVIFForm button[type="reset"]');
        if (resetVifBtn) {
            var idFieldForReset = $("NewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                ["NewRecord_BriefOnReceipts", "NewRecord_PresentStatus", "NewRecord_NoOfTransactions", "NewRecord_RateOfService", "NewRecord_ReceiptsCollection", "NewRecord_ActionTakenPlan"].forEach(function (id) {
                    var field = $(id);
                    if (field) { field.value = ""; }
                });
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-vif-add-drawer"]')) {
            openAppendixVIFDrawer("add");
            return;
        }

        var editVifButton = event.target.closest('[data-action="edit-appendix-vif-record"]');
        if (editVifButton) {
            populateVifEntryForm(editVifButton);
            openAppendixVIFDrawer("edit");
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-vif-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var vifCancelForm = $("appendixVIFForm");
            var vifPcb = window.PreBudgetControlBinding;
            var vifIsDirty = !vifPcb || typeof vifPcb.isFormDirtySinceOpen !== "function" || vifPcb.isFormDirtySinceOpen(vifCancelForm);
            if (!vifIsDirty) {
                closeAppendixVIFDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixVIFDrawer();
                }, false, "No");
            } else {
                closeAppendixVIFDrawer();
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

        if (event.target.closest("#vifColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#vifColumnsButton") && !event.target.closest("#vifColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-vif-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-vif"]');
        if (exportOptionBtn) {
            exportAppendixVIF(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#vifExportButton") && !event.target.closest("#vifExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-vif-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-vif-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#vifCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#vifConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-vif-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-vif-filters"]')) {
            clearAppendixVIFFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-vif-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-vif-prev-page"]')) {
            changeAppendixVIFPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-vif-next-page"]')) {
            changeAppendixVIFPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-vif-goto-page"]');
        if (pageBtn) {
            goToAppendixVIFPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#chargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#vifToastClose")) {
            hideAppendixToast();
        }

        if (!event.target.closest("#vifMinorHeadInput") && !event.target.closest("#vifMinorHeadSuggestions")) {
            clearMinorHeadSuggestions();
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "vifSearchInput") {
            vifCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "vifMinorHeadInput") {
            clearTimeout(minorHeadSearchTimer);
            var value = event.target.value.trim();
            minorHeadSearchTimer = setTimeout(function () { searchMinorHeads(value); }, 250);
            if (value.length >= 4) {
                var label = $("vifMinorHeadNameLabel");
                if (label) {
                    label.textContent = "";
                }
            }
        }
    });

    document.addEventListener("blur", function (event) {
        if (event.target && event.target.id === "vifMinorHeadInput") {
            var value = event.target.value.trim();
            if (value.length >= 4) {
                resolveMinorHeadName(value);
            }
        }
    }, true);

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#vifColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "vifStatusFilter") {
            vifCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "vifPageSize") {
            changeAppendixVIFPageSize();
        }
    });

    // Bug fix / requirement 2026-09-14: a native <button type="reset"> restores every control's
    // page-load initial value, including a readOnly-but-not-disabled Minor Head input (readOnly
    // doesn't exempt a control from form reset the way `disabled` exempts it from FormData) - so
    // Reset would blank the locked Minor Head while Edit mode was still active. Preserve the
    // record id, the Minor Head code/name and the locked state + "Modify" button label when Reset
    // is pressed while still editing; a fresh Add's Reset keeps the old unconditional-clear
    // behavior.
    document.addEventListener("reset", function (event) {
        if (!event.target || event.target.id !== "appendixVIFForm") {
            return;
        }
        var idField = $("NewRecord_Id");
        var wasEditMode = !!(idField && idField.value);
        var minorHeadInput = $("vifMinorHeadInput");
        var nameLabel = $("vifMinorHeadNameLabel");
        var preservedId = wasEditMode ? idField.value : null;
        var preservedMinorHeadCode = wasEditMode && minorHeadInput ? minorHeadInput.value : null;
        var preservedMinorHeadName = wasEditMode && nameLabel ? nameLabel.textContent : null;

        window.setTimeout(function () {
            if (!wasEditMode) {
                if (idField) {
                    idField.value = "";
                }
                if (nameLabel) {
                    nameLabel.textContent = "";
                }
                clearMinorHeadSuggestions();
                return;
            }
            if (idField) {
                idField.value = preservedId;
            }
            if (minorHeadInput) {
                minorHeadInput.value = preservedMinorHeadCode;
            }
            if (nameLabel) {
                nameLabel.textContent = preservedMinorHeadName;
            }
            lockVifMinorHead();
            setVifSaveButtonModifyLabel();
            clearMinorHeadSuggestions();
        }, 0);
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("vifExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("vifColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var suggestions = $("vifMinorHeadSuggestions");
        if (suggestions && !suggestions.classList.contains("hidden")) {
            clearMinorHeadSuggestions();
            return;
        }
        var dialog = $("vifDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("vifDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixVIFDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__vifHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("gridBody") && $("appendixVIFForm")) {
                vifCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentVifDrawerToBody();
        };
        window.PreBudgetControlBinding.__vifHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("gridBody") && $("appendixVIFForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentVifDrawerToBody();
})();
