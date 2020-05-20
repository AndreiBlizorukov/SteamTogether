﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SteamTogether.Utils;

namespace SteamTogether
{
    class Program
    {
        private static IConfiguration _config;

        static async Task Main()
        {
            Console.WriteLine("Loading..");
            LoadConfiguration();

            var steamApiKey = _config["SteamDevKey"];
            if (string.IsNullOrEmpty(steamApiKey))
            {
                throw new ArgumentException("SteamDevKey should be set");
            }

            var client = new Client(steamApiKey);
            var steamIds = _config.GetSection("users").Get<List<long>>();

            Console.WriteLine("Getting data..");
            try
            {
                var players = await client.GetUsersInfo(steamIds);
                var games = players
                    .SelectMany(x => x.Value.OwnedGames)
                    .Distinct(new GameComparator())
                    .Select(
                        x =>
                        {
                            var names = players.Select(o =>
                                {
                                    return new
                                    {
                                        Name = o.Value.Info.Nickname,
                                        AppIds = o.Value.OwnedGames.Select(x => x.AppId)
                                    };
                                })
                                .Where(y => y.AppIds.Contains(x.AppId))
                                .Select(z => z.Name);

                            return new
                            {
                                x.Name,
                                NickNames = names
                            };
                        })
                    .Where(x => x.NickNames.Count() >= _config.GetSection("FilterCount").Get<int>())
                    .OrderByDescending(x => x.NickNames.Count());

                if (!games.Any()) Console.WriteLine("There are no intersections among owned games");
                foreach (var game in games)
                {
                    Console.WriteLine(
                        $"{game.Name}, count: {game.NickNames.Count()} ({string.Join(",", game.NickNames)})");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong");
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }

        private static void LoadConfiguration()
        {
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            if (!File.Exists(appSettingsPath))
            {
                throw new ArgumentException("Set up the configuration file appsettings.json");
            }

            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();
        }
    }
}