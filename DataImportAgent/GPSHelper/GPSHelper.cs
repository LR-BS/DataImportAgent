using Newtonsoft.Json;
using SharedKernel.Domain;
using System.Net.Http.Headers;

namespace DataImportAgent;

public class GPSHelper : IGPSHelper
{
    public (double, double) GetGeolocationData(Property property)
    {
        var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TestClient", "1.0"));

        string? addressQuery = $"https://nominatim.openstreetmap.org/search?q={property.PostCode} {property.City} {property.Street} {property.Housenumber}&format=json&polygon=1&addressdetails=1";

        HttpResponseMessage response = httpClient.GetAsync(addressQuery).Result;
        var httpResult = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result, typeof(OSMObject[])) as OSMObject[];
        if (httpResult != null && httpResult.Any())
        {
            return (
            decimal.ToDouble(Math.Round(httpResult[0].lat, 6)),
            decimal.ToDouble(Math.Round(httpResult[0].lon, 6)));
        }

        return (0, 0);
    }

    private class OSMAddress
    {
        public string house_number { get; set; }
        public string road { get; set; }
        public string town { get; set; }
        public string city { get; set; }
        public string county { get; set; }
        public string state { get; set; }
        public string postcode { get; set; }
        public string country { get; set; }
        public string country_code { get; set; }
    }

    private class OSMObject
    {
        public long place_id { get; set; }
        public string licence { get; set; }
        public string osm_type { get; set; }
        public long osm_id { get; set; }
        public decimal[] boundingbox { get; set; }
        public decimal lat { get; set; }
        public decimal lon { get; set; }
        public string display_name { get; set; }

        //public string class { get; set; }
        public string type { get; set; }

        public decimal importance { get; set; }
        public OSMAddress address { get; set; }
    }
}