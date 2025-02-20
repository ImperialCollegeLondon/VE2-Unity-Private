using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VE2.Core.Common
{
    public static class V_Logger
    {
        public static DebugLogLevel logLevel;

        public static void Message(string message)
        {
            //message = StaticData.fixedUpdateFrame + ": " + message;

            if (Application.isEditor)
                Debug.Log(message % Colorize.Green % FontFormat.Bold);
            else
                Debug.Log(GetTime() + " - " + message);
        }
        public static void Warning(string message, string stackTrace = null)
        {
            //string formattedMessage = StaticData.fixedUpdateFrame + ": ";
            string formattedMessage = message;

            if (Application.isEditor)
                formattedMessage += message % Colorize.Orange % FontFormat.Bold;
            else
                formattedMessage += GetTime() + " - " + "WARN: " + message;

            if (stackTrace != null)
            {
                formattedMessage += "\n" + stackTrace;
            }

            Debug.LogWarning(formattedMessage);
        }

        public static void Error(string message, string stackTrace = null)
        {
            //string formattedMessage = StaticData.fixedUpdateFrame + ": ";
            string formattedMessage = message;

            if (Application.isEditor)
                formattedMessage += message % Colorize.Red % FontFormat.Bold;
            else
                formattedMessage += GetTime() + " - " + "ERROR: " + message;

            if (stackTrace != null)
            {
                formattedMessage += "\n" + stackTrace;
            }

            Debug.LogError(formattedMessage);
        }

        public static void Dev(string message)
        {
            if (Application.isEditor && logLevel == DebugLogLevel.Normal)
                return;

            //message = StaticData.fixedUpdateFrame + ": " + message;

            if (Application.isEditor)
                Debug.Log(message % Colorize.Cyan % FontFormat.Bold);
            else
                Debug.Log(GetTime() + " - " + "DEV: " + message);
        }

        public static void Network(string message)
        {
            if (Application.isEditor && logLevel == DebugLogLevel.Normal)
                return;

            //message = StaticData.fixedUpdateFrame + ": " + message;

            if (Application.isEditor)
                Debug.Log(message % Colorize.Magenta % FontFormat.Bold);
            else
                Debug.Log(GetTime() + " - " + "NETWORK: " + message);
        }

        public static void Verbose(string message)
        {
            if (Application.isEditor && logLevel != DebugLogLevel.Full)
                return;

            //message = StaticData.fixedUpdateFrame + ": " + message;

            if (Application.isEditor)
                Debug.Log(message % Colorize.Blue % FontFormat.Bold);
            else
                Debug.Log(GetTime() + " - " + "VERBOSE: " + message);
        }

        //We want
        //one that throws an error, has ok, and always quits
        //One that throws an error, quits if ok, cancel if continue anywa

        public static void Alert(string message, string okText = "OK", bool quit = false)
        {
            if (!Application.isEditor)
                return;

#if UNITY_EDITOR
            EditorUtility.DisplayDialog("ViRSE Alert", message, okText);
            if (quit)
                UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public static void AlertOptionalQuit(string message, string okText = "OK, I'll fix that now", string cancelText = "Continue anyway")
        {
            if (!Application.isEditor)
                return;

#if UNITY_EDITOR
            if (EditorUtility.DisplayDialog("ViRSE Alert", message, okText, cancelText))
                UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private static string GetTime()
        {
            return DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second;
        }

        public class Colorize
        {

            // Color Example

            public static Colorize Red = new Colorize(Color.red);
            public static Colorize Yellow = new Colorize(Color.yellow);
            public static Colorize Green = new Colorize(Color.green);
            public static Colorize Blue = new Colorize(Color.blue);
            public static Colorize Cyan = new Colorize(Color.cyan);
            public static Colorize Magenta = new Colorize(Color.magenta);

            // Hex Example

            public static Colorize Orange = new Colorize("#FFA500");
            public static Colorize Olive = new Colorize("#808000");
            public static Colorize Purple = new Colorize("#800080");
            public static Colorize DarkRed = new Colorize("#8B0000");
            public static Colorize DarkGreen = new Colorize("#006400");
            public static Colorize DarkOrange = new Colorize("#FF8C00");
            public static Colorize Gold = new Colorize("#FFD700");

            private readonly string _prefix;

            private const string Suffix = "</color>";

            // Convert Color to HtmlString
            private Colorize(Color color)
            {
                _prefix = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>";
            }
            // Use Hex Color
            private Colorize(string hexColor)
            {
                _prefix = $"<color={hexColor}>";
            }

            public static string operator %(string text, Colorize color)
            {
                return color._prefix + text + Suffix;
            }


        }

        public class FontFormat
        {
            private string _prefix;

            private string _suffix;

            public static FontFormat Bold = new FontFormat("b");
            public static FontFormat Italic = new FontFormat("i");
            private FontFormat(string format)
            {
                _prefix = $"<{format}>";
                _suffix = $"</{format}>";
            }

            public static string operator %(string text, FontFormat textFormat)
            {
                return textFormat._prefix + text + textFormat._suffix;
            }
        }
    }

    public enum DebugLogLevel
    {
        Normal,
        Increased,
        Full
    }
}