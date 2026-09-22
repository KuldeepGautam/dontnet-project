document.addEventListener("DOMContentLoaded", function () {
    var dismiss = document.getElementById("compliance-warning-dismiss");
    var modal = document.getElementById("compliance-warning-modal");
    if (!dismiss || !modal) return;

    dismiss.addEventListener("click", function () {
        modal.remove();
        fetch("/User/AcknowledgeComplianceWarning", {
            method: "POST",
            headers: { "X-CSRF-TOKEN": document.querySelector('meta[name="csrf-token"]')?.content || "" }
        }).catch(function () { /* best-effort */ });
    });
});
