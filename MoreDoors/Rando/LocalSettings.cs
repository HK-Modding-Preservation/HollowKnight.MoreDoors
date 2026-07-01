using System;
using System.Collections.Generic;
using MoreDoors.Data;
using Newtonsoft.Json;
using RandomizerCore.StringParsing;

namespace MoreDoors.Rando;

public class LocalSettings
{
    public RandomizationSettings Settings = MoreDoors.GS.RandoSettings;
    public HashSet<string> EnabledDoorNames = [];

    [JsonIgnore]
    public HashSet<string> ModifiedLogicNames = [];

    [JsonIgnore]
    public Dictionary<string, Token> LogicSubstitutions = [];

    public bool IncludeDoor(string doorName) => EnabledDoorNames.Contains(doorName);

    public bool IncludeKeyLocation(string doorName, DoorData doorData)
    {
        if (doorData.Key!.Location == null)
            return false;

        return Settings.AddKeyLocations switch
        {
            AddKeyLocations.None => false,
            AddKeyLocations.MatchingDoors => IncludeDoor(doorName),
            AddKeyLocations.AllDoors => true,
            _ => throw new ArgumentException(
                $"Unknown AddKeyLocations: {Settings.AddKeyLocations}"
            ),
        };
    }
}
