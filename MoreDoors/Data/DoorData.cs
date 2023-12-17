﻿using ItemChanger;
using ItemChanger.Locations;
using MoreDoors.IC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

using JsonUtil = PurenailCore.SystemUtil.JsonUtil<MoreDoors.MoreDoors>;

namespace MoreDoors.Data;

public record DoorData
{
    public static readonly SortedDictionary<string, DoorData> Data = JsonUtil.DeserializeEmbedded<SortedDictionary<string, DoorData>>("MoreDoors.Resources.Data.doors.json");

    public static DoorData GetFromJson(string doorName) => Data[doorName];

    public static DoorData GetFromModule(string doorName) => ItemChangerMod.Modules.Get<MoreDoorsModule>().DoorStates[doorName].Data;

    public static int Count => Data.Count;

    public static void Load()
    {
        foreach (var e in Data)
        {
            var doorName = e.Key;
            var data = e.Value;

            KeyItem key = new(doorName, data);
            key.AddLocationInteropTags(data);

            Finder.DefineCustomItem(key);
            Finder.DefineCustomLocation(data.Key.Location);
        }

        MoreDoors.Log("Loaded Doors");
    }

    public string CamelCaseName;
    public string UpperCaseName;
    public string UIName;

    public record DoorInfo
    {
        public record Location
        {
            public string SceneName;
            public string GateName;
            public bool RequiresLantern;
            public float X;
            public float Y;

            [JsonIgnore]
            public string TransitionName => $"{SceneName}[{GateName}]";

            [JsonIgnore]
            public string TransitionProxyName => $"{SceneName}_Proxy[{GateName}]";
        }

        public ISprite Sprite;
        public string NoKeyDesc;
        public string KeyDesc;
        public Location LeftLocation;
        public Location RightLocation;
        public List<IDeployer>? Deployers;
    }
    public DoorInfo Door;

    public record KeyInfo
    {
        public string ItemName;
        public string UIItemName;
        public string ShopDesc;
        public string InvDesc;
        public string UsedInvDesc;
        public ISprite Sprite;
        public AbstractLocation Location;
        public string Logic;

        public record WorldMapLocation
        {
            public string? SceneName;
            public float X;
            public float Y;

            [JsonIgnore]
            public (string, float, float) AsTuple => (SceneName, X, Y);
        }
        public WorldMapLocation? WorldMapLocationOverride;
        public List<WorldMapLocation>? ExtraWorldMapLocations = null;

        public List<WorldMapLocation> GetWorldMapLocations()
        {
            List<WorldMapLocation> locations = new();

            if (WorldMapLocationOverride != null)
            {
                WorldMapLocation first = WorldMapLocationOverride;
                first.SceneName ??= Location.sceneName;
                locations.Add(first);
            }
            else if (Location is DualLocation dl && dl.trueLocation is CoordinateLocation cl)
            {
                locations.Add(new()
                {
                    SceneName = Location.sceneName,
                    X = cl.x,
                    Y = cl.y
                });
            }
            else
            {
                throw new ArgumentException($"Key {ItemName} is missing world map location");
            }

            ExtraWorldMapLocations?.ForEach(l => locations.Add(l));
            return locations;
        }
    }
    public KeyInfo Key;

    [JsonIgnore]
    public string PDKeyName => $"moreDoors{CamelCaseName}Key";

    [JsonIgnore]
    public string PDDoorOpenedName => $"moreDoors{CamelCaseName}DoorOpened";

    [JsonIgnore]
    public string PDDoorLeftForceOpenedName => $"moreDoors{CamelCaseName}LeftForceOpened";

    [JsonIgnore]
    public string PDDoorRightForceOpenedName => $"moreDoors{CamelCaseName}RightForceOpened";

    [JsonIgnore]
    public string KeyTermName => $"MOREDOORS_{UpperCaseName}_KEY";

    [JsonIgnore]
    public string NoKeyPromptId => $"MOREDOORS_{UpperCaseName}_DOOR_NOKEY";

    [JsonIgnore]
    public string KeyPromptId => $"MOREDOORS_{UpperCaseName}_DOOR_KEY";
}
