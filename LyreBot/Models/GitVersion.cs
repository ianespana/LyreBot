﻿using Melanchall.DryWetMidi.Core;
using System.Linq;

namespace NarakaMidiBot.Models
{
    public class GitVersion
    {
        public string tag_name { get; set; }
        public bool draft { get; set; }
        public bool prerelease { get; set; }
    }
}
