using System.ComponentModel.Design;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Retrodactyl.Extensions.SFML;
using Retrodactyl.Chess.Core;

namespace Retrodactyl.Chess
{
    public abstract class Grid : Drawable
    {
        protected abstract bool IsMoveAllowed(GamePiece gamePiece, Vector2i from, Vector2i to);
        protected abstract void OnMoved();

        public Grid(IEnumerable<GamePiece> tiles, Vector2u gridSize, Vector2f scale, uint gridLayers = 1, DragManager mouseManager = null, int dragLayer=-1, int dropLayer=-1, Vector2f dropScale = default, Vector2u cellSize = default)
        {
            if (dropScale == default)
            {
                dropScale = scale;
            }

            window = mouseManager.Window;
            if (tiles == null)
            {
                this.tiles = new List<GamePiece>();
            }
            else
            {
                this.tiles = tiles.ToList();
            }
            this.mapSize = gridSize;
            this.scale = scale;
            mapLayers = gridLayers;
            map = new int[gridSize.X, gridSize.Y, gridLayers];
            if (cellSize == default ){
                tileSize = this.tiles[0].TextureRect.GetSize().ToVector2f();
            }
            else {
                tileSize = (Vector2f)cellSize;
            }

            // apply scale to tiles
            for (var i = 0; i < this.tiles.Count; i++)
            {
                this.tiles[i].Scale = scale;
            }

            size = new Vector2f(gridSize.X * tileSize.X, gridSize.Y * tileSize.Y);
            scaledSize = new Vector2f(Size.X * scale.X, Size.Y * scale.Y);            
            scaledTileSize = new Vector2f(tileSize.X * scale.X, tileSize.Y * scale.Y);

            // init map
            for (var z = 0; z < mapLayers; z++)
            {
                for (var y = 0; y < mapSize.Y; y++)
                {
                    for (var x = 0; x < mapSize.X; x++)
                    {
                        map[x, y, z] = -1;
                    }
                }
            }

            // mouse
            if (mouseManager != null)
            {
                if (dragLayer != -1)
                {
                    GamePiece picked = null;
                    int pickedIndex = -1;
                    Action<Sprite> dropHandled = null;
                    Vector2f dragOffset = default;
                    Vector2i pickCoords = default;

                    mouseManager.DragStart += (object sender, DragEventArgs e) =>
                    {
                        if (Enabled)
                        {
                            if (e.Button == Mouse.Button.Left
                                && e.DragStart.X >= Rect.Left
                                && e.DragStart.Y >= Rect.Top
                                && e.DragStart.X < Rect.Left + Rect.Width
                                && e.DragStart.Y < Rect.Top + Rect.Height)
                            {
                                picked = SpritePicker(e.DragStart, dragLayer, out pickCoords);
                                
                                if (picked != null)
                                {
                                    moves = GetMoves(picked, pickCoords);
                                    movesOccupied = GetMovesOccupied(picked, pickCoords);
                                    Debug.WriteLine($"[Grid] Picked {pickCoords.X},{pickCoords.Y}");

                                    pickedIndex = map[pickCoords.X, pickCoords.Y, dragLayer];
                                    dropHandled = sprite =>
                                    {
                                    //map[pickCoords.X, pickCoords.Y, dragLayer] = -1;
                                };
                                    picked.Color = new Color(255, 255, 255, 0);
                                    draggingSprite = new GamePiece(picked);
                                    draggingSprite.Color = new Color(255, 255, 255, 200);
                                    dragOffset = (e.DragStart.ToVector2f() - draggingSprite.Position) * -1;

                                    // empty origial area
                                    var subgridSize = getSubGridSizeOfTile(picked);
                                    for (var y = 0; y < subgridSize.Y; y++)
                                    {
                                        for (var x = 0; x < subgridSize.X; x++)
                                        {
                                            map[pickCoords.X + x, pickCoords.Y + y, dragLayer] = -1;
                                        }
                                    }
                                }
                            }
                        }
                    };
                    mouseManager.Dragging += (object sender, DragEventArgs e) =>
                    {
                        if (Enabled && draggingSprite != null)
                        {
                            var dp = e.DragCurrent.ToVector2f();
                            draggingSprite.Position = dp + dragOffset;
                        }
                    };
                    mouseManager.DragEnd += (object sender, DragEventArgs e) =>
                    {
                        if (Enabled)
                        {
                            if (picked != null)
                            {
                                picked.Color = new Color(255, 255, 255, 255);
                                picked = null;
                                moves = null;
                                movesOccupied = null;
                            }
                            if (draggingSprite != null)
                            {
                                if (!mouseManager.Drop(this, draggingSprite, dropHandled, e))
                                {
                                    // if the drop was unhandled, restore the item to its original location
                                    var subgridSize = getSubGridSizeOfTile(draggingSprite);
                                    if (subgridSize.X > 1 || subgridSize.Y > 1)
                                    {
                                        for (var y = 0; y < subgridSize.Y; y++)
                                        {
                                            for (var x = 0; x < subgridSize.X; x++)
                                            {
                                                map[pickCoords.X + x, pickCoords.Y + y, dropLayer] = pickedIndex - int.MinValue;
                                            }
                                        }
                                    }
                                    map[pickCoords.X, pickCoords.Y, dropLayer] = pickedIndex;
                                }
                                draggingSprite = null;
                            }
                        }
                    };
                }
                if (dropLayer != -1)
                {
                    mouseManager.Dropped += (object sender, DropEventArgs e) =>
                    {
                        if (Enabled)
                        {
                            if (e.DragStop.X >= Rect.Left && e.DragStop.Y >= Rect.Top
                                && e.DragStop.X < Rect.Left + Rect.Width
                                && e.DragStop.Y < Rect.Top + Rect.Height)
                            {
                                var p = e.Sprite.GetGlobalBounds().GetCenter().ToVector2i()
                                    + new Vector2i(0, 32); // move the drop point down so it is centered on the base of the chess piece
                                                           //var p = (Vector2i)(e.Sprite.Position + (cellSize.ToVector2f() / 2.0f));
                                var tpi = ScreenToGridCoordinates(p);
                                Debug.WriteLine($"[Grid] Dropped {tpi.X},{tpi.Y}");

                                var subgridSize = getSubGridSizeOfTile(e.Sprite);
                                if (tpi.X >= 0 && tpi.X + (subgridSize.X - 1) < mapSize.X && tpi.Y >= 0 && tpi.Y + (subgridSize.Y - 1) < mapSize.Y)
                                {
                                    var obstacleFound = false;
                                    /*
                                    for (var y=tpi.Y; y<tpi.Y+subgridSize.Y; y++){
                                        for (var x=tpi.X; x<tpi.X+subgridSize.X; x++){
                                            if (map[x, y, dropLayer] != -1){
                                                obstacleFound=true;
                                                break;
                                            }
                                        }
                                    }
                                    */

                                    // check if move is valid according to board state
                                    if (!obstacleFound)
                                    {
                                        obstacleFound = !IsMoveAllowed(e.Sprite, ScreenToGridCoordinates(e.DragStart), tpi);
                                    }
                                    //var index = map[tpi.X, tpi.Y, dropLayer];
                                    //if (index == -1)
                                    if (!obstacleFound)
                                    {
                                        e.Sprite.Color = new Color(255, 255, 255, 255);
                                        if (!this.tiles.Contains(e.Sprite)) this.tiles.Add(e.Sprite);

                                        var tileIndex = this.tiles.IndexOf(e.Sprite);

                                        // if this item is larger than 1x1 fill the other grid cells with "shadow" indeces
                                        // so that items will not be able to overlap

                                        if (subgridSize.X > 1 || subgridSize.Y > 1)
                                        {
                                            for (var y = 0; y < subgridSize.Y; y++)
                                            {
                                                for (var x = 0; x < subgridSize.X; x++)
                                                {
                                                    map[tpi.X + x, tpi.Y + y, dropLayer] = tileIndex - int.MinValue;
                                                }
                                            }
                                        }

                                        map[tpi.X, tpi.Y, dropLayer] = tileIndex;

                                        e.Sprite.Scale = dropScale;
                                        if (e.OnHandled != null)
                                        {
                                            e.OnHandled.Invoke(e.Sprite);
                                            e.Handled = true;
                                            OnMoved();
                                        }
                                    }
                                }
                            }
                        }
                    };
                }
            }

            moveMarker = new CircleShape(13);
            moveMarker.OutlineThickness = 1;
            moveMarker.FillColor = new Color(0,0,0,64 );
            moveMarker.OutlineColor = new Color(0, 0, 0, 32);
            moveMarker.Origin = moveMarker.GetGlobalBounds().GetCenter();

            moveMarkerOccupied = new CircleShape(24);
            moveMarkerOccupied.FillColor = new Color(0, 0, 0, 0);
            moveMarkerOccupied.OutlineThickness = 6;
            moveMarkerOccupied.OutlineColor = new Color(0, 0, 0, 64);
            moveMarkerOccupied.Origin = moveMarkerOccupied.GetGlobalBounds().GetCenter();
        }

        protected Text boardSquareLabel;
        protected Font font;

        protected abstract List<Vector2i> GetMoves(GamePiece gamePiece, Vector2i from);
        protected abstract List<Vector2i> GetMovesOccupied(GamePiece gamePiece, Vector2i from);

        private List<Vector2i> moves;
        private List<Vector2i> movesOccupied;

        public bool Enabled { get; set; }

        public int this[int x, int y, int z]
        {
            get => map[x, y, z];
            set => map[x, y, z] = value;
        }

        public Vector2i ScreenToGridCoordinates(Vector2i screenCoordinates)
        {
            var wmax = scaledSize;//(Vector2f)window.Size;
            var tmax = wmax.Div(scaledTileSize).Floor();

            return window
                .MapPixelToCoords(screenCoordinates)
                .Sub(Position)
                .Div(wmax)
                .Mul(tmax)
                .ToVector2i();
        }

        public GamePiece SpritePicker(Vector2i screenCoordinates, int pickLayer, out Vector2i gridCoords)
        {
            var tpi = ScreenToGridCoordinates(screenCoordinates);
            gridCoords = tpi;

            if (tpi.X >= 0 && tpi.X < mapSize.X && tpi.Y >= 0 && tpi.Y < mapSize.Y)
            {
                var index = map[tpi.X, tpi.Y, pickLayer];
                if (index != -1)
                {
                    // this is a real index
                    if (index >= 0){
                        return tiles[index];
                    }

                    // this is a shadow index
                    else {
                        var sprite= tiles[index + int.MinValue];

                        // search for the top left coordinate
                        var subgrid= getSubGridSizeOfTile(sprite);
                        for (var y= tpi.Y; y >= Math.Max(0,tpi.Y-subgrid.Y); y--){
                            for (var x = tpi.X; x >= Math.Max(0,tpi.X-subgrid.X); x--){
                                var i = map[x,y,pickLayer];
                                if (i >= 0){
                                    gridCoords = new Vector2i(x, y);
                                    return sprite;
                                }
                            }
                        }
                        throw new InvalidOperationException("the coordinate search failed");
                    }
                }
            }
            return null;
        }

        protected GamePiece draggingSprite;
        protected RenderWindow window;
        protected uint mapLayers;
        protected List<GamePiece> tiles;
        protected int[,,] map;
        protected Vector2u mapSize;
        protected Vector2f scale;
        protected Vector2f tileSize;
        protected Vector2f scaledTileSize;
        protected Vector2f scaledSize;
        protected Vector2f size;
        protected Vector2f position;

        public FloatRect Rect => new FloatRect(Position, ScaledSize);
        public virtual Vector2f Position {
            get => position;
            set => position = value;
        }
        public Vector2f Size => size;
        public Vector2f ScaledSize => scaledSize;
        public Vector2f TileSize => tileSize;
        public Vector2f ScaledTileSize => scaledTileSize;

        protected Vector2u getSubGridSizeOfTile( Sprite tile ){
            //return (Vector2u)tile.GetGlobalBounds().GetSize().Div(ScaledTileSize).Ceiling();
            return new Vector2u(1, 1);
        }

        //protected Sprite shadow;
        protected CircleShape moveMarker;
        protected CircleShape moveMarkerOccupied;

        public virtual void Draw(RenderTarget target, RenderStates states)
        {
            if (moves != null)
            {
                foreach (var move in moves)
                {
                    var pos = move.ToVector2f().Mul(tileSize).Mul(scale);
                    var offset = ScaledTileSize / 2.0f;
                    moveMarker.Position = Position + pos + offset;
                    moveMarker.Draw(window, RenderStates.Default);
                }
            }
            if (movesOccupied != null)
            {
                foreach (var move in movesOccupied)
                {
                    var pos = move.ToVector2f().Mul(tileSize).Mul(scale);
                    var offset = ScaledTileSize / 2.0f;
                    moveMarkerOccupied.Position = Position + pos + offset;
                    moveMarkerOccupied.Draw(window, RenderStates.Default);
                }
            }

            // draw the board
            var drawList = new List<GamePiece>();
            for (var z = 0; z < mapLayers; z++) 
            {
                for (var y = 0; y < mapSize.Y; y++)
                {
                    for (var x = 0; x < mapSize.X; x++)
                    {
                        var tileIndex = map[x, y, z];
                        if (tileIndex >= 0)
                        {
                            var tile = tiles[tileIndex];
                            tile.BoardPosition = new Vector2i(x, y);
                            var tilePosition = new Vector2f(x * tileSize.X * scale.X, y * tileSize.Y * scale.Y);
                            var tileCalculatedSize = tile.GetGlobalBounds().GetSize();
                            Vector2f tileOffset = default;

                            tileOffset = (ScaledTileSize - tileCalculatedSize) / 2.0f;                                
                            tileOffset.Y -= 26;

                            if (tile.isMoving || !tile.moveFinished)
                            {
                                tile.Update();
                            }
                            else {
                                tile.Position = Position + tilePosition + tileOffset;
                            }
                            drawList.Add(tile);
                        }
                    }
                }
            }

            //draw board square labels
            for (var x = 0; x < mapSize.X; x++)
            {
                var board_pos = new Vector2f(x, 7);
                var pos = board_pos.Mul(tileSize).Mul(scale);
                boardSquareLabel.DisplayedString = new Square(x, 7).file.ToString();
                if (x % 2 == 0)
                {
                    boardSquareLabel.FillColor = new Color(255, 255, 255, 148);
                    boardSquareLabel.OutlineColor = new Color(0, 0, 0, 64);
                }
                else
                {
                    boardSquareLabel.FillColor = new Color(0, 0, 0, 148);
                    boardSquareLabel.OutlineColor = new Color(255, 255, 255, 64);
                }

                var offset = ScaledTileSize.Sub(new Vector2f(12f, 22f));// + boardSquareLabel.GetGlobalBounds().GetSize().Mul(0.5f);
                boardSquareLabel.Position = Position + pos + offset;
                boardSquareLabel.Draw(window, RenderStates.Default);
            }
            for (var y = 0; y < mapSize.Y; y++)
            {
                var board_pos = new Vector2f(0, y);
                var pos = board_pos.Mul(tileSize).Mul(scale);
                boardSquareLabel.DisplayedString = new Square(0, y).rank.ToString();
                if (y % 2 != 0)
                {
                    boardSquareLabel.FillColor = new Color(255, 255, 255, 148);
                    boardSquareLabel.OutlineColor = new Color(0, 0, 0, 64);
                }
                else
                {
                    boardSquareLabel.FillColor = new Color(0, 0, 0, 148);
                    boardSquareLabel.OutlineColor = new Color(255, 255, 255, 64);
                }
                var offset = new Vector2f(4, 2); //ScaledTileSize - boardSquareLabel.GetGlobalBounds().GetSize().Add(8f);
                boardSquareLabel.Position = Position + pos + offset;
                boardSquareLabel.Draw(window, RenderStates.Default);
            }

            //draw the pieces
            drawList.Sort((a, b) => ((int)a.Position.Y - (int)b.Position.Y));
            foreach (var spr in drawList.Where(x => x.beingTaken))
            {
                spr.Draw(target, states);
            }
            foreach (var spr in drawList.Where(x => !x.beingTaken))
            {
                spr.Draw(target, states);
            }

            if (draggingSprite != null)
            {
                draggingSprite.Draw(target, states);
            }
        }
    }
}
