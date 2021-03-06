﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data.SQLite;

namespace _8BitIMS
{
    /// <summary>
    /// DatabaseInit initializes the database by setting up all the necessary tables, and 
    /// then pulls data from IGDB and inserts the necessary platforms and games into the database.
    /// </summary>
    class DatabaseInit
    {
        private static string DATABASE = "Data Source = inventory.db";
        private static string URL_GAMES = "https://igdbcom-internet-game-database-v1.p.mashape.com/games/?fields=id,name&limit=50&offset=";
        private static string URL_PLATS = "https://igdbcom-internet-game-database-v1.p.mashape.com/platforms/?fields=id,name,games&limit=50&offset=";
        private static string APIKEY = "ZiFOOa8MinmshtGhh0BFXTRsj1Avp1YoihcjsnPU3JAvg3UGPJ";
        private static string ACCEPT = "application/json";

        public DatabaseInit()
        {
           // SetUpTables();
           // InitializeDatabase();
        }

        /// <summary>
        /// Loops through each page from the Internet Games Database (IGDB) and pulls data in the form of JSON
        /// and parses it into C# objects to be queried into SQL database for use within the application. 
        /// </summary>
        private void InitializeDatabase()
        {
            SQLiteConnection conn = new SQLiteConnection(DATABASE); // Sets up a new database connection
            conn.Open();                                            // Opens the database for queries
            var command = conn.CreateCommand();                     // Creates a command variable for SQL commands

            WebClient wc = new WebClient();                         // Webclient is used to scrape from a URL                
            JObject data = null;                                    // Retains the data from the URL in the form of JTokens
            string response;                                        // Response from the URL
            wc.Headers.Add("X-Mashape-Key", APIKEY);                // Adding the Headers to the WebClient - API token
            wc.Headers.Add("Accept", ACCEPT);                       // Telling the WebClient what to accept from the URL

            List<Games> gamesList = new List<Games>();              // List of all game data from the IGDB
            List<Platforms> platsList = new List<Platforms>();      // List of all platform data from IGDB
                                         
            int x = 50;                                             // Offset value for pagination while getting data from IGDB
            int y = 0;                                              // Value for terminating loops

            do
            {
                response = wc.DownloadString(URL_PLATS + x);
                if (response.StartsWith("["))
                {
                    y = 0;
                    while (y < 50)
                    {
                        try
                        {
                            Platforms platform = new Platforms();
                            data = JsonConvert.DeserializeObject<List<JObject>>(response)[y];
                            platform.id = (int)data["id"];
                            platform.name = (string)data["name"];
                            var tokenObj = data["games"];
                            platform.games = tokenObj.ToObject<List<int>>();
                            platsList.Add(platform);
                            y++;


                            Console.WriteLine(data["name"]);
                        } catch (ArgumentOutOfRangeException e)
                        { 
                            break;
                        }
                    }
                }
                x += 50;
            } while (!response.StartsWith("[]"));

            x = y = 0;
            while (x <= 9900)
            {
                response = wc.DownloadString(URL_GAMES + x);
                if (response.StartsWith("["))
                {
                    y = 0;
                    while (y < 50)
                    {
                        Games game = new Games();
                        data = JsonConvert.DeserializeObject<List<JObject>>(response)[y];
                        game.id = (int)data["id"];
                        game.name = (string)data["name"];
                        gamesList.Add(game);
                        y++;

                        Console.WriteLine(data["name"]);
                    }
                    x += 50;
                    
                }

            }
            foreach (Games game in gamesList)
            {
                command.CommandText = "INSERT INTO games(id, name, quantity) VALUES("
                            + game.id + ",'" + game.name.Replace("'", "''") + "', 0);";
                command.ExecuteNonQuery();
            }

            foreach (Platforms plat in platsList)
            {

                command.CommandText = "INSERT INTO platforms(id, name, quantity) VALUES("
                    + plat.id + ",'" + plat.name.Replace("'", "''") + "', 0);";
                command.ExecuteNonQuery();

                foreach (int i in plat.games)
                    foreach (Games title in gamesList)
                    {
                        if (title.id == i)
                        {
                            command.CommandText = "INSERT INTO multiplat_games(game_id, platform_id, quantity) VALUES("
                                + title.id + "," + plat.id + ", 0);";
                            command.ExecuteNonQuery();
                        }
                    }
            }

            
            
            conn.Close();
            
        }

        private void SetUpTables()
        {
            SQLiteConnection conn = new SQLiteConnection("Data Source = inventory.db");
            conn.Open();


            var command = conn.CreateCommand();

            command.CommandText = "CREATE TABLE IF NOT EXISTS platforms("
                + " id int PRIMARY KEY,"
                + " name text NOT NULL,"
                + " quantity int NOT NULL"
                + ");";
            command.ExecuteNonQuery();


            command.CommandText = "CREATE TABLE IF NOT EXISTS games("
               + " id int PRIMARY KEY," 
               + " name text NOT NULL," 
               + " quantity int NOT NULL"
               + ");";
            command.ExecuteNonQuery();


            command.CommandText = "CREATE TABLE IF NOT EXISTS multiplat_games("
                + " game_id int NOT NULL,"
                + " platform_id int NOT NULL,"
                + " quantity int NOT NULL,"
                + " PRIMARY KEY(game_id, platform_id),"
                + " CONSTRAINT fk_game_id FOREIGN KEY(game_id)REFERENCES games(id),"
                + " CONSTRAINT fk_platform_id FOREIGN KEY(platform_id) REFERENCES platforms(id)"
                + ");";
            command.ExecuteNonQuery();

            conn.Close();
        }
    }

    
}
