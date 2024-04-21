using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.DataSource;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using QuantConnect.Configuration;
using System.Text;

namespace QuantConnect.DataProcessing
{
    public class BenzingaNewsJsonDownloader
    {
        private const string BaseUrl = "https://api.benzinga.com/api/v2";
        private const int RetryMaxCount = 3;
        private const int PageSize = 100;
        private readonly DirectoryInfo _destinationDirectory;
        private readonly RateGate _rateGate;
        private readonly string _apiKey;
        DirectoryInfo dailyRootPath = Directory.CreateDirectory(Config.Get("benzinga-daily-folder", "/tmp/benzinga-daily"));
        // private readonly StringBuilder _stringBuilder = new StringBuilder(3000000);

        public BenzingaNewsJsonDownloader(DirectoryInfo destinationDirectory, string apiKey)
        {
            _destinationDirectory = destinationDirectory ?? throw new ArgumentNullException(nameof(destinationDirectory));
            _apiKey = string.IsNullOrWhiteSpace(apiKey) ? Config.Get("benzinga-news-api-key") : apiKey;
            _rateGate = new RateGate(1, TimeSpan.FromSeconds(0.5));
        }

        public void Download(DateTime startDate, DateTime endDate, bool forceOverwrite = true)
        {
            if (endDate < startDate)
                throw new ArgumentException("End date must be greater than or equal to start date.");

            foreach (var date in Time.EachDay(startDate, endDate))
            {
                var finalDateDirectory = _destinationDirectory.CreateSubdirectory(date.ToStringInvariant("yyyyMMdd"));
                DownloadDataForDate(date, finalDateDirectory, forceOverwrite);
            }
        }

        private void DownloadDataForDate(DateTime date, DirectoryInfo dateDirectory, bool forceOverwrite)
        {
            Log.Trace($"Downloading raw JSON for {date} to {dateDirectory}");
            int page = 0;
            // _stringBuilder.Clear();
            if(!dailyRootPath.Exists)
                dailyRootPath.Create();
            var dailyFileName = new FileInfo(Path.Combine(dailyRootPath.FullName, date.ToStringInvariant("yyyyMMdd") + ".json"));
            if(dailyFileName.Exists)
            {
                Log.Trace($"Daily Benzinga JSON file exists and will be overwritten: {dailyFileName.FullName}");
                dailyFileName.Delete();
            }
            
            while (true)
            {
                string pageJson = DownloadPage(date, page);
                // _stringBuilder.Append(pageJson);

                if (string.IsNullOrEmpty(pageJson)) // If there's no more data to download
                    break;

                ProcessJsonResponse(pageJson, dateDirectory, forceOverwrite);
                page++;
            }
            // Log.Trace($"Writing all raw JSON from Benzinga for {date} to {dailyFileName.FullName}.");
            // using (StreamWriter writer = new StreamWriter(dailyFileName.FullName))
            // {
            //     writer.Write(_stringBuilder.ToString());
            // }
        }

        private string DownloadPage(DateTime date, int page)
        {
            _rateGate.WaitToProceed();
            string jsonResponse;
            string address;
            using (var client = new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.Accept, "application/json");
                address = $"{BaseUrl}/news?token={_apiKey}&pageSize={PageSize}&displayOutput=full&date={date:yyyy-MM-dd}&page={page}";
                Log.Trace($"Downloading from {address}");
                jsonResponse = client.DownloadString(address);
            }
            if (jsonResponse == "[]") 
                jsonResponse = String.Empty;
            return jsonResponse;
        }
        
        private void ProcessJsonResponse(string jsonResponse, DirectoryInfo dateDirectory, bool forceOverwrite)
        {
            var entries = JsonConvert.DeserializeObject<JArray>(jsonResponse)
                .Select(token => BenzingaNewsJsonConverter.DeserializeNews(token, enableLogging: true))
                .OrderBy(news => news.Id)
                .ToList();

            if (!entries.Any())
                throw new Exception($"No parseable data found in JSON response: {jsonResponse}");

            string fileName = $"benzinga_api_{entries.First().Id}_{entries.Last().Id}.json";
            FileInfo jsonFile = new FileInfo(Path.Combine(dateDirectory.FullName, fileName));
            Log.Trace($"Writing {entries.Count} news items to {jsonFile.FullName}");
            WriteToFile(jsonResponse, jsonFile, forceOverwrite);
        }

        private void WriteToFile(string jsonResponse, FileInfo jsonFile, bool overwrite)
        {
            if (jsonFile.Exists && !overwrite)
                throw new IOException($"Raw JSON news file exists and not overwriting: {jsonFile.FullName}");

            File.WriteAllText(jsonFile.FullName, jsonResponse);
        }
    }
}
