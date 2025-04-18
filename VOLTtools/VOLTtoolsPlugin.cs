﻿using Rhino;
using Rhino.PlugIns;
using Rhino.UI;
using System;
using System.Drawing;

namespace VOLTtools
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class VOLTtoolsPlugin : Rhino.PlugIns.PlugIn
    {
        public VOLTtoolsPlugin()
        {
            Instance = this;
        }

        ///<summary>Gets the only instance of the VOLTtoolsPlugin plug-in.</summary>
        public static VOLTtoolsPlugin Instance { get; private set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {

            try
            {
                Panels.RegisterPanel(this, typeof(URSimPanel), "UR10e Simulator", null);
                Panels.RegisterPanel(this, typeof(CurvePathAnimatorPanel), "Curve Animator", null);
                return LoadReturnCode.Success;
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                return LoadReturnCode.ErrorShowDialog;
            }
          
            

        }
    }
}