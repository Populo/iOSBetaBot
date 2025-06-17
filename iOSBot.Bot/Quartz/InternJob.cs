using Discord;
using iOSBot.Data;
using iOSBot.Service;
using Microsoft.Extensions.Logging;
using Quartz;

namespace iOSBot.Bot.Quartz;

[DisallowConcurrentExecution]
public class InternJob(
    ICraigService craigService,
    IDiscordService discordService,
    ILogger<InternJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("InternJob Executing...");
        await using var db = new BetaContext();
        var serverCount = discordService.GetServerCount();

        // cycle status
        var content = craigService.GetStatusContent();
        if (content == "server") content = $"Member of: {serverCount} Servers";

        var newStatus = craigService.GetOperationStatus().ToLower() switch
        {
            "sleeping" => UserStatus.AFK,
            "paused" => UserStatus.DoNotDisturb,
            _ => UserStatus.Online,
        };
        _ = discordService.SetStatus(newStatus);

        logger.LogInformation($"New status: {content}");
        _ = discordService.SetActivity(content);

        // update server count
        // keeps killing container for some reason
        try
        {
            var channelId = ulong.Parse(db.Configs.First(c => c.Name == "StatusChannel").Value);
            var env = db.Configs.First(c => c.Name == "Environment").Value;
            var countChannel = await discordService.GetChannel(channelId);
            if (!((IVoiceChannel)countChannel).Name.Contains(serverCount.ToString()))
            {
                await ((IVoiceChannel)countChannel).ModifyAsync(c =>
                    c.Name = $"{env} Bot Servers: {discordService.GetServerCount()}");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error updating server count");
            await discordService.PostError($"Error updating server count channel name:\n{e}");
        }
    }
}