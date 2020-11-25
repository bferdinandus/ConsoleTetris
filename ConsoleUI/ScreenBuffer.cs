using System;
using System.Text;
using Microsoft.Extensions.Options;

namespace ConsoleUI
{
    public class ScreenBuffer
    {
        public const string Black = "\u001b[30m";
        public const string Red = "\u001b[31m";
        public const string Green = "\u001b[32m";
        public const string Yellow = "\u001b[33m";
        public const string Blue = "\u001b[34m";
        public const string Magenta = "\u001b[35m";
        public const string Cyan = "\u001b[36m";
        public const string White = "\u001b[37m";
        public const string ResetColor = "\u001b[0m";

        private readonly ConsoleEngine _engine;
        private readonly int _width;
        private readonly int _height;
        private readonly CharacterInfo[] _screenBuffer;

        private long _lastFramerateUpdate = 0;
        private int _lastFramerateCount = 0;
        private int _framerateCount = 0;

        public ScreenBuffer(IOptions<ScreenOptions> config, ConsoleEngine engine)
        {
            _engine = engine;
            _width = config.Value.Width;
            _height = config.Value.Height;
            _screenBuffer = new CharacterInfo[_width * _height]; //main buffer array
            for (int i = 0; i < _width * _height; i++) {
                _screenBuffer[i] = new CharacterInfo();
            }

            _lastFramerateUpdate = DateTime.Now.Ticks;
        }

        public bool InitBuffer()
        {
            if (!ConsoleEngine.SetupConsole(_width, _height)) {
                return false;
            }

            ClearScreenBuffer();
            DrawScreen();
            return true;
        }

        public void Draw(char glyph, int x, int y, string color = "")
        {
            _screenBuffer[y * _width + x].Glyph = glyph;
            _screenBuffer[y * _width + x].FgColor = color;
        }

        public void DrawText(string text, int x, int y, string color = "")
        {
            //iterate through the array, adding values to buffer
            for (int i = 0; i < text.Length; i++) {
                Draw(text[i], x + i, y, color);
            }
        }

        public void DrawScreen()
        {
            long millisecondNow = DateTime.Now.Ticks;
            if (millisecondNow - _lastFramerateUpdate >= 10000000) {
                _lastFramerateCount = _framerateCount;
                _framerateCount = 0;
                _lastFramerateUpdate = millisecondNow;
            }

            Console.Title = $"-=[ Tetris ]=- Framerate: {_lastFramerateCount} Date: {DateTime.Now}";
            _framerateCount++;

            var screenBuffer = new StringBuilder();
            string currentColor = "";
            for (int i = 0; i < _screenBuffer.Length; i++) {
                CharacterInfo characterInfo = _screenBuffer[i];
                if (!string.IsNullOrEmpty(characterInfo.FgColor)) {
                    // if we have a color for this glyph
                    if (characterInfo.FgColor != currentColor) {
                        // check to see if it is a different color then we are using currently
                        currentColor = characterInfo.FgColor;
                        screenBuffer.Append(characterInfo.FgColor);
                    }
                } else if (!string.IsNullOrEmpty(currentColor)) {
                    // we where using a color, but not anymore, reset color.
                    screenBuffer.Append(ResetColor);
                    currentColor = string.Empty;
                }

                screenBuffer.Append(characterInfo.Glyph);
            }

            // if the last glyph was colored then reset the color, otherwise the next frame will start with this color.
            if (!string.IsNullOrEmpty(currentColor)) {
                screenBuffer.Append(ResetColor);
            }

            //set cursor position to top left and draw the string
            Console.SetCursorPosition(0, 0);
            Console.Write(screenBuffer);

            ClearScreenBuffer();
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

        public void ClearScreenBuffer()
        {
            // fill screen buffer with spaces
            for (int i = 0; i < _width * _height; i++) {
                _screenBuffer[i].Glyph = ' ';
            }
        }
    }
}