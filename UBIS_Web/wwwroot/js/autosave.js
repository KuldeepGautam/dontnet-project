// Offline-safe autosave for data-entry pages: <form data-autosave-key="...">.
// Dual tier — localStorage (works fully offline) + a Redis-backed server endpoint
// (survives a browser crash/forced sign-out, not just this tab). Never auto-applies a
// recovered draft silently: always asks via a dismissible Restore/Discard banner.
(function () {
  "use strict";

  var SAVE_DEBOUNCE_MS = 2000;
  var SERVER_PUSH_INTERVAL_MS = 10000;

  function csrfToken() {
    var meta = document.querySelector('meta[name="csrf-token"]');
    return meta ? meta.content : "";
  }

  function collectFormData(form) {
    var data = {};
    Array.prototype.forEach.call(form.elements, function (el) {
      if (!el.name || el.type === "password" || el.type === "submit" || el.type === "button" || el.type === "hidden") {
        return;
      }
      if (el.type === "checkbox" || el.type === "radio") {
        if (el.checked) {
          data[el.name] = el.value;
        }
      } else {
        data[el.name] = el.value;
      }
    });
    return data;
  }

  function applyFormData(form, data) {
    Object.keys(data).forEach(function (name) {
      var el = form.elements.namedItem(name);
      if (!el || el.type === "password") {
        return;
      }
      if (el.type === "checkbox" || el.type === "radio") {
        el.checked = el.value === data[name];
      } else {
        el.value = data[name];
      }
    });
  }

  function localStorageKey(key) {
    return "ubis-draft:" + key;
  }

  function saveLocal(key, data) {
    try {
      localStorage.setItem(localStorageKey(key), JSON.stringify({ data: data, savedAtUtc: new Date().toISOString() }));
    } catch (e) {
      // Storage full/unavailable (private browsing, quota) — the server tier still covers us when online.
    }
  }

  function loadLocal(key) {
    try {
      var raw = localStorage.getItem(localStorageKey(key));
      return raw ? JSON.parse(raw) : null;
    } catch (e) {
      return null;
    }
  }

  function clearLocal(key) {
    try {
      localStorage.removeItem(localStorageKey(key));
    } catch (e) {
      // ignore
    }
  }

  function pushServer(key, data) {
    if (!navigator.onLine) {
      return;
    }
    fetch("/api/draft/save", {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": csrfToken() },
      body: JSON.stringify({ key: key, dataJson: JSON.stringify(data) })
    }).catch(function () {
      // Offline or AIM/Redis-backed endpoint unavailable — localStorage already has this draft.
    });
  }

  function fetchServer(key) {
    return fetch("/api/draft/get?key=" + encodeURIComponent(key))
      .then(function (res) {
        if (!res.ok) {
          return null;
        }
        return res.json().then(function (body) {
          return { data: JSON.parse(body.dataJson), savedAtUtc: body.savedAtUtc };
        });
      })
      .catch(function () {
        return null;
      });
  }

  function clearServer(key) {
    fetch("/api/draft/" + encodeURIComponent(key), {
      method: "DELETE",
      headers: { "X-CSRF-TOKEN": csrfToken() }
    }).catch(function () {
      // best-effort; localStorage clear already happened
    });
  }

  function showRestoreBanner(form, savedAtUtc, onRestore, onDiscard) {
    var banner = document.createElement("div");
    banner.className = "autosave-banner";
    var when = new Date(savedAtUtc).toLocaleString();
    banner.innerHTML =
      "<span>We recovered a draft from " + when + ".</span>" +
      '<button type="button" data-restore>Restore</button>' +
      '<button type="button" data-discard>Discard</button>';
    form.prepend(banner);

    banner.querySelector("[data-restore]").addEventListener("click", function () {
      onRestore();
      banner.remove();
    });
    banner.querySelector("[data-discard]").addEventListener("click", function () {
      onDiscard();
      banner.remove();
    });
  }

  function initAutosaveForm(form) {
    var key = form.getAttribute("data-autosave-key");
    if (!key) {
      return;
    }

    var local = loadLocal(key);
    fetchServer(key).then(function (server) {
      var newest = null;
      if (local && server) {
        newest = new Date(local.savedAtUtc) >= new Date(server.savedAtUtc) ? local : server;
      } else {
        newest = local || server;
      }

      if (newest) {
        showRestoreBanner(
          form,
          newest.savedAtUtc,
          function () {
            applyFormData(form, newest.data);
          },
          function () {
            clearLocal(key);
            clearServer(key);
          }
        );
      }
    });

    var saveTimer = null;
    var dirty = false;

    form.addEventListener("input", function () {
      dirty = true;
      clearTimeout(saveTimer);
      saveTimer = setTimeout(function () {
        saveLocal(key, collectFormData(form));
      }, SAVE_DEBOUNCE_MS);
    });

    var pushTimer = setInterval(function () {
      if (!dirty) {
        return;
      }
      pushServer(key, collectFormData(form));
      dirty = false;
    }, SERVER_PUSH_INTERVAL_MS);

    form.addEventListener("submit", function () {
      clearLocal(key);
      clearServer(key);
      clearInterval(pushTimer);
    });

    // Native Reset button and the appendix Cancel-edit button (pre-budget-meeting.js's
    // exitAppendixEditMode, which calls form.reset() programmatically — that also dispatches
    // this same "reset" event) both mean the user explicitly discarded what they typed, unlike
    // an accidental tab close/refresh. Without this, the next page load would still offer to
    // restore the just-discarded draft.
    form.addEventListener("reset", function () {
      clearLocal(key);
      clearServer(key);
      dirty = false;
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    Array.prototype.forEach.call(document.querySelectorAll("form[data-autosave-key]"), initAutosaveForm);
  });
})();
