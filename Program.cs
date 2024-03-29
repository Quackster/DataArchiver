﻿using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DataArchiver
{
    public class FurniItem
    {
        public string Type;
        public int SpriteId;
        public string FileName;
        public string Revision;
        public string Unknown;
        public int Length;
        public int Width;
        public string Colour;
        public string Name;
        public string Description;
        public string[] RawData
        {
            get
            {
                return new string[] { Type, Convert.ToString(SpriteId), FileName, Revision, Unknown, Length == -1 ? "" : Convert.ToString(Length), Width == -1 ? "" : Convert.ToString(Width), Colour, Name, Description };
            }
        }

        public bool Ignore;

        public FurniItem(string[] data)
        {
            this.Type = data[0];
            this.SpriteId = int.Parse(data[1]);
            this.FileName = data[2];
            this.Revision = data[3];
            this.Unknown = data[4];
            try
            {
                this.Length = Convert.ToInt32(data[5]);
                this.Width = Convert.ToInt32(data[6]);
            }
            catch (Exception)
            {
                this.Length = -1;
                this.Width = -1;
            }

            this.Colour = data[7];
            this.Name = data[8];
            this.Description = data[9];
        }

        public FurniItem(int SpriteId)
        {
            this.SpriteId = SpriteId;
            this.Ignore = true;
        }
    }

    class Program
    {
        private static HttpClient httpClient = new HttpClient();
        private static List<FurniItem> ItemList;
        private static List<string> Downloading;

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("DataArchiver started");

            var writeDirectory = string.Format("{0}-{1}", "gamedata", DateTime.Now.ToString("yyyy-dd-M"));
            List<string> countryTLDs = new List<string>();

            Downloading = new List<string>();

            try
            {
                countryTLDs.Add("com");
                countryTLDs.Add("de");
                countryTLDs.Add("fi");
                countryTLDs.Add("fr");
                countryTLDs.Add("it");
                countryTLDs.Add("nl");
                countryTLDs.Add("com.br");
                countryTLDs.Add("es");
                countryTLDs.Add("com.tr");

                if (!Directory.Exists(writeDirectory))
                {
                    Directory.CreateDirectory(writeDirectory);

                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.86 Safari/537.36");

                    foreach (var tld in countryTLDs)
                    {
                        var outputDir = Path.Combine(writeDirectory, tld);

                        if (!Directory.Exists(outputDir))
                            Directory.CreateDirectory(outputDir);

                        Console.WriteLine("Downloading external texts for hotel: " + tld);
                        ArchiveExternalTexts(writeDirectory, outputDir, tld);
                    }


                    foreach (var tld in countryTLDs)
                    {
                        var outputDir = Path.Combine(writeDirectory, tld);

                        if (!Directory.Exists(outputDir))
                            Directory.CreateDirectory(outputDir);

                        Console.WriteLine("Downloading external vars for hotel: " + tld);
                        ArchiveExternalVariables(writeDirectory, outputDir, tld);
                    }

                    foreach (var tld in countryTLDs)
                    {
                        var outputDir = Path.Combine(writeDirectory, tld);

                        Console.WriteLine("Downloading external override variables for hotel: " + tld);
                        ArchiveExternalOverrideVariables(writeDirectory, outputDir, tld);
                    }

                    foreach (var tld in countryTLDs)
                    {
                        var outputDir = Path.Combine(writeDirectory, tld);

                        Console.WriteLine("Downloading furnidata texts for hotel: " + tld);
                        ArchiveFurnidataTexts(writeDirectory, outputDir, tld);
                    }

                    foreach (var tld in countryTLDs)
                    {
                        var outputDir = Path.Combine(writeDirectory, tld);

                        Console.WriteLine("Downloading furnidata XML for hotel: " + tld);
                        ArchiveFurnidataXML(writeDirectory, outputDir, tld);
                    }

                    foreach (var tld in countryTLDs)
                    {
                        var outputDir = Path.Combine(writeDirectory, tld);

                        Console.WriteLine("Downloading productdata texts for hotel: " + tld);
                        ArchiveProductdataTexts(writeDirectory, outputDir, tld);
                    }

                    foreach (var tld in countryTLDs)
                    {
                        var outputDir = Path.Combine(writeDirectory, tld);

                        Console.WriteLine("Downloading productdata XML for hotel: " + tld);
                        ArchiveProductdataXML(writeDirectory, outputDir, tld);
                    }
                }

                var officialFileContents = File.ReadAllText(Path.Combine(writeDirectory, "com", "furnidata.txt"));
                officialFileContents = officialFileContents.Replace("]]\n[[", "],[");
                var officialFurnidataList = JsonConvert.DeserializeObject<List<string[]>>(officialFileContents);

                ItemList = new List<FurniItem>();

                foreach (var stringArray in officialFurnidataList)
                {
                    ItemList.Add(new FurniItem(stringArray));
                }

                DownloadPosters(writeDirectory, Path.Combine(writeDirectory, "com", "furnidata.txt"));
                DownloadFurniture(writeDirectory, Path.Combine(writeDirectory, "com", "furnidata.txt"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Done!");
            Console.Read();
        }

        private static void DownloadPosters(string writeDirectory, string furnidata)
        {
            string furniDirectory = Path.Combine(writeDirectory, "swf_furni");
            string unityFurniDirectory = Path.Combine(writeDirectory, "unity_furni");

            if (!Directory.Exists(furniDirectory))
            {
                Directory.CreateDirectory(furniDirectory);
            }

            if (!Directory.Exists(unityFurniDirectory))
            {
                Directory.CreateDirectory(unityFurniDirectory);
            }

            Downloading.Clear();

            string posterRevision = null;

            foreach (var item in ItemList)
            {
                var sprite = item.FileName;

                if (item.FileName.Contains("*"))
                {
                    sprite = item.FileName.Split('*')[0];
                }

                if (sprite != "poster")
                {
                    continue;
                }

                if (Downloading.Contains(sprite))
                {
                    continue;
                }

                if (sprite == "poster")
                {
                    posterRevision = item.Revision;
                    //for (int i = 0; i < 5000; i++)
                    {
                        //sprite = "poster" + i;
                        //Downloading.Add(sprite);
                        //TryDownload(sprite, item.Revision, furniDirectory, false);
                    }
                }

                break;
            }

            if (posterRevision != null)
            {
                string[] lines = File.ReadAllLines(Path.Combine(writeDirectory, "com", "external_flash_texts.txt"));

                foreach (var line in lines)
                {
                    if (!line.StartsWith("poster"))
                        continue;

                    if (!line.Contains("_name="))
                        continue;

                    string equals = line.Substring(0, line.IndexOf('='));
                    string poster = equals.Split('_')[1];

                    TryDownload("poster" + poster, posterRevision, furniDirectory, unityFurniDirectory);
                }
            }
        }

        private static void DownloadFurniture(string writeDirectory, string furnidata)
        {
            string furniDirectory = Path.Combine(writeDirectory, "swf_furni");
            string unityFurniDirectory = Path.Combine(writeDirectory, "unity_furni");

            if (!Directory.Exists(furniDirectory))
            {
                Directory.CreateDirectory(furniDirectory);
            }

            if (!Directory.Exists(unityFurniDirectory))
            {
                Directory.CreateDirectory(unityFurniDirectory);
            }

            Downloading.Clear();

            foreach (var item in ItemList)
            {
                var sprite = item.FileName;

                if (item.FileName.Contains("*"))
                {
                    sprite = item.FileName.Split('*')[0];
                }

                if (Downloading.Contains(sprite))
                {
                    continue;
                }

                if (sprite.ToLower() == "footylamp")
                    TryDownload("footylamp_campaign_ing", item.Revision, furniDirectory, unityFurniDirectory);

                char lastCharacter = sprite[sprite.Length - 1];

                if (Char.IsDigit(lastCharacter))
                {
                    var newSprite = sprite.Substring(0, sprite.Length - 1);
                    TryDownload(newSprite, item.Revision, furniDirectory, unityFurniDirectory);
                }
            }

            Downloading.Clear();

            foreach (var item in ItemList)
            {
                var sprite = item.FileName;

                if (item.FileName.Contains("*"))
                {
                    sprite = item.FileName.Split('*')[0];
                }

                if (Downloading.Contains(sprite))
                {
                    continue;
                }

                TryDownload(sprite, item.Revision, furniDirectory, unityFurniDirectory);
            }
        }

        private static void TryDownload(string sprite, string revision, string furniDirectory, string unityFurniDirectory)
        {
            DownloadRequest(sprite, furniDirectory, unityFurniDirectory, revision);
            DownloadRequest(sprite + "cmp", furniDirectory, unityFurniDirectory, revision, true, sprite);
            DownloadRequest(sprite + "_cmp", furniDirectory, unityFurniDirectory, revision, true, sprite);
            DownloadRequest(sprite + "camp", furniDirectory, unityFurniDirectory, revision, true, sprite);
            DownloadRequest(sprite + "_camp", furniDirectory, unityFurniDirectory, revision, true, sprite);
            DownloadRequest(sprite + "campaign", furniDirectory, unityFurniDirectory, revision, true, sprite);
            DownloadRequest(sprite + "_campaign", furniDirectory, unityFurniDirectory, revision, true, sprite);
            DownloadRequest(sprite + "c", furniDirectory, unityFurniDirectory, revision, true, sprite);
            DownloadRequest(sprite + "_c", furniDirectory, unityFurniDirectory, revision, true, sprite);

            for (int i = 0; i < 10; i++)
            {
                DownloadRequest(string.Format("{0}{1}", sprite, i), furniDirectory, unityFurniDirectory, revision, true, sprite);
            }
        }

        private static void DownloadRequest(string sprite, string furniDirectory, string unityFurniDirectory, string revision, bool isAdvertisement = false, string originalSpriteName = null)
        {
            try
            {
                //Downloading.Add(sprite);

                var writePath = Path.Combine(furniDirectory, sprite + ".swf");

                if (!File.Exists(writePath))
                {
                    var url = "https://images.habbo.com/dcr/hof_furni/" + revision + "/" + sprite + ".swf";

                    var webClient = new WebClient();
                    webClient.DownloadFile(url, writePath);

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Downloaded SWF: " + sprite);
                    Console.ResetColor();
                    
                    if (!ItemList.Select(x => x.FileName.Contains("*") ? x.FileName.Split('*')[0] : x.FileName).Any(x => x == sprite)) 
                    {
                        Log.Information("Downloaded advertisement: " + originalSpriteName + " => " + sprite);
                    }
                }
            }
            catch
            {
                return;
            }
            finally
            {
                //Downloading.Remove(sprite);
            }

            try
            {
                //Downloading.Add(sprite);

                var writePath = Path.Combine(unityFurniDirectory, sprite + ".unity");

                if (!File.Exists(writePath))
                {
                    var url = "https://images.habbo.com/habbo-asset-bundles/dev/2019.3.9f1/Furni/WebGL/" + revision + "/" + sprite;

                    var webClient = new WebClient();
                    webClient.DownloadFile(url, writePath);
    
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Downloaded Unity version: " + sprite);
                    Console.ResetColor();
                }
            }
            catch
            {

            }
        }


        private static void ArchiveExternalVariables(string writeDirectory, string outputDir, string tld)
        {
            string source = getResponse("https://www.habbo." + tld + "/gamedata/external_variables/1");
            var fileVars = Path.Combine(writeDirectory, tld, "external_variables.txt");
            File.WriteAllText(fileVars, source);
        }


        private static void ArchiveExternalOverrideVariables(string writeDirectory, string outputDir, string tld)
        {
            string source = getResponse("https://www.habbo." + tld + "/gamedata/external_override_variables/1");
            var fileVars = Path.Combine(writeDirectory, tld, "external_override_variables.txt");
            File.WriteAllText(fileVars, source);
        }

        private static void ArchiveExternalTexts(string writeDirectory, string outputDir, string tld)
        {
            string source = getResponse("https://www.habbo." + tld + "/gamedata/external_flash_texts/1");
            var fileVars = Path.Combine(writeDirectory, tld, "external_flash_texts.txt");
            File.WriteAllText(fileVars, source);
        }

        private static void ArchiveFurnidataTexts(string writeDirectory, string outputDir, string tld)
        {
            string source = getResponse("https://www.habbo." + tld + "/gamedata/furnidata/1");
            var fileVars = Path.Combine(writeDirectory, tld, "furnidata.txt");
            File.WriteAllText(fileVars, source);
        }

        private static void ArchiveFurnidataXML(string writeDirectory, string outputDir, string tld)
        {
            string source = getResponse("https://www.habbo." + tld + "/gamedata/furnidata_xml/1");
            var fileVars = Path.Combine(writeDirectory, tld, "furnidata.xml");
            File.WriteAllText(fileVars, source);

            var doc = new XmlDocument();
            doc.Load(fileVars);
            doc.Save(fileVars);
        }

        private static void ArchiveProductdataTexts(string writeDirectory, string outputDir, string tld)
        {
            string source = getResponse("https://www.habbo." + tld + "/gamedata/productdata/1");
            var fileVars = Path.Combine(writeDirectory, tld, "productdata.txt");
            File.WriteAllText(fileVars, source);
        }

        private static void ArchiveProductdataXML(string writeDirectory, string outputDir, string tld)
        {
            string source = getResponse("https://www.habbo." + tld + "/gamedata/productdata_xml/1");
            var fileVars = Path.Combine(writeDirectory, tld, "productdata.xml");
            File.WriteAllText(fileVars, source);

            var doc = new XmlDocument();
            doc.Load(fileVars);
            doc.Save(fileVars);
        }

        private static string getResponse(string url)
        {
            HttpResponseMessage res = httpClient.GetAsync(url).GetAwaiter().GetResult();
            string source = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            //archiveFile(url);
            return source;
        }


        private static void archiveFile(string url)
        {
            WebClient webClient = new WebClient();
            //webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.86 Safari/537.36");

            try
            {
                webClient.DownloadString("https://web.archive.org/save/" + url);
            }
            catch { }
        }
    }
}
