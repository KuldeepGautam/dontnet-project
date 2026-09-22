// Generic grid-to-Excel/PDF/CSV export (added 2026-09-15, client instruction: "export
// functionality should depend on columns of grid being export, not hard-code. any update on grid
// columns should not change export functionality. if grid has colspan or rowspan for headers,
// export to excel and PDF should support it.").
//
// Every appendix's own ExportAppendixX MVC action used to hand-maintain a SEPARATE, parallel list
// of column headers + a row-to-cells mapping in C#, independent of the actual <table> markup in
// the Razor view - the two drift apart the moment either one is edited without the other (exactly
// the bug fixed for Appendix III's export headers a few commits before this file was written).
// This scrapes the CURRENT, LIVE grid <table> in the browser instead - its <thead> (including any
// rowspan/colspan grouped-header row), only the currently-VISIBLE <tbody> rows (respecting the
// Columns menu's show/hide state, current sort order, and the toggled "hidden" class the search/
// filter/pagination logic already applies), and which columns are right-aligned (any <th>/<td>
// carrying a `text-right`/`tabular` class) - then posts that straight through to the generic
// PreBudgetMeeting/ExportGrid action, which forwards it to the Reporting service completely
// unchanged. Whatever the grid actually shows is exactly what gets exported - always, with zero
// per-appendix export code to keep in sync.
window.UbisGridExport = (function () {
    "use strict";

    function csrfToken() {
        var meta = document.querySelector('meta[name="csrf-token"]');
        if (meta) {
            return meta.content;
        }
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : "";
    }

    // A column is "excluded" from export if the Columns menu has hidden it (data-column carrying
    // the "column-hidden" class - see any appendix-*-drawer.js's applyColumnVisibility) or if it's
    // the row-actions column (no data-column attribute, header text "Action"/empty, or explicitly
    // marked with data-export-exclude="true" - the S.No./Action columns on every appendix grid).
    function isCellExcluded(cell) {
        if (cell.hasAttribute("data-export-exclude")) {
            return true;
        }
        var column = cell.getAttribute("data-column");
        if (column) {
            var anyVisible = false;
            document.querySelectorAll('[data-column="' + column + '"]').forEach(function (el) {
                if (!el.classList.contains("column-hidden")) {
                    anyVisible = true;
                }
            });
            if (!anyVisible) {
                return true;
            }
        }
        return false;
    }

    function isRightAligned(cell) {
        return cell.classList.contains("text-right") || cell.classList.contains("tabular")
            || cell.classList.contains("ml-auto");
    }

    // Reconstructs Columns[]/ColumnGroups[]/RightAlignedColumns[] from <thead>'s actual <tr>s,
    // respecting real rowspan/colspan attributes - a 1-row <thead> needs no grouping at all; a
    // 2-row <thead> (e.g. Appendix VI's "Proposed" group spanning 3 sub-columns) maps each row-1
    // <th rowspan="2"> to a single ungrouped leaf column and each <th colspan="N"> to a
    // ReportColumnGroup whose N leaf columns come from the matching cells in row 2.
    function scrapeHeader(table) {
        var headRows = Array.prototype.filter.call(
            table.querySelectorAll("thead tr"),
            function (tr) { return tr.children.length > 0; }
        );

        var columns = [];
        var rightAligned = [];
        var columnGroups = null;

        function pushColumn(text, cell) {
            columns.push(text);
            if (isRightAligned(cell)) {
                rightAligned.push(columns.length - 1);
            }
        }

        if (headRows.length <= 1) {
            var onlyRow = headRows[0];
            if (onlyRow) {
                Array.prototype.forEach.call(onlyRow.children, function (cell) {
                    if (isCellExcluded(cell)) {
                        return;
                    }
                    pushColumn(cell.textContent.replace(/\s+/g, " ").trim(), cell);
                });
            }
            return { columns: columns, columnGroups: null, rightAligned: rightAligned };
        }

        // 2-row header. Walk row 1 left-to-right; a rowspan="2" cell is a leaf column (its own
        // text, no group), a colspan="N" cell is a group label whose N leaf columns are consumed
        // from row 2 in order.
        columnGroups = [];
        var row1 = headRows[0];
        var row2Cells = Array.prototype.filter.call(row2CellsOf(headRows[1]), function (cell) {
            return !isCellExcluded(cell);
        });
        var row2Index = 0;

        Array.prototype.forEach.call(row1.children, function (cell) {
            if (isCellExcluded(cell)) {
                return;
            }
            var span = parseInt(cell.getAttribute("colspan"), 10) || 1;
            var rowspan = parseInt(cell.getAttribute("rowspan"), 10) || 1;
            if (rowspan >= 2 || span === 1 && row2Index >= row2Cells.length) {
                // Leaf column spanning both header rows vertically - no group.
                pushColumn(cell.textContent.replace(/\s+/g, " ").trim(), cell);
                columnGroups.push({ label: null, columnSpan: 1 });
            } else {
                for (var i = 0; i < span && row2Index < row2Cells.length; i++, row2Index++) {
                    pushColumn(row2Cells[row2Index].textContent.replace(/\s+/g, " ").trim(), row2Cells[row2Index]);
                }
                columnGroups.push({ label: cell.textContent.replace(/\s+/g, " ").trim(), columnSpan: span });
            }
        });

        return { columns: columns, columnGroups: columnGroups, rightAligned: rightAligned };

        function row2CellsOf(tr) {
            return tr ? tr.children : [];
        }
    }

    // Only rows the user would actually see right now: not display:none via the "hidden" class
    // (search/filter/pagination), and only cells whose data-column is currently visible (mirrors
    // scrapeHeader's own column exclusion so cell count always matches the header's).
    function scrapeRows(table, columnCount) {
        var bodyRows = table.querySelectorAll("tbody tr[data-row], tbody tr:not([data-row])");
        var rows = [];
        Array.prototype.forEach.call(bodyRows, function (tr) {
            if (tr.classList.contains("hidden") || tr.offsetParent === null && document.body.contains(tr) === false) {
                return;
            }
            if (tr.classList.contains("hidden")) {
                return;
            }
            var cells = [];
            Array.prototype.forEach.call(tr.children, function (cell) {
                if (isCellExcluded(cell)) {
                    return;
                }
                cells.push(cell.textContent.replace(/\s+/g, " ").trim());
            });
            if (cells.length === columnCount) {
                rows.push(cells);
            }
        });
        return rows;
    }

    /// Downloads a blob via a temporary object-URL <a> click - works from inside a fetch response
    /// with no page navigation, unlike the old per-appendix hidden-<form>-POST pattern (which only
    /// worked because it targeted a real MVC action returning a File() result directly).
    function downloadBlob(blob, fileName) {
        var url = URL.createObjectURL(blob);
        var link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    }

    /// options: { tableSelector, format ("pdf"|"excel"|"csv"), title, subtitle, fileName,
    /// exportUrl (defaults to the shared generic endpoint), onError(message) }
    function exportTable(options) {
        var table = document.querySelector(options.tableSelector);
        if (!table) {
            if (options.onError) { options.onError("Could not find the grid to export."); }
            return;
        }

        var header = scrapeHeader(table);
        if (header.columns.length === 0) {
            if (options.onError) { options.onError("The grid has no exportable columns."); }
            return;
        }
        var rows = scrapeRows(table, header.columns.length);

        var extensionByFormat = { excel: "xlsx", csv: "csv", pdf: "pdf" };
        var extension = extensionByFormat[options.format] || "pdf";
        var payload = {
            title: options.title,
            subtitle: options.subtitle || "",
            format: options.format,
            fileName: options.fileName || "export",
            columns: header.columns,
            columnGroups: header.columnGroups,
            rightAlignedColumns: header.rightAligned,
            rows: rows
        };

        fetch(options.exportUrl || "/PreBudgetMeeting/PreBudgetMeeting/ExportGrid", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": csrfToken(),
                "RequestVerificationToken": csrfToken()
            },
            body: JSON.stringify(payload)
        })
            .then(function (res) {
                if (!res.ok) {
                    throw new Error("Export failed (" + res.status + ")");
                }
                return res.blob();
            })
            .then(function (blob) {
                downloadBlob(blob, payload.fileName + "." + extension);
            })
            .catch(function (err) {
                if (options.onError) {
                    options.onError(err && err.message ? err.message : "Could not generate the export. Please try again.");
                }
            });
    }

    return { exportTable: exportTable };
})();
