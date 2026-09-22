namespace UBIS.Web.Tools.MenuScaffolder;

// Minimal local copies of the AIM/MenuGenerator response shapes this tool reads.

public sealed record RoleDetailDto(int RoleId, string RoleName);

public sealed record MenuFunctionDto(int FunctionId, string FunctionName, string ActionSlug, string Url);

public sealed record MenuModuleDto(int ModuleId, string ModuleName, string ControllerSlug, List<MenuFunctionDto> Functions);

public sealed record MenuAppDto(int AppId, string AppName, string AreaSlug, List<MenuModuleDto> Modules);

public sealed record MenuFullResponseDto(List<MenuAppDto> Apps);

/// <summary>One (Area, Controller, Action) route discovered from the live menu tree.</summary>
public sealed record RouteEntry(string AreaSlug, string ControllerSlug, string ActionSlug, string AppName, string ModuleName, string FunctionName);
