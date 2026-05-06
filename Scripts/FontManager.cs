using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.IO;
using Library_Management_System.Properties;
using System.Reflection;

namespace Library_Management_System
{
    public class FontManager
    {
        MainForm mainForm;
        string filePath = @"D:\\Visual Studio\\Projects\\Library Management System\\Library Management System\\starz_2016\\starz_2016.ttf";

        // Create a PrivateFontCollection instance
        public PrivateFontCollection pfc = new PrivateFontCollection();

        public bool IsDebugMode()
        {
            bool isDebug = false;

            #if DEBUG
                isDebug = true;
            #else
                isDebug = false;
            #endif

            return isDebug;
        }

        public void Initialize(MainForm main_Form) 
        {
            this.mainForm = main_Form;
            if (IsDebugMode()) 
            {
                filePath = @"D:\\Visual Studio\\Projects\\Library Management System\\Library Management System\\starz_2016\\starz_2016.ttf";
            }
            else 
            {
                filePath = AppDomain.CurrentDomain.BaseDirectory + @"\\\\starz_2016.ttf";
            }
            LoadCustomFont();
            Console.WriteLine("FONTS LOADED");
        }

        public void LoadCustomFont()
        {
            // Check if the file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Font file not found: " + filePath);
            }

            // Load the font from the file
            byte[] fontData = File.ReadAllBytes(filePath);
            IntPtr fontPtr = Marshal.AllocCoTaskMem(fontData.Length);
            Marshal.Copy(fontData, 0, fontPtr, fontData.Length);

            // Add the font to the PrivateFontCollection
            pfc.AddMemoryFont(fontPtr, fontData.Length);

            // Free the unmanaged memory
            Marshal.FreeCoTaskMem(fontPtr);
        }
    }
}
