﻿using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServerManagerTool.DiscordBot.Delegates;
using ServerManagerTool.DiscordBot.Interfaces;
using ServerManagerTool.DiscordBot.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManagerTool.DiscordBot
{
    public sealed class ServerManagerBot : IServerManagerBot
    {
        internal ServerManagerBot()
        {
            Started = false;
        }

        public CancellationToken Token { get; private set; }
        public bool Started { get; private set; }  

        public async Task StartAsync(string discordToken, string commandPrefix, string dataDirectory, HandleCommandDelegate handleCommandCallback, HandleTranslationDelegate handleTranslationCallback, CancellationToken token)
        {
            if (Started)
            {
                return;
            }
            Started = true;

            if (string.IsNullOrWhiteSpace(commandPrefix) || string.IsNullOrWhiteSpace(discordToken) || handleTranslationCallback is null || handleCommandCallback is null)
            {
                return;
            }

            Token = token;

            if (commandPrefix.Any(c => !char.IsLetterOrDigit(c)))
            {
                throw new Exception("#DiscordBot_InvalidPrefixError");
            }

            if (!commandPrefix.EndsWith(DiscordBot.PREFIX_DELIMITER))
            {
                commandPrefix += DiscordBot.PREFIX_DELIMITER;
            }

            var settings = new Dictionary<string, string>
            {
                { "DiscordSettings:Token", discordToken },
                { "DiscordSettings:Prefix", commandPrefix },
                { "ServerManager:DataDirectory", dataDirectory },
            };

            // Begin building the configuration file
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var socketConfig = new DiscordSocketConfig
            {
#if DEBUG
                LogLevel = LogSeverity.Verbose,
#else
                LogLevel = LogSeverity.Info,
#endif
                // Tell Discord.Net to cache 1000 messages per channel
                MessageCacheSize = 1000,
            };
            if (Environment.OSVersion.Version < new Version(6, 2))
            {
                // windows 7 or early
                socketConfig.WebSocketProvider = WS4NetProvider.Instance;
            }

            var commandConfig = new CommandServiceConfig
            {
                // Force all commands to run async
                DefaultRunMode = RunMode.Async,
#if DEBUG
                LogLevel = LogSeverity.Verbose,
#else
                LogLevel = LogSeverity.Info,
#endif
            };

            // Build the service provider
            var services = new ServiceCollection()
                // Add the discord client to the service provider
                .AddSingleton(new DiscordSocketClient(socketConfig))
                // Add the command service to the service provider
                .AddSingleton(new CommandService(commandConfig))
                // Add remaining services to the provider
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<LoggingService>()
                .AddSingleton<StartupService>()
                .AddSingleton<ShutdownService>()
                .AddSingleton<Random>()
                .AddSingleton(config)
                .AddSingleton(handleCommandCallback)
                .AddSingleton(handleTranslationCallback)
                .AddSingleton<IServerManagerBot>(this);

            // Create the service provider
            using (var provider = services.BuildServiceProvider())
            {
                // Initialize the logging service, startup service, and command handler
                provider?.GetRequiredService<LoggingService>();
                await provider?.GetRequiredService<StartupService>().StartAsync();
                provider?.GetRequiredService<CommandHandlerService>();

                try
                {
                    // Prevent the application from closing
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (TaskCanceledException)
                {
                    Debug.WriteLine("Task Canceled");
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("Operation Canceled");
                }

                await provider?.GetRequiredService<ShutdownService>().StopAsync();
            }
        }
    }
}
