using System;
using System.Drawing;

namespace Matric_scope
{
    public class StoneReportData
    {
        public string StoneName { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string ShapeName { get; set; } = "";

        public double LengthMm { get; set; }
        public double WidthMm { get; set; }

        public string WeightCarat { get; set; } = "";

        public DateTime MeasurementDateTime { get; set; } =
            DateTime.Now;

        public string OperatorMachine { get; set; } = "";
        public string Remarks { get; set; } = "";

        public Image StoneImage { get; set; }

        public double LengthWidthRatio
        {
            get
            {
                return WidthMm > 0.000001
                    ? LengthMm / WidthMm
                    : 0.0;
            }
        }

        public StoneReportData CloneForPrint()
        {
            return new StoneReportData
            {
                StoneName = StoneName,
                CustomerName = CustomerName,
                ShapeName = ShapeName,
                LengthMm = LengthMm,
                WidthMm = WidthMm,
                WeightCarat = WeightCarat,
                MeasurementDateTime = MeasurementDateTime,
                OperatorMachine = OperatorMachine,
                Remarks = Remarks,
                StoneImage = StoneImage
            };
        }
    }
}
