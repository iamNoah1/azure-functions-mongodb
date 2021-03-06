using BookReadingProject;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using Xunit;

namespace BookReadingRepositoryIntegrationTest
{
    public class BookReadingRepositoryTester
    {
        private static readonly HttpClient client = new HttpClient();

        private static string apiBaseUrl = GetSetting("API_BASE_URL");

        public static string GetSetting(string key)
        {
            var value = Environment.GetEnvironmentVariable(key);

            if (string.IsNullOrEmpty(value))
            {
                value = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location).AppSettings.Settings[key].Value;
            }

            return value;
        }

        [Fact]
        public async System.Threading.Tasks.Task BookReadingRepositoryShouldAddBookReadingEntryReturn200AndTheAddedBookReadingEntryIfSuccessfullyAdded()
        {
            CleanDB();

            BsonDocument bd = new BsonDocument {
                { "name", "universe in a nutshell"}
            };

            var content = new StringContent(bd.ToJson(), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiBaseUrl + "/api/bookreadings", content);
            var responseContent = response.Content.ReadAsStringAsync();

            BookReading result = JsonConvert.DeserializeObject<BookReading>(responseContent.Result);

            Assert.Equal(200, (int)response.StatusCode);
            Assert.NotEqual(ObjectId.Empty.ToString(), result.id);

            BookReading universe = GetDatabase().GetCollection<BookReading>("bookreadings").Find(entry => entry.name == "universe in a nutshell").Single();

            Assert.Equal("universe in a nutshell", universe.name);
            Assert.Equal(1, universe.priority);
        }

        private static IMongoDatabase GetDatabase()
        {
            string connectionString = GetSetting("MONGO_DB_CONNECTION_STRING");

            MongoClientSettings settings = MongoClientSettings.FromUrl(
                new MongoUrl(connectionString)
            );

            settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            var mongoClient = new MongoClient(settings);

            return mongoClient.GetDatabase("bookreadingsdb");
        }

        private void CleanDB()
        {
            GetDatabase().DropCollection("bookreadings");
        }

        [Fact]
        public async System.Threading.Tasks.Task BookReadingRepositoryShouldReturn200AndAllBookReadingEntries()
        {
            CleanDB();

            var collection = GetDatabase().GetCollection<BookReading>("bookreadings");

            BookReading universe = new BookReading();
            universe.name = "universe in a nutshell";

            BookReading refactoring = new BookReading();
            refactoring.name = "Refactoring";

            collection.InsertOne(universe);
            collection.InsertOne(refactoring);

            var response = await client.GetAsync(apiBaseUrl + "/api/bookreadings");
            var responseContent = response.Content.ReadAsStringAsync();

            BookReading[] result = JsonConvert.DeserializeObject<BookReading[]>(responseContent.Result);

            Assert.Equal(200, (int)response.StatusCode);

            Assert.NotEmpty(result);
            Assert.Equal(2, result.Length);

            Assert.Contains(universe, result);
            Assert.Contains(refactoring, result);
        }

        [Fact]
        public async System.Threading.Tasks.Task BookReadingRepositoryShouldAddBookReadingEntriesWithCorrectPriority()
        {
            CleanDB();

            BsonDocument bd = new BsonDocument {
                { "name", "universe in a nutshell"}
            };

            var content = new StringContent(bd.ToJson(), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiBaseUrl + "/api/bookreadings", content);
            var responseContent = response.Content.ReadAsStringAsync();

            bd = new BsonDocument {
                { "name", "Refactoring"}
            };

            content = new StringContent(bd.ToJson(), Encoding.UTF8, "application/json");

            response = await client.PostAsync(apiBaseUrl + "/api/bookreadings", content);
            responseContent = response.Content.ReadAsStringAsync();

            BookReading universe = GetDatabase().GetCollection<BookReading>("bookreadings").Find(entry => entry.name == "universe in a nutshell").Single();
            BookReading refactoring = GetDatabase().GetCollection<BookReading>("bookreadings").Find(entry => entry.name == "Refactoring").Single();

            Assert.Equal("universe in a nutshell", universe.name);
            Assert.Equal(1, universe.priority);

            Assert.Equal("Refactoring", refactoring.name);
            Assert.Equal(2, refactoring.priority);
        }

        [Fact]
        public async System.Threading.Tasks.Task BookReadingRepositoryShouldReturn200AndDeleteAnEntryIfDeletedSuccessfully()
        {
            CleanDB();

            var collection = GetDatabase().GetCollection<BookReading>("bookreadings");

            BookReading universe = new BookReading();
            universe.name = "universe in a nutshell";

            collection.InsertOne(universe);

            var response = await client.DeleteAsync(apiBaseUrl + "/api/bookreadings/" + universe.id);

            var bookReadings = await GetDatabase().GetCollection<BookReading>("bookreadings").Find(_ => true).ToListAsync();

            Assert.Equal(204, (int)response.StatusCode);
            Assert.Empty(bookReadings);
        }

    }
}
