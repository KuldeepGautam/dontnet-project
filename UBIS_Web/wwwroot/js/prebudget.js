// PreBudget appendix screens - shared behaviors that apply across every appendix view (Export
// dialog, focus-highlight for editable textboxes). Client requirement 2026-09-02. Reuses the same
// .modal-backdrop/.verify-modal/.modal-header/.modal-body/.modal-actions visual shell dialogs.js's
// own openInfoDialog/openConfirmDialog use (see that file's header comment on why this app
// standardized on one dialog look), built locally here since neither of dialogs.js's two fixed
// shapes has room for a format dropdown - not a fork of that file, just the same CSS classes.
(function () {
  "use strict";

  function escapeHtml(value) {
    var div = document.createElement("div");
    div.textContent = value == null ? "" : String(value);
    return div.innerHTML;
  }

  function openExportDialog(baseUrl, demandId) {
    var wrap = document.createElement("div");
    wrap.className = "modal-backdrop";
    wrap.setAttribute("role", "presentation");
    wrap.innerHTML = [
      '<section class="verify-modal" role="dialog" aria-modal="true" aria-labelledby="exportDialogTitle">',
      '<header class="modal-header"><div><h2 id="exportDialogTitle">Ready to Export Data</h2></div></header>',
      '<div class="modal-body">',
      '<div class="verify-field">',
      '<label for="exportFormatSelect">Choose format:</label>',
      '<select id="exportFormatSelect" class="input">',
      '<option value="excel">Excel</option>',
      '<option value="pdf">PDF</option>',
      "</select>",
      "</div>",
      '<div class="modal-actions">',
      '<button class="cancel" type="button" data-cancel>Cancel</button>',
      '<button class="confirm" type="button" data-export>Export</button>',
      "</div>",
      "</div>",
      "</section>"
    ].join("");

    document.body.appendChild(wrap);
    requestAnimationFrame(function () {
      wrap.classList.add("is-open");
    });

    function close() {
      wrap.remove();
      document.removeEventListener("keydown", onKeydown);
    }
    function onKeydown(event) {
      if (event.key === "Escape") close();
    }

    wrap.querySelector("[data-cancel]").addEventListener("click", close);
    wrap.querySelector("[data-export]").addEventListener("click", function () {
      var format = wrap.querySelector("#exportFormatSelect").value;
      var isPdf = format === "pdf";
      var url = baseUrl + "?demandId=" + encodeURIComponent(demandId) + "&isPdf=" + isPdf;
      // A hidden, programmatically-clicked <a> - not window.location.href - triggers the
      // download. Found live while testing: the export action can genuinely take several seconds
      // (BuildAppendixIViewModelAsync does 5 sequential per-year lookups), and assigning
      // location.href for a slow Content-Disposition:attachment response leaves the browser
      // treating it as an ambiguous pending navigation for that whole window - which some
      // browsers resolve by aborting/restarting the underlying connection once they realize it's
      // a download, canceling the server-side request (HttpContext.RequestAborted) mid-generation
      // every time. A clicked <a> is the standard, unambiguous "this is a download, not a
      // navigation" signal from the very first byte, so the browser never treats it as a page
      // navigation to begin with.
      var link = document.createElement("a");
      link.href = url;
      link.rel = "noopener";
      // The "download" attribute (empty = use the server's Content-Disposition filename) signals
      // download intent to the browser from the moment of the click itself, not only once
      // response headers eventually arrive - the actual fix for the slow-response ambiguity
      // described above.
      link.download = "";
      document.body.appendChild(link);
      link.click();
      link.remove();
      close();
    });
    document.addEventListener("keydown", onKeydown);
  }

  document.addEventListener("click", function (event) {
    var trigger = event.target.closest('[data-action="open-appendix-export"]');
    if (!trigger) return;
    var baseUrl = trigger.getAttribute("data-export-base-url");
    var demandId = trigger.getAttribute("data-demand-id");
    if (!baseUrl || !demandId) return;
    openExportDialog(baseUrl, demandId);
  });

  // "If textbox is editable, onfocus change textbox background to lightskyblue" is handled
  // declaratively in prebudget.css via :focus:not([readonly]):not(:disabled) - no JS needed for
  // that one, it's a pure CSS pseudo-class and correctly excludes fields that can't actually be
  // typed into.
})();

// ----------------------------------------------------------------------------------------
// Universal Dynamic Table Column & Multi-Row Header Synchronizer (window.UbisGridColumns)
// ----------------------------------------------------------------------------------------
// Completely dynamic for ALL tables sitewide. Supports single-row and multi-row headers
// with grouped colspans, dynamic sub-column mapping, empty-state spanning, and fixed layouts.
// ----------------------------------------------------------------------------------------
window.UbisGridColumns = (function () {
  "use strict";

  function resolveTable(target) {
    if (!target) return null;
    if (typeof target === "string") {
      return document.getElementById(target) || document.querySelector(target);
    }
    if (target.nodeType === 1) {
      if (target.tagName.toLowerCase() === "table") return target;
      return target.querySelector("table") || target.closest("table");
    }
    return null;
  }

  function resolveMenu(target, table) {
    if (target) {
      if (typeof target === "string") {
        return document.getElementById(target) || document.querySelector(target);
      }
      if (target.nodeType === 1) return target;
    }
    if (table) {
      var container = table.closest(".ubis-modern-appendix, section, .grid-section, main") || document;
      return container.querySelector('[role="menu"][aria-label*="column" i], div[id$="ColumnsMenu"], div[id*="ColumnsMenu"], .columns-items-list');
    }
    return null;
  }

  function findTableForToggle(toggle) {
    var wrapper = toggle.closest(".ubis-modern-appendix, section, .grid-section, .tab-pane, .tab-content, main");
    if (wrapper) {
      var table = wrapper.querySelector("table.ubis-data-grid, table.w-full, table[id*='ChargesTable'], table[id*='chargesTable'], table[id*='Table'], table[id*='Grid'], table");
      if (table) return table;
    }

    var menu = toggle.closest('[role="menu"], div[id*="ColumnsMenu"], div[id*="Menu"]');
    if (menu && menu.id) {
      var triggerBtn = document.querySelector('[aria-controls="' + menu.id + '"]');
      if (triggerBtn) {
        var btnContainer = triggerBtn.closest(".ubis-modern-appendix, section, .grid-section, .tab-pane, main") || document;
        var tableFromBtn = btnContainer.querySelector("table");
        if (tableFromBtn) return tableFromBtn;
      }
    }

    return document.querySelector("table.ubis-data-grid, #chargesTable, table");
  }

  function getGroupMappings(table) {
    var headRows = table.querySelectorAll("thead tr");
    if (headRows.length <= 1) {
      return [];
    }

    var row1 = headRows[0];
    var row2 = headRows[1];
    if (!row1 || !row2) {
      return [];
    }

    var row2Cells = Array.prototype.slice.call(row2.children);
    var cursor = 0;
    var mappings = [];

    Array.prototype.forEach.call(row1.children, function (th) {
      var rowspan = parseInt(th.getAttribute("rowspan"), 10) || 1;
      if (rowspan >= 2) {
        return;
      }

      var initialSpan = parseInt(th.getAttribute("data-original-colspan"), 10);
      if (!initialSpan) {
        initialSpan = parseInt(th.getAttribute("colspan"), 10) || 1;
        th.setAttribute("data-original-colspan", String(initialSpan));
      }

      var subCells = row2Cells.slice(cursor, cursor + initialSpan);
      cursor += initialSpan;

      mappings.push({
        header: th,
        subCells: subCells
      });
    });

    return mappings;
  }

  function syncHeaderGroups(table) {
    var mappings = getGroupMappings(table);
    if (!mappings || mappings.length === 0) return;

    mappings.forEach(function (m) {
      var visibleCount = 0;
      m.subCells.forEach(function (sub) {
        var isHidden = sub.classList.contains("column-hidden") || sub.style.display === "none";
        if (!isHidden) {
          visibleCount++;
        }
      });

      if (visibleCount === 0) {
        m.header.classList.add("column-hidden");
        m.header.style.display = "none";
        m.header.setAttribute("data-export-exclude", "true");
        m.header.colSpan = 1;
      } else {
        m.header.classList.remove("column-hidden");
        m.header.style.removeProperty("display");
        m.header.removeAttribute("data-export-exclude");
        m.header.colSpan = visibleCount;
        m.header.setAttribute("colspan", String(visibleCount));
      }
    });
  }

  function syncEmptyStateColspan(table) {
    var emptyCell = table.querySelector("tbody td.empty-state-cell, tbody tr:only-child td[colspan]");
    if (!emptyCell) return;

    var headRows = table.querySelectorAll("thead tr");
    var totalVisible = 0;

    if (headRows.length <= 1) {
      var singleRow = headRows[0];
      if (singleRow) {
        Array.prototype.forEach.call(singleRow.children, function (c) {
          if (!c.classList.contains("column-hidden") && c.style.display !== "none") {
            totalVisible++;
          }
        });
      }
    } else {
      Array.prototype.forEach.call(headRows[0].children, function (c) {
        var rowspan = parseInt(c.getAttribute("rowspan"), 10) || 1;
        if (rowspan >= 2 && !c.classList.contains("column-hidden") && c.style.display !== "none") {
          totalVisible++;
        }
      });
      Array.prototype.forEach.call(headRows[1].children, function (c) {
        if (!c.classList.contains("column-hidden") && c.style.display !== "none") {
          totalVisible++;
        }
      });
    }

    if (totalVisible > 0) {
      emptyCell.colSpan = totalVisible;
      emptyCell.setAttribute("colspan", String(totalVisible));
    }
  }

  function applyColumnVisibility(tableTarget, menuTarget) {
    var table = resolveTable(tableTarget);
    if (!table) return;

    var menu = resolveMenu(menuTarget, table);
    if (!menu) return;

    var checkboxes = menu.querySelectorAll("[data-column-toggle]");
    if (!checkboxes || checkboxes.length === 0) return;

    Array.prototype.forEach.call(checkboxes, function (checkbox) {
      var colKey = checkbox.getAttribute("data-column-toggle");
      var isVisible = checkbox.checked;
      var cells = table.querySelectorAll('[data-column="' + colKey + '"]');
      Array.prototype.forEach.call(cells, function (cell) {
        cell.classList.toggle("column-hidden", !isVisible);
        if (isVisible) {
          cell.style.removeProperty("display");
        } else {
          cell.style.display = "none";
        }
      });
    });

    syncHeaderGroups(table);
    syncEmptyStateColspan(table);

    table.style.tableLayout = "auto";
    void table.offsetHeight;
    table.style.tableLayout = "";
  }

  function resetColumns(tableTarget, menuTarget) {
    var table = resolveTable(tableTarget);
    var menu = resolveMenu(menuTarget, table);
    if (!menu) return;

    Array.prototype.forEach.call(menu.querySelectorAll("[data-column-toggle]"), function (cb) {
      cb.checked = true;
    });

    applyColumnVisibility(table, menu);
  }

  function initAll() {
    var tables = document.querySelectorAll("table.ubis-data-grid, table[id*='ChargesTable'], table[id*='chargesTable'], table[id*='Grid'], .ubis-modern-appendix table");
    Array.prototype.forEach.call(tables, function (table) {
      applyColumnVisibility(table);
    });
  }

  document.addEventListener("change", function (event) {
    if (event.target && event.target.matches("[data-column-toggle]")) {
      var table = findTableForToggle(event.target);
      var menu = event.target.closest('[role="menu"], div[id*="ColumnsMenu"], div[id*="Menu"], .columns-items-list') || event.target.parentElement;
      if (table) {
        applyColumnVisibility(table, menu);
      }
    }
  });

  document.addEventListener("click", function (event) {
    var resetBtn = event.target.closest('[data-action*="reset"][data-action*="columns"], button[data-action*="reset-columns"]');
    if (resetBtn) {
      var container = resetBtn.closest("section, .ubis-modern-appendix, .grid-section, main") || document;
      var table = container.querySelector("table");
      var menu = resetBtn.closest('[role="menu"], div[id*="ColumnsMenu"], div[id*="Menu"]');
      if (table && menu) {
        resetColumns(table, menu);
      }
    }
  });

  function hookFragmentApplied() {
    if (!window.PreBudgetControlBinding || window.PreBudgetControlBinding.__columnsHooked) {
      return;
    }
    var previous = window.PreBudgetControlBinding.onFragmentApplied;
    window.PreBudgetControlBinding.onFragmentApplied = function () {
      if (typeof previous === "function") {
        previous();
      }
      initAll();
    };
    window.PreBudgetControlBinding.__columnsHooked = true;
  }
  hookFragmentApplied();
  document.addEventListener("DOMContentLoaded", function () {
    hookFragmentApplied();
    initAll();
  });

  return {
    applyColumnVisibility: applyColumnVisibility,
    syncHeaderGroups: syncHeaderGroups,
    syncEmptyStateColspan: syncEmptyStateColspan,
    resetColumns: resetColumns,
    initAll: initAll,
    resolveTable: resolveTable,
    resolveMenu: resolveMenu
  };
})();
