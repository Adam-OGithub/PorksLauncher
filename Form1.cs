using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace PorksLauncher
{
    public partial class Form1 : Form
    {

        public class Functions
        {
            public static string GetAppDataFolder()
            {
                return System.Environment.GetEnvironmentVariable("APPDATA"); //Roaming
            }
            public static string GetAppFolder(string appDataFolder)
            {
                return appDataFolder + "\\pork_launcher";
            }
            public static string GetcsvFile(string csvName)
            {
                string appDataFolder = Functions.GetAppDataFolder();
                string appFolder = Functions.GetAppFolder(appDataFolder);
                return appFolder + "\\" + csvName;
            }
          
            public static string[] GetCsvInfo(string csvFile, string selectedText)
            {
         
                string[] outArray = new string[2];
                //Console.WriteLine("csv NAME: " + csvFile);
                //Console.WriteLine("Selected NAME: " + selectedText);
                // Open the file to read from and see if string is found.
                using (StreamReader sr = File.OpenText(csvFile))
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        string[] words = s.Split(',');
                        string entryOne = words[0];
                        string entryTwo = words[1];

                        if (selectedText == entryTwo)
                        {
                            outArray[0] = entryTwo;
                            outArray[1] = entryOne;

                        }
                    }
                    
                }
                return outArray;
            }

            public static class Prompt
            {
                public static string ShowDialog(string text, string caption)
                {
                    Form prompt = new Form()
                    {
                        Width = 300,
                        Height = 150,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        Text = caption,
                        StartPosition = FormStartPosition.CenterScreen
                    };
                    Label textLabel = new Label() { Left = 50, Top = 20, Text = text, Width = 200 };
                    TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 200 };
                    Button confirmation = new Button() { Text = "Ok", Left = 50, Width = 100, Top = 70, DialogResult = DialogResult.OK };
                    Button cancel = new Button() { Text = "Cancel", Left = 150, Width = 100, Top = 70, DialogResult = DialogResult.Cancel };
                    confirmation.Click += (sender, e) => { prompt.Close(); };
                    prompt.Controls.Add(textBox);
                    prompt.Controls.Add(confirmation);
                    prompt.Controls.Add(cancel);
                    prompt.Controls.Add(textLabel);
                    prompt.AcceptButton = confirmation;

                    return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
                }

            }

                public static bool LoadList(bool reload, ListBox listBoxIn)
            {

                if(reload == true && listBoxIn.Items.Count > 1)
                {
                    listBoxIn.Items.Clear();
                }
                string csvFile = Functions.GetcsvFile("launch.csv");

                // Open the file to read from.
                using (StreamReader sr = File.OpenText(csvFile))
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        if(s != "")
                        {
                            string[] words = s.Split(',');
                            string exectuableName = words[1];
                            listBoxIn.Items.Add(exectuableName);
                        }
                        
                    }

                }
                return reload;
            }

            public static string[] getCvsContent(string filePath)
            {
                string[] outArray = new string[25];
                //Console.WriteLine("csv NAME: " + csvFile);
                //Console.WriteLine("Selected NAME: " + selectedText);
                // Open the file to read from and see if string is found.
                using (StreamReader sr = File.OpenText(filePath))
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        string[] words = s.Split(',');
                        
                        for(int i = 0; i < words.Length; i++)
                        {
                            outArray[i] = words[i];
                        }

                        
                    }

                }
                return outArray;
            }

            public static bool createFiles()
            {
                string[] filesCreatedArr = { "launch.csv", "settings.csv" };
    
                //Gets the appDataFolder location
                string appDataFolder = Functions.GetAppDataFolder();

                //Sets the name and create folder for pork_launcher
                string appFolder = appDataFolder + "\\pork_launcher";
                if (!Directory.Exists(appFolder))
                {
                    Directory.CreateDirectory(appFolder);
                }

                //Creates files in array
                for(int i =0; i < filesCreatedArr.Length; i++)
                {
                    if (!File.Exists(filesCreatedArr[i]))
                    {

                        System.IO.File.Create(appFolder + "/" + filesCreatedArr[i]).Dispose(); ;
                    }
                }
             

                return true;
            }

        }

        static class Globals
        {
            // global int
            public static int counter = 0;

            public static string lastValue = "";

            public static string executablePath = "";

        }

        public Form1()
        {
            Functions.createFiles();
            InitializeComponent();
            // settings_btn.Visible= false;
            Functions.LoadList(false,listBox1);
           
        }


        private void btn_add_Click(object sender, EventArgs e)
        {
        
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "executables (*.exe)|*.exe";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var fileContent = string.Empty;
                    var filePath = string.Empty;
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;

                    //Splits file path 
                    string[] words = filePath.Split('\\');
                    //Gets the last entry in filePath
                    string[] executableNameWithExe = words.Last().Split('.');
                    string executableName = executableNameWithExe[0];

                    string csvFile = Functions.GetcsvFile("launch.csv");

                        // Write to file created on launch
                        using (StreamWriter sw = File.AppendText(csvFile))
                        {
                            sw.WriteLine(filePath + "," + executableName);
                        }
                      
                    listBox1.Items.Add(executableName);

                }


            }

        }

        private void btn_rm_Click(object sender, EventArgs e)
        {
           int listIndex = listBox1.SelectedIndex;
            if (listIndex != -1)
            {
                string selectedText = listBox1.GetItemText(listBox1.SelectedItem);
                string csvFile = Functions.GetcsvFile("launch.csv");
                // Open the file to read from and see if string is found.
                var fileInfo = Functions.GetCsvInfo(csvFile, selectedText);
                //if string is found prompt user and remove from file and list
                if (fileInfo.Length > 1) {
                    string message = "Do you want to remove " + selectedText + " from the list?";
                    string title = "Close Window";
                    MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                    DialogResult result = MessageBox.Show(message, title, buttons);
                    if (result == DialogResult.Yes)
                    {
                        var oldLines = System.IO.File.ReadAllLines(csvFile);
                        var newLines = oldLines.Where(line => !line.Contains(selectedText));
                        System.IO.File.WriteAllLines(csvFile, newLines);
                        listBox1.Items.Remove(fileInfo[0]);
                        Globals.counter = 0;
                    }
                }
        
            } else
            {
                MessageBox.Show("No executable selected to remove!");
            }
    


        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedText = listBox1.GetItemText(listBox1.SelectedItem);
            //Make user double click same index to launch app
            //Single click so you can highlight and remove
            if (Globals.counter >= 1 && Globals.lastValue == selectedText)
            {
                Globals.counter = 0;
                //LAUNCH THE APP
                string csvFile = Functions.GetcsvFile("launch.csv");
                // Open the file to read from and see if string is found.
                var fileInfo = Functions.GetCsvInfo(csvFile, selectedText);
                //if string is found prompt user and remove from file and list
                try
                {
                    if (fileInfo.Length > 1)
                    {
                        Process process = new Process();
                        // Configure the process using the StartInfo properties.
                        process.StartInfo.FileName = fileInfo[1];
                        process.Start();
                    }
                }
                catch (Exception error)
                { 
                Console.WriteLine(error.ToString());
                }

            }
            Globals.lastValue = selectedText;
            Console.WriteLine(Globals.counter.ToString());
            Globals.counter++;

        }

        private void rename_btn_Click(object sender, EventArgs e)
        {
            int listIndex = listBox1.SelectedIndex;
            if (listIndex != -1)
            {
                string selectedText = listBox1.GetItemText(listBox1.SelectedItem);
                string csvFile = Functions.GetcsvFile("launch.csv");
                // Open the file to read from and see if string is found.
                var fileInfo = Functions.GetCsvInfo(csvFile, selectedText);
                //if string is found prompt user and rename file in list

                if (fileInfo.Length > 1)
                {
        
                        var oldLines = System.IO.File.ReadAllLines(csvFile);
                       
                        for (int i = 0; i < oldLines.Length; i++)
                        {                  
                            string input = oldLines[i];
                            string[] line = input.Split(',');

                          if (line[1] == selectedText)
                          {
                           string newName = Functions.Prompt.ShowDialog("Please enter a new name for: " + selectedText, "Renaming: " + selectedText);
                            if(newName.Length > 1)
                            {
                                oldLines[i] = line[0] + "," + newName;
                            }
                           
                          }
                       
                        }
 
                        System.IO.File.WriteAllLines(csvFile, oldLines);
                        Functions.LoadList(true, listBox1);


                    Globals.counter = 0;
                    
                }

            }
            else
            {
                MessageBox.Show("No executable selected to rename!");
            }
        }

        private void settings_btn_Click(object sender, EventArgs e)
        {

            string csvFile = Functions.GetcsvFile("settings.csv");
            string[] settingFileContent = Functions.getCvsContent(csvFile);
            
            Form prompt = new Form()
            {
                Width = 400,
                Height = 500,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Settings",
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabelAutoPrompt = new Label() { Left = 100, Top = 20, Text = "Enable/Disable Auto Prompt", Width = 200 };
            Label textLabelAutoFind = new Label() { Left = 100, Top = 45, Text = "Enable/Disable Auto Executable scan", Width = 200 };
            Button confirmation = new Button() { Text = "Ok", Left = 50, Width = 100, Top = 400, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 150, Width = 100, Top = 400, DialogResult = DialogResult.Cancel };
            Button textLabelAutoPrompt_btn = new Button() { Text = "Cancel", Left = 50, Width = 50, Top = 15, DialogResult = DialogResult.OK };
            Button textLabelAutoFind_btn = new Button() { Text = "Cancel", Left = 50, Width = 50, Top = 40, DialogResult = DialogResult.OK };
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.Controls.Add(textLabelAutoPrompt_btn);
            prompt.Controls.Add(textLabelAutoFind_btn);
            prompt.Controls.Add(textLabelAutoPrompt);
            prompt.Controls.Add(textLabelAutoFind);
            prompt.AcceptButton = confirmation;
            //textLabelAutoFind.Click += new EventHandler(do_something);
            confirmation.Click += new EventHandler(save_settings);
            prompt.ShowDialog();
  
           
        }

       private void save_settings(object sender, EventArgs e)
       {
            Console.WriteLine("HELLO");
       }

    }
}
