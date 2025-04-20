using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Linq;

class Program
{
    private DiscordSocketClient _client;
    private const string DataFilePath = "data.json";
    private Dictionary<ulong, int> _mitoCounts = new();

    static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers
        });

        _client.Log += LogAsync;
        _client.InteractionCreated += InteractionCreatedAsync;
        _client.Ready += ReadyAsync;

        LoadData();

        var token = "MTM2MzE3ODUwOTIyODU3Mjg4NA.GE8Fip.7QuE6rXqwXuiNwQc0839SeupaM5xhHMVnWBlVY"; // <-- tutaj wklej swój token

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        Console.WriteLine("Bot gotowy.");
        await Task.Delay(-1);
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        Console.WriteLine("Bot jest gotowy!");

        foreach (var guild in _client.Guilds)
        {
            var przybijCommand = new SlashCommandBuilder()
                .WithName("przybij")
                .WithDescription("Przybija mitomanstwo użytkownika")
                .AddOption("user", ApplicationCommandOptionType.User, "Użytkownik do przybicia", isRequired: true);

            var listaCommand = new SlashCommandBuilder()
                .WithName("lista")
                .WithDescription("Sprawdza ile mitomanstw ma użytkownik")
                .AddOption("user", ApplicationCommandOptionType.User, "Użytkownik do sprawdzenia", isRequired: true);

            var rankingCommand = new SlashCommandBuilder()
                .WithName("ranking")
                .WithDescription("Wyświetla top 5 największych mitomanów");

            try
            {
                await guild.CreateApplicationCommandAsync(przybijCommand.Build());
                await guild.CreateApplicationCommandAsync(listaCommand.Build());
                await guild.CreateApplicationCommandAsync(rankingCommand.Build());

                Console.WriteLine($"Zarejestrowano komendy /przybij, /lista i /ranking na {guild.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd przy rejestracji komend: {ex.Message}");
            }
        }
    }

    private async Task InteractionCreatedAsync(SocketInteraction interaction)
    {
        if (interaction is SocketSlashCommand command)
        {
            if (command.CommandName == "przybij")
            {
                var user = (SocketUser)command.Data.Options.First().Value;
                ulong userId = user.Id;

                _mitoCounts[userId] = _mitoCounts.ContainsKey(userId) ? _mitoCounts[userId] + 1 : 1;
                SaveData();

                await command.RespondAsync($"Mitomanstwo zostało przybite: {user.Username} (łącznie: {_mitoCounts[userId]})");
            }
            else if (command.CommandName == "lista")
            {
                var user = (SocketUser)command.Data.Options.First().Value;
                ulong userId = user.Id;
                int count = _mitoCounts.ContainsKey(userId) ? _mitoCounts[userId] : 0;

                await command.RespondAsync($"Użytkownik {user.Username} ma {count} mitomanstw.");
            }
            else if (command.CommandName == "ranking")
            {
                if (_mitoCounts.Count == 0)
                {
                    await command.RespondAsync("Brak danych do wyświetlenia rankingu.");
                    return;
                }

                var top5 = _mitoCounts
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(5)
                    .ToList();

                string ranking = "**🏆 Ranking mitomanów:**\n";
                int position = 1;

                foreach (var (userId, count) in top5)
                {
                    var guild = (command.Channel as SocketGuildChannel)?.Guild;
                    var user = guild != null ? await ((IGuild)guild).GetUserAsync(userId) : null;

                    string name = user != null ? user.Username : $"Użytkownik ({userId})";

                    ranking += $"{position}. **{name}** – {count} mitomanstw\n";
                    position++;
                }

                await command.RespondAsync(ranking);
            }

        }
    }

    private void LoadData()
    {
        if (File.Exists(DataFilePath))
        {
            string json = File.ReadAllText(DataFilePath);
            _mitoCounts = JsonSerializer.Deserialize<Dictionary<ulong, int>>(json) ?? new Dictionary<ulong, int>();
            Console.WriteLine("Dane mitomanstw wczytane.");
        }
        else
        {
            _mitoCounts = new Dictionary<ulong, int>();
        }
    }

    private void SaveData()
    {
        string json = JsonSerializer.Serialize(_mitoCounts, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(DataFilePath, json);
    }
}
