using System;
using Microsoft.Extensions.Configuration;

namespace ConsoleUI
{
    public class ScreenBuffer
    {
        private readonly int _width;
        private readonly int _height;
        private char[,] _screenBufferArray;

        public ScreenBuffer(IConfiguration config)
        {
            _width = config.GetValue<int>("screenWidth");
            _height = config.GetValue<int>("screenHeight");

            _screenBufferArray = new char[_width, _height]; //main buffer array
            ClearScreen();

            Console.CursorVisible = false;
            Console.SetWindowSize(_width, _height);
            Console.SetWindowPosition(0, 0);
            Console.SetBufferSize(_width, _height);
        }

        //this method takes a string, and a pair of coordinates and writes it to the buffer
        public void Draw(string text, int x, int y)
        {
            //iterate through the array, adding values to buffer
            for (int i = 0; i < text.Length; i++)
            {
                _screenBufferArray[x + i, y] = text[i];
            }
        }

        public void DrawScreen()
        {
            string screenBuffer = "";

            //iterate through buffer, adding each value to screenBuffer
            for (int y = 0; y < _height - 1; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    screenBuffer += _screenBufferArray[x, y];
                }
            }

            //set cursor position to top left and draw the string
            Console.SetCursorPosition(0, 0);
            Console.Write(screenBuffer);
            Console.SetWindowPosition(0, 0);

            _screenBufferArray = new char[_width, _height];
            //note that the screen is NOT cleared at any point as this will simply overwrite the existing values on screen. Clearing will cause flickering again.
        }

        public void Draw2D(char[] playingField, int fieldWidth, int fieldHeight, int posX, int posY, char? transparentChar = null)
        {
            for (int x = 0; x < fieldWidth; x++)
            {
                for (int y = 0; y < fieldHeight; y++)
                {
                    char currentChar = playingField[y * fieldWidth + x];
                    if (transparentChar == null || currentChar != transparentChar)
                    {
                        _screenBufferArray[x + posX, y + posY] = currentChar;
                    }
                }
            }
        }

        public void ClearScreen()
        {
            // fill screen buffer with spaces
            for (int y = 0; y < _height - 1; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _screenBufferArray[x, y] = ' ';
                }
            }
        }
    }
}