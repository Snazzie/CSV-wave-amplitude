using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSV_Convert
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var software = new Software();
            software.Start();
        }

        class Software
        {

            string sourcePath = null;
            string destinationPath = null;
            List<CsvOutStructure> dataList;

            public void Start()
            {
                dataList = new List<CsvOutStructure>();
                var fbd = new FolderBrowserDialog();
                fbd.Description = "Select Directory containing CSV files";


                // Prompt for locations
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    sourcePath = fbd.SelectedPath;
                }

                fbd.Description = "Select where to generate new CSV files";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    destinationPath = fbd.SelectedPath;
                }

                string[] files = Directory.GetFiles(sourcePath, "*.csv*", SearchOption.TopDirectoryOnly); // get files with csv extension only
                var fileCount = files.Count();
                Console.WriteLine(fileCount + " CSV Files Found");

                if (fileCount > 0)
                {

                    int progressCounter = 0;
                    foreach (string filePath in files)
                    {
                        progressCounter++;


                        // if proccess has files
                        ProccessFile(filePath);
                        Console.WriteLine("{0} out of {1} files completed", progressCounter, files.Count());
                    }
                   

                }
                GenerateCSV(dataList);

                Console.WriteLine("Finish");
                Console.Read();

            }


            private void ProccessFile(string filePath)
            {
                bool retry = true;
                while (retry == true)
                {
                    try
                    {
                        double? upper = null;
                        double? equilibrium = null;
                        double? lower = null;

                        using (var reader = new StreamReader(filePath))
                        {
                            double? previousVal = null;
                            double? currentVal = null;


                            int? previousDirection = null; // 0 = flat    1 = up   2 = down   
                            int? newDirection = null;
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                var values = line.Split(',');
                                var tempVal = StringToDouble(values[1]); // will return null if input value is not a number

                                // if is a number
                                if (tempVal != null)
                                {
                                    currentVal = tempVal;

                                    // find out direction on second run
                                    if ((previousVal == null))
                                    {
                                        if (currentVal > previousVal)
                                        {
                                            newDirection = 1;
                                        }
                                        else if (currentVal < previousVal)
                                        {
                                            newDirection = 2;
                                        }
                                        else
                                        {
                                            newDirection = 0;
                                        }

                                    }

                                    // going up
                                    if (previousDirection == 1)
                                    {
                                        //change direction - down
                                        if (currentVal < previousVal)
                                        {
                                            upper = (double)previousVal; // set upper limit with previous value
                                            newDirection = 2;
                                        }
                                        else if (currentVal > previousVal)
                                        {
                                            //newDirection = 1;
                                        }
                                        else if (previousDirection == 0)
                                        {
                                            if (currentVal > previousVal)
                                            {
                                                newDirection = 1;
                                            }
                                            else if (currentVal < previousVal)
                                            {
                                                newDirection = 2;
                                            }
                                            else
                                            {
                                                newDirection = 0;
                                            }
                                        }


                                    }
                                    // going down
                                    else if (previousDirection == 2)
                                    {
                                        //change direction - up
                                        if (currentVal > previousVal)
                                        {

                                            lower = (double)previousVal; // set lower limit with previous value
                                            newDirection = 1;

                                        }
                                        else if (currentVal < previousVal)
                                        {
                                            //newDirection = 1;
                                        }
                                        else if (previousDirection == 0)
                                        {
                                            if (currentVal > previousVal)
                                            {
                                                newDirection = 1;
                                            }
                                            else if (currentVal < previousVal)
                                            {
                                                newDirection = 2;
                                            }
                                            else
                                            {
                                                newDirection = 0;
                                            }
                                        }

                                    }
                                    else if (previousDirection == 0)
                                    {
                                        if (currentVal > previousVal)
                                        {
                                            newDirection = 1;
                                        }
                                        else if (currentVal < previousVal)
                                        {
                                            newDirection = 2;
                                        }
                                        else if (previousDirection == 0)
                                        {
                                            if (currentVal > previousVal)
                                            {
                                                newDirection = 1;
                                            }
                                            else if (currentVal < previousVal)
                                            {
                                                newDirection = 2;
                                            }
                                            else
                                            {
                                                newDirection = 0;
                                            }
                                        }
                                    }



                                    if (upper != null && lower != null)
                                    {
                                        equilibrium = (upper + lower) / 2;
                                        dataList.Add(new CsvOutStructure() { fileName = Path.GetFileName(filePath), peakAmplitude = (double)(upper - equilibrium) });
                                        retry = false;
                                        break; // no further proccessing needed

                                    }
                                }
                                previousDirection = newDirection;
                                previousVal = currentVal;

                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType() == typeof(IOException))
                        {
                            DialogResult dialogResult = MessageBox.Show("Please close file and retry: " + ex.Message, "A file needs to be closed to continue", MessageBoxButtons.RetryCancel);

                            if (dialogResult != DialogResult.Retry)
                            {
                                retry = false;
                            }

                        }
                        else
                        {
                            throw;
                        }

                    }
                }

            }
            private void GenerateCSV(List<CsvOutStructure> list)
            {
                var retry = true;
                while (retry == true)
                {
                    try
                    {
                        using (var file = File.CreateText(destinationPath + "\\Out.csv"))
                        {
                            file.WriteLine("File Name,Peak Amplitude ");
                            var newList = dataList.OrderByDescending(x => x.peakAmplitude);
                            foreach (CsvOutStructure obj in newList)
                            {
                                var line = string.Format("{0},{1}", obj.fileName, obj.peakAmplitude);
                                file.WriteLine(line);
                            }
                        }
                        retry = false;
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType() == typeof(IOException))
                        {
                            DialogResult dialogResult = MessageBox.Show(ex.Message + " Please close the file and press retry to continue", "A file needs to be closed to continue", MessageBoxButtons.RetryCancel);

                            if (dialogResult != DialogResult.Retry)
                            {
                                retry = false;

                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            /// <summary>
            /// used for skipping column text
            /// Converts string to Double
            /// Returns double or null
            /// 
            /// Function will return null if parsed value is not a number
            /// This helps other proccesses to determine whether to add it to the datalist
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            private static double? StringToDouble(string text)
            {
                double number;
                if (Double.TryParse(text, out number))
                {
                    // Console.WriteLine("'{0}' --> {1}", sciNumber, number);
                    return number;
                }
                else
                {
                    // Console.WriteLine("Unable to parse '{0}'.", sciNumber);

                    return null; // return null to stop 
                }
            }
            /// <summary>
            /// Custom class for storing file name and its peak amplitude
            /// </summary>
            class CsvOutStructure
            {
                public string fileName;
                public double peakAmplitude;
            }
        }
    }
}











