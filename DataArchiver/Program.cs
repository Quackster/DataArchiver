using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DataArchiver
{
    class Program
    {
        private static HttpClient httpClient = new HttpClient();

        static void Main(string[] args)
        {
            var writeDirectory = string.Format("{0}-{1}", "gamedata", DateTime.Now.ToString("yyyy-dd-M"));
            List<string> countryTLDs = new List<string>();

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

                if (Directory.Exists(writeDirectory))
                    Directory.Delete(writeDirectory, true);

                if (!Directory.Exists(writeDirectory))
                    Directory.CreateDirectory(writeDirectory);

                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.86 Safari/537.36");

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

                    Console.WriteLine("Downloading external texts for hotel: " + tld);
                    ArchiveExternalTexts(writeDirectory, outputDir, tld);
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Done!");
            Console.Read();
        }

        private static void ArchiveExternalVariables(string writeDirectory, string outputDir, string tld)
        {
            HttpResponseMessage res = httpClient.GetAsync("https://www.habbo." + tld + "/gamedata/external_variables/1").GetAwaiter().GetResult();
            string source = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var fileVars = Path.Combine(writeDirectory, tld, "external_variables.txt");
            File.WriteAllText(fileVars, source);
        }

        private static void ArchiveExternalTexts(string writeDirectory, string outputDir, string tld)
        {
            HttpResponseMessage res = httpClient.GetAsync("https://www.habbo." + tld + "/gamedata/external_flash_texts/1").GetAwaiter().GetResult();
            string source = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var fileVars = Path.Combine(writeDirectory, tld, "external_flash_texts.txt");
            File.WriteAllText(fileVars, source);
        }

        private static void ArchiveFurnidataTexts(string writeDirectory, string outputDir, string tld)
        {
            HttpResponseMessage res = httpClient.GetAsync("https://www.habbo." + tld + "/gamedata/furnidata/1").GetAwaiter().GetResult();
            string source = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var fileVars = Path.Combine(writeDirectory, tld, "furnidata.txt");
            File.WriteAllText(fileVars, source);
        }

        private static void ArchiveFurnidataXML(string writeDirectory, string outputDir, string tld)
        {
            HttpResponseMessage res = httpClient.GetAsync("https://www.habbo." + tld + "/gamedata/furnidata_xml/1").GetAwaiter().GetResult();
            string source = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var fileVars = Path.Combine(writeDirectory, tld, "furnidata_xml.xml");
            File.WriteAllText(fileVars, source);
        }

        private static void ArchiveProductdataTexts(string writeDirectory, string outputDir, string tld)
        {
            HttpResponseMessage res = httpClient.GetAsync("https://www.habbo." + tld + "/gamedata/productdata/1").GetAwaiter().GetResult();
            string source = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var fileVars = Path.Combine(writeDirectory, tld, "furnidata.txt");
            File.WriteAllText(fileVars, source);
        }

        private static void ArchiveProductdataXML(string writeDirectory, string outputDir, string tld)
        {
            HttpResponseMessage res = httpClient.GetAsync("https://www.habbo." + tld + "/gamedata/productdata_xml/1").GetAwaiter().GetResult();
            string source = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var fileVars = Path.Combine(writeDirectory, tld, "furnidata_xml.xml");
            File.WriteAllText(fileVars, source);
        }
    }
}
