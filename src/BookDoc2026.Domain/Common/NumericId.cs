namespace BookDoc2026.Domain.Common;

public static class NumericId
{
    private const long EpochMilliseconds = 1_767_225_600_000L; // 2026-01-01 UTC
    private const int NodeBits = 10;
    private const int SequenceBits = 12;
    private const int MaximumNodeId = (1 << NodeBits) - 1;
    private const int MaximumSequence = (1 << SequenceBits) - 1;
    private static readonly object Gate = new();
    private static long _lastMilliseconds = -1;
    private static int _sequence;
    private static int _nodeId;

    public static void ConfigureNode(int nodeId)
    {
        if (nodeId is < 0 or > MaximumNodeId)
            throw new ArgumentOutOfRangeException(nameof(nodeId), $"Numeric ID node must be between 0 and {MaximumNodeId}.");
        lock (Gate) _nodeId = nodeId;
    }

    public static long Next()
    {
        lock (Gate)
        {
            var milliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - EpochMilliseconds;
            if (milliseconds < 0) throw new InvalidOperationException("System clock is before the numeric ID epoch.");
            if (milliseconds < _lastMilliseconds) milliseconds = _lastMilliseconds;
            if (milliseconds == _lastMilliseconds)
            {
                _sequence = (_sequence + 1) & MaximumSequence;
                if (_sequence == 0)
                {
                    do { milliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - EpochMilliseconds; }
                    while (milliseconds <= _lastMilliseconds);
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastMilliseconds = milliseconds;
            return (milliseconds << (NodeBits + SequenceBits)) | ((long)_nodeId << SequenceBits) | (uint)_sequence;
        }
    }
}
