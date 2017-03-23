namespace BotwTrainer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public class Codes
    {
        private readonly MainWindow mainWindow;

        public Codes(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        private enum Cheat
        {
            Stamina = 0,
            Health = 1,
            //Run = 2,
            Rupees = 3,
            MoonJump = 4,
            WeaponInv = 5,
            BowInv = 6,
            ShieldInv = 7,
            Speed = 8,
            Mon = 9,
            Urbosa = 10,
            Revali = 11,
            Daruk = 12,
            //Keys = 13,
            Bombs = 14,
            Whips = 15
        }

        private List<Cheat> GetSelected()
        {
            var selected = new List<Cheat>();

            if (this.mainWindow.Stamina.IsChecked == true)
            {
                selected.Add(Cheat.Stamina);
            }

            if (this.mainWindow.Health.IsChecked == true)
            {
                selected.Add(Cheat.Health);
            }

            if (this.mainWindow.Rupees.IsChecked == true)
            {
                selected.Add(Cheat.Rupees);
            }

            if (this.mainWindow.Mon.IsChecked == true)
            {
                selected.Add(Cheat.Mon);
            }

            if (this.mainWindow.Speed.IsChecked == true)
            {
                selected.Add(Cheat.Speed);
            }

            if (this.mainWindow.MoonJump.IsChecked == true)
            {
                selected.Add(Cheat.MoonJump);
            }

            if (this.mainWindow.WeaponSlots.IsChecked == true)
            {
                selected.Add(Cheat.WeaponInv);
            }

            if (this.mainWindow.BowSlots.IsChecked == true)
            {
                selected.Add(Cheat.BowInv);
            }

            if (this.mainWindow.ShieldSlots.IsChecked == true)
            {
                selected.Add(Cheat.ShieldInv);
            }

            if (this.mainWindow.Urbosa.IsChecked == true)
            {
                selected.Add(Cheat.Urbosa);
            }

            if (this.mainWindow.Revali.IsChecked == true)
            {
                selected.Add(Cheat.Revali);
            }

            if (this.mainWindow.Daruk.IsChecked == true)
            {
                selected.Add(Cheat.Daruk);
            }

            if (this.mainWindow.BombTime.IsChecked == true)
            {
                selected.Add(Cheat.Bombs);
            }

            if (this.mainWindow.HorseWhips.IsChecked == true)
            {
                selected.Add(Cheat.Whips);
            }

            return selected;
        }

        public IEnumerable<uint> CreateCodeList()
        {
            var codes = new List<uint>();

            var cheats = this.GetSelected();

            if (cheats.Contains(Cheat.Stamina))
            {
                // Max 453B8000
                var value = uint.Parse(this.mainWindow.CurrentStamina.Text, NumberStyles.HexNumber);

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
                var value = Convert.ToUInt32(this.mainWindow.CurrentHealth.Text);

                codes.Add(0x30000000);
                codes.Add(0x4225B4B0);
                codes.Add(0x43000000);
                codes.Add(0x46000000);
                codes.Add(0x00120430);
                codes.Add(value);
                codes.Add(0xD0000000);
                codes.Add(0xDEADCAFE);
            }

            if (cheats.Contains(Cheat.Speed))
            {
                var value = uint.Parse(this.mainWindow.CbSpeed.SelectedValue.ToString(), NumberStyles.HexNumber);

                uint activator;
                if (this.mainWindow.Controller.SelectedValue.ToString() == "Pro")
                {
                    activator = 0x112671AB;
                }
                else
                {
                    activator = 0x102F48AB;
                }

                codes.Add(0x09000000);
                codes.Add(activator);
                codes.Add(0x00000080);
                codes.Add(0x00000000);
                codes.Add(0x00020000);
                codes.Add(0x439BF514);
                codes.Add(value);
                codes.Add(0x00000000);
                codes.Add(0xD0000000);
                codes.Add(0xDEADCAFE);

                codes.Add(0x06000000);
                codes.Add(activator);
                codes.Add(0x00000080);
                codes.Add(0x00000000);
                codes.Add(0x00020000);
                codes.Add(0x439BF514);
                codes.Add(0x3F800000);
                codes.Add(0x00000000);
                codes.Add(0xD0000000);
                codes.Add(0xDEADCAFE);
            }

            if (cheats.Contains(Cheat.Rupees))
            {
                var value = Convert.ToUInt32(this.mainWindow.CurrentRupees.Text);

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
                var value = Convert.ToUInt32(this.mainWindow.CurrentMon.Text);

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
                uint button;
                uint activator;
                if (this.mainWindow.Controller.SelectedValue.ToString() == "Pro")
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
                var value = Convert.ToUInt32(this.mainWindow.CurrentWeaponSlots.Text);

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
                var value = Convert.ToUInt32(this.mainWindow.CurrentBowSlots.Text);

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
                var value = Convert.ToUInt32(this.mainWindow.CurrentShieldSlots.Text);

                codes.Add(0x00020000);
                codes.Add(0x3FCC0B40);
                codes.Add(value);
                codes.Add(0x00000000);

                codes.Add(0x00020000);
                codes.Add(0x4011128C);
                codes.Add(value);
                codes.Add(0x00000000);
            }

            if (cheats.Contains(Cheat.Urbosa))
            {
                var value = Convert.ToUInt32(this.mainWindow.CurrentUrbosa.Text);

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
                var value = Convert.ToUInt32(this.mainWindow.CurrentRevali.Text);

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
                var value = Convert.ToUInt32(this.mainWindow.CurrentDaruk.Text);

                codes.Add(0x00020000);
                codes.Add(0x3FD50088);
                codes.Add(value);
                codes.Add(0x00000000);

                codes.Add(0x00020000);
                codes.Add(0x4011B9EC);
                codes.Add(value);
                codes.Add(0x00000000);
            }

            if (cheats.Contains(Cheat.Bombs))
            {
                codes.Add(0x00020000);
                codes.Add(0x4383DA34);
                codes.Add(0x42B70000);
                codes.Add(0x00000000);

                codes.Add(0x00020000);
                codes.Add(0x4383DA4C);
                codes.Add(0x42B70000);
                codes.Add(0x00000000);
            }

            if (cheats.Contains(Cheat.Whips))
            {
                codes.Add(0x00000000);
                codes.Add(0x4011124F);
                codes.Add(0x00000003);
                codes.Add(0x00000000);

                codes.Add(0x00000000);
                codes.Add(0x44AFFA8F);
                codes.Add(0x00000003);
                codes.Add(0x00000000);

                codes.Add(0x00000000);
                codes.Add(0x47558581);
                codes.Add(0x00000003);
                codes.Add(0x00000000);
            }

            return codes;
        } 
    }
}
