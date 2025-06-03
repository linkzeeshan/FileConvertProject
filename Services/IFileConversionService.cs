using FileConvertPro.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileConvertPro.Services
{
    public interface IFileConversionService
    {
        /// <summary>
        /// Converts a file based on the specified conversion type
        /// </summary>
        /// <param name="file">The file to convert</param>
        /// <param name="conversionType">The type of conversion to perform</param>
        /// <param name="userId">The ID of the user performing the conversion</param>
        /// <returns>The ID of the created conversion record</returns>
        Task<int> ConvertFileAsync(IFormFile file, ConversionType conversionType, string userId);
        
        /// <summary>
        /// Gets a file conversion by its ID
        /// </summary>
        /// <param name="id">The ID of the conversion</param>
        /// <param name="userId">The ID of the user requesting the conversion</param>
        /// <returns>The file conversion record</returns>
        Task<FileConversion> GetConversionByIdAsync(int id, string userId);
        
        /// <summary>
        /// Gets all conversions for a user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>A collection of file conversion records</returns>
        Task<IEnumerable<FileConversion>> GetUserConversionsAsync(string userId);
    }
}
