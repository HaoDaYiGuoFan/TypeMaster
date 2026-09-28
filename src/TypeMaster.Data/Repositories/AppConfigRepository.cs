using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TypeMaster.Core.Entities;
using TypeMaster.Core.Interfaces;
using TypeMaster.Data.DbContext;

namespace TypeMaster.Data.Repositories;

public class AppConfigRepository : IAppConfigRepository
{
    private readonly TypeMasterDbContext _ctx;

    public AppConfigRepository(TypeMasterDbContext ctx) => _ctx = ctx;

    public async Task<AppConfig> GetAsync()
    {
        var cfg = await _ctx.AppConfigs.FirstOrDefaultAsync(x => x.Id == 1);
        if (cfg is null)
        {
            cfg = new AppConfig
            {
                Id = 1,
                Theme = 0,
                ShowVirtualKeyboard = true,
                EnableSound = true,
                EnableSpeech = true,
                SpeechRate = 0,
                EnableMusic = true,
                MusicVolume = 55,
                SoundVolume = 80,
                FontFamily = "Consolas",
                FontSize = 22,
                WindowWidth = 1100,
                WindowHeight = 720
            };
            _ctx.AppConfigs.Add(cfg);
            await _ctx.SaveChangesAsync();
        }
        return cfg;
    }

    public async Task SaveAsync(AppConfig config)
    {
        var existing = await _ctx.AppConfigs.FirstOrDefaultAsync(x => x.Id == 1);
        if (existing is null)
        {
            existing = new AppConfig { Id = 1 };
            _ctx.AppConfigs.Add(existing);
        }

        existing.Theme = config.Theme;
        existing.ShowVirtualKeyboard = config.ShowVirtualKeyboard;
        existing.EnableSound = config.EnableSound;
        existing.EnableSpeech = config.EnableSpeech;
        existing.SpeechRate = config.SpeechRate;
        existing.EnableMusic = config.EnableMusic;
        existing.EnableMusic = config.EnableMusic;
        existing.MusicVolume = config.MusicVolume;
        existing.SoundVolume = config.SoundVolume;
        existing.FontFamily = config.FontFamily;
        existing.FontSize = config.FontSize;
        existing.WindowWidth = config.WindowWidth;
        existing.WindowHeight = config.WindowHeight;

        await _ctx.SaveChangesAsync();
    }
}
