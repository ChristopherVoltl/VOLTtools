using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System;
using System.Collections.Generic;

namespace VOLTtools
{
    public class VOLTtools : Command
    {
        public VOLTtools()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static VOLTtools Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "VOLTtools";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                var model = URDFLoader.LoadFromJson("C:\\Users\\Chris\\source\\repos\\VOLTtools\\VOLTtools\\Resources\\ur_description\\urdf\\ur10e.json");
                var simRobot = new SimRobot(model);

                SimManager.Robot = simRobot;

                if (model == null)
                {
                    RhinoApp.WriteLine("Model is null!");
                    return Result.Failure;
                }

                var jointValues = new Dictionary<string, float>
                {
                    { "shoulder_pan_joint", 0.5f },
                    { "shoulder_lift_joint", -0.3f },
                    { "elbow_joint", 1.0f },
                    { "wrist_1_joint", 0 },
                    { "wrist_2_joint", 0 },
                    { "wrist_3_joint", 0 }
                };

                var transforms = FKEngine.ComputeFK(model, jointValues, "base_link");
                //FKEngine.DrawRobot(model, transforms);
                simRobot.Update(transforms);
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Exception: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}
