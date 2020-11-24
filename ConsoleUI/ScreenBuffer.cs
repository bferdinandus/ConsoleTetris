using System;
using Microsoft.Extensions.Configuration;

namespace ConsoleUI
{
    public class ScreenBuffer
    {
        private readonly int _width;
        private readonly int _height;
        private readonly char[] _screenBuffer;

        public ScreenBuffer(IConfiguration config)
        {
            _width = config.GetValue<int>("screenWidth");
            _height = config.GetValue<int>("screenHeight");

            _screenBuffer = new char[_width * _height]; //main buffer array

            Console.CursorVisible = false;
            Console.SetBufferSize(_width, _height);
            Console.SetWindowSize(_width, _height);
            Console.SetWindowPosition(0, 0);

            ClearScreenBuffer();
            DrawScreen();
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
            //set cursor position to top left and draw the string
            Console.SetCursorPosition(0, 0);
            Console.Write(_screenBuffer);
            Console.SetWindowPosition(0, 0);

            ClearScreenBuffer();
        }

        public void Draw2D(char[] playingField, int fieldWidth, int fieldHeight, int posX, int posY, char? transparentChar = null)
        {
            for (int x = 0; x < fieldWidth; x++) {
                for (int y = 0; y < fieldHeight; y++) {
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
            for (int y = 0; y < _height - 1; y++) {
                for (int x = 0; x < _width; x++) {
                    _screenBuffer[y * _width + x] = ' ';
                }
            }
        }
    }
}