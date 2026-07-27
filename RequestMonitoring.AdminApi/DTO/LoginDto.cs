namespace RequestMonitoring.AdminApi.DTO;
/// <summary>
/// DTO для передачи данных при входе в систему
/// </summary>
/// <param name="Login"></param>
/// <param name="Password"></param>
public record LoginDto(string Login, string Password);