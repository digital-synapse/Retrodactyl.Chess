using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Retrodactyl.Chess
{
    public sealed class DragManager
    {
        const int drag_threshold = 10;

        public bool Drop(object sender, GamePiece sprite, Action<Sprite> onHandled, DragEventArgs e)
        {
            var eventArgs = new DropEventArgs() { 
                Sprite = sprite, 
                OnHandled = onHandled,
                DragCurrent = e.DragCurrent,
                DragStart = e.DragStart,
                DragStop = e.DragStop,
                Button = e.Button
            };
            Dropped?.Invoke(sender, eventArgs);
            return eventArgs.Handled;
        }

        public DragManager(RenderWindow window)
        {
            window.MouseButtonPressed += Window_MouseButtonPressed;
            window.MouseButtonReleased += Window_MouseButtonReleased;
            window.MouseMoved += Window_MouseMoved;

            Window = window;
        }

        public RenderWindow Window { get; private set; }

        public EventHandler<DragEventArgs> DragStart;
        public EventHandler<DragEventArgs> DragEnd;
        public EventHandler<DragEventArgs> Dragging;
        public EventHandler<MouseMoveEventArgs> MouseMoved;
        public EventHandler<DropEventArgs> Dropped;

        private  Dictionary<Mouse.Button, bool> buttonDown = new Dictionary<Mouse.Button, bool>() { 
            { Mouse.Button.Left, false }, 
            { Mouse.Button.Middle, false }, 
            { Mouse.Button.Right, false }, 
            { Mouse.Button.XButton1, false },
            { Mouse.Button.XButton2, false }};

        private  Dictionary<Mouse.Button, bool> buttonDragging = new Dictionary<Mouse.Button, bool>() {
            { Mouse.Button.Left, false },
            { Mouse.Button.Middle, false },
            { Mouse.Button.Right, false },
            { Mouse.Button.XButton1, false },
            { Mouse.Button.XButton2, false }};

        private Dictionary<Mouse.Button, Vector2i> dragStart = new Dictionary<Mouse.Button, Vector2i>() {
            { Mouse.Button.Left, default },
            { Mouse.Button.Middle, default },
            { Mouse.Button.Right, default },
            { Mouse.Button.XButton1, default },
            { Mouse.Button.XButton2, default }};


        private void Window_MouseMoved(object sender, SFML.Window.MouseMoveEventArgs e)
        {
            if (buttonDown[Mouse.Button.Left])
            {
                buttonDragging[Mouse.Button.Left] = true;
                Dragging?.Invoke(null, new DragEventArgs()
                {
                    DragStart = dragStart[Mouse.Button.Left],
                    DragCurrent = new Vector2i(e.X, e.Y),
                    DragStop = new Vector2i(e.X, e.Y),
                    Button = Mouse.Button.Left
                });
            }
            if (buttonDown[Mouse.Button.Right])
            {
                buttonDragging[Mouse.Button.Right] = true;
                Dragging?.Invoke(null, new DragEventArgs()
                {
                    DragStart = dragStart[Mouse.Button.Right],
                    DragCurrent = new Vector2i(e.X, e.Y),
                    DragStop = new Vector2i(e.X, e.Y),
                    Button = Mouse.Button.Right
                });

            }
            if (buttonDown[Mouse.Button.Middle])
            {
                buttonDragging[Mouse.Button.Middle] = true;
                Dragging?.Invoke(null, new DragEventArgs()
                {
                    DragStart = dragStart[Mouse.Button.Middle],
                    DragCurrent = new Vector2i(e.X, e.Y),
                    DragStop = new Vector2i(e.X, e.Y),
                    Button = Mouse.Button.Middle
                });
            }
            if (buttonDown[Mouse.Button.XButton1])
            {
                buttonDragging[Mouse.Button.XButton1] = true;
                Dragging?.Invoke(null, new DragEventArgs()
                {
                    DragStart = dragStart[Mouse.Button.XButton1],
                    DragCurrent = new Vector2i(e.X, e.Y),
                    DragStop = new Vector2i(e.X, e.Y),
                    Button = Mouse.Button.XButton1
                });

            }
            if (buttonDown[Mouse.Button.XButton2])
            {
                buttonDragging[Mouse.Button.XButton2] = true;
                Dragging?.Invoke(null, new DragEventArgs()
                {
                    DragStart = dragStart[Mouse.Button.XButton2],
                    DragCurrent = new Vector2i(e.X, e.Y),
                    DragStop = new Vector2i(e.X, e.Y),
                    Button = Mouse.Button.XButton2
                });

            }

            MouseMoved?.Invoke(sender, e);
        }

        private void Window_MouseButtonReleased(object sender, SFML.Window.MouseButtonEventArgs e)
        {
            buttonDown[e.Button] = false;

            if (buttonDragging[e.Button])
            {
                var mp = new Vector2i(e.X, e.Y);
                var delta = dragStart[e.Button] - mp;
                //if (Math.Abs(delta.X) > drag_threshold && Math.Abs(delta.Y) > drag_threshold)
                {
#if DEBUG
                    Debug.WriteLine("[MouseManager] Event DragEnd");
#endif

                    DragEnd?.Invoke(null, new DragEventArgs()
                    {
                        DragStart = dragStart[e.Button],
                        DragCurrent = mp,
                        DragStop = mp,
                        Button = e.Button
                    });
                }

                buttonDragging[e.Button] = false;
            }
        }

        private void Window_MouseButtonPressed(object sender, SFML.Window.MouseButtonEventArgs e)
        {
            if (!buttonDown[e.Button])
            {
                dragStart[e.Button] = new Vector2i(e.X, e.Y);
#if DEBUG
                Debug.WriteLine("[MouseManager] Event DragStart");
#endif
                DragStart?.Invoke(sender, new DragEventArgs()
                {
                    DragStart = dragStart[e.Button],
                    Button = e.Button
                });
            }
            buttonDown[e.Button] = true;
        }
    }


    public class DropEventArgs : DragEventArgs
    {
        public GamePiece Sprite { get; set; }

        public Action<GamePiece> OnHandled { get; set; }

        public bool Handled {get; set;}
    }
    public class DragEventArgs : EventArgs
    {
        public Vector2i DragCurrent { get; set; }
        public Vector2i DragStart { get; set; }
        public Vector2i DragStop { get; set; }
        public Mouse.Button Button { get; set; }
    }
}
