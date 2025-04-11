using Rhino;

public static class SimManager
{
    public static RobotModel RobotModel { get; private set; }
    public static SimRobot Robot { get; private set; }

    private static bool _loaded = false;

    public static void EnsureLoaded()
    {
        if (_loaded) return;

        RhinoApp.WriteLine("🧠 SimManager.EnsureLoaded called");

        string path = @"C:\Users\Chris\source\repos\VOLTtools\VOLTtools\Resources\ur_description\urdf\ur10e.json";
        RobotModel = URDFLoader.LoadFromJson(path);
        Robot = new SimRobot(RobotModel);
        _loaded = true;
    }
}
