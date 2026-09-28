using System.Collections.Generic;
using System.Linq;
using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using Modding;
using MoreDoors.Data;
using PurenailCore.SystemUtil;
using RandomizerMod.Menu;
using RandoSettingsManager;
using static RandomizerMod.Localization;

namespace MoreDoors.Rando;

internal class ConnectionMenu
{
    public static ConnectionMenu? Instance { get; private set; }

    public static void Setup()
    {
        RandomizerMenuAPI.AddMenuPage(OnRandomizerMenuConstruction, TryGetMenuButton);
        MenuChangerMod.OnExitMainMenu += () => Instance = null;

        if (ModHooks.GetMod("RandoSettingsManager") is Mod)
        {
            HookRandoSettingsManager();
        }
    }

    private static void HookRandoSettingsManager() =>
        RandoSettingsManagerMod.Instance.RegisterConnection(new SettingsProxy());

    public static void OnRandomizerMenuConstruction(MenuPage page) => Instance = new(page);

    public static bool TryGetMenuButton(MenuPage page, out SmallButton button)
    {
        button = Instance!.entryButton;
        return true;
    }

    private static void SetColor<T>(MenuItem<T> item, T value, T none)
    {
        item.Text.color = EqualityComparer<T>.Default.Equals(value, none)
            ? Colors.FALSE_COLOR
            : Colors.DEFAULT_COLOR;
    }

    private void SetEnabledColor() =>
        entryButton.Text.color = Settings.IsEnabled ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;

    private MenuItem<T> ModifyColors<T>(string fieldName, T none)
    {
        MenuItem<T> item = (MenuItem<T>)factory.ElementLookup[fieldName];
        item.ValueChanged += value =>
        {
            SetColor(item, value, none);
            SetEnabledColor();
        };
        SetColor(item, item.Value, none);
        return item;
    }

    private void HideIf<T>(MenuItem<T> item, T value, params IMenuElement[] elements)
    {
        item.ValueChanged += UpdateVisibility;
        UpdateVisibility(item.Value);

        void UpdateVisibility(T t)
        {
            if (EqualityComparer<T>.Default.Equals(t, value))
            {
                foreach (var element in elements)
                    element.Hide();
            }
            else
            {
                foreach (var element in elements)
                    element.Show();
            }
        }
    }

    private readonly SmallButton entryButton;
    private readonly MenuElementFactory<RandomizationSettings> factory;
    private readonly MenuItem<DoorsLevel> doorsLevel;
    private readonly MenuItem<AddKeyLocations> addKeyLocations;
    private readonly MenuItem<bool> transitions;

    // Event for any change in settings
    private delegate void CustomDoorsChanged();
    private event CustomDoorsChanged OnCustomDoorsChanged;

    private static RandomizationSettings Settings => MoreDoors.GS.RandoSettings;

    private ConnectionMenu(MenuPage connectionsPage)
    {
        MenuPage moreDoorsPage = new("MoreDoors Main Page", connectionsPage);
        entryButton = new(connectionsPage, Localize("More Doors"));
        entryButton.AddHideAndShowEvent(moreDoorsPage);

        factory = new(moreDoorsPage, Settings);
        Localize(factory);

        doorsLevel = ModifyColors(nameof(Settings.DoorsLevel), DoorsLevel.NoDoors);
        addKeyLocations = ModifyColors(nameof(Settings.AddKeyLocations), AddKeyLocations.None);
        SetEnabledColor();

        SmallButton customizeButton = new(moreDoorsPage, Localize("Customize Doors"));
        OnCustomDoorsChanged += () =>
            customizeButton.Text.color = customizeButton.Hidden
                ? Colors.LOCKED_FALSE_COLOR
                : (
                    Settings.EnabledDoors.Count == DoorData.AllRando().Count
                        ? Colors.DEFAULT_COLOR
                        : Colors.TRUE_COLOR
                );

        transitions =
            (MenuItem<bool>)factory.ElementLookup[nameof(Settings.RandomizeDoorTransitions)];
        HideIf(doorsLevel, DoorsLevel.NoDoors, transitions, customizeButton);
        doorsLevel.ValueChanged += _ => OnCustomDoorsChanged();

        MenuPage customPage = new("MoreDoors Customize Doors", moreDoorsPage);
        FillCustomDoorsPage(customPage);
        customizeButton.AddHideAndShowEvent(customPage);

        new VerticalItemPanel(
            moreDoorsPage,
            SpaceParameters.TOP_CENTER_UNDER_TITLE,
            SpaceParameters.VSPACE_MEDIUM,
            true,
            doorsLevel,
            transitions,
            customizeButton,
            addKeyLocations
        );
        OnCustomDoorsChanged();
    }

    public void ApplySettings(RandomizationSettings settings)
    {
        Settings.CopyFrom(settings);
        factory.SetMenuValues(settings);
        OnCustomDoorsChanged();
    }

    private SmallButton NewDoorsToggleButton(MenuPage page, string text, bool enabled)
    {
        SmallButton b = new(page, text);
        OnCustomDoorsChanged += () =>
        {
            if (DoorData.AllRando().Keys.All(d => Settings.IsDoorEnabled(d) == enabled))
                b.Hide();
            else
                b.Show();
        };
        b.OnClick += () =>
        {
            DoorData.AllRando().Keys.ForEach(d => Settings.SetDoorEnabled(d, enabled));
            OnCustomDoorsChanged();
        };
        return b;
    }

    private void FillCustomDoorsPage(MenuPage page)
    {
        List<IMenuElement> doorButtons = [];
        foreach (var e in DoorData.AllRando())
        {
            var doorName = e.Key;
            var data = e.Value;

            ToggleButton button = new(page, Localize(data.UIName));
            button.ValueChanged += b =>
            {
                Settings.SetDoorEnabled(doorName, b);
                OnCustomDoorsChanged();
            };
            OnCustomDoorsChanged += () =>
            {
                if (Settings.IsDoorEnabled(doorName) != button.Value)
                    button.SetValue(!button.Value);
            };

            doorButtons.Add(button);
        }

        SmallButton enableAllButton = NewDoorsToggleButton(page, "Enable All", true);
        SmallButton disableAllButton = NewDoorsToggleButton(page, "Disable All", false);

        GridItemPanel togglePanel = new(
            page,
            SpaceParameters.TOP_CENTER,
            2,
            SpaceParameters.VSPACE_SMALL,
            SpaceParameters.HSPACE_LARGE,
            false,
            enableAllButton,
            disableAllButton
        );
        GridItemPanel doorsPanel = new(
            page,
            SpaceParameters.TOP_CENTER,
            4,
            SpaceParameters.VSPACE_SMALL,
            SpaceParameters.HSPACE_SMALL,
            false,
            [.. doorButtons]
        );
        new VerticalItemPanel(
            page,
            SpaceParameters.TOP_CENTER,
            SpaceParameters.VSPACE_MEDIUM,
            true,
            togglePanel,
            doorsPanel
        );
    }
}
