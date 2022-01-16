﻿using Microsoft.Xna.Framework.Graphics;
using SadConsole;
using SadConsole.Renderers;
using System;
using System.IO;
using Console = SadConsole.Console;
using SadRogue.Primitives;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace ColorBot;
public class Program {
    public static List<(Console con, Action callback)> jobs=new();
    public DiscordClient discord;
    static void Main(string[] args) {
        var p = new Program();
        p.MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
    }
    async Task MainAsync(string[] args) {
        discord = new(new DiscordConfiguration {
            Token = File.ReadAllText("Token.txt"),
            TokenType = TokenType.Bot,
        });

        var slash = discord.UseSlashCommands();
        //Register globally
        slash.RegisterCommands<SlashCommands>();

        //Register on testing servers
        slash.RegisterCommands<SlashCommands>(522132718650130472);
        slash.RegisterCommands<SlashCommands>(402126095056633859);
        slash.RegisterCommands<SlashCommands>(932105577310593084);

        //Set up a MonoGame instance so that SadConsole can render
        Task.Run(() => {
            //Load in standard font
            SadConsole.Game.Create(1, 1, "IBMCGA.font");
            SadConsole.Game.Instance.DefaultFontSize = IFont.Sizes.Four;
            Game.Instance.OnStart = () => {
                var c = new HookConsole();
                Game.Instance.Screen = c;
            };
            SadConsole.Game.Instance.Run();
        });
        await discord.ConnectAsync();
        await Task.Delay(-1);
    }
}
public class HookConsole : Console {
    public HookConsole() : base(1, 1) { }
    public override void Render(TimeSpan delta) {
        foreach(var q in Program.jobs) {
            q.con.Render(delta);
            q.callback();
        }
        Program.jobs.Clear();
        base.Render(delta);
    }
}
public class SlashCommands : SlashCommandModule {
    [SlashCommand("sendc", "Send a simple colored message")]
    public async Task sendColor(InteractionContext ctx,
        [Choice("Red", "Red")]
        [Choice("Orange", "Orange")]
        [Choice("Yellow", "Yellow")]
        [Choice("Green", "Green")]
        [Choice("Blue", "Blue")]
        [Choice("Cyan", "Cyan")]
        [Option("color", "Text color")] string color = null,
        [Option("text", "Plain message text")] string text = null
        ) {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        var fore = Color.White;
        if (color != null) {
            try {
                var p = typeof(Color).GetField(color);
                if (p == null) {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"Invalid color {color}")
                        );
                    return;
                }
                fore = (Color)p.GetValue(null);
            } catch {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"Invalid color {color}")
                    );
                return;
            }
        }
        if(text == null) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"idk")
                    );
            return;
        }

        //Render the text!!!!
        var str = new ColoredString(text, fore, Color.Black);
        var s = new Console(Math.Min(text.Length, 32), 1 + text.Length/32);
        s.Print(0, 0, str);
        Add(ctx, s, str);
    }
    [SlashCommand("sendf", "Send a color-formatted message")]
    public async Task sendFormat(InteractionContext ctx,
        [Option("text", "Color-formatted text")] string text = null
        ) {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        if (text == null) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"idk")
                    );
            return;
        }
        var str = ColoredString.Parser.Parse(text);
        var s = new Console(Math.Min(str.Length, 32), 1 + str.Length / 32);
        s.Print(0, 0, str);
        Add(ctx, s, str);
    }
    [SlashCommand("sendr", "Send a rainbow message")]
    public async Task sendRainbow(InteractionContext ctx,
        [Option("text", "Plain message text")] string text = null
        ) {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        if (text == null) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"idk")
                    );
            return;
        }


        var str = new ColoredString(text);
        var s = new Console(Math.Min(str.Length, 32), 1 + str.Length / 32);

        //Apply rainbow
        int x = 0;
        foreach (var c in str) {
            c.Foreground = Color.FromHSL((1f * x) / Math.Min(32, str.Length), 1, 0.7f);
            x++;
        }
        s.Print(0, 0, str);

        Add(ctx, s, str);
    }

    public static void Add(InteractionContext ctx, Console s, ColoredString cs) {
        //Add the job to the queue
        Program.jobs.Add((s, Done));

        void Done() {
            //Save image in memory stream (Async file operations are too slow)
            var t = ((ScreenSurfaceRenderer)s.Renderer)._backingTexture;
            var stream = new MemoryStream();
            t.SaveAsPng(stream, t.Bounds.Width, t.Bounds.Height);
            stream.Position = 0;

            //Send it
            ctx.EditResponseAsync(new DiscordWebhookBuilder()
                //.WithUsername("aaaaa")
                .AddFile($"{cs.String}.png", stream)
                );
        }
    }
}