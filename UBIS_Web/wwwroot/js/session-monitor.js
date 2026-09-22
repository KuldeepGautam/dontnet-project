// Section 3: (1) an idle-triggered reverse countdown - hidden and not running while the user is
// active; starts only after IDLE_THRESHOLD_MS of no mouse/keyboard/touch activity, showing
// "Logout in N seconds" and counting down to a forced logout, and is cancelled (hidden, timer
// reset) the moment any activity is seen again, even mid-countdown. Bug fix 2026-08-06: this used
// to start a fixed MM:SS countdown immediately on page load regardless of activity, so an actively
// working user would still get logged out on a timer. (2) a remote kill-switch - polls AIM (via a
// same-origin proxy) for "was my IP/Mobile change request just approved," and if so shows a
// 60-second warning before forcing logout. Only runs on authenticated pages (see the data
// attribute check below).
(function () {
  "use strict";

  var POLL_INTERVAL_MS = 5000;
  var KILL_SWITCH_WARNING_SECONDS = 60;
  // Client requirement 2026-09-03: "when user idle from 10 sec, then timer starts from 15
  // minutes" - supersedes the earlier 10-minute threshold below (which existed to give a
  // read-heavy page more room before the countdown even appeared). Now deliberately aggressive:
  // 10 seconds of no mouse/keyboard/touch/scroll activity starts the full 15-minute countdown
  // (data-countdown-seconds, SessionCountdownViewComponent) - any activity still resets it back
  // to the full 15:00 immediately (see onActivity below), so this only actually matters once the
  // user has genuinely stepped away.
  var IDLE_THRESHOLD_MS = 10 * 1000;
  // Client requirement 2026-08-20: once the idle logout countdown (below) drops into its last 2
  // minutes, show a modal alarm with its own live reverse countdown, instead of only the small
  // header badge text change - clicking OK counts as activity (same effect as any other click) and
  // dismisses it; letting it run out logs the user out exactly as before.
  var MODAL_WARNING_SECONDS = 2 * 60;
  var ACTIVITY_EVENTS = ["mousemove", "mousedown", "keydown", "touchstart", "wheel", "scroll"];

  function csrfToken() {
    var meta = document.querySelector('meta[name="csrf-token"]');
    return meta ? meta.content : "";
  }

  function formatMMSS(totalSeconds) {
    var minutes = Math.max(0, Math.floor(totalSeconds / 60));
    var seconds = Math.max(0, totalSeconds % 60);
    return String(minutes).padStart(2, "0") + ":" + String(seconds).padStart(2, "0");
  }

  function initCountdown(container) {
    var totalSeconds = parseInt(container.getAttribute("data-countdown-seconds"), 10);
    if (!totalSeconds || totalSeconds <= 0) {
      return;
    }

    var modal = document.getElementById("sessionExpiryModal");
    var modalCountdownEl = document.getElementById("sessionExpiryCountdown");
    var modalOkButton = document.getElementById("sessionExpiryOk");

    var idleTimer = null;
    var countdownTimer = null;
    var lastActivityReset = 0;

    function hideModal() {
      if (modal) {
        modal.classList.remove("is-open");
      }
    }

    function showModal(remaining) {
      if (modal && !modal.classList.contains("is-open")) {
        modal.classList.add("is-open");
      }
      if (modalCountdownEl) {
        modalCountdownEl.textContent = formatMMSS(remaining);
      }
    }

    function stopCountdown() {
      if (countdownTimer) {
        clearInterval(countdownTimer);
        countdownTimer = null;
      }
      container.style.color = "";
      container.style.fontWeight = "";
      container.textContent = formatMMSS(totalSeconds);
      hideModal();
    }

    function startCountdown() {
      var remaining = totalSeconds;
      container.style.color = "#dc2626";
      container.style.fontWeight = "700";
      // Client requirement 2026-09-03: "time must show in format Minutes:Seconds" - was "Logout
      // in Ns"; formatMMSS already existed for the modal's own display and the idle (not yet
      // counting down) badge state, just never used for the counting-down badge itself.
      container.textContent = formatMMSS(remaining);
      if (remaining <= MODAL_WARNING_SECONDS) {
        showModal(remaining);
      }

      countdownTimer = setInterval(function () {
        remaining -= 1;
        if (remaining <= 0) {
          clearInterval(countdownTimer);
          countdownTimer = null;
          hideModal();
          forceLogout();
          return;
        }
        container.textContent = formatMMSS(remaining);
        if (remaining <= MODAL_WARNING_SECONDS) {
          showModal(remaining);
        }
      }, 1000);
    }

    function armIdleTimer() {
      if (idleTimer) {
        clearTimeout(idleTimer);
      }
      idleTimer = setTimeout(startCountdown, IDLE_THRESHOLD_MS);
    }

    function onActivity() {
      // Lightly throttled - mousemove/scroll can fire many times a second, and every call here
      // is just a clearTimeout/setTimeout pair, not worth doing on literally every event.
      var now = Date.now();
      if (now - lastActivityReset < 250) {
        return;
      }
      lastActivityReset = now;

      if (countdownTimer) {
        stopCountdown();
      }
      armIdleTimer();
    }

    if (modalOkButton) {
      // A click already counts as activity via the document-level "mousedown" listener below (it
      // fires first, since this handler is bound after DOMContentLoaded's initCountdown call
      // during the same synchronous pass) - this handler exists to be explicit and to close the
      // modal even in the rare case a future change makes clicking it not otherwise count as
      // activity (e.g. if mousedown were ever removed from ACTIVITY_EVENTS).
      modalOkButton.addEventListener("click", function () {
        if (countdownTimer) {
          stopCountdown();
        }
        armIdleTimer();
      });
    }

    container.textContent = formatMMSS(totalSeconds);
    ACTIVITY_EVENTS.forEach(function (eventName) {
      document.addEventListener(eventName, onActivity, { passive: true });
    });
    armIdleTimer();
  }

  function submitLogoutForm() {
    var form = document.createElement("form");
    form.method = "post";
    form.action = "/User/Logout";
    var token = document.createElement("input");
    token.type = "hidden";
    token.name = "__RequestVerificationToken";
    token.value = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    form.appendChild(token);
    document.body.appendChild(form);
    form.submit();
  }

  function forceLogout() {
    // Client requirement 2026-09-03: "all sessions for the user should be invalidated" once the
    // idle countdown reaches 0 - revokes every active session for this user server-side (AIM's
    // logout-all-sessions via /api/session/logout-all) before navigating away with the existing
    // /User/Logout POST, which only ever cleared THIS browser's own cookie/session. Best-effort:
    // whether the revoke call succeeds or fails, the logout form still submits either way -
    // losing this browser's own session was already guaranteed before this change, this only adds
    // the "everywhere" reach on top of it.
    fetch("/api/session/logout-all", { method: "POST", headers: { "X-CSRF-TOKEN": csrfToken() } })
      .catch(function () {
        // offline/unreachable - the logout form below still runs regardless
      })
      .then(submitLogoutForm);
  }

  function showKillSwitchWarning(headline) {
    if (document.getElementById("kill-switch-banner")) {
      return; // already showing
    }

    var banner = document.createElement("div");
    banner.id = "kill-switch-banner";
    banner.className = "kill-switch-banner";
    banner.innerHTML =
      "<strong>" + headline + "</strong>" +
      "<p>Please save your recent work. You will be automatically logged out in " +
      '<span id="kill-switch-seconds">' + KILL_SWITCH_WARNING_SECONDS + "</span> seconds.</p>";
    document.body.appendChild(banner);

    var remaining = KILL_SWITCH_WARNING_SECONDS;
    var secondsEl = document.getElementById("kill-switch-seconds");
    var timer = setInterval(function () {
      remaining -= 1;
      if (secondsEl) {
        secondsEl.textContent = String(remaining);
      }
      if (remaining <= 0) {
        clearInterval(timer);
        forceLogout();
      }
    }, 1000);
  }

  function pollChangeRequestStatus() {
    fetch("/api/session/change-request-status", { headers: { "X-CSRF-TOKEN": csrfToken() } })
      .then(function (res) {
        return res.ok ? res.json() : null;
      })
      .then(function (body) {
        if (body && body.justApproved) {
          showKillSwitchWarning("Your IP address change request has been approved.");
        }
      })
      .catch(function () {
        // offline/unreachable — try again on the next interval, don't spam the console
      });
  }

  // Client requirement 2026-08-10: a password or role change made directly in the DB (not through
  // this app) must also force re-login, same as the IP-approval kill-switch above - reuses the
  // identical warn-then-logout UX rather than a second bespoke pattern.
  function pollSecurityStampStatus() {
    fetch("/api/session/security-stamp-status", { headers: { "X-CSRF-TOKEN": csrfToken() } })
      .then(function (res) {
        return res.ok ? res.json() : null;
      })
      .then(function (body) {
        if (body && body.changed) {
          showKillSwitchWarning("Your account credentials have changed.");
        }
      })
      .catch(function () {
        // offline/unreachable — try again on the next interval, don't spam the console
      });
  }

  // Client requirement 2026-09-03: "by any case, if token get expired or invalidated, user
  // session should end and user should redirect to login page." This heartbeat already fires
  // every 5 seconds on every authenticated page - a 401 specifically (not a network hiccup, which
  // still just retries next interval below) means AIM rejected the token because its session was
  // revoked elsewhere (idle-timeout logout-all on another tab/device, an admin force-logout, a
  // password change), not that AIM itself is briefly unreachable. Forces the same real logout
  // (revoke-all + redirect) the idle countdown itself triggers, so a revoked session ends within
  // one poll cycle instead of the user only discovering it whenever they next click something
  // that happens to call a live microservice endpoint.
  function sendHeartbeat() {
    fetch("/api/session/heartbeat", { method: "POST", headers: { "X-CSRF-TOKEN": csrfToken() } })
      .then(function (res) {
        if (res.status === 401) {
          forceLogout();
        }
      })
      .catch(function () {
        // offline/unreachable — try again on the next interval
      });
  }

  document.addEventListener("DOMContentLoaded", function () {
    var countdownEl = document.getElementById("session-countdown");
    if (!countdownEl) {
      return; // anonymous page (e.g. Login) — nothing to monitor
    }

    initCountdown(countdownEl);
    setInterval(pollChangeRequestStatus, POLL_INTERVAL_MS);
    setInterval(pollSecurityStampStatus, POLL_INTERVAL_MS);
    setInterval(sendHeartbeat, POLL_INTERVAL_MS);
  });
})();
