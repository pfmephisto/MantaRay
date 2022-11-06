﻿using System;
using System.Collections.Generic;
using System.Text;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Renci.SshNet;
using System.IO;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Drawing;
using GH_IO.Serialization;
using MantaRay.Components;
using MantaRay.Helpers;

namespace MantaRay.Components
{
    public class GH_ObjToRad : GH_Template_SaveStrings
    {
        /// <summary>
        /// Initializes a new instance of the GH_ObjToRad class.
        /// </summary>
        public GH_ObjToRad()
          : base("ObjToRad", "Obj2Rad",
              "1) Copies your local obj files to the linux drive through SSH\n" +
                "2) Runs the obj2rad command and uses the -m argument for parsing the mapping file",
              "2 Radiance")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Obj Files", "Obj Files", "obj files. Can be generated by the mesh2obj component\nIf it's a windows path, they will be uploaded.\n" +
                "If it's a linux path, then the obj2rad command will be called directly.", GH_ParamAccess.list);
            pManager.AddTextParameter("Map File", "Map File", "mapping file. Can be generated by the mesh2obj component\\nIf it's a windows path, they will be uploaded.\n" +
                "If it's a linux path, then obj2rad command will be called directly", GH_ParamAccess.item);
            pManager[pManager.AddTextParameter("Target folder", "Target folder", "Target folder\n" +
                "Examples:\n" +
                "~/simulation/radFiles\n" +
                "C:\\simulation\\radFiles", GH_ParamAccess.item, "")].Optional = true;
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "Status", "status", GH_ParamAccess.item);
            pManager.AddTextParameter("Rad Files", "Rad Files", "Linux path to the rad files", GH_ParamAccess.item);
            pManager.AddTextParameter("Run", "Run", "Run", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {


            if (!CheckIfRunOrUseOldResults(DA, 1)) return; //template

            SSH_Helper sshHelper = SSH_Helper.CurrentFromDocument(OnPingDocument());

            List<string> allFilePaths = DA.FetchList<string>(this, "Obj Files");

            allFilePaths.Add(DA.Fetch<string>(this, "Map File"));
            allFilePaths.Reverse(); //to make sure the map comes first in the upload process.

            List<string> radFilePaths = new List<string>(allFilePaths.Count);

            string subfolderOverride = DA.Fetch<string>(this, "Target folder", "Subfolder Override").ApplyGlobals().TrimEnd('/', '\\');

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < radFilePaths.Count; i++)
            {
                radFilePaths[i] = radFilePaths[i].ApplyGlobals();
            }


            //SSH_Helper.Execute("cd ~ && ls -lah", sb);
            //SSH_Helper.Execute("pwd", sb);

            // quick way to use ist, but not best practice - SshCommand is not Disposed, ExitStatus not checked...
            //sb.AppendLine(cl.CreateCommand("cd ~ && ls -lah").Execute());
            //sb.AppendLine(cl.CreateCommand("pwd").Execute());
            //sb.AppendLine(cl.CreateCommand("cd /tmp/uploadtest && ls -lah").Execute());

            //SSH_Helper.Execute($"pwd", sb);

            string intendedTargetFolder = string.IsNullOrEmpty(subfolderOverride) ? sshHelper.LinuxHome : subfolderOverride;

            string mapfilePath = "";

            for (int i = 0; i < allFilePaths.Count; i++)
            {
                
                string filePath = allFilePaths[i];
                //try
                //{
                string uploadedServerPath = sshHelper.Upload(filePath, intendedTargetFolder, sb);


                //}
                //catch (Renci.SshNet.Common.SftpPathNotFoundException e)
                //{
                //    string er = $"Could not upload files - Path not found ({targetPath})! {e.Message}";
                //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, er);
                //    sb.AppendFormat(er);
                //    break;
                //}


                if (i > 0) // skipping a command at the map file
                {

                    string radFilePath = Path.GetFileNameWithoutExtension(filePath);

                    sshHelper.Execute($"obj2rad -m {mapfilePath} -f {uploadedServerPath} > {uploadedServerPath.Replace(".obj",".rad")}", log: sb.Length < 10 ? sb : null, errors: sb);

                    radFilePaths.Add($"{intendedTargetFolder}/{radFilePath}.rad");
                }
                else
                {
                    mapfilePath = uploadedServerPath.ToLinuxPath();
                }
            }


            OldResults = radFilePaths.ToArray();
            DA.SetData("Status", sb.ToString());
            DA.SetDataList("Rad Files", radFilePaths);

        }


        protected override Bitmap Icon => Resources.Resources.Ra_Rad_Icon;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2832A54F-8FA4-45EB-ACE8-CC7F09BFA930"); }
        }
    }
}