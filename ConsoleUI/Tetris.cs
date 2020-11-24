using System;
using System.Collections.Generic;
using System.Threading;

namespace ConsoleUI
{
    public class Tetris
    {
        private readonly ScreenBuffer _screenBuffer;

        private const int FieldWidth = 12;
        private const int FieldHeight = 18;
        private const int FieldStartX = 10;
        private const int FieldStartY = 10;

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
        private bool _nextPiece;
        private int _score;

        private readonly List<int> _fullLines = new List<int>();

        public Tetris(ScreenBuffer screenBuffer)
        {
            _screenBuffer = screenBuffer;

            _currentX = FieldWidth / 2;
            _currentY = 0;

            InitPlayingField();
            SpawnPiece();
        }

        public void Run()
        {
            if (!_screenBuffer.InitBuffer()) {
                return;
            }

            // loop
            while (!_gameOver) {
                // GAME TIMING =============================
                Thread.Sleep(50);
                _speedCount++;
                _forceDown = _speedCount == _speed;

                // INPUT ===================================

                // GAME LOGIC ==============================
                UpdatePieceLocation();

                if (_forceDown) {
                    _speedCount = 0;
                }

                if (_nextPiece) {
                    LockPiece();
                    CheckForLines();
                    SpawnPiece();

                    // update score
                    _score += 25;
                    if (_fullLines.Count > 0) {
                        // for every line 2^<num of lines> *100
                        _score += (1 << _fullLines.Count) * 100;
                    }

                    // Update difficulty every 50 pieces
                    _pieceCount++;
                    if (_pieceCount % 50 == 0 && _speed >= 10) {
                        _speed--;
                    }

                    _nextPiece = false;
                }

                // RENDER OUTPUT ===========================
                _screenBuffer.Draw("Hello World!", 0, 0);

                // draw tetromino field
                _screenBuffer.Draw2D(_playingField, FieldWidth, FieldHeight, FieldStartX, FieldStartY);

                // draw score
                _screenBuffer.Draw($"Score: {_score}", FieldStartX + FieldWidth + 6, 16);

                // draw current piece
                _screenBuffer.Draw2D(GetRotatedPiece(_currentPiece, _currentRotation), 4, 4, FieldStartX + _currentX, FieldStartY + _currentY, '.');

                //draw line wait then move stuff down
                if (_fullLines.Count > 0) {
                    // Display Frame (cheekily to draw lines)
                    _screenBuffer.DrawScreen();
                    Thread.Sleep(400); // Delay a bit

                    RemoveFullLines();
                } else {
                    // output to screen
                    _screenBuffer.DrawScreen();
                }
            }

            _screenBuffer.ClearScreenBuffer();
            _screenBuffer.Draw($"Game Over! Score: {_score}", 10, 10);
            _screenBuffer.Draw("Press enter key to exit.", 5, 13);
            _screenBuffer.DrawScreen();

            Console.ReadLine();
        }

        private void RemoveFullLines()
        {
            foreach (int line in _fullLines) {
                for (int px = 1; px < FieldWidth - 1; px++) {
                    for (int py = line; py > 0; py--) {
                        _playingField[py * FieldWidth + px] = _playingField[(py - 1) * FieldWidth + px];
                    }

                    _playingField[px] = ' ';
                }
            }

            _fullLines.Clear();
        }

        private void CheckForLines()
        {
            for (int py = 0; py < 4; py++) {
                if (_currentY + py < FieldHeight - 1) {
                    bool bLine = true;
                    for (int px = 1; px < FieldWidth - 1; px++) {
                        bLine &= _playingField[(_currentY + py) * FieldWidth + px] != ' ';
                    }

                    if (bLine) {
                        // Remove Line, set to =
                        for (int px = 1; px < FieldWidth - 1; px++) {
                            _playingField[(_currentY + py) * FieldWidth + px] = '=';
                        }

                        _fullLines.Add(_currentY + py);
                    }
                }
            }
        }

        private void SpawnPiece()
        {
            _currentX = FieldWidth / 2;
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
            for (int px = 0; px < 4; px++) {
                for (int py = 0; py < 4; py++) {
                    if (Assets.Tetromino[_currentPiece][Rotate(px, py, _currentRotation)] != '.') {
                        _playingField[(_currentY + py) * FieldWidth + (_currentX + px)] = (char) (65 + _currentPiece);
                    }
                }
            }
        }

        private void UpdatePieceLocation()
        {
            if (NativeKeyboard.IsKeyDown(KeyCode.Left)) {
                _currentX -= DoesPieceFit(_currentPiece, _currentRotation, _currentX - 1, _currentY) ? 1 : 0;
            }

            if (NativeKeyboard.IsKeyDown(KeyCode.Right)) {
                _currentX += DoesPieceFit(_currentPiece, _currentRotation, _currentX + 1, _currentY) ? 1 : 0;
            }

            if (NativeKeyboard.IsKeyDown(KeyCode.Down)) {
                if (DoesPieceFit(_currentPiece, _currentRotation, _currentX, _currentY + 1)) {
                    _currentY++;
                } else {
                    // move down failed, ask for next piece
                    _nextPiece = true;
                }
            }

            if (NativeKeyboard.IsKeyDown(KeyCode.Up)) {
                _currentRotation += !_rotateHold && DoesPieceFit(_currentPiece, _currentRotation + 1, _currentX, _currentY) ? 1 : 0;
                _rotateHold = true;
            } else {
                _rotateHold = false;
            }

            if (_forceDown) {
                // Test if piece can be moved down
                if (DoesPieceFit(_currentPiece, _currentRotation, _currentX, _currentY + 1)) {
                    _currentY++; // It can, so do it!
                } else {
                    // move down failed, ask for next piece
                    _nextPiece = true;
                }
            }
        }

        private static char[] GetRotatedPiece(int piece, int rotation)
        {
            char[] rotatedPiece = new char[4 * 4];

            for (int px = 0; px < 4; px++) {
                for (int py = 0; py < 4; py++) {
                    int pieceIndex = Rotate(px, py, rotation);
                    rotatedPiece[py * 4 + px] = Assets.Tetromino[piece][pieceIndex] == '.' ? '.' : (char) (65 + piece);
                }
            }

            return rotatedPiece;
        }

        private void InitPlayingField()
        {
            _playingField = new char[FieldWidth * FieldHeight];
            for (int x = 0; x < FieldWidth; x++) {
                for (int y = 0; y < FieldHeight; y++) {
                    _playingField[y * FieldWidth + x] = x == 0 || x == FieldWidth - 1 || y == FieldHeight - 1 ? '#' : ' ';
                }
            }
        }

        private static int Rotate(int x, int y, int r) =>
            (r % 4) switch {
                0 => y * 4 + x,
                1 => 12 + y - x * 4,
                2 => 15 - y * 4 - x,
                3 => 3 - y + x * 4,
                _ => 0
            };

        private bool DoesPieceFit(int tetromino, int rotation, int posX, int posY)
        {
            for (int px = 0; px < 4; px++) {
                for (int py = 0; py < 4; py++) {
                    int pieceIndex = Rotate(px, py, rotation);
                    int fieldIndex = (posY + py) * FieldWidth + posX + px;

                    if (posX + px >= 0 && posX + px < FieldWidth) {
                        if (posY + py >= 0 && posY + py < FieldHeight) {
                            if (Assets.Tetromino[tetromino][pieceIndex] == 'X' && _playingField[fieldIndex] != ' ') {
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