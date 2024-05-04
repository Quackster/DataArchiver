using Newtonsoft.Json;
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

                // DownloadPosters(writeDirectory, Path.Combine(writeDirectory, "com", "furnidata.txt"));
                DownloadFurniture(writeDirectory, Path.Combine(writeDirectory, "com", "furnidata.txt"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Done!");
            Console.Read();
        }

        /*
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
        */

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

/*
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

            Downloading.Clear();*/

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

            var furnitureAliases = new Dictionary<string, string>
            {
                { "poster1000", "poster" },
                { "poster1001", "poster" },
                { "poster1002", "poster" },
                { "poster1003", "poster" },
                { "poster1004", "poster" },
                { "poster1005", "poster" },
                { "poster1006", "poster" },
                { "poster10", "poster" },
                { "poster11", "poster" },
                { "poster12", "poster" },
                { "poster13", "poster" },
                { "poster14", "poster" },
                { "poster15", "poster" },
                { "poster16", "poster" },
                { "poster17", "poster" },
                { "poster18", "poster" },
                { "poster19", "poster" },
                { "poster1", "poster" },
                { "poster2000", "poster" },
                { "poster2001", "poster" },
                { "poster2002", "poster" },
                { "poster2003", "poster" },
                { "poster2004", "poster" },
                { "poster2005", "poster" },
                { "poster2006", "poster" },
                { "poster2007", "poster" },
                { "poster2008", "poster" },
                { "poster20", "poster" },
                { "poster21", "poster" },
                { "poster22", "poster" },
                { "poster23", "poster" },
                { "poster24", "poster" },
                { "poster25", "poster" },
                { "poster26", "poster" },
                { "poster27", "poster" },
                { "poster28", "poster" },
                { "poster29", "poster" },
                { "poster2", "poster" },
                { "poster30", "poster" },
                { "poster31", "poster" },
                { "poster32", "poster" },
                { "poster33", "poster" },
                { "poster34", "poster" },
                { "poster35", "poster" },
                { "poster36", "poster" },
                { "poster37", "poster" },
                { "poster38", "poster" },
                { "poster39", "poster" },
                { "poster3", "poster" },
                { "poster40", "poster" },
                { "poster41", "poster" },
                { "poster42", "poster" },
                { "poster43", "poster" },
                { "poster44", "poster" },
                { "poster45", "poster" },
                { "poster46", "poster" },
                { "poster47", "poster" },
                { "poster48", "poster" },
                { "poster49", "poster" },
                { "poster4", "poster" },
                { "poster500", "poster" },
                { "poster501", "poster" },
                { "poster502", "poster" },
                { "poster503", "poster" },
                { "poster504", "poster" },
                { "poster505", "poster" },
                { "poster506", "poster" },
                { "poster507", "poster" },
                { "poster508", "poster" },
                { "poster509", "poster" },
                { "poster50", "poster" },
                { "poster510", "poster" },
                { "poster511", "poster" },
                { "poster512", "poster" },
                { "poster513", "poster" },
                { "poster514", "poster" },
                { "poster515", "poster" },
                { "poster516", "poster" },
                { "poster517", "poster" },
                { "poster518", "poster" },
                { "poster51", "poster" },
                { "poster520", "poster" },
                { "poster521", "poster" },
                { "poster522", "poster" },
                { "poster523", "poster" },
                { "poster52", "poster" },
                { "poster53", "poster" },
                { "poster54", "poster" },
                { "poster55", "poster" },
                { "poster56", "poster" },
                { "poster57", "poster" },
                { "poster58", "poster" },
                { "poster5", "poster" },
                { "poster6", "poster" },
                { "poster7", "poster" },
                { "poster8", "poster" },
                { "poster9", "poster" },
                { "footylamp_campaign_ing", "footylamp" },
                { "easy_bowl", "easy_bowl2" },
                { "ads_cllava", "ads_cllava2" },
                { "rare_icecream_campaign2", "rare_icecream_campaign" },
                { "calippo_cmp", "calippo" },
                { "igor_seatcmp", "igor_seat" },
                { "ads_711c", "ads_711" },
                { "ads_cltele_cmp", "ads_cltele" },
                { "ads_ob_pillowcmp", "ads_ob_pillow" },
                { "ads_711shelfcmp", "ads_711shelf" },
                { "ads_frankbcmp", "ads_frankb" },
                { "ads_grefusa_cactus_camp", "ads_grefusa_cactus" },
                { "ads_cl_jukeb_camp", "ads_cl_jukeb" },
                { "ads_reebok_block2cmp", "ads_reebok_block2" },
                { "ads_cl_sofa_cmp", "ads_cl_sofa" },
                { "ads_calip_colac", "ads_calip_cola" },
                { "ads_calip_chaircmp", "ads_calip_chair" },
                { "ads_calip_pool_cmp", "ads_calip_pool" },
                { "ads_calip_telecmp", "ads_calip_tele" },
                { "ads_calip_parasol_cmp", "ads_calip_parasol" },
                { "ads_calip_lava2", "ads_calip_lava" },
                { "ads_calip_fan_cmp", "ads_calip_fan" },
                { "ads_oc_soda_cmp", "ads_oc_soda" },
                { "ads_1800tele_cmp", "ads_1800tele" },
                { "ads_spang_sleep_cmp", "ads_spang_sleep" },
                { "ads_cl_moodi_camp", "ads_cl_moodi" },
                { "ads_droetker_paula_cmp", "ads_droetker_paula" },
                { "ads_chups_camp", "ads_chups" },
                { "garden_seed_cmp", "garden_seed" },
                { "ads_grefusa_yum_camp", "ads_grefusa_yum" },
                { "ads_cheetos_camp", "ads_cheetos" },
                { "ads_chocapic_camp", "ads_chocapic" },
                { "ads_capri_chair_camp", "ads_capri_chair" },
                { "ads_capri_lava_camp", "ads_capri_lava" },
                { "ads_capri_arcade_camp", "ads_capri_arcade" },
                { "ads_pepsi0_camp", "ads_pepsi0" },
                { "ads_cheetos_hotdog_camp", "ads_cheetos_hotdog" },
                { "ads_cheetos_bath_camp", "ads_cheetos_bath" },
                { "ads_oc_soda_cherry_cmp", "ads_oc_soda_cherry" },
                { "ads_disney_tvcmp", "ads_disney_tv" },
                { "ads_hh_safecmp", "ads_hh_safe" },
                { "ads_sunnyvend_camp", "ads_sunnyvend" },
                { "ads_rangocactus_camp", "ads_rangocactus" },
                { "ads_wowpball_camp", "ads_wowpball" },
                { "ads_suun_camp", "ads_suun" },
                { "ads_liisu_camp", "ads_liisu" },
                { "ads_honeymonster_cmp", "ads_honeymonster" },
                { "ads_ag_crate_camp", "ads_ag_crate" },
                { "ads_dfrisss_camp", "ads_dfrisss" }
            };

            foreach (var alias in furnitureAliases.Where(x => x.Value == sprite))
            {
                DownloadRequest(alias.Key, furniDirectory, unityFurniDirectory, revision, true, sprite);
            }
            
            /*DownloadRequest(sprite + "cmp", furniDirectory, unityFurniDirectory, revision, true, sprite);
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
            }*/
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
