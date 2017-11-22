using System;

namespace ShiftScaleAddin {
    /// <summary>
    /// This class is used to perform some utility functions.
    /// Credit goes to <see cref="https://github.com/cgcai/SVY21/blob/master/csharp/svy21/Svy21.cs"/>
    /// </summary>
    class Util {

        /// <summary>
        /// Converts WGS84 lat/lon to Spherical Mercator EPSG:900913 xy meters.
        /// </summary>
        public static void CoordinateToEPSG900913Meters(double lat, double lon, out double x, out double y) {
            const int EarthRadius = 6378137;
            const double OriginShift = 2 * Math.PI * EarthRadius / 2;
            x = lon * OriginShift / 180;
            var posy = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180);
            y = posy * OriginShift / 180;
        }

        private const double RadRatio = Math.PI / 180;		// Ratio to convert degrees to radians.

        // Datum and Projection
        private const double A = 6378137;				// Semi-major axis of reference ellipsoid.
        private const double F = 1 / 298.257223563;	// Ellipsoidal flattening.
        private const double OLat = 1.366666;				// Origin latitude (degrees).
        private const double OLon = 103.833333;			// Origin longitude (degrees).
        private const double No = 38744.572;			// False Northing.
        private const double Eo = 28001.642;			// False Easting.
        private const double K = 1.0;					// Central meridian scale factor.

        // Computed Projection Constants
        // Naming convention: the trailing number is the power of the variable.

        // Semi-minor axis of reference ellipsoid.
        private const double B = A * (1 - F);
        // Squared eccentricity of reference ellipsoid.
        private const double E2 = (2 * F) - (F * F);
        private const double E4 = E2 * E2;
        private const double E6 = E4 * E2;
        private const double N = (A - B) / (A + B);
        private const double N2 = N * N;
        private const double N3 = N2 * N;
        private const double N4 = N2 * N2;
        private const double G = A * (1 - N) * (1 - N2) * (1 + (9 * N2 / 4) + (225 * N4 / 64)) * RadRatio;

        // Naming convention: A0..6 are terms in an expression, not powers.
        private const double A0 = 1 - (E2 / 4) - (3 * E4 / 64) - (5 * E6 / 256);
        private const double A2 = (3.0 / 8.0) * (E2 + (E4 / 4) + (15 * E6 / 128));
        private const double A4 = (15.0 / 256.0) * (E4 + (3 * E6 / 4));
        private const double A6 = 35 * E6 / 3072;

        // Calculates and returns the meridian distance.
        // calcM in other implementations.
        private static double CalculateMeridianDistance(double latitude) {
            double latitudeRadian = latitude * RadRatio;
            double meridianDistance = A * ((A0 * latitudeRadian) - (A2 * Math.Sin(2 * latitudeRadian)) +
                                         (A4 * Math.Sin(4 * latitudeRadian)) - (A6 * Math.Sin(6 * latitudeRadian)));
            return meridianDistance;
        }

        /// <summary>
        /// Converts WGS84 latitude and longitude to SVY21 easting and northing
        /// </summary>
        public static void CoordinateToSVY(double latitude, double longitude, out double x, out double y) {
            // Naming convention: sin2Lat = "square of sin(lat)" = Math.pow(sin(lat), 2.0)
            double latR = latitude * RadRatio;
            double sinLat = Math.Sin(latR);
            double sin2Lat = sinLat * sinLat;
            double cosLat = Math.Cos(latR);
            double cos2Lat = cosLat * cosLat;
            double cos3Lat = cos2Lat * cosLat;
            double cos4Lat = cos3Lat * cosLat;
            double cos5Lat = cos3Lat * cos2Lat;
            double cos6Lat = cos5Lat * cosLat;
            double cos7Lat = cos5Lat * cos2Lat;

            double rho = CalculateRadiusOfCurvatureOfMeridian(sin2Lat);
            double v = CalculateRadiusOfCurvatureInPrimeVertical(sin2Lat);
            double psi = v / rho;
            double t = Math.Tan(latR);
            double w = (longitude - OLon) * RadRatio;
            double M = CalculateMeridianDistance(latitude);
            double Mo = CalculateMeridianDistance(OLat);

            // Naming convention: the trailing number is the power of the variable.
            double w2 = w * w;
            double w4 = w2 * w2;
            double w6 = w4 * w2;
            double w8 = w6 * w2;
            double psi2 = psi * psi;
            double psi3 = psi2 * psi;
            double psi4 = psi2 * psi2;
            double t2 = t * t;
            double t4 = t2 * t2;
            double t6 = t4 * t2;

            // Compute Northing.
            // Naming convention: nTerm1..4 are terms in an expression, not powers.
            double nTerm1 = w2 / 2 * v * sinLat * cosLat;
            double nTerm2 = w4 / 24 * v * sinLat * cos3Lat * (4 * psi2 + psi - t2);
            double nTerm3 = w6 / 720 * v * sinLat * cos5Lat * ((8 * psi4) * (11 - 24 * t2) - (28 * psi3) * (1 - 6 * t2) + psi2 * (1 - 32 * t2) - psi * 2 * t2 + t4);
            double nTerm4 = w8 / 40320 * v * sinLat * cos7Lat * (1385 - 3111 * t2 + 543 * t4 - t6);
            double northing = No + K * (M - Mo + nTerm1 + nTerm2 + nTerm3 + nTerm4);

            // Compute Easting.
            // Naming convention: eTerm1..3 are terms in an expression, not powers.
            double eTerm1 = w2 / 6 * cos2Lat * (psi - t2);
            double eTerm2 = w4 / 120 * cos4Lat * ((4 * psi3) * (1 - 6 * t2) + psi2 * (1 + 8 * t2) - psi * 2 * t2 + t4);
            double eTerm3 = w6 / 5040 * cos6Lat * (61 - 479 * t2 + 179 * t4 - t6);
            double easting = Eo + K * v * w * cosLat * (1 + eTerm1 + eTerm2 + eTerm3);

            x = easting;
            y = northing;
        }

        // Calculates and returns the radius of curvature of the meridian.
        // calcRho in other implementations.
        private static double CalculateRadiusOfCurvatureOfMeridian(double sinSquaredLatitude) {
            double numerator = A * (1 - E2);
            double denominator = Math.Pow(1 - E2 * sinSquaredLatitude, 3.0 / 2.0);
            double curvature = numerator / denominator;
            return curvature;
        }

        // Calculates and returns the radius of curvature in the prime vertical.
        // calcV in other implementations.
        private static double CalculateRadiusOfCurvatureInPrimeVertical(double sinSquaredLatitude) {
            double denominator = Math.Sqrt(1 - E2 * sinSquaredLatitude);
            double radius = A / denominator;
            return radius;
        }
    }
}
