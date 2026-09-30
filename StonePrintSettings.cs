using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Matric_scope
{
    public class StonePrintSettings
    {
        // ------------------------------------------------------------
        // FIELD SELECTION
        // ------------------------------------------------------------

        // Length and Width are mandatory.
        public bool PrintLength { get; set; } = true;
        public bool PrintWidth { get; set; } = true;

        // Ratio title is fixed, but user may choose whether to print it.
        public bool PrintLengthWidthRatio { get; set; } = true;

        // Optional fields.
        public bool PrintStoneName { get; set; } = true;
        public bool PrintCustomerName { get; set; } = true;
        public bool PrintShapeName { get; set; } = true;
        public bool PrintWeight { get; set; } = true;
        public bool PrintDateTime { get; set; } = true;
        public bool PrintOperatorMachine { get; set; } = true;
        public bool PrintRemarks { get; set; } = true;

        // ------------------------------------------------------------
        // FIXED TITLES - NOT EDITABLE
        // ------------------------------------------------------------

        public string LengthTitle
        {
            get { return "Length"; }
        }

        public string WidthTitle
        {
            get { return "Width"; }
        }

        public string RatioTitle
        {
            get { return "Length / Width Ratio"; }
        }

        // ------------------------------------------------------------
        // EDITABLE TITLES
        // ------------------------------------------------------------

        public string StoneNameTitle { get; set; } = "Stone Name / ID";
        public string CustomerNameTitle { get; set; } = "Customer Name";
        public string ShapeNameTitle { get; set; } = "Shape Name";
        public string WeightTitle { get; set; } = "Weight / Carat";
        public string DateTimeTitle { get; set; } = "Measurement Date & Time";
        public string OperatorMachineTitle { get; set; } = "Operator / Machine";
        public string RemarksTitle { get; set; } = "Remarks / Notes";

        public static string SettingsFilePath
        {
            get
            {
                return Path.Combine(
                    Application.UserAppDataPath,
                    "StoneReportPrintSettings.cfg");
            }
        }

        public static StonePrintSettings Load()
        {
            StonePrintSettings settings = new StonePrintSettings();

            try
            {
                if (!File.Exists(SettingsFilePath))
                    return settings;

                string[] lines = File.ReadAllLines(SettingsFilePath);

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();

                    if (string.IsNullOrWhiteSpace(line) ||
                        line.StartsWith("#"))
                        continue;

                    int p = line.IndexOf('=');
                    if (p <= 0)
                        continue;

                    string key = line.Substring(0, p).Trim();
                    string value = line.Substring(p + 1).Trim();

                    bool boolValue;

                    // Selection settings.
                    if (bool.TryParse(value, out boolValue))
                    {
                        switch (key)
                        {
                            case "PrintStoneName":
                                settings.PrintStoneName = boolValue;
                                continue;

                            case "PrintCustomerName":
                                settings.PrintCustomerName = boolValue;
                                continue;

                            case "PrintShapeName":
                                settings.PrintShapeName = boolValue;
                                continue;

                            case "PrintLengthWidthRatio":
                                settings.PrintLengthWidthRatio = boolValue;
                                continue;

                            case "PrintWeight":
                                settings.PrintWeight = boolValue;
                                continue;

                            case "PrintDateTime":
                                settings.PrintDateTime = boolValue;
                                continue;

                            case "PrintOperatorMachine":
                                settings.PrintOperatorMachine = boolValue;
                                continue;

                            case "PrintRemarks":
                                settings.PrintRemarks = boolValue;
                                continue;
                        }
                    }

                    // Editable titles.
                    switch (key)
                    {
                        case "StoneNameTitle":
                            settings.StoneNameTitle =
                                DecodeTitle(value, settings.StoneNameTitle);
                            break;

                        case "CustomerNameTitle":
                            settings.CustomerNameTitle =
                                DecodeTitle(value, settings.CustomerNameTitle);
                            break;

                        case "ShapeNameTitle":
                            settings.ShapeNameTitle =
                                DecodeTitle(value, settings.ShapeNameTitle);
                            break;

                        case "WeightTitle":
                            settings.WeightTitle =
                                DecodeTitle(value, settings.WeightTitle);
                            break;

                        case "DateTimeTitle":
                            settings.DateTimeTitle =
                                DecodeTitle(value, settings.DateTimeTitle);
                            break;

                        case "OperatorMachineTitle":
                            settings.OperatorMachineTitle =
                                DecodeTitle(value, settings.OperatorMachineTitle);
                            break;

                        case "RemarksTitle":
                            settings.RemarksTitle =
                                DecodeTitle(value, settings.RemarksTitle);
                            break;
                    }
                }
            }
            catch
            {
                // If settings cannot be read, use defaults.
            }

            // Mandatory fields can never be disabled.
            settings.PrintLength = true;
            settings.PrintWidth = true;

            settings.NormalizeTitles();

            return settings;
        }

        public void Save()
        {
            PrintLength = true;
            PrintWidth = true;

            NormalizeTitles();

            Directory.CreateDirectory(Application.UserAppDataPath);

            List<string> lines = new List<string>();

            lines.Add("# Stone measurement A4 report settings");
            lines.Add("# Titles are saved as Base64 so any normal text is allowed.");
            lines.Add("");

            lines.Add("PrintStoneName=" + PrintStoneName);
            lines.Add("PrintCustomerName=" + PrintCustomerName);
            lines.Add("PrintShapeName=" + PrintShapeName);
            lines.Add("PrintLengthWidthRatio=" + PrintLengthWidthRatio);
            lines.Add("PrintWeight=" + PrintWeight);
            lines.Add("PrintDateTime=" + PrintDateTime);
            lines.Add("PrintOperatorMachine=" + PrintOperatorMachine);
            lines.Add("PrintRemarks=" + PrintRemarks);
            lines.Add("");

            lines.Add("StoneNameTitle=" + EncodeTitle(StoneNameTitle));
            lines.Add("CustomerNameTitle=" + EncodeTitle(CustomerNameTitle));
            lines.Add("ShapeNameTitle=" + EncodeTitle(ShapeNameTitle));
            lines.Add("WeightTitle=" + EncodeTitle(WeightTitle));
            lines.Add("DateTimeTitle=" + EncodeTitle(DateTimeTitle));
            lines.Add("OperatorMachineTitle=" + EncodeTitle(OperatorMachineTitle));
            lines.Add("RemarksTitle=" + EncodeTitle(RemarksTitle));

            File.WriteAllLines(SettingsFilePath, lines.ToArray());
        }

        public void ResetTitlesToDefault()
        {
            StoneNameTitle = "Stone Name / ID";
            CustomerNameTitle = "Customer Name";
            ShapeNameTitle = "Shape Name";
            WeightTitle = "Weight / Carat";
            DateTimeTitle = "Measurement Date & Time";
            OperatorMachineTitle = "Operator / Machine";
            RemarksTitle = "Remarks / Notes";
        }

        public void NormalizeTitles()
        {
            StoneNameTitle = NormalizeTitle(
                StoneNameTitle,
                "Stone Name / ID");

            CustomerNameTitle = NormalizeTitle(
                CustomerNameTitle,
                "Customer Name");

            ShapeNameTitle = NormalizeTitle(
                ShapeNameTitle,
                "Shape Name");

            WeightTitle = NormalizeTitle(
                WeightTitle,
                "Weight / Carat");

            DateTimeTitle = NormalizeTitle(
                DateTimeTitle,
                "Measurement Date & Time");

            OperatorMachineTitle = NormalizeTitle(
                OperatorMachineTitle,
                "Operator / Machine");

            RemarksTitle = NormalizeTitle(
                RemarksTitle,
                "Remarks / Notes");
        }

        private static string NormalizeTitle(
            string value,
            string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            value = value.Trim();

            // Prevent extremely long titles from breaking report layout.
            if (value.Length > 60)
                value = value.Substring(0, 60);

            return value;
        }

        private static string EncodeTitle(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? "");
            return Convert.ToBase64String(bytes);
        }

        private static string DecodeTitle(
            string encoded,
            string fallback)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(encoded);
                string result = Encoding.UTF8.GetString(bytes);

                return string.IsNullOrWhiteSpace(result)
                    ? fallback
                    : result.Trim();
            }
            catch
            {
                // Backward-compatible fallback if the user manually edits
                // the configuration file with plain text.
                return string.IsNullOrWhiteSpace(encoded)
                    ? fallback
                    : encoded.Trim();
            }
        }
    }
}
