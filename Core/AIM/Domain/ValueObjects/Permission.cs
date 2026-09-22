namespace UBIS.Services.Aim.Domain.ValueObjects;

/// <summary>
/// Represents CRUD (Create, Read, Update, Delete) permissions as an immutable value object.
/// Used to encapsulate permission checks in a type-safe manner.
/// </summary>
public class Permission : IEquatable<Permission>
{
    /// <summary>
    /// Indicates if Create operation is permitted.
    /// </summary>
    public bool CanCreate { get; }

    /// <summary>
    /// Indicates if Read operation is permitted.
    /// </summary>
    public bool CanRead { get; }

    /// <summary>
    /// Indicates if Update operation is permitted.
    /// </summary>
    public bool CanUpdate { get; }

    /// <summary>
    /// Indicates if Delete operation is permitted.
    /// </summary>
    public bool CanDelete { get; }

    public Permission(bool canCreate = false, bool canRead = true, bool canUpdate = false, bool canDelete = false)
    {
        CanCreate = canCreate;
        CanRead = canRead;
        CanUpdate = canUpdate;
        CanDelete = canDelete;
    }

    /// <summary>
    /// Generates a permission matrix string suitable for JWT claims (e.g., "CRU", "CR", "R").
    /// </summary>
    /// <returns>String representation of permissions (e.g., "CRUD", "CRU", "R").</returns>
    public string ToPermissionMatrix()
    {
        var matrix = string.Empty;
        if (CanCreate) matrix += "C";
        if (CanRead) matrix += "R";
        if (CanUpdate) matrix += "U";
        if (CanDelete) matrix += "D";
        return string.IsNullOrEmpty(matrix) ? "NONE" : matrix;
    }

    /// <summary>
    /// Checks if user has permission for the specified operation.
    /// </summary>
    public bool HasPermission(string operation)
    {
        return operation.ToUpperInvariant() switch
        {
            "CREATE" or "C" => CanCreate,
            "READ" or "R" => CanRead,
            "UPDATE" or "U" => CanUpdate,
            "DELETE" or "D" => CanDelete,
            _ => false
        };
    }

    /// <summary>
    /// Determines if any CRUD permission is granted.
    /// </summary>
    public bool HasAnyPermission => CanCreate || CanRead || CanUpdate || CanDelete;

    /// <summary>
    /// Determines if user has full CRUD permissions.
    /// </summary>
    public bool HasFullPermissions => CanCreate && CanRead && CanUpdate && CanDelete;

    public bool Equals(Permission? other)
    {
        if (other is null) return false;
        return CanCreate == other.CanCreate &&
               CanRead == other.CanRead &&
               CanUpdate == other.CanUpdate &&
               CanDelete == other.CanDelete;
    }

    public override bool Equals(object? obj) => Equals(obj as Permission);

    public override int GetHashCode()
    {
        return HashCode.Combine(CanCreate, CanRead, CanUpdate, CanDelete);
    }

    public override string ToString() => ToPermissionMatrix();

    public static bool operator ==(Permission? left, Permission? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Permission? left, Permission? right) => !(left == right);
}
