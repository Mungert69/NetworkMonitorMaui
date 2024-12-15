namespace NetworkMonitor.Maui.Utils
{
    public class CircleLayoutCalculator
    {
         private int maxIndicatorsPerCircle;
        private int minIndicatorsPerCircle;
        private double outerCircleRadius;
        private double radiusDecrement;
        private double indicatorRadius;

        public CircleLayoutCalculator(int maxIndicatorsPerCircle, int minIndicatorsPerCircle, double outerCircleRadius, double radiusDecrement, double indicatorRadius)
        {
            this.maxIndicatorsPerCircle = maxIndicatorsPerCircle;
            this.minIndicatorsPerCircle = minIndicatorsPerCircle;
            this.outerCircleRadius = outerCircleRadius;
            this.radiusDecrement = radiusDecrement;
            this.indicatorRadius = indicatorRadius;
        }

          public int CalculateIndicatorsPerCircle(int circleNumber)
        {
            double innerCircleRadius = outerCircleRadius - (circleNumber * radiusDecrement);
            int indicatorsInInnerCircle = (int)(maxIndicatorsPerCircle * (innerCircleRadius / outerCircleRadius));
            return Math.Max(indicatorsInInnerCircle, minIndicatorsPerCircle);
        }
   public double CalculateCircleRadius(int circleNumber, int totalCircles)
{
    // Directly reduce the radius for each subsequent circle
    double radius = outerCircleRadius - (circleNumber * radiusDecrement);
    return Math.Max(radius, indicatorRadius); // Ensure radius is not less than the indicator radius
}


        public (double X, double Y) CalculatePosition(int indicatorIndex, int totalIndicators)
        {
            int currentCircle = 0;
            int totalCircles = (int)Math.Ceiling(Math.Sqrt(totalIndicators));
            int indicatorsInCurrentCircle = CalculateIndicatorsPerCircle(currentCircle);
            int indicatorCount = 0;

            while (indicatorIndex >= indicatorCount + indicatorsInCurrentCircle)
            {
                indicatorCount += indicatorsInCurrentCircle;
                currentCircle++;
                indicatorsInCurrentCircle = CalculateIndicatorsPerCircle(currentCircle);
            }

            int positionInCircle = indicatorIndex - indicatorCount;
            double radius = CalculateCircleRadius(currentCircle, totalCircles);
            double angle = 2 * Math.PI * positionInCircle / indicatorsInCurrentCircle;
            double xPosition = 0.5 + radius * Math.Cos(angle);
            double yPosition = 0.5 + radius * Math.Sin(angle);

            return (xPosition, yPosition);
        }
    }
}
