document.addEventListener("DOMContentLoaded", function () {
  initTheme();
  initShell();
  initAccordion();
  initPasswordToggle();
  initChartBars();
  initAutoSubmitSelects();
  initInputMasking();
  initNumericLengthLimit();
  initRequiredFieldHighlight();
  initNavigationLoader();
  initBreadcrumbCurrentPageColor();

  if (window.lucide) {
    lucide.createIcons();
  }
});

// Client requirement 2026-08-31: "Routing text on all pages, like Pre Budget Meeting / Data Entry
// / Autonomous Master for Appendix VI C and VI E, current page text ... should be in black text
// color" - the last " / "-separated segment (the current page's own title) should stand out from
// the rest of the trail. Every page's breadcrumb is server-rendered as one plain-text
// @ViewData["Breadcrumb"] string (see BreadcrumbFilter) inside an identical, scaffolder-templated
// <p class="text-xs font-bold uppercase tracking-wide text-teal"> - confirmed via a repo-wide grep
// that this exact class combination appears exactly once per page, across 764 view files, so it's
// safe to target globally here instead of hand-editing every one of those files (which all just
// read the same ViewData key as inert text - splitting it out this way, once, covers all of them
// instead of needing @Html.Raw() added to each). Dark-mode-safe rather than literally always black
// (pure black text on this app's dark slate panel background in dark mode would be unreadable) -
// same reasoning as every other "make this text black" fix this session, which paired it with a
// dark: override.
function initBreadcrumbCurrentPageColor() {
  document.querySelectorAll("p.text-xs.font-bold.uppercase.tracking-wide.text-teal").forEach(function (el) {
    var text = el.textContent;
    if (!text || text.indexOf(" / ") === -1) {
      return;
    }

    var segments = text.split(" / ");
    var currentPage = segments.pop();

    el.textContent = "";
    el.appendChild(document.createTextNode(segments.join(" / ") + " / "));

    var currentPageSpan = document.createElement("span");
    currentPageSpan.className = "text-slate-900 dark:text-white";
    currentPageSpan.textContent = currentPage;
    el.appendChild(currentPageSpan);
  });
}

// Client requirement 2026-08-21: every page must show the user something is happening the instant
// they click a link or submit a form - both so a slow page load doesn't look broken, and so a
// second click on the same link/button can't fire a duplicate request. This is intentionally the
// FIRST step (a site-wide navigation loader), not per-page AJAX data-loading - that's a much larger,
// page-by-page conversion (ECL's Data Analysis already does it as the template: instant shell, then
// its own spinner while the grid loads) planned as separate follow-up work module by module.
//
// Only intercepts things that will cause a real full-page navigation:
//   - <a href="..."> with a genuine same-tab, same-document navigation target.
//   - <form> submits that actually go through (checked via a deferred event.defaultPrevented read,
//     see below - this is what makes it safe to add site-wide without auditing every existing
//     AJAX form/link one by one).
// Explicitly skipped: modifier-clicks (ctrl/cmd/shift/middle-click - opens a new tab, this page
// never navigates away), target="_blank", download links, #/javascript:/mailto:/tel: hrefs, and
// anything already carrying its own loading UX (data-export-link - the ECL export buttons already
// show "Generating Report..." - or an explicit data-no-loader opt-out).
function initNavigationLoader() {
  var overlay = document.getElementById("navLoaderOverlay");
  if (!overlay) return;

  var FAILSAFE_MS = 15000;
  var failsafeTimer = null;

  function showLoader() {
    // Reuses the existing shared .ubis-loading-overlay pattern (Login/Dashboard) rather than a new
    // one - same "is-visible" toggle class, same spinner/backdrop styling already shipped.
    overlay.classList.add("is-visible");
    failsafeTimer = window.setTimeout(hideLoader, FAILSAFE_MS);
  }

  function hideLoader() {
    overlay.classList.remove("is-visible");
    if (failsafeTimer) {
      window.clearTimeout(failsafeTimer);
      failsafeTimer = null;
    }
  }

  // A real navigation destroys this whole document (and the failsafe timer with it), so hideLoader
  // only ever actually runs in the "navigation didn't happen after all" case (blocked validation,
  // an in-page anchor, a cancelled confirm(), etc.) - restoring the page instead of leaving it
  // looking permanently frozen.
  document.addEventListener("click", function (event) {
    if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
      return;
    }

    var link = event.target.closest("a[href]");
    if (!link) return;

    var href = link.getAttribute("href");
    if (!href || href.charAt(0) === "#" || href.indexOf("javascript:") === 0 || href.indexOf("mailto:") === 0 || href.indexOf("tel:") === 0) {
      return;
    }
    if (link.target === "_blank" || link.hasAttribute("download") || link.hasAttribute("data-no-loader") || link.hasAttribute("data-export-link")) {
      return;
    }

    showLoader();
  });

  document.addEventListener("submit", function (event) {
    var form = event.target;
    if (!(form instanceof HTMLFormElement) || form.hasAttribute("data-no-loader")) {
      return;
    }

    // Deferred so every other "submit" listener on this same form (e.g. a page's own
    // event.preventDefault() for an AJAX-driven filter form) has already run by the time this
    // checks event.defaultPrevented - this is what lets the loader apply site-wide without needing
    // every existing/future AJAX form to opt out individually.
    window.setTimeout(function () {
      if (!event.defaultPrevented) {
        showLoader();
      }
    }, 0);
  });
}

function initTheme() {
  if (localStorage.getItem("theme") === "dark") {
    document.documentElement.setAttribute("data-theme", "dark");
    document.documentElement.classList.add("dark");
  }

  document.querySelectorAll("#themeToggle").forEach(function (button) {
    button.addEventListener("click", function () {
      var isDark = document.documentElement.classList.toggle("dark");
      document.documentElement.setAttribute("data-theme", isDark ? "dark" : "light");
      localStorage.setItem("theme", isDark ? "dark" : "light");
    });
  });
}

function initShell() {
  var body = document.body;
  var sidebar = document.getElementById("sidebar");
  var overlay = document.getElementById("drawerOverlay");
  var desktopToggle = document.getElementById("sidebarToggle");

  if (desktopToggle) {
    desktopToggle.addEventListener("click", function () {
      var isCollapsed = body.classList.toggle("sidebar-collapsed");
      updateSidebarToggleButton(isCollapsed);
    });
  }

  // Client requirement 2026-08-27: "collapse sidebar" tooltip -> "Hide Menu List"; while
  // collapsed, the button swaps to the horizontally-mirrored icon and "Expand Menu List", then
  // swaps back once the sidebar is re-expanded - same setAttribute("data-lucide", ...) +
  // lucide.createIcons() re-render pattern initPasswordToggle already uses for its eye/eye-off swap.
  function updateSidebarToggleButton(isCollapsed) {
    if (!desktopToggle) return;
    desktopToggle.title = isCollapsed ? "Expand Menu List" : "Hide Menu List";
    // By the time this first runs, lucide has already replaced the original <i data-lucide="...">
    // with a rendered <svg> (site.js's own DOMContentLoaded handler calls lucide.createIcons()
    // once at the very end) - querySelector("i") would find nothing at that point, so this has to
    // match on the data-lucide attribute itself (which lucide carries over onto the rendered <svg>)
    // rather than assuming the tag is still <i>.
    var icon = desktopToggle.querySelector("[data-lucide]");
    if (icon) {
      icon.setAttribute("data-lucide", isCollapsed ? "panel-left-open" : "panel-left-close");
    }
    if (window.lucide) {
      lucide.createIcons();
    }
  }

  document.querySelectorAll("[data-sidebar-mobile]").forEach(function (button) {
    button.addEventListener("click", function () {
      if (!sidebar) return;
      var isHidden = sidebar.classList.contains("-translate-x-full");
      sidebar.classList.toggle("-translate-x-full", !isHidden);
      if (overlay) {
        overlay.classList.toggle("hidden", !isHidden);
      }
    });
  });

  if (overlay) {
    overlay.addEventListener("click", closeDrawer);
  }

  window.addEventListener("resize", function () {
    if (window.innerWidth >= 1024) {
      closeDrawer();
    }
  });

  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
      closeDrawer();
    }
  });

  function closeDrawer() {
    if (!sidebar) return;
    sidebar.classList.add("-translate-x-full");
    if (overlay) {
      overlay.classList.add("hidden");
    }
  }
}

function initAccordion() {
  // Delegated on the capture phase (2026-07-20): the sidebar's menu content can now be replaced
  // after the initial page load (see dashboard-bootstrap.js, which fetches the menu tree
  // separately from login so Dashboard doesn't wait on it) — a direct per-element listener
  // attached only at DOMContentLoaded would miss any .sidebar-accordion swapped in afterwards.
  // The native "toggle" event on <details> doesn't bubble, so this has to listen during capture
  // (capture-phase delivery doesn't depend on bubbling) rather than delegate the usual way.
  document.addEventListener("toggle", function (event) {
    var detail = event.target;
    if (!(detail instanceof HTMLElement) || !detail.classList.contains("sidebar-accordion") || !detail.open) {
      return;
    }

    document.querySelectorAll(".sidebar-accordion").forEach(function (other) {
      if (other !== detail && !other.contains(detail) && !detail.contains(other)) {
        other.open = false;
      }
    });
  }, true);
}

function initChartBars() {
  document.querySelectorAll(".chart-bars").forEach(function (chart) {
    var values = (chart.dataset.values || "40,60,80").split(",").map(Number);
    chart.innerHTML = values.map(function (value) {
      return '<div style="height:' + value + '%" title="' + value + '%"></div>';
    }).join("");
  });
}

// Replaces onchange="this.form.submit()" attributes (blocked by the app's own
// Content-Security-Policy: script-src 'self' - CSP disallows ALL inline script, both event
// handler attributes and inline <script> blocks, so this has to live in an external file like
// this one) on the demand/appendix filter <select> elements across the Pre-Budget Meeting pages.
//
// Client requirement 2026-08-28 ("load it immediately with loader" on REMeetingAllocation):
// investigated why the site-wide navigation loader (initNavigationLoader above) never showed for
// any of these auto-submit filters, on any page - found that HTMLFormElement.submit() is
// spec'd to NOT dispatch a "submit" event at all (only a real user-driven submission, or the
// newer requestSubmit(), fires one), so initNavigationLoader's document-level "submit" listener
// never saw these navigations. Every data-auto-submit select on every page using this function
// was affected, not just the one that was reported - switched to requestSubmit() (fires a real,
// cancelable submit event, same as a user pressing Enter/clicking a submit button) with a
// submit() fallback for the rare browser without it.
function initAutoSubmitSelects() {
  document.querySelectorAll("[data-auto-submit]").forEach(function (select) {
    select.addEventListener("change", function () {
      if (this.form) {
        if (this.form.requestSubmit) {
          this.form.requestSubmit();
        } else {
          this.form.submit();
        }
      }
    });
  });
}

// Strips disallowed characters as the user types, e.g. <input data-mask="digits"
// data-mask-maxlength="10">. Same CSP reasoning as initAutoSubmitSelects above - this replaces
// what used to be one-off inline <script> blocks on individual pages (also silently blocked by
// script-src 'self'), consolidated here so every masked field behaves consistently. The real
// enforcement is always the server-side [RegularExpression]/[Required] on the ViewModel plus
// jQuery Unobtrusive Validation on submit - this is just so invalid characters never appear in
// the box in the first place.
function initInputMasking() {
  var patterns = {
    digits: /\D/g,
    ip: /[^0-9.]/g,
    // Client requirement 2026-08-31 (Appendix II's "MoF Approval Details" fields): "should not
    // able to type in special characters." Strips at the keystroke, not a form-level pattern="" -
    // deliberately NOT the same mistake as the Appendix VI/VII-A "letters and spaces only" bug
    // fixed earlier this session (a pattern="" that keeps the field permanently invalid no matter
    // what's typed, looping the same validation dialog forever). Approval-reference text
    // routinely needs digits and common reference punctuation ("F.No. 7(3)/2024-Bgt dated
    // 12.03.2025"), so this only blocks genuinely unusual symbols
    // (<>{}[]\|~`^*+=_!@$%";) rather than restricting to letters alone.
    "no-special-chars": /[<>{}[\]\\|~`^*+=_!@$%";]/g,
    // Bug report 2026-08-31 (Appendix VI-B's "Remarks" field, "on edit Remarks field is not
    // ediable"): root cause was the field's own pattern="[A-Za-z0-9\s.@@/#$%,'()]*" - the Razor
    // @@-escape for a literal "@" immediately followed by "/" in that attribute value corrupted
    // the SERVER'S OWN rendered HTML (confirmed by reading the raw HTML the server actually sent,
    // before any client JS ran: a stray `disabled` attribute landed on the element, mid-mangled
    // pattern/title text) - not a client-side bug at all. Replaces that pattern="" (already a
    // known-bad approach per the "no-special-chars" comment above) with the same keystroke-mask
    // convention, preserving this field's own originally-intended allowed set (letters/digits/
    // whitespace plus . @ / # $ % , ' ( ) - wider than "no-special-chars" allows, since Remarks
    // here was deliberately meant to accept those symbols).
    "vib-remarks": /[^A-Za-z0-9\s.@/#$%,'()]/g,
    // Bug report 2026-09-08 (Appendix VI-C's "Reasons for Creation of Corpus Fund" - native
    // "Please match the requested format." validation blocking a legitimate save): the exact same
    // pattern="[A-Za-z0-9\s.@@/#$%,'()]*" Razor-@@-corruption bug as vib-remarks above, just never
    // hit until now because this field is optional (no `required`) - the browser only ever runs a
    // pattern check once the field has *some* value, so an empty field never triggered it during
    // earlier testing. Same fix, same originally-intended allowed character set.
    "vic-corpus-reason": /[^A-Za-z0-9\s.@/#$%,'()]/g
  };

  // Delegated on "document" (same pattern as initNumericLengthLimit below), not a one-time
  // querySelectorAll at DOMContentLoaded - found live while wiring Appendix II's new
  // "no-special-chars" mask: this page's fields only exist once loaded into #partialViewContainer
  // by the AJAX Demand/Appendix cascade, well after DOMContentLoaded already ran, so the old
  // one-time binding would have silently never applied to any of them (same class of "wired but
  // never actually runs" bug already found and fixed elsewhere this session). Delegation makes
  // every data-mask field work app-wide regardless of when it entered the DOM, with zero per-page
  // changes - fixes this for the two pre-existing digits/ip callers too (EditProfile.cshtml,
  // UserProfile/Index.cshtml - both already worked since those are plain full pages, not AJAX
  // fragments, so this is a no-risk improvement for them, not a behavior change).
  document.addEventListener("input", function (event) {
    var el = event.target;
    if (!el.classList) return;

    // Client requirement 2026-09-02 (prebudget.css's .TextualData): "only characters a-z are
    // permitted, no numeric, no special character" - the class name alone is the trigger here
    // (unlike every other mask below, which needs an explicit data-mask="..." attribute), so any
    // field across every PreBudget appendix just has to carry class="TextualData ..." to get this
    // for free, no second attribute to remember. Spaces are allowed too (not literally a-z only) -
    // a field that rejects the space bar would make any multi-word text impossible to type at all,
    // the same class of bug already documented above for "letters and spaces only" fields.
    if (el.classList.contains("TextualData")) {
      el.value = el.value.replace(/[^a-zA-Z\s]/g, "");
    }

    // Client requirement 2026-09-02 (prebudget.css's .RemarksText): letters, digits, full stop,
    // comma, and spaces - deliberately not applied to any Remarks-shaped field that already
    // carries data-mask="no-special-chars"/"vib-remarks" or its own pattern="" (see the CSS
    // comment for why - those need wider reference-style punctuation this set doesn't allow).
    if (el.classList.contains("RemarksText")) {
      el.value = el.value.replace(/[^a-zA-Z0-9.,\s]/g, "");
    }

    if (!el.hasAttribute || !el.hasAttribute("data-mask")) return;
    var pattern = patterns[el.getAttribute("data-mask")];
    if (!pattern) return;
    var maxLength = parseInt(el.getAttribute("data-mask-maxlength"), 10);

    var stripped = el.value.replace(pattern, "");
    el.value = maxLength ? stripped.slice(0, maxLength) : stripped;
  });
}

// Client requirement 2026-08-27: "all numeric input fields in all forms ia all modules: max
// diigits 15 inlcuding two decimal places and dot", refined to a regex shape (2026-08-27 follow-up):
// "12 numric digits then optional . then optional 2 digits" -> ^\d{0,12}(\.\d{0,2})?$ (12 integer
// digits + optional '.' + up to 2 decimal digits = the "999999999999.99" shape, 15 characters max).
// type="number" doesn't honor the maxlength/pattern attributes in any browser, so this has to be JS
// - but delegated on "document" (like initNavigationLoader above) instead of touching every
// appendix/form page across PreBudget/ECL/SBE: it applies to every input[type=number] app-wide,
// including ones an AJAX fragment swaps in later, with zero per-page changes needed. Any keystroke
// that would break the pattern is rejected outright (reverted to the last value that still matched)
// rather than silently truncated, so the field's value always satisfies the regex. data-allow-negative
// fields (e.g. Appendix IV-A's ActualsUptoSept, see pre-budget-meeting.js) get a leading "-" allowed.
function initNumericLengthLimit() {
  var PATTERN = /^\d{0,12}(\.\d{0,2})?$/;
  var PATTERN_ALLOW_NEGATIVE = /^-?\d{0,12}(\.\d{0,2})?$/;

  document.addEventListener("input", function (event) {
    var input = event.target;
    if (!(input instanceof HTMLInputElement) || input.type !== "number") return;

    var value = input.value;
    var pattern = input.hasAttribute("data-allow-negative") ? PATTERN_ALLOW_NEGATIVE : PATTERN;

    if (value === "" || pattern.test(value)) {
      input.dataset.lastValidNumeric = value;
      return;
    }

    input.value = input.dataset.lastValidNumeric || "";
  });
}

// Client requirement 2026-08-27, revised same day: "dont show light red this way. controls will
// be default color, lightred color only display after save or modify button will be clicked to
// show what values are missed to enter by user." Replaces the earlier native-:invalid CSS-only
// approach (which highlighted empty required fields immediately on page load) - fields now stay
// default-colored until a real Save/Submit/Modify attempt, and only the required fields still
// empty/invalid at that moment get the .field-missing class (styled in styles.css). Delegated on
// "document" (single global entry, same pattern as every other init* here) so it covers every
// form across every module with zero per-page changes.
function initRequiredFieldHighlight() {
  // Capture phase, and hooked to the button's "click" rather than the form's "submit": every
  // appendix page's own Save/Submit handler is a click listener on the submit button that calls
  // event.preventDefault() itself (to show its own validation dialog) whenever the form is
  // invalid - which stops the click's default action before native HTML5 validation ever runs and
  // before "submit" would fire, so a "submit" listener here would never see the invalid case at
  // all. Capture phase means this runs before that per-page handler, so the highlight always
  // appears the instant Save/Submit/Modify is clicked, independent of what the page does next.
  document.addEventListener("click", function (event) {
    var button = event.target.closest('button[type="submit"], input[type="submit"]');
    if (!button || button.hasAttribute("formnovalidate")) return;

    var form = button.form;
    if (!(form instanceof HTMLFormElement)) return;

    highlightMissingRequiredFields(form);
  }, true);

  // Native constraint validity (checkValidity()) recomputes itself on every value change, so this
  // just has to re-check and drop the class the instant a previously-flagged field becomes valid -
  // "once value is enter light red color shold go".
  ["input", "change"].forEach(function (eventName) {
    document.addEventListener(eventName, function (event) {
      var el = event.target;
      if (!el.classList || !el.classList.contains("field-missing")) return;
      if (typeof el.checkValidity === "function" && el.checkValidity()) {
        el.classList.remove("field-missing");
      }
    });
  });

  // Bug report 2026-08-31 (Appendix III, but this is the same shared, module-wide handler every
  // form uses - not III-specific): "Reset button not dis-coloring validating red textboxes." A
  // native form.reset() restores field VALUES but dispatches no "input"/"change" event per field,
  // so the two listeners just above (the only place .field-missing ever gets removed) never ran -
  // a field flagged red by a failed Submit stayed visibly red after Reset even though its value
  // did clear. The "reset" DOM event does bubble (same assumption pre-budget-meeting.js's own
  // per-appendix reset handlers already rely on), so delegating on "document" covers every reset
  // button on every form for free, same as every other init* here.
  document.addEventListener("reset", function (event) {
    var form = event.target;
    if (!(form instanceof HTMLFormElement)) return;
    form.querySelectorAll(".field-missing").forEach(function (el) {
      el.classList.remove("field-missing");
    });
  });
}

function highlightMissingRequiredFields(form) {
  form.querySelectorAll("[required]").forEach(function (field) {
    if (typeof field.checkValidity !== "function") return;
    field.classList.toggle("field-missing", !field.checkValidity());
  });
}

function initPasswordToggle() {
  document.querySelectorAll("[data-toggle-password]").forEach(function (toggle) {
    var targetId = toggle.getAttribute("data-toggle-password");
    var input = document.getElementById(targetId);
    if (!input) return;

    toggle.addEventListener("click", function () {
      input.type = input.type === "password" ? "text" : "password";
      var icon = toggle.querySelector("i");
      if (icon) {
        icon.setAttribute("data-lucide", input.type === "password" ? "eye" : "eye-off");
      }
      if (window.lucide) {
        lucide.createIcons();
      }
    });
  });
}
