using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using Amazon_Product_Surfer.Models;

namespace Amazon_Product_Surfer.Controllers
{
    public class HomeController : Controller
    {
        String accessKeyId = "XXXXXXXXXXXXXXXXXXXX"; // Insert Amazon access key.
        String secretAccessKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"; // Insert Amazon secret access key.

        List<ItemFromAmazon> items = new List<ItemFromAmazon>();
        List<ItemFromAmazon> itemsFromAmazon = new List<ItemFromAmazon>();

        // GET: Amazon_Product_Surfer
        public ActionResult Index(int error = 0)
        {
            ViewBag.error = error;
            return View();
        }

        // Processes the search calls from client: makes query to Amazon 
        // (many if there are lots of results) while taking into account Amazon's 
        // query limits. 
        // GET: Amazon_Product_Surfer/Search
        public ActionResult Search(int page, string searchTerm, int isLast=0)
        {
            // Catch potential web exceptions that Amazon will return.
            try {
                string itemNameFromSearch;
                try
                {
                    itemNameFromSearch = Request.Form["Name"].ToString();
                }
                catch (NullReferenceException e)
                {
                    itemNameFromSearch = searchTerm;
                }
                int pageNumberOfOurSite = page;
                // Calculates which page to query.
                var pageAndItemToQuery = calculateQueryPage(pageNumberOfOurSite);
                int pageNumberForURL = pageAndItemToQuery.Item1;
                // Which item in the response would be the first in our chosen page.
                int itemIndexToStartFrom = pageAndItemToQuery.Item2;

                //This is the first query URL that queries for initial result.
                string URLString = generateRequestURL(itemNameFromSearch, pageNumberForURL, false, ""); 
                string URLstringDebug = URLString;

                XmlDocument xml = new XmlDocument();
                xml.Load(URLString);
                XmlNodeList xnListTitles = xml.GetElementsByTagName("Title");
                XmlNodeList xnListURLs = xml.GetElementsByTagName("DetailPageURL");
                XmlNodeList xnListIDs = xml.GetElementsByTagName("ASIN");
                XmlNodeList xnListTotal = xml.GetElementsByTagName("TotalResults");

                // How many items are there in total in Amazon's database.
                int totalQueryResults = int.Parse(xnListTotal[0].InnerText); 

                // If there are items in the return we need to process them.
                if (xnListTitles.Count > 0)
                {
                    // Set a limit to the amount of items we can get.
                    int itemCount;

                    // Does the total amount of results limit preloaded page
                    if (totalQueryResults < 26) 
                    {
                        // Does the current page set a limit
                        if (xnListTitles.Count < 10) 
                        {
                            itemCount = xnListTitles.Count;
                        }
                        else
                        {
                            itemCount = totalQueryResults;
                        }
                    }
                    else
                    {
                        itemCount = 26;
                    }

                    string itemLookupCombo = "";
                    
                    // Loop through the items and add them to our items list.
                    // If the item count reaches 10 and we have more left, we need to 
                    // query amazon again.
                    for (int i = 0; i < itemCount; i++)
                    {
                        if (pageNumberForURL < 5)
                        {
                            // Makes a query every time it hits 10 items because of Amazon's limitation
                            // of 10 items per query.
                            if (itemIndexToStartFrom == 10)
                            {
                                // Reset response page item counter
                                itemIndexToStartFrom = 0;
                                // Increase the page we are looking to query from Amazon
                                pageNumberForURL += 1; 
                                string URLString3 = generateRequestURL(itemNameFromSearch, pageNumberForURL, false, "");
                                XmlDocument xml3 = new XmlDocument();

                                xml3.Load(URLString3);

                                // Update the previous lists
                                xnListTitles = xml3.GetElementsByTagName("Title");
                                xnListURLs = xml3.GetElementsByTagName("DetailPageURL");
                                xnListIDs = xml3.GetElementsByTagName("ASIN");

                                // If the response has less elements than 10 we should stop adding items.
                                // Check if page returned less than 10 items and is it more or less than we have left to add.
                                int howManyElementsOnPage = xnListTitles.Count; 
                                if (howManyElementsOnPage < 10 && (itemCount - i + 1) > howManyElementsOnPage) 
                                {
                                    // Readjust the 26 item limit to fit the item amount we got as a response
                                    itemCount -= itemCount - i + 1 - howManyElementsOnPage; 
                                }

                            }
                        }
                        else
                        {
                            break;
                        }
                        ItemFromAmazon item1 = new ItemFromAmazon(xnListIDs[itemIndexToStartFrom].InnerText, xnListTitles[itemIndexToStartFrom].InnerText,
                                                                    -1, -1, -1, -1, -1, -1, xnListURLs[itemIndexToStartFrom].InnerText);
                        itemIndexToStartFrom += 1;
                        itemsFromAmazon.Add(item1);
                    }

                    int elemCounter = 0;
                    int elementsLeft = itemsFromAmazon.Count;

                    // Query Amazon for Amazon's, new and used prices.
                    // Currently I couldn't find a XML format from them that 
                    // Would give me all the info I need so I had to query again.
                    foreach (ItemFromAmazon element in itemsFromAmazon)
                    {
                        elemCounter += 1;
                        itemLookupCombo = itemLookupCombo + "%2C%20" + element.ID;

                        // After ten items query Amazon for prices and add them to our the items
                        // in Amazon item list.
                        if (elemCounter % 10 == 0)
                        {
                            URLString = generateRequestURL("", 0, true, itemLookupCombo);
                            elementsLeft -= 10;
                            XmlDocument xml2 = new XmlDocument();
                            xml2.Load(URLString);
                            XmlNodeList xnItems = xml2.GetElementsByTagName("Item");

                            for (int i = 0; i < xnItems.Count - 1; i++)
                            {
                                int ItemIndex = elemCounter - 10 + i;
                                // The fishcatching is to prepare for situations where there is nothing to sell
                                // in the category for which we are looking for the price.
                                if (xnItems[i]["OfferSummary"] != null)
                                {
                                    try
                                    {
                                        string priceNew = xnItems[i]["OfferSummary"]["LowestNewPrice"]["Amount"].InnerText;
                                        int priceNewForItem = int.Parse(priceNew);
                                        itemsFromAmazon[ItemIndex].priceNew = priceNewForItem;
                                        itemsFromAmazon[ItemIndex].priceNewF = (float)priceNewForItem / 100;
                                    }
                                    catch (NullReferenceException e)
                                    {
                                        itemsFromAmazon[ItemIndex].priceNew = -1;
                                        itemsFromAmazon[ItemIndex].priceNewF = -1;
                                    }

                                    try
                                    {
                                        string priceUsed = xnItems[i]["OfferSummary"]["LowestUsedPrice"]["Amount"].InnerText;
                                        int priceUsedForItem = int.Parse(priceUsed);
                                        itemsFromAmazon[ItemIndex].priceUsed = priceUsedForItem;
                                        itemsFromAmazon[ItemIndex].priceUsedF = (float)priceUsedForItem / 100;
                                    }
                                    catch (NullReferenceException e)
                                    {
                                        itemsFromAmazon[ItemIndex].priceUsed = -1;
                                        itemsFromAmazon[ItemIndex].priceUsedF = -1;
                                    }

                                }
                                else
                                {
                                    itemsFromAmazon[ItemIndex].priceNew = -1;
                                    itemsFromAmazon[ItemIndex].priceUsed = -1;
                                    itemsFromAmazon[ItemIndex].priceNewF = -1;
                                    itemsFromAmazon[ItemIndex].priceUsedF = -1;
                                }

                                try
                                {
                                    string priceAmazon = xnItems[i]["Offers"]["Offer"]["OfferListing"]["Price"]["Amount"].InnerText;
                                    int priceAmazonForItem = int.Parse(priceAmazon);
                                    itemsFromAmazon[ItemIndex].priceAmazon = priceAmazonForItem;
                                    itemsFromAmazon[ItemIndex].priceAmazonF = (float)priceAmazonForItem / 100;
                                }
                                catch (NullReferenceException e)
                                {
                                    itemsFromAmazon[ItemIndex].priceAmazon = -1;
                                    itemsFromAmazon[ItemIndex].priceAmazonF = -1;
                                }
                            }

                            itemLookupCombo = "";
                        }
                    }

                    // This part is for the leftover elements that do not reach 10.
                    if (elementsLeft > 0)
                    {
                        URLString = generateRequestURL("", 0, true, itemLookupCombo);
                        XmlDocument xml2 = new XmlDocument();
                        xml2.Load(URLString);
                        XmlNodeList xnItems = xml2.GetElementsByTagName("Item");

                        for (int i = 0; i < xnItems.Count - 1; i++)
                        {
                            int ItemIndex = elemCounter - elementsLeft + i;
                            if (xnItems[i]["OfferSummary"] != null)
                            {
                                try
                                {
                                    string priceNew = xnItems[i]["OfferSummary"]["LowestNewPrice"]["Amount"].InnerText;
                                    int priceNewForItem = int.Parse(priceNew);
                                    itemsFromAmazon[ItemIndex].priceNew = priceNewForItem;
                                    itemsFromAmazon[ItemIndex].priceNewF = (float)priceNewForItem / 100;
                                }
                                catch (NullReferenceException e)
                                {
                                    itemsFromAmazon[ItemIndex].priceNew = -1;
                                    itemsFromAmazon[ItemIndex].priceNewF = -1;
                                }

                                try
                                {
                                    string priceUsed = xnItems[i]["OfferSummary"]["LowestUsedPrice"]["Amount"].InnerText;
                                    int priceUsedForItem = int.Parse(priceUsed);
                                    itemsFromAmazon[ItemIndex].priceUsed = priceUsedForItem;
                                    itemsFromAmazon[ItemIndex].priceUsedF = (float)priceUsedForItem / 100;
                                }
                                catch (NullReferenceException e)
                                {
                                    itemsFromAmazon[ItemIndex].priceUsed = -1;
                                    itemsFromAmazon[ItemIndex].priceUsedF = -1;
                                }

                            }
                            else
                            {
                                itemsFromAmazon[ItemIndex].priceNew = -1;
                                itemsFromAmazon[ItemIndex].priceUsed = -1;
                                itemsFromAmazon[ItemIndex].priceNewF = -1;
                                itemsFromAmazon[ItemIndex].priceUsedF = -1;
                            }
                            try
                            {
                                string priceAmazon = xnItems[i]["Offers"]["Offer"]["OfferListing"]["Price"]["Amount"].InnerText;
                                int priceAmazonForItem = int.Parse(priceAmazon);
                                itemsFromAmazon[ItemIndex].priceAmazon = priceAmazonForItem;
                                itemsFromAmazon[ItemIndex].priceAmazonF = (float)priceAmazonForItem / 100;
                            }
                            catch (NullReferenceException e)
                            {
                                itemsFromAmazon[ItemIndex].priceAmazon = -1;
                                itemsFromAmazon[ItemIndex].priceAmazonF = -1;
                            }
                        }
                        itemLookupCombo = "";
                    }
                }
                if (totalQueryResults > 39)
                {
                    totalQueryResults = 39;
                }
                double totalQueryDouble = Convert.ToDouble(totalQueryResults);
                double amountOfPages = Math.Ceiling(totalQueryDouble / 13);

                ViewBag.AmounOfPages = Convert.ToInt32(amountOfPages);
                ViewBag.CurrentPage = Convert.ToInt32(pageNumberOfOurSite);
                ViewBag.Message2 = URLstringDebug;

                string[] currencies = new string[] { "USD", "EUR", "RUB" };

                ViewBag.codes = new SelectList(currencies);

                // Function for future reference (how to operate with jason and models).
                /*string url = "https://openexchangerates.org/api/latest.json?app_id=ce2cdca5548143c7b99a4ecdb2b0f4e1";
                WebClient client = new WebClient();
                String jsonData = client.DownloadString(url);

                dynamic data = JObject.Parse(jsonData);
                string jasonDataRates = data.rates.ToString();
                Rootobject exchangeRates = JsonConvert.DeserializeObject<Rootobject>(jasonDataRates);*/

                ViewModel toView = new ViewModel(itemsFromAmazon);
                toView.searchTerm = itemNameFromSearch;
                toView.page = page;
                toView.islast = isLast;
                toView.maxPages = Convert.ToInt32(amountOfPages);

                return View(toView);
            }
            catch (WebException e)
            {
                return RedirectToAction("Index", new { error=1 });
            }
        }

        // This is the AJAX call that a page makes for the next page's content.
        // It's pretty much the same as main search action, with slight changes here and there.
        //[HttpPost]
        public PartialViewResult ajaxNewPage(int page, string searchTerm, string divToUpdate)
        {
            string itemNameFromSearch = "";
            itemNameFromSearch = searchTerm;

            int pageNumberOfOurSite = page;
            //Calculates which page to query
            var pageAndItemToQuery = calculateQueryPage(pageNumberOfOurSite);
            int pageNumberForURL = pageAndItemToQuery.Item1;
            //Which item in the response would be the first in our chosen page.
            int itemIndexToStartFrom = pageAndItemToQuery.Item2;

            string URLString = generateRequestURL(itemNameFromSearch, pageNumberForURL, false, ""); //This is the first query URL that queries for initial result.
            string URLstringDebug = URLString;

            XmlDocument xml = new XmlDocument();
            xml.Load(URLString);
            XmlNodeList xnListTitles = xml.GetElementsByTagName("Title");
            XmlNodeList xnListURLs = xml.GetElementsByTagName("DetailPageURL");
            XmlNodeList xnListIDs = xml.GetElementsByTagName("ASIN");
            XmlNodeList xnListTotal = xml.GetElementsByTagName("TotalResults");
            int totalQueryResults = int.Parse(xnListTotal[0].InnerText); //How many items are in total in Amazon's database

            if (xnListTitles.Count > 0)
            {
                int itemCount;// Set a limit to the amount of items we can get.

                if (totalQueryResults < 13) // Does the total amount of results limit preloaded page.
                {
                    if (xnListTitles.Count < 10) // Does the current page set a limit.
                    {
                        itemCount = xnListTitles.Count;
                    }
                    else
                    {
                        itemCount = totalQueryResults;
                    }
                }
                else
                {
                    itemCount = 13;
                }

                string itemLookupCombo = "";

                for (int i = 0; i < itemCount; i++)
                {
                    if (pageNumberForURL < 5)
                    {
                        // Makes a query every time it hits 10 items because of Amazon's limitation.
                        if (itemIndexToStartFrom == 10)
                        {
                            // Reset response page item counter
                            itemIndexToStartFrom = 0;
                            // Increase the page we are looking to query from Amazon
                            pageNumberForURL += 1; 
                            string URLString3 = generateRequestURL(itemNameFromSearch, pageNumberForURL, false, "");
                            XmlDocument xml3 = new XmlDocument();
                            xml3.Load(URLString3);
                            // Update the previous lists
                            xnListTitles = xml3.GetElementsByTagName("Title");
                            xnListURLs = xml3.GetElementsByTagName("DetailPageURL");
                            xnListIDs = xml3.GetElementsByTagName("ASIN");
                            // If the response has less elements than 10 we should stop adding items
                            // Check if page returned less than 10 items and is it more or less than we have left to add
                            int howManyElementsOnPage = xnListTitles.Count; 
                            if (howManyElementsOnPage < 10 && (itemCount - i + 1) > howManyElementsOnPage) 
                            {
                                // Readjust the 26 item limit to fit the item amount we got as a response
                                itemCount -= itemCount - i + 1 - howManyElementsOnPage; 
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                    ItemFromAmazon item1 = new ItemFromAmazon(xnListIDs[itemIndexToStartFrom].InnerText, xnListTitles[itemIndexToStartFrom].InnerText,
                                                                -1, -1, -1, -1, -1, -1, xnListURLs[itemIndexToStartFrom].InnerText);
                    itemIndexToStartFrom += 1;
                    itemsFromAmazon.Add(item1);
                }

                int elemCounter = 0;
                int elementsLeft = itemsFromAmazon.Count;

                // Query Amazon for Amazon's, new and used prices.
                // Currently I couldn't find a XML format from them that 
                // Would give me all the info I need so I had to query again.
                foreach (ItemFromAmazon element in itemsFromAmazon)
                {
                    elemCounter += 1;
                    itemLookupCombo = itemLookupCombo + "%2C%20" + element.ID;

                    // After ten items query Amazon for prices and add them to our the items
                    // in Amazon item list.
                    if (elemCounter % 10 == 0)
                    {

                        URLString = generateRequestURL("", 0, true, itemLookupCombo);
                        elementsLeft -= 10;
                        XmlDocument xml2 = new XmlDocument();
                        xml2.Load(URLString);
                        XmlNodeList xnItems = xml2.GetElementsByTagName("Item");

                        for (int i = 0; i < xnItems.Count - 1; i++)
                        {
                            int ItemIndex = elemCounter - 10 + i;
                            if (xnItems[i]["OfferSummary"] != null)
                            {
                                // The fishcatching is to prepare for situations where there is nothing to sell
                                // in the category for which we are looking for the price.
                                try
                                {
                                    string priceNew = xnItems[i]["OfferSummary"]["LowestNewPrice"]["Amount"].InnerText;
                                    int priceNewForItem = int.Parse(priceNew);
                                    itemsFromAmazon[ItemIndex].priceNew = priceNewForItem;
                                    itemsFromAmazon[ItemIndex].priceNewF = (float)priceNewForItem / 100;
                                }
                                catch (NullReferenceException e)
                                {
                                    itemsFromAmazon[ItemIndex].priceNew = -1;
                                    itemsFromAmazon[ItemIndex].priceNewF = -1;
                                }

                                try
                                {
                                    string priceUsed = xnItems[i]["OfferSummary"]["LowestUsedPrice"]["Amount"].InnerText;
                                    int priceUsedForItem = int.Parse(priceUsed);
                                    itemsFromAmazon[ItemIndex].priceUsed = priceUsedForItem;
                                    itemsFromAmazon[ItemIndex].priceUsedF = (float)priceUsedForItem / 100;
                                }
                                catch (NullReferenceException e)
                                {
                                    itemsFromAmazon[ItemIndex].priceUsed = -1;
                                    itemsFromAmazon[ItemIndex].priceUsedF = -1;
                                }

                            }
                            else
                            {
                                itemsFromAmazon[ItemIndex].priceNew = -1;
                                itemsFromAmazon[ItemIndex].priceUsed = -1;
                                itemsFromAmazon[ItemIndex].priceNewF = -1;
                                itemsFromAmazon[ItemIndex].priceUsedF = -1;
                            }

                            try
                            {
                                string priceAmazon = xnItems[i]["Offers"]["Offer"]["OfferListing"]["Price"]["Amount"].InnerText;
                                int priceAmazonForItem = int.Parse(priceAmazon);
                                itemsFromAmazon[ItemIndex].priceAmazon = priceAmazonForItem;
                                itemsFromAmazon[ItemIndex].priceAmazonF = (float)priceAmazonForItem / 100;
                            }
                            catch (NullReferenceException e)
                            {
                                itemsFromAmazon[ItemIndex].priceAmazon = -1;
                                itemsFromAmazon[ItemIndex].priceAmazonF = -1;
                            }
                        }

                        itemLookupCombo = "";
                    }
                }

                if (elementsLeft > 0)
                {
                    URLString = generateRequestURL("", 0, true, itemLookupCombo);
                    XmlDocument xml2 = new XmlDocument();
                    xml2.Load(URLString);
                    XmlNodeList xnItems = xml2.GetElementsByTagName("Item");

                    for (int i = 0; i < xnItems.Count - 1; i++)
                    {
                        int ItemIndex = elemCounter - elementsLeft + i;
                        if (xnItems[i]["OfferSummary"] != null)
                        {
                            try
                            {
                                string priceNew = xnItems[i]["OfferSummary"]["LowestNewPrice"]["Amount"].InnerText;
                                int priceNewForItem = int.Parse(priceNew);
                                itemsFromAmazon[ItemIndex].priceNew = priceNewForItem;
                                itemsFromAmazon[ItemIndex].priceNewF = (float)priceNewForItem / 100;
                            }
                            catch (NullReferenceException e)
                            {
                                itemsFromAmazon[ItemIndex].priceNew = -1;
                                itemsFromAmazon[ItemIndex].priceNewF = -1;
                            }

                            try
                            {
                                string priceUsed = xnItems[i]["OfferSummary"]["LowestUsedPrice"]["Amount"].InnerText;
                                int priceUsedForItem = int.Parse(priceUsed);
                                itemsFromAmazon[ItemIndex].priceUsed = priceUsedForItem;
                                itemsFromAmazon[ItemIndex].priceUsedF = (float)priceUsedForItem / 100;
                            }
                            catch (NullReferenceException e)
                            {
                                itemsFromAmazon[ItemIndex].priceUsed = -1;
                                itemsFromAmazon[ItemIndex].priceUsedF = -1;
                            }

                        }
                        else
                        {
                            itemsFromAmazon[ItemIndex].priceNew = -1;
                            itemsFromAmazon[ItemIndex].priceUsed = -1;
                            itemsFromAmazon[ItemIndex].priceNewF = -1;
                            itemsFromAmazon[ItemIndex].priceUsedF = -1;
                        }
                        try
                        {
                            string priceAmazon = xnItems[i]["Offers"]["Offer"]["OfferListing"]["Price"]["Amount"].InnerText;
                            int priceAmazonForItem = int.Parse(priceAmazon);
                            itemsFromAmazon[ItemIndex].priceAmazon = priceAmazonForItem;
                            itemsFromAmazon[ItemIndex].priceAmazonF = (float)priceAmazonForItem / 100;
                        }
                        catch (NullReferenceException e)
                        {
                            itemsFromAmazon[ItemIndex].priceAmazon = -1;
                            itemsFromAmazon[ItemIndex].priceAmazonF = -1;
                        }
                    }
                    itemLookupCombo = "";
                }
            }
            if (totalQueryResults > 39)
            {
                totalQueryResults = 39;
            }
            double totalQueryDouble = Convert.ToDouble(totalQueryResults);
            double amountOfPages = Math.Ceiling(totalQueryDouble / 13);

            ViewBag.AmounOfPages = Convert.ToInt32(amountOfPages);
            ViewBag.CurrentPage = Convert.ToInt32(pageNumberOfOurSite);
            ViewBag.Message2 = divToUpdate;

            ViewModel toView2 = new ViewModel(itemsFromAmazon);
            toView2.searchTerm = itemNameFromSearch;
            toView2.page = page;

            return PartialView("NextPageSearch", toView2);
        }
        
        // This will be AJAX called by client when changing exchange rates.
        // It will send the requested result as JSON to server.     
        public ActionResult GetExchangeRate(string Currency)
        {  
            string url = "https://openexchangerates.org/api/latest.json?app_id=8893560cc1e84907956b05d8f0bd3545";
            WebClient client = new WebClient();
            String jsonData = client.DownloadString(url);

            dynamic data = JObject.Parse(jsonData);
            string jasonDataRates = data.rates.ToString();
            Rootobject exchangeRates = JsonConvert.DeserializeObject<Rootobject>(jasonDataRates);

            PropertyInfo prop = typeof(Rootobject).GetProperty(Currency);
            float finalExchangeRate = (float)prop.GetValue(exchangeRates, null);

            return Json(finalExchangeRate, JsonRequestBehavior.AllowGet);
        }

        // Generates the request URL for amazon.
        public string generateRequestURL(string itemName, int pageNumber, bool checkPrice, string itemID)
        {
            string searchWord = cleanSearchWord(itemName);
            DateTime now = DateTime.UtcNow;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, 0, DateTimeKind.Utc);
            string TIMESTAMP_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffZ";
            string timeStampRaw = now.ToString(TIMESTAMP_FORMAT, CultureInfo.InvariantCulture);
            string timeStampAdjusted = timeStampRaw.Replace(":", "%3A");

            // Part of url necessary for REST.
            string URLAction = "GET";
            string URLEndPoint = "ecs.amazonaws.com";
            string URLPrefix2 = "/onca/xml";
            string URLAWSAccessKey = "AWSAccessKeyId=" + accessKeyId;
            string URLAssociateTag = "&AssociateTag=PutYourAssociateTagHere";
            string URLItemId = "";
            string URLItemPage = "&ItemPage=" + pageNumber;
            string URLKeywords = "&Keywords=" + searchWord;
            string URLOperation = "&Operation=ItemSearch";
            string URLResponseGroup = "";
            string URLSearchIndex = "&SearchIndex=All";
            string URLService = "&Service=AWSECommerceService";
            string URLTimestamp = "&Timestamp=" + timeStampAdjusted;
            string URLVersion = "&Version=2013-08-01";

            //This will adjust the URL for checking the prices.
            if (checkPrice)
            {
                URLItemId = "&ItemId=" + itemID;
                URLItemPage = "";
                URLKeywords = "";
                URLOperation = "&Operation=ItemLookup";
                URLResponseGroup = "&ResponseGroup=Offers"; 
                URLSearchIndex = "";
            }

            string forSignature = URLAction + "\n" + URLEndPoint + "\n" + URLPrefix2 + "\n" + URLAWSAccessKey
                                                   + URLAssociateTag + URLItemId + URLItemPage + URLKeywords + URLOperation
                                                   + URLResponseGroup + URLSearchIndex + URLService + URLTimestamp
                                                   + URLVersion;

            UTF8Encoding encoding = new UTF8Encoding();
            HMACSHA256 hmac = new HMACSHA256(encoding.GetBytes(secretAccessKey));
            string signature = Convert.ToBase64String(hmac.ComputeHash(encoding.GetBytes(forSignature)));

            string signature2 = signature.Replace("+", "%2B");
            string signature3 = signature2.Replace("=", "%3D");

            string finalURL = "http://" + URLEndPoint + URLPrefix2 + "?" + URLAWSAccessKey + URLAssociateTag + URLItemId
                                        + URLItemPage + URLKeywords + URLOperation + URLResponseGroup
                                        + URLSearchIndex + URLService + URLTimestamp + URLVersion
                                        + "&Signature=" + signature3;

            return finalURL;
        }

        // Calculate the page to query from Amazon and the index number
        // of the first element.
        public Tuple<int, int> calculateQueryPage(double pageNumber)
        {
            double firstItemOnWebPage = (pageNumber - 1) * 13 + 1;
            double pageToQuery = Math.Floor((firstItemOnWebPage - 1) / 10) + 1;
            double firstItemInResponse = (firstItemOnWebPage / 10 - Math.Floor(firstItemOnWebPage / 10)) * 10;

            if (firstItemInResponse == 0)
            {
                firstItemInResponse = 9;
            }
            else
            {
                firstItemInResponse = firstItemInResponse - 1;
            }

            return Tuple.Create(Convert.ToInt32(pageToQuery), Convert.ToInt32(firstItemInResponse));
        }

        public string cleanSearchWord(string itemName)
        {
            string result;
            if (itemName == "")
            {
                result = "%20";
            }
            else
            {
                result = Uri.EscapeDataString(itemName);
            }
            return result;
        }
    }
}