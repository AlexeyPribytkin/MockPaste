using System.Runtime.InteropServices;
using MockPaste.Infrastructure.Native;

namespace MockPaste.Infrastructure;

public sealed class InputSimulationService
{
    public bool SimulatePaste()
    {
        try
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
        catch (Exception ex)
        {
            AppLogger.Error("Failed to simulate paste", ex);
            return false;
        }
    }
}
