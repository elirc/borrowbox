namespace BorrowBox.Core.Common;

public sealed class BorrowBoxException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
