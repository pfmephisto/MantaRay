﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace GrasshopperRadianceLinuxConnector
{
    public class GH_MeshToObj : GH_Template
    {
        /// <summary>
        /// Initializes a new instance of the GH_MeshToRad class.
        /// </summary>
        public GH_MeshToObj()
          : base("GH_MeshToRad", "GH_MeshToRad",
              "GH_MeshToRad. Heavily inspired by\n" +
                "https://github.com/ladybug-tools/honeybee-legacy/blob/master/userObjects/Honeybee_MSH2RAD.ghuser",
              "Geometry")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh", GH_ParamAccess.tree); //TODO: change to tree and allow parallel runs
            pManager.AddTextParameter("Name", "Name", "Name (will save name.rad)", GH_ParamAccess.tree);
            pManager.AddTextParameter("ModifierName", "ModifierName", "ModifierName - Name of the radiance material", GH_ParamAccess.tree);
            pManager.AddTextParameter("Local Working Directory", "Local Working Directory", "Working Directory locally on your machine. WINDOWS dir.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Local Files", "Local Files", "Local Files", GH_ParamAccess.list);
            pManager.AddTextParameter("Mapping File Path", "mpf", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Grasshopper.Kernel.Data.GH_Structure<GH_Mesh> inMeshes = DA.FetchTree<GH_Mesh>("Mesh");

            Grasshopper.Kernel.Data.GH_Structure<GH_String> names = DA.FetchTree<GH_String>("Name");

            Grasshopper.Kernel.Data.GH_Structure<GH_String> modifierNames = DA.FetchTree<GH_String>("ModifierName");

            string workingDir = DA.Fetch<string>("Local Working Directory");

            workingDir = (workingDir.EndsWith("\\") || workingDir.EndsWith("/")) ? workingDir : workingDir + "\\";

            string mappingFilePath = workingDir + "mapping.map";

            bool run = DA.Fetch<bool>("Run");

            if (run)
            {
                object myLock = new object();


                // Write the mapping.map file
                
                StringBuilder mapping = new StringBuilder();

                List<string> localFilePaths = new List<string>(inMeshes.Branches.Count);

                for (int i = 0; i < inMeshes.Branches.Count; i++)
                {
                    string modifierName = modifierNames[i][0].Value.Replace(" ", "_");

                    string name = names[i][0].Value.Replace(" ", "_");

                    mapping.AppendFormat("{0} (Group \"{1}\")\n", modifierName, name);

                    localFilePaths.Add(workingDir + name + ".obj");
                }

                System.IO.File.WriteAllText(mappingFilePath, mapping.ToString());



                // Write an obj file for each branch in the meshes list

                //Parallel.For(0, inMeshes.Branches.Count, q =>
                //{
                for (int q = 0; q < inMeshes.Branches.Count; q++)
                {


                    string name = names[q][0].Value.Replace(" ", "_"); //TODO: more fixes?

                    string geometryFilePath = workingDir + $"{name}.obj";

                    StringBuilder geometryFile = new StringBuilder();

                    geometryFile.Append("# Written with GrasshopperRadianceLinuxConnector/GH_MeshToRad\n");

                    geometryFile.AppendFormat("g {0}\n", name);

                    foreach (Mesh mesh in inMeshes[q].Select(m => m.Value))
                    {


                        for (int j = 0; j < mesh.Vertices.Count; j++)
                        {
                            geometryFile.AppendFormat("v {0:0.000} {1:0.000} {2:0.000}\n", mesh.Vertices[j].X, mesh.Vertices[j].Y, mesh.Vertices[j].Z);
                            //TODO: Tolerances/Units?
                        }

                        for (int j = 0; j < mesh.Faces.Count; j++)
                        {
                            if (mesh.Faces[j].IsQuad)
                            {
                                geometryFile.AppendFormat("f {0} {1} {2} {3}\n", mesh.Faces[j].A, mesh.Faces[j].B, mesh.Faces[j].C, mesh.Faces[j].D);
                            }
                            else
                            {
                                geometryFile.AppendFormat("f {0} {1} {2}\n", mesh.Faces[j].A, mesh.Faces[j].B, mesh.Faces[j].C, mesh.Faces[j].D);
                            }
                        }
                    }

                    System.IO.File.WriteAllText(geometryFilePath, geometryFile.ToString());
                }
                //});


                DA.SetDataList("Local File Paths", localFilePaths);
                DA.SetData("Mapping File Path", mappingFilePath);


            }


        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7262DFF6-5027-40E7-A493-840F258EFB83"); }
        }
    }
}