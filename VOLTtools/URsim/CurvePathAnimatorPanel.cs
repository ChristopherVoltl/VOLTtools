// CurvePathAnimatorPanel.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Eto.Drawing;
using Rhino;
using Rhino.Geometry;
using Rhino.Input;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

[Guid("E4E1F83B-9D76-4C47-9381-54F7A4E6CDE0")]

public class CurvePathAnimatorPanel : Panel
{
    private Button selectCurveButton;
    private Slider divisionSlider;
    private Button playButton;
    private Slider frameSlider;

    private Curve selectedCurve;
    private List<Plane> pathPlanes = new List<Plane>();
    private int currentFrame = 0;
    private bool isPlaying = false;
    private UITimer animationTimer;

    public CurvePathAnimatorPanel()
    {
        var layout = new StackLayout { Padding = 10 };

        selectCurveButton = new Button { Text = "Select Curve" };
        selectCurveButton.Click += (s, e) => SelectCurve();

        divisionSlider = new Slider { MinValue = 2, MaxValue = 100, Value = 20 };
        divisionSlider.ValueChanged += (s, e) => GeneratePlanes();

        playButton = new Button { Text = "Play" };
        playButton.Click += (s, e) => TogglePlay();

        frameSlider = new Slider { MinValue = 0, MaxValue = 19, Value = 0 };
        frameSlider.ValueChanged += (s, e) => UpdateRobotPose();

        animationTimer = new UITimer { Interval = 0.05 }; // 20 fps
        animationTimer.Elapsed += (s, e) => AdvanceFrame();

        layout.Items.Add(selectCurveButton);
        layout.Items.Add(new Eto.Forms.Label { Text = "Division Count" });
        layout.Items.Add(divisionSlider);
        layout.Items.Add(playButton);
        layout.Items.Add(frameSlider);

        Content = layout;
    }

    private void SelectCurve()
    {
        var rc = RhinoGet.GetOneObject("Select curve path", false, Rhino.DocObjects.ObjectType.Curve, out var objRef);
        if (rc != Rhino.Commands.Result.Success || objRef == null) return;

        selectedCurve = objRef.Curve();
        GeneratePlanes();
    }

    private void GeneratePlanes()
    {
        if (selectedCurve == null) return;

        int count = divisionSlider.Value;
        pathPlanes = new List<Plane>();

        double t0 = selectedCurve.Domain.T0;
        double t1 = selectedCurve.Domain.T1;

        for (int i = 0; i < count; i++)
        {
            double t = t0 + (t1 - t0) * i / (count - 1);
            Point3d pt = selectedCurve.PointAt(t);
            Vector3d tan = selectedCurve.TangentAt(t);
            if (!tan.Unitize()) tan = Vector3d.ZAxis;
            Plane plane = new Plane(pt, tan);
            pathPlanes.Add(plane);
        }

        frameSlider.MaxValue = count - 1;
        frameSlider.Value = 0;
        UpdateRobotPose();
    }

    private void TogglePlay()
    {
        isPlaying = !isPlaying;
        playButton.Text = isPlaying ? "Pause" : "Play";
        if (isPlaying) animationTimer.Start();
        else animationTimer.Stop();
    }

    private void AdvanceFrame()
    {
        if (pathPlanes.Count == 0) return;

        currentFrame = (currentFrame + 1) % pathPlanes.Count;
        frameSlider.Value = currentFrame;
        UpdateRobotPose();
    }

    private void UpdateRobotPose()
    {
        if (pathPlanes.Count == 0 || SimManager.RobotModel == null) return;

        currentFrame = frameSlider.Value;
        Plane target = pathPlanes[currentFrame];
        Point3d tcp = target.Origin;

        var joints = FKEngine.GetOrderedJoints(SimManager.RobotModel, "base_link");
        var guess = SimManager.Robot.GetCurrentJointAngles();
        float[] guess1 = joints.Select(j => guess.ContainsKey(j.Name) ? guess[j.Name] : 0f).ToArray();

        var solution = IKSolver.SolveIK(SimManager.RobotModel, "base_link", SimManager.SelectedTool, tcp, guess1);

        if (solution != null)
        {
            var transformMap = FKEngine.ComputeFK(SimManager.RobotModel, solution.Select((v, i) => new { Name = SimManager.JointNames[i], Value = v })
            .ToDictionary(x => x.Name, x => x.Value), "base_link");
            SimManager.Robot.Update(transformMap);
        }
        else
        {
            RhinoApp.WriteLine("IK failed at frame {0}", currentFrame);
        }
    }
}

