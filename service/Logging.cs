public static partial class Logging {
	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to convert key.")]
	public static partial void LogDerringError(this ILogger logger, Exception e);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Parameters indicate that '{key}' is a conversion target, but '{clash}' already exists.")]
	public static partial void LogNameClash(this ILogger logger, string key, string clash);
}
