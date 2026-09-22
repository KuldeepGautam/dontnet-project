document.addEventListener("DOMContentLoaded", function () {
    window.lucide && window.lucide.createIcons();

    var dialog = document.getElementById("forgotPasswordDialog");
    var openButton = document.getElementById("openForgotPassword");
    if (!dialog || !openButton) { return; }

    var errorEl = document.getElementById("forgotPasswordError");
    var infoEl = document.getElementById("forgotPasswordInfo");
    var tokenInput = dialog.querySelector('input[name="__RequestVerificationToken"]');
    var csrfToken = tokenInput ? tokenInput.value : "";

    var steps = {
        unavailable: document.getElementById("fpStepUnavailable"),
        request: document.getElementById("fpRequestForm"),
        verify: document.getElementById("fpVerifyForm"),
        newPassword: document.getElementById("fpNewPasswordForm"),
        success: document.getElementById("fpStepSuccess")
    };

    var userNameInput = document.getElementById("fpUserName");
    var otpInput = document.getElementById("fpOtp");
    var newPasswordInput = document.getElementById("fpNewPassword");
    var confirmPasswordInput = document.getElementById("fpConfirmPassword");
    var successMessageEl = document.getElementById("fpSuccessMessage");

    // Carried between steps within one dialog session - never touches the DOM/URL.
    var currentUserName = "";
    var currentResetToken = "";

    function showStep(name) {
        Object.keys(steps).forEach(function (key) {
            if (steps[key]) { steps[key].style.display = key === name ? "" : "none"; }
        });
        if (errorEl) { errorEl.textContent = ""; }
        if (infoEl) { infoEl.textContent = ""; }
    }

    function postJson(url, body) {
        return fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": csrfToken,
                "X-Requested-With": "XMLHttpRequest"
            },
            body: JSON.stringify(body || {})
        }).then(function (res) { return res.ok ? res.json() : Promise.reject(); });
    }

    function resetDialog() {
        currentUserName = "";
        currentResetToken = "";
        if (userNameInput) { userNameInput.value = ""; }
        if (otpInput) { otpInput.value = ""; }
        if (newPasswordInput) { newPasswordInput.value = ""; }
        if (confirmPasswordInput) { confirmPasswordInput.value = ""; }
        showStep("request");
    }

    function closeDialog() {
        dialog.classList.remove("is-open");
        // Defensive, same reasoning as the open handler below: make sure Change Password can never
        // be left mid-fade (or stale-open) behind this dialog as it closes.
        var changeDialog = document.getElementById("changePasswordDialog");
        if (changeDialog) {
            changeDialog.classList.add("no-transition");
            changeDialog.classList.remove("is-open");
            void changeDialog.offsetWidth;
            changeDialog.classList.remove("no-transition");
        }
        // Deferred until the fade-out finishes (matches the 180ms .modal-backdrop transition) -
        // resetDialog() calls showStep("request"), which swaps the dialog's visible content (e.g.
        // from the "unavailable" message back to the username-entry form). Doing that immediately
        // swapped the content WHILE the dialog was still visible and fading out, which looked like
        // a second, different dialog flashing in right before the close completed.
        window.setTimeout(resetDialog, 180);
    }

    openButton.addEventListener("click", function () {
        // Mutual exclusion with the Change Password dialog (both share the same full-screen
        // .modal-backdrop/z-index). Closed INSTANTLY (no fade) - both dialogs otherwise animate
        // opacity over 180ms, and fading this one out at the same moment the other fades in makes
        // them briefly cross-fade into each other (visible as "the other dialog flashes").
        var changeDialog = document.getElementById("changePasswordDialog");
        if (changeDialog) {
            changeDialog.classList.add("no-transition");
            changeDialog.classList.remove("is-open");
            void changeDialog.offsetWidth; // forces the browser to apply no-transition before it's removed
            changeDialog.classList.remove("no-transition");
        }

        dialog.classList.add("is-open");
        resetDialog();

        fetch("/User/ForgotPasswordAvailability", { headers: { "X-Requested-With": "XMLHttpRequest" } })
            .then(function (res) { return res.ok ? res.json() : Promise.reject(); })
            .then(function (data) {
                showStep(data.available ? "request" : "unavailable");
            })
            .catch(function () {
                showStep("unavailable");
            });
    });

    var closeButton = document.getElementById("fpClose");
    var closeUnavailableButton = document.getElementById("fpCloseUnavailable");
    var cancelRequestButton = document.getElementById("fpCancelRequest");
    var closeSuccessButton = document.getElementById("fpCloseSuccess");
    [closeButton, closeUnavailableButton, cancelRequestButton, closeSuccessButton].forEach(function (btn) {
        if (btn) { btn.addEventListener("click", closeDialog); }
    });

    function requestOtp(isResend) {
        var userName = userNameInput ? userNameInput.value.trim() : "";
        if (!userName) {
            if (errorEl) { errorEl.textContent = "Enter your username."; }
            return;
        }

        postJson("/User/RequestPasswordResetOtp", { UserName: userName })
            .then(function (data) {
                if (data.available === false) {
                    showStep("unavailable");
                    return;
                }
                if (!data.success) {
                    if (errorEl) { errorEl.textContent = data.errorMessage || "Could not send a code. Please try again."; }
                    return;
                }

                currentUserName = userName;
                if (isResend) {
                    showStep("verify");
                    if (infoEl) { infoEl.textContent = "A new code has been sent."; }
                } else {
                    showStep("verify");
                    var channelParts = [];
                    if (data.maskedEmail) { channelParts.push(data.maskedEmail); }
                    if (data.maskedMobile) { channelParts.push(data.maskedMobile); }
                    if (infoEl) {
                        infoEl.textContent = channelParts.length
                            ? "If this account exists, a code was sent to " + channelParts.join(" and ") + "."
                            : data.message;
                    }
                }
            })
            .catch(function () {
                if (errorEl) { errorEl.textContent = "Could not reach the server. Please try again."; }
            });
    }

    if (steps.request) {
        steps.request.addEventListener("submit", function (event) {
            event.preventDefault();
            requestOtp(false);
        });
    }

    var resendButton = document.getElementById("fpResend");
    if (resendButton) {
        resendButton.addEventListener("click", function () {
            if (userNameInput) { userNameInput.value = currentUserName; }
            requestOtp(true);
        });
    }

    if (steps.verify) {
        steps.verify.addEventListener("submit", function (event) {
            event.preventDefault();
            var otp = otpInput ? otpInput.value.trim() : "";
            if (!otp) {
                if (errorEl) { errorEl.textContent = "Enter the 6-digit code."; }
                return;
            }

            postJson("/User/VerifyPasswordResetOtp", { UserName: currentUserName, Otp: otp })
                .then(function (data) {
                    if (!data.success) {
                        if (errorEl) { errorEl.textContent = data.errorMessage || "Invalid or expired code."; }
                        return;
                    }
                    currentResetToken = data.resetToken;
                    showStep("newPassword");
                })
                .catch(function () {
                    if (errorEl) { errorEl.textContent = "Could not reach the server. Please try again."; }
                });
        });
    }

    if (steps.newPassword) {
        steps.newPassword.addEventListener("submit", function (event) {
            event.preventDefault();
            var newPassword = newPasswordInput ? newPasswordInput.value : "";
            var confirmPassword = confirmPasswordInput ? confirmPasswordInput.value : "";
            if (newPassword !== confirmPassword) {
                if (errorEl) { errorEl.textContent = "Passwords do not match."; }
                return;
            }

            postJson("/User/CompletePasswordReset", {
                ResetToken: currentResetToken,
                NewPassword: newPassword,
                ConfirmNewPassword: confirmPassword
            })
                .then(function (data) {
                    if (!data.success) {
                        if (errorEl) { errorEl.textContent = data.errorMessage || "This code has expired. Please start over."; }
                        return;
                    }
                    if (successMessageEl) { successMessageEl.textContent = data.message || "Your password has been reset."; }
                    showStep("success");
                })
                .catch(function () {
                    if (errorEl) { errorEl.textContent = "Could not reach the server. Please try again."; }
                });
        });
    }
});
