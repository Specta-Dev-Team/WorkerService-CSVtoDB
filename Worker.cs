using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;

    private string SourceFolder;
    private string DestinationFolder;
    private string FailedFolder;
    private string LogFolder;
    private string ConnectionString;
    private int IntervalSeconds;

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;

        SourceFolder = _config["Folders:Source"];
        DestinationFolder = _config["Folders:Destination"];
        FailedFolder = _config["Folders:Failed"];
        LogFolder = _config["Folders:Logs"];
        ConnectionString = _config.GetConnectionString("DefaultConnection");
        IntervalSeconds = int.Parse(_config["ImportIntervalSeconds"]);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ProcessCsvFiles();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CSV files.");
            }

            await Task.Delay(IntervalSeconds * 1000, stoppingToken);
        }
    }

    private void ProcessCsvFiles()
    {
        if (!Directory.Exists(SourceFolder)) Directory.CreateDirectory(SourceFolder);
        if (!Directory.Exists(DestinationFolder)) Directory.CreateDirectory(DestinationFolder);
        if (!Directory.Exists(FailedFolder)) Directory.CreateDirectory(FailedFolder);
        if (!Directory.Exists(LogFolder)) Directory.CreateDirectory(LogFolder);

        string[] files = Directory.GetFiles(SourceFolder, "*.csv");
        foreach (var file in files)
        {
            try
            {
                string fileName = Path.GetFileName(file);
                _logger.LogInformation($"Processing {fileName}...");

                var lines = File.ReadAllLines(file);
                if (lines.Length <= 1) throw new Exception("CSV has no data rows.");

                using var connection = new NpgsqlConnection(ConnectionString);
                connection.Open();

                for (int i = 1; i < lines.Length; i++)
                {
                    var values = lines[i].Split(',');

                    DateTime delschdate = DateTime.Parse(values[0], CultureInfo.InvariantCulture);
                    string customer = values[1];
                    string cordinator = values[2];
                    string salorderno = values[3];
                    string jobno = values[4];
                    string itemcode = values[5];
                    string itemname = values[6];
                    int noofbrkdown = int.TryParse(values[7], out int brk) ? brk : 0;
                    int noofletterbrkdown = int.TryParse(values[8], out int letterBrk) ? letterBrk : 0;
                    string articleno = values[9];
                    string projectno = values[10];
                    string cpono = values[11];
                    string cusmatno = values[12];
                    string custrefno = values[13];
                    int soqty = int.TryParse(values[14], out int so) ? so : 0;
                    int delqty = int.TryParse(values[15], out int del) ? del : 0;
                    DateTime scheduledate = DateTime.Parse(values[16], CultureInfo.InvariantCulture);
                    string address = values[17];
                    string delplace = values[18];

                    string query = @"
                        INSERT INTO DeliverySchedule 
                        (delschdate, customer, cordinator, salorderno, jobno, itemcode, itemname, 
                         noofbrkdown, noofletterbrkdown, articleno, projectno, cpono, cusmatno, 
                         custrefno, soqty, delqty, scheduledate, address, delplace)
                        VALUES
                        (@delschdate, @customer, @cordinator, @salorderno, @jobno, @itemcode, @itemname,
                         @noofbrkdown, @noofletterbrkdown, @articleno, @projectno, @cpono, @cusmatno,
                         @custrefno, @soqty, @delqty, @scheduledate, @address, @delplace)";

                    using var cmd = new NpgsqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@delschdate", delschdate);
                    cmd.Parameters.AddWithValue("@customer", customer);
                    cmd.Parameters.AddWithValue("@cordinator", cordinator);
                    cmd.Parameters.AddWithValue("@salorderno", salorderno);
                    cmd.Parameters.AddWithValue("@jobno", jobno);
                    cmd.Parameters.AddWithValue("@itemcode", itemcode);
                    cmd.Parameters.AddWithValue("@itemname", itemname);
                    cmd.Parameters.AddWithValue("@noofbrkdown", noofbrkdown);
                    cmd.Parameters.AddWithValue("@noofletterbrkdown", noofletterbrkdown);
                    cmd.Parameters.AddWithValue("@articleno", articleno);
                    cmd.Parameters.AddWithValue("@projectno", projectno);
                    cmd.Parameters.AddWithValue("@cpono", cpono);
                    cmd.Parameters.AddWithValue("@cusmatno", cusmatno);
                    cmd.Parameters.AddWithValue("@custrefno", custrefno);
                    cmd.Parameters.AddWithValue("@soqty", soqty);
                    cmd.Parameters.AddWithValue("@delqty", delqty);
                    cmd.Parameters.AddWithValue("@scheduledate", scheduledate);
                    cmd.Parameters.AddWithValue("@address", address);
                    cmd.Parameters.AddWithValue("@delplace", delplace);
                    cmd.ExecuteNonQuery();
                }

                string destPath = Path.Combine(DestinationFolder, Path.GetFileName(file));
                if (File.Exists(destPath)) destPath = Path.Combine(DestinationFolder, $"{Path.GetFileNameWithoutExtension(file)}_{DateTime.Now:HHmmss}.csv");
                File.Move(file, destPath);
                _logger.LogInformation($"SUCCESS: {Path.GetFileName(file)} imported and moved.");
            }
            catch (Exception ex)
            {
                string destPath = Path.Combine(FailedFolder, Path.GetFileName(file));
                if (File.Exists(destPath)) destPath = Path.Combine(FailedFolder, $"{Path.GetFileNameWithoutExtension(file)}_{DateTime.Now:HHmmss}.csv");
                File.Move(file, destPath);
                _logger.LogError(ex, $"FAILED: {Path.GetFileName(file)}");
            }
        }
    }
}
