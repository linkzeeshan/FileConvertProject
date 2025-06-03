using FileConvertPro.Data;
using FileConvertPro.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace FileConvertPro.Services
{
    public class FileConversionService : IFileConversionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileConversionService> _logger;

        public FileConversionService(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<FileConversionService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<int> ConvertFileAsync(IFormFile file, ConversionType conversionType, string userId)
        {
            try
            {
                // Create directories if they don't exist
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", userId);
                var convertedFolder = Path.Combine(_environment.WebRootPath, "converted", userId);

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                if (!Directory.Exists(convertedFolder))
                    Directory.CreateDirectory(convertedFolder);

                // Generate unique file names
                var originalFileName = Path.GetFileName(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
                var originalFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the original file
                using (var stream = new FileStream(originalFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create a record in the database
                var conversion = new FileConversion
                {
                    UserId = userId,
                    OriginalFileName = originalFileName,
                    OriginalFilePath = originalFilePath,
                    ConversionType = conversionType,
                    Status = ConversionStatus.Processing,
                    CreatedAt = DateTime.UtcNow
                };

                _context.FileConversions.Add(conversion);
                await _context.SaveChangesAsync();

                // Process the conversion (synchronously for now, can be moved to background job later)
                await ProcessConversionAsync(conversion);

                return conversion.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file conversion");
                throw;
            }
        }

        public async Task<FileConversion> GetConversionByIdAsync(int id, string userId)
        {
            return await _context.FileConversions
                .Where(c => c.Id == id && c.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<FileConversion>> GetUserConversionsAsync(string userId)
        {
            return await _context.FileConversions
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        private async Task ProcessConversionAsync(FileConversion conversion)
        {
            try
            {
                var convertedFolder = Path.Combine(_environment.WebRootPath, "converted", conversion.UserId);
                var convertedFileName = $"{Path.GetFileNameWithoutExtension(conversion.OriginalFileName)}_{Guid.NewGuid()}";
                string convertedFilePath = null;

                switch (conversion.ConversionType)
                {
                    case ConversionType.JpgToPng:
                        convertedFilePath = await ConvertImageAsync(conversion.OriginalFilePath, convertedFolder, convertedFileName, ".png");
                        break;
                    case ConversionType.PngToJpg:
                        convertedFilePath = await ConvertImageAsync(conversion.OriginalFilePath, convertedFolder, convertedFileName, ".jpg");
                        break;
                    case ConversionType.JpgToWebp:
                    case ConversionType.PngToWebp:
                        convertedFilePath = await ConvertImageAsync(conversion.OriginalFilePath, convertedFolder, convertedFileName, ".webp");
                        break;
                    case ConversionType.WebpToJpg:
                        convertedFilePath = await ConvertImageAsync(conversion.OriginalFilePath, convertedFolder, convertedFileName, ".jpg");
                        break;
                    case ConversionType.WebpToPng:
                        convertedFilePath = await ConvertImageAsync(conversion.OriginalFilePath, convertedFolder, convertedFileName, ".png");
                        break;
                    case ConversionType.MDBToCSV:
                        // This is a Windows-only operation
                        if (OperatingSystem.IsWindows())
                        {
                            convertedFilePath = await ConvertMDBToCSVAsync(conversion.OriginalFilePath, convertedFolder, convertedFileName);
                        }
                        else
                        {
                            throw new PlatformNotSupportedException("MDB to CSV conversion is only supported on Windows.");
                        }
                        break;
                    // Other conversion types will be implemented later
                    default:
                        throw new NotImplementedException($"Conversion type {conversion.ConversionType} is not implemented yet.");
                }

                // Update the conversion record
                conversion.ConvertedFileName = Path.GetFileName(convertedFilePath);
                conversion.ConvertedFilePath = convertedFilePath;
                conversion.Status = ConversionStatus.Completed;
                conversion.CompletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Update the conversion record with error
                conversion.Status = ConversionStatus.Failed;
                conversion.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync();

                _logger.LogError(ex, $"Error processing conversion {conversion.Id}");
            }
        }

        private async Task<string> ConvertImageAsync(string sourcePath, string destinationFolder, string fileName, string extension)
        {
            var destinationPath = Path.Combine(destinationFolder, $"{fileName}{extension}");

            using (var image = await Image.LoadAsync(sourcePath))
            {
                switch (extension.ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                        await image.SaveAsJpegAsync(destinationPath);
                        break;
                    case ".png":
                        await image.SaveAsPngAsync(destinationPath);
                        break;
                    case ".webp":
                        await image.SaveAsWebpAsync(destinationPath);
                        break;
                    default:
                        throw new NotSupportedException($"Image format {extension} is not supported.");
                }
            }

            return destinationPath;
        }

        // Additional conversion methods will be implemented as needed
        
        [SupportedOSPlatform("windows")]
        private async Task<string> ConvertMDBToCSVAsync(string sourcePath, string destinationFolder, string fileName)
        {
            var destinationPath = Path.Combine(destinationFolder, $"{fileName}.csv");
            
            try
            {
                // Try different providers in order of preference
                string[] providers = new string[] 
                { 
                    "Microsoft.ACE.OLEDB.12.0",  // Office 2010 and later
                    "Microsoft.Jet.OLEDB.4.0"    // Older Access versions
                };
                
                OleDbConnection connection = null;
                Exception lastException = null;
                
                // Try each provider until one works
                foreach (var provider in providers)
                {
                    try
                    {
                        string connectionString = $"Provider={provider};Data Source={sourcePath};Persist Security Info=False;";
                        _logger.LogInformation($"Attempting to connect with provider: {provider}");
                        
                        connection = new OleDbConnection(connectionString);
                        await connection.OpenAsync();
                        
                        // If we get here, the connection succeeded
                        _logger.LogInformation($"Successfully connected with provider: {provider}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, $"Failed to connect with provider {provider}. Trying next provider if available.");
                        
                        // Dispose of the connection if it was created
                        if (connection != null)
                        {
                            connection.Dispose();
                            connection = null;
                        }
                    }
                }
                
                // If all providers failed, throw the last exception
                if (connection == null)
                {
                    throw new Exception("Failed to connect to the MDB file. Make sure you have the Microsoft Access Database Engine installed.", lastException);
                }
                
                using (connection) // Use the successful connection
                {
                    // Get all tables in the database
                    _logger.LogInformation("Getting schema information from MDB file");
                    DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
                    
                    if (schemaTable == null || schemaTable.Rows.Count == 0)
                    {
                        throw new Exception("No tables found in the MDB file.");
                    }
                    
                    // Use the first table for simplicity (can be enhanced to handle multiple tables)
                    string tableName = schemaTable.Rows[0]["TABLE_NAME"].ToString();
                    _logger.LogInformation($"Converting table: {tableName}");
                    
                    // Query the table
                    _logger.LogInformation($"Querying data from table: {tableName}");
                    OleDbCommand command = new OleDbCommand($"SELECT * FROM [{tableName}]", connection);
                    OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    
                    _logger.LogInformation($"Retrieved {dataTable.Rows.Count} rows from table {tableName}");
                    
                    // Write to CSV file
                    _logger.LogInformation($"Writing data to CSV file: {destinationPath}");
                    using (StreamWriter writer = new StreamWriter(destinationPath, false, Encoding.UTF8))
                    {
                        // Write headers
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            writer.Write(dataTable.Columns[i].ColumnName);
                            if (i < dataTable.Columns.Count - 1)
                                writer.Write(",");
                        }
                        writer.WriteLine();
                        
                        // Write data rows
                        foreach (DataRow row in dataTable.Rows)
                        {
                            for (int i = 0; i < dataTable.Columns.Count; i++)
                            {
                                string value = row[i]?.ToString() ?? string.Empty;
                                // Handle values with commas, quotes, etc.
                                if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                                {
                                    value = $"\"{value.Replace("\"", "\"\"")}\"";
                                }
                                writer.Write(value);
                                if (i < dataTable.Columns.Count - 1)
                                    writer.Write(",");
                            }
                            writer.WriteLine();
                        }
                    }
                    
                    _logger.LogInformation($"Successfully converted MDB to CSV: {destinationPath}");
                }
                
                return destinationPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting MDB to CSV: {Message}", ex.Message);
                
                // Create a simple CSV file with the error message so the user gets some output
                try 
                {
                    using (StreamWriter writer = new StreamWriter(destinationPath, false, Encoding.UTF8))
                    {
                        writer.WriteLine("Error,Details");
                        writer.WriteLine($"\"Conversion failed\",\"{ex.Message.Replace("\"", "\"\"")}\""); 
                    }
                    
                    _logger.LogInformation("Created error CSV file for failed conversion");
                    return destinationPath;
                }
                catch (Exception writeEx)
                {
                    _logger.LogError(writeEx, "Failed to create error CSV file");
                }
                
                throw new Exception($"Error converting MDB to CSV: {ex.Message}", ex);
            }
        }
    }
}
