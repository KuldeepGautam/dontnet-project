// Re-design 2026-09-15 (designer reference: UBIS-drawer-070926 "appendix-IV.html") - drawer/grid
// interaction layer for Appendix IV. Mirrors appendix-iva-drawer.js's structure exactly (same
// drawer slide-in, columns picker, row action menu, delete dialog, toast, Search-only filter +
// pagination pattern - see appendix-via-drawer.js's header comment for the original reasoning
// behind each piece). No Export dropdown, no Demand/Financial Year/Status filters - kept
// consistent with every other redesigned appendix (client decision 2026-09-15: match the
// established Search-only convention rather than the designer mockup's fuller filter bar).
//
// This file owns ONLY the drawer/grid chrome (see ubis2_appendix_redesign_reuse_shared_files
// memory). The Scheme -> Sub-Scheme cascade, the B.E. auto-load from SBE, and the live
// %-w.r.t-B.E./Addl-R.E.-Sought/Addl-N.B.E.-Sought calcs (recalcAppendixIVCalculations) and the
// Edit-populate field mapping are ALL already generic, shared logic in
// PreBudget-control-binding.js (populateSchemeGradedEntryForm / schemeGradedAppendixForms,
// already wired for data-action="edit-appendix-iv-record" and #appendixIVForm on
// `document`-scoped listeners) - none of that is duplicated or modified here; keeping the same
// field/element ids (NewRecord_SchemeId, NewRecord_SubSchemeId, ivPercentWrtBE, ivAddlReSought,
// ivAddlNbeSought, form id appendixIVForm, ...) is what keeps it working unchanged after this
// reparents the form to document.body.
//
// Appendix IV has NO single-record rule (duplicate check keys on Scheme(+SubScheme), not
// one-per-Demand). Row action menu = Edit / Delete only - no Freeze (Freeze is header-level, same
// convention as every other appendix this session).
//
// Scheme/Sub-Scheme ARE locked during Edit (client requirement 2026-09-14, overriding the earlier
// "not locked" design this comment used to document) - see populateAppendixIVEntryForm's own
// lockSelectForEditScoped calls and this file's own Reset-click interception below.
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("ivToast");
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
        $("ivToastTitle").textContent = variant.title;
        $("ivToastIcon").setAttribute("data-lucide", variant.icon);
        $("ivToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("ivToast");
        if (!toast) {
            return;
        }
        toast.classList.remove("ubis-toast-show");
        toast.classList.add("ubis-toast-hide");
    }

    // ----------------------------------------------------------------------------------------
    // Export dropdown (client requirement 2026-09-17: "same functionality as Appendix III") -
    // delegates to the shared wwwroot/js/grid-export.js, which scrapes the live #ivChargesTable
    // DOM directly (columns-driven, matches whatever the Columns menu currently shows), same
    // pattern as Appendix III/V-A/V-B/V-C's own Export.
    // ----------------------------------------------------------------------------------------
    function toggleExportMenu(event) {
        event.stopPropagation();
        var menu = $("ivExportMenu");
        var button = $("ivExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("ivExportMenu");
        var button = $("ivExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    // Client requirement 2026-09-18: export must match the government circular's own Appendix-IV
    // table exactly (grouped by Category, with autogenerated Total rows) - a shape the old
    // scrape-the-visible-grid exporter (window.UbisGridExport.exportTable) can't produce, since the
    // circular's grouping/columns/totals don't exist on the live grid at all. Switched to the same
    // real-controller-action fetch pattern as ExportAppendixIA/ExportAppendixIIIA/ExportAppendixIIIB.
    function exportAppendixIV(format, triggerButton) {
        var form = $("appendixIVForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixIV?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

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
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixIV." + format);
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
        var menu = $("ivColumnsMenu");
        var button = $("ivColumnsButton");
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
        var menu = $("ivColumnsMenu");
        var button = $("ivColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("ivColumnsMenu");
        if (!menu) {
            return;
        }
        Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (checkbox) {
            var column = checkbox.getAttribute("data-column-toggle");
            var show = checkbox.checked;
            document.querySelectorAll('#ivChargesTable [data-column="' + column + '"]').forEach(function (cell) {
                cell.classList.toggle("column-hidden", !show);
            });
        });
    }

    function resetColumns() {
        var menu = $("ivColumnsMenu");
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
        var menu = $("iv-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("iv-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("iv-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "ivGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit) - shell only.
    // ----------------------------------------------------------------------------------------
    function openAppendixIVDrawer(mode) {
        var drawer = $("ivDrawer");
        var backdrop = $("ivDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixIVForm");

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
        }

        var titleEl = $("ivDrawerTitle");
        var subtitleEl = $("ivDrawerSubtitle");
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
            var firstField = $("NewRecord_SchemeId");
            if (firstField && !firstField.readOnly && !firstField.disabled) {
                firstField.focus();
            }
        }, 320);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function reparentIvaDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="ivDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="ivDrawerBackdrop"]');
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

    function closeAppendixIVDrawer() {
        var drawer = $("ivDrawer");
        var backdrop = $("ivDrawerBackdrop");
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
        var dialog = $("ivDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("ivDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("ivCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("ivDeleteDialog");
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
        var body = $("ivGridBody");
        return body ? Array.prototype.slice.call(body.querySelectorAll("tr[data-row]")) : [];
    }

    var ivCurrentPage = 1;
    var ivPageSize = 10;

    // Client instruction 2026-09-21 (follow-up #2): the Scheme/Sub-Scheme dropdown *options*
    // must reflect what's actually present in the grid, not the full unfiltered Model.Records
    // list - "category type shows data that is in grid... scheme names are based on selected
    // category that are in grid... sub-scheme depend on selected scheme plus data in grid".
    // i.e. Category Type always lists every value in the grid; once a Category is picked, Scheme
    // only lists schemes that still have a matching row; once a Scheme is picked too, Sub-Scheme
    // only lists sub-schemes that still have a matching row. Recomputed from the live <tr> rows'
    // data-category-type/data-scheme-id/data-sub-scheme-id attributes (plus the current
    // Search/Status filters, so those narrow the dropdowns too) every time applyGridFilters runs -
    // cheap since these grids are small, and keeps the dropdowns honest without an AJAX round trip
    // (unlike Appendix III's server-backed cascade).
    function rowMatchesForOptions(row, upstream) {
        var searchEl = $("ivSearchInput");
        var statusEl = $("ivStatusFilter");
        var search = (searchEl ? searchEl.value : "").trim().toLowerCase();
        var status = statusEl ? statusEl.value : "All";
        var haystack = (row.getAttribute("data-search") || "").toLowerCase();
        var rowStatus = row.getAttribute("data-status") || "Active";
        if (search && haystack.indexOf(search) === -1) {
            return false;
        }
        if (status !== "All" && rowStatus !== status) {
            return false;
        }
        if (upstream.categoryType && row.getAttribute("data-category-type") !== upstream.categoryType) {
            return false;
        }
        if (upstream.scheme && upstream.scheme !== "All" && row.getAttribute("data-scheme-id") !== upstream.scheme) {
            return false;
        }
        return true;
    }

    function rebuildAppendixIVDependentFilterOptions() {
        var categoryTypeEl = $("ivCategoryTypeFilter");
        var schemeEl = $("ivSchemeFilter");
        var subSchemeEl = $("ivSubSchemeFilter");
        if (!schemeEl || !subSchemeEl) {
            return;
        }
        var rows = currentGridRows();
        var categoryType = categoryTypeEl ? categoryTypeEl.value : "";

        var validSchemeIds = {};
        rows.forEach(function (row) {
            if (rowMatchesForOptions(row, { categoryType: categoryType })) {
                var sid = row.getAttribute("data-scheme-id");
                if (sid) {
                    validSchemeIds[sid] = true;
                }
            }
        });
        var schemeStillValid = false;
        Array.prototype.forEach.call(schemeEl.querySelectorAll("option"), function (option) {
            if (option.value === "All") {
                return;
            }
            var belongs = !!validSchemeIds[option.value];
            option.classList.toggle("hidden", !belongs);
            option.disabled = !belongs;
            if (belongs && option.value === schemeEl.value) {
                schemeStillValid = true;
            }
        });
        if (schemeEl.value !== "All" && !schemeStillValid) {
            schemeEl.value = "All";
        }

        var selectedScheme = schemeEl.value;
        var validSubSchemeIds = {};
        rows.forEach(function (row) {
            if (rowMatchesForOptions(row, { categoryType: categoryType, scheme: selectedScheme })) {
                var ssid = row.getAttribute("data-sub-scheme-id");
                if (ssid) {
                    validSubSchemeIds[ssid] = true;
                }
            }
        });
        var subSchemeStillValid = false;
        Array.prototype.forEach.call(subSchemeEl.querySelectorAll("option[data-scheme-id]"), function (option) {
            var belongsToScheme = selectedScheme === "All" || option.getAttribute("data-scheme-id") === selectedScheme;
            var presentInGrid = !!validSubSchemeIds[option.value];
            var belongs = belongsToScheme && presentInGrid;
            option.classList.toggle("hidden", !belongs);
            option.disabled = !belongs;
            if (belongs && option.value === subSchemeEl.value) {
                subSchemeStillValid = true;
            }
        });
        if (subSchemeEl.value !== "All" && !subSchemeStillValid) {
            subSchemeEl.value = "All";
        }
    }

    function applyGridFilters() {
        rebuildAppendixIVDependentFilterOptions();
        var rows = currentGridRows();
        var searchEl = $("ivSearchInput");
        var statusEl = $("ivStatusFilter");
        var categoryTypeEl = $("ivCategoryTypeFilter");
        var schemeEl = $("ivSchemeFilter");
        var subSchemeEl = $("ivSubSchemeFilter");
        var search = (searchEl ? searchEl.value : "").trim().toLowerCase();
        var status = statusEl ? statusEl.value : "All";
        // Category Type's default option is value="" ("Select", matching the merged design) rather
        // than an explicit "All" sentinel like Scheme/Sub-Scheme use - both mean "no filter".
        var categoryType = categoryTypeEl ? categoryTypeEl.value : "";
        var scheme = schemeEl ? schemeEl.value : "All";
        var subScheme = subSchemeEl ? subSchemeEl.value : "All";

        var matched = [];
        rows.forEach(function (row) {
            var haystack = (row.getAttribute("data-search") || "").toLowerCase();
            var rowStatus = row.getAttribute("data-status") || "Active";
            var matchesSearch = !search || haystack.indexOf(search) !== -1;
            var matchesStatus = status === "All" || rowStatus === status;
            var matchesCategoryType = !categoryType || row.getAttribute("data-category-type") === categoryType;
            var matchesScheme = scheme === "All" || row.getAttribute("data-scheme-id") === scheme;
            var matchesSubScheme = subScheme === "All" || row.getAttribute("data-sub-scheme-id") === subScheme;
            if (matchesSearch && matchesStatus && matchesCategoryType && matchesScheme && matchesSubScheme) {
                matched.push(row);
            } else {
                row.classList.add("hidden");
            }
        });

        var totalPages = Math.max(1, Math.ceil(matched.length / ivPageSize));
        if (ivCurrentPage > totalPages) {
            ivCurrentPage = totalPages;
        }
        var start = (ivCurrentPage - 1) * ivPageSize;
        var pageRowSet = matched.slice(start, start + ivPageSize);

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

        var empty = $("ivEmptyState");
        var gridWrap = $("ivGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("ivTotalText");
        var rangeText = $("ivRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixIVPagination(totalPages);
    }

    function renderAppendixIVPagination(totalPages) {
        var holder = $("ivPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - ivCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-iv-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === ivCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("ivPrevBtn");
        var nextBtn = $("ivNextBtn");
        if (prevBtn) {
            prevBtn.disabled = ivCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", ivCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = ivCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", ivCurrentPage === totalPages);
        }
    }

    function goToAppendixIVPage(page) {
        ivCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixIVPage(delta) {
        goToAppendixIVPage(ivCurrentPage + delta);
    }

    function changeAppendixIVPageSize() {
        var sizeEl = $("ivPageSize");
        ivPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        ivCurrentPage = 1;
        applyGridFilters();
    }

    var sortKey = null;
    var sortDir = "asc";

    function sortGridBy(key) {
        var body = $("ivGridBody");
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
            var cmp = av.toLowerCase().localeCompare(bv.toLowerCase());
            return sortDir === "asc" ? cmp : -cmp;
        });
        rows.forEach(function (row) { body.appendChild(row); });
        applyGridFilters();
    }

    function clearAppendixIVFilters() {
        var searchEl = $("ivSearchInput");
        var statusEl = $("ivStatusFilter");
        var categoryTypeEl = $("ivCategoryTypeFilter");
        var schemeEl = $("ivSchemeFilter");
        var subSchemeEl = $("ivSubSchemeFilter");
        if (searchEl) {
            searchEl.value = "";
        }
        if (statusEl) {
            statusEl.value = "All";
        }
        if (categoryTypeEl) {
            categoryTypeEl.value = "";
        }
        if (schemeEl) {
            schemeEl.value = "All";
        }
        if (subSchemeEl) {
            subSchemeEl.value = "All";
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

        // Client requirement 2026-09-14: "edit mode scheme and subscheme should be disabled. on
        // click reset, these dropdown remains disabled and retain their value" - both are locked
        // via lockSelectForEditScoped in PreBudget-control-binding.js's populateAppendixIVEntryForm
        // (a deliberate change from this appendix's earlier "not locked" design - see this file's
        // own header comment). A native <button type="reset"> restores every control's page-load
        // initial value regardless (blanking them while leaving them disabled), and the shared
        // generic reset listener (any form inside .ubis-modern-appendix) unconditionally
        // hard-clears the form on top of that - racing either with a "reset" event listener proved
        // unreliable elsewhere (see Appendix III-B/VI-B/VI-D/VI-E/VI-F/VI-G/VI/VII-A/VII-B/VI-A's
        // own comments for the confirmed race). Intercept the button CLICK instead and, while
        // still in Edit mode, prevent the native reset entirely and blank only the plain editable
        // fields ourselves - Scheme, Sub-Scheme and the record id are left completely untouched;
        // B.E. is auto-loaded/readonly and stays untouched too (Scheme/Sub-Scheme aren't changing,
        // so it's still correct). Dispatches input+change on the blanked fields afterward so the
        // existing document-scoped %-w.r.t-B.E. formula recalc (recalcAppendixIVCalculations)
        // picks up the now-empty values, same mechanism hardClearAppendixForm itself already uses.
        var resetIvBtn = event.target.closest('#appendixIVForm button[type="reset"]');
        if (resetIvBtn) {
            var idFieldForReset = $("NewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                ["NewRecord_Actuals", "NewRecord_ActualsUptoSeptPrevYear", "NewRecord_ActualsUptoSept", "NewRecord_ProposedNBE", "NewRecord_RemarksMinistry", "NewRecord_RemarksBudget", "NewRecord_BudgetRecommendedRE", "NewRecord_BudgetRecommendedNBE"].forEach(function (id) {
                    var field = $(id);
                    if (!field) {
                        return;
                    }
                    field.value = "";
                    field.dispatchEvent(new Event("input", { bubbles: true }));
                    field.dispatchEvent(new Event("change", { bubbles: true }));
                });
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-iv-add-drawer"]')) {
            openAppendixIVDrawer("add");
            return;
        }

        var editIVButton = event.target.closest('[data-action="edit-appendix-iv-record"]');
        if (editIVButton) {
            // Field population + the Scheme->Sub-Scheme cascade + BE auto-load + Saving/Excess
            // recalc is handled by PreBudget-control-binding.js's own (document-scoped, unchanged)
            // dispatch for this same data-action - this just slides the drawer open.
            openAppendixIVDrawer("edit");
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-iv-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var ivCancelForm = $("appendixIVForm");
            var ivPcb = window.PreBudgetControlBinding;
            var ivIsDirty = !ivPcb || typeof ivPcb.isFormDirtySinceOpen !== "function" || ivPcb.isFormDirtySinceOpen(ivCancelForm);
            if (!ivIsDirty) {
                closeAppendixIVDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixIVDrawer();
                }, false, "No");
            } else {
                closeAppendixIVDrawer();
            }
            return;
        }

        var rowMenuBtn = event.target.closest(".row-menu-btn[data-action-id]");
        if (rowMenuBtn && rowMenuBtn.closest("#ivChargesTable")) {
            toggleActionMenu(rowMenuBtn.getAttribute("data-action-id"), rowMenuBtn, event);
            return;
        }
        if (!event.target.closest(".row-menu-btn") && !event.target.closest(".action-menu")) {
            document.querySelectorAll(".action-menu").forEach(function (m) { m.classList.add("hidden"); });
        }

        if (event.target.closest("#ivColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#ivColumnsButton") && !event.target.closest("#ivColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-iv-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var ivExportOptionBtn = event.target.closest('[data-action="export-appendix-iv"]');
        if (ivExportOptionBtn) {
            exportAppendixIV(ivExportOptionBtn.getAttribute("data-format"), ivExportOptionBtn);
            return;
        }
        if (!event.target.closest("#ivExportButton") && !event.target.closest("#ivExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-iv-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-iv-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#ivCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#ivConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-iv-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-iv-filters"]')) {
            clearAppendixIVFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-iv-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-iv-prev-page"]')) {
            changeAppendixIVPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-iv-next-page"]')) {
            changeAppendixIVPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-iv-goto-page"]');
        if (pageBtn) {
            goToAppendixIVPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#ivChargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#ivToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#ivColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "ivStatusFilter") {
            ivCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "ivCategoryTypeFilter") {
            ivCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "ivSchemeFilter") {
            ivCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "ivSubSchemeFilter") {
            ivCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "ivPageSize") {
            changeAppendixIVPageSize();
        }
    });

    // Native <button type="reset"> (and the Add drawer's own exitAppendixEditMode->form.reset())
    // wipes the previous-year auto-loaded B.E. without re-fetching it, and resets Sub-Scheme back
    // to its placeholder without re-disabling it - re-run the Scheme change dispatch that
    // PreBudget-control-binding.js's own cascade/BE-auto-load listens on. Mostly a no-op guard
    // right after a reset (Scheme is blank then); the real re-fetch happens once the user re-picks
    // a Scheme.
    document.addEventListener("reset", function (event) {
        if (event.target && event.target.id === "appendixIVForm") {
            window.setTimeout(function () {
                var schemeField = $("NewRecord_SchemeId");
                if (schemeField && schemeField.value) {
                    schemeField.dispatchEvent(new Event("change", { bubbles: true }));
                }
            }, 0);
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "ivSearchInput") {
            ivCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var ivExportMenuEsc = $("ivExportMenu");
        if (ivExportMenuEsc && !ivExportMenuEsc.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("ivColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("ivDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("ivDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixIVDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__ivHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("ivGridBody") && $("appendixIVForm")) {
                ivCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentIvaDrawerToBody();
        };
        window.PreBudgetControlBinding.__ivHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("ivGridBody") && $("appendixIVForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentIvaDrawerToBody();
})();
