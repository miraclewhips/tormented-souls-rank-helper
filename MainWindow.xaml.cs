using System.Collections.Generic;
using System.Windows;
using System;
using System.Diagnostics;
using System.Timers;

namespace tormented_souls_rank_helper
{
    using gh;

    public partial class MainWindow : Window
    {
        IntPtr hProc;
        IntPtr baseAddress;

        private static Timer UpdateTimer;

        public MainWindow()
        {
            InitializeComponent();

            UpdateTimer = new System.Timers.Timer();
            UpdateTimer.Interval = 1000;
            UpdateTimer.Elapsed += Update;
            UpdateTimer.Enabled = true;
        }

        private string pad(double n)
        {
            if(n < 10)
            {
                return "0" + n;
            }
            else
            {
                return n.ToString();
            }
        }

        private string FormatTime(float time)
        {
            var hours = Math.Floor(time / 3600);
            var mins = Math.Floor((time / 60) % 60);
            var seconds = Math.Floor(time % 60);

            if (hours > 0)
            {
                return pad(hours) + ":" + pad(mins) + ":" + pad(seconds);
            }
            else
            {
                return pad(mins) + ":" + pad(seconds);
            }
        }

        private string FormatDistance(float dist)
        {
            return String.Format("{0:n}m", dist);
        }

        private void Update(Object source, System.Timers.ElapsedEventArgs e)
        {
            Process[] proc = Process.GetProcessesByName("TormentedSouls");

            this.Dispatcher.Invoke(() =>
            {
                noProcessElement.Visibility = proc.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
                resultsElement.Visibility = proc.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            });

            if (proc.Length == 0)
            {
                return;
            }

            hProc = ghapi.OpenProcess(ghapi.ProcessAccessFlags.All, false, proc[0].Id);
            baseAddress = ghapi.GetModuleBaseAddress(proc[0], "mono-2.0-bdwgc.dll");

            float valTime = ValueFloat(ghapi.FindDMAAddy(hProc, (IntPtr)(baseAddress + 0x49A358), new int[] { 0xF8, 0xA0, 0x1E8, 0x1E8, 0x0, 0x10, 0xA4 }));
            int valSaves = ValueInt(ghapi.FindDMAAddy(hProc, (IntPtr)(baseAddress + 0x49A358), new int[] { 0xF8, 0xA0, 0x1E8, 0x1E8, 0x0, 0x10, 0xA8 }));
            int valKills = ValueInt(ghapi.FindDMAAddy(hProc, (IntPtr)(baseAddress + 0x49A358), new int[] { 0xF8, 0xA0, 0x1E8, 0x1E8, 0x0, 0x10, 0xAC }));
            int valMedkits = ValueInt(ghapi.FindDMAAddy(hProc, (IntPtr)(baseAddress + 0x49A358), new int[] { 0xF8, 0xA0, 0x1E8, 0x1E8, 0x0, 0x10, 0xB0 }));
            float valDistance = ValueFloat(ghapi.FindDMAAddy(hProc, (IntPtr)(baseAddress + 0x49A358), new int[] { 0xF8, 0xA0, 0x1E8, 0x1E8, 0x0, 0x10, 0xD4 }));
            int valDamage = ValueInt(ghapi.FindDMAAddy(hProc, (IntPtr)(baseAddress + 0x49A358), new int[] { 0xF8, 0xA0, 0x1E8, 0x1E8, 0x0, 0x10, 0xD0 }));

            byte valBookAnna = ValueByte(ghapi.FindDMAAddy(hProc, (IntPtr)(baseAddress + 0x49A358), new int[] { 0xF8, 0xA0, 0x1E8, 0x1E8, 0x0, 0x10, 0xCA }));
            byte valBookBertram = ValueByte(ghapi.FindDMAAddy(hProc, (IntPtr)(baseAddress + 0x49A358), new int[] { 0xF8, 0xA0, 0x1E8, 0x1E8, 0x0, 0x10, 0xC9 }));
            byte valBookMaria = ValueByte(ghapi.FindDMAAddy(hProc, (IntPtr)(baseAddress + 0x49A358), new int[] { 0xF8, 0xA0, 0x1E8, 0x1E8, 0x0, 0x10, 0xCB }));
            byte valBookWilliam = ValueByte(ghapi.FindDMAAddy(hProc, (IntPtr)(baseAddress + 0x49A358), new int[] { 0xF8, 0xA0, 0x1E8, 0x1E8, 0x0, 0x10, 0xCC }));
            int valMainBooks = valBookAnna + valBookBertram + valBookMaria + valBookWilliam;

            List<ResultField> results = new List<ResultField>();
            results.Add(new ResultField() {
                Name = "Total Time",
                Hint = "Under 4 hours",
                Value = FormatTime(valTime),
                Valid = valTime < 14400
            });

            results.Add(new ResultField() {
                Name = "Saves",
                Hint = "3 or fewer",
                Value = valSaves.ToString(),
                Valid = valSaves <= 3
            });

            results.Add(new ResultField() {
                Name = "Defeated Enemies",
                Hint = "40 or more",
                Value = valKills.ToString(),
                Valid = valKills >= 40
            });

            results.Add(new ResultField() {
                Name = "Medical Kits Found",
                Hint = "5 or more",
                Value = valMedkits.ToString(),
                Valid = valMedkits >= 5
            });

            results.Add(new ResultField() {
                Name = "Traveled Distance",
                Hint = "Under 10,000m",
                Value = FormatDistance(valDistance),
                Valid = valDistance < 10000
            });

            results.Add(new ResultField() {
                Name = "Damage Received",
                Hint = "Under 500",
                Value = valDamage.ToString(),
                Valid = valDamage < 500
            });

            results.Add(new ResultField() {
                Name = "Main Books Completed",
                Hint = "4 - Anna, Bertram, Maria, William",
                Value = valMainBooks.ToString(),
                Valid = valMainBooks == 4
            });

            this.Dispatcher.Invoke(() =>
            {
                resultsElement.ItemsSource = results;
            });
        }

        private float ValueFloat(IntPtr value)
        {
            byte[] val = new byte[4];
            ghapi.ReadProcessMemory(hProc, value, val, 16, out _);
            return System.BitConverter.ToSingle(val);
        }

        private int ValueInt(IntPtr value)
        {
            byte[] val = new byte[4];
            ghapi.ReadProcessMemory(hProc, value, val, 16, out _);
            return System.BitConverter.ToInt32(val);
        }

        private byte ValueByte(IntPtr value)
        {
            byte[] val = new byte[1];
            ghapi.ReadProcessMemory(hProc, value, val, 16, out _);
            return val[0];
        }
    }

    public class ResultField
    {
        public string Name { get; set; }
        public string Hint { get; set; }
        public string Value { get; set; }
        public bool Valid { get; set; } = false;
    }
}