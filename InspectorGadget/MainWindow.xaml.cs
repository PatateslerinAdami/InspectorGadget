using Microsoft.Win32;
using ReplayFiles.Core;
using ReplayFiles.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Text.Json.Serialization;
using ReplayFiles.Core.GamePackets;

namespace InspectorGadget
{
    public class AppSettings
    {
        public string ReplaysFolder { get; set; } = "";
        public string CsvFolder { get; set; } = "";
    }

    public enum LogCategory { None, Spell, Missile, Buff, VFX, Animation, LookAt }

    public partial class MainWindow : Window
    {
        private Dictionary<uint, string> _spellHashes = new Dictionary<uint, string>();
        private Dictionary<uint, string> _buffHashes = new Dictionary<uint, string>();
        private Dictionary<uint, string> _vfxHashes = new Dictionary<uint, string>();

        private CancellationTokenSource _scanCts;

        private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };
        private readonly string _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        private class DumpEvent
        {
            public string TimeStr { get; set; }
            public object ParsedData { get; set; }
            public HashSet<uint> InvolvedNetIds { get; set; } = new HashSet<uint>();
            public LogCategory Category { get; set; }
            public string LogText { get; set; }
            public Brush Color { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            this.Closing += (s, e) => SaveSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        FolderPathTextBox.Text = settings.ReplaysFolder;
                        CsvPathTextBox.Text = settings.CsvFolder;
                    }
                }
            }
            catch (Exception ex) { LogMessage($"Failed to load settings: {ex.Message}", Brushes.Red); }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings { ReplaysFolder = FolderPathTextBox.Text, CsvFolder = CsvPathTextBox.Text };
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch { }
        }

        private CheckBox GetCheckBoxForCategory(LogCategory category)
        {
            return category switch
            {
                LogCategory.Spell => ChkSpells,
                LogCategory.Missile => ChkMissiles,
                LogCategory.Buff => ChkBuffs,
                LogCategory.VFX => ChkVFX,
                LogCategory.Animation => ChkAnimations,
                LogCategory.LookAt => ChkLookAt,
                _ => null
            };
        }

        private void LogMessage(string message, Brush color = null)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var tb = new TextBlock { Text = message, Foreground = color ?? Brushes.LightGray, Margin = new Thickness(0, 2, 0, 2), FontFamily = new FontFamily("Consolas") };
                OutputStack.Children.Add(tb);
            });
        }

        private StackPanel AddLogExpander(string header, Brush color, object rawData, LogCategory category = LogCategory.None)
        {
            StackPanel innerPanel = null;
            Dispatcher.Invoke(() =>
            {
                var expander = new Expander { Header = header, Foreground = color, Margin = new Thickness(0, 5, 0, 5), FontFamily = new FontFamily("Consolas"), IsExpanded = true };
                innerPanel = new StackPanel { Margin = new Thickness(20, 5, 0, 5) };

                var dataExpander = new Expander { Header = "View Raw Packet Data", Foreground = Brushes.Gray };
                var rawDataText = new TextBox
                {
                    Text = JsonSerializer.Serialize(rawData, _jsonOptions),
                    Foreground = Brushes.DarkGray,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap
                };
                dataExpander.Content = rawDataText;

                innerPanel.Children.Add(dataExpander);
                expander.Content = innerPanel;

                var cb = GetCheckBoxForCategory(category);
                if (cb != null)
                    expander.SetBinding(UIElement.VisibilityProperty, new Binding("IsChecked") { Source = cb, Converter = (IValueConverter)FindResource("BoolToVis") });

                OutputStack.Children.Add(expander);
            });
            return innerPanel;
        }

        private void AddSubLog(StackPanel parentPanel, string text, Brush color, object rawData, LogCategory category)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var expander = new Expander { Header = text, Foreground = color, Margin = new Thickness(0, 2, 0, 2) };
                var rawDataText = new TextBox
                {
                    Text = JsonSerializer.Serialize(rawData, _jsonOptions),
                    Foreground = Brushes.DarkGray,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap
                };
                expander.Content = rawDataText;

                var cb = GetCheckBoxForCategory(category);
                if (cb != null)
                    expander.SetBinding(UIElement.VisibilityProperty, new Binding("IsChecked") { Source = cb, Converter = (IValueConverter)FindResource("BoolToVis") });

                parentPanel.Children.Add(expander);
            });
        }

        private void AddFileHeaderWithButton(string fileName, string filePath, string targetChampion, string targetEntity)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 15, 0, 5) };
                var tb = new TextBlock { Text = $"[FILE] {fileName}", Foreground = Brushes.White, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };

                string target = !string.IsNullOrEmpty(targetChampion) ? targetChampion : targetEntity;
                var btn = new Button { Content = $"Show ALL Events for {target}", Padding = new Thickness(10, 2, 10, 2), Background = new SolidColorBrush(Color.FromRgb(72, 61, 139)), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
                btn.Click += (s, e) => ShowAllChampionEvents(filePath, targetChampion, targetEntity);

                panel.Children.Add(tb);
                if (!string.IsNullOrEmpty(target)) panel.Children.Add(btn);

                OutputStack.Children.Add(panel);
            });
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Select Replays Folder" };
            if (dialog.ShowDialog() == true) { FolderPathTextBox.Text = dialog.FolderName; SaveSettings(); }
        }

        private void BrowseCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Select CSV Folder" };
            if (dialog.ShowDialog() == true) { CsvPathTextBox.Text = dialog.FolderName; SaveSettings(); }
        }

        private uint HashString(string path)
        {
            uint hash = 0;
            var mask = 0xF0000000;
            for (var i = 0; i < path.Length; i++)
            {
                hash = char.ToLower(path[i]) + 0x10 * hash;
                if ((hash & mask) > 0) hash ^= hash & mask ^ (hash & mask) >> 24;
            }
            return hash;
        }

        private void LoadCsvHashes(string csvFolder)
        {
            _spellHashes.Clear(); _buffHashes.Clear(); _vfxHashes.Clear();
            LoadSingleCsv(Path.Combine(csvFolder, "inibin_hashes.csv"), _spellHashes);
            LoadSingleCsv(Path.Combine(csvFolder, "luaobj_hashes.csv"), _buffHashes);
            LoadSingleCsv(Path.Combine(csvFolder, "troybin_hashes.csv"), _vfxHashes);
            LogMessage($"Loaded {_spellHashes.Count} Spells, {_buffHashes.Count} Buffs, and {_vfxHashes.Count} VFX hashes.", Brushes.LightGreen);
        }

        private void LoadSingleCsv(string path, Dictionary<uint, string> targetDict)
        {
            if (!File.Exists(path)) return;
            bool isFirstLine = true;
            foreach (var line in File.ReadLines(path))
            {
                if (isFirstLine) { isFirstLine = false; continue; }
                var parts = line.Split(',');
                if (parts.Length >= 4)
                {
                    string nameStr = parts[1].Trim().Replace("\"", "");
                    string hashStr = parts[3].Trim().Replace("\"", "");
                    if (hashStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hashStr = hashStr.Substring(2);
                    if (uint.TryParse(hashStr, System.Globalization.NumberStyles.HexNumber, null, out uint hash))
                        targetDict[hash] = nameStr;
                }
            }
        }

        private string GetNameFromHash(uint hash, Dictionary<uint, string> dict)
        {
            return dict.TryGetValue(hash, out string name) ? name : $"0x{hash:X8}";
        }

        private string GetEntityName(uint netId, Dictionary<uint, string> netIdToName)
        {
            if (netId == 0) return "None";
            return netIdToName.TryGetValue(netId, out string name) ? $"{name} ({netId})" : $"Unknown ({netId})";
        }

        private ReplayMetadata ReadMetadataFast(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                reader.ReadBytes(4);
                int jsonLength = reader.ReadInt32();
                byte[] jsonBytes = reader.ReadBytes(jsonLength);
                return JsonSerializer.Deserialize<ReplayMetadata>(jsonBytes, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        private string FormatTime(float realTimeMs)
        {
            return realTimeMs < 0
                ? "-" + TimeSpan.FromMilliseconds(Math.Abs(realTimeMs)).ToString(@"mm\:ss\.fff")
                : TimeSpan.FromMilliseconds(realTimeMs).ToString(@"mm\:ss\.fff");
        }

        private void ProcessNameMapping(object parsedData, Dictionary<uint, string> netIdToName, Dictionary<string, int> nameCounts, HashSet<uint> targetNetIds, string targetChampion, string targetEntity)
        {
            if (parsedData == null) return;

            string baseName = null;
            uint foundNetId = 0;

            if (parsedData is CreateHeroData hero)
            {
                baseName = hero.Skin;
                foundNetId = hero.NetID;
            }
            else if (parsedData is SpawnMinionS2CData minion)
            {
                baseName = string.IsNullOrEmpty(minion.Name) ? minion.SkinName : minion.Name;
                foundNetId = minion.NetID;
            }
            else if (parsedData is SpawnBotS2CData bot)
            {
                baseName = bot.SkinName;
                foundNetId = bot.NetID;
            }
            else if (parsedData is SpawnLevelPropS2CData prop)
            {
                baseName = prop.PropName;
                foundNetId = prop.NetID;
            }
            else if (parsedData is S2C_SpawnTurretData turret)
            {
                baseName = turret.Name;
                foundNetId = turret.NetID;
            }
            else if (parsedData is OnEnterVisibilityClientData visData)
            {
                foreach (var embedded in visData.EmbeddedPackets)
                {
                    ProcessNameMapping(embedded, netIdToName, nameCounts, targetNetIds, targetChampion, targetEntity);
                }
                foreach (var stack in visData.CharacterDataStack)
                {
                    RegisterEntity(stack.SkinName, stack.NetID, netIdToName, nameCounts, targetNetIds, targetChampion, targetEntity);
                }
            }

            if (baseName != null && foundNetId != 0)
            {
                RegisterEntity(baseName, foundNetId, netIdToName, nameCounts, targetNetIds, targetChampion, targetEntity);
            }
        }

        private void RegisterEntity(string baseName, uint netId, Dictionary<uint, string> netIdToName, Dictionary<string, int> nameCounts, HashSet<uint> targetNetIds, string targetChampion, string targetEntity)
        {
            if (netIdToName.ContainsKey(netId)) return;

            if (!nameCounts.ContainsKey(baseName)) nameCounts[baseName] = 1;
            else nameCounts[baseName]++;

            string indexedName = nameCounts[baseName] > 1 ? $"{baseName}_{nameCounts[baseName]}" : baseName;
            netIdToName[netId] = indexedName;

            if ((!string.IsNullOrEmpty(targetChampion) && baseName.Replace(" ", "").Equals(targetChampion.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(targetEntity) && baseName.Replace(" ", "").Equals(targetEntity.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)))
            {
                targetNetIds.Add(netId);
            }
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (_scanCts != null)
            {
                _scanCts.Cancel();
                return;
            }

            string folderPath = FolderPathTextBox.Text;
            string spellName = SpellNameTextBox.Text.Trim();
            string targetChampion = ChampionTextBox.Text.Trim();
            string targetEntity = EntityTextBox.Text.Trim();

            if (string.IsNullOrEmpty(folderPath)) return;
            if (string.IsNullOrEmpty(spellName) && string.IsNullOrEmpty(targetChampion) && string.IsNullOrEmpty(targetEntity))
            {
                LogMessage("Please enter a Champion, Entity, or Spell Name!", Brushes.Red);
                return;
            }

            if (!string.IsNullOrEmpty(CsvPathTextBox.Text)) LoadCsvHashes(CsvPathTextBox.Text);

            float trackDuration = float.Parse(TrackTimeTextBox.Text) * 1000f;
            int maxOccurrences = int.Parse(MaxOccurrencesTextBox.Text);
            uint targetHash = string.IsNullOrEmpty(spellName) ? 0 : HashString(spellName);

            bool includeTargeted = ChkIncludeTargeted.IsChecked == true;

            _scanCts = new CancellationTokenSource();
            var token = _scanCts.Token;

            ScanButton.Content = "Stop Scan";
            ScanButton.Background = Brushes.DarkRed;
            ScanButton.Foreground = Brushes.White;

            OutputStack.Children.Clear();

            if (targetHash != 0) LogMessage($"Searching for Spell: '{spellName}' (Hash: 0x{targetHash:X8})", Brushes.Cyan);
            else LogMessage($"Discovery Mode: Tracking events for '{(!string.IsNullOrEmpty(targetChampion) ? targetChampion : targetEntity)}'", Brushes.Cyan);

            try
            {
                await Task.Run(() =>
                {
                    int occurrencesFound = 0;
                    var files = Directory.EnumerateFiles(folderPath, "*.lrf", SearchOption.AllDirectories).ToList();
                    LogMessage($"Found {files.Count} total .lrf files. Starting scan...\n", Brushes.White);

                    Dictionary<uint, string> discoveredSpells = new Dictionary<uint, string>();

                    for (int i = 0; i < files.Count; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        if (occurrencesFound >= maxOccurrences) break;

                        string file = files[i];
                        bool fileHeaderAdded = false;

                        if (!string.IsNullOrEmpty(targetChampion))
                        {
                            var metadata = ReadMetadataFast(file);
                            if (metadata == null || metadata.Players == null) continue;
                            if (!metadata.Players.Any(p => p.Champion.Replace(" ", "").Equals(targetChampion.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))) continue;

                            AddFileHeaderWithButton(Path.GetFileName(file), file, targetChampion, targetEntity);
                            fileHeaderAdded = true;
                        }

                        try
                        {
                            using FileStream stream = File.OpenRead(file);
                            var packets = new ReplayReader().Parse(stream).ToList();

                            float gameStartTime = packets.FirstOrDefault(p => p.Payload.Length > 0 && p.Payload.Span[0] == 0x5C)?.Time ?? 0;

                            var activeTrackers = new List<(float EndTimeMs, uint CasterNetId, StackPanel UIContainer)>();
                            HashSet<uint> targetNetIds = new HashSet<uint>();
                            Dictionary<uint, string> netIdToName = new Dictionary<uint, string>();
                            Dictionary<string, int> nameCounts = new Dictionary<string, int>();

                            foreach (var packet in packets)
                            {
                                if (token.IsCancellationRequested) break;
                                if (packet.Payload.Length == 0) continue;

                                uint packetId = packet.PacketId;
                                float realTimeMs = packet.Time - gameStartTime;
                                string timeStr = FormatTime(realTimeMs);

                                ProcessNameMapping(packet.ParsedData, netIdToName, nameCounts, targetNetIds, targetChampion, targetEntity);

                                if (packetId == 0xB5 && packet.ParsedData is NPC_CastSpellAnsData castData)
                                {
                                    bool spellMatch = targetHash == 0 || castData.SpellHash == targetHash;
                                    bool isCaster = targetNetIds.Count == 0 || targetNetIds.Contains(castData.CasterNetID);
                                    bool isTarget = includeTargeted && castData.TargetNetIDs.Any(id => targetNetIds.Contains(id));

                                    if (spellMatch && (isCaster || isTarget))
                                    {
                                        if (!fileHeaderAdded) { AddFileHeaderWithButton(Path.GetFileName(file), file, targetChampion, targetEntity); fileHeaderAdded = true; }

                                        occurrencesFound++;
                                        string castedSpellName = discoveredSpells.TryGetValue(castData.SpellHash, out string knownName) ? knownName : GetNameFromHash(castData.SpellHash, _spellHashes);
                                        string casterName = GetEntityName(castData.CasterNetID, netIdToName);

                                        string header = $"[{timeStr}] [OCCURRENCE #{occurrencesFound}] Spell '{castedSpellName}' casted by {casterName}!";
                                        var uiContainer = AddLogExpander(header, Brushes.Yellow, castData, LogCategory.None);
                                        activeTrackers.Add((realTimeMs + trackDuration, castData.CasterNetID, uiContainer));
                                    }
                                }
                                else if (packetId == 0x3B && packet.ParsedData is MissileReplicationData misData)
                                {
                                    bool spellMatch = targetHash == 0 || misData.SpellHash == targetHash;
                                    bool isCaster = targetNetIds.Count == 0 || targetNetIds.Contains(misData.CasterNetID);

                                    if (spellMatch && isCaster)
                                    {
                                        if (!fileHeaderAdded) { AddFileHeaderWithButton(Path.GetFileName(file), file, targetChampion, targetEntity); fileHeaderAdded = true; }

                                        occurrencesFound++;
                                        string castedSpellName = discoveredSpells.TryGetValue(misData.SpellHash, out string knownName) ? knownName : GetNameFromHash(misData.SpellHash, _spellHashes);
                                        string casterName = GetEntityName(misData.CasterNetID, netIdToName);

                                        string header = $"[{timeStr}] [OCCURRENCE #{occurrencesFound}] Missile '{castedSpellName}' spawned by {casterName}!";
                                        var uiContainer = AddLogExpander(header, Brushes.Orange, misData, LogCategory.None);
                                        activeTrackers.Add((realTimeMs + trackDuration, misData.CasterNetID, uiContainer));
                                    }
                                }

                                activeTrackers.RemoveAll(t => realTimeMs > t.EndTimeMs);

                                if (activeTrackers.Count > 0)
                                {
                                    foreach (var tracker in activeTrackers)
                                    {
                                        float timeOffsetMs = realTimeMs - (tracker.EndTimeMs - trackDuration);

                                        if (packetId == 0xB7 && packet.ParsedData is NPC_BuffAdd2Data buffData)
                                        {
                                            bool isCaster = buffData.CasterNetID == tracker.CasterNetId;
                                            bool isTarget = includeTargeted && buffData.RoutingNetID == tracker.CasterNetId;

                                            if (isCaster || isTarget)
                                            {
                                                string buffName = GetNameFromHash(buffData.BuffNameHash, _buffHashes);
                                                string targetName = GetEntityName(buffData.RoutingNetID, netIdToName);
                                                string casterName = GetEntityName(buffData.CasterNetID, netIdToName);
                                                AddSubLog(tracker.UIContainer, $"[+{timeOffsetMs:0}ms] BUFF: {buffName} | Caster: {casterName} -> Target: {targetName}", Brushes.LightPink, buffData, LogCategory.Buff);
                                                if (!discoveredSpells.ContainsKey(buffData.BuffNameHash) && !buffName.StartsWith("0x")) discoveredSpells[buffData.BuffNameHash] = buffName;
                                            }
                                        }
                                        else if (packetId == 0x87 && packet.ParsedData is FXCreateGroupPacketData fxData)
                                        {
                                            foreach (var group in fxData.Groups)
                                            {
                                                foreach (var fx in group.FXCreateData)
                                                {
                                                    bool isCaster = fx.CasterNetID == tracker.CasterNetId;
                                                    bool isTarget = includeTargeted && fx.TargetNetID == tracker.CasterNetId;

                                                    if (isCaster || isTarget)
                                                    {
                                                        string vfxName = GetNameFromHash(group.EffectNameHash, _vfxHashes);
                                                        string casterName = GetEntityName(fx.CasterNetID, netIdToName);
                                                        string targetName = GetEntityName(fx.TargetNetID, netIdToName);
                                                        AddSubLog(tracker.UIContainer, $"[+{timeOffsetMs:0}ms] VFX: {vfxName} | Caster: {casterName} -> Target: {targetName}", Brushes.LightSkyBlue, group, LogCategory.VFX);
                                                        if (!discoveredSpells.ContainsKey(group.EffectNameHash) && !vfxName.StartsWith("0x")) discoveredSpells[group.EffectNameHash] = vfxName.Replace(".troy", "");
                                                    }
                                                }
                                            }
                                        }
                                        else if (packetId == 0xB0 && packet.ParsedData is S2C_PlayAnimationData animData && animData.RoutingNetID == tracker.CasterNetId)
                                        {
                                            AddSubLog(tracker.UIContainer, $"[+{timeOffsetMs:0}ms] ANIMATION PLAYED: {animData.AnimationName}", Brushes.MediumPurple, animData, LogCategory.Animation);
                                        }
                                        else if (packetId == 0x10F && packet.ParsedData is S2C_UnitSetLookAtData lookAtData)
                                        {
                                            bool isActor = lookAtData.RoutingNetID == tracker.CasterNetId;
                                            bool isTarget = includeTargeted && lookAtData.TargetNetID == tracker.CasterNetId;

                                            if (isActor || isTarget)
                                            {
                                                string actorName = GetEntityName(lookAtData.RoutingNetID, netIdToName);
                                                string targetName = GetEntityName(lookAtData.TargetNetID, netIdToName);
                                                AddSubLog(tracker.UIContainer, $"[+{timeOffsetMs:0}ms] UNIT LOOK AT: {actorName} -> {targetName}", Brushes.LightSeaGreen, lookAtData, LogCategory.LookAt);
                                            }
                                        }
                                    }
                                }

                                if (occurrencesFound >= maxOccurrences && activeTrackers.Count == 0) break;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Error parsing {Path.GetFileName(file)}: {ex.Message}", Brushes.Red);
                        }
                    }

                    if (token.IsCancellationRequested)
                        LogMessage("\nScan Stopped by User.", Brushes.Orange);
                    else
                        LogMessage("\nScan Complete!", Brushes.LimeGreen);
                }, token);
            }
            finally
            {
                _scanCts?.Dispose();
                _scanCts = null;

                ScanButton.Content = "Start Scan";
                ScanButton.ClearValue(Button.BackgroundProperty);
                ScanButton.ClearValue(Button.ForegroundProperty);
            }
        }

        private async void ShowAllChampionEvents(string filePath, string targetChampion, string targetEntity)
        {
            OutputStack.Children.Clear();
            string target = !string.IsNullOrEmpty(targetChampion) ? targetChampion : targetEntity;
            LogMessage($"Dumping ALL events for {target} in {Path.GetFileName(filePath)}...", Brushes.Cyan);

            bool includeTargeted = ChkIncludeTargeted.IsChecked == true;
            int maxEntities = int.TryParse(MaxDumpTextBox.Text, out int md) ? md : 6;

            await Task.Run(() =>
            {
                try
                {
                    using FileStream stream = File.OpenRead(filePath);
                    var packets = new ReplayReader().Parse(stream).ToList();

                    float gameStartTime = packets.FirstOrDefault(p => p.Payload.Length > 0 && p.Payload.Span[0] == 0x5C)?.Time ?? 0;

                    HashSet<uint> targetNetIds = new HashSet<uint>();
                    Dictionary<uint, string> netIdToName = new Dictionary<uint, string>();
                    Dictionary<string, int> nameCounts = new Dictionary<string, int>();

                    var allDumpEvents = new List<DumpEvent>();

                    foreach (var packet in packets)
                    {
                        if (packet.Payload.Length == 0) continue;
                        uint packetId = packet.PacketId;
                        float realTimeMs = packet.Time - gameStartTime;
                        string timeStr = FormatTime(realTimeMs);

                        ProcessNameMapping(packet.ParsedData, netIdToName, nameCounts, targetNetIds, targetChampion, targetEntity);

                        if (targetNetIds.Count == 0) continue;

                        DumpEvent dumpEv = null;

                        if (packetId == 0xB5 && packet.ParsedData is NPC_CastSpellAnsData castData)
                        {
                            bool isCaster = targetNetIds.Contains(castData.CasterNetID);
                            bool isTarget = includeTargeted && castData.TargetNetIDs.Any(id => targetNetIds.Contains(id));

                            if (isCaster || isTarget)
                            {
                                string name = GetNameFromHash(castData.SpellHash, _spellHashes);
                                string casterName = GetEntityName(castData.CasterNetID, netIdToName);
                                dumpEv = new DumpEvent { TimeStr = timeStr, ParsedData = castData, Category = LogCategory.Spell, Color = Brushes.Yellow, LogText = $"[{timeStr}] SPELL CAST: {name} by {casterName}" };
                                if (isCaster) dumpEv.InvolvedNetIds.Add(castData.CasterNetID);
                                if (isTarget) foreach (var id in castData.TargetNetIDs) if (targetNetIds.Contains(id)) dumpEv.InvolvedNetIds.Add(id);
                            }
                        }
                        else if (packetId == 0x3B && packet.ParsedData is MissileReplicationData misData)
                        {
                            bool isCaster = targetNetIds.Contains(misData.CasterNetID);
                            if (isCaster)
                            {
                                string name = GetNameFromHash(misData.SpellHash, _spellHashes);
                                string casterName = GetEntityName(misData.CasterNetID, netIdToName);
                                dumpEv = new DumpEvent { TimeStr = timeStr, ParsedData = misData, Category = LogCategory.Missile, Color = Brushes.Orange, LogText = $"[{timeStr}] MISSILE: {name} by {casterName}" };
                                dumpEv.InvolvedNetIds.Add(misData.CasterNetID);
                            }
                        }
                        else if (packetId == 0xB7 && packet.ParsedData is NPC_BuffAdd2Data buffData)
                        {
                            bool isCaster = targetNetIds.Contains(buffData.CasterNetID);
                            bool isTarget = includeTargeted && targetNetIds.Contains(buffData.RoutingNetID);

                            if (isCaster || isTarget)
                            {
                                string name = GetNameFromHash(buffData.BuffNameHash, _buffHashes);
                                string targetName = GetEntityName(buffData.RoutingNetID, netIdToName);
                                string casterName = GetEntityName(buffData.CasterNetID, netIdToName);
                                dumpEv = new DumpEvent { TimeStr = timeStr, ParsedData = buffData, Category = LogCategory.Buff, Color = Brushes.LightPink, LogText = $"[{timeStr}] BUFF: {name} | Caster: {casterName} -> Target: {targetName}" };
                                if (isCaster) dumpEv.InvolvedNetIds.Add(buffData.CasterNetID);
                                if (isTarget) dumpEv.InvolvedNetIds.Add(buffData.RoutingNetID);
                            }
                        }
                        else if (packetId == 0x87 && packet.ParsedData is FXCreateGroupPacketData fxData)
                        {
                            foreach (var group in fxData.Groups)
                            {
                                foreach (var fx in group.FXCreateData)
                                {
                                    bool isCaster = targetNetIds.Contains(fx.CasterNetID);
                                    bool isTarget = includeTargeted && targetNetIds.Contains(fx.TargetNetID);

                                    if (isCaster || isTarget)
                                    {
                                        string name = GetNameFromHash(group.EffectNameHash, _vfxHashes);
                                        string casterName = GetEntityName(fx.CasterNetID, netIdToName);
                                        string targetName = GetEntityName(fx.TargetNetID, netIdToName);
                                        dumpEv = new DumpEvent { TimeStr = timeStr, ParsedData = group, Category = LogCategory.VFX, Color = Brushes.LightSkyBlue, LogText = $"[{timeStr}] VFX SPAWNED: {name} | Caster: {casterName} -> Target: {targetName}" };
                                        if (isCaster) dumpEv.InvolvedNetIds.Add(fx.CasterNetID);
                                        if (isTarget) dumpEv.InvolvedNetIds.Add(fx.TargetNetID);
                                    }
                                }
                            }
                        }
                        else if (packetId == 0xB0 && packet.ParsedData is S2C_PlayAnimationData animData && targetNetIds.Contains(animData.RoutingNetID))
                        {
                            string actorName = GetEntityName(animData.RoutingNetID, netIdToName);
                            dumpEv = new DumpEvent { TimeStr = timeStr, ParsedData = animData, Category = LogCategory.Animation, Color = Brushes.MediumPurple, LogText = $"[{timeStr}] ANIMATION PLAYED: {animData.AnimationName} on {actorName}" };
                            dumpEv.InvolvedNetIds.Add(animData.RoutingNetID);
                        }
                        else if (packetId == 0x10F && packet.ParsedData is S2C_UnitSetLookAtData lookAtData)
                        {
                            bool isActor = targetNetIds.Contains(lookAtData.RoutingNetID);
                            bool isTarget = includeTargeted && targetNetIds.Contains(lookAtData.TargetNetID);

                            if (isActor || isTarget)
                            {
                                string actorName = GetEntityName(lookAtData.RoutingNetID, netIdToName);
                                string targetName = GetEntityName(lookAtData.TargetNetID, netIdToName);
                                dumpEv = new DumpEvent { TimeStr = timeStr, ParsedData = lookAtData, Category = LogCategory.LookAt, Color = Brushes.LightSeaGreen, LogText = $"[{timeStr}] UNIT LOOK AT: {actorName} -> {targetName}" };
                                if (isActor) dumpEv.InvolvedNetIds.Add(lookAtData.RoutingNetID);
                                if (isTarget) dumpEv.InvolvedNetIds.Add(lookAtData.TargetNetID);
                            }
                        }

                        if (dumpEv != null)
                        {
                            allDumpEvents.Add(dumpEv);
                        }
                    }

                    if (allDumpEvents.Count == 0)
                    {
                        LogMessage("No events found for this target.", Brushes.Orange);
                        return;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        var mainExpander = new Expander { Header = $"ALL EVENTS FOR {target} ({allDumpEvents.Count} total)", Foreground = Brushes.White, IsExpanded = true, Margin = new Thickness(0, 5, 0, 5), FontFamily = new FontFamily("Consolas") };
                        var mainPanel = new StackPanel { Margin = new Thickness(20, 5, 0, 5) };

                        var comboPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
                        comboPanel.Children.Add(new TextBlock { Text = "Perspective: ", Foreground = Brushes.LightGray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) });

                        var perspectiveCombo = new ComboBox { Width = 300 };
                        perspectiveCombo.Items.Add(new ComboBoxItem { Content = "All Instances", Tag = 0u });

                        var activeNetIds = allDumpEvents.SelectMany(e => e.InvolvedNetIds).Distinct().Take(maxEntities).ToList();
                        foreach (var netId in activeNetIds)
                        {
                            perspectiveCombo.Items.Add(new ComboBoxItem { Content = GetEntityName(netId, netIdToName), Tag = netId });
                        }
                        perspectiveCombo.SelectedIndex = 0;
                        comboPanel.Children.Add(perspectiveCombo);
                        mainPanel.Children.Add(comboPanel);

                        var logsPanel = new StackPanel();
                        mainPanel.Children.Add(logsPanel);
                        mainExpander.Content = mainPanel;
                        OutputStack.Children.Add(mainExpander);

                        Action renderLogs = () =>
                        {
                            logsPanel.Children.Clear();
                            uint selectedNetId = (uint)((ComboBoxItem)perspectiveCombo.SelectedItem).Tag;

                            var filtered = selectedNetId == 0 ? allDumpEvents : allDumpEvents.Where(e => e.InvolvedNetIds.Contains(selectedNetId));

                            foreach (var ev in filtered)
                            {
                                AddSubLog(logsPanel, ev.LogText, ev.Color, ev.ParsedData, ev.Category);
                            }
                        };

                        perspectiveCombo.SelectionChanged += (s, e) => renderLogs();
                        renderLogs();

                        LogMessage("Dump Complete!", Brushes.LimeGreen);
                    });
                }
                catch (Exception ex) { LogMessage($"Error: {ex.Message}", Brushes.Red); }
            });
        }
    }
}