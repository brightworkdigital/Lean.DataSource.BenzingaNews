/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Configuration;
using QuantConnect.DataSource;
using QuantConnect.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace QuantConnect.DataProcessing
{
    public class Program
    {
        public static void Main()
        {
            // var dateValue = Environment.GetEnvironmentVariable("QC_DATAFLEET_DEPLOYMENT_DATE");
            // var date = Parse.DateTimeExact(dateValue, "yyyyMMdd");
            
            var timeUtc = DateTime.UtcNow;
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);
            var endDate = easternTime.AddDays(-1).Date;
            // var startDate = new DateTime(2023, 4, 1);
            var startDate = endDate.AddDays(-3);

            var tempOutputDirectory = Config.Get("temp-output-directory", "/tmp/temp-output-directory");
            var processedDataFolder = Config.Get("processed-data-directory", Path.Combine(Globals.DataFolder, "alternative", "benzinga"));
            var downloadDestinationFolder = Directory.CreateDirectory(Path.Combine(Config.Get("raw-folder", "/tmp/raw"), "alternative", "benzinga"));

            var timer = Stopwatch.StartNew();

            try
            {
                var downloader = new BenzingaNewsJsonDownloader(downloadDestinationFolder, "3dd92888678144968d54c4aa1448e0b9");
                downloader.Download(startDate, endDate);  
            }
            catch (Exception err)
            {
                Log.Error(err, "Downloading failed. Exiting with status code 1");
                Environment.Exit(1);
            }
            
            
            foreach (var date in Time.EachDay(startDate, endDate))
            {
                var converter = new BenzingaNewsDataConverter(
                    new DirectoryInfo(Path.Combine(Config.Get("raw-folder", "/tmp/raw"), "alternative", "benzinga")),
                    new DirectoryInfo(Path.Combine(tempOutputDirectory, "alternative", "benzinga")),
                    new DirectoryInfo(processedDataFolder)
                );

                try
                {
                    if (!converter.Convert(date))
                    {
                        Log.Error($"Alternative.BenzingaNews(): Failed to successfully convert data for date {date:yyyy-MM-dd}. Exiting with status code 1");
                        Environment.Exit(1);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Converter failed. Exiting with status code 1");
                    Environment.Exit(1);
                }

            }

            timer.Stop();
            Log.Trace($"Alternative.BenzingaNews(): Completed processing of data in {timer.Elapsed}");
            Environment.Exit(0);
        }
    }
}
