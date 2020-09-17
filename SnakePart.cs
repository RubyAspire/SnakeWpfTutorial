using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace SnakeGameWPFTutorial
{
    public class SnakePart
    {
        public UIElement uiElement { get; set; }
        public Point Position { get; set; }
        public bool IsHead { get; set; }
    }
}
