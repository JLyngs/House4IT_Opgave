using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Diagnostics;

namespace Prisliste
{

    class Program
    {

        static void Main(string[] args)
        {

            try // Koden køres, skulle fejl ske vil de fanges i catch
            {

                var prisliste1 = ReadPrisliste("../../../Prislister/prisliste1.csv"); // Sti til fil_1
                var prisliste2 = ReadPrisliste("../../../Prislister/prisliste2.csv"); // Sti til fil_2


                var opdateretPrisliste1 = BeregnSalgsPris(prisliste1); // Beregner salgsprisen
                var opdateretPrisliste2 = BeregnSalgsPris(prisliste2);

                BlackTilSort(opdateretPrisliste1); //Udskifter alle "black" med "sort" i prislisten
                BlackTilSort(opdateretPrisliste2);

                UdskrivCSV([.. opdateretPrisliste1, .. opdateretPrisliste2], "../../../Prislister/ny_prisliste.csv"); // Udskriver de to opdaterede prislister til en ny CSV fil

                Console.WriteLine("Filen er nu oprettet i mappen Prislister."); //Udskriver besked til brugeren om filoprettelsen

                Console.ReadKey(); // Venter på input fra brugeren før console lukker.

            }
            catch (Exception ex) // Håndterer fejl
            {
                Console.WriteLine($"Fejl: {ex.Message}"); // Udskriver fejlbeskeden
            }

        }


        static List<Varer> ReadPrisliste(string filnavn) //Method til at læse CSV filen
        {

            using var reader = new StreamReader(filnavn); // Åbner CSV filen for Read

            using var csv = new CsvReader(reader, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) { Delimiter = ";" }); // Læser filen - - Delimiter sættes til semikolon

            var records = csv.GetRecords<Varer>().ToList(); // Gemmer indholdet af CSV filen i en liste

            return records; // Returnerer listen

        }

        static List<Varer> BeregnSalgsPris(List<Varer> prisliste)
        {
            decimal ValutaKurs = 7.45m; // Valutakursen for EUR til DKK, Satisk Valuta.. - Kan ændres til dynamisk valutakurs med API
            foreach (var item in prisliste) // Gennemgpår listen
            {
                item.KostprisEUR = item.KostprisEUR?.Replace(" €", "").Trim();
                decimal.TryParse(item.KostprisEUR, out decimal kostprisEUR); // Fjerner "€" og trimmer whitespace fra kostpris
                decimal kostprisDKK = kostprisEUR * ValutaKurs; // Beregner kostpris i DKK

                // tildel prisgruppe til item
                item.BeregnSalgsprisDKK = TildelPrisGruppe(item.PriceGroup.ToString()); // Tildeler prisgruppe til item
                if (item.BeregnSalgsprisDKK != "N/A") // Hvis der er en prisgruppe
                {
                    decimal salgspris = kostprisDKK * (1 + (decimal.Parse(item.BeregnSalgsprisDKK) / 100)); // Beregner salgspris
                    item.BeregnSalgsprisDKK = salgspris.ToString("0.00"); // Gemmer salgsprisen i item med 2 decimaler
                }
                else
                {
                    item.BeregnSalgsprisDKK = "N/A Invalid prisgruppe"; // Hvis prisgruppen er ugyldig
                }
                item.KostprisEUR = kostprisDKK.ToString("0.00"); // Opdaterer kostprisen i dkk med 2 decimaler
            }
            return prisliste; // Return listen
        }

        static string TildelPrisGruppe(string prisgruppe)
        {

            return prisgruppe switch
            {
                "21" => "30",
                "22" => "50",
                "23" or "24" => "40",
                "25" => "50",
                _ => "N/A", // udskriver N/A hvis der ikke er et match
            };
        }

        static void BlackTilSort(List<Varer> prisliste)
        {
            foreach (var item in prisliste) // Gennemgår listen
            {
                if (!string.IsNullOrEmpty(item.ArticleDescription) && item.ArticleDescription.Contains("black", StringComparison.CurrentCultureIgnoreCase)) // Hvis der er "black" i navn
                {
                    item.ArticleDescription = item.ArticleDescription.Replace("black", "sort"); // Udskifter "black" med "sort"
                }
            }
        }

        static void UdskrivCSV(List<Varer> prisliste, string filnavn) // Udskriver listen til en CSV fil
        {
            using var writer = new StreamWriter(filnavn); // Åbner filen for skrivning
            using var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) { Delimiter = ";" }); // Skriver til filen - - Delimiter sættes til semikolon
            csv.Context.RegisterClassMap<VarerMap>(); // Registrerer klassen
            csv.WriteRecords(prisliste); // Skriver listen til filen
            

            
        }

        public class Varer // Class til prisliste items
        {
            [Name("Item")]
            public string? Item { get; set; }

            [Name("article description")]
            public string? ArticleDescription { get; set; }

            [Name("kostpris EUR")]
            public string? KostprisEUR { get; set; }

            [Name("price group")]
            public int PriceGroup { get; set; }

            [Optional]
            public string? BeregnSalgsprisDKK { get; set; }
        }

        public class VarerMap : ClassMap<Varer> // ClassMap til prisliste items
        {
            public VarerMap()
            {
                Map(m => m.Item).Name("Varenummer");
                Map(m => m.ArticleDescription).Name("Navn");
                Map(m => m.KostprisEUR).Name("kostpris i danske kroner");
                Map(m => m.BeregnSalgsprisDKK).Name("Beregnet salgspris i danske kroner");
            }
        }
    }
}