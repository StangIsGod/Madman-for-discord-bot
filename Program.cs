using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Threading;

namespace madman_discord
{
    class Program
    {
        public DiscordSocketClient Client;
        public static CommandService Command;
        public static IServiceProvider Service;

        public string Token = "ここにtokenを入れてね";

        public static SocketMessage LastMessage = null; 

        static void Main(string[] args)
                            => new Program().MainThread().GetAwaiter().GetResult();

        public Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        string SelectedName = "";

        public async Task MessageRecieved(SocketMessage messageP)
        {
            var message = messageP as SocketUserMessage;
            if (message == null)
                return;

            var context = new CommandContext(Client, message);
            var CommandContext = message.Content;

            if (string.IsNullOrWhiteSpace(CommandContext))
                return;

            string[] arr = CommandContext.Split(' ');

            if (message.Author.IsBot)
            { 
                if (CommandContext == "狂人の指定を開始します。\n狂人を希望する方はこのメッセージの下にある絵文字をタッチしてください。")
                {
                    var YourEmoji = new Emoji("😀");
                    await message.AddReactionAsync(YourEmoji);
                    LastMessage = message;
                }
            }
            else
            {
                if (CommandContext == "/madman")
                {
                    await message.Channel.SendMessageAsync("狂人の指定を開始します。\n狂人を希望する方はこのメッセージの下にある絵文字をタッチしてください。");
                }

                if (CommandContext == "/madman help")
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    builder.WithTitle("Madman Help");
                    builder.AddField("/madman", "狂人の募集を開始します。", true);
                    builder.AddField("/madman start", "抽選を開始します。", true);
                    builder.AddField("/madman finish", "狂人の答え合わせをします。", true);
                    builder.AddField("/madman reset", "狂人の募集をリセットします。\n間違えて参加した人が居た場合に使用してください。", true);
                    builder.WithAuthor("Created by STNG");
                    builder.WithColor(Color.DarkRed);

                    await message.Channel.SendMessageAsync("", false, builder.Build());
                }

                if (CommandContext == "/madman start")
                {
                    await message.Channel.SendMessageAsync("狂人の抽選を行います。");
                    Thread.Sleep(1500);
                    int Random = new System.Random().Next(0, Members.Count - 1);
                    var user = Client.GetUser(Members[Random].ID);


                    Console.WriteLine(Members[Random].NAME + "様が選出されました。");
                    SelectedName = Members[Random].NAME;
                    await user.SendMessageAsync("お　前　が　狂　人　だ　会　話　を　狂　わ　せ　ろ");

                  
                    await message.Channel.DeleteMessageAsync(LastMessage);
                    LastMessage = null;
                    Members = new List<JoinMember>();

                    Madman_index = 0;
                    await message.Channel.SendMessageAsync("狂人の抽選が完了しました。狂人に選ばれたユーザーはMadmanから個人メッセージが届きます。\n個人メッセージをご確認ください。\n全員確認した後にゲームをスタートしてください。");

                }

                if (CommandContext == "/madman finish")
                {
                    if (SelectedName != "")
                    {
                        await message.Channel.SendMessageAsync("今回の狂人は 【" + SelectedName + "】様でした。");
                        Console.WriteLine("狂人の正体を発言しました。[" + SelectedName + "]");

                        LastMessage = null;
                        Members = new List<JoinMember>();
                        SelectedName = "";
                        Madman_index = 0;
                        Console.WriteLine("初期化完了。");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("今回の狂人はいませんでした。");
                    }
                    
                }

                if (CommandContext == "/madman reset")
                {
                    if (LastMessage == null)
                        return;


                    await message.Channel.DeleteMessageAsync(LastMessage);
                    LastMessage = null;
                    Members = new List<JoinMember>();
                    SelectedName = "";
                    Madman_index = 0;
                    Console.WriteLine("初期化完了。");
                    await message.Channel.SendMessageAsync("狂人の設定を初期化しました。\nもう一度始める場合は再度[/madman]をチャットで実行してください。");
                }
            }

        }

        ulong[] Madmans = new ulong[20];
        string[] Madmans_name = new string[20];
        int Madman_index;

        public struct JoinMember
        {
            public ulong ID { set; get; }
            public string NAME { set; get; }

        }

        List<JoinMember> Members = new List<JoinMember>();

        public async Task ReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name.Equals("😀"))
            {
                JoinMember _a = new JoinMember();
                _a.ID = reaction.UserId;
                _a.NAME = reaction.User.ToString();

                if (Members.IndexOf(_a) != -1)
                {
                    Members.Remove(_a);
                    Console.WriteLine(_a.NAME + "[" + _a.ID + "]" + "の参加を取り消しにしました。");
                }
            }
        }

        public async Task ReactionAdded(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (message.Id == LastMessage.Id && LastMessage.Author.Id != reaction.UserId)
            {
                if (Madman_index >= 20)
                {
                    await channel.SendMessageAsync(reaction.User.ToString() + "参加人数が上限の20人になったため、これ以上の参加は不可能です。");
                }
                else
                {
                    if (reaction.Emote.Name.Equals("😀"))
                    {
                        JoinMember _a = new JoinMember();
                        _a.ID = reaction.UserId;
                        _a.NAME = reaction.User.ToString();

                        if (Members.IndexOf(_a) == -1)
                        {
                            Members.Add(_a);
                            Console.WriteLine(_a.NAME + "[" + _a.ID + "]" + "が参加しました。");
                        }
                    }
                }
            }
        }

        public async Task MainThread()
        {

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });
            Client.Log += Log;
            Command = new CommandService();
            Service = new ServiceCollection().BuildServiceProvider();
            Client.MessageReceived += MessageRecieved;
            Client.ReactionAdded += ReactionAdded;
            Client.ReactionRemoved += ReactionRemoved;
            await Client.SetGameAsync("/madman help");
            await Command.AddModulesAsync(Assembly.GetEntryAssembly(), Service);
            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();
            await Task.Delay(-1);
        }

    }
}
