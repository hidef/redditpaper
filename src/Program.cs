using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace ConsoleApplication
{
    public class Program
    {
        static HttpClient httpClient = new HttpClient();
        
        public static void Main(string[] args)
        {
            string subreddit = "astrophotography";


            httpClient.DefaultRequestHeaders.Add("User-Agent", "RedditPaper v0.1");

            HttpResponseMessage response = httpClient.GetAsync($"https://api.reddit.com/r/{subreddit}").Result;

            string responseDocument = response.Content.ReadAsStringAsync().Result;

            JObject document = JObject.Parse(responseDocument);
            IEnumerable<JToken> tokens = document.SelectTokens("$.data.children..data.url");


            if ( !Directory.Exists("tmp")) 
            {
                Directory.CreateDirectory("tmp");
            }
    
            tokens
                .Select(t => t.Value<string>())
                .Where(t => t.ToLower().Contains("jpg"))
                .ForEach(Download);
        }
    
        private static void Download(string url) 
        {
            Uri uri = new Uri(url);
            HttpResponseMessage response = httpClient.GetAsync(uri).Result;
            FileInfo fi = new FileInfo(Path.Combine("tmp", uri.LocalPath.Substring(1)));
            Console.WriteLine("FullName: " + fi.FullName);
            using ( var contentStream = response.Content.ReadAsStreamAsync().Result)
            using ( var writeStream = fi.OpenWrite())
            {
                contentStream.CopyTo(writeStream);
            }
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
