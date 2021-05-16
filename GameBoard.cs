using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using Retrodactyl.Chess.Core;
using Retrodactyl.Extensions.SFML;

namespace Retrodactyl.Chess
{
    public class GameBoard : Grid
    {
        public GameBoard(Sprite board, Sprite[] black, Sprite[] white, Font labelFont, Vector2f position, Vector2u gridSize, Vector2f scale, uint gridLayers = 1, DragManager mouseManager = null, int dragLayer = -1, int dropLayer = -1, Vector2f dropScale = default, Vector2u cellSize = default)
            : base(null, gridSize, scale, gridLayers, mouseManager, dragLayer, dropLayer, dropScale, cellSize)
        {
            font = labelFont;
            boardSquareLabel = new Text("",font,17);
            //boardSquareLabel.Style = Text.Styles.Bold;
            
            boardSquareLabel.OutlineThickness = 1;

            boardSprite = board;
            Position = position;
            chessBoard = new Board(true);
            //chessBoard = new Board("rnb1k1nr/ppppbppp/8/4Q3/8/4P3/PPP2qPP/RNB1KBNR w 11");

            /*** setup "tiles" (the game piece sprites) ******************************************/
            blackRook = new Sprite(black[11]);
            blackBishop = new Sprite(black[00]);
            blackKnight = new Sprite(black[06]);
            blackQueen = new Sprite(black[10]);
            blackKing = new Sprite(black[08]);
            blackPawn = new Sprite(black[12]);
            whitePawn = new Sprite(white[12]);
            whiteRook = new Sprite(white[11]);
            whiteBishop = new Sprite(white[00]);
            whiteKnight = new Sprite(white[05]);
            whiteQueen = new Sprite(white[10]);
            whiteKing = new Sprite(white[08]);
            //shadow = new Sprite(black[13]);

            blackPieces = new Dictionary<int, Sprite>()
            {
                { PieceType.Bishop, blackBishop },
                { PieceType.King, blackKing },
                { PieceType.Knight, blackKnight },
                { PieceType.Pawn, blackPawn },
                { PieceType.Queen, blackQueen },
                { PieceType.Rook, blackRook },
            };

            whitePieces = new Dictionary<int, Sprite>()
            {
                { PieceType.Bishop, whiteBishop },
                { PieceType.King, whiteKing },
                { PieceType.Knight, whiteKnight },
                { PieceType.Pawn, whitePawn },
                { PieceType.Queen, whiteQueen },
                { PieceType.Rook, whiteRook },
            };

            updateBoardFromGameState();
        }
        
        private void updateBoardFromGameState()
        {
            try
            {
                var remaining = new List<GamePiece>(tiles);
                for (int i = 0; i < 64; i++)
                {

                    int x = i % 8;
                    int y = i / 8;//7 - (i / 8);
                    var p = chessBoard[i];
                    if (p != null) { 
                        var pieceType = p.type;
                        var found = remaining.Find(f => f.Piece == p);
                        if (found == null)
                        {
                            if (p.player == Player.Black)
                            {
                                found = new GamePiece(blackPieces[pieceType], p);
                            }
                            else // p.IsWhite
                            {
                                found = new GamePiece(whitePieces[pieceType], p);
                            }
                            tiles.Add(found);
                        }
                        remaining.Remove(found);
                        base.map[x, y, 0] = tiles.IndexOf(found);                        
                    }
                    else
                    {
                        base.map[x, y, 0] = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }
        }

        protected override List<Vector2i> GetMoves(GamePiece gamePiece, Vector2i from)
        {
            if (chessBoard.CurrentPlayer == Player.Black) return null;
            var moves = chessBoard.GetMoves();
            return moves.Where(m => m.piece == gamePiece.Piece && chessBoard[m.to] == null)
                .Select(m => new Vector2i(m.to.x, m.to.y))
                .ToList();
        }
        protected override List<Vector2i> GetMovesOccupied(GamePiece gamePiece, Vector2i from)
        {
            if (chessBoard.CurrentPlayer == Player.Black) return null;
            var moves = chessBoard.GetMoves();
            return moves.Where(m => m.piece == gamePiece.Piece && chessBoard[m.to] != null)
                .Select(m => new Vector2i(m.to.x, m.to.y))
                .Concat(new[] { from })
                .ToList();
        }

        protected override bool IsMoveAllowed(GamePiece gamePiece, Vector2i from, Vector2i to)
        {
            if (chessBoard.CurrentPlayer == Player.Black) return false;
            if (aiThinkingTask == null || aiThinkingTask.IsCompleted)
            {

                var moves = chessBoard.GetMoves();
                if (!moves.Any())
                {
                    GameEndText = "Stalemate";
                    return false;
                }

                var fromSquare = new Square(from.X, from.Y);
                var toSquare = new Square(to.X, to.Y);

                var move = moves.FirstOrDefault(m => m.from == fromSquare && m.to == toSquare);
                if (move != default(Move))
                {
                    chessBoard.Move(move);
                    updateGameText();
                    return true;
                }
            }
            return false;
        }

        protected override void OnMoved()
        {
            updateBoardFromGameState();

            //Debug.WriteLine(chessGame.Pos.GenerateFen().ToString());

            if (chessBoard.CurrentPlayer == Player.Black)
            {
                if (aiThinkingTask == null || aiThinkingTask.IsCompleted)
                {
                    aiThinking = true;
                    aiThinkingTask = Task.Run(async () =>
                    {
                        try
                        {
                            var ai = new GameAI();
                            var best = ai.Search(chessBoard);
                            var move = best;

                            aiThinking = false;

                            var fromIndex = move.from.ToInt();
                            var fromX = move.from.x;//fromIndex % 8;
                            var fromY = move.from.y;// 7 - (fromIndex / 8);
                            var tileIndex = map[fromX, fromY, 0];
                            var sprite = tiles[tileIndex];

                            var toIndex = move.to.ToInt();
                            var toX = move.to.x;//toIndex % 8;
                            var toY = move.to.y;//7 - (toIndex / 8);
                            var tile2Index = map[toX, toY, 0];
                            GamePiece pieceBeingTaken = null;
                            if (tile2Index != -1)
                            {
                                pieceBeingTaken = tiles[tile2Index];
                                pieceBeingTaken.beingTaken = true;
                            }

                            var tilePosition = new Vector2f(toX * tileSize.X * scale.X, toY * tileSize.Y * scale.Y);
                            var tileOffset = (ScaledTileSize - sprite.GetGlobalBounds().GetSize()) / 2.0f;
                            tileOffset.Y -= 24;
                            await sprite.MoveAsync(Position + tilePosition + tileOffset);

                            chessBoard.Move(move);
                            updateBoardFromGameState();
                            sprite.moveFinished = true;
                            if (pieceBeingTaken != null)
                                pieceBeingTaken.beingTaken = false;

                            updateGameText();
                            //Debug.WriteLine(chessGame.Pos.GenerateFen().ToString());
                        }
                        catch (Exception ex)
                        {
                            Debugger.Break();
                        }
                    });
                }
            }
        }

        private void updateGameText()
        {
            if (chessBoard.IsMate) GameEndText = "Checkmate!";
            else if (chessBoard.CurrentPlayerInCheck) GameEndText = "Check";
            else if (!chessBoard.GetMoves().Any()) GameEndText = "Stalemate";
            else if (chessBoard.InRepetition) GameEndText = "Draw - Threefold Repetition";

            else GameEndText = null;
        }
        public string GameEndText { get; set; }

        private Task aiThinkingTask;
        public bool aiThinking;

        public override void Draw(RenderTarget target, RenderStates states)
        {
            boardSprite.Draw(target, states);
            base.Draw(target, states);
        }

        public override Vector2f Position { 
            get => base.Position; 
            set 
            { 
                base.Position = value;
                boardSprite.Position = value;
            }
        }

        private static Sprite boardSprite;
        private static Sprite blackRook;
        private static Sprite blackBishop;
        private static Sprite blackKnight;
        private static Sprite blackQueen;
        private static Sprite blackKing;
        private static Sprite blackPawn;
        private static Sprite whiteRook;
        private static Sprite whiteBishop;
        private static Sprite whiteKnight;
        private static Sprite whiteQueen;
        private static Sprite whiteKing;
        private static Sprite whitePawn;
        

        private static Dictionary<int, Sprite> blackPieces;
        private static Dictionary<int, Sprite> whitePieces;

        //private Board chessBoard;
        //private PieceValue chessPieceValue;
        //private Position chessPosition;
        //private IGame chessGame;
        //private State chessState;        

        private Board chessBoard;
    }
}
