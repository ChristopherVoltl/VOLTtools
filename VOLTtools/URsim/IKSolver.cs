// Damped Least Squares Inverse Kinematics Solver for UR10e
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Rhino;
using Rhino.Geometry;

public static class IKSolver
{
    public static float[] SolveIK(RobotModel model, string baseLink, string endEffectorLink, Point3d target, float[] initialGuess = null)
    {
        const int maxIterations = 200;
        const double threshold = 0.001;
        const double stepSize = 1.0;
        const double lambda = 0.01;

        var joints = FKEngine.GetOrderedJoints(model, baseLink);
        int dof = joints.Count;

        float[] angles = initialGuess != null ? (float[])initialGuess.Clone() : new float[dof];

        for (int iter = 0; iter < maxIterations; iter++)
        {
            var jointDict = joints.Select((j, i) => new { j.Name, Value = angles[i] })
                                  .ToDictionary(x => x.Name, x => x.Value);

            var fk = FKEngine.ComputeFK(model, jointDict, baseLink);
            if (!fk.TryGetValue(endEffectorLink, out var eeTransform))
                return null;

            Point3d currentPos = eeTransform * Point3d.Origin;
            Vector3d error = target - currentPos;

            if (error.Length < threshold)
            {
                RhinoApp.WriteLine($"✅ Converged in {iter} iterations.");
                return angles;
            }

            // Approximate Jacobian
            var J = new Matrix(3, dof);
            for (int i = 0; i < dof; i++)
            {
                float original = angles[i];
                angles[i] += 0.0001f;

                var perturbed = joints.Select((j, k) => new { j.Name, Value = angles[k] })
                                      .ToDictionary(x => x.Name, x => x.Value);
                var perturbedFK = FKEngine.ComputeFK(model, perturbed, baseLink);
                Point3d perturbedPos = perturbedFK[endEffectorLink] * Point3d.Origin;

                Vector3d grad = (perturbedPos - currentPos) / 0.0001;
                J[0, i] = grad.X;
                J[1, i] = grad.Y;
                J[2, i] = grad.Z;

                angles[i] = original;
            }

            // Damped least squares: Δθ = Jᵗ * (J*Jᵗ + λ²I)⁻¹ * error
            Matrix JT = J.Duplicate();
            JT.Transpose();
            Matrix JJt = J * JT;

            // Add damping manually (damping = λ² * I)
            for (int i = 0; i < JJt.RowCount; i++)
            {
                JJt[i, i] += lambda * lambda;
            }

            // Invert JJt
            Matrix inv = JJt.Duplicate();
            bool success = inv.Invert(.001);
            if (!success)
            {
                RhinoApp.WriteLine("❌ Failed to invert matrix.");
                return null;
            }
            Matrix errorVec = new Matrix(3, 1);
            errorVec[0, 0] = error.X;
            errorVec[1, 0] = error.Y;
            errorVec[2, 0] = error.Z;

            Matrix dTheta = JT * inv * errorVec;

            for (int i = 0; i < dof; i++)
                angles[i] += (float)(stepSize * dTheta[i, 0]);

            RhinoApp.WriteLine($"[IK] Iter {iter}: Error = {error.Length:0.0000}");
        }

        RhinoApp.WriteLine("❌ IK failed to converge.");
        return null;
    }
}


