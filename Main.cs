using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Scenarios;
using Game.ScenarioEditor;
using Godot;

public partial class Main : Control
{
    private GalaxyDefinition _galaxy;
    private PlayersDefinition _players;
    private ScenarioDefinition _scenarioDefinition;
    private RulesDefinition _rules;
    private ThemeDefinition _theme;
    private string _scenarioFilePath = string.Empty;
    private string _backgroundImageSourcePath = string.Empty;
    private bool _isDirty;
    private bool _syncingControls;
    private bool _loadingStarForm;
    private bool _loadingSectorForm;

    private SpinBox _rowsInput;
    private SpinBox _columnsInput;
    private SpinBox _playerCountInput;
    private SpinBox _smallStarFontSizeInput;
    private SpinBox _mediumStarFontSizeInput;
    private SpinBox _largeStarFontSizeInput;
    private ColorPickerButton _backgroundColorPicker;
    private ColorPickerButton _sectorLineColorPicker;
    private GalaxyCanvas _galaxyCanvas;
    private VBoxContainer _starTypesList;
    private Button _addStarButton;
    private Button _placeStarButton;
    private VBoxContainer _starForm;
    private LineEdit _starNameInput;
    private SpinBox _starXInput;
    private SpinBox _starYInput;
    private OptionButton _starTypeSelector;
    private CheckBox _starPopulationInput;
    private CheckBox _starResourcesInput;
    private CheckBox _starHomeInput;
    private VBoxContainer _sectorForm;
    private LineEdit _sectorNameInput;
    private Label _scenarioTitleLabel;
    private Label _selectionLabel;
    private Label _statusLabel;
    private int _selectedRow = -1;
    private int _selectedColumn = -1;
    private string _editingStarId = string.Empty;

    public override void _Ready()
    {
        InitializeScenario();
        BuildInterface();
    }

    private void InitializeScenario()
    {
        var scenario = new ScenarioLoader().Load("res://Data/Scenarios/Classic4000AD/scenario.json");
        _scenarioDefinition = scenario.Scenario;
        _galaxy = scenario.Galaxy;
        _players = scenario.Players;
        _rules = scenario.Rules;
        _theme = scenario.Theme;
    }

    private void BuildInterface()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(margin);

        var page = new VBoxContainer();
        margin.AddChild(page);
        var header = new HBoxContainer();
        page.AddChild(header);
        _scenarioTitleLabel = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _scenarioTitleLabel.AddThemeFontSizeOverride("font_size", 24);
        header.AddChild(_scenarioTitleLabel);

        var saveButton = new Button { Text = "Save" };
        saveButton.Pressed += SaveScenario;
        header.AddChild(saveButton);

        var loadButton = new Button { Text = "Load" };
        loadButton.Pressed += RequestLoadScenario;
        header.AddChild(loadButton);
        UpdateScenarioTitle();

        var content = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        page.AddChild(content);

        var sidebarScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(340, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        content.AddChild(sidebarScroll);

        var sidebar = new VBoxContainer();
        sidebarScroll.AddChild(sidebar);
        BuildGalaxyControls(sidebar);
        sidebar.AddChild(new HSeparator());
        BuildPlayerControls(sidebar);
        sidebar.AddChild(new HSeparator());
        BuildStarControls(sidebar);

        _galaxyCanvas = new GalaxyCanvas
        {
            Galaxy = _galaxy,
            CustomMinimumSize = new Vector2(480, 360),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _galaxyCanvas.SectorSelected += OnSectorSelected;
        _galaxyCanvas.StarSelected += OnStarSelected;
        content.AddChild(_galaxyCanvas);
        LoadBackgroundImageForScenario("res://Data/Scenarios/Classic4000AD/scenario.json");
    }

    private void BuildGalaxyControls(VBoxContainer sidebar)
    {
        sidebar.AddChild(new Label { Text = "Galaxy grid" });

        _backgroundColorPicker = new ColorPickerButton
        {
            Color = ParseColor(_galaxy.BackgroundColor),
            CustomMinimumSize = new Vector2(48, 32)
        };
        var colorPicker = _backgroundColorPicker.GetPicker();
        colorPicker.HexVisible = true;
        colorPicker.EditAlpha = false;
        _backgroundColorPicker.ColorChanged += UpdateGalaxyBackgroundColor;
        AddLabeledControl(sidebar, "Background color", _backgroundColorPicker);

        _sectorLineColorPicker = new ColorPickerButton
        {
            Color = ParseColor(_galaxy.SectorLineColor),
            CustomMinimumSize = new Vector2(48, 32)
        };
        var sectorLineColorPicker = _sectorLineColorPicker.GetPicker();
        sectorLineColorPicker.HexVisible = true;
        sectorLineColorPicker.EditAlpha = false;
        _sectorLineColorPicker.ColorChanged += UpdateSectorLineColor;
        AddLabeledControl(sidebar, "Sector line color", _sectorLineColorPicker);

        var backgroundImageControls = new HBoxContainer();
        var chooseBackgroundImageButton = new Button
        {
            Text = "Choose image",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        chooseBackgroundImageButton.Pressed += OpenBackgroundImageDialog;
        backgroundImageControls.AddChild(chooseBackgroundImageButton);

        var clearBackgroundImageButton = new Button { Text = "Clear" };
        clearBackgroundImageButton.Pressed += ClearBackgroundImage;
        backgroundImageControls.AddChild(clearBackgroundImageButton);
        AddLabeledControl(sidebar, "Background image", backgroundImageControls);

        _rowsInput = CreateIntegerInput(1, Math.Max(100, _galaxy.Rows), _galaxy.Rows);
        _rowsInput.ValueChanged += value =>
        {
            if (!_syncingControls)
            {
                RequestGridSize((int)value, _galaxy.Columns);
            }
        };
        AddLabeledControl(sidebar, "Rows", _rowsInput);

        _columnsInput = CreateIntegerInput(1, Math.Max(100, _galaxy.Columns), _galaxy.Columns);
        _columnsInput.ValueChanged += value =>
        {
            if (!_syncingControls)
            {
                RequestGridSize(_galaxy.Rows, (int)value);
            }
        };
        AddLabeledControl(sidebar, "Columns", _columnsInput);

        sidebar.AddChild(new Label { Text = "Star name font size (% of star diameter)" });
        _smallStarFontSizeInput = CreateStarFontSizeInput(
            _galaxy.SmallStarFontSizePercent,
            value => UpdateStarFontSize(StarSize.Small, value));
        AddLabeledControl(sidebar, "Small", _smallStarFontSizeInput);

        _mediumStarFontSizeInput = CreateStarFontSizeInput(
            _galaxy.MediumStarFontSizePercent,
            value => UpdateStarFontSize(StarSize.Medium, value));
        AddLabeledControl(sidebar, "Medium", _mediumStarFontSizeInput);

        _largeStarFontSizeInput = CreateStarFontSizeInput(
            _galaxy.LargeStarFontSizePercent,
            value => UpdateStarFontSize(StarSize.Large, value));
        AddLabeledControl(sidebar, "Large", _largeStarFontSizeInput);
    }

    private static SpinBox CreateStarFontSizeInput(int value, Action<int> onValueChanged)
    {
        var input = CreateIntegerInput(5, 50, value);
        input.ValueChanged += newValue => onValueChanged((int)newValue);
        return input;
    }

    private void UpdateStarFontSize(StarSize size, int value)
    {
        if (_syncingControls)
        {
            return;
        }

        switch (size)
        {
            case StarSize.Small:
                _galaxy.SmallStarFontSizePercent = value;
                break;
            case StarSize.Medium:
                _galaxy.MediumStarFontSizePercent = value;
                break;
            case StarSize.Large:
                _galaxy.LargeStarFontSizePercent = value;
                break;
        }

        _galaxyCanvas.QueueRedraw();
        MarkDirty();
    }

    private void UpdateSectorLineColor(Color color)
    {
        if (_syncingControls)
        {
            return;
        }

        _galaxy.SectorLineColor = ToHtmlColor(color);
        _galaxyCanvas.QueueRedraw();
        MarkDirty();
    }

    private void UpdateGalaxyBackgroundColor(Color color)
    {
        if (_syncingControls)
        {
            return;
        }

        _galaxy.BackgroundColor = ToHtmlColor(color);
        _galaxyCanvas.QueueRedraw();
        MarkDirty();
    }

    private void OpenBackgroundImageDialog()
    {
        var dialog = new FileDialog
        {
            Title = "Choose Galaxy Background Image",
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem
        };
        dialog.AddFilter("*.png, *.jpg, *.jpeg, *.webp, *.bmp, *.tga ; Images");
        dialog.FileSelected += path =>
        {
            SetBackgroundImage(path);
            dialog.QueueFree();
        };
        dialog.Canceled += dialog.QueueFree;
        AddChild(dialog);
        dialog.PopupCenteredRatio(0.8f);
    }

    private void SetBackgroundImage(string path)
    {
        var absolutePath = Path.GetFullPath(path);
        var image = Image.LoadFromFile(absolutePath);
        if (image.IsEmpty())
        {
            SetStatus($"Could not load background image: {path}");
            return;
        }

        _backgroundImageSourcePath = absolutePath;
        _galaxyCanvas.BackgroundTexture = ImageTexture.CreateFromImage(image);
        _galaxyCanvas.QueueRedraw();
        MarkDirty();
    }

    private void ClearBackgroundImage()
    {
        _backgroundImageSourcePath = string.Empty;
        _galaxy.BackgroundImageFile = string.Empty;
        _galaxyCanvas.BackgroundTexture = null;
        _galaxyCanvas.QueueRedraw();
        MarkDirty();
    }

    private bool LoadBackgroundImageForScenario(string scenarioPath)
    {
        _backgroundImageSourcePath = string.Empty;
        _galaxyCanvas.BackgroundTexture = null;
        if (string.IsNullOrWhiteSpace(_galaxy.BackgroundImageFile))
        {
            return true;
        }

        var absoluteScenarioPath = scenarioPath.StartsWith("res://", StringComparison.Ordinal)
            || scenarioPath.StartsWith("user://", StringComparison.Ordinal)
            ? ProjectSettings.GlobalizePath(scenarioPath)
            : Path.GetFullPath(scenarioPath);
        var scenarioDirectory = Path.GetDirectoryName(absoluteScenarioPath);
        if (string.IsNullOrWhiteSpace(scenarioDirectory))
        {
            SetStatus("Could not resolve the scenario folder for its background image.");
            return false;
        }

        var imagePath = Path.GetFullPath(Path.Combine(scenarioDirectory, _galaxy.BackgroundImageFile));
        _backgroundImageSourcePath = imagePath;
        var image = Image.LoadFromFile(imagePath);
        if (image.IsEmpty())
        {
            SetStatus($"Could not load galaxy background image: {imagePath}");
            return false;
        }

        _galaxyCanvas.BackgroundTexture = ImageTexture.CreateFromImage(image);
        return true;
    }

    private void BuildPlayerControls(VBoxContainer sidebar)
    {
        sidebar.AddChild(new Label { Text = "Players" });

        _playerCountInput = CreateIntegerInput(2, 4, _players.Players.Count);
        _playerCountInput.ValueChanged += value =>
        {
            if (!_syncingControls)
            {
                RequestPlayerCount((int)value);
            }
        };
        AddLabeledControl(sidebar, "Number of players", _playerCountInput);

        sidebar.AddChild(new Label { Text = "Star types" });
        _starTypesList = new VBoxContainer();
        sidebar.AddChild(_starTypesList);
        RebuildStarTypeEditors();

        var addStarTypeButton = new Button { Text = "Add Star Type" };
        addStarTypeButton.Pressed += AddStarType;
        sidebar.AddChild(addStarTypeButton);
    }

    private void BuildStarControls(VBoxContainer sidebar)
    {
        _selectionLabel = new Label { Text = "Select a sector to add a star." };
        sidebar.AddChild(_selectionLabel);

        _sectorForm = new VBoxContainer { Visible = false };
        sidebar.AddChild(_sectorForm);
        _sectorNameInput = new LineEdit
        {
            PlaceholderText = "A or A1",
            MaxLength = 2
        };
        _sectorNameInput.TextChanged += OnSectorNameChanged;
        AddLabeledControl(_sectorForm, "Designation", _sectorNameInput);

        _addStarButton = new Button
        {
            Text = "Add Star",
            Disabled = true
        };
        _addStarButton.Pressed += BeginAddStar;
        sidebar.AddChild(_addStarButton);

        _starForm = new VBoxContainer { Visible = false };
        sidebar.AddChild(_starForm);
        _starForm.AddChild(new HSeparator());
        _starForm.AddChild(new Label { Text = "New star" });

        _starNameInput = new LineEdit
        {
            PlaceholderText = "Star name"
        };
        _starNameInput.TextChanged += OnStarNameChanged;
        AddLabeledControl(_starForm, "Name", _starNameInput);

        _starXInput = CreateRelativePositionInput();
        _starXInput.ValueChanged += _ => ApplyEditingStarForm();
        AddLabeledControl(_starForm, "X (0-1)", _starXInput);

        _starYInput = CreateRelativePositionInput();
        _starYInput.ValueChanged += _ => ApplyEditingStarForm();
        AddLabeledControl(_starForm, "Y (0-1)", _starYInput);

        _starTypeSelector = new OptionButton();
        _starTypeSelector.ItemSelected += _ => ApplyEditingStarForm();
        AddLabeledControl(_starForm, "Star type", _starTypeSelector);

        _starPopulationInput = new CheckBox { Text = "Population" };
        _starPopulationInput.Toggled += _ => ApplyEditingStarForm();
        _starForm.AddChild(_starPopulationInput);

        _starResourcesInput = new CheckBox { Text = "Resources" };
        _starResourcesInput.Toggled += _ => ApplyEditingStarForm();
        _starForm.AddChild(_starResourcesInput);

        _starHomeInput = new CheckBox { Text = "Home star" };
        _starHomeInput.Toggled += _ => ApplyEditingStarForm();
        _starForm.AddChild(_starHomeInput);

        _placeStarButton = new Button { Text = "Place Star", Disabled = true };
        _placeStarButton.Pressed += SaveStar;
        _starForm.AddChild(_placeStarButton);

        _statusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        sidebar.AddChild(_statusLabel);
        RefreshStarTypeSelector();
    }

    private static SpinBox CreateIntegerInput(double minimum, double maximum, double value)
    {
        return new SpinBox
        {
            MinValue = minimum,
            MaxValue = maximum,
            Step = 1,
            Value = value,
            AllowGreater = false,
            AllowLesser = false
        };
    }

    private static SpinBox CreateRelativePositionInput()
    {
        return new SpinBox
        {
            MinValue = 0,
            MaxValue = 1,
            Step = 0.01,
            Value = 0.5,
            AllowGreater = false,
            AllowLesser = false
        };
    }

    private static void AddLabeledControl(VBoxContainer parent, string labelText, Control control)
    {
        var row = new HBoxContainer();
        var label = new Label
        {
            Text = labelText,
            CustomMinimumSize = new Vector2(110, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(label);
        row.AddChild(control);
        parent.AddChild(row);
    }

    private void RequestPlayerCount(int count)
    {
        var removedPlayerIds = _players.Players
            .Skip(count)
            .Select(player => player.Id)
            .ToHashSet(StringComparer.Ordinal);
        var affectedAssets = _players.StartingAssets.Count(asset => removedPlayerIds.Contains(asset.PlayerId));
        if (affectedAssets == 0)
        {
            ApplyPlayerCount(count);
            return;
        }

        var dialog = new ConfirmationDialog
        {
            Title = "Reduce player count?",
            DialogText = $"This removes {affectedAssets} starting asset(s) owned by removed players. Continue?"
        };
        dialog.Confirmed += () =>
        {
            ApplyPlayerCount(count);
            dialog.QueueFree();
        };
        dialog.Canceled += () =>
        {
            _syncingControls = true;
            _playerCountInput.Value = _players.Players.Count;
            _smallStarFontSizeInput.Value = _galaxy.SmallStarFontSizePercent;
            _mediumStarFontSizeInput.Value = _galaxy.MediumStarFontSizePercent;
            _largeStarFontSizeInput.Value = _galaxy.LargeStarFontSizePercent;
            _syncingControls = false;
            dialog.QueueFree();
        };
        AddChild(dialog);
        dialog.PopupCentered();
    }

    private void ApplyPlayerCount(int count)
    {
        var removedPlayerIds = _players.Players
            .Skip(count)
            .Select(player => player.Id)
            .ToHashSet(StringComparer.Ordinal);
        _players.StartingAssets.RemoveAll(asset => removedPlayerIds.Contains(asset.PlayerId));

        while (_players.Players.Count < count)
        {
            var index = _players.Players.Count;
            _players.Players.Add(new PlayerDefinition
            {
                Id = $"player-{index + 1}",
                Name = $"Player {index + 1}",
                Color = GetDefaultColor(index)
            });
        }

        if (_players.Players.Count > count)
        {
            _players.Players.RemoveRange(count, _players.Players.Count - count);
        }

        MarkDirty();
    }

    private void RebuildStarTypeEditors()
    {
        if (_starTypesList == null)
        {
            return;
        }

        foreach (var child in _starTypesList.GetChildren())
        {
            child.QueueFree();
        }

        for (var index = 0; index < _galaxy.StarTypes.Count; index++)
        {
            var typeIndex = index;
            var starType = _galaxy.StarTypes[index];
            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = $"Type {index + 1}",
                CustomMinimumSize = new Vector2(55, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            var nameInput = new LineEdit
            {
                Text = starType.Name,
                PlaceholderText = "Type name",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            nameInput.TextChanged += name => UpdateStarTypeName(typeIndex, name);
            row.AddChild(nameInput);

            var colorButton = new ColorPickerButton
            {
                Color = ParseColor(starType.DisplayColor),
                CustomMinimumSize = new Vector2(40, 32)
            };
            colorButton.ColorChanged += color => UpdateStarTypeColor(typeIndex, color);
            row.AddChild(colorButton);

            var sizeSelector = new OptionButton
            {
                CustomMinimumSize = new Vector2(76, 32)
            };
            sizeSelector.AddItem("Small", (int)StarSize.Small);
            sizeSelector.AddItem("Medium", (int)StarSize.Medium);
            sizeSelector.AddItem("Large", (int)StarSize.Large);
            sizeSelector.Select((int)starType.Size);
            sizeSelector.ItemSelected += selected => UpdateStarTypeSize(typeIndex, (StarSize)selected);
            row.AddChild(sizeSelector);

            var removeButton = new Button
            {
                Text = "X",
                CustomMinimumSize = new Vector2(32, 32)
            };
            removeButton.Pressed += () => RequestRemoveStarType(starType.Id);
            row.AddChild(removeButton);
            _starTypesList.AddChild(row);
        }
    }

    private void AddStarType()
    {
        var index = 1;
        while (_galaxy.StarTypes.Any(starType => starType.Id == $"star-type-{index}"))
        {
            index++;
        }

        _galaxy.StarTypes.Add(new StarTypeDefinition
        {
            Id = $"star-type-{index}",
            Name = $"Star Type {index}",
            DisplayColor = GetDefaultColor(index - 1)
        });
        RebuildStarTypeEditors();
        RefreshStarTypeSelector();
        UpdateAddStarButton();
        MarkDirty();
    }

    private void RequestRemoveStarType(string starTypeId)
    {
        var affectedStars = _galaxy.Stars.Count(star => star.StarTypeId == starTypeId);
        if (affectedStars == 0)
        {
            RemoveStarType(starTypeId);
            return;
        }

        var dialog = new ConfirmationDialog
        {
            Title = "Remove star type?",
            DialogText = $"This removes {affectedStars} star(s) using this type. Continue?"
        };
        dialog.Confirmed += () =>
        {
            RemoveStarType(starTypeId);
            dialog.QueueFree();
        };
        dialog.Canceled += dialog.QueueFree;
        AddChild(dialog);
        dialog.PopupCentered();
    }

    private void RemoveStarType(string starTypeId)
    {
        var editingStarIsRemoved = _galaxy.Stars.Any(star =>
            star.Id == _editingStarId && star.StarTypeId == starTypeId);
        var removedStars = _galaxy.Stars.RemoveAll(star => star.StarTypeId == starTypeId);
        _galaxy.StarTypes.RemoveAll(starType => starType.Id == starTypeId);
        if (editingStarIsRemoved)
        {
            _editingStarId = string.Empty;
            _starForm.Visible = false;
        }

        RebuildStarTypeEditors();
        RefreshStarTypeSelector();
        UpdateAddStarButton();
        if (_galaxy.StarTypes.Count == 0)
        {
            _starForm.Visible = false;
        }
        _galaxyCanvas.QueueRedraw();
        MarkDirty();
        SetStatus(removedStars == 0
            ? "Star type removed."
            : $"Star type and {removedStars} star(s) removed.");
    }

    private void UpdateStarTypeName(int index, string name)
    {
        var current = _galaxy.StarTypes[index];
        _galaxy.StarTypes[index] = new StarTypeDefinition
        {
            Id = current.Id,
            Name = name,
            DisplayColor = current.DisplayColor,
            ProductionValue = current.ProductionValue,
            Size = current.Size
        };
        RefreshStarTypeSelector();
        MarkDirty();
    }

    private void UpdateStarTypeColor(int index, Color color)
    {
        var current = _galaxy.StarTypes[index];
        _galaxy.StarTypes[index] = new StarTypeDefinition
        {
            Id = current.Id,
            Name = current.Name,
            DisplayColor = ToHtmlColor(color),
            ProductionValue = current.ProductionValue,
            Size = current.Size
        };
        _galaxyCanvas?.QueueRedraw();
        MarkDirty();
    }

    private void UpdateStarTypeSize(int index, StarSize size)
    {
        var current = _galaxy.StarTypes[index];
        _galaxy.StarTypes[index] = new StarTypeDefinition
        {
            Id = current.Id,
            Name = current.Name,
            DisplayColor = current.DisplayColor,
            ProductionValue = current.ProductionValue,
            Size = size
        };
        _galaxyCanvas.QueueRedraw();
        MarkDirty();
    }

    private void RefreshStarTypeSelector()
    {
        if (_starTypeSelector == null)
        {
            return;
        }

        var selectedIndex = _starTypeSelector.Selected;
        _starTypeSelector.Clear();
        foreach (var starType in _galaxy.StarTypes)
        {
            _starTypeSelector.AddItem(string.IsNullOrWhiteSpace(starType.Name) ? "(unnamed)" : starType.Name);
        }

        _starTypeSelector.Disabled = _galaxy.StarTypes.Count == 0;
        if (_galaxy.StarTypes.Count > 0)
        {
            _starTypeSelector.Select(Math.Clamp(selectedIndex, 0, _galaxy.StarTypes.Count - 1));
        }
    }

    private void RequestGridSize(int rows, int columns)
    {
        var affectedStars = CountStarsOutsideGrid(rows, columns);
        if (affectedStars == 0)
        {
            ApplyGridSize(rows, columns);
            return;
        }

        var dialog = new ConfirmationDialog
        {
            Title = "Resize galaxy?",
            DialogText = $"This resize removes {affectedStars} star(s) from sectors outside the new grid. Continue?"
        };
        dialog.Confirmed += () =>
        {
            ApplyGridSize(rows, columns);
            dialog.QueueFree();
        };
        dialog.Canceled += () =>
        {
            SyncGridInputs();
            dialog.QueueFree();
        };
        AddChild(dialog);
        dialog.PopupCentered();
    }

    private int CountStarsOutsideGrid(int rows, int columns)
    {
        return _galaxy.Stars.Count(star =>
        {
            var sector = _galaxy.Sectors.FirstOrDefault(candidate => candidate.Id == star.SectorId);
            return sector == null || sector.Row >= rows || sector.Column >= columns;
        });
    }

    private void ApplyGridSize(int rows, int columns)
    {
        var removedStars = _galaxy.Stars.RemoveAll(star =>
        {
            var sector = _galaxy.Sectors.FirstOrDefault(candidate => candidate.Id == star.SectorId);
            return sector == null || sector.Row >= rows || sector.Column >= columns;
        });

        _galaxy.Rows = rows;
        _galaxy.Columns = columns;
        RebuildSectors();

        if (_selectedRow >= rows || _selectedColumn >= columns)
        {
            _selectedRow = -1;
            _selectedColumn = -1;
            _addStarButton.Disabled = true;
            _starForm.Visible = false;
            UpdateSelectedSectorEditor();
        }
        else if (_selectedRow >= 0 && _selectedColumn >= 0)
        {
            UpdateSelectedSectorEditor();
        }

        if (!string.IsNullOrEmpty(_editingStarId)
            && !_galaxy.Stars.Exists(star => star.Id == _editingStarId))
        {
            _editingStarId = string.Empty;
            _starForm.Visible = false;
        }

        SyncGridInputs();
        _galaxyCanvas.QueueRedraw();
        MarkDirty();
        SetStatus(removedStars == 0
            ? $"Galaxy grid resized to {rows} x {columns}."
            : $"Galaxy grid resized; removed {removedStars} star(s) in deleted sectors.");
    }

    private void RebuildSectors()
    {
        var existingNames = _galaxy.Sectors.ToDictionary(sector => sector.Id, sector => sector.Name, StringComparer.Ordinal);
        _galaxy.Sectors.Clear();
        for (var row = 0; row < _galaxy.Rows; row++)
        {
            for (var column = 0; column < _galaxy.Columns; column++)
            {
                var id = GetSectorId(row, column);
                var name = existingNames.TryGetValue(id, out var existingName)
                    ? existingName
                    : string.Empty;
                _galaxy.Sectors.Add(new SectorDefinition
                {
                    Id = id,
                    Name = name,
                    Row = row,
                    Column = column
                });
            }
        }
    }

    private void SyncGridInputs()
    {
        _syncingControls = true;
        _rowsInput.MaxValue = Math.Max(100, _galaxy.Rows);
        _columnsInput.MaxValue = Math.Max(100, _galaxy.Columns);
        _rowsInput.Value = _galaxy.Rows;
        _columnsInput.Value = _galaxy.Columns;
        _playerCountInput.Value = _players.Players.Count;
        _syncingControls = false;
    }

    private void OnSectorSelected(int row, int column)
    {
        _selectedRow = row;
        _selectedColumn = column;
        UpdateSelectedSectorEditor();
        UpdateAddStarButton();
        _galaxyCanvas.SelectedRow = row;
        _galaxyCanvas.SelectedColumn = column;
        _galaxyCanvas.QueueRedraw();
        if (_galaxy.StarTypes.Count == 0)
        {
            SetStatus("Add a star type before placing stars.");
        }
    }

    private void UpdateSelectedSectorEditor()
    {
        var sector = _galaxy.Sectors.Find(candidate =>
            candidate.Row == _selectedRow && candidate.Column == _selectedColumn);
        if (sector == null)
        {
            _sectorForm.Visible = false;
            _selectionLabel.Text = "Select a sector to add a star.";
            return;
        }

        _loadingSectorForm = true;
        _sectorNameInput.Text = sector.Name;
        _loadingSectorForm = false;
        _sectorForm.Visible = true;
        _selectionLabel.Text = string.IsNullOrWhiteSpace(sector.Name)
            ? $"Selected Sector {sector.Row + 1}, {sector.Column + 1}"
            : $"Selected Sector {sector.Name}";
    }

    private void OnSectorNameChanged(string text)
    {
        if (_loadingSectorForm)
        {
            return;
        }

        var uppercaseText = text.ToUpperInvariant();
        if (uppercaseText != text)
        {
            var caretColumn = _sectorNameInput.CaretColumn;
            _loadingSectorForm = true;
            _sectorNameInput.Text = uppercaseText;
            _sectorNameInput.CaretColumn = Math.Min(caretColumn, uppercaseText.Length);
            _loadingSectorForm = false;
            text = uppercaseText;
        }

        if (string.IsNullOrEmpty(text))
        {
            SetStatus("A sector designation must start with a letter.");
            return;
        }

        if (!char.IsLetter(text[0])
            || (text.Length == 2 && !char.IsLetterOrDigit(text[1])))
        {
            SetStatus("Use one or two characters: a letter, optionally followed by a letter or number.");
            return;
        }

        var current = _galaxy.Sectors.FindIndex(sector =>
            sector.Row == _selectedRow && sector.Column == _selectedColumn);
        if (current < 0)
        {
            return;
        }

        if (_galaxy.Sectors.Where((sector, index) => index != current)
            .Any(sector => string.Equals(sector.Name, text, StringComparison.OrdinalIgnoreCase)))
        {
            SetStatus($"Sector designation '{text}' is already in use.");
            return;
        }

        var existing = _galaxy.Sectors[current];
        _galaxy.Sectors[current] = new SectorDefinition
        {
            Id = existing.Id,
            Name = text,
            Color = existing.Color,
            Row = existing.Row,
            Column = existing.Column
        };
        _selectionLabel.Text = $"Selected Sector {text}";
        _galaxyCanvas.QueueRedraw();
        MarkDirty();
    }

    private void OnStarSelected(string starId)
    {
        var star = _galaxy.Stars.Find(candidate => candidate.Id == starId);
        if (star == null)
        {
            return;
        }

        var starTypeIndex = _galaxy.StarTypes.FindIndex(starType => starType.Id == star.StarTypeId);
        if (starTypeIndex < 0)
        {
            return;
        }

        var sector = _galaxy.Sectors.Find(candidate => candidate.Id == star.SectorId);
        if (sector == null)
        {
            return;
        }

        _selectedRow = sector.Row;
        _selectedColumn = sector.Column;
        UpdateSelectedSectorEditor();
        _galaxyCanvas.SelectedRow = sector.Row;
        _galaxyCanvas.SelectedColumn = sector.Column;
        _galaxyCanvas.QueueRedraw();
        UpdateAddStarButton();

        _editingStarId = star.Id;
        _starForm.Visible = true;
        _loadingStarForm = true;
        _starNameInput.Text = star.Name.ToUpperInvariant();
        _starXInput.Value = star.Position.X;
        _starYInput.Value = star.Position.Y;
        _starTypeSelector.Select(starTypeIndex);
        _starPopulationInput.ButtonPressed = star.HasPopulation;
        _starResourcesInput.ButtonPressed = star.HasResources;
        _starHomeInput.ButtonPressed = star.IsHomeStar;
        _loadingStarForm = false;
        _placeStarButton.Text = "Done";
        SetStarFormValidity(star.Name);
        SetStatus($"Editing '{star.Name}'.");
    }

    private void BeginAddStar()
    {
        _editingStarId = string.Empty;
        _starForm.Visible = true;
        _loadingStarForm = true;
        _starNameInput.Clear();
        _starXInput.Value = 0.5;
        _starYInput.Value = 0.5;
        _starPopulationInput.ButtonPressed = false;
        _starResourcesInput.ButtonPressed = false;
        _starHomeInput.ButtonPressed = false;
        RefreshStarTypeSelector();
        if (_galaxy.StarTypes.Count > 0)
        {
            _starTypeSelector.Select(0);
        }

        _loadingStarForm = false;
        _placeStarButton.Text = "Place Star";
        SetStarFormValidity(_starNameInput.Text);
        _starNameInput.GrabFocus();
    }

    private void SaveStar()
    {
        if (!string.IsNullOrEmpty(_editingStarId))
        {
            _editingStarId = string.Empty;
            _starForm.Visible = false;
            _placeStarButton.Text = "Place Star";
            SetStatus("Star changes applied.");
            return;
        }

        var name = _starNameInput.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name) || _selectedRow < 0 || _selectedColumn < 0)
        {
            return;
        }

        var starTypeIndex = (int)_starTypeSelector.Selected;
        if (starTypeIndex < 0 || starTypeIndex >= _galaxy.StarTypes.Count)
        {
            SetStatus("Select a star type before placing the star.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_galaxy.StarTypes[starTypeIndex].Name))
        {
            SetStatus("Enter a name for the selected star type before placing the star.");
            return;
        }

        var star = new StarDefinition
        {
            Id = GetNextStarId(),
            Name = name,
            SectorId = GetSectorId(_selectedRow, _selectedColumn),
            StarTypeId = _galaxy.StarTypes[starTypeIndex].Id,
            HasPopulation = _starPopulationInput.ButtonPressed,
            HasResources = _starResourcesInput.ButtonPressed,
            IsHomeStar = _starHomeInput.ButtonPressed,
            Position = new PositionDefinition
            {
                X = (float)_starXInput.Value,
                Y = (float)_starYInput.Value
            }
        };
        _galaxy.Stars.Add(star);

        _galaxyCanvas.QueueRedraw();
        MarkDirty();
        var sector = _galaxy.Sectors.Find(candidate =>
            candidate.Row == _selectedRow && candidate.Column == _selectedColumn);
        var sectorName = sector?.Name ?? $"{_selectedRow + 1}, {_selectedColumn + 1}";
        SetStatus($"Added '{name}' to Sector {sectorName}.");
        _loadingStarForm = true;
        _starNameInput.Clear();
        _loadingStarForm = false;
        SetStarFormValidity(_starNameInput.Text);
        _starNameInput.GrabFocus();
    }

    private void SaveScenario()
    {
        if (!string.IsNullOrWhiteSpace(_scenarioFilePath))
        {
            SaveScenarioTo(_scenarioFilePath);
            return;
        }

        OpenScenarioFileDialog(save: true);
    }

    private void RequestLoadScenario()
    {
        if (!_isDirty)
        {
            OpenScenarioFileDialog(save: false);
            return;
        }

        var dialog = new ConfirmationDialog
        {
            Title = "Discard unsaved changes?",
            DialogText = "Loading another scenario will replace the current edits. Continue?"
        };
        dialog.Confirmed += () =>
        {
            dialog.QueueFree();
            OpenScenarioFileDialog(save: false);
        };
        dialog.Canceled += dialog.QueueFree;
        AddChild(dialog);
        dialog.PopupCentered();
    }

    private void OpenScenarioFileDialog(bool save)
    {
        var initialDirectory = Path.Combine(ProjectSettings.GlobalizePath("user://"), "Scenarios", "4000AD");
        try
        {
            Directory.CreateDirectory(initialDirectory);
        }
        catch (IOException exception)
        {
            SetStatus($"Could not open scenario folder: {exception.Message}");
            return;
        }
        catch (UnauthorizedAccessException exception)
        {
            SetStatus($"Could not open scenario folder: {exception.Message}");
            return;
        }

        var dialog = new FileDialog
        {
            Title = save ? "Save Scenario" : "Load Scenario",
            FileMode = save ? FileDialog.FileModeEnum.SaveFile : FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            CurrentDir = initialDirectory,
            CurrentFile = "scenario.json"
        };
        dialog.AddFilter("*.json ; Scenario files");
        dialog.FileSelected += path =>
        {
            if (save)
            {
                SaveScenarioTo(path);
            }
            else
            {
                LoadScenarioFrom(path);
            }

            dialog.QueueFree();
        };
        dialog.Canceled += dialog.QueueFree;
        AddChild(dialog);
        dialog.PopupCenteredRatio(0.8f);
    }

    private void SaveScenarioTo(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
        {
            path = Path.ChangeExtension(path, ".json");
        }

        var data = new ScenarioData
        {
            Scenario = _scenarioDefinition,
            Galaxy = _galaxy,
            Players = _players,
            Rules = _rules,
            Theme = _theme
        };

        try
        {
            new ScenarioSaver().Save(path, data, _backgroundImageSourcePath);
            _scenarioFilePath = path;
            if (!string.IsNullOrWhiteSpace(_galaxy.BackgroundImageFile))
            {
                _backgroundImageSourcePath = Path.Combine(
                    Path.GetDirectoryName(Path.GetFullPath(path))!,
                    _galaxy.BackgroundImageFile);
            }
            _isDirty = false;
            UpdateScenarioTitle();
            SetStatus($"Saved scenario to {path}.");
        }
        catch (IOException exception)
        {
            SetStatus($"Could not save scenario: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            SetStatus($"Could not save scenario: {exception.Message}");
        }
        catch (ArgumentException exception)
        {
            SetStatus($"Could not save scenario: {exception.Message}");
        }
    }

    private void LoadScenarioFrom(string path)
    {
        ScenarioData scenario;
        try
        {
            scenario = new ScenarioLoader().Load(path);
        }
        catch (IOException exception)
        {
            SetStatus($"Could not load scenario: {exception.Message}");
            return;
        }
        catch (UnauthorizedAccessException exception)
        {
            SetStatus($"Could not load scenario: {exception.Message}");
            return;
        }
        catch (ArgumentException exception)
        {
            SetStatus($"Could not load scenario: {exception.Message}");
            return;
        }

        _scenarioDefinition = scenario.Scenario;
        _galaxy = scenario.Galaxy;
        _players = scenario.Players;
        _rules = scenario.Rules;
        _theme = scenario.Theme;
        _syncingControls = true;
        _backgroundColorPicker.Color = ParseColor(_galaxy.BackgroundColor);
        _sectorLineColorPicker.Color = ParseColor(_galaxy.SectorLineColor);
        _syncingControls = false;
        _scenarioFilePath = path;
        _isDirty = false;
        _selectedRow = -1;
        _selectedColumn = -1;

        _galaxyCanvas.Galaxy = _galaxy;
        _galaxyCanvas.SelectedRow = -1;
        _galaxyCanvas.SelectedColumn = -1;
        var backgroundImageLoaded = LoadBackgroundImageForScenario(path);
        _galaxyCanvas.QueueRedraw();
        _selectionLabel.Text = "Select a sector to add a star.";
        _starForm.Visible = false;
        _sectorForm.Visible = false;
        _starNameInput.Clear();
        _starPopulationInput.ButtonPressed = false;
        _starResourcesInput.ButtonPressed = false;
        _starHomeInput.ButtonPressed = false;
        _placeStarButton.Disabled = true;
        RebuildStarTypeEditors();
        RefreshStarTypeSelector();
        UpdateAddStarButton();
        SyncGridInputs();
        UpdateScenarioTitle();
        if (backgroundImageLoaded)
        {
            SetStatus($"Loaded scenario '{_scenarioDefinition.Name}'.");
        }
    }

    private void UpdateAddStarButton()
    {
        _addStarButton.Disabled = _selectedRow < 0 || _galaxy.StarTypes.Count == 0;
    }

    private void MarkDirty()
    {
        _isDirty = true;
        UpdateScenarioTitle();
        SetStatus("Unsaved changes.");
    }

    private void UpdateScenarioTitle()
    {
        if (_scenarioTitleLabel != null && _scenarioDefinition != null)
        {
            _scenarioTitleLabel.Text =
                $"4000 A.D. Scenario Editor - {_scenarioDefinition.Name}{(_isDirty ? " *" : string.Empty)}";
        }
    }

    private void SetStarFormValidity(string text)
    {
        _placeStarButton.Disabled = string.IsNullOrWhiteSpace(text);
    }

    private void OnStarNameChanged(string text)
    {
        var uppercaseText = text.ToUpperInvariant();
        if (uppercaseText != text)
        {
            var caretColumn = _starNameInput.CaretColumn;
            _loadingStarForm = true;
            _starNameInput.Text = uppercaseText;
            _starNameInput.CaretColumn = Math.Min(caretColumn, uppercaseText.Length);
            _loadingStarForm = false;
            text = uppercaseText;
        }

        SetStarFormValidity(text);
        ApplyEditingStarForm();
    }

    private void ApplyEditingStarForm()
    {
        if (_loadingStarForm || string.IsNullOrEmpty(_editingStarId))
        {
            return;
        }

        var starIndex = _galaxy.Stars.FindIndex(star => star.Id == _editingStarId);
        var starTypeIndex = (int)_starTypeSelector.Selected;
        if (starIndex < 0 || starTypeIndex < 0 || starTypeIndex >= _galaxy.StarTypes.Count)
        {
            return;
        }

        var current = _galaxy.Stars[starIndex];
        _galaxy.Stars[starIndex] = new StarDefinition
        {
            Id = current.Id,
            Name = _starNameInput.Text.Trim(),
            SectorId = current.SectorId,
            StarTypeId = _galaxy.StarTypes[starTypeIndex].Id,
            HasPopulation = _starPopulationInput.ButtonPressed,
            HasResources = _starResourcesInput.ButtonPressed,
            IsHomeStar = _starHomeInput.ButtonPressed,
            Position = new PositionDefinition
            {
                X = (float)_starXInput.Value,
                Y = (float)_starYInput.Value
            }
        };
        _galaxyCanvas.QueueRedraw();
        MarkDirty();
    }

    private string GetNextStarId()
    {
        var index = 1;
        while (_galaxy.Stars.Any(star => star.Id == $"star-{index}"))
        {
            index++;
        }

        return $"star-{index}";
    }

    private static string GetSectorId(int row, int column) => $"sector-r{row + 1}-c{column + 1}";

    private static string GetDefaultColor(int index)
    {
        string[] colors = ["#E74C3C", "#3498DB", "#2ECC71", "#9B59B6"];
        return colors[index % colors.Length];
    }

    private static Color ParseColor(string value)
    {
        if (value.Length == 7
            && value[0] == '#'
            && uint.TryParse(value.Substring(1), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out var rgb))
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255.0f,
                ((rgb >> 8) & 0xFF) / 255.0f,
                (rgb & 0xFF) / 255.0f);
        }

        return Colors.White;
    }

    private static string ToHtmlColor(Color color)
    {
        return $"#{Mathf.RoundToInt(color.R * 255):X2}{Mathf.RoundToInt(color.G * 255):X2}{Mathf.RoundToInt(color.B * 255):X2}";
    }

    private void SetStatus(string text)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = text;
        }
    }
}
