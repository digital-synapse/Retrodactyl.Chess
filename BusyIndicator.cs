using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Text;

namespace Retrodactyl.Chess
{
    public class BusyIndicator : Drawable
    {
        public BusyIndicator()
        {
            shapes = new CircleShape[3] { new CircleShape(), new CircleShape(), new CircleShape()};
            foreach (var shape in shapes)
            {
                shape.FillColor = new Color(220, 200, 200, 0); //Color.Transparent;//Color.White;
                shape.OutlineColor = new Color(220, 200, 200, 0);
                shape.OutlineThickness = 1;
                shape.Radius = 3;
            }
            /*
            shapes[0].Scale = new SFML.System.Vector2f(1, 1);
            shapes[1].Scale = new SFML.System.Vector2f(1.24f, 1.24f);
            shapes[2].Scale = new SFML.System.Vector2f(1.49f, 1.49f);
            */
            shapes[2].Radius = 2f;
            shapes[1].Radius = 3f;
            shapes[0].Radius = 4f;
        }

        CircleShape[] shapes;
        bool[] growing = new bool[] { true,true,true};

        public bool Visible { 
            get => visible; 
            set {
                visible = value;
                if (visible) canDraw = true;
            } 
        }
        private bool visible;
        private bool canDraw = false;
        public void Draw(RenderTarget target, RenderStates states)
        {
            if (canDraw)
            {
                if (Visible)
                {
                    foreach (var shape in shapes)
                    {
                        var a = shape.FillColor.A + 1;
                        if (a >= 50) a = 50;
                        shape.FillColor = new Color(shape.FillColor.R, shape.FillColor.G, shape.FillColor.B, (byte)a);
                        a = shape.OutlineColor.A + 5;
                        if (a >= 255) a = 255;
                        shape.OutlineColor = new Color(shape.OutlineColor.R, shape.OutlineColor.G, shape.OutlineColor.B, (byte)a);
                    }
                }
                else
                {
                    foreach (var shape in shapes)
                    {
                        var a = shape.FillColor.A - 1;
                        if (a <= 0) a = 0;
                        shape.FillColor = new Color(shape.FillColor.R, shape.FillColor.G, shape.FillColor.B, (byte)a);
                        a = shape.OutlineColor.A - 5;
                        if (a <= 0)
                        {
                            a = 0; 
                            canDraw = false;
                            shape.FillColor = new Color(shape.FillColor.R, shape.FillColor.G, shape.FillColor.B, (byte)a);
                        }
                        shape.OutlineColor = new Color(shape.OutlineColor.R, shape.OutlineColor.G, shape.OutlineColor.B, (byte)a);
                    }
                }
                for (var i = 0; i < shapes.Length; i++)
                {
                    var shape = shapes[i];
                    float s = shape.Radius;
                    if (growing[i])
                    {
                        s += 0.075f;
                        if (s >= 4) growing[i] = false;
                    }
                    else
                    {
                        s -= 0.075f;
                        if (s <= 2) growing[i] = true;
                    }
                    shape.Radius = s;
                    shape.Origin = new Vector2f(shape.Radius, shape.Radius);
                    shape.Draw(target, states);
                }
            }
        }

        public Vector2f Position
        {
            get => shapes[0].Position;
            set
            {
                for (var i = 0; i < shapes.Length; i++)
                {
                    shapes[i].Position = new Vector2f(value.X + (i * 25) -25, value.Y);
                }
            }
        }
    }
}
