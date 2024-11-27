using Discord;
using Discord.WebSocket;

namespace Lostwave.Tools.Services
{
    public class DiscordService
    {
        private IConfiguration _configuration;
        private DiscordSocketClient _client;
        public DiscordService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task Start()
        {
            _client = new DiscordSocketClient();

            var token = _configuration["Discord:BotToken"];

            await _client.LoginAsync(Discord.TokenType.Bot, token);
            await _client.StartAsync();

            await _client.SetCustomStatusAsync("Searching for the most obscure stuff");

            _client.JoinedGuild += OnJoinedGuild;
            _client.Ready += OnClientReady;
        }

        private async Task OnClientReady()
        {
            foreach (var guild in _client.Guilds)
            {
                InitializeGuild(guild);
            }
        }

        private void InitializeGuild(SocketGuild guild)
        {
            var guildCommand = new SlashCommandBuilder();

        }

        private async Task OnJoinedGuild(SocketGuild guild)
        {
            InitializeGuild(guild);
        }
    }
}
