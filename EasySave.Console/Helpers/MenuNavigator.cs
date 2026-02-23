using System;
using System.Collections.Generic;
using EasySave.Core.Properties;

namespace EasySave.ConsoleUI.Helpers
{
    /// <summary>
    /// Handles console menu navigation with arrow keys and selections.
    /// </summary>
    public class MenuNavigator
    {
        public static void ShowMenu(string title, int selectedIndex, string[] options, string[]? activeMarkers = null)
        {
            Console.Clear();
            Console.WriteLine("-------------------------------------");
            Console.WriteLine(title);
            Console.WriteLine("-------------------------------------");

            for (int i = 0; i < options.Length; i++)
            {
                string activeTag = activeMarkers != null && i < activeMarkers.Length ? activeMarkers[i] : "";
                
                if (i == selectedIndex)
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"> {options[i]}{activeTag}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"  {options[i]}{activeTag}");
                }
            }
            Console.WriteLine("-------------------------------------");
        }

        public static int NavigateMenu(int currentIndex, ConsoleKeyInfo key, int optionsCount)
        {
            if (key.Key == ConsoleKey.UpArrow)
                return (currentIndex == 0) ? optionsCount - 1 : currentIndex - 1;
            else if (key.Key == ConsoleKey.DownArrow)
                return (currentIndex + 1) % optionsCount;
            
            return currentIndex;
        }

        public static void ShowMultiSelectMenu(string title, List<string> items, List<int> selectedIndices, int currentIndex)
        {
            Console.Clear();
            Console.WriteLine(title);
            Console.WriteLine("-------------------------------------");

            if (items.Count == 0)
            {
                Console.WriteLine(Lang.MenuNoItemAvailable);
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                string checkbox = selectedIndices.Contains(i) ? "[X]" : "[ ]";
                string prefix = (i == currentIndex) ? ">" : " ";

                if (i == currentIndex) Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{prefix} {checkbox} {items[i]}");
                Console.ResetColor();
            }
        }

        public static void ShowSingleSelectMenu(string title, List<string> items, int currentIndex, ConsoleColor highlightColor = ConsoleColor.Red)
        {
            Console.Clear();
            Console.WriteLine(title);
            Console.WriteLine("-------------------------------------");

            if (items.Count == 0)
            {
                Console.WriteLine(Lang.MenuNoItemAvailable);
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                string prefix = (i == currentIndex) ? "> " : "  ";
                if (i == currentIndex) Console.ForegroundColor = highlightColor;
                Console.WriteLine($"{prefix}{items[i]}");
                Console.ResetColor();
            }
        }
    }
}
