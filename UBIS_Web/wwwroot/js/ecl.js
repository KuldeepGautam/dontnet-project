// ECL module client-side behavior (CSP: script-src 'self', no unsafe-inline — every handler here is
// wired via addEventListener from this external file, never an inline onclick/onchange attribute).
// Covers: AddSchemeOutlay's Yes/No 16th-FC toggle, Authority "Others" toggle (both Yes/No blocks, with
// distinct element ids — the mockup's own copy of this script referenced 'approvalAuthYes' and
// 'appraisalAuthNo', ids that don't exist in their respective blocks; the markup here uses
// appraisalAuthYes/approvalAuthYes in the Yes block and appraisalAuthNo/approvalAuthNo in the No
// block, matching what setupOtherToggle is actually called with below), WhetherAppraised/IsApproved
// Yes/No toggles, the CentralSharePercentage live preview, SchemeEndYear-gated outlay-year columns,
// Category->Scheme cascades, and a PDF upload UX pre-check.
(function () {
  "use strict";

  function onReady(fn) {
    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", fn);
    } else {
      fn();
    }
  }

  // --- Small toast helper for export feedback. UBIS_Web's shared _Layout.cshtml doesn't load
  // app.js or include a #toastRegion (that pattern only exists in the static designer mockups) -
  // rather than wire in ~1500 lines of unvetted legacy mockup JS just for one function, this reuses
  // the already-shipped .toast-item/.toast-success/.toast-error CSS (styles.css) with its own tiny
  // self-contained region, created lazily so no layout change is needed.
  function eclToast(message, tone) {
    var region = document.getElementById("eclToastRegion");
    if (!region) {
      region = document.createElement("div");
      region.id = "eclToastRegion";
      region.className = "fixed right-4 top-20 z-50 space-y-3";
      document.body.appendChild(region);
    }

    var item = document.createElement("div");
    item.className = "toast-item toast-" + (tone || "info");
    item.textContent = message;
    region.appendChild(item);

    window.setTimeout(function () {
      item.remove();
    }, 3500);
  }

  // --- Export to PDF/Excel: these can take a few seconds to generate (Reporting microservice
  // renders the whole document server-side), so a plain <a href> gives no feedback while the user
  // waits and no confirmation once the file actually lands. Fetches the file as a Blob instead,
  // showing a "Generating Report..." state on the clicked link and a success/error toast once the
  // download either completes or fails.
  function downloadExport(url, link) {
    var originalHtml = link.innerHTML;
    link.innerHTML = '<span class="inline-block h-3 w-3 animate-spin rounded-full border-2 border-slate-300 border-t-teal mr-1"></span>Generating Report...';
    link.style.pointerEvents = "none";
    link.setAttribute("aria-disabled", "true");

    fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } })
      .then(function (response) {
        if (!response.ok) {
          throw new Error("Export request failed with status " + response.status);
        }
        var contentType = response.headers.get("Content-Type") || "";
        if (contentType.indexOf("pdf") === -1 && contentType.indexOf("spreadsheet") === -1) {
          // A redirected-to-login HTML page (expired session) or a JSON error body both land here.
          throw new Error("Unexpected response content type: " + contentType);
        }

        var disposition = response.headers.get("Content-Disposition") || "";
        var fileNameMatch = /filename="?([^";]+)"?/.exec(disposition);
        var fileName = fileNameMatch ? fileNameMatch[1] : "report";

        return response.blob().then(function (blob) {
          return { blob: blob, fileName: fileName };
        });
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
        eclToast("Report has been downloaded successfully.", "success");
      })
      .catch(function () {
        eclToast("Could not generate the export. Please try again.", "error");
      })
      .finally(function () {
        link.innerHTML = originalHtml;
        link.style.pointerEvents = "";
        link.removeAttribute("aria-disabled");
      });
  }

  // --- Wires every [data-export-link] anchor (DataAnalysis.cshtml's two static links plus
  // _EclReportGrid.cshtml's, shared by Demand Wise ECL Status / Top Line / Pending Demand) to
  // downloadExport above instead of a normal navigation. Reads the href fresh at click time, not
  // cached at page load, so it still works correctly on DataAnalysis where the href is rewritten by
  // syncExportLinks() whenever the AJAX filters change.
  function initExportButtons() {
    document.querySelectorAll("[data-export-link]").forEach(function (link) {
      if (link.dataset.exportBound) return;
      link.dataset.exportBound = "1";
      link.addEventListener("click", function (event) {
        event.preventDefault();
        var href = link.getAttribute("href");
        if (href) downloadExport(href, link);
      });
    });
  }

  function toggleGroup(radios, onValue, showEl, hideEl) {
    radios.forEach(function (radio) {
      radio.addEventListener("change", function () {
        if (this.value === onValue) {
          if (showEl) showEl.classList.remove("hidden");
          if (hideEl) hideEl.classList.add("hidden");
        } else {
          if (showEl) showEl.classList.add("hidden");
          if (hideEl) hideEl.classList.remove("hidden");
        }
      });
    });
  }

  function setupOtherToggle(selectId, inputId, triggerValue) {
    var select = document.getElementById(selectId);
    var input = document.getElementById(inputId);
    if (!select || !input) return;

    select.addEventListener("change", function () {
      if (this.value === triggerValue) {
        input.classList.remove("hidden");
        input.focus();
      } else {
        input.classList.add("hidden");
        input.value = "";
      }
    });
  }

  function setupYesNoStatusToggle(radioName, statusInputId) {
    var radios = document.querySelectorAll('input[name="' + radioName + '"]');
    var input = document.getElementById(statusInputId);
    if (!radios.length || !input) return;

    radios.forEach(function (radio) {
      radio.addEventListener("change", function () {
        if (this.value === "N") {
          input.classList.remove("hidden");
          input.required = true;
        } else {
          input.classList.add("hidden");
          input.required = false;
          input.value = "";
        }
      });
    });
  }

  // --- 16th FC Yes/No case toggle ---
  function initFcToggle() {
    var radios = Array.prototype.slice.call(document.querySelectorAll('input[name="Is16Fc"]'));
    var caseYes = document.getElementById("caseYesFields");
    var caseNo = document.getElementById("caseNoFields");
    if (!radios.length) return;
    toggleGroup(radios, "Y", caseYes, caseNo);
    radios.forEach(function (radio) {
      radio.addEventListener("change", function () {
        if (this.value === "N" && caseYes) caseYes.classList.add("hidden");
        if (this.value === "N" && caseNo) caseNo.classList.remove("hidden");
      });
    });
  }

  // --- Central Share % live preview only — the authoritative value is always server-computed ---
  function initCentralSharePreview() {
    var totalOutlay = document.getElementById("totalOutlay");
    var centralShare = document.getElementById("centralShare");
    var preview = document.getElementById("centralSharePercentagePreview");
    if (!totalOutlay || !centralShare || !preview) return;

    function recompute() {
      var total = parseFloat(totalOutlay.value);
      var share = parseFloat(centralShare.value);
      if (!total || total <= 0 || isNaN(share)) {
        preview.value = "0.00";
        return;
      }
      preview.value = ((share / total) * 100).toFixed(2);
    }

      function totalOutlayChnges() {
          var e = document.getElementById("categorySelect");
          var cvalue = e.value;
          var ctext = e.options[e.selectedIndex].text;


          var centralShareInput = document.getElementById("centralShare");
          var totalOutlayInput = document.getElementById("totalOutlay");


          if (!centralShareInput || !totalOutlayInput) return;

          if (ctext == 'II - Central Sector Schemes/Projects') {
              var totalOutlay = totalOutlayInput.value;
              if (totalOutlay != "") {
                  centralShareInput.value = totalOutlay;
              }
          }
          else {
              centralShareInput.value = '';

          }
      }
      totalOutlay.addEventListener("input", recompute);
      totalOutlay.addEventListener("input", totalOutlayChnges);
      centralShare.addEventListener("input", recompute);
  }

  // --- SchemeEndYear gates which outlay-year columns accept input ---
  function initSchemeEndYearGate() {
    var select = document.getElementById("schemeEndYear");
    var table = document.getElementById("outlayGridTable");
    if (!select || !table) return;

    function applyGate() {
      var endYear = select.value;
      var inputs = table.querySelectorAll("input[data-outlay-year]");
      // No SchemeEndYear chosen yet -> every column stays editable (nothing to cap against). Once
      // one is chosen, only columns past it lock - previously this locked EVERY column, including
      // the earlier ones, until a choice was made at all, which blocked entry on a fresh form.
      inputs.forEach(function (input) {
        var year = input.getAttribute("data-outlay-year");
        var disable = !!endYear && year > endYear;
        input.disabled = disable;
        input.classList.toggle("bg-slate-100", disable);
        input.classList.toggle("cursor-not-allowed", disable);
        if (disable) input.value = "0.00";
      });
    }

    select.addEventListener("change", applyGate);
    applyGate();
  }

  // --- Category -> Scheme cascade (GET /ECL/ECLDataEntry/GetSchemes?categoryId=) ---
  // Client-corrected 2026-08-20: schemes are scoped by BOTH Demand and Category. Demand is a fixed
  // hidden field on AddSchemeOutlay (not user-changeable there), so this only listens for Category
  // changes, but sends both values on every fetch.
  function initCategorySchemeCascade() {
    var categorySelect = document.getElementById("categorySelect");
    var schemeSelect = document.getElementById("schemeSelect");
    var demandIdField = document.getElementById("demandIdField");
    if (!categorySelect || !schemeSelect || !demandIdField) return;

    var endpoint = categorySelect.getAttribute("data-schemes-url");
    if (!endpoint) return;

    categorySelect.addEventListener("change", function () {
      var categoryId = this.value;
      var demandId = demandIdField.value;
      schemeSelect.innerHTML = '<option value="">Loading...</option>';
      schemeSelect.disabled = true;
      if (!categoryId || !demandId) {
        schemeSelect.innerHTML = '<option value="">--Select Scheme--</option>';
        schemeSelect.disabled = false;
        return;
      }

      fetch(endpoint + "?demandId=" + encodeURIComponent(demandId) + "&categoryId=" + encodeURIComponent(categoryId), { headers: { "X-Requested-With": "XMLHttpRequest" } })
        .then(function (res) { return res.ok ? res.json() : []; })
        .then(function (schemes) {
          var html = '<option value="">--Select Scheme--</option>';
          (schemes || []).forEach(function (s) {
            html += '<option value="' + s.schemeId + '">' + (s.schemeSrNo || 0) + " - " + s.schemeName + "</option>";
          });
          schemeSelect.innerHTML = html;
          schemeSelect.disabled = false;
        })
        .catch(function () {
          schemeSelect.innerHTML = '<option value="">Could not load schemes</option>';
          schemeSelect.disabled = false;
        });

        var e = categorySelect;
        var cvalue = e.value;
        var ctext = e.options[e.selectedIndex].text;
        // 1. Get the element once to avoid typos and improve performance
        var centralShareInput = document.getElementById("centralShare");
        var totalOutlayInput = document.getElementById("totalOutlay");

        if (ctext == 'II - Central Sector Schemes/Projects') {
            centralShareInput.readOnly = true; // Fixed ID casing
        }
        else {
            centralShareInput.readOnly = false; // Fixed ID casing
        }

        centralShareInput.value = "";
        totalOutlayInput.value = "";

        document.getElementById("centralSharePercentagePreview").value = '0.00';
        document.getElementById("schemeEndYear").value = '';
        document.getElementById("appraisalAuthYes").value = '';
        document.getElementById("approvalAuthYes").value = '';
        document.getElementById("appraisalStatusInput").value = '';
        document.getElementById("approvedStatusInput").value = '';
        document.getElementById("file-upload").value = '';
        document.getElementById("file-upload-warning").textContent = '';
        document.getElementById("file-upload-status").textContent = '';
    });

  }

  // --- Category+Demand -> Umbrella Scheme cascade
  // (GET /ECL/ECLMaster/GetUmbrellaSchemes?categoryId=&demandId=) — added 2026-08-19: the Demand
  // select previously had no wiring at all, so the Umbrella Scheme dropdown only ever reflected
  // whatever demandId the page was originally loaded with and never updated when a different
  // Demand (or Category) was picked. Needs BOTH values (client-corrected 2026-08-19: real source
  // is dbo.M_UmbScheme filtered by CategoryId AND DemandId) - fires on a change to either select,
  // only actually fetching once both have a value. Same shape as initCategorySchemeCascade. ---
  function initDemandUmbrellaCascade() {
    var demandSelect = document.getElementById("demandSelect");
    var categorySelect = document.getElementById("categorySelect");
    var umbrellaSchemeSelect = document.getElementById("umbrellaSchemeSelect");
    if (!demandSelect || !categorySelect || !umbrellaSchemeSelect) return;

    var endpoint = demandSelect.getAttribute("data-umbrella-schemes-url");
    if (!endpoint) return;

    function reload() {
      var demandId = demandSelect.value;
      var categoryId = categorySelect.value;
      umbrellaSchemeSelect.innerHTML = '<option value="">Loading...</option>';
      umbrellaSchemeSelect.disabled = true;
      if (!demandId || !categoryId) {
        umbrellaSchemeSelect.innerHTML = '<option value="">--Select Umbrella Scheme--</option>';
        umbrellaSchemeSelect.disabled = false;
        return;
      }

      fetch(endpoint + "?categoryId=" + encodeURIComponent(categoryId) + "&demandId=" + encodeURIComponent(demandId), { headers: { "X-Requested-With": "XMLHttpRequest" } })
        .then(function (res) { return res.ok ? res.json() : []; })
        .then(function (schemes) {
          var html = '<option value="">--Select Umbrella Scheme--</option>';
          (schemes || []).forEach(function (u) {
            html += '<option value="' + u.umbSchemeId + '">' + u.umSchemeName + "</option>";
          });
          umbrellaSchemeSelect.innerHTML = html;
          umbrellaSchemeSelect.disabled = false;
        })
        .catch(function () {
          umbrellaSchemeSelect.innerHTML = '<option value="">Could not load umbrella schemes</option>';
          umbrellaSchemeSelect.disabled = false;
        });
    }

    demandSelect.addEventListener("change", reload);
    categorySelect.addEventListener("change", reload);
  }

  function csrfToken() {
    var meta = document.querySelector('meta[name="csrf-token"]');
    return meta ? meta.content : "";
  }

  // --- PDF upload: client pre-check (5 MB / application/pdf) is UX only, then upload immediately
  // via the controller's UploadDocument proxy so the returned StoredFileName can ride along on the
  // form's own Save submit — the API (EclOutlayController.UploadDocument) is the authoritative
  // validator (magic-byte check, 5 MB limit re-enforced server-side). ---
  function initPdfPreCheck() {
    var fileInput = document.getElementById("file-upload");
    var warning = document.getElementById("file-upload-warning");
    var status = document.getElementById("file-upload-status");
    if (!fileInput) return;

    fileInput.addEventListener("change", function () {
      if (warning) warning.textContent = "";
      if (status) status.textContent = "";
      var file = this.files && this.files[0];
      if (!file) return;

      if (file.type && file.type !== "application/pdf") {
        if (warning) warning.textContent = "Only PDF files are accepted.";
        this.value = "";
        return;
      }
      if (file.size > 5 * 1024 * 1024) {
        if (warning) warning.textContent = "File must be smaller than 5 MB.";
        this.value = "";
        return;
      }

      var uploadUrl = this.getAttribute("data-upload-url");
      if (!uploadUrl) return;

      var demandField = document.getElementById(this.getAttribute("data-demand-field") === "DemandId" ? "demandIdField" : this.getAttribute("data-demand-field"));
      var schemeField = document.getElementById("schemeSelect");
      var targetField = document.getElementById(this.getAttribute("data-filename-target"));

      var formData = new FormData();
      formData.append("file", file);
      formData.append("demandId", demandField ? demandField.value : "0");
      formData.append("schemeId", schemeField ? schemeField.value : "0");

      if (status) status.textContent = "Uploading...";
      fetch(uploadUrl, { method: "POST", headers: { "X-CSRF-TOKEN": csrfToken() }, body: formData })
        .then(function (res) { return res.json(); })
        .then(function (body) {
          if (body.success) {
            if (targetField) targetField.value = body.fileName;
            if (status) status.textContent = "Uploaded: " + body.fileName;
          } else {
            if (warning) warning.textContent = body.message || "Upload failed.";
            if (status) status.textContent = "";
          }
        })
        .catch(function () {
          if (warning) warning.textContent = "Upload failed — please try again.";
          if (status) status.textContent = "";
        });
    });
  }

  // --- Generic "changing a filter re-submits the GET form" wiring (report/grid filter toolbars) —
  // CSP forbids inline onchange=, so any <form data-auto-submit-filters> gets every <select> inside
  // it wired here instead. ---
  function initAutoSubmitFilters() {
    document.querySelectorAll("form[data-auto-submit-filters] select").forEach(function (select) {
      select.addEventListener("change", function () {
        select.form.submit();
      });
    });
  }

  // --- Data Analysis report: loads the grid via AJAX (client request 2026-08-20 - the page was
  // slow because the GET itself fetched every outlay row before rendering anything; now the shell
  // renders immediately and this fetches the grouped-by-Demand data separately, with a loading
  // spinner shown meanwhile). Filter changes re-fetch instead of a full page reload. ---
  function initDataAnalysisReport() {
    var form = document.getElementById("dataAnalysisFilters");
    var loading = document.getElementById("dataAnalysisLoading");
    var empty = document.getElementById("dataAnalysisEmpty");
    var tables = document.getElementById("dataAnalysisTables");
    var leftBody = document.getElementById("dataAnalysisLeftBody");
    var rightBody = document.getElementById("dataAnalysisRightBody");
    if (!form || !loading || !empty || !tables || !leftBody || !rightBody) return;

    var dataUrl = form.getAttribute("data-data-url");
    if (!dataUrl) return;

    var pdfLink = document.getElementById("exportPdfLink");
    var excelLink = document.getElementById("exportExcelLink");
    var pdfBaseUrl = pdfLink ? pdfLink.getAttribute("href") : null;
    var excelBaseUrl = excelLink ? excelLink.getAttribute("href") : null;

    function syncExportLinks(query) {
      if (pdfLink && pdfBaseUrl) pdfLink.setAttribute("href", pdfBaseUrl + "?" + query);
      if (excelLink && excelBaseUrl) excelLink.setAttribute("href", excelBaseUrl + "?" + query);
    }

    function escapeHtml(value) {
      var div = document.createElement("div");
      div.textContent = value == null ? "" : String(value);
      return div.innerHTML;
    }

    function money(value) {
      var n = typeof value === "number" ? value : parseFloat(value);
      return (isNaN(n) ? 0 : n).toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function renderGroups(groups) {
      var leftHtml = "";
      var rightHtml = "";
      groups.forEach(function (group) {
        leftHtml += '<tr><td colspan="12" class="text-left font-bold text-blue-700 bg-slate-50">' + escapeHtml(group.demandLabel) + "</td></tr>";
        rightHtml += '<tr><td colspan="3" class="text-left font-bold text-blue-700 bg-slate-50">' + escapeHtml(group.demandLabel) + "</td></tr>";

        group.rows.forEach(function (row) {
          leftHtml += '<tr><td class="text-left">' + escapeHtml(row.schemeLabel) + '</td><td class="text-left">' + escapeHtml(row.categoryLabel) + "</td>";
          for (var i = 0; i < 10; i++) {
            leftHtml += '<td class="text-right">' + money(row.outlay[i]) + "</td>";
          }
          leftHtml += "</tr>";

          var approvedColor = row.approvedByDoe === "Y" ? "text-teal" : row.approvedByDoe === "R" ? "text-red-600" : "text-amber-600";
          rightHtml += '<tr><td class="text-left">' + escapeHtml(row.userRemarks) + '</td><td class="text-left">' + escapeHtml(row.approverRemarks) +
            '</td><td class="text-center font-bold ' + approvedColor + '">' + escapeHtml(row.approvedByDoe) + "</td></tr>";
        });

        leftHtml += '<tr><td colspan="2" class="text-left font-bold">Total Outlay</td>';
        for (var t = 0; t < 10; t++) {
          leftHtml += '<td class="text-right font-bold">' + money(group.totalOutlay[t]) + "</td>";
        }
        leftHtml += "</tr>";

        leftHtml += '<tr><td colspan="2" class="text-left font-bold">Total Actual</td>';
        for (var a = 0; a < 10; a++) {
          leftHtml += '<td class="text-right font-bold">' + money(group.totalActual[a]) + "</td>";
        }
        leftHtml += "</tr>";
      });

      leftBody.innerHTML = leftHtml;
      rightBody.innerHTML = rightHtml;
    }

    function load() {
      loading.classList.remove("hidden");
      empty.classList.add("hidden");
      tables.classList.add("hidden");

      var params = new URLSearchParams(new FormData(form));
      syncExportLinks(params.toString());
      fetch(dataUrl + "?" + params.toString(), { headers: { "X-Requested-With": "XMLHttpRequest" } })
        .then(function (res) { return res.json(); })
        .then(function (data) {
          loading.classList.add("hidden");
          if (!data.success) {
            empty.textContent = data.message || "Could not load report data.";
            empty.classList.remove("hidden");
            return;
          }
          if (!data.groups || !data.groups.length) {
            empty.textContent = "No data for the selected filters.";
            empty.classList.remove("hidden");
            return;
          }
          renderGroups(data.groups);
          tables.classList.remove("hidden");
        })
        .catch(function () {
          loading.classList.add("hidden");
          empty.textContent = "Could not load report data. Please try again.";
          empty.classList.remove("hidden");
        });
    }

    form.addEventListener("submit", function (event) { event.preventDefault(); });
    form.querySelectorAll("select").forEach(function (select) {
      select.addEventListener("change", load);
    });

    load();
  }

  // --- AddActuals row picker navigation (CSP forbids inline onchange=) ---
  function initRowSelectNavigate() {
    var select = document.getElementById("rowSelect");
    if (!select) return;
    var base = select.getAttribute("data-navigate-base");
    if (!base) return;

    select.addEventListener("change", function () {
      if (this.value) {
        window.location.href = base + encodeURIComponent(this.value);
      }
    });
  }

  // --- Umbrella scheme toggle (AddSchemes master screen) ---
  function initUmbrellaToggle() {
    var categorySelect = document.getElementById("categorySelect");
    var umbrellaRow = document.getElementById("umbrellaRow");
    var umbrellaDropdownContainer = document.getElementById("umbrellaDropdownContainer");
    var umbrellaRadios = document.querySelectorAll('input[name="isUmbrella"]');
    var umbrellaSchemeSelect = document.getElementById("umbrellaSchemeSelect");
    if (!categorySelect || !umbrellaRow) return;

    categorySelect.addEventListener("change", function () {
      if (this.value) {
        umbrellaRow.classList.remove("hidden");
      } else {
        umbrellaRow.classList.add("hidden");
        if (umbrellaDropdownContainer) umbrellaDropdownContainer.classList.add("invisible");
        umbrellaRadios.forEach(function (r) { r.checked = false; });
        if (umbrellaSchemeSelect) umbrellaSchemeSelect.value = "";
      }
    });

    umbrellaRadios.forEach(function (radio) {
      radio.addEventListener("change", function () {
        if (!umbrellaDropdownContainer) return;
        if (this.value === "Y") {
          umbrellaDropdownContainer.classList.remove("invisible");
        } else {
          umbrellaDropdownContainer.classList.add("invisible");
          if (umbrellaSchemeSelect) umbrellaSchemeSelect.value = "";
        }
      });
    });
  }

  // --- DataToApprove grid: single-row Reject reveals a Reject/Cancel confirm pair (client request
  // 2026-08-21) instead of submitting immediately, and requires that row's own DOE Remarks field —
  // client-side nicety, the API is authoritative either way. Bulk Approve Selected/Reject Selected
  // validate at least one row is checked, and Reject Selected additionally requires every checked
  // row to have its own DOE Remarks filled in (server-side ECLApproveController.RejectSelected
  // enforces the same rule and reports which rows were skipped).
  function initApprovalGrid() {
    var form = document.getElementById("approvalGridForm");
    if (!form) {
      return;
    }

    form.addEventListener("click", function (event) {
      var trigger = event.target.closest("[data-reject-trigger]");
      if (trigger) {
        var row = trigger.closest("tr");
        row.querySelector(".row-action-buttons").classList.add("hidden");
        row.querySelector(".row-reject-confirm").classList.remove("hidden");
        return;
      }

      var cancel = event.target.closest("[data-reject-cancel]");
      if (cancel) {
        var cancelRow = cancel.closest("tr");
        cancelRow.querySelector(".row-reject-confirm").classList.add("hidden");
        cancelRow.querySelector(".row-action-buttons").classList.remove("hidden");
      }
    });

    form.addEventListener("submit", function (event) {
      var submitter = event.submitter;
      if (!submitter) {
        return;
      }

      // Single-row Reject confirm button.
      if (submitter.hasAttribute("data-reject-confirm")) {
        var row = submitter.closest("tr");
        var remarksField = row.querySelector('[name^="remarks["]');
        if (remarksField && !remarksField.value.trim()) {
          event.preventDefault();
          remarksField.classList.add("border-red-500");
          remarksField.focus();
          window.alert("DOE Remarks are required to reject a row.");
        }
        return;
      }

      // Bulk Approve Selected / Reject Selected.
      if (submitter.id === "approveSelectedBtn" || submitter.id === "rejectSelectedBtn") {
        var checked = Array.prototype.slice.call(form.querySelectorAll(".row-select-checkbox:checked"));
        if (checked.length === 0) {
          event.preventDefault();
          window.alert("Select at least one row first.");
          return;
        }

        if (submitter.id === "rejectSelectedBtn") {
          var missingRemarks = checked.filter(function (cb) {
            var rowEl = cb.closest("tr");
            var remarksInput = rowEl.querySelector('[name^="remarks["]');
            return !remarksInput || !remarksInput.value.trim();
          });
          if (missingRemarks.length > 0) {
            event.preventDefault();
            missingRemarks.forEach(function (cb) {
              var remarksInput = cb.closest("tr").querySelector('[name^="remarks["]');
              if (remarksInput) remarksInput.classList.add("border-red-500");
            });
            window.alert("DOE Remarks are required for every selected row before rejecting.");
            return;
          }
          if (!window.confirm("Reject " + checked.length + " selected row(s)? This cannot be undone.")) {
            event.preventDefault();
          }
        } else if (!window.confirm("Approve " + checked.length + " selected row(s)?")) {
          event.preventDefault();
        }
      }
    });
  }

  // --- Add Actuals modal (client request 2026-08-18: opens in place from the saved grid on
  // AddSchemeOutlay, "avoid unnecessary page navigation" — same open/close pattern as the login
  // screen's #changePasswordDialog: classList "is-open" toggle, form.reset() on open, focus first
  // field. Fetches the row's current Outlay/Actuals via GetActualsForRow, saves via
  // SaveActualsAjax — both JSON companions to the full-page AddActuals GET/POST, which still work
  // unchanged for anyone reaching that route directly. ---
  function initActualsModal() {
    var modal = document.getElementById("addActualsModal");
    var form = document.getElementById("addActualsForm");
    if (!modal || !form) return;

    var rowIdField = document.getElementById("addActualsRowId");
    var schemeNameField = document.getElementById("addActualsSchemeName");
    var statusField = document.getElementById("addActualsStatus");
    var errorField = document.getElementById("addActualsError");
    var headerRow = document.getElementById("addActualsHeaderRow");
    var inputRow = document.getElementById("addActualsInputRow");
    var getUrl = form.getAttribute("data-get-url");
    var saveUrl = form.getAttribute("data-save-url");
    // Real financial years (e.g. "2026-2027", ..., 10 of them starting at ECL_Config.ECL_StartYear)
    // instead of generic "Year 1"/"Year 2" placeholders - same list the saved-grid's own year
    // columns use, threaded through as a data attribute so this modal never hard-codes years.
    var yearColumns = (form.getAttribute("data-year-columns") || "").split(",").filter(Boolean);

    // Restricts a text input to numeric values with at most 2 decimal places as the user types -
    // client-side only (defense in depth); the server is the authoritative validator.
    function restrictToTwoDecimalPlaces(input) {
      input.addEventListener("input", function () {
        var cleaned = input.value.replace(/[^0-9.]/g, "");
        var firstDot = cleaned.indexOf(".");
        if (firstDot !== -1) {
          cleaned = cleaned.slice(0, firstDot + 1) + cleaned.slice(firstDot + 1).replace(/\./g, "");
          var decimals = cleaned.slice(firstDot + 1);
          if (decimals.length > 2) {
            cleaned = cleaned.slice(0, firstDot + 1) + decimals.slice(0, 2);
          }
        }
        if (cleaned !== input.value) input.value = cleaned;
      });
    }

    function closeModal() {
      modal.classList.remove("is-open");
    }

    function openModal(rowId) {
      if (errorField) errorField.textContent = "";
      form.reset();
      rowIdField.value = rowId;
      schemeNameField.value = "Loading…";
      statusField.value = "";
      headerRow.innerHTML = "";
      inputRow.innerHTML = "";

      fetch(getUrl + "?rowId=" + encodeURIComponent(rowId), { headers: { "X-Requested-With": "XMLHttpRequest" } })
        .then(function (res) { return res.ok ? res.json() : Promise.reject(); })
        .then(function (data) {
          schemeNameField.value = data.schemeName || "";
          statusField.value = data.doeApprovalStatusDisplay || "";

          var outlay = data.outlay || [];
          var actuals = data.actuals || [];
          for (var i = 0; i < 10; i++) {
            var th = document.createElement("th");
            th.className = "border-b border-r border-slate-200 p-2.5";
            th.textContent = yearColumns[i] || ("Year " + (i + 1));
            headerRow.appendChild(th);

            var td = document.createElement("td");
            td.className = "border-r border-slate-200 p-1.5";
            var input = document.createElement("input");
            input.type = "text";
            input.inputMode = "decimal";
            input.className = "input text-right py-1 text-xs";
            input.name = "actuals[" + i + "]";
            input.value = (actuals[i] != null ? actuals[i] : 0).toFixed ? (actuals[i] != null ? actuals[i] : 0).toFixed(2) : "0.00";
            input.title = "Outlay: " + ((outlay[i] != null ? outlay[i] : 0).toFixed ? outlay[i].toFixed(2) : "0.00");
            restrictToTwoDecimalPlaces(input);
            td.appendChild(input);
            inputRow.appendChild(td);
          }

          modal.classList.add("is-open");
          var firstInput = inputRow.querySelector("input");
          if (firstInput) firstInput.focus();
        })
        .catch(function () {
          schemeNameField.value = "";
          if (errorField) errorField.textContent = "Could not load this row. Please try again.";
          modal.classList.add("is-open");
        });
    }

    document.querySelectorAll("[data-open-actuals-modal]").forEach(function (button) {
      button.addEventListener("click", function () {
        openModal(this.getAttribute("data-row-id"));
      });
    });

    var closeButton = document.getElementById("addActualsClose");
    var cancelButton = document.getElementById("addActualsCancel");
    if (closeButton) closeButton.addEventListener("click", closeModal);
    if (cancelButton) cancelButton.addEventListener("click", closeModal);
    modal.addEventListener("click", function (event) {
      if (event.target === modal) closeModal();
    });
    document.addEventListener("keydown", function (event) {
      if (event.key === "Escape" && modal.classList.contains("is-open")) closeModal();
    });

    form.addEventListener("submit", function (event) {
      event.preventDefault();
      if (errorField) errorField.textContent = "";

      var formData = new FormData(form);
      fetch(saveUrl, { method: "POST", headers: { "X-CSRF-TOKEN": csrfToken() }, body: formData })
        .then(function (res) {
          if (res.ok) return res.json();
          return res.json().then(function (body) { return Promise.reject(body); });
        })
        .then(function () {
          closeModal();
          window.location.reload();
        })
        .catch(function (body) {
          if (errorField) errorField.textContent = (body && body.message) || "Could not save actuals. Please try again.";
        });
    });
  }

  // --- Saved Scheme Outlays grid: client-side pagination, 10 rows/page (client spec 2026-08-18).
  // No server round trip - all rows already render server-side, this just shows/hides <tr>s and
  // renders numbered page buttons. ---
  function initGridPagination() {
    var table = document.getElementById("savedOutlaysTable");
    var pager = document.getElementById("savedOutlaysPagination");
    if (!table || !pager) return;

    var pageSize = parseInt(table.getAttribute("data-page-size"), 10) || 10;
    var rows = Array.prototype.slice.call(table.querySelectorAll("tbody tr"));
    // Empty-state row (colspan) shouldn't be paginated away.
    if (rows.length <= 1 || table.querySelectorAll("tbody tr td.empty-state-cell").length) return;

    var pageCount = Math.ceil(rows.length / pageSize);
    if (pageCount <= 1) return;

    var currentPage = 1;

    function showPage(page) {
      currentPage = Math.min(Math.max(page, 1), pageCount);
      rows.forEach(function (row, index) {
        var rowPage = Math.floor(index / pageSize) + 1;
        row.hidden = rowPage !== currentPage;
      });
      renderPager();
    }

    function renderPager() {
      pager.innerHTML = "";

      var prev = document.createElement("button");
      prev.type = "button";
      prev.textContent = "‹";
      prev.disabled = currentPage === 1;
      prev.addEventListener("click", function () { showPage(currentPage - 1); });
      pager.appendChild(prev);

      for (var p = 1; p <= pageCount; p++) {
        (function (pageNumber) {
          var button = document.createElement("button");
          button.type = "button";
          button.textContent = String(pageNumber);
          if (pageNumber === currentPage) button.classList.add("is-active");
          button.addEventListener("click", function () { showPage(pageNumber); });
          pager.appendChild(button);
        })(p);
      }

      var next = document.createElement("button");
      next.type = "button";
      next.textContent = "›";
      next.disabled = currentPage === pageCount;
      next.addEventListener("click", function () { showPage(currentPage + 1); });
      pager.appendChild(next);
    }

    showPage(1);
  }

  // --- File-preview popup (client spec 2026-08-18: "file should open in popup") — same
  // modal-backdrop/verify-modal pattern as every other dialog in this app. Loads the PDF into an
  // <iframe> pointed at ECLDataEntryController.ViewDocument, which proxies the ECL API. ---
  function initViewDocumentModal() {
    var modal = document.getElementById("viewDocumentModal");
    var frame = document.getElementById("viewDocumentFrame");
    var closeButton = document.getElementById("viewDocumentClose");
    if (!modal || !frame) return;

    function closeModal() {
      modal.classList.remove("is-open");
      frame.src = "about:blank"; // stop the PDF from continuing to render/play in the background
    }

    document.querySelectorAll("[data-view-document]").forEach(function (button) {
      button.addEventListener("click", function () {
        var rowId = this.getAttribute("data-row-id");
        frame.src = "/ECL/ECLDataEntry/ViewDocument?rowId=" + encodeURIComponent(rowId);
        modal.classList.add("is-open");
      });
    });

    if (closeButton) closeButton.addEventListener("click", closeModal);
    modal.addEventListener("click", function (event) {
      if (event.target === modal) closeModal();
    });
    document.addEventListener("keydown", function (event) {
      if (event.key === "Escape" && modal.classList.contains("is-open")) closeModal();
    });
  }

  // --- Delete confirmation on the saved grid's Delete column ---
  // "PreBudget still having issue on dialog box of validation and record saving... same for ECL"
  // (client requirement, 2026-08-25) - AddSchemeOutlay is a full-page POST/reload (not an AJAX
  // fragment like PreBudget's appendix screens), so there's no applyFragment-style hook to piggyback
  // on; this runs once on every page load instead and checks whether the server just rendered a
  // StatusMessage (i.e. this load IS the result of a Save/Modify submit).
  function initSaveStatusDialog() {
    // Bug report 2026-08-27 (ECL Approve): document.querySelector('[role="status"]') without any
    // scoping picked up _Layout.cshtml's GLOBAL #navLoaderOverlay ("Loading…", no error class) -
    // present on every single page, before the actual page's own status paragraph in DOM order -
    // instead of the real per-page status element. That's why "Record Saved" kept appearing on
    // every ECL page load regardless of whether anything was actually saved (even a plain dropdown
    // reload, or an approve/reject call that failed with a real error message right above it).
    var statusEl = document.querySelector('[role="status"]:not(#navLoaderOverlay)');
    if (!statusEl || !window.openInfoDialog) return;

    var text = statusEl.textContent.trim();
    if (!text) return;

    var isError = statusEl.classList.contains("text-red-600");
    statusEl.style.display = "none";

    if (isError) {
      window.openInfoDialog("Could not save", text);
      return;
    }

    // "Dialog Heading: Record Saved, Text: Record has been successfully saved." (client
    // requirement, exact wording) - same Create/Modify heading split as PreBudget's own
    // showStatusMessageAsDialog, in case a future save message here ever starts distinguishing
    // the two the way PreBudget's already does.
    var isModify = /modif/i.test(text);
    window.openInfoDialog(
      isModify ? "Record Updated" : "Record Saved",
      isModify ? "Record has been successfully updated." : "Record has been successfully saved."
    );
  }

  // Same friendly-field-name lookup as PreBudget's own humanizeFieldName, adapted for this area's
  // markup: ECL's fields use a `<span>` label as a PRECEDING SIBLING inside a `.table-toolbar-field`
  // wrapper (not an actual `<label>` element wrapping the input, unlike PreBudget's convention) -
  // tries both shapes so this stays reusable if a future ECL screen ever does wrap with `<label>`.
  function humanizeFieldName(el) {
    var label = el.closest("label");
    if (label) {
      var labelSpan = label.querySelector("span");
      if (labelSpan && labelSpan.textContent.trim()) return labelSpan.textContent.trim();
    }
    var wrapper = el.closest(".table-toolbar-field");
    if (wrapper) {
      var wrapperSpan = wrapper.querySelector("span");
      if (wrapperSpan && wrapperSpan.textContent.trim()) return wrapperSpan.textContent.trim();
    }
    var raw = (el.name || el.id || "field").replace(/^NewRecord[_.]/, "");
    return raw.replace(/([a-z0-9])([A-Z])/g, "$1 $2").trim();
  }

  // "Dialog Heading: Please Enter required Fields. Dialog Message: Select Scheme, Enter values for
  // User Remarks, RE 2025-2026" (client requirement, exact wording) - same clause-building logic as
  // PreBudget's own submit-button click handler: each invalid dropdown gets its own "Select {Field}"
  // clause, every other invalid field is bundled into one "Enter values for A, B, C" clause. `form`
  // has `novalidate` (see AddSchemeOutlay.cshtml) so the browser's own native validation bubble
  // never gets a chance to fire first.
  function initFormValidationDialog() {
    var form = document.getElementById("addSchemeOutlayForm");
    if (!form) return;

    form.addEventListener("submit", function (event) {
      if (form.checkValidity()) return;

      event.preventDefault();
      var selectClauses = [];
      var enterValuesFields = [];
      Array.prototype.forEach.call(form.querySelectorAll("input, select, textarea"), function (el) {
        if (!el.willValidate || el.validity.valid) return;
        if (el.tagName === "SELECT") {
          selectClauses.push("Select " + humanizeFieldName(el));
        } else {
          enterValuesFields.push(humanizeFieldName(el));
        }
      });

      var clauses = selectClauses.slice();
      if (enterValuesFields.length) {
        clauses.push("Enter values for " + enterValuesFields.join(", "));
      }

      if (clauses.length && window.openInfoDialog) {
        window.openInfoDialog("Please Enter required Fields", clauses.join(", "));
      }
    });
  }

  function initDeleteConfirm() {
    document.querySelectorAll("[data-confirm-delete]").forEach(function (form) {
      form.addEventListener("submit", function (event) {
        if (!window.confirm("Delete this saved outlay row? This cannot be undone from here.")) {
          event.preventDefault();
        }
      });
    });
  }

  onReady(function () {
    initFcToggle();
    setupOtherToggle("appraisalAuthYes", "appraisalAuthOtherYes", "Others");
    setupOtherToggle("approvalAuthYes", "approvalAuthOtherYes", "Others");
    setupOtherToggle("appraisalAuthNo", "appraisalAuthOtherNo", "Others");
    setupOtherToggle("approvalAuthNo", "approvalAuthOtherNo", "Others");
    setupYesNoStatusToggle("WhetherAppraised", "appraisalStatusInput");
    setupYesNoStatusToggle("IsApproved", "approvedStatusInput");
    initCentralSharePreview();
    initSchemeEndYearGate();
    initCategorySchemeCascade();
    initDemandUmbrellaCascade();
    initAutoSubmitFilters();
    initDataAnalysisReport();
    initExportButtons();
    initRowSelectNavigate();
    initPdfPreCheck();
    initUmbrellaToggle();
    initApprovalGrid();
    initActualsModal();
    initGridPagination();
    initViewDocumentModal();
    initDeleteConfirm();
    initSaveStatusDialog();
    initFormValidationDialog();
    if (window.lucide) window.lucide.createIcons();
  });
})();
