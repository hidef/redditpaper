using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace ConsoleApplication
{
    public class Program
    {
        static HttpClient httpClient = new HttpClient();
        
        public static void Main(string[] args)
        {
            // Configuration
            string subreddit = "astrophotography";
            string outputFolder = "tmp";
            int interval = 2.Minutes();

            // Preparation
            httpClient.DefaultRequestHeaders.Add("User-Agent", "RedditPaper v0.1");

            if ( !Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            while ( true )
            {
                Console.WriteLine("------");

                // Discovery
                HttpResponseMessage response = httpClient.GetAsync($"https://api.reddit.com/r/{subreddit}/top?limit=25").Result;
                string responseDocument = response.Content.ReadAsStringAsync().Result;

                JObject document = JObject.Parse(responseDocument);
                IEnumerable<JToken> tokens = document.SelectTokens("$.data.children..data.url");
        
                // Retrieval
                tokens
                    .Select(t => new Uri(t.Value<string>()))
                    .Where(t => t.LocalPath.ToLower().Contains("jpg"))
                    .ForEach(t => Download(t, outputFolder));

                Thread.Sleep(interval);
            }
        }
    
        private static void Download(Uri uri, string outputFolder) 
        {
            FileInfo fi = new FileInfo(Path.Combine(outputFolder, uri.LocalPath.Substring(1)));
            
            if ( fi.Exists ) return ; // Skip files we've already got, get new ones only.

            HttpResponseMessage response = httpClient.GetAsync(uri).Result;
            Console.WriteLine("FullName: " + fi.FullName);
            using ( var contentStream = response.Content.ReadAsStreamAsync().Result)
            using ( var writeStream = fi.OpenWrite())
            {
                contentStream.CopyTo(writeStream);
            }
        }
    }
    

    public static class UnitExtensions
    {
        public static int Minutes(this int minutes)
        {
            return minutes * 60.Seconds();
        }

        public static int Seconds(this int seconds)
        {
            return seconds * 1000;
        }
    }

    public static class SuperLinq 
    {
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach(T item in enumeration)
            {
                action(item);
            }
        }
    }
}
