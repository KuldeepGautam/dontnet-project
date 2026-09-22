 (function () {
      var PRE_BUDGET_CONFIG = {
        breadcrumbRoot: "Pre Budget Meeting",
        departments: {
          "re-meeting-modules": {
            label: "Department of Agriculture & Farmers Welfare",
            appendices: {
              appendix1: {
                label: "Appendix I - Budget and Expenditure Trends",
                file: "appendix1.html"
              },
              appendix1a: {
                label: "Appendix I-A: Project Demand with Ministry",
                file: "appendix1A.html"
              },
               appendix2: {
                label: "Appendix II : Quarterly Expenditure Plan Progress",
                file: "appendix2.html"
              },
               appendix3A: {
                label: "Appendix III : CNA/SNA Balances of Schemes",
                file: "appendix3CNASNA.html"
              },
               appendix3B: {
                label: "Appendix III-A : TSA Assignment and Expenditure",
                file: "appendix3.html"
              } ,
               appendix4: {
                label: "Appendix IV : Estimates of Schemes",
                file: "appendix4.html"
              },
              appendix4A: {
                label: "Appendix IV-A : Estimates of Expenditure Under Special Component Plan for Scheduled Castes (Minor Head 789)",
                file: "appendix4A.html"
              },
              appendix4B: {
                label: "Appendix IV-B : Estimates of Expenditure Under Tribal Area Sub Plan (Minor Head 796)",
                file: "appendix4B.html"
              },

              appendix5Consolidated: {
                label: "Appendix V : Estimates of Establishment & Other Central Expenditure",
                file: "appendix5Consolidated.html"
              },
              appendix5a: {
                label: "Appendix V-A : Grant in Aid to Autonomous and other Bodies",
                file: "appendix5.html"
              },
                appendix5b: {
                label: "Appendix V-B : Details of Establishment Expenditure - Object Head wise",
                file: "appendix5.html"
              },
                appendix5c: {
                label: "Appendix V-C : Details of Establishment Expenditure - Other than AB",
                file: "appendix5.html"
              },
              appendix6: {
                label: "Appendix VI : Non Tax Revenue",
                file: "appendix6.html"
              },
             
               appendix6A: {
                label: "Appendix VI-A : List of User Charges levied by the Departments/Ministries",
                file: "appendix6A.html"
              },
                appendix6B: {
                label: "Appendix VI-B : Pending Liabilities of Ministries",
                file: "appendix6B.html"
              },
               Appendix6C: {
                label: "Appendix VI-C : Details of Corpus Funds",
                file: "appendix6C.html"
              },
               appendix6D: {
                label: "Appendix VI-D : Available internal resources with Grantee Bodies/Autonomous Institutions",
                file: "appendix6B.html"
              },
               appendix6E: {
                label: "Appendix VI-E : Details of Autonomous Bodies for which Corpus Fund has been created out of Grants-in-aid (GiA) support",
                file: "appendix6ECorpusABGiA.html"
              },
                appendix7A: {
                label: "Appendix VII-A : Statement showing the estimate of recoveries taken in reduction of expenditure under the Major Head",
                file: "appendix7A.html"
              },
                appendix7B: {
                label: "Appendix VII-B : Statement showing the commercial receipts of Departmentally run commercial undertakings and its revenue expenditure",
                file: "appendix7B.html"
              },


            }
          }
        },
        defaultDepartment: "re-meeting-modules",
        defaultAppendix: "appendix1"
      };

      function initPreBudgetMeeting() {
        var departmentSelect = document.getElementById("departmentSelect");
        var appendixSelect = document.getElementById("appendixSelect");
        var host = document.getElementById("partialViewContainer") || document.getElementById("appendixHost");

        if (!departmentSelect || !appendixSelect || !host) {
          return;
        }

        populateDepartments(departmentSelect);
        departmentSelect.addEventListener("change", handleDepartmentChange);
        appendixSelect.addEventListener("change", handleAppendixChange);

        var initialDepartment = PRE_BUDGET_CONFIG.defaultDepartment;
        if (PRE_BUDGET_CONFIG.departments[initialDepartment]) {
          departmentSelect.value = initialDepartment;
          populateAppendices(appendixSelect, initialDepartment, PRE_BUDGET_CONFIG.defaultAppendix);
          loadSelectedAppendix();
        } else {
          updateBreadcrumb();
        }

        function handleDepartmentChange() {
          populateAppendices(appendixSelect, departmentSelect.value, PRE_BUDGET_CONFIG.defaultAppendix);
          loadSelectedAppendix();
        }

        function handleAppendixChange() {
          loadSelectedAppendix();
        }

        function loadSelectedAppendix() {
          var departmentKey = departmentSelect.value;
          var appendixKey = appendixSelect.value;
          var department = PRE_BUDGET_CONFIG.departments[departmentKey];
          var appendix = department && department.appendices ? department.appendices[appendixKey] : null;

          updateBreadcrumb(departmentKey, appendixKey);

          if (!department || !appendix) {
            host.innerHTML = buildMessageState("Select valid options", "Please choose a department and appendix to continue.", "triangle-alert");
            host.setAttribute("aria-busy", "false");
            refreshIcons(host);
            return;
          }

          loadAppendixContent(host, departmentKey, appendix);
        }
      }

      function populateDepartments(select) {
        var options = ['<option value="">Select department</option>'];
        Object.keys(PRE_BUDGET_CONFIG.departments).forEach(function (key) {
          options.push('<option value="' + escapeHtml(key) + '">' + escapeHtml(PRE_BUDGET_CONFIG.departments[key].label) + '</option>');
        });
        select.innerHTML = options.join("");
      }

      function populateAppendices(select, departmentKey, preferredAppendixKey) {
        var department = PRE_BUDGET_CONFIG.departments[departmentKey];
        var options = ['<option value="">Select appendix</option>'];

        if (department && department.appendices) {
          Object.keys(department.appendices).forEach(function (key) {
            options.push('<option value="' + escapeHtml(key) + '">' + escapeHtml(department.appendices[key].label) + '</option>');
          });
        }

        select.innerHTML = options.join("");

        if (department && department.appendices) {
          if (preferredAppendixKey && department.appendices[preferredAppendixKey]) {
            select.value = preferredAppendixKey;
          } else {
            var firstKey = Object.keys(department.appendices)[0] || "";
            select.value = firstKey;
          }
        }
      }

      function updateBreadcrumb(departmentKey, appendixKey) {
        var nav = document.querySelector('nav[aria-label="Breadcrumb"]');
        if (!nav) {
          return;
        }

        var segments = [PRE_BUDGET_CONFIG.breadcrumbRoot];
        var department = PRE_BUDGET_CONFIG.departments[departmentKey];
        var appendix = department && department.appendices ? department.appendices[appendixKey] : null;

        if (department) {
          segments.push(department.label);
        }

        if (appendix) {
          segments.push(appendix.label);
        }

        nav.innerHTML = segments.map(function (segment, index) {
          var isLast = index === segments.length - 1;
          var cls = isLast ? "font-bold text-slate-700 dark:text-slate-200" : "text-slate-500";
          return '<span class="' + cls + '">' + escapeHtml(segment) + '</span>';
        }).join('<span class="mx-2 text-slate-400">&gt;</span>');
      }

      function loadAppendixContent(host, departmentKey, appendix) {
        var viewKey = departmentKey + "/" + appendix.file;
        var renderer = window.UBISAppendixViews && typeof window.UBISAppendixViews.getRenderer === "function"
          ? window.UBISAppendixViews.getRenderer(viewKey)
          : null;

        host.setAttribute("aria-busy", "true");
        host.innerHTML = buildMessageState("Loading appendix", "Opening " + appendix.label + "...", "loader-circle");
        refreshIcons(host);
        if (!renderer) {
          showAppendixLoadError(host, appendix);
          return;
        }

        try {
          host.innerHTML = [
            '<div class="pre-budget-frame-shell">',
            '<div class="pre-budget-partial-view">',
            renderer(),
            '</div>',
            '</div>'
          ].join("");
          initializeAppendixView(host);
          host.setAttribute("aria-busy", "false");
          if (typeof toast === "function") {
            toast("Appendix loaded", appendix.label + " opened without refreshing the page.", "success");
          }
        } catch (error) {
          showAppendixLoadError(host, appendix);
        }
      }

      function initializeAppendixView(host) {
        if (window.UBISAppendixActions && typeof window.UBISAppendixActions.refresh === "function") {
          window.UBISAppendixActions.refresh(host);
        }

        bindAppendixFiveTabs(host);
        refreshIcons(host);
      }

      function bindAppendixFiveTabs(host) {
        var tabPairs = [
          { triggerId: "tab-1-trigger", bodyId: "body-tab1" },
          { triggerId: "tab-2-trigger", bodyId: "body-tab2" },
          { triggerId: "tab-3-trigger", bodyId: "body-tab3" }
        ];

        tabPairs.forEach(function (pair) {
          var trigger = host.querySelector("#" + pair.triggerId);
          var body = host.querySelector("#" + pair.bodyId);

          if (!trigger || !body || trigger.dataset.partialBound === "true") {
            return;
          }

          trigger.addEventListener("change", function () {
            body.checked = true;
          });
          trigger.dataset.partialBound = "true";
        });
      }

      function showAppendixLoadError(host, appendix) {
        host.innerHTML = buildMessageState("Unable to load appendix", "The selected appendix file could not be opened. If you are opening the page directly from disk, this fallback has already been attempted. Please confirm the file exists in the department folder.", "file-warning");
        host.setAttribute("aria-busy", "false");
        refreshIcons(host);
        if (typeof toast === "function") {
          toast("Load failed", "The selected appendix page could not be opened.", "error");
        }
      }

      function buildMessageState(title, copy, icon) {
        return '<section class="pre-budget-placeholder is-compact"><div class="pre-budget-placeholder-icon"><i data-lucide="' + escapeHtml(icon) + '"></i></div><div><h3>' + escapeHtml(title) + '</h3><p>' + escapeHtml(copy) + '</p></div></section>';
      }

      function refreshIcons(scope) {
        if (window.lucide) {
          window.lucide.createIcons();
        }
      }

      function escapeHtml(value) {
        return String(value)
          .replace(/&/g, "&amp;")
          .replace(/</g, "&lt;")
          .replace(/>/g, "&gt;")
          .replace(/"/g, "&quot;")
          .replace(/'/g, "&#39;");
      }

      if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initPreBudgetMeeting);
      } else {
        initPreBudgetMeeting();
      }
    })();
