using HtmlAgilityPack;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Diagnostics;

namespace AruodasWebScraper.Scrap
{
    public class AruodasUrl
    {
        public const string DOMAIN_NAME = "http://www.aruodas.lt";
        public AruodasUrlParameters Parameters { get; private set; }
        public AruodasUrl(string url)
        {
            if (!url.StartsWith(DOMAIN_NAME))
                throw new Exception(string.Format("Url is not {0} domain", AruodasUrl.DOMAIN_NAME));
            Parameters = new AruodasUrlParameters(url);
        }
        public override string ToString()
        {
            return string.Format("{0}/?{1}", DOMAIN_NAME, Parameters.ToString());
        }
    }
    public class AruodasUrlParameters
    {
        const string URL_POSTFIX = "&act=makeSearch&FOrder=Actuality";
        /// <summary>
        /// Savivaldybe
        /// </summary>
        public int Region { get; set; }
        /// <summary>
        /// Gyvenviete
        /// </summary>
        public int District { get; set; }
        /// <summary>
        /// Ieskomu objektu tipas
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// Puslapis
        /// </summary>
        public int Page { get; set; }
        public AruodasUrlParameters(string url)
        {
            string urlQuery = new Uri(url).Query;
            var queryParameters = HttpUtility.ParseQueryString(urlQuery);
            Region = GetIntParameterValue(queryParameters, "FRegion", 0);
            District = GetIntParameterValue(queryParameters, "FDistrict", 0);
            Type = GetIntParameterValue(queryParameters, "obj", 0);
            Page = GetIntParameterValue(queryParameters, "Page", 0);
        }
        int GetIntParameterValue(NameValueCollection queryParameters, string parameterName, int defaultValue)
        {
            int value;
            if (int.TryParse(queryParameters.Get(parameterName), out value))
                return value;
            return defaultValue;
        }
        public override string ToString()
        {
            return string.Format("FDistrict={0}&obj={1}&FRegion={2}&Page={3}{4}", District, Type, Region, Page, URL_POSTFIX);
        }
    }
    public class AruodasPage
    {
        public HtmlNode MainContentNode { get; set; }
        public HtmlNode RecordsTableHeaderRowNode { get; set; }
        public HtmlNodeCollection RecordsRowNodes { get; set; }
        public HtmlNode PagingNode { get; set; }
        public AruodasPage(HtmlDocument htmlDoc)
        {
            MainContentNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='main-content']");
            RecordsTableHeaderRowNode = MainContentNode.SelectSingleNode("//table[@class='list-search']/thead/tr");
            RecordsRowNodes = MainContentNode.SelectNodes("//table[@class='list-search']/tbody/tr");
            PagingNode = MainContentNode.SelectSingleNode("//div[@class='pagination']");
        }
    }
    public class AruodasBaseData
    {
        /// <summary>
        /// Puslapiu su skelbimais skaicius
        /// </summary>
        public int PagesCount { get; set; }
        /// <summary>
        /// Skelbimu skaicius
        /// </summary>
        public int RecordsCount { get; set; }
    }
    public class AruodasBaseDataExtractor
    {
        public AruodasBaseData Extract(AruodasPage page)
        {
            AruodasBaseData baseData = new AruodasBaseData();
            baseData.PagesCount = Convert.ToInt32(page.PagingNode.SelectSingleNode("a[last()-1]").InnerText);
            string listCountText = page.RecordsTableHeaderRowNode.SelectSingleNode("th[@class='list-count']").InnerText;
            List<string> foundNumbersList = NumberExtractor.Instance.Extract(listCountText).ToList();
            baseData.RecordsCount = Convert.ToInt32(foundNumbersList.First());
            return baseData;
        }
    }
    public class AruodasRecordData
    {
        /// <summary>
        /// Unikalus aruodas.lt Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Url del platesnes informacijos
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Miestas
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// Mikrorajonas
        /// </summary>
        public string Borough { get; set; }
        /// <summary>
        /// Gatve
        /// </summary>
        public string Street { get; set; }
        /// <summary>
        /// Kambariu skaicius
        /// </summary>
        public int RoomsCount { get; set; }
        /// <summary>
        /// Buto plotas
        /// </summary>
        public int Area { get; set; }
        /// <summary>
        /// Aukstas
        /// </summary>
        public int Floor { get; set; }
        /// <summary>
        /// Viso aukstu name
        /// </summary>
        public int TotalFloors { get; set; }
        /// <summary>
        /// Kaina
        /// </summary>
        public int Price { get; set; }
    }
    public class AruodasRecordDataExtractor
    {
        const int MIN_RECORD_ROW_COLUMNS = 4;
        Regex _placeTextFormatter = new Regex("(^ )|( $)");
        public AruodasRecordData Extract(HtmlNode rowNode)
        {
            if (!IsRecordRowNode(rowNode)) return null;
            AruodasRecordData recordData = new AruodasRecordData();
            
            recordData.Id = rowNode.SelectSingleNode(".//div[@data-id]").Attributes["data-id"].Value;
            recordData.Url = rowNode.SelectSingleNode("td[2]/h3/a").Attributes["href"].Value;

            string placeText = rowNode.SelectSingleNode("td[2]/h3/a").InnerText;
            string[] placeTextSplittedAndFormatted = placeText.Split(',').Select((unformattedPlaceText => { return _placeTextFormatter.Replace(unformattedPlaceText, ""); })).ToArray();
            recordData.City = placeTextSplittedAndFormatted.Count() > 0 ? placeTextSplittedAndFormatted[0] : "";
            recordData.Borough = placeTextSplittedAndFormatted.Count() > 1 ? placeTextSplittedAndFormatted[1] : "";
            recordData.Street = placeTextSplittedAndFormatted.Count() > 2 ? placeTextSplittedAndFormatted[2] : "";

            string roomsCountText = rowNode.SelectSingleNode("td[3]").InnerText;
            List<string> foundNumbersList = NumberExtractor.Instance.Extract(roomsCountText).ToList();
            recordData.RoomsCount = Convert.ToInt32(foundNumbersList.First());

            string areaText = rowNode.SelectSingleNode("td[4]").InnerText;
            foundNumbersList = NumberExtractor.Instance.Extract(areaText).ToList();
            recordData.Area = Convert.ToInt32(foundNumbersList.First());
            
            string floorsText = rowNode.SelectSingleNode("td[5]").InnerText;
            foundNumbersList = NumberExtractor.Instance.Extract(floorsText).ToList();
            recordData.Floor = Convert.ToInt32(foundNumbersList.First());
            recordData.TotalFloors = Convert.ToInt32(foundNumbersList.Last());

            string priceText = rowNode.SelectSingleNode(".//*[@class='list-item-price']").InnerText;
            foundNumbersList = NumberExtractor.Instance.Extract(priceText).ToList();
            recordData.Price = Convert.ToInt32(foundNumbersList.First());
            return recordData;
        }
        bool IsRecordRowNode(HtmlNode rowNode)
        {
            return rowNode.SelectNodes("td").Count > MIN_RECORD_ROW_COLUMNS;
        }
    }
    public class AruodasRecordDataCollection : List<AruodasRecordData>
    {

    }
    public class NumberExtractor
    {
        static NumberExtractor _instance;
        public static NumberExtractor Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new NumberExtractor();
                return _instance;
            }
        }
        private NumberExtractor() { }

        const string REGEX_NUMBER_EXTRACTOR = @"\d+";
        Regex _regex = new Regex(REGEX_NUMBER_EXTRACTOR);
        public IEnumerable<string> Extract(string text)
        {
            MatchCollection matches = _regex.Matches(text);
            foreach (Match match in matches)
                yield return match.Value;
        }
    }
    public delegate void AruodasBaseDataHandler(AruodasBaseData data);
    public delegate void AruodasRecordDataHandler(AruodasRecordData recordData);
    public class AruodasScraper: IDisposable
    {
        AruodasUrl _url;
        int _requestIntervalMs;
        Thread _thread;
        bool _run = false;
        ManualResetEvent _sleeper = new ManualResetEvent(false);
        HtmlWeb _htmlDocumentGetter = new HtmlWeb();
        AruodasBaseDataExtractor _baseDataExtractor = new AruodasBaseDataExtractor();
        AruodasRecordDataExtractor _recordDataExtractor = new AruodasRecordDataExtractor();
        public event AruodasBaseDataHandler OnBaseDataLoaded;
        public event AruodasRecordDataHandler OnRecordDataExtracted;
        public AruodasScraper(string url, int requestIntervalMs = 0)
        {
            _url = new AruodasUrl(url);
            _requestIntervalMs = requestIntervalMs;
            _thread = new Thread(Run);
            _thread.Name = "Aruodas scraper";
        }
        public void Start()
        {
            _run = true;
            _sleeper.Reset();
            _thread.Start();
        }
        public void Stop()
        {
            _run = false;
            _sleeper.Set();
            _thread.Join();
        }
        public void Dispose()
        {
            _sleeper.Dispose();
        }
        void Run()
        {
            AruodasPage page = GetPage(_url);
            AruodasBaseData baseData = _baseDataExtractor.Extract(page);
            OnBaseDataLoaded?.Invoke(baseData);
            _url.Parameters.Page = 1;
            while (_run && _url.Parameters.Page < baseData.PagesCount)
            {
                page = GetPage(_url);
                foreach (var rowNode in page.RecordsRowNodes)
                    try
                    {
                        OnRecordDataExtracted?.Invoke(_recordDataExtractor.Extract(rowNode));
                    }
                    catch (Exception e) { Debug.WriteLine(e.Message + e.StackTrace); }
                _url.Parameters.Page++;
                if (_requestIntervalMs > 0)
                    _sleeper.WaitOne(_requestIntervalMs);
            }
        }
        AruodasPage GetPage(AruodasUrl url)
        {
            HtmlDocument htmlDoc = _htmlDocumentGetter.Load(_url.ToString());
            AruodasPage page = new AruodasPage(htmlDoc);
            return page;
        }
    }
}
