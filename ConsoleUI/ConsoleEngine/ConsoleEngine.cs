using System;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleEngine.Models;

// ReSharper disable InconsistentNaming

namespace ConsoleEngine
{
    public abstract class ConsoleEngine
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

        private CharacterInfo[] _screenBuffer;
        private int _screenWidth;
        private int _screenHeight;

        private bool _loopActive;

        protected const long TicksPerMillisecond = 10000;
        protected const long TicksPerSecond = TicksPerMillisecond * 1000;
        protected const long TicksPerMinute = TicksPerSecond * 60;
        protected const long TicksPerHour = TicksPerMinute * 60;
        protected const long TicksPerDay = TicksPerHour * 24;

        // protected ConsoleEngine() {}

        protected abstract bool OnUserCreate();
        protected abstract bool OnUserUpdate(long elapsedTime);

        public bool SetupConsole(int width, int height)
        {
            _screenWidth = width;
            _screenHeight = height;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                IntPtr iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
                if (!GetConsoleMode(iStdOut, out uint outConsoleMode)) {
                    Console.WriteLine("failed to get output console mode");
                    Console.ReadKey();
                    return false;
                }

                outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
                if (!SetConsoleMode(iStdOut, outConsoleMode)) {
                    Console.WriteLine($"failed to set output console mode, error code: {GetLastError().ToString()}");
                    Console.ReadKey();
                    return false;
                }
            }

            Console.CursorVisible = false;
            Console.SetBufferSize(width, height);
            Console.SetWindowSize(width, height);
            Console.SetWindowPosition(0, 0);

            _screenBuffer = new CharacterInfo[_screenWidth * _screenHeight]; //main buffer array
            for (int i = 0; i < _screenWidth * _screenHeight; i++) {
                _screenBuffer[i] = new CharacterInfo();
            }

            ClearScreenBuffer();
            DrawScreen();

            return true;
        }

        public void Start()
        {
            _loopActive = true;

            Loop();
        }

        private void Loop()
        {
            if (!OnUserCreate()) {
                _loopActive = false;
            }

            long ticks1 = DateTime.Now.Ticks;

            while (_loopActive) {
                // Handle Timing
                long ticks2 = DateTime.Now.Ticks;
                long elapsedTime = ticks2 - ticks1;
                ticks1 = ticks2;

                if (!OnUserUpdate(elapsedTime)) {
                    _loopActive = false;
                }

                // update title
                float elapsedSeconds = (float) elapsedTime / TicksPerSecond;
                Console.Title = $"-=[ Tetris ]=- Framerate: {(int) (1 / elapsedSeconds)} Date: {DateTime.Now}";
                DrawScreen();
            }
        }

        private void ClearScreenBuffer()
        {
            // fill screen buffer with spaces
            for (int i = 0; i < _screenWidth * _screenHeight; i++) {
                _screenBuffer[i].Glyph = ' ';
            }
        }

        private void DrawScreen()
        {
            StringBuilder screenBuffer = new StringBuilder(_screenWidth * _screenHeight);
            AnsiColor? currentColor = null;
            foreach (CharacterInfo characterInfo in _screenBuffer) {
                if (characterInfo.FgColor != null) {
                    // if we have a color for this glyph
                    if (characterInfo.FgColor != currentColor) {
                        // check to see if it is a different color then we are using currently
                        currentColor = characterInfo.FgColor;
                        screenBuffer.Append(ColorString(characterInfo.FgColor));
                    }
                } else if (currentColor != null) {
                    // we where using a color, but not anymore, reset color.
                    screenBuffer.Append(ColorString(AnsiColor.ResetColor));
                    currentColor = null;
                }

                screenBuffer.Append(characterInfo.Glyph);
            }

            // if the last glyph was colored then reset the color, otherwise the next frame will start with this color.
            if (currentColor != null) {
                screenBuffer.Append(ColorString(AnsiColor.ResetColor));
            }

            //set cursor position to top left and draw the string
            Console.SetCursorPosition(0, 0);
            Console.Write(screenBuffer);

            ClearScreenBuffer();
        }

        private string ColorString(AnsiColor? color) => color == null ? string.Empty : $"\u001b[{(int) color}m";

        private void Draw(char glyph, int x, int y, AnsiColor? color)
        {
            _screenBuffer[y * _screenWidth + x].Glyph = glyph;
            _screenBuffer[y * _screenWidth + x].FgColor = color;
        }

        protected void DrawText(string text, int x, int y, AnsiColor? color = null)
        {
            //iterate through the array, adding values to buffer
            for (int i = 0; i < text.Length; i++) {
                Draw(text[i], x + i, y, color);
            }
        }

        protected void Draw2D(CharacterInfo[] playingField, int fieldWidth, int fieldHeight, int posX, int posY, char? transparentChar = null)
        {
            for (int y = 0; y < fieldHeight; y++) {
                for (int x = 0; x < fieldWidth; x++) {
                    CharacterInfo currentChar = playingField[y * fieldWidth + x];
                    if (transparentChar == null || currentChar.Glyph != transparentChar) {
                        _screenBuffer[(y + posY) * _screenWidth + x + posX].Glyph = currentChar.Glyph;
                        _screenBuffer[(y + posY) * _screenWidth + x + posX].FgColor = currentChar.FgColor;
                    }
                }
            }
        }
    }
}