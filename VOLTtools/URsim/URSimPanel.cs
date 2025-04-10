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

    public URSimPanel()
    {
        var layout = new StackLayout { Padding = 10 };

        var model = URDFLoader.LoadFromJson("C:\\Users\\Chris\\source\\repos\\VOLTtools\\VOLTtools\\Resources\\ur_description\\urdf\\ur10e.json");
        SimManager.RobotModel = model;
        SimManager.Robot = new SimRobot(model);

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

        if (gp.CommandResult() == Result.Success)
        {
            var target = gp.Point();
            RhinoApp.WriteLine($"Target picked: {target}");

            // Stub: Replace this with your IK logic if available
            RhinoApp.WriteLine("IK solver not yet integrated with RobotModel.");
        }
    }
}


