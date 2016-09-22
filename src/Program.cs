using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Security.Cryptography;
using System.Text;

namespace ConsoleApplication
{
    public class Program
    {
        static HttpClient httpClient = new HttpClient();
        
        public static void Main(string[] args)
        {
            // Configuration
            string subreddit = Environment.GetEnvironmentVariable("subreddit") ?? "astrophotography";
            string outputFolder = Environment.GetEnvironmentVariable("outputPath") ?? "tmp";
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
            string fileName = uri.LocalPath;
            using (MD5 md5Hash = MD5.Create())
            {
                fileName = GetMd5Hash(md5Hash, fileName);
            }
            string tempFileName = Path.Combine(outputFolder, fileName);
            FileInfo fi = new FileInfo(tempFileName + ".jpg");

            if ( fi.Exists ) return ; // Skip files we've already got, get new ones only.

            HttpResponseMessage response = httpClient.GetAsync(uri).Result;
            Console.WriteLine("FullName: " + fi.FullName);
            using ( var contentStream = response.Content.ReadAsStreamAsync().Result)
            using ( var writeStream = fi.OpenWrite())
            {
                contentStream.CopyTo(writeStream);
            }
        }

        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
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
