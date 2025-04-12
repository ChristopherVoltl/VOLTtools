using Rhino;
using System.Collections.Generic;
using System.Linq;

public static class SimManager
{
    public static RobotModel RobotModel { get; private set; }
    public static SimRobot Robot { get; private set; }

    private static bool _loaded = false;

    public static string SelectedTool = "tool0";

    public static List<string> JointNames = new List<string>();

    public static void EnsureLoaded()
    {
        if (_loaded) return;

        RhinoApp.WriteLine("SimManager.EnsureLoaded called");

        string path = @"C:\Users\Chris\source\repos\VOLTtools\VOLTtools\Resources\ur_description\urdf\ur10e.json";
        RobotModel = URDFLoader.LoadFromJson(path);


        JointNames = RobotModel.Joints.Values
           .Where(j => j.Type == "revolute" || j.Type == "continuous")
           .Select(j => j.Name)
           .ToList();

        // Initialize the robot with the loaded model
        Robot = new SimRobot(RobotModel);
        _loaded = true;
    }
}
