using System.IO;
using SFML.Graphics;
using SFML.Window;
using System;
using System.Collections.Generic;
using SFML.System;
using System.Diagnostics;
using System.Linq;
using Retrodactyl.Chess.Core;
using Retrodactyl.Extensions.SFML;
using Retrodactyl.Extensions.DotNet;

namespace Retrodactyl.Chess
{
    class Program
    {
        


        static void Main(string[] args)
        {
            //perfTest();return;

            var appStartTime = DateTime.Now;

            RenderWindow window = new RenderWindow(new VideoMode(768, 768), "Derpy Chess");
            window.SetFramerateLimit(40);
            window.SetActive();
            window.Closed += (object sender, EventArgs e) => window.Close();

            var parchment = FileSystem.LoadSprite("./data/parchment.png");
            var backgroundSprite = FileSystem.LoadSprite("./data/background_512x512.png");
            var vignette = FileSystem.LoadSprite("./data/vignette_512x512.png");
            var mouseManager = new DragManager(window);
            var storybookFont = FileSystem.LoadFont("./data/storybook.ttf");
            var oldenglishFont = FileSystem.LoadFont("./data/oldenglish.ttf");
            var boardSprite = FileSystem.LoadSprite("./data/board_512x552.png");
            var blackSprites = FileSystem.LoadTiles("./data/black2_128x128.png", 128, 128);
            var whiteSprites = FileSystem.LoadTiles("./data/white3_128x128.png", 128, 128);

            var pixelFont = FileSystem.LoadFont("./data/rainyhearts.ttf");
            GameBoard board = new GameBoard(boardSprite, blackSprites, whiteSprites, pixelFont, new Vector2f(), new Vector2u(8, 8), new Vector2f(1, 1), 1, mouseManager, 0, 0, new Vector2f(1, 1), new Vector2u(64, 64)); ;
            var busyIndicator = new BusyIndicator();

            var resize = new Action<float, float>((float width, float height) =>
            {
                window.SetView(new View(new FloatRect(0, 0, width, height)));
                vignette.Scale = window.Size.ToVector2f().Div(vignette.TextureRect.GetSize().ToVector2f());
                board.Position = ((window.Size.ToVector2f() * 0.5f) - (boardSprite.TextureRect.GetSize().ToVector2f() * 0.5f)) + new Vector2f(0,16);
                parchment.Position = (window.Size.ToVector2f() * 0.5f) - (parchment.TextureRect.GetSize().ToVector2f() * 0.5f);
                busyIndicator.Position = new Vector2f(window.Size.X / 2, board.Position.Y - 72);
            });
            resize(window.Size.X, window.Size.Y);
            window.Resized += (object sender, SizeEventArgs e) => resize(e.Width, e.Height);


            // --- logo -----------
            var logo = FileSystem.LoadSprite("./data/retrodactyl_logo.png");
            logo.Scale = new Vector2f(2, 2);
            logo.Origin = logo.TextureRect.GetSize().ToVector2f() * 0.5f;
            logo.Position = window.Size.ToVector2f() * 0.5f - new Vector2f(0, 25);
            logo.Color = new Color(255, 255, 255, 0);
            var viewingLogo = true;
            var logoFade = false;
            DateTime logoFadeStart = DateTime.MaxValue;
            while (window.IsOpen && viewingLogo)
            {
                window.Clear(new Color(0xe5f1fdff));
                window.DispatchEvents();
                logo.Draw(window, RenderStates.Default);
                window.Display();

                if ((Keyboard.IsKeyPressed(Keyboard.Key.Enter) || Keyboard.IsKeyPressed(Keyboard.Key.Space) || Mouse.IsButtonPressed(Mouse.Button.Left)))
                {
                    logoFade = true;
                    logoFadeStart = DateTime.MinValue;
                }

                if (logoFade && logoFadeStart < DateTime.Now)
                {
                    logo.Color -= new Color(0, 0, 0, 5);
                    if (logo.Color.A < 1)
                    {
                        viewingLogo = false;
                    }
                }
                
                if (!logoFade)
                {
                    logo.Color += new Color(0, 0, 0, 5);
                    if (logo.Color.A >= 254)
                    {
                        logoFadeStart = DateTime.Now + TimeSpan.FromSeconds(2);
                        logoFade = true;
                    }
                }
            }
            
            // ---- story ---------------------------------------------------------------------------------------
            /*
            var storybookText = new Text();
            storybookText.Font = oldenglishFont;
            storybookText.FillColor = new Color(48,32,32, 200);
            storybookText.OutlineColor = new Color(48, 32, 32, 64);
            storybookText.OutlineThickness = 1;
            storybookText.CharacterSize = 48;
            storybookText.DisplayedString =
                "Once upon a time," + Environment.NewLine +
                "there was a dude who" + Environment.NewLine +
                "liked to play chess but" + Environment.NewLine +
                "really sucked at it..." + Environment.NewLine + Environment.NewLine +
                "He never enjoyed chess" + Environment.NewLine +
                "because he never won." + Environment.NewLine + Environment.NewLine +
                "This game is dedicated" + Environment.NewLine +
                "to that guy.";

            storybookText.Position = parchment.Position + new Vector2f(80,64);

            bool viewingStory = true;
            bool storyFade = false;
            while (window.IsOpen && viewingStory)
            {
                window.Clear();
                window.DispatchEvents();

                for (var y = 0; y < window.Size.Y; y += backgroundSprite.TextureRect.Height)
                {
                    for (var x = 0; x < window.Size.X; x += backgroundSprite.TextureRect.Width)
                    {
                        backgroundSprite.Position = new Vector2f(x, y);
                        backgroundSprite.Draw(window, RenderStates.Default);
                    }
                }
                vignette.Draw(window, RenderStates.Default);
                parchment.Draw(window, RenderStates.Default);
                storybookText.Draw(window, RenderStates.Default);
                window.Display();

                if ((Keyboard.IsKeyPressed(Keyboard.Key.Enter) || Keyboard.IsKeyPressed(Keyboard.Key.Space) || Mouse.IsButtonPressed(Mouse.Button.Left)))
                {
                    storyFade = true;
                }
                if (storyFade) { 
                    parchment.Color -= new Color(0, 0, 0, 3);
                    storybookText.FillColor -= new Color(0, 0, 0, 4);
                    storybookText.OutlineColor -= new Color(0, 0, 0, 4);
                    if (parchment.Color.A < 1)
                    {
                        viewingStory = false;
                    }
                }
            }
            */

            // --- title ------------------------------------------------------------------------------------
            var titleText = new Text();
            titleText.Font = oldenglishFont;
            titleText.FillColor = new Color(48, 32, 32, 200);
            titleText.OutlineColor = new Color(48, 32, 32, 64);
            titleText.OutlineThickness = 1;
            titleText.CharacterSize = 92;
            titleText.DisplayedString = "Derpy Chess";
            titleText.Position = new Vector2f(10+parchment.GetGlobalBounds().GetCenter().X - titleText.GetGlobalBounds().GetCenter().X, parchment.Position.Y + 64);

            var playGameText = new Text();
            playGameText.Font = oldenglishFont;
            playGameText.FillColor = new Color(48, 32, 32, 200);
            playGameText.OutlineColor = new Color(48, 32, 32, 64);
            playGameText.OutlineThickness = 1;
            playGameText.CharacterSize = 48;
            playGameText.DisplayedString = "Play Game";
            //playGameText.Origin = playGameText.GetGlobalBounds().GetSize() * 0.5f;
            //playGameText.Position = window.Size.ToVector2f() * 0.5f;
            playGameText.Position =
                new Vector2f(10, 0)
                + parchment.GetGlobalBounds().GetCenter()
                - playGameText.GetGlobalBounds().GetCenter();


            bool viewingTitle = true;
            bool titleFade = false;
            while (window.IsOpen && viewingTitle)
            {
                window.Clear();
                window.DispatchEvents();

                for (var y = 0; y < window.Size.Y; y += backgroundSprite.TextureRect.Height)
                {
                    for (var x = 0; x < window.Size.X; x += backgroundSprite.TextureRect.Width)
                    {
                        backgroundSprite.Position = new Vector2f(x, y);
                        backgroundSprite.Draw(window, RenderStates.Default);
                    }
                }
                vignette.Draw(window, RenderStates.Default);
                parchment.Draw(window, RenderStates.Default);
                titleText.Draw(window, RenderStates.Default);
                playGameText.Draw(window, RenderStates.Default);

                window.Display();

                if ((Keyboard.IsKeyPressed(Keyboard.Key.Enter) || Keyboard.IsKeyPressed(Keyboard.Key.Space) || Mouse.IsButtonPressed(Mouse.Button.Left)))
                {
                    titleFade = true;
                }
                if (titleFade)
                {
                    parchment.Color -= new Color(0, 0, 0, 3);
                    titleText.FillColor -= new Color(0, 0, 0, 4);
                    titleText.OutlineColor -= new Color(0, 0, 0, 4);
                    playGameText.FillColor -= new Color(0, 0, 0, 4);
                    playGameText.OutlineColor -= new Color(0, 0, 0, 4);
                    if (parchment.Color.A < 1)
                    {
                        viewingTitle = false;
                    }
                }
                else
                {
                    parchment.Color += new Color(0, 0, 0, 3);
                    if (playGameText.GetGlobalBounds().Contains(Mouse.GetPosition(window)))
                    {
                        playGameText.FillColor = new Color(150, 0, 32, 200);
                        playGameText.OutlineColor = new Color(150, 0, 32, 64);
                    }
                    else
                    {
                        playGameText.FillColor = new Color(48, 32, 32, 200);
                        playGameText.OutlineColor = new Color(48, 32, 32, 64);
                    }
                }
            }

            // --- game ------------------------------------------------------------------------
            var debugText = new Text();
            debugText.Font = storybookFont;
            debugText.FillColor = new Color(0, 255, 0, 255);
            debugText.CharacterSize = 16;


            if (window.IsOpen)
            {
                // play game
                var endgameText = new Text();
                endgameText.Font = storybookFont;
                endgameText.FillColor = Color.White;
                endgameText.OutlineColor = Color.Black;
                endgameText.OutlineThickness = 2;
                endgameText.CharacterSize = 48;
                endgameText.DisplayedString = "Checkmate!";

                board.Enabled = true;
                while (window.IsOpen)
                {
                    window.Clear();
                    window.DispatchEvents();

                    for (var y = 0; y < window.Size.Y; y += backgroundSprite.TextureRect.Height)
                    {
                        for (var x = 0; x < window.Size.X; x += backgroundSprite.TextureRect.Width)
                        {
                            backgroundSprite.Position = new Vector2f(x, y);
                            backgroundSprite.Draw(window, RenderStates.Default);
                        }
                    }
                    vignette.Draw(window, RenderStates.Default);
                    board.Draw(window, RenderStates.Default);
                    busyIndicator.Visible = board.aiThinking;
                    busyIndicator.Draw(window, RenderStates.Default);
                    if (board.GameEndText != null)
                    {
                        endgameText.DisplayedString = board.GameEndText;
                        var textBounds = endgameText.GetGlobalBounds();
                        endgameText.Position = new Vector2f(
                            (window.Size.X * 0.5f) - (textBounds.Width * 0.5f),
                            window.Size.Y - textBounds.Height - 32);//(window.Size.Y * 0.5f) - (textBounds.Height * 0.5f));
                        endgameText.Draw(window, RenderStates.Default);
                    }

                    // debug view
                    if (Keyboard.IsKeyPressed(Keyboard.Key.D))
                    //#if DEBUG
                    {
                        var labels = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "8", "7", "6", "5", "4", "3", "2", "1" };
                        int i = 0;
                        for (var x= board.Position.X; x< board.Position.X + board.Rect.Width; x+= board.Rect.Width / 8)
                        {
                            debugText.DisplayedString = labels[i++];
                            debugText.Position = new Vector2f(x+32 - debugText.GetGlobalBounds().Width * 0.5f, board.Position.Y - 32 - debugText.GetGlobalBounds().Height * 0.5f);
                            debugText.Draw(window, RenderStates.Default);
                            debugText.Position = new Vector2f(x + 32 - debugText.GetGlobalBounds().Width * 0.5f, board.Position.Y + board.Rect.Height +32 - debugText.GetGlobalBounds().Height * 0.5f);
                            debugText.Draw(window, RenderStates.Default);
                        }
                        for (var y= board.Position.Y; y< board.Position.Y + board.Rect.Height; y+= board.Rect.Height / 8)
                        {
                            debugText.DisplayedString = labels[i++];
                            debugText.Position = new Vector2f(board.Position.X - 32 - debugText.GetGlobalBounds().Width * 0.5f, y+32 - debugText.GetGlobalBounds().Height * 0.5f);
                            debugText.Draw(window, RenderStates.Default);
                            debugText.Position = new Vector2f(board.Position.X + board.Rect.Width + 32 - debugText.GetGlobalBounds().Width * 0.5f, y + 32 - debugText.GetGlobalBounds().Height * 0.5f);
                            debugText.Draw(window, RenderStates.Default);
                        }
                    }
                    //#endif

                    window.Display();
                }
            }
        }
    }
}
