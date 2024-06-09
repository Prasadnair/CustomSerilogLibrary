# CustomSerilogLibrary
Store the serilog Configuration in Database, and read it from database and store in cache. utilize this for logging.
# ensure that you have a database where you need to execute the below scripts.
Create a Table
CREATE TABLE SerilogConfig (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Configuration NVARCHAR(MAX) NOT NULL
);
# insert the data 
INSERT INTO SerilogConfig (Configuration)
VALUES (N'{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.MSSqlServer"],
    "MinimumLevel": {
      "Default": "Warning"
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "MSSqlServer",
        "Args": {
          "connectionString": "{SerilogConnectionString}",
          "tableName": "Logs",
          "autoCreateSqlTable": true
        }
      }
    ]
  }
}');


