using Silk.NET.Maths;

namespace Szeminarium1_24_02_17_2
{
    internal class CameraDescriptor
    {
        public double DistanceToOrigin { get; private set; } = 5;
        public double AngleToZYPlane { get; private set; } = 0;
        public double AngleToZXPlane { get; private set; } = 0;
        private const double DistanceScaleFactor = 1.1;
        private const double AngleStep = Math.PI / 36; // 5 fok

        public Vector3D<float> Position => GetPointFromAngles(DistanceToOrigin, AngleToZYPlane, AngleToZXPlane);
        public Vector3D<float> UpVector => Vector3D.Normalize(GetPointFromAngles(1, AngleToZYPlane, AngleToZXPlane + Math.PI / 2));
        public Vector3D<float> Target => Vector3D<float>.Zero;

        public void IncreaseZXAngle() => AngleToZXPlane += AngleStep;
        public void DecreaseZXAngle() => AngleToZXPlane -= AngleStep;
        public void IncreaseZYAngle() => AngleToZYPlane += AngleStep;
        public void DecreaseZYAngle() => AngleToZYPlane -= AngleStep;
        public void IncreaseDistance() => DistanceToOrigin *= DistanceScaleFactor;
        public void DecreaseDistance() => DistanceToOrigin /= DistanceScaleFactor;

        private static Vector3D<float> GetPointFromAngles(double distance, double zyAngle, double zxAngle)
        {
            double x = distance * Math.Cos(zxAngle) * Math.Sin(zyAngle);
            double z = distance * Math.Cos(zxAngle) * Math.Cos(zyAngle);
            double y = distance * Math.Sin(zxAngle);
            return new Vector3D<float>((float)x, (float)y, (float)z);
        }
    }
}