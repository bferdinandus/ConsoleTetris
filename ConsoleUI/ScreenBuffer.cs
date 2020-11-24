using System;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ConsoleUI
{
    public class ScreenBuffer
    {
        private readonly ConsoleEngine _engine;
        private readonly int _width;
        private readonly int _height;
        private readonly char[] _screenBuffer;

        private long _lastFramerateUpdate = 0;
        private int _lastFramerateCount = 0;
        private int _framerateCount = 0;

        public ScreenBuffer(IOptions<ScreenOptions> config, ConsoleEngine engine)
        {
            _engine = engine;
            _width = config.Value.Width;
            _height = config.Value.Height;
            _screenBuffer = new char[_width * _height]; //main buffer array

            _lastFramerateUpdate = DateTime.Now.Ticks;
        }

        public bool InitBuffer()
        {
            if (!_engine.SetupConsole(_width, _height)) {
                return false;
            }

            ClearScreenBuffer();
            DrawScreen();
            return true;
        }

        //this method takes a string, and a pair of coordinates and writes it to the buffer
        public void Draw(string text, int x, int y)
        {
            //iterate through the array, adding values to buffer
            for (int i = 0; i < text.Length; i++) {
                _screenBuffer[y * _width + x + i] = text[i];
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

            //set cursor position to top left and draw the string
            Console.SetCursorPosition(0, 0);
            Console.Write(_screenBuffer);

            ClearScreenBuffer();
        }

        public void Draw2D(char[] playingField, int fieldWidth, int fieldHeight, int posX, int posY, char? transparentChar = null)
        {
            for (int y = 0; y < fieldHeight; y++) {
                for (int x = 0; x < fieldWidth; x++) {
                    char currentChar = playingField[y * fieldWidth + x];
                    if (transparentChar == null || currentChar != transparentChar) {
                        _screenBuffer[(y + posY) * _width + x + posX] = currentChar;
                    }
                }
            }
        }

        public void ClearScreenBuffer()
        {
            // fill screen buffer with spaces
            for (int i = 0; i < _width * _height - 1; i++) {
                _screenBuffer[i] = ' ';
            }
        }
    }
}