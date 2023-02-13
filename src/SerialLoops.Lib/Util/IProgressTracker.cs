﻿using System;

namespace SerialLoops.Lib.Util
{
    public interface IProgressTracker
    {
        public int Loaded { get; set; } 
        public int Total { get; set; }
        public string CurrentlyLoading { get; set; }

        public void Focus(string item, int count)
        {
            Total += count;
            CurrentlyLoading = item;
        }

    }
}
