using System.Text;

namespace RequestMonitoring.Library.Shared;

public static class LogSanitizer
{
    /// <summary>
    /// Удаляет управляющие символы из строки перед записью в лог
    /// </summary>
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (!char.IsControl(ch))
                sb.Append(ch);
        }
        return sb.ToString();
    }
}
