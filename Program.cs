using AruodasWebScraper.Scrap;
using System;

namespace AruodasWebScraper
{
    class Program
    {
        static void Main(string[] args)
        {
            AruodasScraper scraper = new AruodasScraper("http://www.aruodas.lt/butai/vilniuje/?FDistrict=1&obj=4&FRegion=461&mod=Siulo&act=makeSearch&Page=1", 500);
            scraper.OnBaseDataLoaded += OnBaseDataLoaded;
            scraper.OnRecordDataExtracted += OnRecordDataExtracted;
            scraper.Start();
            Console.WriteLine("Press key to stop");
            Console.ReadKey(true);
            scraper.Stop();
            scraper.Dispose();
            Console.WriteLine("Press key to close");
            Console.ReadKey(true);
        }
        private static void OnRecordDataExtracted(AruodasRecordData recordData)
        {
            if (recordData == null)
                return;
            var d = recordData;
            Console.WriteLine(string.Format("Id={0}, City={1}, Borough={2}, Street={3}, RoomsCount={4}, Area={5}m^2, Floor={6}/{7}, Price={8}Eur", d.Id, d.City, d.Borough, d.Street, d.RoomsCount, d.Area, d.Floor, d.TotalFloors, d.Price));
            System.Threading.Thread.Sleep(2000);
        }
        private static void OnBaseDataLoaded(AruodasBaseData data)
        {
            Console.WriteLine(string.Format("PagesCount={0}, RecordsCount={1}", data.PagesCount, data.RecordsCount));
        }
    }
}
