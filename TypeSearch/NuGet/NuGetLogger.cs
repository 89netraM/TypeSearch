using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using INuGetLogger = NuGet.Common.ILogger;
using IMSLogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = NuGet.Common.LogLevel;

namespace TypeSearch.NuGet;

public class NuGetLogger : INuGetLogger
{
	private readonly IMSLogger logger;

	public NuGetLogger(IMSLogger logger)
	{
		this.logger = logger;
	}

	public void LogDebug(string data) =>
		logger.LogDebug(data);

	public void LogVerbose(string data) =>
		logger.LogTrace(data);

	public void LogInformation(string data) =>
		logger.LogInformation(data);

	public void LogMinimal(string data) =>
		logger.LogCritical(data);

	public void LogWarning(string data) =>
		logger.LogWarning(data);

	public void LogError(string data) =>
		logger.LogError(data);

	public void LogInformationSummary(string data) =>
		logger.LogInformation(data);

	public void Log(LogLevel level, string data)
	{
		switch (level)
		{
			case LogLevel.Debug:
				LogDebug(data);
				break;
			case LogLevel.Verbose:
				LogVerbose(data);
				break;
			case LogLevel.Information:
				LogInformation(data);
				break;
			case LogLevel.Minimal:
				LogMinimal(data);
				break;
			case LogLevel.Warning:
				LogWarning(data);
				break;
			case LogLevel.Error:
				LogError(data);
				break;
		}
	}

	public Task LogAsync(LogLevel level, string data)
	{
		Log(level, data);
		return Task.CompletedTask;
	}

	public void Log(ILogMessage message) =>
		Log(message.Level, message.Message);

	public Task LogAsync(ILogMessage message) =>
		LogAsync(message.Level, message.Message);
}
