using System;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace ConsoleUI
{
    public class Tetris
    {
        private readonly ScreenBuffer _screenBuffer;
        private int _width;
        private int _height;
        private const int _fieldWidth = 12;
        private const int _fieldHeight = 18;
        private const int _fieldStartX = 10;
        private const int _fieldStartY = 10;

        private char[] _playingField;

        private int _currentPiece = 2;
        private int _currentRotation = 1;
        private int _currentX;
        private int _currentY;

        private int _speed = 20;
        private bool _rotateHold;
        private bool _gameOver;
        private int _pieceCount;
        private int _speedCount;
        private bool _forceDown;
        private bool _moveDownFailed;


        public Tetris(IConfiguration config, ScreenBuffer screenBuffer)
        {
            _screenBuffer = screenBuffer;
            _width = config.GetValue<int>("screenWidth");
            _height = config.GetValue<int>("screenHeight");

            _currentX = _fieldWidth / 2;
            _currentY = 0;

            InitPlayingField();
        }

        public void Run()
        {
            // loop
            while (!_gameOver)
            {
                // GAME TIMING =============================
                Thread.Sleep(50);
                _speedCount++;
                _forceDown = _speedCount == _speed;

                // INPUT ===================================


                // GAME LOGIC ==============================
                UpdatePieceLocation();

                if (_forceDown)
                {
                    _speedCount = 0;
                }

                if (_moveDownFailed)
                {
                    LockPiece();
                    // CheckForLines();
                    SpawnPiece();
                    // Update difficulty every 50 pieces
                    _pieceCount++;
                    if (_pieceCount % 50 == 0)
                        if (_speed >= 10)
                        {
                            _speed--;
                        }

                    _moveDownFailed = false;
                }

                // RENDER OUTPUT ===========================
                _screenBuffer.Draw("Hello World!", 0, 0);

                // draw tetromino field
                _screenBuffer.Draw2D(_playingField, _fieldWidth, _fieldHeight, _fieldStartX, _fieldStartY);

                // draw current piece
                _screenBuffer.Draw2D(GetRotatedPiece(_currentPiece, _currentRotation), 4, 4, _fieldStartX + _currentX, _fieldStartY + _currentY, '.');

                // output to screen
                _screenBuffer.DrawScreen();
            }

            _screenBuffer.ClearScreen();
            _screenBuffer.Draw("GameOver", 10, 10);
            _screenBuffer.DrawScreen();

            Console.ReadLine();
        }

        private void SpawnPiece()
        {
            _currentX = _fieldWidth / 2;
            _currentY = 0;
            _currentRotation = 0;
            Random rand = new Random();
            _currentPiece = rand.Next(0, 6);

            // If piece does not fit straight away, game over!
            _gameOver = !DoesPieceFit(_currentPiece, _currentRotation, _currentX, _currentY);
        }

        private void LockPiece()
        {
            // It can't move down! Lock the piece in place
            for (int px = 0; px < 4; px++)
            {
                for (int py = 0; py < 4; py++)
                {
                    if (Assets.Tetromino[_currentPiece][Rotate(px, py, _currentRotation)] != '.')
                    {
                        _playingField[(_currentY + py) * _fieldWidth + (_currentX + px)] = (char) (65 + _currentPiece);
                    }
                }
            }
        }

        private void UpdatePieceLocation()
        {
            if (NativeKeyboard.IsKeyDown(KeyCode.Left))
            {
                _currentX -= DoesPieceFit(_currentPiece, _currentRotation, _currentX - 1, _currentY) ? 1 : 0;
            }

            if (NativeKeyboard.IsKeyDown(KeyCode.Right))
            {
                _currentX += DoesPieceFit(_currentPiece, _currentRotation, _currentX + 1, _currentY) ? 1 : 0;
            }

            if (NativeKeyboard.IsKeyDown(KeyCode.Down))
            {
                if (DoesPieceFit(_currentPiece, _currentRotation, _currentX, _currentY + 1))
                {
                    _currentY++;
                }
                else
                {
                    _moveDownFailed = true;
                }
            }

            if (NativeKeyboard.IsKeyDown(KeyCode.Up))
            {
                _currentRotation += !_rotateHold && DoesPieceFit(_currentPiece, _currentRotation + 1, _currentX, _currentY) ? 1 : 0;
                _rotateHold = true;
            }
            else
            {
                _rotateHold = false;
            }

            if (_forceDown)
            {
                // Test if piece can be moved down
                if (DoesPieceFit(_currentPiece, _currentRotation, _currentX, _currentY + 1))
                {
                    _currentY++; // It can, so do it!
                }
                else
                {
                    _moveDownFailed = true;
                }
            }
        }

        private static char[] GetRotatedPiece(int piece, int rotation)
        {
            char[] rotatedPiece = new char[4 * 4];

            for (int px = 0; px < 4; px++)
            {
                for (int py = 0; py < 4; py++)
                {
                    int pieceIndex = Rotate(px, py, rotation);
                    rotatedPiece[py * 4 + px] = Assets.Tetromino[piece][pieceIndex] == '.' ? '.' : (char) (65 + piece);
                }
            }

            return rotatedPiece;
        }

        private void InitPlayingField()
        {
            _playingField = new char[_fieldWidth * _fieldHeight];
            for (int x = 0; x < _fieldWidth; x++)
            {
                for (int y = 0; y < _fieldHeight; y++)
                {
                    _playingField[y * _fieldWidth + x] = x == 0 || x == _fieldWidth - 1 || y == _fieldHeight - 1 ? '#' : ' ';
                }
            }
        }

        private static int Rotate(int x, int y, int r) =>
            (r % 4) switch
            {
                0 => y * 4 + x,
                1 => 12 + y - x * 4,
                2 => 15 - y * 4 - x,
                3 => 3 - y + x * 4,
                _ => 0
            };

        private bool DoesPieceFit(int tetromino, int rotation, int posX, int posY)
        {
            for (int px = 0; px < 4; px++)
            {
                for (int py = 0; py < 4; py++)
                {
                    int pieceIndex = Rotate(px, py, rotation);
                    int fieldIndex = (posY + py) * _fieldWidth + posX + px;

                    if (posX + px >= 0 && posX + px < _fieldWidth)
                    {
                        if (posY + py >= 0 && posY + py < _fieldHeight)
                        {
                            if (Assets.Tetromino[tetromino][pieceIndex] == 'X' && _playingField[fieldIndex] != ' ')
                            {
                                return false; // fail on first hit
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}