// "Approved design for all appendixes" (2026-09-03, UBIS-One-Screen-html prototype) - drawer/
// grid interaction layer for the redesigned Appendix VI-A page. Adapted from the prototype's own
// js/script.js, but rewired to the real server-rendered grid/form instead of the prototype's
// in-memory `records` array: every row here comes from Model.Records (Razor), and Add/Edit/
// Delete/Freeze all go through the SAME real POST actions (SaveAppendixVIA/DeleteAppendixVIA/
// FreezeAppendixTemplate) the old inline-table view already used - this file only owns the new
// visual chrome (drawer slide-in, columns picker, row action menu, delete dialog, toast,
// client-side search/status filter over the already-rendered rows).
//
// Every listener below is delegated on `document` (not attached to elements inside the
// AJAX-swapped fragment) so it keeps working across PreBudgetMeeting's fragment reloads without
// needing to be re-attached - same resilience pattern PreBudget-control-binding.js already uses
// for its own dispatcher.
(function () {
    "use strict";

    function $(id) { return document.getElementById(id); }

    // ----------------------------------------------------------------------------------------
    // Toast
    // ----------------------------------------------------------------------------------------
    var toastTimer;
    function showAppendixToast(message, type) {
        var toast = $("viaToast");
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
        $("viaToastTitle").textContent = variant.title;
        $("viaToastIcon").setAttribute("data-lucide", variant.icon);
        $("viaToastMessage").textContent = message;
        toast.classList.remove("ubis-toast-hide");
        toast.classList.add("ubis-toast-show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(hideAppendixToast, 4000);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    function hideAppendixToast() {
        var toast = $("viaToast");
        if (!toast) {
            return;
        }
        toast.classList.remove("ubis-toast-show");
        toast.classList.add("ubis-toast-hide");
    }

    // Exposed so future redesigned appendixes can reuse the same toast without copy-pasting it.
    window.UbisAppendixToast = { show: showAppendixToast, hide: hideAppendixToast };

    // ----------------------------------------------------------------------------------------
    // Export dropdown (2026-09-07, Appendix6a.html design - "clicking open menu" PDF/Excel/CSV)
    // ----------------------------------------------------------------------------------------
    function toggleExportMenu(event) {
        event.stopPropagation();
        var menu = $("viaExportMenu");
        var button = $("viaExportButton");
        if (!menu || !button) {
            return;
        }
        var opening = menu.classList.contains("hidden");
        closeColumnsMenu();
        menu.classList.toggle("hidden", !opening);
        button.setAttribute("aria-expanded", String(opening));
    }

    function closeExportMenu() {
        var menu = $("viaExportMenu");
        var button = $("viaExportButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    // Same AJAX-safe blob-download pattern already established for ECL's export links
    // (wwwroot/js/ecl.js's downloadExport) - fetch as a Blob with the XHR marker header instead of
    // a plain <a href> navigation, so an expired-session redirect (a 401/redirect-to-login HTML
    // page) is caught here instead of being silently saved to disk as a corrupt "report.pdf".
    function exportAppendixVia(format, triggerButton) {
        var form = $("appendixVIAForm");
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

        var url = "/PreBudgetMeeting/PreBudgetMeeting/ExportAppendixVIA?demandId=" + encodeURIComponent(demandId) + "&format=" + encodeURIComponent(format);

        fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error("Export request failed with status " + response.status);
                }
                var contentType = response.headers.get("Content-Type") || "";
                if (contentType.indexOf("pdf") === -1 && contentType.indexOf("spreadsheet") === -1 && contentType.indexOf("csv") === -1) {
                    // A redirected-to-login HTML page (expired session) or a JSON error body land here.
                    throw new Error("Unexpected response content type: " + contentType);
                }
                var disposition = response.headers.get("Content-Disposition") || "";
                var fileNameMatch = /filename="?([^";]+)"?/.exec(disposition);
                var fileName = fileNameMatch ? fileNameMatch[1] : ("AppendixVIA." + format);
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
        var menu = $("viaColumnsMenu");
        var button = $("viaColumnsButton");
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
        var menu = $("viaColumnsMenu");
        var button = $("viaColumnsButton");
        if (!menu || !button) {
            return;
        }
        menu.classList.add("hidden");
        button.setAttribute("aria-expanded", "false");
    }

    function applyColumnVisibility() {
        var menu = $("viaColumnsMenu");
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
        var menu = $("viaColumnsMenu");
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
        // Bug report 2026-09-03 ("clicking ... doesn't open the menu"): this used to read
        // event.currentTarget for the button, but the click listener is delegated on `document`
        // (see the wiring at the bottom of this file) - inside a delegated handler,
        // currentTarget is always the element the listener is ATTACHED to (document), never the
        // element that was actually clicked. positionActionMenu(menu, button) then tried to call
        // .getBoundingClientRect() on `document`, which doesn't have that method, threw, and
        // silently aborted before the menu was ever shown. The caller now passes the real
        // .row-menu-btn element it already matched via closest() instead.
        event.stopPropagation();
        var menu = $("via-menu-" + id);
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
        if (!menu) {
            return;
        }
        var id = menu.id.replace("via-menu-", "");
        var button = document.querySelector('.row-menu-btn[data-action-id="' + id + '"]');
        if (button) {
            positionActionMenu(menu, button);
        }
    }

    window.addEventListener("resize", repositionOpenActionMenu);
    window.addEventListener("scroll", function (event) {
        if (event.target && event.target.id === "viaGridScroll") {
            repositionOpenActionMenu();
        }
    }, true);

    // ----------------------------------------------------------------------------------------
    // Drawer (Add / Edit) - the real #appendixVIAForm now lives inside this drawer's markup.
    //
    // Field population on Edit used to be left to PreBudget-control-binding.js's existing
    // "edit-appendix-via-record" dispatch (populateSimpleEntryForm), like every other appendix.
    // That doesn't work here: positionActionMenu() (below) reparents the open action-menu to
    // document.body so it isn't clipped by the scrollable grid's overflow - which means a click
    // on "Edit" inside it no longer bubbles through #partialViewContainer at all, and
    // PreBudget-control-binding.js's click listener is delegated on THAT container, not
    // `document`. Its handler simply never saw the click, so the drawer opened empty. Population
    // is done directly below instead, using the same data-* attributes/field IDs that dispatch
    // already relied on, driven by this file's own document-level listener (unaffected by the
    // reparenting since document is above both containers either way).
    // ----------------------------------------------------------------------------------------
    var VIA_EDIT_FIELD_MAP = [
        ["NewRecord_TitleOfCharge", "data-title-of-charge"],
        ["NewRecord_Service", "data-service"],
        ["NewRecord_OrgDept", "data-org-dept"],
        ["NewRecord_RateOfCharge", "data-rate-of-charge"],
        ["NewRecord_UnitOfCollection", "data-unit-of-collection"],
        ["NewRecord_DateOfRateFixation", "data-date-of-rate-fixation"],
        ["NewRecord_FixationStatute", "data-fixation-statute"],
        ["NewRecord_TotalRevenueY1", "data-total-revenue-y1"],
        ["NewRecord_TotalRevenueY2", "data-total-revenue-y2"],
        ["NewRecord_TotalRevenueY3", "data-total-revenue-y3"],
        ["NewRecord_CompetentAuthority", "data-competent-authority"],
        ["NewRecord_PeriodOfFixation", "data-period-of-fixation"],
        ["NewRecord_Salary", "data-salary"],
        ["NewRecord_OfficeExpenses", "data-office-expenses"],
        ["NewRecord_OtherExpenses", "data-other-expenses"],
        ["NewRecord_Remarks", "data-remarks"]
    ];

    function populateAppendixViaFieldsFromButton(editButton) {
        var idField = $("NewRecord_Id");
        if (idField) {
            idField.value = editButton.getAttribute("data-id") || "";
        }

        VIA_EDIT_FIELD_MAP.forEach(function (pair) {
            var input = $(pair[0]);
            if (input) {
                input.value = editButton.getAttribute(pair[1]) || "";
            }
        });

        setSegmentByGroup("q1", editButton.getAttribute("data-is-collection-cost-higher") === "true" ? "YES" : "NO");
        setSegmentByGroup("q2", editButton.getAttribute("data-is-trans-cost-higher") === "true" ? "YES" : "NO");
        updateRemarksCounter();

        var form = $("appendixVIAForm");
        if (form && window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.enterAppendixEditMode === "function") {
            window.PreBudgetControlBinding.enterAppendixEditMode(form);
        } else if (form) {
            // Fallback if enterAppendixEditMode isn't exposed: still swap the button label so the
            // user isn't looking at a "Submit" button that will actually update, not create.
            var submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn) {
                if (!submitBtn.hasAttribute("data-original-label")) {
                    submitBtn.setAttribute("data-original-label", submitBtn.innerHTML);
                }
                submitBtn.innerHTML = submitBtn.getAttribute("data-original-label").replace(/Save Record|Submit/i, "Modify");
            }
        }
    }

    function openAppendixViaDrawer(mode, editButton) {
        var drawer = $("viaDrawer");
        var backdrop = $("viaDrawerBackdrop");
        if (!drawer || !backdrop) {
            return;
        }
        var form = $("appendixVIAForm");

        const txtTitleOfCharge = document.getElementById("NewRecord_TitleOfCharge");
        if (txtTitleOfCharge) {
            txtTitleOfCharge.readOnly = false;
        }

        if (mode === "edit" && editButton) {
            populateAppendixViaFieldsFromButton(editButton);
            if (txtTitleOfCharge) {
                txtTitleOfCharge.readOnly = true;
            }
        }

        if (mode === "add" && form) {
            if (window.PreBudgetControlBinding && typeof window.PreBudgetControlBinding.exitAppendixEditMode === "function") {
                window.PreBudgetControlBinding.exitAppendixEditMode(form);
            } else {
                form.reset();
            }
            setSegmentByGroup("q1", "NO");
            setSegmentByGroup("q2", "NO");
            updateRemarksCounter();
        }

        var titleEl = $("viaDrawerTitle");
        var subtitleEl = $("viaDrawerSubtitle");
        if (titleEl) {
            titleEl.textContent = mode === "edit" ? "Edit Record" : "Add Record";
        }
        if (subtitleEl) {
            subtitleEl.textContent = mode === "edit"
                ? "Update the selected user-charge record"
                : "Create a new user charge record";
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
            var titleField = $("NewRecord_TitleOfCharge");
            if (titleField) {
                titleField.focus();
            }
        }, 320);
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    // Bug found 2026-09-07 ("still missing header for create new record in slider"): #viaDrawer
    // renders deep inside the AJAX-fragment tree (#partialViewContainer > .panel section >
    // .ubis-modern-appendix > #viaDrawer), several levels below where the app shell's own sticky
    // masthead <header> (_Layout.cshtml, z-20) lives. Despite the drawer's own z-[70] comfortably
    // outranking the masthead's z-20 on paper, real browsers were painting the masthead ON TOP of
    // the drawer's header (verified live: elementsFromPoint at the drawer header's own screen
    // coordinate returned the masthead's children first) - hiding the title, icon and the X close
    // button behind the site header, exactly like the ".action-menu escapes grid overflow
    // clipping by reparenting to document.body" fix already used elsewhere in this same file.
    // Same fix here: move the drawer and its backdrop out to be direct children of <body> so they
    // stack in the true root context, clear of every ancestor the AJAX fragment nests them under.
    // Must re-run on every fragment (re)load, since PreBudgetMeeting's AJAX swap throws away and
    // re-renders this fragment's entire DOM (including a fresh #viaDrawer) each time.
    function reparentViaDrawerToBody() {
        // Every AJAX fragment (re)load renders a BRAND NEW #viaDrawer/#viaDrawerBackdrop pair
        // inside the fragment, while a previous load's copy may still be sitting as a direct
        // child of <body> from an earlier call to this function. With two elements sharing the
        // same id, plain getElementById("viaDrawer") is not reliable for picking the fresh one,
        // so find it explicitly as whichever copy is NOT already a direct child of body (the
        // stale copy always is, since that's exactly where the previous call moved it to).
        var allDrawers = document.querySelectorAll('[id="viaDrawer"]');
        var allBackdrops = document.querySelectorAll('[id="viaDrawerBackdrop"]');
        var drawer = null;
        allDrawers.forEach(function (node) {
            if (node.parentElement !== document.body) { drawer = node; }
        });
        var backdrop = null;
        allBackdrops.forEach(function (node) {
            if (node.parentElement !== document.body) { backdrop = node; }
        });

        // Bug report 2026-09-08: switching the Appendix dropdown AWAY from VI-A while its drawer
        // happened to be open used to leave that already-reparented #viaDrawer sitting in <body>
        // forever - the freshly-loaded OTHER appendix's own fragment contains no new #viaDrawer at
        // all, so the old "find whichever copy isn't already a body child" logic found nothing
        // fresh and silently kept reusing the stale, still-open one, which then sat on top of
        // (and blocked every click on) whatever appendix loaded next. If no fresh copy exists,
        // this appendix's markup isn't part of the current fragment any more - remove every
        // leftover instead of keeping one around.
        if (!drawer) {
            allDrawers.forEach(function (node) { node.remove(); });
            allBackdrops.forEach(function (node) { node.remove(); });
            return;
        }

        allDrawers.forEach(function (node) { if (node !== drawer) { node.remove(); } });
        allBackdrops.forEach(function (node) { if (node !== backdrop) { node.remove(); } });

        // AppendixVIA.cshtml's entire markup - grid AND drawer alike - is wrapped in a single
        // <div class="ubis-modern-appendix"> so appendix-drawer-design.css's scoped rules (input/
        // select/textarea/button letter-spacing, .text-[10px]/.text-xs sizing, etc.) reach the
        // drawer's own fields. Moving the drawer out from under that wrapper would silently drop
        // that styling, so the class has to move WITH it onto the drawer element itself.
        if (drawer) {
            drawer.classList.add("ubis-modern-appendix");
            // Bug report 2026-09-08 ("after modifying record, upper Demand/Appendix selector
            // gone"): marks this element for pre-budget-meeting.js's document-level submit
            // listener (see its own comment) - #appendixVIAForm lives inside this drawer, and its
            // Save/Freeze/Nil submit would otherwise silently fall back to a real, un-intercepted
            // browser post once the drawer is reparented out of #partialViewContainer here.
            drawer.setAttribute("data-reparented-fragment", "true");
            if (drawer.parentElement !== document.body) {
                document.body.appendChild(drawer);
            }
        }
        if (backdrop && backdrop.parentElement !== document.body) {
            // Backdrop must sit immediately before the drawer in DOM order (both are already
            // isolated by their own z-[60]/z-[70], but keeping the paint order intuitive avoids
            // surprises if a future tweak ever relies on sibling order instead of z-index alone).
            document.body.insertBefore(backdrop, drawer || null);
        }
    }

    function closeAppendixViaDrawer() {
        var drawer = $("viaDrawer");
        var backdrop = $("viaDrawerBackdrop");
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
    // Delete confirmation dialog - wraps the SAME real per-row <form asp-action="DeleteAppendixVIA">
    // already emitted by AppendixVIA.cshtml; this only replaces the generic data-confirm native
    // confirm() with the new styled dialog for this page.
    // ----------------------------------------------------------------------------------------
    var pendingDeleteForm = null;

    function openDeleteDialog(form, label) {
        var dialog = $("viaDeleteDialog");
        if (!dialog) {
            return;
        }
        pendingDeleteForm = form;
        var nameEl = $("viaDeleteRecordName");
        if (nameEl) {
            nameEl.textContent = label || "this record";
        }
        dialog.classList.remove("ubis-dialog-hide");
        dialog.classList.add("ubis-dialog-show");
        dialog.setAttribute("aria-hidden", "false");
        var cancelBtn = $("viaCancelDeleteBtn");
        if (cancelBtn) {
            cancelBtn.focus();
        }
    }

    function closeDeleteDialog() {
        var dialog = $("viaDeleteDialog");
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
        // Real submit - picked up by pre-budget-meeting.js's existing delegated submit handling
        // (same fetch/applyFragment pipeline every other appendix's delete form already uses), so
        // no fetch()/redirect logic is duplicated here.
        if (form.requestSubmit) {
            form.requestSubmit();
        } else {
            form.submit();
        }
    }

    // ----------------------------------------------------------------------------------------
    // Search / status filter over the already server-rendered rows (no separate data array -
    // Model.Records via Razor stays the single source of truth).
    // ----------------------------------------------------------------------------------------
    function currentGridRows() {
        var body = $("gridBody");
        return body ? Array.prototype.slice.call(body.querySelectorAll("tr[data-row]")) : [];
    }

    // Client requirement 2026-09-07 ("designer design confirm grid has pagination by default 10
    // rows and can be modified rows per page as in design"): this footer's Rows/Previous/Next/page
    // -number controls existed only as inert static markup until now - nothing ever wired them up.
    // Pagination runs entirely over the server-rendered rows already in the DOM (Model.Records via
    // Razor stays the single source of truth - this never re-fetches or re-sorts data from the
    // server), the same progressive-enhancement approach already used for search/status filtering.
    var viaCurrentPage = 1;
    var viaPageSize = 10;

    function applyGridFilters() {
        var rows = currentGridRows();
        var searchEl = $("viaSearchInput");
        var statusEl = $("viaStatusFilter");
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

        var totalPages = Math.max(1, Math.ceil(matched.length / viaPageSize));
        if (viaCurrentPage > totalPages) {
            viaCurrentPage = totalPages;
        }
        var start = (viaCurrentPage - 1) * viaPageSize;
        var pageRows = matched.slice(start, start + viaPageSize);
        var pageRowSet = pageRows;

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

        var empty = $("viaEmptyState");
        var gridWrap = $("viaGridScroll");
        if (empty) {
            empty.classList.toggle("hidden", matched.length !== 0);
        }
        if (gridWrap) {
            gridWrap.classList.toggle("hidden", matched.length === 0 && rows.length > 0);
        }

        var totalText = $("viaTotalText");
        var rangeText = $("viaRangeText");
        if (totalText) {
            totalText.textContent = String(matched.length);
        }
        if (rangeText) {
            rangeText.textContent = pageRowSet.length ? ((start + 1) + "–" + (start + pageRowSet.length)) : "0";
        }

        renderAppendixViaPagination(totalPages);
    }

    function renderAppendixViaPagination(totalPages) {
        var holder = $("viaPageNumbers");
        if (holder) {
            holder.innerHTML = "";
            var pages = [];
            for (var i = 1; i <= totalPages; i++) {
                if (totalPages <= 5 || i === 1 || i === totalPages || Math.abs(i - viaCurrentPage) <= 1) {
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
                b.setAttribute("data-action", "appendix-via-goto-page");
                b.setAttribute("data-page", String(p));
                b.className = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-xs font-bold " +
                    (p === viaCurrentPage ? "bg-ubis-navy text-white" : "text-slate-500 hover:bg-slate-100");
                b.textContent = String(p);
                holder.appendChild(b);
                last = p;
            });
        }

        var prevBtn = $("viaPrevBtn");
        var nextBtn = $("viaNextBtn");
        if (prevBtn) {
            prevBtn.disabled = viaCurrentPage === 1;
            prevBtn.classList.toggle("opacity-40", viaCurrentPage === 1);
        }
        if (nextBtn) {
            nextBtn.disabled = viaCurrentPage === totalPages;
            nextBtn.classList.toggle("opacity-40", viaCurrentPage === totalPages);
        }
    }

    function goToAppendixViaPage(page) {
        viaCurrentPage = page;
        applyGridFilters();
    }

    function changeAppendixViaPage(delta) {
        goToAppendixViaPage(viaCurrentPage + delta);
    }

    function changeAppendixViaPageSize() {
        var sizeEl = $("viaPageSize");
        viaPageSize = sizeEl ? (parseInt(sizeEl.value, 10) || 10) : 10;
        viaCurrentPage = 1;
        applyGridFilters();
    }

    function renumberVisibleRows(rows) {
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
        renumberVisibleRows(rows);
    }

    function clearAppendixViaFilters() {
        var searchEl = $("viaSearchInput");
        var statusEl = $("viaStatusFilter");
        if (searchEl) {
            searchEl.value = "";
        }
        if (statusEl) {
            statusEl.value = "All";
        }
        applyGridFilters();
    }

    // ----------------------------------------------------------------------------------------
    // Segmented Yes/No buttons + remarks character counter (Add/Edit drawer)
    // ----------------------------------------------------------------------------------------
    function setSegment(button) {
        var group = button.getAttribute("data-group");
        document.querySelectorAll('[data-group="' + group + '"]').forEach(function (btn) {
            btn.classList.remove("seg-active");
            btn.classList.add("border-slate-300", "text-slate-600");
        });
        button.classList.add("seg-active");
        button.classList.remove("border-slate-300", "text-slate-600");
        var targetId = button.getAttribute("data-target");
        if (targetId) {
            var radio = document.getElementById(targetId);
            if (radio) {
                radio.checked = true;
            }
        }
    }

    // Same effect as clicking the matching seg-btn (setSegment above), but driven by a value
    // (e.g. from a record being edited) rather than a real click - used by both the Add-mode
    // reset and Edit-mode field population.
    function setSegmentByGroup(group, value) {
        document.querySelectorAll('#appendixVIAForm .seg-btn[data-group="' + group + '"]').forEach(function (btn) {
            var active = btn.textContent.trim() === value;
            btn.classList.toggle("seg-active", active);
            if (active) {
                btn.classList.remove("border-slate-300", "text-slate-600");
            } else {
                btn.classList.add("border-slate-300", "text-slate-600");
            }
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

    function updateRemarksCounter() {
        var remarks = $("NewRecord_Remarks");
        var counter = $("viaCharCount");
        if (remarks && counter) {
            counter.textContent = String(remarks.value.length);
        }
    }

    // ----------------------------------------------------------------------------------------
    // Event wiring - delegated on document, survives AJAX fragment reloads.
    // ----------------------------------------------------------------------------------------
    document.addEventListener("click", function (event) {
        // Bug report 2026-09-07: "On Edit click, the shortcut menu not close and right slider
        // open. on clicking any option, first close shortcut menu then perform that operation."
        // Clicking a button INSIDE an open .action-menu (Edit/Duplicate/Freeze/Delete) used to
        // only close it via the generic "clicked outside" check further down - which explicitly
        // skips closing when the click landed inside the menu itself, so choosing an option left
        // it open behind whatever that option did (the drawer sliding in, a toast, etc). Closing
        // it first, unconditionally, for every one of these buttons - the specific action below
        // still runs against the same click afterward.
        if (event.target.closest(".action-menu button, .action-menu a")) {
            document.querySelectorAll(".action-menu").forEach(function (m) {
                m.classList.add("hidden");
                m.style.visibility = "";
            });
        }

        // Client requirement 2026-09-14: "on edit mode, reset should retain value of Title of the
        // User Charge" - Title of Charge is locked via readOnly during Edit (part of the record's
        // identity/duplicate-check key), but a native <button type="reset"> restores every
        // control's page-load initial value regardless (blanking it), and the shared generic reset
        // listener (any form inside .ubis-modern-appendix) unconditionally hard-clears the form on
        // top of that - racing either with a "reset" event listener proved unreliable elsewhere
        // (see Appendix III-B/VI-B/VI-D/VI-E/VI-F/VI-G/VI/VII-A/VII-B's own comments for the
        // confirmed race). Intercept the button CLICK instead and, while still in Edit mode,
        // prevent the native reset entirely and blank only the OTHER editable fields ourselves -
        // Title of Charge and the record id are left completely untouched.
        var resetViaBtn = event.target.closest('#appendixVIAForm button[type="reset"]');
        if (resetViaBtn) {
            var idFieldForReset = $("NewRecord_Id");
            if (idFieldForReset && idFieldForReset.value) {
                event.preventDefault();
                ["NewRecord_Service", "NewRecord_OrgDept", "NewRecord_RateOfCharge", "NewRecord_UnitOfCollection",
                    "NewRecord_DateOfRateFixation", "NewRecord_FixationStatute", "NewRecord_TotalRevenueY1",
                    "NewRecord_TotalRevenueY2", "NewRecord_TotalRevenueY3", "NewRecord_CompetentAuthority",
                    "NewRecord_PeriodOfFixation", "NewRecord_Salary", "NewRecord_OfficeExpenses",
                    "NewRecord_OtherExpenses", "NewRecord_Remarks"].forEach(function (id) {
                    var field = $(id);
                    if (field) { field.value = ""; }
                });
                setSegmentByGroup("q1", "NO");
                setSegmentByGroup("q2", "NO");
                updateRemarksCounter();
            }
            return;
        }

        if (event.target.closest('[data-action="open-appendix-via-add-drawer"]')) {
            openAppendixViaDrawer("add");
            return;
        }

        var editVIAButton = event.target.closest('[data-action="edit-appendix-via-record"]');
        if (editVIAButton) {
            openAppendixViaDrawer("edit", editVIAButton);
            return;
        }

        var cancelDrawerButton = event.target.closest('[data-action="close-appendix-via-drawer"]');
        if (cancelDrawerButton) {
            // Client requirement 2026-09-17: confirm before discarding an in-progress Add/Modify
            // (same pattern as Appendix I's own Cancel confirmation) - but skip the confirm
            // entirely if nothing was actually changed since the drawer opened.
            var viaCancelForm = $("appendixVIAForm");
            var viaPcb = window.PreBudgetControlBinding;
            var viaIsDirty = !viaPcb || typeof viaPcb.isFormDirtySinceOpen !== "function" || viaPcb.isFormDirtySinceOpen(viaCancelForm);
            if (!viaIsDirty) {
                closeAppendixViaDrawer();
                return;
            }
            if (window.openConfirmDialog) {
                window.openConfirmDialog("Cancel", "Are you sure you want to cancel?", "Yes", function () {
                    // Client requirement 2026-09-17: Cancel's Yes just closes the drawer now -
                    // it must NOT clear the form's fields.
                    closeAppendixViaDrawer();
                }, false, "No");
            } else {
                closeAppendixViaDrawer();
            }
            return;
        }

        var rowMenuBtn = event.target.closest(".row-menu-btn[data-action-id]");
        if (rowMenuBtn) {
            toggleActionMenu(rowMenuBtn.getAttribute("data-action-id"), rowMenuBtn, event);
            return;
        }
        if (!event.target.closest(".row-menu-btn") && !event.target.closest(".action-menu")) {
            document.querySelectorAll(".action-menu").forEach(function (m) { m.classList.add("hidden"); });
        }

        if (event.target.closest("#viaColumnsButton")) {
            toggleColumnsMenu(event);
            return;
        }
        if (!event.target.closest("#viaColumnsButton") && !event.target.closest("#viaColumnsMenu")) {
            closeColumnsMenu();
        }

        if (event.target.closest('[data-action="toggle-appendix-via-export-menu"]')) {
            toggleExportMenu(event);
            return;
        }
        var exportOptionBtn = event.target.closest('[data-action="export-appendix-via"]');
        if (exportOptionBtn) {
            exportAppendixVia(exportOptionBtn.getAttribute("data-format"), exportOptionBtn);
            return;
        }
        if (!event.target.closest("#viaExportButton") && !event.target.closest("#viaExportMenu")) {
            closeExportMenu();
        }

        if (event.target.closest('[data-action="reset-appendix-via-columns"]')) {
            resetColumns();
            return;
        }

        var deleteTriggerBtn = event.target.closest('[data-action="delete-appendix-via-trigger"]');
        if (deleteTriggerBtn) {
            event.preventDefault();
            // Bug found via live DB round-trip verification (2026-09-08): the Delete <form> is a
            // SIBLING of this button inside the same .action-menu container, not an ancestor - so
            // deleteTriggerBtn.closest("form") always returned null, meaning openDeleteDialog got
            // pendingDeleteForm = null and confirmDelete() silently no-op'd on every "Delete
            // record" confirm click for this whole appendix family (VI-A/B/C/D/E all copied this
            // same broken pattern). Look the form up within the shared .action-menu instead.
            var deleteForm = deleteTriggerBtn.closest(".action-menu").querySelector("form");
            var row = deleteTriggerBtn.closest("tr");
            var label = row ? row.getAttribute("data-search") : "this record";
            openDeleteDialog(deleteForm, label);
            return;
        }

        if (event.target.closest("#viaCancelDeleteBtn")) {
            closeDeleteDialog();
            return;
        }
        if (event.target.closest("#viaConfirmDeleteBtn")) {
            confirmDelete();
            return;
        }

        if (event.target.closest('[data-action="apply-appendix-via-filters"]')) {
            applyGridFilters();
            return;
        }
        if (event.target.closest('[data-action="clear-appendix-via-filters"]')) {
            clearAppendixViaFilters();
            return;
        }
        if (event.target.closest('[data-action="refresh-appendix-via-grid"]')) {
            applyGridFilters();
            return;
        }

        if (event.target.closest('[data-action="appendix-via-prev-page"]')) {
            changeAppendixViaPage(-1);
            return;
        }
        if (event.target.closest('[data-action="appendix-via-next-page"]')) {
            changeAppendixViaPage(1);
            return;
        }
        var pageBtn = event.target.closest('[data-action="appendix-via-goto-page"]');
        if (pageBtn) {
            goToAppendixViaPage(parseInt(pageBtn.getAttribute("data-page"), 10) || 1);
            return;
        }

        var sortBtn = event.target.closest("[data-sort-key]");
        if (sortBtn) {
            sortGridBy(sortBtn.getAttribute("data-sort-key"));
            return;
        }

        var segBtn = event.target.closest(".seg-btn[data-group]");
        if (segBtn) {
            setSegment(segBtn);
            return;
        }

        if (event.target.closest("#viaToastClose")) {
            hideAppendixToast();
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.matches("[data-column-toggle]")) {
            applyColumnVisibility();
        }
        if (event.target.id === "viaStatusFilter") {
            viaCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "viaPageSize") {
            changeAppendixViaPageSize();
        }
    });

    document.addEventListener("input", function (event) {
        if (event.target.id === "viaSearchInput") {
            viaCurrentPage = 1;
            applyGridFilters();
        }
        if (event.target.id === "NewRecord_Remarks") {
            updateRemarksCounter();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }
        var exportMenu = $("viaExportMenu");
        if (exportMenu && !exportMenu.classList.contains("hidden")) {
            closeExportMenu();
            return;
        }
        var columnsMenu = $("viaColumnsMenu");
        if (columnsMenu && !columnsMenu.classList.contains("hidden")) {
            closeColumnsMenu();
            return;
        }
        var dialog = $("viaDeleteDialog");
        if (dialog && dialog.classList.contains("ubis-dialog-show")) {
            closeDeleteDialog();
            return;
        }
        var drawer = $("viaDrawer");
        if (drawer && drawer.classList.contains("ubis-drawer-open")) {
            closeAppendixViaDrawer();
        }
    });

    // Re-apply column-visibility/search/status/row-numbering whenever the VI-A fragment is
    // (re)loaded - PreBudget-control-binding.js's onFragmentApplied already fires after every
    // AJAX swap (see pre-budget-meeting.js's applyFragment), so this wraps it rather than
    // introducing a second, separate fragment-load hook.
    function hookFragmentApplied() {
        if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__viaHooked) {
            return;
        }
        var previous = window.PreBudgetControlBinding.onFragmentApplied;
        window.PreBudgetControlBinding.onFragmentApplied = function () {
            if (typeof previous === "function") {
                previous();
            }
            if ($("gridBody")) {
                viaCurrentPage = 1;
                applyColumnVisibility();
                applyGridFilters();
            }
            reparentViaDrawerToBody();
        };
        window.PreBudgetControlBinding.__viaHooked = true;
    }
    hookFragmentApplied();
    // In case this script loads before PreBudget-control-binding.js finishes setting up its own
    // namespace (script tag order should prevent this, but this is a cheap safety net).
    document.addEventListener("DOMContentLoaded", hookFragmentApplied);

    // Bug found alongside the pagination work (2026-09-07): onFragmentApplied only fires after an
    // AJAX appendix switch - a genuine first page load (this appendix loaded directly, or a normal
    // full-page navigation) never called applyGridFilters() at all, so every row rendered
    // unpaginated (ignoring the "default 10 rows" requirement) until the user touched a filter.
    if ($("gridBody")) {
        applyColumnVisibility();
        applyGridFilters();
    }
    reparentViaDrawerToBody();
})();
