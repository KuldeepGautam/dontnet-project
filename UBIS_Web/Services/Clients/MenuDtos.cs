namespace UBIS.Web.Services.Clients;

// Local mirror of MenuGenerator's Application/DTOs/MenuFullResponseDto.cs shape.

public sealed record MenuFunctionDto(
    int FunctionId,
    string FunctionName,
    string ActionSlug,
    string Url);

public sealed record MenuModuleDto(
    int ModuleId,
    string ModuleName,
    string ControllerSlug,
    List<MenuFunctionDto> Functions);

public sealed record MenuAppDto(
    int AppId,
    string AppName,
    string AreaSlug,
    List<MenuModuleDto> Modules);

public sealed record MenuFullResponseDto(
    List<MenuAppDto> Apps);
