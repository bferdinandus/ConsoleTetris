using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleUI.Models;

namespace ConsoleUI
{
    public class Tetris : ConsoleEngine
    {
        //private readonly ConsoleEngine _consoleEngine;

        private const int FieldWidth = 12;
        private const int FieldHeight = 18;
        private const int FieldStartX = 10;
        private const int FieldStartY = 10;

        private CharacterInfo[] _playingField;

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

        public Tetris() {}

        protected override bool OnUserCreate()
        {
            _currentX = FieldWidth / 2;
            _currentY = 0;

            InitPlayingField();
            SpawnPiece();

            return true;
        }

        /*protected override bool OnUserUpdateTest(float elapsedTime)
        {
            DrawText($"{DateTime.Now}", 10, 10, AnsiColor.Red);
            DrawText($"elapsedTime: {elapsedTime}", 10, 11, AnsiColor.Green);
            DrawText($"Framerate: {1.0 / (elapsedTime / _ticksPerSecond)}", 10, 13, AnsiColor.Yellow);


            long millisecondNow = DateTime.Now.Ticks;
            if (millisecondNow - _lastFramerateUpdate >= _ticksPerSecond) {
                _lastFramerateCount = _framerateCount;
                _framerateCount = 0;
                _lastFramerateUpdate = millisecondNow;
            }

            _framerateCount++;

            DrawText($"Framerate count: {_lastFramerateCount}", 10, 15, AnsiColor.Yellow);

            if (NativeKeyboard.IsKeyDown(KeyCode.Z)) {
                return false;
            }

            return true;
        }*/

        protected override bool OnUserUpdate(float elapsedTime)
        {
            if (!_gameOver) {
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
                DrawText("Hello World!", 0, 0);

                // draw tetromino field
                Draw2D(_playingField, FieldWidth, FieldHeight, FieldStartX, FieldStartY);

                // draw score
                DrawText($"Score: {_score}", FieldStartX + FieldWidth + 6, 16, AnsiColor.Green);

                // draw current piece
                Draw2D(GetRotatedPiece(_currentPiece, _currentRotation), 4, 4, FieldStartX + _currentX, FieldStartY + _currentY, '.');

                //draw line wait then move stuff down
                if (_fullLines.Count > 0) {
                    // Display Frame (cheekily to draw lines)
                    Thread.Sleep(400); // Delay a bit

                    RemoveFullLines();
                } else {
                    // output to screen
                }
            }

            if (_gameOver) {
                DrawText($"Game Over! Score: {_score}", 10, 10, AnsiColor.Red);
                DrawText("Press Z key to exit.", 5, 13, AnsiColor.Magenta);

                if (NativeKeyboard.IsKeyDown(KeyCode.Z)) {
                    return false;
                }
            }

            return true;
        }

        private static AnsiColor PieceColor(int piece)
        {
            return piece switch {
                0 => AnsiColor.Blue,
                1 => AnsiColor.Cyan,
                2 => AnsiColor.Green,
                3 => AnsiColor.Magenta,
                4 => AnsiColor.Red,
                5 => AnsiColor.Yellow,
                _ => AnsiColor.White
            };
        }

        private void RemoveFullLines()
        {
            foreach (int line in _fullLines) {
                for (int px = 1; px < FieldWidth - 1; px++) {
                    for (int py = line; py > 0; py--) {
                        _playingField[py * FieldWidth + px] = _playingField[(py - 1) * FieldWidth + px];
                    }

                    _playingField[px].Glyph = ' ';
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
                        bLine &= _playingField[(_currentY + py) * FieldWidth + px].Glyph != ' ';
                    }

                    if (bLine) {
                        // Remove Line, set to =
                        for (int px = 1; px < FieldWidth - 1; px++) {
                            _playingField[(_currentY + py) * FieldWidth + px].Glyph = '=';
                            _playingField[(_currentY + py) * FieldWidth + px].FgColor = AnsiColor.White;
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
                        _playingField[(_currentY + py) * FieldWidth + (_currentX + px)].Glyph = '\u2588';
                        _playingField[(_currentY + py) * FieldWidth + (_currentX + px)].FgColor = PieceColor(_currentPiece);
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

        private static CharacterInfo[] GetRotatedPiece(int piece, int rotation)
        {
            CharacterInfo[] rotatedPiece = new CharacterInfo[4 * 4];

            for (int px = 0; px < 4; px++) {
                for (int py = 0; py < 4; py++) {
                    int pieceIndex = Rotate(px, py, rotation);
                    rotatedPiece[py * 4 + px] = new CharacterInfo { Glyph = Assets.Tetromino[piece][pieceIndex] == '.' ? '.' : '\u2588', FgColor = PieceColor(piece) };
                }
            }

            return rotatedPiece;
        }

        private void InitPlayingField()
        {
            _playingField = new CharacterInfo[FieldWidth * FieldHeight];
            for (int x = 0; x < FieldWidth; x++) {
                for (int y = 0; y < FieldHeight; y++) {
                    _playingField[y * FieldWidth + x] = new CharacterInfo { Glyph = x == 0 || x == FieldWidth - 1 || y == FieldHeight - 1 ? '#' : ' ' };
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
                            if (Assets.Tetromino[tetromino][pieceIndex] == 'X' && _playingField[fieldIndex].Glyph != ' ') {
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