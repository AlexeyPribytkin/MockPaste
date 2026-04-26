using System.Runtime.InteropServices;
using MockPaste.Infrastructure.Native;

namespace MockPaste.Infrastructure;

/// <summary>
/// Simulates a Ctrl+V keyboard shortcut via <c>SendInput</c> to trigger a paste
/// in the currently focused window.
/// </summary>
public sealed class InputSimulationService
{
    /// <summary>
    /// Sends a Ctrl+V key-down/key-up sequence to the OS input queue.
    /// Returns <c>true</c> when all four inputs were accepted by <c>SendInput</c>.
    /// </summary>
    public bool SimulatePaste()
    {
        NativeMethods.INPUT[] inputs =
        [
            NativeMethods.KeyDown(NativeMethods.VK_CONTROL),
            NativeMethods.KeyDown(NativeMethods.VK_V),
            NativeMethods.KeyUp(NativeMethods.VK_V),
            NativeMethods.KeyUp(NativeMethods.VK_CONTROL),
        ];

        uint sent = NativeMethods.SendInput(
            (uint)inputs.Length,
            inputs,
            Marshal.SizeOf<NativeMethods.INPUT>());

        if (sent != (uint)inputs.Length)
        {
            AppLogger.Warning($"SendInput sent {sent}/{inputs.Length} inputs");
            return false;
        }

        return true;
    }
}
