// Tiny reusable admin utility: prints a BCrypt hash for a plaintext password, using the exact same
// work factor (12) AuthenticationService.AuthenticateAsync/SetPasswordInternal already use in
// Core/AIM, so a hash produced here is indistinguishable from one the real app would generate.
// Not a general-purpose password tool - just the one thing an admin needs when hand-writing an
// UPDATE M_User SET Password = '...' statement (e.g. Reset-UserPasswordToDefault.ps1 at the repo
// root). Prints ONLY the hash to stdout - safe to capture with $(dotnet run ...) from PowerShell.
//
// Usage: dotnet run --project Tools/PasswordHasher -- <plaintext-password> [workFactor]

if (args.Length < 1 || string.IsNullOrEmpty(args[0]))
{
    Console.Error.WriteLine("Usage: dotnet run --project Tools/PasswordHasher -- <plaintext-password> [workFactor]");
    return 1;
}

var plaintext = args[0];
var workFactor = args.Length > 1 && int.TryParse(args[1], out var wf) ? wf : 12;

Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(plaintext, workFactor));
return 0;
