using System;
using System.Threading;

namespace FluentFlyoutWPF.Classes;

// Keep retries alive even when the audio service or endpoint is unavailable at logon.
internal sealed class CaptureRecovery : IDisposable
{
    private readonly object _gate = new();
    private readonly Func<bool> _start;
    private readonly Action _stop;
    private readonly Timer _timer;
    private bool _enabled;
    private bool _running;
    private bool _disposed;
    private int _restartRequested;

    internal CaptureRecovery(Func<bool> start, Action stop)
    {
        _start = start;
        _stop = stop;
        _timer = new Timer(_ => Poll(), null, Timeout.Infinite, Timeout.Infinite);
    }

    internal void SetEnabled(bool enabled)
    {
        lock (_gate)
        {
            if (_disposed || _enabled == enabled) return;
            _enabled = enabled;
            _timer.Change(enabled ? 0 : Timeout.Infinite, enabled ? 3000 : Timeout.Infinite);
            if (!enabled)
            {
                _running = false;
                _stop();
            }
        }
    }

    // Audio callbacks must not wait for the lifecycle lock: StopRecording may wait for them.
    internal void RequestRestart() => Interlocked.Exchange(ref _restartRequested, 1);

    internal void Poll()
    {
        lock (_gate)
        {
            if (_disposed || !_enabled) return;
            if (Interlocked.Exchange(ref _restartRequested, 0) != 0)
            {
                _running = false;
                _stop();
            }
            if (_running) return;
            _running = _start();
            if (!_running) _stop(); // Also release partially initialized captures.
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _enabled = false;
            _timer.Dispose();
            _running = false;
            _stop();
        }
    }
}
