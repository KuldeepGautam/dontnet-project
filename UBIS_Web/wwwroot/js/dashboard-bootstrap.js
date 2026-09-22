// Deferred post-login data (2026-07-20; role added 2026-08-03). Login used to block on fetching
// the role assignment (AIM), the menu tree (MenuGenerator), and the compliance snapshot (AIM)
// before it could even redirect to Dashboard — see UserController.CompleteSignInAsync's doc
// comment. All three are now fetched here instead, right after an authenticated page has already
// rendered, fired in parallel (none of these three fetch calls awaits another) so Dashboard shows
// up immediately and each piece fills in as it completes rather than the page waiting on the
// slowest one.
document.addEventListener("DOMContentLoaded", function () {
    var sidebar = document.getElementById("sidebar");
    if (!sidebar) {
        return; // anonymous page (e.g. Login) — nothing to bootstrap
    }

    // Only present on Dashboard itself (see Dashboard.cshtml) - null on every other authenticated
    // page, so every use below is guarded and simply becomes a no-op there.
    var statusBox = document.getElementById("bootstrapStatus");
    var statusRoleEl = document.getElementById("bootstrapStatusRole");
    var statusMenuEl = document.getElementById("bootstrapStatusMenu");
    var loadingOverlay = document.getElementById("dashboardStatus");
    var roleDone = false;
    var menuDone = false;

    function hideStatusBoxIfAllDone() {
        if (roleDone && menuDone) {
            if (statusBox) { statusBox.remove(); }
            if (loadingOverlay) { loadingOverlay.classList.remove("is-visible"); }
        }
    }

    function markDone(el, which) {
        if (el) {
            el.textContent = el.getAttribute("data-done") || el.textContent;
        }
        if (which === "role") { roleDone = true; }
        if (which === "menu") { menuDone = true; }
        hideStatusBoxIfAllDone();
    }

    // Present on every authenticated page's header (_UserMenu.cshtml) and, additionally, in the
    // welcome banner on Dashboard itself (Dashboard.cshtml) - update whichever of the two exist.

    function applyRoleName(roleName) {
        var targets = [
            document.getElementById("userMenuRoleName"),
            document.getElementById("dashboardRoleName"),
            document.getElementById("headerProfileRoleName"),
            document.getElementById("headerMenuRoleName")
        ];
        targets.forEach(function (el) {
            if (el) {
                el.textContent = el.id === "userMenuRoleName" ? "(" + roleName + ")" : roleName;
            }
        });
    }

    fetch("/User/ResolveMyRole", { headers: { "X-Requested-With": "XMLHttpRequest" } })
        .then(function (res) { return res.ok ? res.json() : Promise.reject(); })
        .then(function (data) {
            if (data.success && data.roleName) {
                applyRoleName(data.roleName);
            }
            markDone(statusRoleEl, "role");
        })
        .catch(function () {
            // Leave the "Loading…" placeholders as-is; the next page navigation (which re-reads
            // session server-side) picks up the role once a later ResolveMyRole call succeeds.
            markDone(statusRoleEl, "role");
        });

    if (sidebar.getAttribute("data-menu-loaded") === "false") {
        fetch("/User/SidebarMenuPartial", { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (res) { return res.ok ? res.text() : Promise.reject(); })
            .then(function (html) {
                var fetched = document.createElement("div");
                fetched.innerHTML = html.trim();
                var fetchedRoot = fetched.firstElementChild;
                var fetchedNav = fetched.querySelector("nav");
                var currentNav = sidebar.querySelector("nav");

                // Only the <nav> (menu content) is swapped in, not the whole #sidebar — the
                // header above it (logo, mobile close button) has its own listeners bound once
                // at page load (see site.js initShell) that would be lost if that markup were
                // replaced instead of the element they're attached to just being left alone.
                if (fetchedNav && currentNav) {
                    currentNav.innerHTML = fetchedNav.innerHTML;
                }
                if (fetchedRoot) {
                    sidebar.setAttribute("data-menu-loaded", fetchedRoot.getAttribute("data-menu-loaded") || "true");
                }
                window.lucide && window.lucide.createIcons();
                markDone(statusMenuEl, "menu");
            })
            .catch(function () {
                // Leave the "Loading your menu…" placeholder as-is; nothing more useful to show
                // without knowing why it failed, and the next page navigation will just retry.
                markDone(statusMenuEl, "menu");
            });
    } else {
        markDone(statusMenuEl, "menu");
    }

    // Populates session with the compliance snapshot (freeze/IP-binding/staleness) for this and
    // future requests on this session. Not attempting to dynamically show the staleness modal
    // from this response — that's rendered server-side from session at page-load time already
    // (see UserController.Dashboard), so it simply starts working correctly from the next
    // navigation once this completes, rather than adding a second client-side render path here.
    fetch("/User/RefreshComplianceStatus", { headers: { "X-Requested-With": "XMLHttpRequest" } })
        .catch(function () {
            // Offline/unreachable — IP-binding/freeze enforcement just stays inactive until a
            // later page load succeeds; nothing for the user to act on here.
        });
});
