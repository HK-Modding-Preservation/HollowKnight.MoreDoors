using System.Collections.Generic;
using ConnectionSettingsRando;
using MoreDoors.Data;
using MoreDoors.Rando;
using ConnectionMenu = MoreDoors.Rando.ConnectionMenu;

namespace MoreDoors.CSRInterop;

internal static class CSRInterop
{
    internal static void Setup()
    {
        CSR.Register(
            nameof(MoreDoors),
            rng =>
            {
                var (settings, stats) = RandomizeSettings(rng);
                ConnectionMenu.Instance?.ApplySettings(settings);
                return stats;
            }
        );
    }

    private static (RandomizationSettings, RandomizationStats) RandomizeSettings(System.Random rng)
    {
        SettingsRandomizer randomizer = new();
        var (settings, stats) = randomizer.Randomize(
            MoreDoors.GS.RandoSettings,
            rng,
            nameof(MoreDoors)
        );

        // Randomize the doors individually.
        settings.DoorsLevel = DoorsLevel.AllDoors;

        IReadOnlyList<string> path = [nameof(MoreDoors)];
        if (
            SettingsRandomizer.Skip(
                settings.EnabledDoors.GetType(),
                nameof(settings.EnabledDoors),
                path
            )
        )
            SettingsRandomizer.TrackSkip(nameof(settings.EnabledDoors), path, stats);
        else
        {
            SettingsRandomizer.TrackRando(nameof(settings.EnabledDoors), path, stats);

            settings.EnabledDoors.Clear();
            List<string> keys = [.. DoorData.AllRando().Keys];
            keys.Sort();

            // Toggle each door randomly.
            foreach (var name in keys)
            {
                if (rng.Next(2) == 1)
                    settings.EnabledDoors.Add(name);
            }
        }

        return (settings, stats);
    }
}
