using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ConsoleUI.Models;

// ReSharper disable InconsistentNaming

namespace ConsoleUI
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
        private int _width;
        private int _height;
        protected long _lastFramerateUpdate;
        protected int _lastFramerateCount;
        protected int _framerateCount;

        private bool _loopActive;

        protected const double _ticksPerSecond = 1e7;

        protected ConsoleEngine() {}

        protected abstract bool OnUserCreate();
        protected abstract bool OnUserUpdate(float elapsedTime);

        public bool SetupConsole(int width, int height)
        {
            _width = width;
            _height = height;

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

            _screenBuffer = new CharacterInfo[_width * _height]; //main buffer array
            for (int i = 0; i < _width * _height; i++) {
                _screenBuffer[i] = new CharacterInfo();
            }

            ClearScreenBuffer();
            DrawScreen();

            return true;
        }

        public async void Start()
        {
            _loopActive = true;

            await LoopAsync();
        }

        private Task LoopAsync()
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
                Console.Title = $"-=[ Tetris ]=- Framerate: {(int) (1.0 / (elapsedTime / _ticksPerSecond))} Date: {DateTime.Now}";
                DrawScreen();
            }

            return Task.CompletedTask;
        }

        private void ClearScreenBuffer()
        {
            // fill screen buffer with spaces
            for (int i = 0; i < _width * _height; i++) {
                _screenBuffer[i].Glyph = ' ';
            }
        }

        private void DrawScreen()
        {
            StringBuilder screenBuffer = new StringBuilder();
            AnsiColor? currentColor = null;
            for (int i = 0; i < _screenBuffer.Length; i++) {
                CharacterInfo characterInfo = _screenBuffer[i];
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

        public void Draw(char glyph, int x, int y, AnsiColor? color)
        {
            _screenBuffer[y * _width + x].Glyph = glyph;
            _screenBuffer[y * _width + x].FgColor = color;
        }

        public void DrawText(string text, int x, int y, AnsiColor? color = null)
        {
            //iterate through the array, adding values to buffer
            for (int i = 0; i < text.Length; i++) {
                Draw(text[i], x + i, y, color);
            }
        }

        public void Draw2D(CharacterInfo[] playingField, int fieldWidth, int fieldHeight, int posX, int posY, char? transparentChar = null)
        {
            for (int y = 0; y < fieldHeight; y++) {
                for (int x = 0; x < fieldWidth; x++) {
                    CharacterInfo currentChar = playingField[y * fieldWidth + x];
                    if (transparentChar == null || currentChar.Glyph != transparentChar) {
                        _screenBuffer[(y + posY) * _width + x + posX].Glyph = currentChar.Glyph;
                        _screenBuffer[(y + posY) * _width + x + posX].FgColor = currentChar.FgColor;
                    }
                }
            }
        }
    }
}