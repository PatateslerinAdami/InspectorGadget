using Microsoft.Win32;
using ReplayFiles.Core;
using ReplayFiles.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text.Json.Serialization;

namespace InspectorGadget
{
    public class AppSettings
    {
        public string ReplaysFolder { get; set; } = "";
        public string CsvFolder { get; set; } = "";
    }

    public partial class MainWindow : Window
    {
        private Dictionary<uint, string> _spellHashes = new Dictionary<uint, string>();
        private Dictionary<uint, string> _buffHashes = new Dictionary<uint, string>();
        private Dictionary<uint, string> _vfxHashes = new Dictionary<uint, string>();

        private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };
        private readonly string _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

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

        private void LogMessage(string message, Brush color = null)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var tb = new TextBlock { Text = message, Foreground = color ?? Brushes.LightGray, Margin = new Thickness(0, 2, 0, 2), FontFamily = new FontFamily("Consolas") };
                OutputStack.Children.Add(tb);
            });
        }

        private StackPanel AddLogExpander(string header, Brush color, object rawData)
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
                OutputStack.Children.Add(expander);
            });
            return innerPanel;
        }

        private void AddSubLog(StackPanel parentPanel, string text, Brush color, object rawData)
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
                parentPanel.Children.Add(expander);
            });
        }

        private void AddFileHeaderWithButton(string fileName, string filePath, string targetChampion)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 5) };
                var tb = new TextBlock { Text = $"[FILE] {fileName}", Foreground = Brushes.White, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };

                var btn = new Button { Content = $"Show ALL Events for {targetChampion}", Padding = new Thickness(5, 2, 5, 2), Background = Brushes.DarkSlateBlue, Foreground = Brushes.White };
                btn.Click += (s, e) => ShowAllChampionEvents(filePath, targetChampion);

                panel.Children.Add(tb);
                if (!string.IsNullOrEmpty(targetChampion)) panel.Children.Add(btn);

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

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = FolderPathTextBox.Text;
            string spellName = SpellNameTextBox.Text.Trim();
            string targetChampion = ChampionTextBox.Text.Trim();

            if (string.IsNullOrEmpty(folderPath)) return;
            if (string.IsNullOrEmpty(spellName) && string.IsNullOrEmpty(targetChampion))
            {
                LogMessage("Please enter either a Champion or a Spell Name!", Brushes.Red);
                return;
            }

            if (!string.IsNullOrEmpty(CsvPathTextBox.Text)) LoadCsvHashes(CsvPathTextBox.Text);

            float trackDuration = float.Parse(TrackTimeTextBox.Text) * 1000f;
            int maxOccurrences = int.Parse(MaxOccurrencesTextBox.Text);
            uint targetHash = string.IsNullOrEmpty(spellName) ? 0 : HashString(spellName);

            ScanButton.IsEnabled = false;
            OutputStack.Children.Clear();

            if (targetHash != 0) LogMessage($"Searching for Spell: '{spellName}' (Hash: 0x{targetHash:X8})", Brushes.Cyan);
            else LogMessage($"Discovery Mode: Tracking ALL spells cast by '{targetChampion}'", Brushes.Cyan);

            await Task.Run(() =>
            {
                int occurrencesFound = 0;
                var files = Directory.EnumerateFiles(folderPath, "*.lrf", SearchOption.AllDirectories).ToList();
                LogMessage($"Found {files.Count} total .lrf files. Starting scan...\n", Brushes.White);

                Dictionary<uint, string> discoveredSpells = new Dictionary<uint, string>();

                for (int i = 0; i < files.Count; i++)
                {
                    if (occurrencesFound >= maxOccurrences) break;

                    string file = files[i];
                    if (!string.IsNullOrEmpty(targetChampion))
                    {
                        var metadata = ReadMetadataFast(file);
                        if (metadata == null || metadata.Players == null) continue;
                        if (!metadata.Players.Any(p => p.Champion.Replace(" ", "").Equals(targetChampion.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))) continue;
                    }

                    AddFileHeaderWithButton(Path.GetFileName(file), file, targetChampion);

                    try
                    {
                        using FileStream stream = File.OpenRead(file);
                        var packets = new ReplayReader().Parse(stream).ToList();

                        float gameStartTime = 0;
                        foreach (var p in packets)
                        {
                            if (p.Payload.Length > 0 && p.Payload.Span[0] == 0x5C)
                            {
                                gameStartTime = p.Time;
                                break;
                            }
                        }

                        var activeTrackers = new List<(float EndTimeMs, uint CasterNetId, StackPanel UIContainer)>();
                        uint targetChampionNetId = 0;

                        foreach (var packet in packets)
                        {
                            if (packet.Payload.Length == 0) continue;
                            byte packetId = packet.Payload.Span[0];
                            float realTimeMs = packet.Time - gameStartTime;
                            string timeStr = FormatTime(realTimeMs);

                            if (packetId == 0x4C && packet.ParsedData is CreateHeroData heroData)
                            {
                                if (!string.IsNullOrEmpty(targetChampion) && heroData.Skin.Replace(" ", "").Equals(targetChampion.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                                    targetChampionNetId = heroData.NetID;
                            }

                            if (packetId == 0xB5 && packet.ParsedData is NPC_CastSpellAnsData castData)
                            {
                                bool isMatch = (targetHash != 0 && castData.SpellHash == targetHash) ||
                                               (targetHash == 0 && targetChampionNetId != 0 && castData.CasterNetID == targetChampionNetId);

                                if (isMatch)
                                {
                                    occurrencesFound++;
                                    string castedSpellName = discoveredSpells.TryGetValue(castData.SpellHash, out string knownName) ? knownName : GetNameFromHash(castData.SpellHash, _spellHashes);

                                    string header = $"[{timeStr}] [OCCURRENCE #{occurrencesFound}] Spell '{castedSpellName}' casted!";
                                    var uiContainer = AddLogExpander(header, Brushes.Yellow, castData);

                                    activeTrackers.Add((realTimeMs + trackDuration, castData.CasterNetID, uiContainer));
                                }
                            }
                            else if (packetId == 0x3B && packet.ParsedData is MissileReplicationData misData)
                            {
                                bool isMatch = (targetHash != 0 && misData.SpellHash == targetHash) ||
                                               (targetHash == 0 && targetChampionNetId != 0 && misData.CasterNetID == targetChampionNetId);

                                if (isMatch)
                                {
                                    occurrencesFound++;
                                    string castedSpellName = discoveredSpells.TryGetValue(misData.SpellHash, out string knownName) ? knownName : GetNameFromHash(misData.SpellHash, _spellHashes);

                                    string header = $"[{timeStr}] [OCCURRENCE #{occurrencesFound}] Missile '{castedSpellName}' spawned!";
                                    var uiContainer = AddLogExpander(header, Brushes.Orange, misData);

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
                                        if (buffData.CasterNetID == tracker.CasterNetId || buffData.RoutingNetID == tracker.CasterNetId)
                                        {
                                            string buffName = GetNameFromHash(buffData.BuffNameHash, _buffHashes);
                                            AddSubLog(tracker.UIContainer, $"[+{timeOffsetMs:0}ms] BUFF ADDED: {buffName}", Brushes.LightPink, buffData);
                                            if (!discoveredSpells.ContainsKey(buffData.BuffNameHash) && !buffName.StartsWith("0x")) discoveredSpells[buffData.BuffNameHash] = buffName;
                                        }
                                    }
                                    else if (packetId == 0x87 && packet.ParsedData is FXCreateGroupPacketData fxData)
                                    {
                                        foreach (var group in fxData.Groups)
                                        {
                                            foreach (var fx in group.FXCreateData)
                                            {
                                                if (fx.TargetNetID == tracker.CasterNetId || fx.CasterNetID == tracker.CasterNetId)
                                                {
                                                    string vfxName = GetNameFromHash(group.EffectNameHash, _vfxHashes);
                                                    AddSubLog(tracker.UIContainer, $"[+{timeOffsetMs:0}ms] VFX SPAWNED: {vfxName}", Brushes.LightSkyBlue, group);
                                                    if (!discoveredSpells.ContainsKey(group.EffectNameHash) && !vfxName.StartsWith("0x")) discoveredSpells[group.EffectNameHash] = vfxName.Replace(".troy", "");
                                                }
                                            }
                                        }
                                    }
                                    else if (packetId == 0x3B && packet.ParsedData is MissileReplicationData subMisData)
                                    {
                                        if (subMisData.CasterNetID == tracker.CasterNetId)
                                        {
                                            string misName = GetNameFromHash(subMisData.SpellHash, _spellHashes);
                                            AddSubLog(tracker.UIContainer, $"[+{timeOffsetMs:0}ms] MISSILE SPAWNED: {misName}", Brushes.Orange, subMisData);
                                        }
                                    }
                                    else if (packetId == 0xB0 && packet.ParsedData is S2C_PlayAnimationData animData && animData.RoutingNetID == tracker.CasterNetId)
                                    {
                                        AddSubLog(tracker.UIContainer, $"[+{timeOffsetMs:0}ms] ANIMATION PLAYED: {animData.AnimationName}", Brushes.MediumPurple, animData);
                                    }
                                    else if (packetId == 0x29 && packet.ParsedData is S2C_StopAnimationData stopAnimData && stopAnimData.RoutingNetID == tracker.CasterNetId)
                                    {
                                        AddSubLog(tracker.UIContainer, $"[+{timeOffsetMs:0}ms] ANIMATION STOPPED: {stopAnimData.AnimationName}", Brushes.MediumPurple, stopAnimData);
                                    }
                                    else if (packetId == 0x6B && packet.ParsedData is S2C_SetAnimStatesData animStateData && animStateData.RoutingNetID == tracker.CasterNetId)
                                    {
                                        AddSubLog(tracker.UIContainer, $"[+{timeOffsetMs:0}ms] ANIMATION STATE OVERRIDE", Brushes.Plum, animStateData);
                                    }
                                    else if (packetId == 0x10F && packet.ParsedData is S2C_UnitSetLookAtData lookAtData && lookAtData.RoutingNetID == tracker.CasterNetId)
                                    {
                                        AddSubLog(tracker.UIContainer, $"[+{timeOffsetMs:0}ms] UNIT LOOK AT", Brushes.LightSeaGreen, lookAtData);
                                    }
                                    else if (packetId == 0x6C && packet.ParsedData is S2C_ChainMissileSyncData chainData && chainData.OwnerNetworkID == tracker.CasterNetId)
                                    {
                                        AddSubLog(tracker.UIContainer, $"[+{timeOffsetMs:0}ms] CHAIN MISSILE SYNC", Brushes.Gold, chainData);
                                    }
                                }
                            }

                            if (occurrencesFound >= maxOccurrences && activeTrackers.Count == 0) break;
                        }
                    }
                    catch { /* Skip corrupted files */ }
                }

                LogMessage("\nScan Complete!", Brushes.LimeGreen);
            });

            ScanButton.IsEnabled = true;
        }

        private async void ShowAllChampionEvents(string filePath, string targetChampion)
        {
            OutputStack.Children.Clear();
            LogMessage($"Dumping ALL events for {targetChampion} in {Path.GetFileName(filePath)}...", Brushes.Cyan);

            await Task.Run(() =>
            {
                try
                {
                    using FileStream stream = File.OpenRead(filePath);
                    var packets = new ReplayReader().Parse(stream).ToList();

                    float gameStartTime = 0;
                    foreach (var p in packets)
                    {
                        if (p.Payload.Length > 0 && p.Payload.Span[0] == 0x5C)
                        {
                            gameStartTime = p.Time;
                            break;
                        }
                    }

                    uint targetChampionNetId = 0;
                    var mainContainer = AddLogExpander($"ALL EVENTS FOR {targetChampion}", Brushes.White, new { Info = "Click to expand all events" });

                    foreach (var packet in packets)
                    {
                        if (packet.Payload.Length == 0) continue;
                        byte packetId = packet.Payload.Span[0];
                        float realTimeMs = packet.Time - gameStartTime;
                        string timeStr = FormatTime(realTimeMs);

                        if (packetId == 0x4C && packet.ParsedData is CreateHeroData heroData)
                        {
                            if (heroData.Skin.Replace(" ", "").Equals(targetChampion.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                                targetChampionNetId = heroData.NetID;
                        }

                        if (targetChampionNetId == 0) continue;

                        if (packetId == 0xB5 && packet.ParsedData is NPC_CastSpellAnsData castData && castData.CasterNetID == targetChampionNetId)
                        {
                            string name = GetNameFromHash(castData.SpellHash, _spellHashes);
                            AddSubLog(mainContainer, $"[{timeStr}] SPELL CAST: {name}", Brushes.Yellow, castData);
                        }
                        else if (packetId == 0x3B && packet.ParsedData is MissileReplicationData misData && misData.CasterNetID == targetChampionNetId)
                        {
                            string name = GetNameFromHash(misData.SpellHash, _spellHashes);
                            AddSubLog(mainContainer, $"[{timeStr}] MISSILE: {name}", Brushes.Orange, misData);
                        }
                        else if (packetId == 0xB7 && packet.ParsedData is NPC_BuffAdd2Data buffData && (buffData.CasterNetID == targetChampionNetId || buffData.RoutingNetID == targetChampionNetId))
                        {
                            string name = GetNameFromHash(buffData.BuffNameHash, _buffHashes);
                            AddSubLog(mainContainer, $"[{timeStr}] BUFF ADDED: {name}", Brushes.LightPink, buffData);
                        }
                        else if (packetId == 0x87 && packet.ParsedData is FXCreateGroupPacketData fxData)
                        {
                            foreach (var group in fxData.Groups)
                            {
                                foreach (var fx in group.FXCreateData)
                                {
                                    if (fx.TargetNetID == targetChampionNetId || fx.CasterNetID == targetChampionNetId)
                                    {
                                        string name = GetNameFromHash(group.EffectNameHash, _vfxHashes);
                                        AddSubLog(mainContainer, $"[{timeStr}] VFX SPAWNED: {name}", Brushes.LightSkyBlue, group);
                                    }
                                }
                            }
                        }

                        else if (packetId == 0xB0 && packet.ParsedData is S2C_PlayAnimationData animData && animData.RoutingNetID == targetChampionNetId)
                        {
                            AddSubLog(mainContainer, $"[{timeStr}] ANIMATION PLAYED: {animData.AnimationName}", Brushes.MediumPurple, animData);
                        }
                        else if (packetId == 0x29 && packet.ParsedData is S2C_StopAnimationData stopAnimData && stopAnimData.RoutingNetID == targetChampionNetId)
                        {
                            AddSubLog(mainContainer, $"[{timeStr}] ANIMATION STOPPED: {stopAnimData.AnimationName}", Brushes.MediumPurple, stopAnimData);
                        }
                        else if (packetId == 0x6B && packet.ParsedData is S2C_SetAnimStatesData animStateData && animStateData.RoutingNetID == targetChampionNetId)
                        {
                            AddSubLog(mainContainer, $"[{timeStr}] ANIMATION STATE OVERRIDE", Brushes.Plum, animStateData);
                        }
                        else if (packetId == 0x10F && packet.ParsedData is S2C_UnitSetLookAtData lookAtData && lookAtData.RoutingNetID == targetChampionNetId)
                        {
                            AddSubLog(mainContainer, $"[{timeStr}] UNIT LOOK AT", Brushes.LightSeaGreen, lookAtData);
                        }
                        else if (packetId == 0x6C && packet.ParsedData is S2C_ChainMissileSyncData chainData && chainData.OwnerNetworkID == targetChampionNetId)
                        {
                            AddSubLog(mainContainer, $"[{timeStr}] CHAIN MISSILE SYNC", Brushes.Gold, chainData);
                        }
                    }
                    LogMessage("Dump Complete!", Brushes.LimeGreen);
                }
                catch (Exception ex) { LogMessage($"Error: {ex.Message}", Brushes.Red); }
            });
        }
    }
}