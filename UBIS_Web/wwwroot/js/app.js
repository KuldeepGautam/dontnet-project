
var CRUD_CONFIGS = {
  "re-data.html": {
    entityName: "budget record",
    formTitle: "Budget Entry",
    tableTitle: "Budget Allocation Table",
    tableDescription: "Add, review, edit, and remove budget entries instantly.",
    storageKey: "ubis-budget-records",
    uniqueKey: "demandNumber",
    columns: [
      { key: "demandNumber", label: "Demand Number", inputType: "text", placeholder: "e.g. D-101" },
      { key: "department", label: "Department", inputType: "text", placeholder: "Department name" },
      { key: "financialYear", label: "Financial Year", inputType: "text", placeholder: "e.g. 2026-2027" },
      { key: "revenueBE", label: "Revenue BE", inputType: "number", inputMode: "decimal", placeholder: "0.00", align: "right", sortType: "number" },
      { key: "revenueRE", label: "Revenue RE", inputType: "number", inputMode: "decimal", placeholder: "0.00", align: "right", sortType: "number" },
      { key: "capitalBE", label: "Capital BE", inputType: "number", inputMode: "decimal", placeholder: "0.00", align: "right", sortType: "number" }
    ],
    seed: [
      { id: "budget-1", demandNumber: "D-101", department: "Agriculture", financialYear: "2026-2027", revenueBE: "1820.00", revenueRE: "1745.00", capitalBE: "640.00" },
      { id: "budget-2", demandNumber: "D-102", department: "Health", financialYear: "2026-2027", revenueBE: "1280.50", revenueRE: "1195.25", capitalBE: "420.00" },
      { id: "budget-3", demandNumber: "D-103", department: "Education", financialYear: "2026-2027", revenueBE: "2210.00", revenueRE: "2100.00", capitalBE: "880.75" },
       { id: "budget-4", demandNumber: "D-103", department: "Education", financialYear: "2026-2027", revenueBE: "2210.00", revenueRE: "2100.00", capitalBE: "880.75" },
        { id: "budget-5", demandNumber: "D-103", department: "Education", financialYear: "2026-2027", revenueBE: "2210.00", revenueRE: "2100.00", capitalBE: "880.75" }
    ]
  },
  "schemes.html": {
    entityName: "scheme",
    formTitle: "Add Scheme",
    tableTitle: "Scheme List",
    tableDescription: "Maintain a clean, searchable register of schemes.",
    storageKey: "ubis-scheme-records",
    uniqueKey: "schemeCode",
    columns: [
      { key: "schemeCode", label: "Scheme Code", inputType: "text", placeholder: "e.g. SCM-210" },
      { key: "schemeName", label: "Scheme Name", inputType: "text", placeholder: "Scheme title" },
      { key: "department", label: "Department", inputType: "text", placeholder: "Department name" },
      { key: "allocationAmount", label: "Allocation Amount", inputType: "number", inputMode: "decimal", placeholder: "0.00", align: "right", sortType: "number" },
      { key: "status", label: "Status", inputType: "text", placeholder: "Approved, Pending, Review", filterable: true },
      { key: "nodalOfficer", label: "Nodal Officer", inputType: "text", placeholder: "Officer name" }
    ],
    seed: [
      { id: "scheme-1", schemeCode: "SCM-101", schemeName: "National Irrigation Support", department: "Agriculture", allocationAmount: "845.00", status: "Approved", nodalOfficer: "A. Sharma" },
      { id: "scheme-2", schemeCode: "SCM-204", schemeName: "Rural Health Renewal", department: "Health", allocationAmount: "620.00", status: "Pending", nodalOfficer: "R. Thomas" },
      { id: "scheme-3", schemeCode: "SCM-311", schemeName: "Digital Classrooms", department: "Education", allocationAmount: "910.50", status: "Review", nodalOfficer: "P. Iyer" }
    ]
  },
  "expenditure.html": {
    entityName: "expenditure record",
    formTitle: "Expenditure Entry",
    tableTitle: "Expenditure Records",
    tableDescription: "Track expenditure submissions with instant updates and edits.",
    storageKey: "ubis-expenditure-records",
    uniqueKey: "demandNumber",
    columns: [
      { key: "demandNumber", label: "Demand Number", inputType: "text", placeholder: "e.g. D-101" },
      { key: "scheme", label: "Scheme", inputType: "text", placeholder: "Scheme name" },
      { key: "expenditureDate", label: "Expenditure Date", inputType: "date", placeholder: false, sortType: "date" },
      { key: "revenueAmount", label: "Revenue Amount", inputType: "number", inputMode: "decimal", placeholder: "0.00", align: "right", sortType: "number" },
      { key: "capitalAmount", label: "Capital Amount", inputType: "number", inputMode: "decimal", placeholder: "0.00", align: "right", sortType: "number" },
      { key: "remarks", label: "Remarks", inputType: "text", placeholder: "Short note" }
    ],
    seed: [
      { id: "exp-1", demandNumber: "D-101", scheme: "National Irrigation Support", expenditureDate: "2026-06-15", revenueAmount: "125.00", capitalAmount: "40.00", remarks: "Quarterly release" },
      { id: "exp-2", demandNumber: "D-102", scheme: "Rural Health Renewal", expenditureDate: "2026-06-19", revenueAmount: "90.25", capitalAmount: "12.00", remarks: "Vendor payment" },
      { id: "exp-3", demandNumber: "D-103", scheme: "Digital Classrooms", expenditureDate: "2026-06-23", revenueAmount: "160.00", capitalAmount: "58.50", remarks: "Infrastructure upgrade" }
    ]
  },
  "users.html": {
    entityName: "user",
    formTitle: "Add User",
    tableTitle: "User List and Roles",
    tableDescription: "Manage user accounts with compact editing and validation.",
    storageKey: "ubis-user-records",
    uniqueKey: "loginId",
    columns: [
      { key: "loginId", label: "Login ID", inputType: "text", placeholder: "e.g. AGRICOOP01" },
      { key: "fullName", label: "Full Name", inputType: "text", placeholder: "Full name" },
      { key: "email", label: "Email", inputType: "email", placeholder: "name@example.gov.in" },
      { key: "role", label: "Role", inputType: "text", placeholder: "Role title" },
      { key: "department", label: "Department", inputType: "text", placeholder: "Department name" },
      { key: "status", label: "Status", inputType: "text", placeholder: "Active, Pending, Inactive", filterable: true }
    ],
    seed: [
      { id: "user-1", loginId: "AGRICOOP01", fullName: "Anita Verma", email: "anita.verma@gov.in", role: "Data Operator", department: "Agriculture", status: "Active" },
      { id: "user-2", loginId: "HEALTH02", fullName: "Rahul Menon", email: "rahul.menon@gov.in", role: "Reviewer", department: "Health", status: "Pending" },
      { id: "user-3", loginId: "EDU03", fullName: "Priya Rao", email: "priya.rao@gov.in", role: "Admin", department: "Education", status: "Active" }
    ]
  },
  "profile.html": {
    entityName: "profile record",
    formTitle: "User Information",
    tableTitle: "Profile Updates",
    tableDescription: "Review saved profile snapshots and keep updates traceable.",
    storageKey: "ubis-profile-records",
    uniqueKey: "username",
    columns: [
      { key: "username", label: "Username", inputType: "text", placeholder: "Username" },
      { key: "department", label: "Department", inputType: "text", placeholder: "Department name" },
      { key: "email", label: "Email", inputType: "email", placeholder: "name@example.gov.in" },
      { key: "role", label: "Role", inputType: "text", placeholder: "Role title" }
    ],
    seed: [
      { id: "profile-1", username: "AGRICOOP", department: "Agriculture", email: "agricoop@gov.in", role: "Data Operator" }
    ]
  },
  "settings.html": {
    entityName: "settings profile",
    formTitle: "System Settings",
    tableTitle: "Saved Configurations",
    tableDescription: "Store and revise configuration sets without reloading the page.",
    storageKey: "ubis-settings-records",
    uniqueKey: "systemName",
    columns: [
      { key: "systemName", label: "System Name", inputType: "text", placeholder: "System name" },
      { key: "financialYear", label: "Financial Year", inputType: "text", placeholder: "e.g. 2026-2027" },
      { key: "requireMfa", label: "Require MFA", inputType: "text", placeholder: "Enabled or Disabled" },
      { key: "sessionTimeout", label: "Session Timeout", inputType: "text", placeholder: "e.g. 30 minutes" },
      { key: "notificationChannels", label: "Notification Channels", inputType: "text", placeholder: "Budget approvals, Audit alerts" }
    ],
    seed: [
      { id: "settings-1", systemName: "Union Budget Information System", financialYear: "2026-2027", requireMfa: "Enabled", sessionTimeout: "30 minutes", notificationChannels: "Budget approvals, Audit alerts" }
    ]
  }
};

var SIDEBAR_STATE_KEY = "ubis-sidebar-state";

document.addEventListener("DOMContentLoaded", function () {
  renderSharedShell();
  pruneDecorativeIcons();
  initTheme();
  initShell();
  initAccordion();
  initPageChrome();
  initEmbeddedMode();
  initLogin();
  initCrudPages();
  initCharts();
  initToasts();
  markActiveChild();
  initFloatingSidebarMenus();

  if (window.lucide) {
    lucide.createIcons();
  }
});

function renderSharedShell() {
  if (window.UBISAppIncludes && typeof window.UBISAppIncludes.renderShell === "function") {
    window.UBISAppIncludes.renderShell();
  }
}

function initTheme() {
  if (localStorage.getItem("theme") === "dark") {
    document.documentElement.classList.add("dark");
  }

  document.querySelectorAll("#themeToggle,#themeToggle2").forEach(function (button) {
    button.addEventListener("click", function () {
      document.documentElement.classList.toggle("dark");
      localStorage.setItem("theme", document.documentElement.classList.contains("dark") ? "dark" : "light");
    });
  });
}

function initShell() {
  var body = document.body;
  var sidebar = document.getElementById("sidebar");
  var overlay = document.getElementById("drawerOverlay");
  var desktopToggle = document.getElementById("sidebarToggle");

  simplifySidebar();
  simplifyHeaderTools();
  applyStoredSidebarState();

  if (desktopToggle) {
    desktopToggle.addEventListener("click", function () {
      body.classList.toggle("sidebar-collapsed");
      saveSidebarState({
        desktopCollapsed: body.classList.contains("sidebar-collapsed")
      });
      syncFloatingPanelState();
    });
  }

  document.querySelectorAll("[data-sidebar-mobile]").forEach(function (button) {
    button.addEventListener("click", function () {
      var isHidden = sidebar.classList.contains("-translate-x-full");
      sidebar.classList.toggle("-translate-x-full", !isHidden);
      if (overlay) {
        overlay.classList.toggle("hidden", !isHidden);
      }
      saveSidebarState({
        mobileDrawerOpen: isHidden
      });
    });
  });

  if (sidebar) {
    sidebar.addEventListener("click", function (event) {
      var link = event.target.closest("a[href]");
      if (!link) {
        return;
      }

      persistSidebarStateForNavigation();
    });
  }

  if (overlay) {
    overlay.addEventListener("click", closeDrawer);
  }

  window.addEventListener("resize", function () {
    if (window.innerWidth >= 1024) {
      closeDrawer();
    }
    applyStoredSidebarState();
  });

  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
      closeDrawer();
    }
  });

  function closeDrawer() {
    if (!sidebar) {
      return;
    }

    sidebar.classList.add("-translate-x-full");
    if (overlay) {
      overlay.classList.add("hidden");
    }
    saveSidebarState({
      mobileDrawerOpen: false
    });
  }

  function applyStoredSidebarState() {
    var state = readSidebarState();

    if (window.innerWidth >= 1024) {
      body.classList.toggle("sidebar-collapsed", !!state.desktopCollapsed);
      if (sidebar) {
        sidebar.classList.remove("-translate-x-full");
      }
      if (overlay) {
        overlay.classList.add("hidden");
      }
      return;
    }

    body.classList.remove("sidebar-collapsed");
    if (!sidebar) {
      return;
    }

    var shouldOpenDrawer = !!state.mobileDrawerOpen;
    sidebar.classList.toggle("-translate-x-full", !shouldOpenDrawer);
    if (overlay) {
      overlay.classList.toggle("hidden", !shouldOpenDrawer);
    }
  }

  function persistSidebarStateForNavigation() {
    saveSidebarState({
      desktopCollapsed: window.innerWidth >= 1024 && body.classList.contains("sidebar-collapsed"),
      mobileDrawerOpen: false
    });
  }
}

function readSidebarState() {
  try {
    var saved = JSON.parse(localStorage.getItem(SIDEBAR_STATE_KEY) || "{}");
    return {
      desktopCollapsed: !!saved.desktopCollapsed,
      mobileDrawerOpen: !!saved.mobileDrawerOpen
    };
  } catch (error) {
    return {
      desktopCollapsed: false,
      mobileDrawerOpen: false
    };
  }
}

function saveSidebarState(patch) {
  var nextState = readSidebarState();
  Object.keys(patch || {}).forEach(function (key) {
    nextState[key] = patch[key];
  });
  localStorage.setItem(SIDEBAR_STATE_KEY, JSON.stringify(nextState));
}

function initAccordion() {
  document.querySelectorAll(".sidebar-accordion").forEach(function (detail) {
    detail.addEventListener("toggle", function () {
      if (!detail.open) {
        return;
      }

      document.querySelectorAll(".sidebar-accordion").forEach(function (other) {
        if (other !== detail) {
          other.open = false;
        }
      });
    });
  });
}

function initPageChrome() {
  var titleBand = document.querySelector("main > div.mb-5.rounded-t-lg.bg-teal");
  if (titleBand) {
    titleBand.classList.add("page-title-band");
  }

  var heroPanel = document.querySelector("section.animate-fade-in > div.rounded-2xl.bg-gradient-to-r");
  if (heroPanel) {
    heroPanel.classList.add("dashboard-hero");
  }

  document.querySelectorAll(".workflow").forEach(function (item) {
    item.classList.add("timeline-item");
  });
}

function initEmbeddedMode() {
  var params = new URLSearchParams(window.location.search);
  if (params.get("embed") !== "1") {
    return;
  }

  document.body.classList.add("embed-mode");

  var sidebar = document.getElementById("sidebar");
  if (sidebar) {
    sidebar.remove();
  }

  var overlay = document.getElementById("drawerOverlay");
  if (overlay) {
    overlay.remove();
  }

  var header = document.querySelector("#appFrame > header");
  if (header) {
    header.remove();
  }

  var footer = document.querySelector("#appFrame > footer");
  if (footer) {
    footer.remove();
  }

  var breadcrumb = document.querySelector('nav[aria-label="Breadcrumb"]');
  if (breadcrumb) {
    breadcrumb.remove();
  }

  var titleBand = document.querySelector(".page-title-band");
  if (titleBand) {
    titleBand.remove();
  }

  var appFrame = document.getElementById("appFrame");
  if (appFrame) {
    appFrame.classList.remove("lg:pl-72");
  }
}
function simplifySidebar() {
  var current = location.pathname.split("/").pop() || "dashboard.html";
  var groupedPages = ["dashboard.html", "budget-management.html", "schemes.html", "expenditure.html", "reports.html"];

  document.querySelectorAll(".nav-link").forEach(function (link) {
    var href = (link.getAttribute("href") || "").split("#")[0];
    if (groupedPages.indexOf(href) >= 0) {
      link.remove();
    }
  });

  document.querySelectorAll(".sidebar-accordion").forEach(function (detail) {
    var firstChild = detail.querySelector(".sidebar-child-link, .sidebar-sub-link");
    var summary = detail.querySelector(".nav-section");
    if (!firstChild || !summary) {
      return;
    }

    var href = (firstChild.getAttribute("href") || "").split("#")[0];
    if (href === current) {
      detail.open = true;
      summary.classList.add("is-current");
    }
  });
}

function simplifyHeaderTools() {
  var alertButton = document.querySelector('.icon-btn[aria-label="Notifications"]');
  if (alertButton) {
    alertButton.classList.add("header-alert-btn");
    alertButton.innerHTML = '<span class="header-alert-label">Alerts</span><span class="header-alert-count">4</span>';
  }

  var searchLabel = document.querySelector("header label.relative");
  if (searchLabel) {
    searchLabel.classList.add("global-search");
  }
}

function markActiveChild() {
  var current = location.pathname.split("/").pop() || "dashboard.html";

  document.querySelectorAll(".sidebar-child-link, .sidebar-sub-link").forEach(function (link) {
    var href = (link.getAttribute("href") || "").split("#")[0];
    if (href === current) {
      link.classList.add("active-child");
      var branch = link.closest(".sidebar-child-accordion");
      if (branch) {
        branch.open = true;
        var branchSummary = branch.querySelector(":scope > summary");
        if (branchSummary) {
          branchSummary.classList.add("active-child");
        }
      }

      var group = link.closest(".sidebar-accordion");
      if (group) {
        group.open = true;
        var groupSummary = group.querySelector(":scope > summary");
        if (groupSummary) {
          groupSummary.classList.add("is-current");
        }
      }
    }
  });
}

function initFloatingSidebarMenus() {
  var sidebar = document.getElementById("sidebar");
  if (!sidebar || sidebar.dataset.floatingMenuReady === "true") {
    syncFloatingPanelState();
    return;
  }

  sidebar.dataset.floatingMenuReady = "true";

  var panel = ensureFloatingSidebarPanel();
  var state = {
    activeTrigger: null,
    closeTimer: 0
  };

  sidebar.querySelectorAll("[data-main-trigger]").forEach(function (trigger) {
    trigger.addEventListener("mouseenter", function () {
      if (window.innerWidth >= 1024 && canUseFloatingSidebar()) {
        openFloatingSidebar(trigger, state, false);
      }
    });

    trigger.addEventListener("focus", function () {
      if (canUseFloatingSidebar()) {
        openFloatingSidebar(trigger, state, true);
      }
    });

    trigger.addEventListener("mouseleave", function () {
      if (window.innerWidth >= 1024) {
        scheduleFloatingPanelClose();
      }
    });

    trigger.addEventListener("click", function (event) {
      if (!canUseFloatingSidebar()) {
        return;
      }

      var groupId = trigger.getAttribute("data-main-id");
      var navigationState = window.UBISAppIncludes && typeof window.UBISAppIncludes.getSidebarNavigationState === "function"
        ? window.UBISAppIncludes.getSidebarNavigationState()
        : [];
      var group = navigationState.find(function (item) {
        return item.id === groupId;
      });

      if (group && group.directLink && group.href) {
        return;
      }

      event.preventDefault();
      openFloatingSidebar(trigger, state, true);
    });
  });

  panel.addEventListener("mouseenter", clearFloatingPanelClose);
  panel.addEventListener("mouseleave", scheduleFloatingPanelClose);
  panel.addEventListener("focusin", clearFloatingPanelClose);
  panel.addEventListener("click", function (event) {
    if (event.target.closest("[data-floating-link]")) {
      saveSidebarState({
        desktopCollapsed: window.innerWidth >= 1024 && document.body.classList.contains("sidebar-collapsed"),
        mobileDrawerOpen: false
      });
      closeFloatingSidebar(state);
    }
  });

  document.addEventListener("click", function (event) {
    if (!panel.classList.contains("is-open")) {
      return;
    }

    if (panel.contains(event.target) || event.target.closest("[data-main-trigger]")) {
      return;
    }

    closeFloatingSidebar(state);
  });

  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
      closeFloatingSidebar(state, true);
    }
  });

  window.addEventListener("resize", function () {
    syncFloatingPanelState();
  });

  function scheduleFloatingPanelClose() {
    clearFloatingPanelClose();
    state.closeTimer = window.setTimeout(function () {
      closeFloatingSidebar(state);
    }, 220);
  }

  function clearFloatingPanelClose() {
    if (state.closeTimer) {
      window.clearTimeout(state.closeTimer);
      state.closeTimer = 0;
    }
  }
}

function ensureFloatingSidebarPanel() {
  var existingPanel = document.getElementById("sidebarFloatingMenu");
  if (existingPanel) {
    return existingPanel;
  }

  var panel = document.createElement("div");
  panel.id = "sidebarFloatingMenu";
  panel.className = "sidebar-floating-menu";
  panel.setAttribute("aria-hidden", "true");
  panel.innerHTML = '<div class="sidebar-floating-menu__title"></div><div class="sidebar-floating-menu__scroll"></div>';
  document.body.appendChild(panel);
  return panel;
}

function canUseFloatingSidebar() {
  var sidebar = document.getElementById("sidebar");
  if (!sidebar) {
    return false;
  }

  var isDesktopCollapsed = document.body.classList.contains("sidebar-collapsed") && window.innerWidth >= 1024;
  var isMobileDrawerOpen = window.innerWidth < 1024 && !sidebar.classList.contains("-translate-x-full");

  return isDesktopCollapsed || isMobileDrawerOpen;
}

function syncFloatingPanelState() {
  var panel = document.getElementById("sidebarFloatingMenu");
  if (!panel) {
    return;
  }

  if (!canUseFloatingSidebar()) {
    panel.classList.remove("is-open");
    panel.setAttribute("aria-hidden", "true");
    panel.removeAttribute("style");
    document.querySelectorAll("[data-main-trigger]").forEach(function (trigger) {
      trigger.setAttribute("aria-expanded", "false");
    });
  }
}

function openFloatingSidebar(trigger, state, shouldFocus) {
  var panel = ensureFloatingSidebarPanel();
  var mainId = trigger.getAttribute("data-main-id");
  var navigationState = window.UBISAppIncludes && typeof window.UBISAppIncludes.getSidebarNavigationState === "function"
    ? window.UBISAppIncludes.getSidebarNavigationState()
    : [];
  var group = navigationState.find(function (item) {
    return item.id === mainId;
  });

  if (!group) {
    return;
  }

  if (state.closeTimer) {
    window.clearTimeout(state.closeTimer);
    state.closeTimer = 0;
  }

  state.activeTrigger = trigger;

  renderFloatingSidebarContent(panel, group);

  document.querySelectorAll("[data-main-trigger]").forEach(function (item) {
    item.setAttribute("aria-expanded", item === trigger ? "true" : "false");
  });

  positionFloatingSidebar(panel, trigger);
  panel.classList.add("is-open");
  panel.setAttribute("aria-hidden", "false");

  if (shouldFocus) {
    var firstLink = panel.querySelector("a[href], button");
    if (firstLink) {
      firstLink.focus();
    }
  }
}

function closeFloatingSidebar(state, restoreFocus) {
  var panel = document.getElementById("sidebarFloatingMenu");
  if (!panel) {
    return;
  }

  if (state && state.closeTimer) {
    window.clearTimeout(state.closeTimer);
    state.closeTimer = 0;
  }

  panel.classList.remove("is-open");
  panel.setAttribute("aria-hidden", "true");

  document.querySelectorAll("[data-main-trigger]").forEach(function (trigger) {
    trigger.setAttribute("aria-expanded", "false");
  });

  if (restoreFocus && state && state.activeTrigger) {
    state.activeTrigger.focus();
  }
}

function renderFloatingSidebarContent(panel, group) {
  var title = panel.querySelector(".sidebar-floating-menu__title");
  var scrollHost = panel.querySelector(".sidebar-floating-menu__scroll");

  if (!title || !scrollHost) {
    return;
  }

  title.innerHTML = group.href
    ? '<a href="' + group.href + '"' + (group.current ? ' aria-current="page"' : "") + '>' + group.label + '</a>'
    : group.label;

  scrollHost.innerHTML = group.children.map(function (child) {
    var childTitle = child.directLink
      ? '<a href="' + child.href + '" class="sidebar-floating-link' + (child.active ? " is-active" : "") + '" data-floating-link' +
        (child.current ? ' aria-current="page"' : "") + ">" + child.label + "</a>"
      : '<div class="sidebar-floating-link' + (child.active ? " is-active" : "") + '">' + child.label + "</div>";
    var subMenu = child.items.length
      ? '<div class="sidebar-floating-submenu is-open">' +
        child.items.map(function (item) {
          return '<a href="' + item.href + '" class="sidebar-floating-sublink' + (item.active ? " is-active" : "") + '" data-floating-link' +
            (item.current ? ' aria-current="page"' : "") + ">" + item.label + "</a>";
        }).join("") +
        "</div>"
      : "";

    return '<div class="sidebar-floating-group">' +
      childTitle +
      subMenu +
      "</div>";
  }).join("");
}

function positionFloatingSidebar(panel, trigger) {
  var margin = 12;
  var availableHeight = Math.max(220, window.innerHeight - (margin * 2));
  var scrollHost = panel.querySelector(".sidebar-floating-menu__scroll");
  if (scrollHost) {
    scrollHost.style.maxHeight = (availableHeight - 64) + "px";
  }

  panel.style.visibility = "hidden";
  panel.style.left = "0px";
  panel.style.top = "0px";
  panel.classList.add("is-open");

  var triggerRect = trigger.getBoundingClientRect();
  var panelRect = panel.getBoundingClientRect();
  var preferredLeft = triggerRect.right + margin;
  var maxLeft = window.innerWidth - panelRect.width - margin;
  var left = Math.min(preferredLeft, Math.max(margin, maxLeft));
  var top = Math.max(margin, Math.min(triggerRect.top, window.innerHeight - panelRect.height - margin));

  panel.style.left = left + "px";
  panel.style.top = top + "px";
  panel.style.visibility = "";
}

function pruneDecorativeIcons() {
  document.querySelectorAll(".support-card > i, .quick > i, .btn-secondary i, .float-field.with-icon > i").forEach(function (icon) {
    icon.remove();
  });

  document.querySelectorAll(".float-field.with-icon").forEach(function (field) {
    field.classList.remove("with-icon");
  });
}

function initLogin() {
  var form = document.getElementById("loginForm");
  if (!form) {
    return;
  }

  var toggle = document.getElementById("togglePassword");
  var password = document.getElementById("password");
  if (toggle && password) {
    toggle.addEventListener("click", function () {
      password.type = password.type === "password" ? "text" : "password";
      var icon = toggle.querySelector("i");
      if (icon) {
        icon.setAttribute("data-lucide", password.type === "password" ? "eye" : "eye-off");
      }
      if (window.lucide) {
        lucide.createIcons();
      }
    });
  }

  form.addEventListener("submit", function (event) {
    event.preventDefault();
    if (!form.checkValidity()) {
      form.reportValidity();
      return;
    }

    var button = document.getElementById("signInBtn");
    var spinner = button ? button.querySelector("i") : null;
    var label = button ? button.querySelector("span") : null;

    if (button) {
      button.disabled = true;
    }
    if (label) {
      label.textContent = "Signing in";
    }
    if (spinner) {
      spinner.classList.remove("hidden");
    }

    window.setTimeout(function () {
      location.href = "dashboard.html";
    }, 700);
  });
}

function initCrudPages() {
  var pageKey = location.pathname.split("/").pop() || "index.html";
  var config = CRUD_CONFIGS[pageKey];
  if (!config) {
    return;
  }

  var form = document.querySelector("[data-demo-form]");
  if (!form) {
    return;
  }

  hydrateFormFields(form, config);

  var panel = resolveCrudPanel(form);
  mountCrudPanel(panel, config);

  var state = {
    config: config,
    form: form,
    panel: panel,
    records: loadRecords(config),
    activeRowEditId: "",
    sortKey: config.columns[0].key,
    sortDir: "asc",
    query: "",
    filter: "",
    busy: false
  };

  bindCrud(state);
  renderCrud(state);
  resetFormState(state);
}

function hydrateFormFields(form, config) {
  var fields = Array.from(form.querySelectorAll(".field"));

  fields.forEach(function (field, index) {
    var meta = config.columns[index];
    if (!meta) {
      return;
    }

    var control = field.querySelector("input,select,textarea");
    var label = field.querySelector("span");
    var message = field.querySelector("p");

    if (label) {
      label.innerHTML = meta.label + " <b>*</b>";
    }

    if (!control) {
      return;
    }

    control.name = meta.key;
    control.required = true;
    control.setAttribute("autocomplete", "off");

    if (control.tagName === "INPUT" && meta.inputType) {
      control.type = meta.inputType;
    }

    if (meta.inputMode) {
      control.setAttribute("inputmode", meta.inputMode);
    }

    if (meta.placeholder !== false && control.tagName !== "SELECT") {
      control.placeholder = meta.placeholder || "Enter " + meta.label.toLowerCase();
    } else if (meta.placeholder === false) {
      control.removeAttribute("placeholder");
    }

    if (message) {
      message.textContent = meta.label + " is required.";
    }
  });

  var submitButton = form.querySelector('button[type="submit"] span');
  if (submitButton) {
    submitButton.textContent = "Add Record";
  }
}

function resolveCrudPanel(form) {
  var existingTable = document.querySelector(".demo-table");
  if (existingTable) {
    return existingTable.closest("section, .panel");
  }

  var wrapper = document.createElement("section");
  wrapper.className = "panel overflow-hidden p-0";
  wrapper.dataset.crudTable = "1";

  var host = form.closest(".panel");
  if (host && host.parentElement) {
    host.parentElement.appendChild(wrapper);
  } else {
    form.parentElement.appendChild(wrapper);
  }

  return wrapper;
}

function mountCrudPanel(panel, config) {
  var filterColumn = config.columns.find(function (column) {
    return column.filterable;
  });

  panel.innerHTML = [
    '<div class="table-shell">',
    '<div class="table-shell-header">',
    '<div>',
    '<h2 class="panel-title">' + escapeHtml(config.tableTitle) + '</h2>',
    '<p class="table-shell-copy">' + escapeHtml(config.tableDescription) + '</p>',
    '</div>',
    '<div class="table-toolbar">',
    '<label class="table-toolbar-field">',
    '<span>Search</span>',
    '<input class="input table-search" type="search" placeholder="Search records" aria-label="Search records" />',
    '</label>',
    filterColumn ? buildFilterMarkup(filterColumn) : '',
    '</div>',
    '</div>',
    '<div class="table-summary"><p class="row-count text-sm text-slate-500"></p></div>',
    '<div class="overflow-x-auto"><table class="demo-table min-w-full text-sm"><thead><tr>' + buildHeaderMarkup(config.columns) + '<th class="text-right">Actions</th></tr></thead><tbody></tbody></table></div>',
    '</div>'
  ].join("");
}

function buildFilterMarkup(column) {
  return [
    '<label class="table-toolbar-field table-toolbar-field-compact">',
    '<span>' + escapeHtml(column.label) + '</span>',
    '<select class="input table-filter" aria-label="Filter by ' + escapeHtml(column.label.toLowerCase()) + '">',
    '<option value="">All</option>',
    '<option value="Approved">Approved</option>',
    '<option value="Pending">Pending</option>',
    '<option value="Review">Review</option>',
    '<option value="Active">Active</option>',
    '<option value="Inactive">Inactive</option>',
    '</select>',
    '</label>'
  ].join("");
}

function buildHeaderMarkup(columns) {
  return columns.map(function (column) {
    var align = column.align === "right" ? ' class="text-right"' : "";
    return '<th' + align + '><button type="button" class="sort-trigger" data-sort="' + escapeHtml(column.key) + '">' + escapeHtml(column.label) + '<span class="sort-indicator" aria-hidden="true"></span></button></th>';
  }).join("");
}
function bindCrud(state) {
  var form = state.form;
  var panel = state.panel;
  var search = panel.querySelector(".table-search");
  var filter = panel.querySelector(".table-filter");

  if (search) {
    search.addEventListener("input", function () {
      state.query = search.value.trim().toLowerCase();
      renderCrud(state);
    });
  }

  if (filter) {
    filter.addEventListener("change", function () {
      state.filter = filter.value.trim().toLowerCase();
      renderCrud(state);
    });
  }

  form.addEventListener("submit", function (event) {
    event.preventDefault();
    if (state.busy) {
      return;
    }

    var result = collectFormValues(state);
    if (!result.valid) {
      toast("Validation error", "Please complete all required fields correctly.", "error");
      return;
    }

    if (hasDuplicate(state, result.values, "")) {
      toast("Duplicate record", "A record with the same primary identifier already exists.", "error");
      focusUniqueField(state);
      return;
    }

    saveRecord(state, result.values);
  });

  form.addEventListener("reset", function () {
    window.setTimeout(function () {
      clearValidation(form);
      resetFormState(state);
    }, 0);
  });

  panel.addEventListener("click", function (event) {
    var button = event.target.closest("[data-action]");
    if (!button) {
      return;
    }

    var record = state.records.find(function (item) {
      return item.id === button.dataset.id;
    });
    if (!record) {
      return;
    }

    if (button.dataset.action === "view") {
      openDetailsDialog(state.config, record);
      return;
    }

    if (button.dataset.action === "edit") {
      beginRowEdit(state, record.id);
      return;
    }

    if (button.dataset.action === "save") {
      saveInlineRow(state, record.id);
      return;
    }

    if (button.dataset.action === "reset") {
      resetInlineRow(state, record.id);
      return;
    }

    if (button.dataset.action === "cancel") {
      cancelInlineRow(state, record.id);
      return;
    }

    if (button.dataset.action === "delete") {
      openConfirmDialog("Delete record?", "This action permanently removes the selected record.", "Delete", function () {
        deleteRecord(state, record.id);
      }, true);
    }
  });

  panel.querySelectorAll("[data-sort]").forEach(function (trigger) {
    trigger.addEventListener("click", function () {
      var key = trigger.dataset.sort;
      if (state.sortKey === key) {
        state.sortDir = state.sortDir === "asc" ? "desc" : "asc";
      } else {
        state.sortKey = key;
        state.sortDir = "asc";
      }

      renderCrud(state);
    });
  });
}

function collectFormValues(state) {
  var values = {};
  var valid = true;

  state.config.columns.forEach(function (column) {
    var control = state.form.querySelector('[name="' + column.key + '"]');
    var field = control ? control.closest('.field') : null;
    var rawValue = control ? control.value.trim() : "";
    var message = validateValue(column, rawValue);

    if (message) {
      valid = false;
    }

    if (field) {
      setFieldState(field, message);
    }

    values[column.key] = rawValue;
  });

  return { valid: valid, values: values };
}

function validateValue(column, value) {
  if (!value) {
    return column.label + " is required.";
  }

  if (column.inputType === "email" && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
    return "Enter a valid email address.";
  }

  return "";
}

function setFieldState(field, message) {
  field.classList.toggle("invalid", Boolean(message));
  var help = field.querySelector("p");
  if (help && message) {
    help.textContent = message;
  }
}

function clearValidation(form) {
  form.querySelectorAll(".field").forEach(function (field) {
    field.classList.remove("invalid");
  });
}

function hasDuplicate(state, values, excludedId) {
  var key = state.config.uniqueKey;
  if (!key) {
    return false;
  }

  var nextValue = normalizeValue(values[key]);
  return state.records.some(function (record) {
    if (excludedId && record.id === excludedId) {
      return false;
    }

    return normalizeValue(record[key]) === nextValue;
  });
}

function focusUniqueField(state) {
  var key = state.config.uniqueKey;
  if (!key) {
    return;
  }

  var control = state.form.querySelector('[name="' + key + '"]');
  if (control) {
    control.focus();
  }
}

function saveRecord(state, values) {
  state.busy = true;
  setSubmitState(state.form, true, "Saving...");

  window.setTimeout(function () {
    state.records.unshift(mergeRecord(createId(state.config.storageKey), values));
    persistRecords(state.config, state.records);
    renderCrud(state);
    state.form.reset();
    clearValidation(state.form);
    resetFormState(state);
    state.busy = false;
    toast("Record saved", "The table was refreshed without reloading the page.", "success");
  }, 220);
}

function beginRowEdit(state, id) {
  if (state.activeRowEditId && state.activeRowEditId !== id) {
    toast("Finish current edit", "Save or delete the row already in edit mode before editing another.", "info");
    return;
  }

  state.activeRowEditId = id;
  renderCrud(state);

  var firstInput = state.panel.querySelector('tr[data-row-id="' + id + '"] .row-inline-input');
  if (firstInput) {
    firstInput.focus();
    firstInput.select();
  }
}

function saveInlineRow(state, id) {
  var row = state.panel.querySelector('tr[data-row-id="' + id + '"]');
  if (!row) {
    return;
  }

  var values = {};
  var errors = [];

  state.config.columns.forEach(function (column) {
    var input = row.querySelector('[data-inline-field="' + column.key + '"]');
    var value = input ? input.value.trim() : "";
    var message = validateValue(column, value);

    if (input) {
      input.classList.toggle("inline-error", Boolean(message));
      input.setAttribute("aria-invalid", message ? "true" : "false");
    }

    if (message) {
      errors.push(message);
    }

    values[column.key] = value;
  });

  if (errors.length) {
    toast("Validation error", errors[0], "error");
    return;
  }

  if (hasDuplicate(state, values, id)) {
    toast("Duplicate record", "A record with the same primary identifier already exists.", "error");
    return;
  }

  openConfirmDialog("Save changes?", "The selected row will be updated immediately.", "Save", function () {
    state.records = state.records.map(function (record) {
      return record.id === id ? mergeRecord(id, values) : record;
    });
    state.activeRowEditId = "";
    persistRecords(state.config, state.records);
    renderCrud(state);
    toast("Record updated", "The row was updated successfully.", "success");
  }, false);
}

function resetInlineRow(state, id) {
  var record = state.records.find(function (item) {
    return item.id === id;
  });
  var row = state.panel.querySelector('tr[data-row-id="' + id + '"]');
  if (!record || !row) {
    return;
  }

  state.config.columns.forEach(function (column) {
    var input = row.querySelector('[data-inline-field="' + column.key + '"]');
    if (input) {
      input.value = record[column.key] || "";
      input.classList.remove("inline-error");
      input.setAttribute("aria-invalid", "false");
    }
  });

  toast("Row reset", "Original values have been restored for this row.", "info");
}

function cancelInlineRow(state, id) {
  if (state.activeRowEditId !== id) {
    return;
  }

  state.activeRowEditId = "";
  renderCrud(state);
  toast("Edit cancelled", "The row returned to read-only mode.", "info");
}

function deleteRecord(state, id) {
  state.records = state.records.filter(function (record) {
    return record.id !== id;
  });

  if (state.activeRowEditId === id) {
    state.activeRowEditId = "";
  }

  persistRecords(state.config, state.records);
  renderCrud(state);
  toast("Record deleted", "The entry has been removed successfully.", "success");
}

function resetFormState(state) {
  var heading = state.form.querySelector("h2");
  if (heading) {
    heading.textContent = state.config.formTitle;
  }

  setSubmitState(state.form, false, "Add Record");
}
function renderCrud(state) {
  var body = state.panel.querySelector("tbody");
  var count = state.panel.querySelector(".row-count");
  var filterable = state.config.columns.find(function (column) {
    return column.filterable;
  });
  var query = state.query;
  var activeFilter = state.filter;

  var filtered = state.records.filter(function (record) {
    var matchesQuery = !query || state.config.columns.some(function (column) {
      return String(record[column.key] || "").toLowerCase().includes(query);
    });
    var matchesFilter = !filterable || !activeFilter || normalizeValue(record[filterable.key]) === normalizeValue(activeFilter);
    return matchesQuery && matchesFilter;
  });

  filtered.sort(function (left, right) {
    return compareRecords(left, right, state);
  });

  body.innerHTML = filtered.length ? filtered.map(function (record) {
    return buildRowMarkup(record, state);
  }).join("") : '<tr><td colspan="' + (state.config.columns.length + 1) + '" class="empty-state-cell">No records match the current filters.</td></tr>';

  if (count) {
    count.textContent = "Showing " + filtered.length + " of " + state.records.length + " records";
  }

  state.panel.querySelectorAll("[data-sort]").forEach(function (trigger) {
    var indicator = trigger.querySelector(".sort-indicator");
    if (!indicator) {
      return;
    }

    indicator.textContent = trigger.dataset.sort === state.sortKey ? (state.sortDir === "asc" ? "^" : "v") : "";
  });

  if (window.lucide) {
    lucide.createIcons();
  }
}

function buildRowMarkup(record, state) {
  var isEditing = state.activeRowEditId === record.id;
  var cells = state.config.columns.map(function (column) {
    var align = column.align === "right" ? ' class="text-right"' : "";
    var content = isEditing ? buildEditableCell(column, record[column.key]) : escapeHtml(formatCellValue(record[column.key]));
    return '<td' + align + '>' + content + '</td>';
  }).join("");

  return [
    '<tr data-row-id="' + escapeHtml(record.id) + '" class="' + (isEditing ? 'table-row-editing' : '') + '">',
    cells,
    '<td class="action-stack action-cell">',
    isEditing ? buildActionIcon("save", record.id, "Save row", "save") : buildActionIcon("view", record.id, "View details", "eye"),
    isEditing ? buildActionIcon("reset", record.id, "Reset row values", "rotate-ccw") : buildActionIcon("edit", record.id, "Edit row", "square-pen"),
    isEditing ? buildActionIcon("cancel", record.id, "Cancel editing", "x") : "",
    buildActionIcon("delete", record.id, "Delete row", "trash-2"),
    '</td>',
    '</tr>'
  ].join("");
}

function buildEditableCell(column, value) {
  var type = column.inputType || "text";
  var attrs = [
    'class="input row-inline-input"',
    'data-inline-field="' + escapeHtml(column.key) + '"',
    'type="' + escapeHtml(type) + '"',
    'value="' + escapeHtml(value || "") + '"',
    'placeholder="' + escapeHtml(column.placeholder || column.label) + '"',
    'aria-label="' + escapeHtml(column.label) + '"'
  ];

  if (column.inputMode) {
    attrs.push('inputmode="' + escapeHtml(column.inputMode) + '"');
  }

  return '<div class="row-inline-cell">' + '<input ' + attrs.join(" ") + ' />' + '</div>';
}

function buildActionIcon(action, id, label, icon) {
  var tone = "table-action-icon";
  if (action === "delete") {
    tone = "table-action-danger";
  } else if (action === "save") {
    tone = "table-action-save";
  } else if (action === "reset") {
    tone = "table-action-reset";
  } else if (action === "cancel") {
    tone = "table-action-cancel";
  }
  return '<button type="button" class="table-icon-btn ' + tone + '" data-action="' + escapeHtml(action) + '" data-id="' + escapeHtml(id) + '" title="' + escapeHtml(label) + '" aria-label="' + escapeHtml(label) + '"><i data-lucide="' + escapeHtml(icon) + '"></i></button>';
}

function compareRecords(left, right, state) {
  var key = state.sortKey;
  var column = state.config.columns.find(function (item) {
    return item.key === key;
  }) || state.config.columns[0];
  var multiplier = state.sortDir === "asc" ? 1 : -1;
  var leftValue = left[key] || "";
  var rightValue = right[key] || "";

  if (column.sortType === "number") {
    return ((parseFloat(leftValue) || 0) - (parseFloat(rightValue) || 0)) * multiplier;
  }

  if (column.sortType === "date") {
    return (new Date(leftValue).getTime() - new Date(rightValue).getTime()) * multiplier;
  }

  return String(leftValue).localeCompare(String(rightValue), undefined, { sensitivity: "base" }) * multiplier;
}

function loadRecords(config) {
  try {
    var saved = JSON.parse(localStorage.getItem(config.storageKey) || "null");
    if (Array.isArray(saved) && saved.length) {
      return saved;
    }
  } catch (error) {
    console.warn("Unable to parse saved records for", config.storageKey, error);
  }

  persistRecords(config, config.seed || []);
  return (config.seed || []).slice();
}

function persistRecords(config, records) {
  localStorage.setItem(config.storageKey, JSON.stringify(records));
}

function mergeRecord(id, values) {
  var next = { id: id };
  Object.keys(values).forEach(function (key) {
    next[key] = values[key];
  });
  return next;
}

function createId(prefix) {
  return prefix + "-" + Date.now();
}

function setSubmitState(form, isBusy, label) {
  var button = form.querySelector('button[type="submit"]');
  var text = button ? button.querySelector("span") : null;
  var spinner = button ? button.querySelector("i") : null;

  if (button) {
    button.disabled = isBusy;
  }

  if (text) {
    text.textContent = label;
  }

  if (spinner) {
    spinner.classList.toggle("hidden", !isBusy);
  }
}

// Shared app-wide dialog control (client request 2026-08-25: "create a tool/control type and call
// with heading and message from everywhere required" — for all PreBudget, ECL, and future web
// modules, not just the appendix screens that already called openInfoDialog). Renders the SAME
// visual design as the Login screen's Change Password dialog (.modal-backdrop/.verify-modal/
// .modal-header/.modal-body/.modal-actions — see styles.css), not the older, smaller
// .confirm-backdrop/.confirm-card style openDetailsDialog below still uses for its wider record-
// detail grid (a distinct feature, left alone). Built fresh and appended to document.body on every
// call rather than requiring each page to pre-declare a dialog element in its own markup - `is-open`
// is added one animation frame after insertion so the CSS transition (opacity/transform) still
// plays, matching how Login's own pre-declared dialogs animate in.
function buildAppDialog(heading, bodyHtml, actionsHtml) {
  var wrap = document.createElement("div");
  wrap.className = "modal-backdrop";
  wrap.setAttribute("role", "presentation");
  wrap.innerHTML = [
    '<section class="verify-modal" role="dialog" aria-modal="true" aria-labelledby="appDialogTitle">',
    '<header class="modal-header"><div><h2 id="appDialogTitle">' + escapeHtml(heading) + '</h2></div></header>',
    '<div class="modal-body">' + bodyHtml + '<div class="modal-actions">' + actionsHtml + '</div></div>',
    '</section>'
  ].join("");

  document.body.appendChild(wrap);
  requestAnimationFrame(function () { wrap.classList.add("is-open"); });
  return wrap;
}

function closeAppDialog(wrap) {
  wrap.remove();
}

// Confirmation dialog: Cancel + a labelled confirm action (e.g. "Are you sure to freeze?").
function openConfirmDialog(title, message, confirmLabel, onConfirm, danger) {
  var wrap = buildAppDialog(
    title,
    '<p style="margin:0;color:var(--ubis-muted);font-size:.92rem;line-height:1.5">' + escapeHtml(message) + '</p>',
    '<button class="cancel" type="button" data-cancel>Cancel</button>' +
      '<button class="confirm" type="button" data-confirm' + (danger ? ' style="background:#dc2626"' : '') + '>' + escapeHtml(confirmLabel) + '</button>'
  );

  function close() { closeAppDialog(wrap); document.removeEventListener("keydown", onKeydown); }
  function onKeydown(e) { if (e.key === "Escape") close(); }

  wrap.querySelector("[data-cancel]").addEventListener("click", close);
  wrap.querySelector("[data-confirm]").addEventListener("click", function () { onConfirm(); close(); });
  document.addEventListener("keydown", onKeydown);
}

// Single-OK informational dialog — the canonical way every module shows a validation summary
// ("Please Enter required Fields" / "Select Scheme, Enter values for User Remarks, RE 2025-2026")
// or a save/modify confirmation ("Record Saved" / "Record has been successfully saved."), per the
// client's exact wording (2026-08-25). `message` may contain newlines - rendered as separate
// lines, not escaped/joined into one.
function openInfoDialog(title, message) {
  var lines = String(message).split("\n").map(function (line) {
    return '<div style="margin-bottom:4px">' + escapeHtml(line) + "</div>";
  }).join("");
  var wrap = buildAppDialog(
    title,
    '<div style="color:var(--ubis-muted);font-size:.92rem;line-height:1.5">' + lines + "</div>",
    '<button class="confirm" type="button" data-close>OK</button>'
  );

  function close() { closeAppDialog(wrap); document.removeEventListener("keydown", onKeydown); }
  function onKeydown(e) { if (e.key === "Escape") close(); }

  wrap.querySelector("[data-close]").addEventListener("click", close);
  document.addEventListener("keydown", onKeydown);
}

function openDetailsDialog(config, record) {
  var wrap = document.createElement("div");
  wrap.className = "confirm-backdrop";
  wrap.innerHTML = [
    '<div class="confirm-card confirm-card-wide" role="dialog" aria-modal="true">',
    '<h2 class="text-xl font-black text-navy dark:text-white">' + escapeHtml(config.entityName.charAt(0).toUpperCase() + config.entityName.slice(1)) + ' details</h2>',
    '<div class="detail-grid">' + config.columns.map(function (column) {
      return '<div class="detail-item"><span>' + escapeHtml(column.label) + '</span><strong>' + escapeHtml(formatCellValue(record[column.key])) + '</strong></div>';
    }).join("") + '</div>',
    '<div class="confirm-actions"><button class="btn-primary" data-close>Close</button></div>',
    '</div>'
  ].join("");

  document.body.appendChild(wrap);
  wrap.querySelector("[data-close]").addEventListener("click", function () { wrap.remove(); });
}
function initCharts() {
  document.querySelectorAll(".chart-bars").forEach(function (chart) {
    var values = (chart.dataset.values || "40,60,80").split(",").map(Number);
    chart.innerHTML = values.map(function (value) {
      return '<div style="height:' + value + '%" title="' + value + '%"></div>';
    }).join("");
  });
}

function initToasts() {
  document.querySelectorAll("[data-toast]").forEach(function (button) {
    if (button.dataset.bound) {
      return;
    }

    button.dataset.bound = "1";
    button.addEventListener("click", function () {
      toast("Done", button.dataset.toast, "info");
    });
  });
}

function toast(title, message, tone) {
  var region = document.getElementById("toastRegion");
  if (!region) {
    return;
  }

  var item = document.createElement("div");
  item.className = "toast-item toast-" + (tone || "info");
  item.innerHTML = '<p class="font-black">' + escapeHtml(title) + '</p><p class="mt-1 text-sm text-slate-500">' + escapeHtml(message) + '</p>';
  region.appendChild(item);

  window.setTimeout(function () {
    item.remove();
  }, 2800);
}

function formatCellValue(value) {
  if (value === undefined || value === null || value === "") {
    return "-";
  }

  return String(value);
}

function normalizeValue(value) {
  return String(value || "").trim().toLowerCase();
}

function escapeHtml(value) {
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/\"/g, "&quot;")
    .replace(/'/g, "&#39;");
}
