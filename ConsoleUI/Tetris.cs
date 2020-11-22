using System;
using Microsoft.Extensions.Configuration;

namespace ConsoleUI
{
    public class Tetris
    {
        private readonly ScreenBuffer _screenBuffer;
        private int _width;
        private int _height;

        public Tetris(IConfiguration config, ScreenBuffer screenBuffer)
        {
            _screenBuffer = screenBuffer;
            _width = config.GetValue<int>("screenWidth");
            _height = config.GetValue<int>("screenHeight");
        }

        public void Run()
        {
            _screenBuffer.Draw("Hello World!", 0, 0);
            _screenBuffer.DrawScreen();

            Console.ReadLine();
        }
    }
}