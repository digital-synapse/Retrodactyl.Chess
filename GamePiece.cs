using Retrodactyl.Chess.Core;
using Retrodactyl.Extensions.SFML;
using SFML.Graphics;
using SFML.System;
using System.Threading.Tasks;

namespace Retrodactyl.Chess
{
    public class GamePiece : Sprite
    {
        public GamePiece(Sprite sprite, IPiece piece)
            : base (sprite)
        {
            this.Piece = piece;
        }

        public GamePiece(GamePiece chessPiece)
            : base(chessPiece)
        {
            this.Piece = chessPiece.Piece;
        }

        public IPiece Piece {get; set;}

        public Vector2i BoardPosition { get; set; }
        public Vector2f MoveTarget 
        { 
            get => moveTarget; 
            set {
                isMoving = true;
                moveFinished = false;
                moveTarget = value;
                moveOrigin = Position;
            }
        }
        private Vector2f moveTarget;
        private Vector2f moveOrigin;
        public bool isMoving { get; set; }
        public bool moveFinished { get; set; } = true;
        public async Task MoveAsync(Vector2f moveTarget)
        {
            MoveTarget = moveTarget;
            while (isMoving)
            {
                await Task.Delay(20);
            }
        }
        public bool beingTaken { get; set; }
        private float a = 255;

        public void Update()
        {
            if (isMoving)
            {
                // move to the target destination using linear interpoaltion
                Position = new Vector2f(
                    lerp(Position.X, MoveTarget.X, 0.1f),
                    lerp(Position.Y, MoveTarget.Y, 0.1f)
                );

                // has it reached the destination?
                if ((Position - MoveTarget).Abs().Mul() < 0.1f)
                {
                    isMoving = false;
                }
            }

            if (beingTaken)
            {
                a = lerp(a, 0, 0.05f);
                this.Color = new Color(255, 255, 255, (byte)a);
                if (a < 0.1f)
                {
                    beingTaken = false;
                }
            }

        }
        

        float lerp(float x, float y, float z)
        {
            return x + (y - x) * z;

            //return ((1.0f - z) * x) + (z * y);
        }
    }
}
