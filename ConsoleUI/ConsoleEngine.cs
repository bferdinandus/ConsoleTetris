using System;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace ConsoleUI
{
    public class ConsoleEngine
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        public static bool SetupConsole(int width, int height)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                IntPtr iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
                if (!GetConsoleMode(iStdOut, out uint outConsoleMode)) {
                    Console.WriteLine("failed to get output console mode");
                    Console.ReadKey();
                    return false;
                }

                outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
                if (!SetConsoleMode(iStdOut, outConsoleMode)) {
                    Console.WriteLine($"failed to set output console mode, error code: {GetLastError()}");
                    Console.ReadKey();
                    return false;
                }
            }

            Console.CursorVisible = false;
            Console.SetBufferSize(width, height);
            Console.SetWindowSize(width, height);
            Console.SetWindowPosition(0, 0);

            return true;
        }
    }
}