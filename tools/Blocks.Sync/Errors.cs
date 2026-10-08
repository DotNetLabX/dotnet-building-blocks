namespace Blocks.Sync;

internal sealed class RefusedException(string message) : Exception(message);

internal sealed class EnvironmentException(string message) : Exception(message);
