using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matric_scope
{
    public class ImageMetrology
    {
        //private double _pixelsPerMetric = 133.370;//0;
        private double _pixelsPerMetric = 131.086;
        
        public double Calibrate(double pixelRadius, double actualDiameterMm)
        {
            double pixelDiameter = pixelRadius * 2.0;

            if (actualDiameterMm <= 0)
                throw new ArgumentException("Actual diameter must be greater than zero.");

            _pixelsPerMetric = pixelDiameter / actualDiameterMm;

            return _pixelsPerMetric;
        }

        public double GetRealDiameter(CircleSegment targetCircle)
        {
            try
            {
                _pixelsPerMetric = Convert.ToDouble(new ModifyRegistry().Read("ppm"));
            }
            catch
            {

            }
            if (_pixelsPerMetric <= 0)
                throw new InvalidOperationException("System not calibrated. Call Calibrate() first.");

            double pixelDiameter = targetCircle.Radius * 2.0;
            return pixelDiameter / _pixelsPerMetric;
        }
    }
}
