namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that returns the currently playing song from the configured music source
/// </summary>
public class CommandScreenMusicGet : ICommand
{
    public Task Execute(IClient? sender, object? data)
    {
        // MusicManager polls in the background, a slow source must not hold up the command queue
        var song = MusicManager.MusicManager.GetInstance().GetCurrent();

        MessageHandler.SendResponse(sender, new
        {
            Command = "screen-music-get-response",
            Data = new
            {
                active = song.Active,
                artist = song.Artist,
                title = song.Title,
                cover = song.Cover,
            },
        });

        return Task.CompletedTask;
    }
}
