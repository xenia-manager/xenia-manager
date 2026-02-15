using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;

namespace XeniaManager.ViewModels.Pages;

public partial class LibraryPageViewModel : ViewModelBase
{
    // Library properties
    [ObservableProperty] private bool _isGridView = true;
    [ObservableProperty] private string _viewToggleText = "List View";
    [ObservableProperty] private Symbol _viewToggleIcon = Symbol.List;

    // Zoom Properties
    [ObservableProperty] private double _zoomValue = 100;
    public double ZoomMinimum => 50;
    public double ZoomMaximum => 300;
    public double ZoomTickFrequency => 10;
    public string ZoomToolTip => $"{ZoomValue}%";
    public double MinItemWidth => 150 * (ZoomValue / 100.0);
    public double MinItemHeight => 200 * (ZoomValue / 100.0);
    public double ItemSpacing => 8 * (ZoomValue / 100.0);

    // Games List
    public ObservableCollection<string> DummyGames { get; }

    public LibraryPageViewModel()
    {
        string[] titles =
        [
            "Halo 3", "Halo Reach", "Halo 4", "Halo Wars", "Halo 3: ODST",
            "Gears of War", "Gears of War 2", "Gears of War 3", "Gears of War: Judgment",
            "Forza Horizon", "Forza Motorsport 3", "Forza Motorsport 4",
            "Red Dead Redemption", "Red Dead Redemption: Undead Nightmare",
            "Fable II", "Fable III", "Fable Anniversary",
            "Viva Piñata", "Viva Piñata: Trouble in Paradise",
            "Grand Theft Auto IV", "Grand Theft Auto V",
            "Call of Duty: Modern Warfare", "Call of Duty: Modern Warfare 2",
            "Call of Duty: Modern Warfare 3", "Call of Duty: Black Ops",
            "Call of Duty: Black Ops II", "Call of Duty: World at War",
            "The Elder Scrolls V: Skyrim", "The Elder Scrolls IV: Oblivion",
            "Fallout 3", "Fallout: New Vegas",
            "Mass Effect", "Mass Effect 2", "Mass Effect 3",
            "Dead Space", "Dead Space 2", "Dead Space 3",
            "Assassin's Creed", "Assassin's Creed II", "Assassin's Creed: Brotherhood",
            "Assassin's Creed: Revelations", "Assassin's Creed III",
            "Batman: Arkham Asylum", "Batman: Arkham City", "Batman: Arkham Origins",
            "BioShock", "BioShock 2", "BioShock Infinite",
            "Borderlands", "Borderlands 2",
            "Burnout Paradise", "Burnout Revenge",
            "Crackdown", "Crackdown 2",
            "Dark Souls", "Dark Souls II",
            "Dead Rising", "Dead Rising 2", "Dead Rising 3",
            "Destiny",
            "Devil May Cry 4", "DmC: Devil May Cry",
            "Diablo III",
            "Dishonored",
            "Dragon Age: Origins", "Dragon Age II",
            "Far Cry 3", "Far Cry 4",
            "FIFA 14", "FIFA 15",
            "Fight Night Round 3", "Fight Night Champion",
            "Final Fantasy XIII", "Final Fantasy XIII-2",
            "Just Cause 2",
            "Kingdoms of Amalur: Reckoning",
            "L.A. Noire",
            "Left 4 Dead", "Left 4 Dead 2",
            "LEGO Batman", "LEGO Star Wars",
            "Lost Odyssey",
            "Mafia II",
            "Max Payne 3",
            "Metal Gear Solid V: Ground Zeroes",
            "Minecraft",
            "Mirror's Edge",
            "NBA 2K14",
            "Need for Speed: Most Wanted",
            "Ninja Gaiden II",
            "Portal 2",
            "Rage",
            "Resident Evil 5", "Resident Evil 6",
            "Saints Row 2", "Saints Row: The Third", "Saints Row IV",
            "Skate", "Skate 2", "Skate 3",
            "Sleeping Dogs",
            "Sonic Generations",
            "South Park: The Stick of Truth",
            "Splinter Cell: Blacklist",
            "SSX",
            "Tekken 6", "Tekken Tag Tournament 2",
            "The Orange Box",
            "The Witcher 2",
            "Titanfall",
            "Tomb Raider",
            "Watch Dogs",
            "XCOM: Enemy Unknown"
        ];

        DummyGames = new ObservableCollection<string>(
            Enumerable.Range(0, 100000).Select(i => titles[i % titles.Length])
        );
    }

    [RelayCommand]
    private void ToggleView()
    {
        IsGridView = !IsGridView;

        if (IsGridView)
        {
            ViewToggleText = "List View";
            ViewToggleIcon = Symbol.List;
        }
        else
        {
            ViewToggleText = "Grid View";
            ViewToggleIcon = Symbol.Grid;
        }
    }

    partial void OnZoomValueChanged(double value)
    {
        // Notify dependent properties when zoom value changes
        OnPropertyChanged(nameof(ZoomToolTip));
        OnPropertyChanged(nameof(MinItemWidth));
        OnPropertyChanged(nameof(MinItemHeight));
        OnPropertyChanged(nameof(ItemSpacing));
    }
}