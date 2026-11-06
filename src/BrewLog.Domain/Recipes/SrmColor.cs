namespace BrewLog.Domain.Recipes;

/// <summary>Перетворює колір пива в SRM на RGB для кольорової плашки; WPF-типи тут не потрібні.</summary>
public static class SrmColor
{
    /// <summary>R = 255·0,975^SRM, G = 255·0,88^SRM, B = 255·0,7^SRM. SRM 0 дає білий колір.</summary>
    public static (byte R, byte G, byte B) ToRgb(double srm)
    {
        var depth = Math.Max(srm, 0);
        return (Channel(0.975, depth), Channel(0.88, depth), Channel(0.7, depth));
    }

    private static byte Channel(double factor, double srm) => (byte)Math.Round(255 * Math.Pow(factor, srm));
}
