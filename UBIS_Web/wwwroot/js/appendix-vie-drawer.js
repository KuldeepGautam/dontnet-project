// "Approved design for all appendixes" (2026-09-08, UBIS-Pre-budget-html/Appendix6e.html's
// drawer/form section - the file's own GRID section is a leftover copy-paste of Appendix6d.html's
// mock content and was not used as a reference here, see AppendixVIE.cshtml's header comment) -
// drawer/grid interaction layer for the redesigned Appendix VI-E page. Mirrors
// appendix-vid-drawer.js's own structure (see that file's header comment, and
// appendix-via-drawer.js's for the original reasoning behind each piece: drawer slide-in, columns
// picker, row action menu, delete dialog, toast, search/status filter + pagination, Export
// dropdown). Add/Edit/Delete/Freeze/Nil all go through the SAME real POST actions
// (SaveAppendixVIE/DeleteAppendixVIE/FreezeAppendixTemplate/SetNilSubmission) the old
// inline-table view already used.
//
// Simpler than VI-D: no select-lock-on-edit requirement here (Autonomous Body isn't singled out
// for identity-locking the way VI-D's 2026-08-31 requirement calls for), just the previous-year
// reference auto-fill re-wired at document level (same reasoning as VI-C's own, smaller
// re-wiring - this drawer's own reparenting-to-body breaks container-scoped delegation).
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("vieToast");
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
        $("vieToastTitle").textContent = variant.title;
        $("vieToastIcon").setAttribute("data-lucide", variant.icon);
        $("vieToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("vieToast");
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
        var menu = $("vieExportMenu");
        var button = $("vieExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("vieExportMenu");
        var button = $("vieExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function exportAppendixVie(format, triggerButton) {
        var form = $("appendixVIEForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixVIE?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

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
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixVIE." + format);
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
        var menu = $("vieColumnsMenu");
        var button = $("vieColumnsButton");
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
        var menu = $("vieColumnsMenu");
        var button = $("vieColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("vieColumnsMenu");
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
        var menu = $("vieColumnsMenu");
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
        var menu = $("vie-menu-" + id);
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
        if (!menu || !menu.id || menu.id.indexOf("vie-menu-") !== 0) {
            return;
        }
        var id = menu.id.replace("vie-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "vieGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit)
    // ----------------------------------------------------------------------------------------
    var VIE_EDIT_FIELD_MAP = [
        ["NewRecord_AutonomousBodyId", "data-autonomous-body-id"],
        ["NewRecord_CorpusFundBalance1", "data-corpus-fund-balance1"],
        ["NewRecord_CorpusFundBalance2", "data-corpus-fund-balance2"],
        ["NewRecord_CorpusFundBankName", "data-corpus-fund-bank-name"],
        ["NewRecord_CorpusFundReason", "data-corpus-fund-reason"],
        ["NewRecord_GiaRE", "data-gia-re"],
        ["NewRecord_GiaBE", "data-gia-be"]
    ];

    function populateAppendixVieFieldsFromButton(editButton) {
        var idField = $("NewRecord_Id");
        if (idField) {
            idField.value = editButton.getAttribute("data-id") || "";
        }

        VIE_EDIT_FIELD_MAP.forEach(function (pair) {
            var input = $(pair[0]);
            if (input) {
                input.value = editButton.getAttribute(pair[1]) || "";
            }
        });

        var form = $("appendixVIEForm");
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

    function openAppendixVieDrawer(mode, editButton) {
        var drawer = $("vieDrawer");
        var backdrop = $("vieDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixVIEForm");

        // Bug fix 2026-09-14 (client report: "as autonomous body is already selected and disabled,
        // why this message is coming and avoiding to save record"): a plain `select.disabled = true`
        // drops the field from FormData entirely on submit, so the server's required-field check saw
        // an empty AutonomousBodyId. Use the shared lockSelectForEdit/unlockSelectsAfterEdit helpers
        // instead - they mirror the disabled select's value into a same-named hidden input so it still
        // posts. Same mechanism already used for VI-B/VI-A's own locked identity fields.
        if (form && window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.unlockSelectsAfterEdit === "function") {
            window.PreBudgetControlBinding.unlockSelectsAfterEdit(form);
        } else {
            var ddlReset = document.getElementById("NewRecord_AutonomousBodyId");
            if (ddlReset) {
                ddlReset.disabled = false;
            }
        }

        if (mode === "edit" && editButton) {
            populateAppendixVieFieldsFromButton(editButton);
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

        var titleEl = $("vieDrawerTitle");
        var subtitleEl = $("vieDrawerSubtitle");
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
            if (bodyField) {
                bodyField.focus();
            }
        }, 320);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    // Same fix as VI-A/VI-B/VI-C/VI-D's own drawer reparenting - see appendix-via-drawer.js's
    // comment for the full reasoning (drawer painted behind the app shell's masthead otherwise).
    function reparentVieDrawerToBody() {
        var allDrawers = document.querySelectorAll('[id="vieDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="vieDrawerBackdrop"]');
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

    function closeAppendixVieDrawer() {
        var drawer = $("vieDrawer");
        var backdrop = $("vieDrawerBackdrop");
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
        var dialog = $("vieDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("vieDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("vieCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("vieDeleteDialog");
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

    var vieCurrentPage = 1;
    var viePageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("vieSearchInput");
        var statusEl = $("vieStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / viePageSize));
        if (vieCurrentPage > totalPages) {
            vieCurrentPage = totalPages;
        }
        var start = (vieCurrentPage - 1) * viePageSize;
        var pageRowSet = matched.slice(start, start + viePageSize);

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

        var empty = $("vieEmptyState");
        var gridWrap = $("vieGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("vieTotalText");
        var rangeText = $("vieRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixViePagination(totalPages);
    }

    function renderAppendixViePagination(totalPages) {
        var holder = $("viePageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - vieCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-vie-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === vieCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("viePrevBtn");
        var nextBtn = $("vieNextBtn");
        if (prevBtn) {
            prevBtn.disabled = vieCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", vieCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = vieCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", vieCurrentPage === totalPages);
        }
    }

    function goToAppendixViePage(page) {
        vieCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixViePage(delta) {
        goToAppendixViePage(vieCurrentPage + delta);
    }

    function changeAppendixViePageSize() {
        var sizeEl = $("viePageSize");
        viePageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        vieCurrentPage = 1;
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

    function clearAppendixVieFilters() {
        var searchEl = $("vieSearchInput");
        var statusEl = $("vieStatusFilter");
        if (searchEl) {
            searchEl.value = "";
        }
        if (statusEl) {
            statusEl.value = "All";
        }
        applyGridFilters();
    }

    // ----------------------------------------------------------------------------------------
    // Previous-year-reference auto-fill on Autonomous Body change, re-wired at document level
    // (see file header comment) since the drawer housing this select gets reparented to body.
    // ----------------------------------------------------------------------------------------
    document.addEventListener("change", function (event) {
        if (event.target.id !== "NewRecord_AutonomousBodyId" || !event.target.closest("#appendixVIEForm")) {
            return;
        }
        var pcb = window.PreBudgetControlBinding;
        if (!pcb || typeof pcb.loadAppendixVIEPreviousYearReference !== "function") {
            return;
        }
        var form = $("appendixVIEForm");
        pcb.loadAppendixVIEPreviousYearReference(form ? form.getAttribute("data-demand-id") : null, event.target.value);
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

        // Client requirement 2026-09-14: "retain value and disable dropdown for autonomous body
        // on click reset button". A native <button type="reset"> restores every control's
        // page-load initial value (blanking the locked select while leaving it disabled) and
        // PreBudget-control-binding.js's own generic reset listener (any form inside
        // .ubis-modern-appendix) unconditionally hard-clears the form on top of that via its own
        // setTimeout(0) - racing either with a "reset" event listener here proved unreliable (see
        // Appendix III-B/VI-B/VI-D/VI-F/VI-G's own comments for the confirmed race). Intercept the
        // button CLICK instead (before any "reset" event can fire) and, while still in Edit mode,
        // prevent the native reset entirely and blank only the plain editable fields ourselves -
        // the Autonomous Body select and the record id are left completely untouched.
        var resetVieBtn = event.target.closest('#appendixVIEForm button[type="reset"]');
        if (resetVieBtn) {
            var idFieldForReset = $("NewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                ["NewRecord_CorpusFundBalance1", "NewRecord_CorpusFundBalance2", "NewRecord_CorpusFundBankName", "NewRecord_CorpusFundReason", "NewRecord_GiaRE", "NewRecord_GiaBE"].forEach(function (id) {
                    var field = $(id);
                    if (field) { field.value = ""; }
                });
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-vie-add-drawer"]')) {
            openAppendixVieDrawer("add");
            return;
        }

        var editVIEButton = event.target.closest('[data-action="edit-appendix-vie-record"]');
        if (editVIEButton) {
            openAppendixVieDrawer("edit", editVIEButton);
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-vie-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var vieCancelForm = $("appendixVIEForm");
            var viePcb = window.PreBudgetControlBinding;
            var vieIsDirty = !viePcb || typeof viePcb.isFormDirtySinceOpen !== "function" || viePcb.isFormDirtySinceOpen(vieCancelForm);
            if (!vieIsDirty) {
                closeAppendixVieDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixVieDrawer();
                }, false, "No");
            } else {
                closeAppendixVieDrawer();
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

        if (event.target.closest("#vieColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#vieColumnsButton") && !event.target.closest("#vieColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-vie-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-vie"]');
        if (exportOptionBtn) {
            exportAppendixVie(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#vieExportButton") && !event.target.closest("#vieExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-vie-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-vie-trigger"]');
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

        if (event.target.closest("#vieCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#vieConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-vie-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-vie-filters"]')) {
            clearAppendixVieFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-vie-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-vie-prev-page"]')) {
            changeAppendixViePage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-vie-next-page"]')) {
            changeAppendixViePage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-vie-goto-page"]');
        if (pageBtn) {
            goToAppendixViePage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn && sortBtn.closest("#chargesTable")) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        if (event.target.closest("#vieToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]") && event.target.closest("#vieColumnsMenu")) {
            applyColumnVisibility();
        }
        if (event.target.id === "vieStatusFilter") {
            vieCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "viePageSize") {
            changeAppendixViePageSize();
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "vieSearchInput") {
            vieCurrentPage = 1;
            applyGridFilters();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("vieExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("vieColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("vieDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("vieDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixVieDrawer();
        }
    });

    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__vieHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("gridBody") && $("appendixVIEForm")) {
                vieCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentVieDrawerToBody();
        };
        window.PreBudgetControlBinding.__vieHooked = true;
    }
    hookFragmentApplied();
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    if ($("gridBody") && $("appendixVIEForm")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentVieDrawerToBody();
})();
