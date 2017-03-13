namespace BotwTrainer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;

    using BotwTrainer.Properties;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow
    {
        // The original list of values that take effect when you save / load
        private const uint SaveItemStart = 0x3FCE7FF0;

        // Technically your first item as they are stored in reverse so we work backwards
        private const uint ItemEnd = 0x43CA2AEC;

        private const uint ItemStart = 0x43C6B2AC;

        private const uint CodeHandlerStart = 0x01133000;

        private const uint CodeHandlerEnd = 0x01134300;

        private const uint CodeHandlerEnabled = 0x10014CFC;

        private readonly List<Item> items;

        private readonly JToken json;

        private readonly string version;

        private readonly List<TextBox> tbChanged = new List<TextBox>();

        private readonly List<ComboBox> ddChanged = new List<ComboBox>();

        private readonly List<CheckBox> cbChanged = new List<CheckBox>();

        private TCPGecko tcpGecko;

        private DispatcherTimer timer;

        private TimeSpan time;

        private int itemsFound;

        private bool connected;

        public MainWindow()
        {
            this.InitializeComponent();

            this.Loaded += this.MainWindowLoaded;

            IpAddress.Text = Settings.Default.IpAddress;
            this.version = Settings.Default.CurrentVersion;

            this.Title = string.Format("{0} v{1}", this.Title, this.version);

            var client = new WebClient
                             {
                                 BaseAddress = Settings.Default.VersionUrl,
                                 Encoding = Encoding.UTF8,
                                 CachePolicy =
                                     new System.Net.Cache.RequestCachePolicy(
                                     System.Net.Cache.RequestCacheLevel.BypassCache)
                             };

            client.Headers.Add("Cache-Control", "no-cache");
            client.DownloadStringCompleted += this.ClientDownloadStringCompleted;

            client.DownloadStringAsync(new Uri(string.Format("{0}{1}", client.BaseAddress, "version.txt")));

            this.items = new List<Item>();

            try
            {
                var file = Assembly.GetExecutingAssembly().GetManifestResourceStream("BotwTrainer.items.json");
                using (var reader = new StreamReader(file))
                {
                    var data = reader.ReadToEnd();
                    this.json = JObject.Parse(data);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error loading json");
            }
        }

        private enum Cheat
        {
            Stamina = 0,
            Health = 1,
            Run = 2,
            Rupees = 3,
            MoonJump = 4,
            WeaponInv = 5,
            BowInv = 6,
            ShieldInv = 7,
            Speed = 8,
            Mon = 9,
            Urbosa = 10,
            Revali = 11,
            Daruk = 12
        }

        private bool HasChanged
        {
            get
            {
                return this.tbChanged.Any() || this.cbChanged.Any() || this.ddChanged.Any();
            }
        }

        private bool LoadDataAsync()
        {
            try
            {
                var x = 0;

                var currentItemAddress = ItemEnd;

                while (currentItemAddress >= ItemStart)
                {
                    // Skip FFFFFFFF invalild items. Usuauly end of the list
                    var page = this.tcpGecko.peek(currentItemAddress);
                    if (page > 9)
                    {
                        var percent = (100m / 418m) * x;
                        Dispatcher.Invoke(
                            () =>
                                {
                                    ProgressText.Text = string.Format("{0}/{1}", x, 418);
                                    this.UpdateProgress(Convert.ToInt32(percent));
                                });

                        currentItemAddress -= 0x220;
                        x++;

                        continue;
                    }

                    // Dump each item memory block
                    var stream = new MemoryStream();
                    this.tcpGecko.Dump(currentItemAddress, currentItemAddress + 0x70, stream);

                    var unknown = this.ReadStream(stream, 4);
                    var value = this.ReadStream(stream, 8);
                    var equipped = this.ReadStream(stream, 12);
                    var nameStart = currentItemAddress + 0x1C;

                    stream.Seek(28, SeekOrigin.Begin);
                    var builder = new StringBuilder();
                    for (var i = 0; i < 36; i++)
                    {
                        var data = stream.ReadByte();
                        if (data == 0)
                        {
                            break;
                        }

                        builder.Append((char)data);
                    }

                    var id = builder.ToString();
                    
                    var item = new Item
                                   {
                                       BaseAddress = currentItemAddress,
                                       Page = Convert.ToInt32(page),
                                       Unknown = Convert.ToInt32(unknown),
                                       Value = value,
                                       Equipped = equipped,
                                       NameStart = nameStart,
                                       Id = id,
                                       Modifier1Value = this.ReadStream(stream, 92).ToString("x8").ToUpper(),
                                       Modifier2Value = this.ReadStream(stream, 96).ToString("x8").ToUpper(),
                                       Modifier3Value = this.ReadStream(stream, 100).ToString("x8").ToUpper(),
                                       Modifier4Value = this.ReadStream(stream, 104).ToString("x8").ToUpper(),
                                       Modifier5Value = this.ReadStream(stream, 108).ToString("x8").ToUpper()
                                   };

                    // look for name in json
                    var name = this.GetNameFromId(item.Id, item.PageName);
                    item.Name = name;

                    this.items.Add(item);

                    var currentPercent = (100m / 418m) * x;
                    Dispatcher.Invoke(
                        () =>
                            {
                                ProgressText.Text = string.Format("{0}/{1}", x, 418);
                                this.UpdateProgress(Convert.ToInt32(currentPercent));
                            });

                    currentItemAddress -= 0x220;
                    x++;
                }

                this.itemsFound = this.items.Count;

                return true;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => this.LogError(ex));
                return false;
            }
        }

        private async void LoadClick(object sender, RoutedEventArgs e)
        {
            this.ToggleControls("Load");

            this.items.Clear();

            // talk to wii u and get mem dump of data
            var result = await Task.Run(() => this.LoadDataAsync());

            if (result)
            {
                this.DebugData();

                this.LoadTab(this.Weapons, 0);
                this.LoadTab(this.Bows, 1);
                this.LoadTab(this.Arrows, 2);
                this.LoadTab(this.Shields, 3);
                this.LoadTab(this.Armor, 4);
                this.LoadTab(this.Materials, 7);
                this.LoadTab(this.Food, 8);
                this.LoadTab(this.KeyItems, 9);

                // Code Tab Values
                CurrentStamina.Text = this.tcpGecko.peek(0x42439598).ToString("x8").ToUpper();
                var healthPointer = this.tcpGecko.peek(0x4225B4B0);
                CurrentHealth.Text = this.tcpGecko.peek(healthPointer + 0x430).ToString(CultureInfo.InvariantCulture);
                CurrentRupees.Text = this.tcpGecko.peek(0x4010AA0C).ToString(CultureInfo.InvariantCulture);
                CurrentMon.Text = this.tcpGecko.peek(0x4010B14C).ToString(CultureInfo.InvariantCulture);
                CbSpeed.SelectedValue = this.tcpGecko.peek(0x439BF514).ToString("X").ToUpper();
                CurrentWeaponSlots.Text = this.tcpGecko.peek(0x3FCFB498).ToString(CultureInfo.InvariantCulture);
                CurrentBowSlots.Text = this.tcpGecko.peek(0x3FD4BB50).ToString(CultureInfo.InvariantCulture);
                CurrentShieldSlots.Text = this.tcpGecko.peek(0x3FCC0B40).ToString(CultureInfo.InvariantCulture);
                /*
                CurrentUrbosa.Text = this.tcpGecko.peek(0x3FCFFA80).ToString(CultureInfo.InvariantCulture);
                CurrentRevali.Text = this.tcpGecko.peek(0x3FD5ED90).ToString(CultureInfo.InvariantCulture);
                CurrentDaruk.Text = this.tcpGecko.peek(0x3FD50088).ToString(CultureInfo.InvariantCulture);
                 */

                this.Notification.Content = string.Format("Items found: {0}", this.itemsFound);

                this.ToggleControls("DataLoaded");

                this.cbChanged.Clear();
                this.tbChanged.Clear();
                this.ddChanged.Clear();

                this.Save.IsEnabled = this.HasChanged;
            }
        }

        private void ConnectClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // cause error to test
                //var foundTextBox = (TextBox)this.FindName("Item_123");
                //foundTextBox.Text = "error";

                this.tcpGecko = new TCPGecko(this.IpAddress.Text, 7331);

                this.connected = this.tcpGecko.Connect();

                if (this.connected)
                {
                    // Saved settings stuff
                    var shown = Settings.Default.Warning;

                    if (shown < 3)
                    {
                        Settings.Default.Warning++;

                        //MessageBox.Show("WARNING: Item names are now editable. Using bad data may mess up your game so use with care.");
                    }

                    Settings.Default.IpAddress = IpAddress.Text;
                    Settings.Default.Save();

                    Controller.SelectedValue = Settings.Default.Controller;

                    this.ToggleControls("Connected");
                }
            }
            catch (ETCPGeckoException ex)
            {
                this.connected = false;

                MessageBox.Show(ex.Message);
            }
            catch (System.Net.Sockets.SocketException)
            {
                this.connected = false;

                MessageBox.Show("Wrong IP");
            }
            catch (Exception ex)
            {
                this.LogError(ex);
            }
        }

        private void DisconnectClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.connected = false;
                this.tcpGecko.Disconnect();

                this.ToggleControls("Disconnected");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            // Clear old errors
            ErrorLog.Document.Blocks.Clear();

            // Grab the values from the relevant tab and poke them back to memory
            var tab = (TabItem)TabControl.SelectedItem;

            if (!this.HasChanged)
            {
                // Nothing to update
                MessageBox.Show("No changes have been made");
                return;
            }

            try
            {
                // For these we amend the 0x3FCE7FF0 area which requires save/load
                if (Equals(tab, this.Weapons) || Equals(tab, this.Bows) || Equals(tab, this.Shields)
                    || Equals(tab, this.Armor))
                {
                    var weaponList = this.items.Where(x => x.Page == 0).ToList();
                    var bowList = this.items.Where(x => x.Page == 1).ToList();
                    var arrowList = this.items.Where(x => x.Page == 2).ToList();
                    var shieldList = this.items.Where(x => x.Page == 3).ToList();
                    var armorList = this.items.Where(x => x.Page == 4 || x.Page == 5 || x.Page == 6).ToList();

                    var y = 0;
                    if (Equals(tab, this.Weapons))
                    {
                        foreach (var item in weaponList)
                        {
                            var foundTextBox = (TextBox)this.FindName("Value_" + item.ValueAddressHex);
                            if (foundTextBox != null)
                            {
                                var offset = (uint)(SaveItemStart + (y * 0x8));
                                this.tcpGecko.poke32(offset, Convert.ToUInt32(foundTextBox.Text));
                            }

                            y++;
                        }
                    }

                    if (Equals(tab, this.Bows))
                    {
                        // jump past weapons before we start
                        y += weaponList.Count;

                        foreach (var item in bowList)
                        {
                            var foundTextBox = (TextBox)this.FindName("Value_" + item.ValueAddressHex);
                            if (foundTextBox != null)
                            {
                                var offset = (uint)(SaveItemStart + (y * 0x8));

                                this.tcpGecko.poke32(offset, Convert.ToUInt32(foundTextBox.Text));
                            }

                            y++;
                        }
                    }

                    if (Equals(tab, this.Shields))
                    {
                        // jump past weapons/bows/arrows before we start
                        y += weaponList.Count + bowList.Count + arrowList.Count;

                        foreach (var item in shieldList)
                        {
                            var foundTextBox = (TextBox)this.FindName("Value_" + item.ValueAddressHex);
                            if (foundTextBox != null)
                            {
                                var offset = (uint)(SaveItemStart + (y * 0x8));

                                this.tcpGecko.poke32(offset, Convert.ToUInt32(foundTextBox.Text));
                            }

                            y++;
                        }
                    }

                    if (Equals(tab, this.Armor))
                    {
                        // jump past weapons/bows/arrows/shields before we start
                        y += weaponList.Count + bowList.Count + arrowList.Count + shieldList.Count;

                        foreach (var item in armorList)
                        {
                            var offset = (uint)(SaveItemStart + (y * 0x8));

                            var foundTextBox = (TextBox)this.FindName("Value_" + item.ValueAddressHex);
                            if (foundTextBox != null)
                            {
                                this.tcpGecko.poke32(offset, Convert.ToUInt32(foundTextBox.Text));
                            }

                            y++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Attempting to save data in 0x3FCE7FF0 region.");
            }

            try
            {
                // Only update what has changed to avoid corruption.
                foreach (var tb in this.tbChanged)
                {
                    // These text boxes have been edited
                    var type = tb.Name.Split('_')[0];
                    var tag = tb.Tag;

                    if (type == "Id")
                    {
                        var newName = Encoding.Default.GetBytes(tb.Text);

                        var add = uint.Parse(tag.ToString(), NumberStyles.HexNumber);
                        var thisItem = this.items.Single(i => i.NameStart == add);

                        //clear current name
                        this.tcpGecko.poke32(add, 0x0);
                        this.tcpGecko.poke32(add + 0x4, 0x0);
                        this.tcpGecko.poke32(add + 0x8, 0x0);
                        this.tcpGecko.poke32(add + 0xC, 0x0);
                        this.tcpGecko.poke32(add + 0x10, 0x0);
                        this.tcpGecko.poke32(add + 0x14, 0x0);

                        uint x = 0x0;
                        foreach (var b in newName)
                        {
                            this.tcpGecko.poke08(add + x, b);
                            x = x + 0x1;
                        }

                        thisItem.Id = tb.Text;

                        // Name
                        var foundTextBox = (TextBox)this.FindName("JsonName_" + tag);
                        if (foundTextBox != null)
                        {
                            foundTextBox.Text = this.GetNameFromId(thisItem.Id, thisItem.PageName);
                        }
                    }

                    if (type == "Value")
                    {
                        var address = uint.Parse(tag.ToString(), NumberStyles.HexNumber);
                        int val;
                        bool parsed = int.TryParse(tb.Text, out val);
                        if (parsed)
                        {
                            this.tcpGecko.poke32(address, Convert.ToUInt32(val));
                        }
                    }

                    if (type == "Mod")
                    {
                        var address = uint.Parse(tag.ToString(), NumberStyles.HexNumber);
                        uint val;
                        bool parsed = uint.TryParse(tb.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out val);
                        if (parsed)
                        {
                            this.tcpGecko.poke32(address, val);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Attempting to update changed fields");
            }


            try
            {
                // For the 'Codes' tab we mimic JGecko and send cheats to codehandler
                if (Equals(tab, this.Codes))
                {
                    var selected = new List<Cheat>();

                    if (Stamina.IsChecked == true)
                    {
                        selected.Add(Cheat.Stamina);
                    }

                    if (Health.IsChecked == true)
                    {
                        selected.Add(Cheat.Health);
                    }

                    if (Rupees.IsChecked == true)
                    {
                        selected.Add(Cheat.Rupees);
                    }

                    if (Mon.IsChecked == true)
                    {
                        selected.Add(Cheat.Mon);
                    }

                    if (Run.IsChecked == true)
                    {
                        selected.Add(Cheat.Run);
                    }

                    if (Speed.IsChecked == true)
                    {
                        selected.Add(Cheat.Speed);
                    }

                    if (MoonJump.IsChecked == true)
                    {
                        selected.Add(Cheat.MoonJump);
                    }

                    if (WeaponSlots.IsChecked == true)
                    {
                        selected.Add(Cheat.WeaponInv);
                    }

                    if (BowSlots.IsChecked == true)
                    {
                        selected.Add(Cheat.BowInv);
                    }

                    if (ShieldSlots.IsChecked == true)
                    {
                        selected.Add(Cheat.ShieldInv);
                    }

                    /*
                    if (Urbosa.IsChecked == true)
                    {
                        selected.Add(Cheat.Urbosa);
                    }

                    if (Revali.IsChecked == true)
                    {
                        selected.Add(Cheat.Revali);
                    }

                    if (Daruk.IsChecked == true)
                    {
                        selected.Add(Cheat.Daruk);
                    }
                    */

                    this.SetCheats(selected);

                    Settings.Default.Controller = Controller.SelectedValue.ToString();
                    Settings.Default.Save();
                }

                this.DebugData();
                Debug.UpdateLayout();

                // clear changed after save
                this.tbChanged.Clear();
                this.cbChanged.Clear();
                this.ddChanged.Clear();

                this.Save.IsEnabled = false;
            }
            catch (Exception ex)
            {
                this.LogError(ex);
            }
        }

        private void ExportClick(object sender, RoutedEventArgs e)
        {
            this.ExportToExcel();
        }

        private void LoadTab(ContentControl tab, int page)
        {
            var scroll = new ScrollViewer { Name = "ScrollContent", Margin = new Thickness(10), VerticalAlignment = VerticalAlignment.Top };

            var holder = new WrapPanel { Margin = new Thickness(0), VerticalAlignment = VerticalAlignment.Top };

            // setup grid
            var grid = this.GenerateTabGrid(tab.Name);

            var x = 1;
            var list = this.items.Where(i => i.Page == page).OrderByDescending(i => i.BaseAddress);

            if (page == 4)
            {
                list = this.items.Where(i => i.Page == 4 || i.Page == 5 || i.Page == 6).OrderByDescending(i => i.BaseAddress);
            }

            foreach (var item in list)
            {
                grid.RowDefinitions.Add(new RowDefinition());

                // Name - Readonly data
                var name = new TextBox
                {
                    Text = item.Name,
                    Margin = new Thickness(0),
                    BorderThickness = new Thickness(0),
                    Height = 22,
                    Width = 190,
                    IsReadOnly = true,
                    Name = "JsonName_" + item.NameStartHex
                };

                // we register the name so we can update it later without having to refresh
                var check = (TextBox)this.FindName("JsonName_" + item.NameStartHex);
                if (check != null)
                {
                    this.UnregisterName("JsonName_" + item.NameStartHex);
                }

                this.RegisterName("JsonName_" + item.NameStartHex, name);
                

                // Id
                var id = new TextBox
                {
                    Text = item.Id,
                    Tag = item.NameStartHex,
                    ToolTip = item.NameStartHex,
                    Margin = new Thickness(0), 
                    Height = 22, 
                    Width = 130,
                    IsReadOnly = false,
                    Name = "Id_" + item.NameStartHex
                };

                id.TextChanged += this.TextChanged;

                check = (TextBox)this.FindName("Id_" + item.NameStartHex);
                if (check != null)
                {
                    this.UnregisterName("Id_" + item.NameStartHex);
                }

                this.RegisterName("Id_" + item.NameStartHex, id);

                // Current item is red
                if (item.EquippedBool)
                {
                    id.Foreground = Brushes.Red;
                    name.Foreground = Brushes.Red;
                }

                // add first 2 fields
                Grid.SetRow(name, x);
                Grid.SetColumn(name, 0);
                grid.Children.Add(name);

                Grid.SetRow(id, x);
                Grid.SetColumn(id, 1);
                grid.Children.Add(id);

                // Value to 0 if its FFFFF etc
                var value = item.Value;
                if (value > int.MaxValue)
                {
                    value = 0;
                }

                var val = this.GenerateGridTextBox(value.ToString(), item.ValueAddressHex, "Value_", x, 2, 70);
                val.PreviewTextInput += this.NumberValidationTextBox;
                grid.Children.Add(val);

                // Mod1
                var mtb1 = this.GenerateGridTextBox(item.Modifier1Value, item.Modifier1Address, "Mod_", x, 3, 70);
                grid.Children.Add(mtb1);

                // Mod2
                var mtb2 = this.GenerateGridTextBox(item.Modifier2Value, item.Modifier2Address, "Mod_", x, 4, 70);
                grid.Children.Add(mtb2);

                // Mod3s
                var mtb3 = this.GenerateGridTextBox(item.Modifier3Value, item.Modifier3Address, "Mod_", x, 5, 70);
                grid.Children.Add(mtb3);

                // Mod4
                var mtb4 = this.GenerateGridTextBox(item.Modifier4Value, item.Modifier4Address, "Mod_", x, 6, 70);
                grid.Children.Add(mtb4);

                // Mod5
                var mtb5 = this.GenerateGridTextBox(item.Modifier5Value, item.Modifier5Address, "Mod_", x, 7, 70);
                grid.Children.Add(mtb5);

                // dropdown
                /*
                var test = new ComboBox
                               {
                                   Name = "CbName_" + item.NameStartHex, 
                                   ItemsSource = this.weaponList, 
                                   Width = 150, 
                                   Height = 25, 
                                   SelectedValue = item.Name
                               };
                Grid.SetRow(test, x);
                Grid.SetColumn(test, 7);
                grid.Children.Add(test);
                */
                x++;
            }

            grid.Height = x * 35;

            holder.Children.Add(new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(20, 10, 0, 0),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Text = "Items move around. What you see below may not be what is in memory. Refresh to get the latest data before you try to save anything.",
                Foreground = Brushes.Red
            });

            holder.Children.Add(grid);

            scroll.Content = holder;

            tab.Content = scroll;
        }

        private void DebugData()
        { 
            // Debug Grid data
            DebugGrid.ItemsSource = this.items;

            try
            {
                // Show extra info in 'Codes' tab to see if our cheats are looking in the correct place
                var stamina1 = this.tcpGecko.peek(0x42439594).ToString("x8");
                var stamina2 = this.tcpGecko.peek(0x42439598).ToString("x8");
                this.StaminaData.Content = string.Format("[0x42439594 = {0}, 0x42439598 = {1}]", stamina1, stamina2);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Stamina Code");
            }

            try
            {
                var health1 = this.tcpGecko.peek(0x4225B4B0);
                var health2 = this.tcpGecko.peek(health1 + 0x430);
                this.HealthData.Content = string.Format("0x{0} = {1}", (health1 + 0430).ToString("x8").ToUpper(), health2);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Health Code");
            }

            try
            { 
                var rupee1 = this.tcpGecko.peek(0x3FC92D10);
                var rupee2 = this.tcpGecko.peek(0x4010AA0C);
                this.RupeeData.Content = string.Format("[0x3FC92D10 = {0}, 0x4010AA0C = {1}]", rupee1, rupee2);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Rupee Code");
            }

            try
            { 
                var mon1 = this.tcpGecko.peek(0x3FD41158);
                var mon2 = this.tcpGecko.peek(0x4010B14C);
                this.MonData.Content = string.Format("[0x3FD41158 = {0}, 0x4010B14C = {1}]", mon1, mon2);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Mon Code");
            }

            try
            { 
                var run = this.tcpGecko.peek(0x43A88CC4).ToString("X");
                this.RunData.Content = string.Format("0x43A88CC4 = {0} (Redundant really due to speed code)", run);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Run Code");
            }

            try
            { 
                var speed = this.tcpGecko.peek(0x439BF514).ToString("X");
                this.SpeedData.Content = string.Format("0x439BF514 = {0}", speed);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Speed Code");
            }

                this.MoonJumpData.Content = "Hold X";

            try
            { 
                var weapon1 = this.tcpGecko.peek(0x3FCFB498);
                var weapon2 = this.tcpGecko.peek(0x4010B34C);
                this.WeaponSlotsData.Content = string.Format("[0x3FCFB498 = {0}, 0x4010B34C = {1}]", weapon1, weapon2);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Weapon Slot Code");
            }

            try
            {
                var bow1 = this.tcpGecko.peek(0x3FD4BB50);
                var bow2 = this.tcpGecko.peek(0x4011126C);
                this.BowSlotsData.Content = string.Format("[0x3FD4BB50 = {0}, 0x4011126C = {1}]", bow1, bow2);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Bow Slot Code");
            }

            try
            {
                var shield1 = this.tcpGecko.peek(0x3FCC0B40);
                var shield2 = this.tcpGecko.peek(0x4011128C);
                this.ShieldSlotsData.Content = string.Format("[0x3FCC0B40 = {0}, 0x4011128C = {1}]", shield1, shield2);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Shield Slot Code");
            }

            /*
            try
            {
                var urbosa1 = this.tcpGecko.peek(0x3FCFFA80);
                var urbosa2 = this.tcpGecko.peek(0x4011BA2C);
                this.UrbosaData.Content = string.Format("[0x3FCFFA80 = {0}, 0x4011BA2C = {1}]", urbosa1, urbosa2);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Urbosa Code");
            }

            try
            {
                var revali1 = this.tcpGecko.peek(0x3FD5ED90);
                var revali2 = this.tcpGecko.peek(0x4011BA0C);
                this.RevaliData.Content = string.Format("[0x3FD5ED90 = {0}, 0x4011BA0C = {1}]", revali1, revali2);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Urbosa Code");
            }

            try
            {
                var daruk1 = this.tcpGecko.peek(0x3FD50088);
                var daruk2 = this.tcpGecko.peek(0x4011B9EC);
                this.DarukData.Content = string.Format("[0x3FD50088 = {0}, 0x4011B9EC = {1}]", daruk1, daruk2);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Urbosa Code");
            }
             * */
        }

        private void SetCheats(ICollection<Cheat> cheats)
        {
            try
            {
                // Disable codehandler before we modify
                this.tcpGecko.poke32(CodeHandlerEnabled, 0x00000000);

                // clear current codes
                var clear = CodeHandlerStart;
                while (clear <= CodeHandlerEnd)
                {
                    this.tcpGecko.poke32(clear, 0x0);
                    clear += 0x4;
                }

                var codes = new List<uint>();

                // TODO: Consider moving first and last line of each to loop at the end to avoid duplicating them
                // Most are 32 bit writes
                if (cheats.Contains(Cheat.Stamina))
                {
                    // Max 453B8000
                    var value = uint.Parse(CurrentStamina.Text, NumberStyles.HexNumber);

                    codes.Add(0x00020000);
                    codes.Add(0x42439594);
                    codes.Add(value);
                    codes.Add(0x00000000);

                    codes.Add(0x00020000);
                    codes.Add(0x42439598);
                    codes.Add(value);
                    codes.Add(0x00000000);
                }

                if (cheats.Contains(Cheat.Health))
                {
                    var value = uint.Parse(CurrentHealth.Text, NumberStyles.HexNumber);

                    codes.Add(0x30000000);
                    codes.Add(0x4225B4B0);
                    codes.Add(0x43000000);
                    codes.Add(0x46000000);
                    codes.Add(0x00120430);
                    codes.Add(value);
                    codes.Add(0xD0000000);
                    codes.Add(0xDEADCAFE);
                }

                if (cheats.Contains(Cheat.Run))
                {
                    codes.Add(0x00020000);
                    codes.Add(0x43A88CC4);
                    codes.Add(0x3FC00000);
                    codes.Add(0x00000000);
                }

                if (cheats.Contains(Cheat.Speed))
                {
                    var value = uint.Parse(CbSpeed.SelectedValue.ToString(), NumberStyles.HexNumber);

                    //codes.Add(0x09020000);
                    //codes.Add(0x102F48A8);
                    //codes.Add(0x00004000);
                    //codes.Add(0x00000000);

                    codes.Add(0x00020000);
                    codes.Add(0x439BF514);
                    codes.Add(value);
                    codes.Add(0x00000000);

                    //codes.Add(0xD0000000);
                    //codes.Add(0xDEADCAFE);
                }

                if (cheats.Contains(Cheat.Rupees))
                {
                    var value = Convert.ToUInt32(CurrentRupees.Text);

                    codes.Add(0x00020000);
                    codes.Add(0x3FC92D10);
                    codes.Add(value);
                    codes.Add(0x00000000);

                    codes.Add(0x00020000);
                    codes.Add(0x4010AA0C);
                    codes.Add(value);
                    codes.Add(0x00000000);
                }

                if (cheats.Contains(Cheat.Mon))
                {
                    var value = Convert.ToUInt32(CurrentMon.Text);

                    codes.Add(0x00020000);
                    codes.Add(0x3FD41158);
                    codes.Add(value);
                    codes.Add(0x00000000);

                    codes.Add(0x00020000);
                    codes.Add(0x4010B14C);
                    codes.Add(value);
                    codes.Add(0x00000000);
                }

                if (cheats.Contains(Cheat.MoonJump))
                {
                    uint activator;
                    uint button;
                    if (this.Controller.SelectedValue.ToString() == "Pro")
                    {
                        activator = 0x112671AB;
                        button = 0x00000008;
                    }
                    else
                    {
                        activator = 0x102F48AA;
                        button = 0x00000020;
                    }

                    codes.Add(0x09000000);
                    codes.Add(activator);
                    codes.Add(button);
                    codes.Add(0x00000000);
                    codes.Add(0x00020000);
                    codes.Add(0x439BF528);
                    codes.Add(0xBE800000);
                    codes.Add(0x00000000);
                    codes.Add(0xD0000000);
                    codes.Add(0xDEADCAFE);

                    codes.Add(0x06000000);
                    codes.Add(activator);
                    codes.Add(button);
                    codes.Add(0x00000000);
                    codes.Add(0x00020000);
                    codes.Add(0x439BF528);
                    codes.Add(0x3F800000);
                    codes.Add(0x00000000);
                    codes.Add(0xD0000000);
                    codes.Add(0xDEADCAFE);
                }

                if (cheats.Contains(Cheat.WeaponInv))
                {
                    var value = Convert.ToUInt32(CurrentWeaponSlots.Text);

                    codes.Add(0x00020000);
                    codes.Add(0x3FCFB498);
                    codes.Add(value);
                    codes.Add(0x00000000);

                    codes.Add(0x00020000);
                    codes.Add(0x4010B34C);
                    codes.Add(value);
                    codes.Add(0x00000000);
                }

                if (cheats.Contains(Cheat.BowInv))
                {
                    var value = Convert.ToUInt32(CurrentBowSlots.Text);

                    codes.Add(0x00020000);
                    codes.Add(0x3FD4BB50);
                    codes.Add(value);
                    codes.Add(0x00000000);

                    codes.Add(0x00020000);
                    codes.Add(0x4011126C);
                    codes.Add(value);
                    codes.Add(0x00000000);
                }

                if (cheats.Contains(Cheat.ShieldInv))
                {
                    var value = Convert.ToUInt32(CurrentShieldSlots.Text);

                    codes.Add(0x00020000);
                    codes.Add(0x3FCC0B40);
                    codes.Add(value);
                    codes.Add(0x00000000);

                    codes.Add(0x00020000);
                    codes.Add(0x4011128C);
                    codes.Add(value);
                    codes.Add(0x00000000);
                }

                /*
                if (cheats.Contains(Cheat.Urbosa))
                {
                    var value = Convert.ToUInt32(CurrentUrbosa.Text);

                    codes.Add(0x00020000);
                    codes.Add(0x3FCFFA80);
                    codes.Add(value);
                    codes.Add(0x00000000);

                    codes.Add(0x00020000);
                    codes.Add(0x4011BA2C);
                    codes.Add(value);
                    codes.Add(0x00000000);
                }

                if (cheats.Contains(Cheat.Revali))
                {
                    var value = Convert.ToUInt32(CurrentRevali.Text);

                    codes.Add(0x00020000);
                    codes.Add(0x3FD5ED90);
                    codes.Add(value);
                    codes.Add(0x00000000);

                    codes.Add(0x00020000);
                    codes.Add(0x4011BA0C);
                    codes.Add(value);
                    codes.Add(0x00000000);
                }

                if (cheats.Contains(Cheat.Daruk))
                {
                    var value = Convert.ToUInt32(CurrentDaruk.Text);

                    codes.Add(0x00020000);
                    codes.Add(0x3FD50088);
                    codes.Add(value);
                    codes.Add(0x00000000);

                    codes.Add(0x00020000);
                    codes.Add(0x4011B9EC);
                    codes.Add(value);
                    codes.Add(0x00000000);
                }
                */

                // Write our selected codes
                var address = CodeHandlerStart;
                foreach (var code in codes)
                {
                    this.tcpGecko.poke32(address, code);
                    address += 0x4;
                }

                // Re-enable codehandler
                this.tcpGecko.poke32(CodeHandlerEnabled, 0x00000001);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Set Cheats");
            }
        }

        private void ToggleControls(string state)
        {
            if (state == "Connected")
            {
                this.Load.IsEnabled = true;
                this.Connect.IsEnabled = false;
                this.Connect.Visibility = Visibility.Hidden;

                this.Disconnect.IsEnabled = true;
                this.Disconnect.Visibility = Visibility.Visible;

                this.IpAddress.IsEnabled = false;

                if (this.Load.Visibility == Visibility.Hidden)
                {
                    this.Refresh.IsEnabled = true;
                }
            }

            if (state == "Disconnected")
            {
                this.Load.IsEnabled = false;
                this.Connect.IsEnabled = true;
                this.Connect.Visibility = Visibility.Visible;
                this.Disconnect.IsEnabled = false;
                this.Disconnect.Visibility = Visibility.Hidden;
                this.IpAddress.IsEnabled = true;

                this.Refresh.IsEnabled = false;
            }

            if (state == "Load")
            {
                TabControl.IsEnabled = false;
                this.Load.IsEnabled = false;
                this.Load.Visibility = Visibility.Hidden;

                this.Refresh.IsEnabled = false;
            }

            if (state == "DataLoaded")
            {
                TabControl.IsEnabled = true;
                this.Refresh.IsEnabled = true;
            }

            if (state == "ForceRefresh")
            {
                TabControl.IsEnabled = false;
                this.Save.IsEnabled = false;
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Save == null)
            {
                return;
            }

            if (Debug.IsSelected || Help.IsSelected || Credits.IsSelected)
            {
                this.Save.IsEnabled = false;
                return;
            }

            this.Save.IsEnabled = this.HasChanged;
        }

        private void ClientDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                var result = e.Result;
                if (result != this.version)
                {
                    MessageBox.Show(string.Format("An update is available: {0}", result));
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error checking for new version.");
            }
        }

        private void UpdateProgress(int percent)
        {
            Progress.Value = percent;
        }

        private void ExportToExcel()
        {
            try
            {
                DebugGrid.SelectAllCells();
                DebugGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                ApplicationCommands.Copy.Execute(null, DebugGrid);
                var result = (string)Clipboard.GetData(DataFormats.CommaSeparatedValue);
                DebugGrid.UnselectAllCells();

                var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var excelFile = new StreamWriter(path + @"\debug.csv");
                excelFile.WriteLine(result);
                excelFile.Close();

                MessageBox.Show("File exported to " + path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private uint ReadStream(Stream stream, long offset)
        {
            var buffer = new byte[4];

            stream.Seek(offset, SeekOrigin.Begin);
            stream.Read(buffer, 0, 4);
            var data = ByteSwap.Swap(BitConverter.ToUInt32(buffer, 0));

            return data;
        }

        private TextBox GenerateGridTextBox(string value, string field, string type, int x, int col, int width = 75)
        {
            var tb = new TextBox
            {
                Text = value, 
                ToolTip = field,
                Tag = field,
                Width = width, 
                Height = 22, 
                Margin = new Thickness(10, 0, 10, 0), 
                Name = type + field, 
                IsEnabled = true, 
                CharacterCasing = CharacterCasing.Upper, 
                MaxLength = 8
            };

            tb.TextChanged += this.TextChanged;

            var check = (TextBox)this.FindName(type + field);
            if (check != null)
            {
                this.UnregisterName(type + field);
            }

            this.RegisterName(type + field, tb);

            Grid.SetRow(tb, x);
            Grid.SetColumn(tb, col);

            return tb;
        }

        private Grid GenerateTabGrid(string tab)
        {
            var grid = new Grid
            {
                Name = "TabGrid",
                Margin = new Thickness(10),
                ShowGridLines = true,
                VerticalAlignment = VerticalAlignment.Top
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) }); // Name
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // Id
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            grid.RowDefinitions.Add(new RowDefinition());

            // Headers
            var nameHeader = new TextBlock
            {
                Text = "Item Name",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Width = 190
            };
            Grid.SetRow(nameHeader, 0);
            Grid.SetColumn(nameHeader, 0);
            grid.Children.Add(nameHeader);

            var idHeader = new TextBlock
            {
                Text = "Item Id",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Width = 150,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetRow(idHeader, 0);
            Grid.SetColumn(idHeader, 1);
            grid.Children.Add(idHeader);

            var valueHeader = new TextBlock
            {
                Text = "Item Value",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(valueHeader, 0);
            Grid.SetColumn(valueHeader, 2);
            grid.Children.Add(valueHeader);

            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var headerNames = new[] { "Mod. 1", "Mod. 2", "Mod. 3", "Mod. 4", "Mod. 5" };

            for (int y = 0; y < 5; y++)
            {
                if (tab == "Food")
                {
                    headerNames = new[] { "Hearts", "Duration", "Mod Value?", "Mod Type", "Mod Level" };
                }

                if (tab == "Weapons" || tab == "Bows" || tab == "Shields")
                {
                    headerNames = new[] { "Mod Amount", "N/A", " Mod Type", "N/A", "N/A" };
                }

                var header = new TextBlock
                {
                    Text = headerNames[y],
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, y + 3);
                grid.Children.Add(header);
            }

            return grid;
        }

        private string GetNameFromId(string id, string pagename)
        {
            if (pagename == "Head" || pagename == "Torso" || pagename == "Legs")
            {
                pagename = "Armor";
            }

            var name = "Unknown";
            var path = string.Format("Items.{0}.{1}.Name", pagename.Replace(" ", string.Empty), id);
            var obj = this.json.SelectToken(path);
            if (obj != null)
            {
                name = obj.ToString();
            }

            return name;
        }

        private void LogError(Exception ex, string more = null)
        {
            var paragraph = new Paragraph
            {
                FontSize = 14,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                LineHeight = 14
            };

            if (more != null)
            {
                paragraph.Inlines.Add(more + Environment.NewLine);
            }

            paragraph.Inlines.Add(ex.Message);
            paragraph.Inlines.Add(ex.StackTrace);

            ErrorLog.Document.Blocks.Add(paragraph);

            ErrorLog.Document.Blocks.Add(new Paragraph());

            TabControl.IsEnabled = true;

            MessageBox.Show("Error caught. Check Error Tab");
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ddChanged.Add(sender as ComboBox);

            this.Save.IsEnabled = this.HasChanged;
        }

        private void TextChanged(object sender, TextChangedEventArgs textChangedEventArgs)
        {
            this.tbChanged.Add(sender as TextBox);

            this.Save.IsEnabled = this.HasChanged;
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            this.cbChanged.Add(sender as CheckBox);

            this.Save.IsEnabled = this.HasChanged;
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.Save.IsEnabled = this.HasChanged;

            this.timer = new DispatcherTimer();
            this.timer.Tick += this.TimerTick;
            this.timer.Interval = new TimeSpan(0, 2, 0);
            //this.timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            this.ToggleControls("ForceRefresh");
            MessageBox.Show("Refresh time!");
        }
    }
}