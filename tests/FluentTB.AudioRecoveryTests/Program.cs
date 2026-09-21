using FluentFlyoutWPF.Classes;

int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}

int attempts = 0, cleanups = 0;
bool ready = false;
using (var recovery = new CaptureRecovery(() => { attempts++; return ready; }, () => cleanups++))
{
    recovery.Poll();
    Check(attempts == 0, "Disabled capture must not start.");
    recovery.SetEnabled(true);
    // Drive retries without waiting for Windows audio or the periodic timer.
    for (int i = 0; i < 8; i++) recovery.Poll();
    Check(attempts >= 8, "Startup retries must outlive the old five-attempt limit.");
    Check(cleanups >= 8, "Failed starts must release partially initialized captures.");
    ready = true;
    recovery.Poll();
    int started = attempts;
    for (int i = 0; i < 8; i++) recovery.Poll();
    Check(attempts == started, "A healthy silent capture must not restart.");
    recovery.RequestRestart();
    recovery.Poll();
    Check(attempts == started + 1, "Recording/device failure must reopen capture.");
    recovery.SetEnabled(false);
    started = attempts;
    recovery.RequestRestart();
    recovery.Poll();
    Check(attempts == started, "A queued restart must not re-enable disabled capture.");
    recovery.SetEnabled(true);
    recovery.Poll();
    Check(attempts > started, "Capture must resume when enabled again.");
    recovery.Dispose();
    started = attempts;
    recovery.RequestRestart();
    recovery.SetEnabled(true);
    recovery.Poll();
    Check(attempts == started, "Dispose must prevent all subsequent starts.");
}

// Exercise the real timer: initial unavailability must recover without UI interaction.
int available = 0;
using var captured = new ManualResetEventSlim();
using var unavailable = new ManualResetEventSlim();
using (var recovery = new CaptureRecovery(() =>
{
    if (Volatile.Read(ref available) == 0)
    {
        unavailable.Set();
        return false;
    }
    captured.Set();
    return true;
}, () => { }))
{
    recovery.SetEnabled(true);
    Check(unavailable.Wait(TimeSpan.FromSeconds(8)), "Initial unavailable endpoint was not tested.");
    Volatile.Write(ref available, 1);
    Check(captured.Wait(TimeSpan.FromSeconds(8)), "Periodic retry did not recover.");
}

// Disabling during an in-flight start must wait for it and release its capture.
using var entered = new ManualResetEventSlim();
using var release = new ManualResetEventSlim();
int active = 0;
using (var recovery = new CaptureRecovery(() =>
{
    entered.Set();
    if (!release.Wait(TimeSpan.FromSeconds(8))) return false;
    Interlocked.Exchange(ref active, 1);
    return true;
}, () => Interlocked.Exchange(ref active, 0)))
{
    recovery.SetEnabled(true);
    Check(entered.Wait(TimeSpan.FromSeconds(8)), "Capture did not enter startup.");
    var disable = Task.Run(() => recovery.SetEnabled(false));
    release.Set();
    Check(disable.Wait(TimeSpan.FromSeconds(8)), "Disabling capture deadlocked.");
    recovery.Poll();
    Check(Volatile.Read(ref active) == 0, "An in-flight start survived disable.");
}
Console.WriteLine($"{checks} audio recovery checks passed.");

int[] alignments = { 0, 1, 0, 1 };
int[] expectedPositions = { 2, 0, 2, 0 };
for (int i = 0; i < alignments.Length; i++)
    Check(TaskbarWidgetPlacement.Resolve(true, alignments[i] == 1, 1, false) == expectedPositions[i], "Live alignment change must override a saved middle position.");
foreach (int manual in new[] { 0, 1, 2 })
{
    Check(TaskbarWidgetPlacement.Resolve(false, true, manual, false) == manual, "Disabling automatic placement must restore manual choice.");
    Check(TaskbarWidgetPlacement.Resolve(true, false, manual, true) == manual, "Vertical taskbars must retain their manual position.");
}
Console.WriteLine("10 widget alignment policy checks passed (no live shell test).");
