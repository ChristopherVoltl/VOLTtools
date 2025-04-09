// UR10eKinematics.cs
using System;
using Rhino.Geometry;

public class UR10eKinematics
{
    public static Transform TransformDH(double a, double alpha, double d, double theta)
    {
        double ct = Math.Cos(theta);
        double st = Math.Sin(theta);
        double ca = Math.Cos(alpha);
        double sa = Math.Sin(alpha);

        var t = Transform.Identity;

        t.M00 = ct;
        t.M01 = -st * ca;
        t.M02 = st * sa;
        t.M03 = a * ct;

        t.M10 = st;
        t.M11 = ct * ca;
        t.M12 = -ct * sa;
        t.M13 = a * st;

        t.M20 = 0;
        t.M21 = sa;
        t.M22 = ca;
        t.M23 = d;

        // Bottom row remains [0, 0, 0, 1]

        return t;
    }

    public static double[] SolvePositionIK(Point3d target)
    {
        // UR10e link lengths (in meters)
        double d1 = 0.1273;
        double a2 = -0.612;
        double a3 = -0.5723;
        double d4 = 0.1639;
        double d5 = 0.1157;
        double d6 = 0.0922;

        // TCP offset from wrist center (along Z6 axis)
        Vector3d tcpOffset = new Vector3d(0, 0, d6);
        Point3d wc = target - tcpOffset;

        // q1: base rotation
        double q1 = Math.Atan2(wc.Y, wc.X);

        // wrist center in base frame
        double r = Math.Sqrt(wc.X * wc.X + wc.Y * wc.Y);
        double z = wc.Z - d1;

        // triangle for q2, q3
        double D = (r * r + z * z - a2 * a2 - a3 * a3) / (2 * a2 * a3);

        if (Math.Abs(D) > 1) return null; // unreachable

        double q3 = Math.Acos(D); // elbow down
        double q2 = Math.Atan2(z, r) - Math.Atan2(a3 * Math.Sin(q3), a2 + a3 * Math.Cos(q3));

        return new double[] { q1, q2, q3, 0, 0, 0 }; // shoulder and elbow only
    }

    public static Transform[] ComputeFK(double[] jointAngles)
    {
        if (jointAngles.Length != 6)
            throw new ArgumentException("Expected 6 joint angles");

        double[] a = { 0, -0.612, -0.5723, 0, 0, 0 };
        double[] alpha = { Math.PI / 2, 0, 0, Math.PI / 2, -Math.PI / 2, 0 };
        double[] d = { 0.1273, 0, 0, 0.1639, 0.1157, 0.0922 };

        Transform[] transforms = new Transform[6];
        Transform current = Transform.Identity;

        for (int i = 0; i < 6; i++)
        {
            var A = TransformDH(a[i], alpha[i], d[i], jointAngles[i]);
            current = current * A;
            transforms[i] = current;
        }

        return transforms;
    }
}
