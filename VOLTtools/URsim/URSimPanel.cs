// URSimPanel.cs
using Eto.Forms;
using Eto.Drawing;
using System.Linq;
using Rhino;
using Rhino.Input.Custom;
using System.Collections.Generic;

public class URSimPanel : Dialog
{
    private List<Slider> sliders = new List<Slider>();

    public URSimPanel()
    {
        Title = "UR10e Control Panel";
        ClientSize = new Size(300, 450);
        this.WindowStyle = WindowStyle.Utility;
        this.Resizable = false;

        var layout = new StackLayout { Padding = 10 };

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
        double[] angles = sliders.Select(sl => Rhino.RhinoMath.ToRadians(sl.Value)).ToArray();
        var fk = UR10eKinematics.ComputeFK(angles);
        URVisualizer.DrawRobot(fk);
    }

    private void PickTarget()
    {
        var gp = new GetPoint();
        gp.SetCommandPrompt("Pick target position for TCP");
        gp.Get();

        if (gp.CommandResult() == Rhino.Commands.Result.Success)
        {
            var target = gp.Point();
            var solution = UR10eKinematics.SolvePositionIK(target);
            if (solution != null)
            {
                var fk = UR10eKinematics.ComputeFK(solution);
                URVisualizer.DrawRobot(fk);
            }
            else
            {
                RhinoApp.WriteLine("Target unreachable.");
            }
        }
    }
}
