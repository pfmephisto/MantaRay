﻿using MantaRay.RadViewer.HeadsUpDisplay;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Rhino.Display;
using Rhino.Geometry;
using Grasshopper.Kernel;

namespace MantaRay.RadViewer
{

    public abstract class RadianceObject
    {
        public string ModifierName;
        public string ObjectType;
        public string Name;
        public RadianceObject Modifier;


        public RadianceObject(string[] data)
        {
            ModifierName = data[0];
            ObjectType = data[1];
            Name = data[2];
        }

        public RadianceObject()
        {

        }

        [Pure]
        public static RadianceObject FromString(string line)
        {
            const string rep_new_line_re = @"/\s\s+/g";

            string[] data = Regex.Replace(line, rep_new_line_re, " ").Trim().Split(' ').Where(d => !String.IsNullOrEmpty(d)).ToArray();

            if (data.Length < 3)
                return null;

            string type = data[1];

            if (type.Length == 0)
                return null;

            switch (type)
            {
                case "polygon":
                    return new RaPolygon(data);
                case "sphere":
                    return new RaSphere(data);
                case "cylinder":
                    return new RaCylinder(data);
                case "tube":
                    return new RaTube(data);
                case "bubble":
                    return new RaBubble(data);
                case "cone":
                case "plastic":
                case "glass":
                case "metal":
                case "trans":
                case "glow":
                case "mirror":
                case "bsdf":
                default:
                    return new RadianceMaterial(data);
            }

        }


    }
}
