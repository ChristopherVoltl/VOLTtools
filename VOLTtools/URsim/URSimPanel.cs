// URSimPanel.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System.Runtime.InteropServices;

[Guid("B7CBE031-8DA7-4A41-83D5-939A328C9F14")]
public class URSimPanel : Panel
{
    private List<Slider> sliders = new List<Slider>();
    
    private DropDown toolSelector;
    private string SelectedTool => toolSelector.SelectedValue as string ?? "tool0";

    public URSimPanel()
    {
        var layout = new StackLayout { Padding = 10 };

        RhinoApp.WriteLine("🧩 URSimPanel constructor called");

        SimManager.EnsureLoaded();

        toolSelector = new DropDown
        {
            Width = 100,
            DataStore = new[] { "tool0", "flange", "wrist_3_link" },
            SelectedIndex = 0
        };

        layout.Items.Add(new Label { Text = "Select Tool Frame:" });
        layout.Items.Add(toolSelector);


        for (int i = 0; i < 6; i++)
        {
            var s = new Slider { MinValue = -180, MaxValue = 180, Value = 0, Width = 250 };
            s.ValueChanged += (sender, e) => UpdateVisualization();
            layout.Items.Add(new Label { Text = $"Joint {i + 1}" });
            layout.Items.Add(s);
            sliders.Add(s);
        }

        var simulateButton = new Button { Text = "Simulate" };
        simulateButton.Click += (s, e) => UpdateVisualization();

        var targetButton = new Button { Text = "Pick Target Point" };
        targetButton.Click += (s, e) => PickTarget();

        layout.Items.Add(simulateButton);
        layout.Items.Add(targetButton);
        Content = layout;

    }


    private void UpdateVisualization()
    {
        double[] angles = sliders.Select(sl => RhinoMath.ToRadians(sl.Value)).ToArray();

        var jointAngles = new Dictionary<string, float>
    {
        { "shoulder_pan_joint", (float)angles[0] },
        { "shoulder_lift_joint", (float)angles[1] },
        { "elbow_joint", (float)angles[2] },
        { "wrist_1_joint", (float)angles[3] },
        { "wrist_2_joint", (float)angles[4] },
        { "wrist_3_joint", (float)angles[5] }
    };

        var transforms = FKEngine.ComputeFK(SimManager.RobotModel, jointAngles, "base_link");
        SimManager.Robot.Update(transforms);
    }

    private void PickTarget()
    {
        var gp = new GetPoint();
        gp.SetCommandPrompt("Pick target position for TCP");
        gp.Get();

        if (gp.CommandResult() != Result.Success)
            return;

        var target = gp.Point();
        RhinoApp.WriteLine($"Target picked: {target}");

        float[] initial = sliders.Select(sl => (float)RhinoMath.ToRadians(sl.Value)).ToArray();
        var initialGuess = sliders.Select(sl => (float)RhinoMath.ToRadians(sl.Value)).ToArray();

        var solution = IKSolver.SolveIK(
                     SimManager.RobotModel,
                     "base_link",
                     SelectedTool,
                     target,
                     initialGuess);


        if (solution != null)
        {
            for (int i = 0; i < sliders.Count; i++)
                sliders[i].Value = (int)RhinoMath.ToDegrees(solution[i]);

            var jointAngles = new Dictionary<string, float>
        {
            { "shoulder_pan_joint", solution[0] },
            { "shoulder_lift_joint", solution[1] },
            { "elbow_joint", solution[2] },
            { "wrist_1_joint", solution[3] },
            { "wrist_2_joint", solution[4] },
            { "wrist_3_joint", solution[5] },
        };

            var transforms = FKEngine.ComputeFK(SimManager.RobotModel, jointAngles, "base_link");
            SimManager.Robot.Update(transforms);
        }
        else
        {
            RhinoApp.WriteLine("Target unreachable or IK failed to converge.");
        }
        RhinoDoc.ActiveDoc.Objects.AddPoint(target);
    }

}


