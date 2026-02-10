namespace capstone_backend.Extensions.Common
{
    public static class GeoCalculator
    {
        private const double EarthRadiusKm = 6371.0;

        public static double CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            return Compute((double)lat1, (double)lon1, (double)lat2, (double)lon2);
        }

        private static double Compute(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var lat1Rad = DegreesToRadians(lat1);
            var lat2Rad = DegreesToRadians(lat2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) *
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
